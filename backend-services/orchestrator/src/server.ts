import express, { Request, Response } from 'express';
import Docker from 'dockerode';
import { WebSocketServer, WebSocket } from 'ws';
import { v4 as uuidv4 } from 'uuid';
import http from 'http';
import cors from 'cors';
import dotenv from 'dotenv';

dotenv.config();

const PORT = parseInt(process.env.PORT || '5000', 10);
const GAME_SERVER_IMAGE = process.env.GAME_SERVER_IMAGE || 'freefire-game-server:latest';
const HOST_IP = process.env.HOST_IP || '0.0.0.0';
const BASE_GAME_PORT = parseInt(process.env.BASE_GAME_PORT || '7777', 10);

const docker = new Docker({ socketPath: '/var/run/docker.sock' });

interface GameServerInstance {
    containerId: string;
    matchId: string;
    port: number;
    status: 'starting' | 'running' | 'stopping' | 'stopped';
    createdAt: number;
    playerCount: number;
}

const activeServers = new Map<string, GameServerInstance>();
let nextPort = BASE_GAME_PORT;

const app = express();
app.use(cors());
app.use(express.json());

const server = http.createServer(app);
const wss = new WebSocketServer({ server, path: '/ws' });

const clientConnections = new Map<string, WebSocket>();

wss.on('connection', (ws: WebSocket) => {
    const connectionId = uuidv4();

    ws.on('message', (message: Buffer) => {
        try {
            const data = JSON.parse(message.toString());
            if (data.type === 'register' && data.playerId) {
                clientConnections.set(data.playerId, ws);
            }
        } catch {
            // ignore malformed messages
        }
    });

    ws.on('close', () => {
        for (const [playerId, conn] of clientConnections.entries()) {
            if (conn === ws) {
                clientConnections.delete(playerId);
                break;
            }
        }
    });

    ws.send(JSON.stringify({ type: 'connected', connectionId }));
});

function notifyPlayers(
    players: Array<{ playerId: string }>,
    serverInfo: { matchId: string; serverIp: string; port: number }
): void {
    const message = JSON.stringify({
        type: 'match_ready',
        matchId: serverInfo.matchId,
        serverIp: serverInfo.serverIp,
        port: serverInfo.port,
    });

    for (const player of players) {
        const ws = clientConnections.get(player.playerId);
        if (ws && ws.readyState === WebSocket.OPEN) {
            ws.send(message);
        }
    }
}

app.get('/health', (_req: Request, res: Response) => {
    res.json({
        status: 'ok',
        service: 'orchestrator',
        activeServers: activeServers.size,
    });
});

app.post('/api/orchestrator/allocate', async (req: Request, res: Response): Promise<void> => {
    try {
        const { matchId, players } = req.body;

        if (!matchId || !players || !Array.isArray(players)) {
            res.status(400).json({ error: 'matchId and players array are required.' });
            return;
        }

        const gamePort = nextPort++;
        const containerName = `freefire-gs-${matchId.slice(0, 8)}`;

        const container = await docker.createContainer({
            Image: GAME_SERVER_IMAGE,
            name: containerName,
            Env: [
                `MATCH_ID=${matchId}`,
                `PLAYER_COUNT=${players.length}`,
                `TICK_RATE=30`,
                `PORT=${gamePort}`,
            ],
            ExposedPorts: {
                [`${gamePort}/udp`]: {},
            },
            HostConfig: {
                PortBindings: {
                    [`${gamePort}/udp`]: [{ HostPort: `${gamePort}` }],
                },
                Memory: 512 * 1024 * 1024,
                NanoCpus: 1_000_000_000,
                AutoRemove: true,
            },
        });

        await container.start();

        const instance: GameServerInstance = {
            containerId: container.id,
            matchId,
            port: gamePort,
            status: 'running',
            createdAt: Date.now(),
            playerCount: players.length,
        };

        activeServers.set(matchId, instance);

        const serverInfo = { matchId, serverIp: HOST_IP, port: gamePort };
        notifyPlayers(players, serverInfo);

        console.log(`[orchestrator] Server allocated: ${containerName} on port ${gamePort}`);

        res.json({
            status: 'allocated',
            serverIp: HOST_IP,
            port: gamePort,
            matchId,
            containerId: container.id,
        });
    } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Allocation failed';
        console.error(`[orchestrator] Allocation error: ${message}`);
        res.status(500).json({ error: message });
    }
});

app.post('/api/orchestrator/deallocate', async (req: Request, res: Response): Promise<void> => {
    try {
        const { matchId } = req.body;

        const instance = activeServers.get(matchId);
        if (!instance) {
            res.status(404).json({ error: 'Match server not found.' });
            return;
        }

        const container = docker.getContainer(instance.containerId);
        await container.stop({ t: 5 });

        activeServers.delete(matchId);

        console.log(`[orchestrator] Server deallocated: match ${matchId}`);
        res.json({ status: 'deallocated', matchId });
    } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Deallocation failed';
        res.status(500).json({ error: message });
    }
});

app.get('/api/orchestrator/servers', (_req: Request, res: Response) => {
    const servers = Array.from(activeServers.values()).map((s) => ({
        matchId: s.matchId,
        port: s.port,
        status: s.status,
        playerCount: s.playerCount,
        uptimeMs: Date.now() - s.createdAt,
    }));

    res.json({ servers, total: servers.length });
});

setInterval(async () => {
    const maxLifetimeMs = 45 * 60 * 1000;
    const now = Date.now();

    for (const [matchId, instance] of activeServers.entries()) {
        if (now - instance.createdAt > maxLifetimeMs) {
            try {
                const container = docker.getContainer(instance.containerId);
                await container.stop({ t: 5 });
                activeServers.delete(matchId);
                console.log(`[orchestrator] Auto-recycled stale server: ${matchId}`);
            } catch (err) {
                console.error(`[orchestrator] Failed to recycle ${matchId}:`, err);
            }
        }
    }
}, 60_000);

server.listen(PORT, '0.0.0.0', () => {
    console.log(`[orchestrator] Listening on port ${PORT}`);
});

export { app, server };

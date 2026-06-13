import { Router, Response } from 'express';
import { authenticateToken, AuthenticatedRequest } from '../middleware/auth';
import { PlayerStats } from '../models/PlayerStats';
import { Inventory } from '../models/Inventory';
import { User } from '../models/User';

const router = Router();

router.get('/profile', authenticateToken, async (req: AuthenticatedRequest, res: Response): Promise<void> => {
    try {
        const userId = req.user!.userId;
        const user = await User.findById(userId).select('-passwordHash -refreshToken -emailHash');
        const stats = await PlayerStats.findOne({ userId });
        const inventory = await Inventory.findOne({ userId });

        if (!user) {
            res.status(404).json({ error: 'User not found.' });
            return;
        }

        res.json({
            profile: {
                userId: user._id,
                username: user.username,
                createdAt: user.createdAt,
                lastLogin: user.lastLogin,
            },
            stats: stats
                ? {
                      wins: stats.wins,
                      kills: stats.kills,
                      matchesPlayed: stats.matchesPlayed,
                      headshots: stats.headshots,
                      accuracyRate: stats.accuracyRate,
                      currentRankPoints: stats.currentRankPoints,
                      seasonHighRank: stats.seasonHighRank,
                  }
                : null,
            inventory: inventory
                ? {
                      items: inventory.items,
                      activeLoadout: inventory.activeLoadout,
                      slotsUsed: inventory.items.length,
                      maxSlots: inventory.maxSlots,
                  }
                : null,
        });
    } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Failed to load profile';
        res.status(500).json({ error: message });
    }
});

router.post('/inventory/equip', authenticateToken, async (req: AuthenticatedRequest, res: Response): Promise<void> => {
    try {
        const userId = req.user!.userId;
        const { itemId, slot } = req.body;

        if (!itemId || !slot) {
            res.status(400).json({ error: 'itemId and slot are required.' });
            return;
        }

        const inventory = await Inventory.findOne({ userId });
        if (!inventory) {
            res.status(404).json({ error: 'Inventory not found.' });
            return;
        }

        const ownedItem = inventory.items.find((item) => item.itemId === itemId);
        if (!ownedItem) {
            res.status(403).json({ error: 'Item not owned.' });
            return;
        }

        switch (slot) {
            case 'character':
                if (ownedItem.itemType !== 'character') {
                    res.status(400).json({ error: 'Item is not a character.' });
                    return;
                }
                inventory.activeLoadout.characterId = itemId;
                break;
            case 'glooWallSkin':
                if (ownedItem.itemType !== 'gloo_wall_skin') {
                    res.status(400).json({ error: 'Item is not a gloo wall skin.' });
                    return;
                }
                inventory.activeLoadout.glooWallSkin = itemId;
                break;
            case 'pet':
                if (ownedItem.itemType !== 'pet') {
                    res.status(400).json({ error: 'Item is not a pet.' });
                    return;
                }
                inventory.activeLoadout.petId = itemId;
                break;
            default:
                if (ownedItem.itemType !== 'weapon_skin') {
                    res.status(400).json({ error: 'Item is not a weapon skin.' });
                    return;
                }
                inventory.activeLoadout.weaponSkins.set(slot, itemId);
                break;
        }

        await inventory.save();

        res.json({
            status: 'Equipped',
            activeLoadout: inventory.activeLoadout,
        });
    } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Failed to equip item';
        res.status(500).json({ error: message });
    }
});

router.get('/leaderboard', async (_req, res: Response): Promise<void> => {
    try {
        const topPlayers = await PlayerStats.find()
            .sort({ currentRankPoints: -1 })
            .limit(100)
            .populate('userId', 'username')
            .lean();

        res.json({
            leaderboard: topPlayers.map((p, index) => ({
                rank: index + 1,
                username: (p.userId as { username: string })?.username ?? 'Unknown',
                rankPoints: p.currentRankPoints,
                wins: p.wins,
                kills: p.kills,
            })),
        });
    } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Failed to load leaderboard';
        res.status(500).json({ error: message });
    }
});

export { router as lobbyRouter };

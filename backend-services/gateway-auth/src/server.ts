import express from 'express';
import mongoose from 'mongoose';
import cors from 'cors';
import helmet from 'helmet';
import rateLimit from 'express-rate-limit';
import dotenv from 'dotenv';
import { authRouter } from './routes/auth';
import { lobbyRouter } from './routes/lobby';

dotenv.config();

const app = express();
const PORT = parseInt(process.env.PORT || '3000', 10);
const MONGO_URI = process.env.MONGO_URI || 'mongodb://mongo:27017/freefire';

app.use(helmet());
app.use(cors());
app.use(express.json({ limit: '1mb' }));

const globalLimiter = rateLimit({
    windowMs: 15 * 60 * 1000,
    max: 100,
    standardHeaders: true,
    legacyHeaders: false,
    message: { error: 'Too many requests, please try again later.' },
});
app.use(globalLimiter);

app.use('/api/auth', authRouter);
app.use('/api/lobby', lobbyRouter);

app.get('/health', (_req, res) => {
    res.json({ status: 'ok', service: 'gateway-auth', uptime: process.uptime() });
});

async function bootstrap(): Promise<void> {
    try {
        await mongoose.connect(MONGO_URI, {
            maxPoolSize: 10,
            serverSelectionTimeoutMS: 5000,
            socketTimeoutMS: 45000,
        });
        console.log('[gateway-auth] MongoDB connected.');

        app.listen(PORT, '0.0.0.0', () => {
            console.log(`[gateway-auth] Auth gateway listening on port ${PORT}`);
        });
    } catch (err) {
        console.error('[gateway-auth] Failed to start:', err);
        process.exit(1);
    }
}

bootstrap();

export { app };

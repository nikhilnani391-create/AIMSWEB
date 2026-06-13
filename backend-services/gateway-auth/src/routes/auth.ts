import { Router, Request, Response } from 'express';
import jwt from 'jsonwebtoken';
import argon2 from 'argon2';
import crypto from 'crypto';
import { User } from '../models/User';
import { PlayerStats } from '../models/PlayerStats';
import { Inventory } from '../models/Inventory';

const router = Router();

const JWT_SECRET = process.env.JWT_SECRET || 'free_fire_ultra_secret_key';
const JWT_EXPIRY = '1h';
const REFRESH_EXPIRY = '7d';

function generateRefreshToken(): string {
    return crypto.randomBytes(64).toString('hex');
}

function hashEmail(email: string): string {
    return crypto.createHash('sha256').update(email.toLowerCase().trim()).digest('hex');
}

router.post('/register', async (req: Request, res: Response): Promise<void> => {
    try {
        const { username, email, password } = req.body;

        if (!username || !email || !password) {
            res.status(400).json({ error: 'Username, email, and password are required.' });
            return;
        }

        if (password.length < 8) {
            res.status(400).json({ error: 'Password must be at least 8 characters.' });
            return;
        }

        if (username.length < 3 || username.length > 24) {
            res.status(400).json({ error: 'Username must be between 3 and 24 characters.' });
            return;
        }

        const emailHash = hashEmail(email);
        const passwordHash = await argon2.hash(password, {
            type: argon2.argon2id,
            memoryCost: 65536,
            timeCost: 3,
            parallelism: 4,
        });

        const newUser = new User({ username, emailHash, passwordHash });
        await newUser.save();

        await PlayerStats.create({ userId: newUser._id });
        await Inventory.create({ userId: newUser._id });

        const accessToken = jwt.sign(
            { userId: newUser._id.toString(), username: newUser.username },
            JWT_SECRET,
            { expiresIn: JWT_EXPIRY }
        );

        const refreshToken = generateRefreshToken();
        newUser.refreshToken = refreshToken;
        await newUser.save();

        res.status(201).json({
            status: 'Success',
            userId: newUser._id,
            accessToken,
            refreshToken,
        });
    } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Registration failed';
        if (message.includes('duplicate key')) {
            res.status(409).json({ error: 'Username or email already exists.' });
            return;
        }
        res.status(400).json({ error: message });
    }
});

router.post('/login', async (req: Request, res: Response): Promise<void> => {
    try {
        const { username, password } = req.body;

        if (!username || !password) {
            res.status(400).json({ error: 'Username and password are required.' });
            return;
        }

        const user = await User.findOne({ username });
        if (!user) {
            res.status(401).json({ error: 'Invalid credentials.' });
            return;
        }

        const validPassword = await argon2.verify(user.passwordHash, password);
        if (!validPassword) {
            res.status(401).json({ error: 'Invalid credentials.' });
            return;
        }

        user.lastLogin = new Date();
        const refreshToken = generateRefreshToken();
        user.refreshToken = refreshToken;
        await user.save();

        const accessToken = jwt.sign(
            { userId: user._id.toString(), username: user.username },
            JWT_SECRET,
            { expiresIn: JWT_EXPIRY }
        );

        const stats = await PlayerStats.findOne({ userId: user._id });

        res.status(200).json({
            accessToken,
            refreshToken,
            rankPoints: stats?.currentRankPoints ?? 1000,
            username: user.username,
        });
    } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Login failed';
        res.status(500).json({ error: message });
    }
});

router.post('/refresh', async (req: Request, res: Response): Promise<void> => {
    try {
        const { refreshToken } = req.body;

        if (!refreshToken) {
            res.status(400).json({ error: 'Refresh token required.' });
            return;
        }

        const user = await User.findOne({ refreshToken });
        if (!user) {
            res.status(403).json({ error: 'Invalid refresh token.' });
            return;
        }

        const newAccessToken = jwt.sign(
            { userId: user._id.toString(), username: user.username },
            JWT_SECRET,
            { expiresIn: JWT_EXPIRY }
        );

        const newRefreshToken = generateRefreshToken();
        user.refreshToken = newRefreshToken;
        await user.save();

        res.status(200).json({
            accessToken: newAccessToken,
            refreshToken: newRefreshToken,
        });
    } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Token refresh failed';
        res.status(500).json({ error: message });
    }
});

router.post('/logout', async (req: Request, res: Response): Promise<void> => {
    try {
        const { refreshToken } = req.body;
        if (refreshToken) {
            await User.findOneAndUpdate({ refreshToken }, { refreshToken: null });
        }
        res.status(200).json({ status: 'Logged out.' });
    } catch {
        res.status(500).json({ error: 'Logout failed.' });
    }
});

export { router as authRouter };

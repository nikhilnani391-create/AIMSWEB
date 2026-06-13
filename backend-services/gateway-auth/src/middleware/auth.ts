import { Request, Response, NextFunction } from 'express';
import jwt from 'jsonwebtoken';

const JWT_SECRET = process.env.JWT_SECRET || 'free_fire_ultra_secret_key';

export interface AuthPayload {
    userId: string;
    username: string;
}

export interface AuthenticatedRequest extends Request {
    user?: AuthPayload;
}

export function authenticateToken(
    req: AuthenticatedRequest,
    res: Response,
    next: NextFunction
): void {
    const authHeader = req.headers.authorization;
    const token = authHeader?.startsWith('Bearer ') ? authHeader.slice(7) : null;

    if (!token) {
        res.status(401).json({ error: 'Access token required.' });
        return;
    }

    try {
        const decoded = jwt.verify(token, JWT_SECRET) as AuthPayload;
        req.user = decoded;
        next();
    } catch {
        res.status(403).json({ error: 'Invalid or expired token.' });
    }
}

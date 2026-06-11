import type { Request, Response, NextFunction } from 'express';

/**
 * Extend Express Request to include the authenticated user's ID.
 */
declare global {
  namespace Express {
    interface Request {
      userId?: string;
    }
  }
}

/**
 * Authentication middleware placeholder.
 * Verifies the JWT from the Authorization header and attaches userId to the request.
 *
 * TODO: Implement actual JWT verification using jsonwebtoken or jose:
 *   const token = req.headers.authorization?.replace('Bearer ', '');
 *   const payload = jwt.verify(token, process.env.JWT_SECRET);
 *   req.userId = payload.sub;
 */
export function requireAuth(req: Request, res: Response, next: NextFunction): void {
  const authHeader = req.headers.authorization;

  if (!authHeader?.startsWith('Bearer ')) {
    res.status(401).json({ error: 'Authentication required' });
    return;
  }

  const token = authHeader.slice(7);

  if (!token) {
    res.status(401).json({ error: 'Invalid token' });
    return;
  }

  // TODO: Replace with real JWT verification.
  // For now, reject all requests to enforce that auth must be implemented before shipping.
  res.status(501).json({ error: 'JWT verification not yet implemented' });
}

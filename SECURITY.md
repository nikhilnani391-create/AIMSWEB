# Security Audit Report

## Audit Date: 2026-06-11

## Summary

A security audit was performed across all branches of the AIMSWEB repository. The codebase consists of:
- A static HTML website (Laxven Systems B2B site) on `website-redesign-strategy-*`
- A Node.js/TypeScript backend (Snapchat clone) on `init-snapchat-clone-*`

---

## Findings

### CRITICAL

| # | Category | Location | Description | Status |
|---|----------|----------|-------------|--------|
| 1 | Missing Authentication | `backend/src/services/SnapService.ts` | `handleSnapViewed()` accepted any caller without verifying identity — any user knowing a snap ID could mark it as viewed | **Fixed** |
| 2 | Missing Authorization | `backend/src/services/SnapService.ts` | No check that the requesting user is the snap's `receiver_id` | **Fixed** |
| 3 | Unvalidated Input | `backend/src/services/SnapService.ts` | `snapId` passed directly to `findById()` without ObjectId format validation — could trigger unexpected Mongoose cast errors or be leveraged for NoSQL injection patterns | **Fixed** |

### HIGH

| # | Category | Location | Description | Status |
|---|----------|----------|-------------|--------|
| 4 | Sensitive Data in Logs | `backend/src/services/SnapService.ts` | `media_url` (S3 paths/signed URLs) logged to stdout via `console.log` | **Fixed** |
| 5 | Insecure Dependency | `backend/package.json` | `@types/mongoose@^5.11.96` is deprecated and abandoned; Mongoose 6+ ships its own types | **Fixed** |
| 6 | No CORS Configuration | Backend | No CORS middleware — once Express is wired up, the server would accept requests from any origin | **Fixed** (middleware added) |
| 7 | No Rate Limiting | Backend | No protection against brute-force or abuse | **Fixed** (middleware added) |

### MEDIUM

| # | Category | Location | Description | Status |
|---|----------|----------|-------------|--------|
| 8 | No Security Headers | Backend | Missing `Strict-Transport-Security`, `X-Content-Type-Options`, etc. | **Fixed** (middleware added) |
| 9 | No `.env.example` | Backend | Developers may hardcode secrets without guidance on what env vars are expected | **Fixed** |
| 10 | CDN TailwindCSS in Production | `railway-website/index.html` | Uses `cdn.tailwindcss.com` (development-only CDN) — can be modified by CDN compromise and is not recommended for production | Noted |
| 11 | No CSP Header | `railway-website/index.html` | Missing Content-Security-Policy meta tag on the static site | Noted |

### LOW / INFORMATIONAL

| # | Category | Location | Description | Status |
|---|----------|----------|-------------|--------|
| 12 | Unreliable Deletion | `SnapService.ts` | `setTimeout` for snap deletion is lost on server restart — use a persistent job queue | Noted (comment added) |
| 13 | Form without Backend | `railway-website/index.html` | Contact form uses `type="button"` — doesn't submit; no CSRF token | Noted |
| 14 | No Hardcoded Secrets Found | All branches | `.env` is properly gitignored; no API keys, tokens, or passwords found in source | — |

---

## Recommendations for Next Steps

1. **Implement JWT verification** in `backend/src/middlewares/auth.ts` before any endpoint goes live.
2. **Add `helmet` and `express-rate-limit`** packages for production-grade security headers and rate limiting.
3. **Replace `setTimeout`** snap deletion with Redis TTL or BullMQ as noted in the plan.
4. **Add a Content-Security-Policy** meta tag to `railway-website/index.html` and switch from CDN TailwindCSS to a build-time Tailwind compilation.
5. **Run `npm audit`** regularly and integrate it into CI.

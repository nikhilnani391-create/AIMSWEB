# Snapchat Clone Development Plan & Architecture

## Overview
This document outlines the development plan, backend architecture, database schema, and project structure for a fully functional, production-ready "Snapchat Clone" mobile application. The app focuses on real-time, ephemeral media sharing and instant messaging.

## Technology Stack
*   **Frontend:** React Native (with TypeScript) for cross-platform compatibility and native performance.
*   **Backend:** Node.js with Express using TypeScript.
*   **Database:** MongoDB for user profiles/relations, Redis for caching and ephemeral message lifecycles.
*   **Real-time Communication:** WebSockets (Socket.io) for instant messaging and presence tracking.
*   **Cloud Storage:** AWS S3 (with CloudFront CDN) for temporary media hosting.

## Development Milestones

### Milestone 1: Backend Setup, Authentication & Database Setup
1.  **Project Initialization:** Setup Node.js/Express backend with TypeScript. Initialize React Native frontend.
2.  **Database Connection:** Connect to MongoDB and Redis.
3.  **User Models:** Define Mongoose schemas for User profiles.
4.  **Authentication:** Implement JWT-based authentication (simulated OTP or email/password). Include route protection middleware.
5.  **Friendship System:** Implement APIs for adding, accepting, blocking friends, and fetching contact lists.

### Milestone 2: Cloud Storage & Media Uploads
1.  **AWS S3 Setup:** Configure S3 buckets with appropriate lifecycle rules for automatic deletion after a set period.
2.  **Upload Service:** Create a backend service for generating pre-signed URLs or handling direct uploads. Implement media compression on the frontend before upload.
3.  **Frontend Camera Integration:** Integrate native device cameras in React Native. Build UI for capturing photos/videos (max 10s) and adding basic overlays (text/drawing).

### Milestone 3: Real-Time WebSockets & Chat
1.  **Socket.io Setup:** Integrate Socket.io on the backend and frontend. Handle connection/disconnection and presence tracking.
2.  **Real-Time Chat:** Implement text-based chat.
3.  **Chat Persistence:** Messages are cleared by default but can be saved via long-press (saving status updated in MongoDB).

### Milestone 4: Ephemeral Messaging (The "Snap" Mechanic)
1.  **Snap Schema:** Define the database schema for ephemeral media (Snaps).
2.  **Media Lifecycle:**
    *   Sender uploads media to S3, gets URL, and sends a Snap metadata message via WebSocket/API to the backend.
    *   Recipient receives notification.
    *   Recipient opens media -> Client starts countdown -> Notifies backend of "viewed" status.
    *   Backend marks Snap as viewed and schedules deletion (using Redis TTL or BullMQ worker).
3.  **Deletion Worker:** Implement a reliable background worker to permanently delete media from S3 and metadata from MongoDB after the timer expires.
4.  **Screenshot Detection:** Implement native listeners in React Native to detect screenshots and notify the sender via WebSocket.

### Milestone 5: Stories Feature
1.  **Story Schema:** Define schema for Stories.
2.  **Story Upload & Display:** Allow users to post to their "Story". Fetch and display friends' active stories.
3.  **24-Hour Deletion:** Implement a background process or rely on database TTL indexes to automatically delete stories after 24 hours.

## Frontend Architecture (React Native)

The frontend will follow a scalable module-based architecture:
*   **`/src/components`:** Reusable UI components (buttons, input fields, camera overlays).
*   **`/src/screens`:** Main application screens (Camera, Chat, Stories, Settings).
*   **`/src/navigation`:** React Navigation configurations (stacks, tabs, modals).
*   **`/src/services`:** API interactions (Axios/fetch clients, WebSocket managers).
*   **`/src/store`:** Global state management (Zustand or Redux slices).
*   **`/src/assets`:** Static resources (images, icons, fonts).
*   **`/src/utils`:** Helper functions, constants, formatting tools.

## Backend Architecture (Node.js)

The backend will follow a layered architecture:
*   **`/src/controllers`:** Handle HTTP request/response parsing and validation.
*   **`/src/services`:** Contain business logic (e.g., Auth, Snap handling, S3 interaction).
*   **`/src/models`:** Define Mongoose schemas and database interactions.
*   **`/src/sockets`:** Handle WebSocket events and real-time logic.
*   **`/src/middlewares`:** Handle authentication, error catching, and logging.
*   **`/src/workers`:** Background tasks (e.g., cleaning up expired media).

## Database Schema (MongoDB + Mongoose)

### User Schema
```typescript
{
  _id: ObjectId,
  username: { type: String, unique: true, required: true },
  phone_number: { type: String, unique: true },
  passwordHash: { type: String, required: true }, // If using email/pwd
  friends: [{ type: ObjectId, ref: 'User' }],
  blocked_users: [{ type: ObjectId, ref: 'User' }],
  created_at: Date,
  updated_at: Date
}
```

### Snap Schema (Ephemeral Media)
```typescript
{
  _id: ObjectId,
  sender_id: { type: ObjectId, ref: 'User', required: true },
  receiver_id: { type: ObjectId, ref: 'User', required: true },
  media_url: { type: String, required: true }, // S3 Object Key or CloudFront URL
  media_type: { type: String, enum: ['image', 'video'], required: true },
  duration: { type: Number, min: 1, max: 10, required: true }, // in seconds
  status: { type: String, enum: ['sent', 'delivered', 'viewed', 'deleted'], default: 'sent' },
  viewed_at: { type: Date, default: null }, // Timestamp when opened
  created_at: { type: Date, default: Date.now },
}
```

### Chat Message Schema
```typescript
{
  _id: ObjectId,
  sender_id: { type: ObjectId, ref: 'User' },
  receiver_id: { type: ObjectId, ref: 'User' },
  content: { type: String },
  is_saved_by_sender: { type: Boolean, default: false },
  is_saved_by_receiver: { type: Boolean, default: false },
  created_at: { type: Date, default: Date.now }
}
```

### Story Schema
```typescript
{
  _id: ObjectId,
  user_id: { type: ObjectId, ref: 'User', required: true },
  media_url: { type: String, required: true },
  media_type: { type: String, enum: ['image', 'video'] },
  duration: { type: Number, default: 5 },
  expires_at: { type: Date, required: true }, // Indexed for TTL deletion
  created_at: { type: Date, default: Date.now }
}
```

# Project Free Fire — Battle Royale Clone

A production-ready, cross-platform 50-player Battle Royale game inspired by Garena Free Fire.

## Architecture

```
                         [ Global Load Balancer ]
                                     |
         +---------------------------+---------------------------+
         |                           |                           |
[ Auth & Profile API ]     [ Matchmaking Service ]     [ Asset Delivery (CDN) ]
   (Node.js/Mongo)              (Go + Redis)                (Addressables)
         |                           |                           |
         +---------------------------+---------------------------+
                                     |
                       [ Dedicated Game Server ]
                        (Authoritative Unity/C#)
```

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Client Engine | Unity (C#) |
| Auth/Profile API | Node.js / TypeScript / Express |
| Matchmaking | Go + Redis |
| Server Orchestration | Node.js + Docker |
| Database | MongoDB (profiles, inventory) + Redis (matchmaking queue) |
| Dedicated Game Server | Authoritative C# (Mirror/FishNet networking) |
| Infrastructure | Docker, Kubernetes-ready |

## Project Structure

```
/project-root
├── /backend-services
│   ├── /gateway-auth       # TypeScript Express Auth API
│   ├── /matchmaker          # Go Matchmaking Service
│   └── /orchestrator        # Node.js Docker Controller
├── /game-client-server      # Shared Unity/C# codebase
│   ├── /Assets
│   │   ├── /Scripts
│   │   │   ├── /Networking  # Mirror network syncing
│   │   │   ├── /Player      # Controllers, Weapons, Inputs
│   │   │   ├── /Combat      # Hitscan, Lag Compensation
│   │   │   ├── /Systems     # Zone, Loot Spawner, Game Loop
│   │   │   ├── /Inventory   # Loot tables, pickups
│   │   │   └── /GlooWall    # Gloo Wall mechanic
│   │   └── /Data            # ScriptableObject definitions
│   └── /ProjectSettings
└── docker-compose.yml
```

## Quick Start

```bash
# Start infrastructure (MongoDB, Redis, all services)
docker-compose up --build

# Services will be available at:
# Auth API:        http://localhost:3000
# Matchmaker:      http://localhost:4000
# Orchestrator:    http://localhost:5000
```

## Networking

- **Protocol:** UDP via KCP/LiteNetLib for gameplay; HTTP/WebSocket for lobby & matchmaking
- **Architecture:** Client-Server Authoritative at 30Hz tick rate
- **Features:** Client-side prediction, server reconciliation, lag compensation (400ms history buffer)

## Game Features

- 50-player Battle Royale matches
- Plane flyover with skydiving/parachuting
- 4-stage dynamic safe zone contraction
- Hitscan combat with server-side lag compensation
- Procedural loot spawning with weighted rarity
- Gloo Wall defensive mechanic
- Anti-cheat movement validation
- Object pooling for mobile performance optimization

# 2D Car Game Development Plan

## 1. Frontend Architecture

The game uses a vanilla TypeScript approach with HTML5 Canvas for rendering. No heavy game frameworks (like Phaser) are used, aiming for a lightweight, fast, and modular structure.

### 1.1 Technology Stack
- **Language**: TypeScript (ES6+) for strict typing, better tooling, and clean OOP architecture.
- **Rendering**: HTML5 `<canvas>` using the 2D Context (`CanvasRenderingContext2D`).
- **Styling**: Tailwind CSS for all UI overlays (Start Menu, HUD, Game Over Screen) outside the canvas.
- **Tooling**: Vite for fast bundling and development server.

### 1.2 Folder Structure (`/src`)
- `game/`: Core game management.
  - `Game.ts`: The main Game Engine class. Handles the `requestAnimationFrame` loop, timing (`deltaTime`), canvas setup, and overall state machine (Start, Playing, GameOver). Orchestrates updates and drawing.
- `entities/`: Game objects.
  - `Car.ts`: Player character. Handles position, velocity, acceleration, friction, and steering angles using 2D vector math.
  - `Obstacle.ts` (Future): Enemy cars or barriers.
- `utils/`: Helpers and input.
  - `InputHandler.ts`: Tracks keyboard state (arrow keys/WASD) and provides a clean interface for the Car to poll input.
- `ui/`: (Optional, currently handled via `main.ts` tying DOM elements to game state).

### 1.3 State Management
The game's state (score, high score, active screen) is managed within the `Game` class. Changes in state trigger updates to the DOM (Tailwind CSS overlays) via callbacks or direct DOM manipulation in `main.ts`. LocalStorage is used to persist the high score.

## 2. Implementation Milestones

### Phase 1: Setup and Boilerplate (Current Phase)
- [x] Initialize Vite + TypeScript project.
- [x] Install and configure Tailwind CSS.
- [x] Create project directory structure.
- [x] Write `DEVELOPMENT_PLAN.md`.
- [ ] Implement `InputHandler` boilerplate.
- [ ] Implement `Car` physics boilerplate (vectors, acceleration).
- [ ] Implement `Game` loop boilerplate (`requestAnimationFrame`).
- [ ] Setup HTML UI overlays with Tailwind CSS and link logic in `main.ts`.

### Phase 2: Core Physics and Movement
- [ ] Connect `InputHandler` to the `Car` class.
- [ ] Refine car physics: implement friction, top speed caps, and realistic steering based on current velocity.
- [ ] Ensure smooth movement within the canvas boundaries (wall collision or infinite scrolling illusion).

### Phase 3: Obstacles and Environment
- [ ] Create an `Obstacle` class.
- [ ] Implement an object pool or spawner system in `Game.ts` to procedurally generate obstacles.
- [ ] Create an illusion of movement (e.g., scrolling road lines).

### Phase 4: Collision and Game Logic
- [ ] Implement AABB (Axis-Aligned Bounding Box) or SAT (Separating Axis Theorem) collision detection between the Car and Obstacles.
- [ ] Track score based on time survived or distance traveled.
- [ ] Implement Game Over state transition upon collision.

### Phase 5: UI and Polish
- [ ] Complete Start Menu and Game Over screen logic (restarting without page reload).
- [ ] Implement High Score saving/loading via `localStorage`.
- [ ] Finalize HUD (Score, Speedometer).
- [ ] Polish graphics (replace simple rectangles with sprites if desired) and fine-tune game feel.

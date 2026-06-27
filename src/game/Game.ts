import { Car } from '../entities/Car';
import { InputHandler } from '../utils/InputHandler';

export type GameState = 'START' | 'PLAYING' | 'GAMEOVER';

export class Game {
  private canvas: HTMLCanvasElement;
  private ctx: CanvasRenderingContext2D;
  private lastTime: number = 0;
  private animationId: number = 0;

  public state: GameState = 'START';
  public score: number = 0;

  private car: Car;
  private inputHandler: InputHandler;

  private updateUICallback: (state: GameState, score: number) => void;

  constructor(canvas: HTMLCanvasElement, updateUICallback: (state: GameState, score: number) => void) {
    this.canvas = canvas;
    const context = canvas.getContext('2d');
    if (!context) throw new Error("Could not get 2D context");
    this.ctx = context;

    this.updateUICallback = updateUICallback;

    this.car = new Car(this.canvas.width / 2, this.canvas.height / 2, 30, 50);
    this.inputHandler = new InputHandler();
  }

  public start() {
    this.state = 'PLAYING';
    this.score = 0;
    this.car = new Car(this.canvas.width / 2, this.canvas.height - 100, 30, 50);
    this.updateUICallback(this.state, this.score);
    this.lastTime = performance.now();
    this.loop(this.lastTime);
  }

  public stop() {
    this.state = 'GAMEOVER';
    this.updateUICallback(this.state, Math.floor(this.score));
    cancelAnimationFrame(this.animationId);
  }

  private update(deltaTime: number) {
    if (this.state !== 'PLAYING') return;

    this.car.update(this.inputHandler.keys, deltaTime);

    // Boilerplate score update
    this.score += deltaTime * 0.01;
    this.updateUICallback(this.state, Math.floor(this.score));
  }

  private draw() {
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

    // Draw track or background here

    this.car.draw(this.ctx);
  }

  private loop = (timestamp: number) => {
    const deltaTime = timestamp - this.lastTime;
    this.lastTime = timestamp;

    this.update(deltaTime);
    this.draw();

    if (this.state === 'PLAYING') {
      this.animationId = requestAnimationFrame(this.loop);
    }
  }
}

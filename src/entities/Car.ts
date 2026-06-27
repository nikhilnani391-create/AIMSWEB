export class Car {
  public x: number;
  public y: number;
  public width: number;
  public height: number;

  public velocity: number;
  public acceleration: number;
  public maxSpeed: number;
  public friction: number;

  public angle: number;
  public steeringSpeed: number;

  constructor(x: number, y: number, width: number, height: number) {
    this.x = x;
    this.y = y;
    this.width = width;
    this.height = height;

    this.velocity = 0;
    this.acceleration = 0.2;
    this.maxSpeed = 10;
    this.friction = 0.05;

    this.angle = 0;
    this.steeringSpeed = 0.04;
  }

  update(keys: { [key: string]: boolean }, _deltaTime: number) {
    // Boilerplate for handling inputs and applying physics

    // Forward / Backward
    if (keys['ArrowUp'] || keys['w']) {
      this.velocity += this.acceleration;
    }
    if (keys['ArrowDown'] || keys['s']) {
      this.velocity -= this.acceleration;
    }

    // Steering
    if (this.velocity !== 0) {
      const flip = this.velocity > 0 ? 1 : -1;
      if (keys['ArrowLeft'] || keys['a']) {
        this.angle -= this.steeringSpeed * flip;
      }
      if (keys['ArrowRight'] || keys['d']) {
        this.angle += this.steeringSpeed * flip;
      }
    }

    // Cap speed
    if (this.velocity > this.maxSpeed) {
      this.velocity = this.maxSpeed;
    }
    if (this.velocity < -this.maxSpeed / 2) {
      this.velocity = -this.maxSpeed / 2; // reverse slower
    }

    // Apply friction
    if (this.velocity > 0) {
      this.velocity -= this.friction;
    }
    if (this.velocity < 0) {
      this.velocity += this.friction;
    }

    // Snap to 0 if very slow to stop endless micro-sliding
    if (Math.abs(this.velocity) < this.friction) {
      this.velocity = 0;
    }

    // Move car based on angle and velocity
    this.x += Math.sin(this.angle) * this.velocity;
    this.y -= Math.cos(this.angle) * this.velocity;
  }

  draw(ctx: CanvasRenderingContext2D) {
    ctx.save();
    ctx.translate(this.x, this.y);
    ctx.rotate(this.angle);

    // Simple car body
    ctx.fillStyle = 'blue';
    ctx.fillRect(-this.width / 2, -this.height / 2, this.width, this.height);

    ctx.restore();
  }
}

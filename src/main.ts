import './style.css';
import { Game, type GameState } from './game/Game';

// Get DOM Elements
const canvas = document.getElementById('gameCanvas') as HTMLCanvasElement;
const startMenu = document.getElementById('startMenu') as HTMLDivElement;
const hud = document.getElementById('hud') as HTMLDivElement;
const gameOverScreen = document.getElementById('gameOverScreen') as HTMLDivElement;
const startBtn = document.getElementById('startBtn') as HTMLButtonElement;
const restartBtn = document.getElementById('restartBtn') as HTMLButtonElement;
const scoreDisplay = document.getElementById('scoreDisplay') as HTMLSpanElement;
const highScoreDisplay = document.getElementById('highScoreDisplay') as HTMLSpanElement;
const finalScoreDisplay = document.getElementById('finalScoreDisplay') as HTMLSpanElement;
const newHighScoreMsg = document.getElementById('newHighScoreMsg') as HTMLParagraphElement;

// Resize canvas to fit window
function resizeCanvas() {
  canvas.width = window.innerWidth;
  canvas.height = window.innerHeight;
}
window.addEventListener('resize', resizeCanvas);
resizeCanvas(); // Initial sizing

// High Score Management
let highScore = parseInt(localStorage.getItem('carGameHighScore') || '0', 10);
highScoreDisplay.innerText = highScore.toString();

// UI Update Callback bound to Game engine
function updateUI(state: GameState, score: number) {
  // Update Score display continuously
  scoreDisplay.innerText = score.toString();

  // Handle State changes
  if (state === 'START') {
    startMenu.classList.remove('hidden');
    hud.classList.add('hidden');
    gameOverScreen.classList.add('hidden');
  }
  else if (state === 'PLAYING') {
    startMenu.classList.add('hidden');
    hud.classList.remove('hidden');
    gameOverScreen.classList.add('hidden');
  }
  else if (state === 'GAMEOVER') {
    startMenu.classList.add('hidden');
    hud.classList.add('hidden'); // Hide HUD or keep it? usually hide.
    gameOverScreen.classList.remove('hidden');

    finalScoreDisplay.innerText = score.toString();

    // Check high score
    if (score > highScore) {
      highScore = score;
      localStorage.setItem('carGameHighScore', highScore.toString());
      highScoreDisplay.innerText = highScore.toString();
      newHighScoreMsg.classList.remove('hidden');
    } else {
      newHighScoreMsg.classList.add('hidden');
    }
  }
}

// Initialize Game Engine
const game = new Game(canvas, updateUI);

// Event Listeners for UI Buttons
startBtn.addEventListener('click', () => {
  game.start();
});

restartBtn.addEventListener('click', () => {
  game.start();
});

// Expose game instance to window for debugging (optional)
(window as any).game = game;

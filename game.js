// ============================================================
// 3D Space Shooter - HTML5 Canvas Game
// ============================================================

(function () {
    "use strict";

    // --- Canvas & Context ---
    const canvas = document.getElementById("gameCanvas");
    const ctx = canvas.getContext("2d");

    // --- DOM Elements ---
    const scoreDisplay = document.getElementById("score-display");
    const livesDisplay = document.getElementById("lives-display");
    const powerUpDisplay = document.getElementById("power-up-display");
    const gameOverScreen = document.getElementById("game-over");
    const startScreen = document.getElementById("start-screen");
    const levelDisplay = document.getElementById("level-display");

    // --- Constants ---
    const W = canvas.width;
    const H = canvas.height;
    const HORIZON_Y = H * 0.35;
    const VP = { x: W / 2, y: HORIZON_Y }; // vanishing point

    const PLAYER_SPEED = 5;
    const PLAYER_BOOST_SPEED = 8;
    const BULLET_SPEED = 12;
    const ENEMY_BASE_SPEED = 1.5;
    const STAR_COUNT = 200;
    const MAX_LIVES = 5;
    const FIRE_COOLDOWN = 150; // ms
    const RAPID_FIRE_COOLDOWN = 60; // ms
    const SHIELD_DURATION = 8000; // ms
    const RAPID_FIRE_DURATION = 7000; // ms
    const SPREAD_DURATION = 7000; // ms
    const POWERUP_SPAWN_INTERVAL = 6000; // ms

    // --- Game State ---
    let gameState = "start"; // start, playing, gameover
    let score = 0;
    let lives = 3;
    let level = 1;
    let enemiesDefeated = 0;
    let enemiesPerLevel = 10;
    let lastFireTime = 0;
    let lastPowerUpSpawn = 0;
    let shieldActive = false;
    let shieldTimer = 0;
    let rapidFireActive = false;
    let rapidFireTimer = 0;
    let spreadShotActive = false;
    let spreadShotTimer = 0;
    let comboCount = 0;
    let comboTimer = 0;
    let screenShake = 0;
    let levelTransition = false;
    let levelTransitionTimer = 0;

    // --- Input ---
    const keys = {};
    document.addEventListener("keydown", function (e) {
        keys[e.code] = true;
        if (e.code === "Enter") {
            if (gameState === "start") {
                startGame();
            } else if (gameState === "gameover") {
                resetGame();
            }
        }
        if (["Space", "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"].indexOf(e.code) !== -1) {
            e.preventDefault();
        }
    });
    document.addEventListener("keyup", function (e) {
        keys[e.code] = false;
    });

    // --- Player ---
    const player = {
        x: W / 2,
        y: H - 80,
        width: 40,
        height: 50,
        vx: 0,
        vy: 0,
        tilt: 0
    };

    // --- Collections ---
    let bullets = [];
    let enemies = [];
    let particles = [];
    let stars = [];
    let powerUps = [];
    let explosions = [];
    let floatingTexts = [];

    // --- Star Field ---
    function initStars() {
        stars = [];
        for (let i = 0; i < STAR_COUNT; i++) {
            stars.push({
                x: Math.random() * W,
                y: Math.random() * H,
                z: Math.random() * 3 + 0.5,
                brightness: Math.random() * 0.8 + 0.2
            });
        }
    }

    // --- 3D Projection Helpers ---
    function project3D(x, y, z) {
        // z: 0 = at camera, 1 = at horizon
        var scale = 1 - z * 0.85;
        var px = VP.x + (x - VP.x) * scale;
        var py = VP.y + (y - VP.y) * scale;
        return { x: px, y: py, scale: scale };
    }

    function getDepthScale(y) {
        // Objects closer to horizon are smaller (farther away)
        var t = Math.max(0, Math.min(1, (y - HORIZON_Y) / (H - HORIZON_Y)));
        return 0.3 + t * 0.7;
    }

    // --- Drawing Helpers ---
    function drawGlowLine(x1, y1, x2, y2, color, width) {
        ctx.save();
        ctx.shadowColor = color;
        ctx.shadowBlur = 10;
        ctx.strokeStyle = color;
        ctx.lineWidth = width || 2;
        ctx.beginPath();
        ctx.moveTo(x1, y1);
        ctx.lineTo(x2, y2);
        ctx.stroke();
        ctx.restore();
    }

    function drawGlowRect(x, y, w, h, color, alpha) {
        ctx.save();
        ctx.globalAlpha = alpha || 1;
        ctx.shadowColor = color;
        ctx.shadowBlur = 15;
        ctx.fillStyle = color;
        ctx.fillRect(x, y, w, h);
        ctx.restore();
    }

    // --- Star Field Rendering ---
    function updateAndDrawStars(dt) {
        for (var i = 0; i < stars.length; i++) {
            var s = stars[i];
            s.y += s.z * 2 * (dt / 16);
            if (s.y > H) {
                s.y = 0;
                s.x = Math.random() * W;
                s.z = Math.random() * 3 + 0.5;
            }
            var alpha = s.brightness * (s.z / 3.5);
            var size = s.z * 0.8;
            ctx.fillStyle = "rgba(255,255,255," + alpha + ")";
            ctx.fillRect(s.x, s.y, size, size);
        }
    }

    // --- 3D Grid Floor ---
    function drawGrid() {
        ctx.save();
        ctx.globalAlpha = 0.15;
        // Horizontal lines
        for (var i = 0; i <= 20; i++) {
            var t = i / 20;
            var y = HORIZON_Y + t * (H - HORIZON_Y);
            var spread = t * W * 0.8;
            drawGlowLine(W / 2 - spread, y, W / 2 + spread, y, "#0ff", 1);
        }
        // Vertical converging lines
        for (var j = -8; j <= 8; j++) {
            var bx = W / 2 + j * 60;
            drawGlowLine(VP.x, HORIZON_Y, bx, H, "#0ff", 1);
        }
        ctx.restore();
    }

    // --- Player Drawing ---
    function drawPlayer() {
        var p = player;
        var scale = getDepthScale(p.y);

        ctx.save();
        ctx.translate(p.x, p.y);
        ctx.rotate(p.tilt * 0.05);

        // Ship body (3D-ish look)
        var sw = p.width * scale;
        var sh = p.height * scale;

        // Engine glow
        ctx.shadowColor = "#f80";
        ctx.shadowBlur = 20;
        ctx.fillStyle = "#f80";
        ctx.beginPath();
        ctx.ellipse(0, sh * 0.4, sw * 0.15, sh * 0.2 + Math.random() * 5, 0, 0, Math.PI * 2);
        ctx.fill();

        // Left engine
        ctx.beginPath();
        ctx.ellipse(-sw * 0.25, sh * 0.35, sw * 0.08, sh * 0.12 + Math.random() * 3, 0, 0, Math.PI * 2);
        ctx.fill();

        // Right engine
        ctx.beginPath();
        ctx.ellipse(sw * 0.25, sh * 0.35, sw * 0.08, sh * 0.12 + Math.random() * 3, 0, 0, Math.PI * 2);
        ctx.fill();

        ctx.shadowBlur = 0;

        // Main hull
        ctx.fillStyle = "#1a3a5c";
        ctx.beginPath();
        ctx.moveTo(0, -sh * 0.5);
        ctx.lineTo(-sw * 0.4, sh * 0.3);
        ctx.lineTo(-sw * 0.15, sh * 0.4);
        ctx.lineTo(sw * 0.15, sh * 0.4);
        ctx.lineTo(sw * 0.4, sh * 0.3);
        ctx.closePath();
        ctx.fill();

        // Cockpit highlight
        ctx.fillStyle = "#0ff";
        ctx.shadowColor = "#0ff";
        ctx.shadowBlur = 10;
        ctx.beginPath();
        ctx.moveTo(0, -sh * 0.35);
        ctx.lineTo(-sw * 0.1, -sh * 0.05);
        ctx.lineTo(sw * 0.1, -sh * 0.05);
        ctx.closePath();
        ctx.fill();

        // Wings
        ctx.fillStyle = "#2a5a8c";
        ctx.shadowBlur = 0;
        // Left wing
        ctx.beginPath();
        ctx.moveTo(-sw * 0.15, sh * 0.1);
        ctx.lineTo(-sw * 0.5, sh * 0.35);
        ctx.lineTo(-sw * 0.35, sh * 0.15);
        ctx.closePath();
        ctx.fill();
        // Right wing
        ctx.beginPath();
        ctx.moveTo(sw * 0.15, sh * 0.1);
        ctx.lineTo(sw * 0.5, sh * 0.35);
        ctx.lineTo(sw * 0.35, sh * 0.15);
        ctx.closePath();
        ctx.fill();

        // Wing tips glow
        ctx.fillStyle = "#0ff";
        ctx.shadowColor = "#0ff";
        ctx.shadowBlur = 8;
        ctx.beginPath();
        ctx.arc(-sw * 0.48, sh * 0.32, 2, 0, Math.PI * 2);
        ctx.fill();
        ctx.beginPath();
        ctx.arc(sw * 0.48, sh * 0.32, 2, 0, Math.PI * 2);
        ctx.fill();

        // Shield effect
        if (shieldActive) {
            ctx.strokeStyle = "rgba(0,255,255,0.5)";
            ctx.shadowColor = "#0ff";
            ctx.shadowBlur = 20;
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.ellipse(0, 0, sw * 0.7, sh * 0.6, 0, 0, Math.PI * 2);
            ctx.stroke();
            ctx.globalAlpha = 0.1;
            ctx.fillStyle = "#0ff";
            ctx.fill();
        }

        ctx.restore();
    }

    // --- Bullet Drawing ---
    function drawBullet(b) {
        var scale = getDepthScale(b.y);
        var size = 4 * scale;

        ctx.save();
        ctx.shadowColor = "#0ff";
        ctx.shadowBlur = 15;

        // Bullet trail
        var grad = ctx.createLinearGradient(b.x, b.y, b.x, b.y + 20 * scale);
        grad.addColorStop(0, "rgba(0,255,255,1)");
        grad.addColorStop(1, "rgba(0,255,255,0)");
        ctx.fillStyle = grad;
        ctx.fillRect(b.x - size / 2, b.y, size, 20 * scale);

        // Bullet head
        ctx.fillStyle = "#fff";
        ctx.beginPath();
        ctx.arc(b.x, b.y, size, 0, Math.PI * 2);
        ctx.fill();

        ctx.restore();
    }

    // --- Enemy Types ---
    function createEnemy() {
        var type = Math.random();
        var e;
        if (type < 0.5) {
            // Basic fighter
            e = {
                type: "fighter",
                x: Math.random() * (W - 100) + 50,
                y: HORIZON_Y + 20,
                z: 0.9,
                width: 35,
                height: 35,
                hp: 1 + Math.floor(level / 3),
                maxHp: 1 + Math.floor(level / 3),
                speed: ENEMY_BASE_SPEED + level * 0.15,
                points: 100,
                color: "#f44",
                wobble: Math.random() * Math.PI * 2,
                wobbleSpeed: 0.02 + Math.random() * 0.02
            };
        } else if (type < 0.8) {
            // Fast scout
            e = {
                type: "scout",
                x: Math.random() * (W - 100) + 50,
                y: HORIZON_Y + 20,
                z: 0.9,
                width: 25,
                height: 25,
                hp: 1,
                maxHp: 1,
                speed: ENEMY_BASE_SPEED * 1.8 + level * 0.2,
                points: 150,
                color: "#ff0",
                wobble: Math.random() * Math.PI * 2,
                wobbleSpeed: 0.05 + Math.random() * 0.03,
                zigzag: true
            };
        } else {
            // Heavy cruiser
            e = {
                type: "cruiser",
                x: Math.random() * (W - 150) + 75,
                y: HORIZON_Y + 20,
                z: 0.9,
                width: 55,
                height: 50,
                hp: 3 + Math.floor(level / 2),
                maxHp: 3 + Math.floor(level / 2),
                speed: ENEMY_BASE_SPEED * 0.6 + level * 0.1,
                points: 300,
                color: "#f0f",
                wobble: Math.random() * Math.PI * 2,
                wobbleSpeed: 0.01
            };
        }
        return e;
    }

    function drawEnemy(e) {
        var t = (e.y - HORIZON_Y) / (H - HORIZON_Y);
        var scale = 0.3 + t * 0.7;
        var sw = e.width * scale;
        var sh = e.height * scale;

        ctx.save();
        ctx.translate(e.x, e.y);

        if (e.type === "fighter") {
            // Fighter ship
            ctx.fillStyle = e.color;
            ctx.shadowColor = e.color;
            ctx.shadowBlur = 10;
            ctx.beginPath();
            ctx.moveTo(0, sh * 0.5);
            ctx.lineTo(-sw * 0.4, -sh * 0.3);
            ctx.lineTo(-sw * 0.15, -sh * 0.4);
            ctx.lineTo(sw * 0.15, -sh * 0.4);
            ctx.lineTo(sw * 0.4, -sh * 0.3);
            ctx.closePath();
            ctx.fill();

            ctx.fillStyle = "#800";
            ctx.beginPath();
            ctx.moveTo(0, sh * 0.2);
            ctx.lineTo(-sw * 0.1, -sh * 0.1);
            ctx.lineTo(sw * 0.1, -sh * 0.1);
            ctx.closePath();
            ctx.fill();
        } else if (e.type === "scout") {
            // Scout - small and triangular
            ctx.fillStyle = e.color;
            ctx.shadowColor = e.color;
            ctx.shadowBlur = 8;
            ctx.beginPath();
            ctx.moveTo(0, sh * 0.5);
            ctx.lineTo(-sw * 0.5, -sh * 0.4);
            ctx.lineTo(sw * 0.5, -sh * 0.4);
            ctx.closePath();
            ctx.fill();
        } else if (e.type === "cruiser") {
            // Cruiser - wide and armored
            ctx.fillStyle = e.color;
            ctx.shadowColor = e.color;
            ctx.shadowBlur = 12;
            ctx.beginPath();
            ctx.moveTo(0, sh * 0.5);
            ctx.lineTo(-sw * 0.5, sh * 0.1);
            ctx.lineTo(-sw * 0.45, -sh * 0.3);
            ctx.lineTo(-sw * 0.1, -sh * 0.5);
            ctx.lineTo(sw * 0.1, -sh * 0.5);
            ctx.lineTo(sw * 0.45, -sh * 0.3);
            ctx.lineTo(sw * 0.5, sh * 0.1);
            ctx.closePath();
            ctx.fill();

            // Cruiser detail
            ctx.fillStyle = "#606";
            ctx.fillRect(-sw * 0.2, -sh * 0.2, sw * 0.4, sh * 0.3);
        }

        // HP bar for multi-hit enemies
        if (e.maxHp > 1) {
            var barW = sw * 0.8;
            var barH = 4;
            ctx.fillStyle = "#333";
            ctx.fillRect(-barW / 2, -sh * 0.55 - 6, barW, barH);
            ctx.fillStyle = "#0f0";
            ctx.fillRect(-barW / 2, -sh * 0.55 - 6, barW * (e.hp / e.maxHp), barH);
        }

        ctx.restore();
    }

    // --- Power-Up Types ---
    function createPowerUp() {
        var types = ["shield", "rapidfire", "spread", "life", "bomb"];
        var type = types[Math.floor(Math.random() * types.length)];
        return {
            type: type,
            x: Math.random() * (W - 100) + 50,
            y: HORIZON_Y + 30,
            z: 0.85,
            size: 20,
            speed: 1.2,
            bobPhase: Math.random() * Math.PI * 2,
            collected: false
        };
    }

    function drawPowerUp(pu) {
        var t = (pu.y - HORIZON_Y) / (H - HORIZON_Y);
        var scale = 0.4 + t * 0.6;
        var s = pu.size * scale;
        var bob = Math.sin(pu.bobPhase) * 3;

        ctx.save();
        ctx.translate(pu.x, pu.y + bob);

        // Outer glow ring
        var color;
        var symbol;
        switch (pu.type) {
            case "shield":
                color = "#0ff";
                symbol = "S";
                break;
            case "rapidfire":
                color = "#f80";
                symbol = "R";
                break;
            case "spread":
                color = "#f0f";
                symbol = "W";
                break;
            case "life":
                color = "#0f0";
                symbol = "+";
                break;
            case "bomb":
                color = "#ff0";
                symbol = "B";
                break;
            default:
                color = "#fff";
                symbol = "?";
        }

        // Rotating diamond shape
        ctx.rotate(pu.bobPhase * 2);
        ctx.shadowColor = color;
        ctx.shadowBlur = 15;
        ctx.strokeStyle = color;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(0, -s);
        ctx.lineTo(s, 0);
        ctx.lineTo(0, s);
        ctx.lineTo(-s, 0);
        ctx.closePath();
        ctx.stroke();
        ctx.fillStyle = color;
        ctx.globalAlpha = 0.3;
        ctx.fill();
        ctx.globalAlpha = 1;

        // Symbol
        ctx.rotate(-pu.bobPhase * 2); // counter-rotate for readable text
        ctx.fillStyle = "#fff";
        ctx.font = Math.floor(s) + "px Courier New";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText(symbol, 0, 0);

        ctx.restore();
    }

    // --- Particles & Effects ---
    function spawnExplosion(x, y, color, count) {
        for (var i = 0; i < (count || 15); i++) {
            var angle = Math.random() * Math.PI * 2;
            var speed = Math.random() * 4 + 1;
            particles.push({
                x: x,
                y: y,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                life: 1,
                decay: 0.01 + Math.random() * 0.03,
                size: Math.random() * 4 + 1,
                color: color || "#ff0"
            });
        }
    }

    function spawnFloatingText(x, y, text, color) {
        floatingTexts.push({
            x: x,
            y: y,
            text: text,
            color: color || "#ff0",
            life: 1,
            vy: -2
        });
    }

    function drawParticles() {
        for (var i = particles.length - 1; i >= 0; i--) {
            var p = particles[i];
            ctx.save();
            ctx.globalAlpha = p.life;
            ctx.fillStyle = p.color;
            ctx.shadowColor = p.color;
            ctx.shadowBlur = 5;
            ctx.fillRect(p.x - p.size / 2, p.y - p.size / 2, p.size, p.size);
            ctx.restore();
        }
    }

    function drawFloatingTexts() {
        for (var i = floatingTexts.length - 1; i >= 0; i--) {
            var ft = floatingTexts[i];
            ctx.save();
            ctx.globalAlpha = ft.life;
            ctx.fillStyle = ft.color;
            ctx.shadowColor = ft.color;
            ctx.shadowBlur = 10;
            ctx.font = "bold 18px Courier New";
            ctx.textAlign = "center";
            ctx.fillText(ft.text, ft.x, ft.y);
            ctx.restore();
        }
    }

    // --- Update Logic ---
    function updatePlayer(dt) {
        var speed = keys["ShiftLeft"] || keys["ShiftRight"] ? PLAYER_BOOST_SPEED : PLAYER_SPEED;
        var targetVx = 0;
        var targetVy = 0;

        if (keys["ArrowLeft"] || keys["KeyA"]) targetVx = -speed;
        if (keys["ArrowRight"] || keys["KeyD"]) targetVx = speed;
        if (keys["ArrowUp"] || keys["KeyW"]) targetVy = -speed;
        if (keys["ArrowDown"] || keys["KeyS"]) targetVy = speed;

        // Smooth interpolation
        player.vx += (targetVx - player.vx) * 0.15;
        player.vy += (targetVy - player.vy) * 0.15;
        player.tilt += (targetVx * 0.5 - player.tilt) * 0.1;

        player.x += player.vx * (dt / 16);
        player.y += player.vy * (dt / 16);

        // Clamp to bounds
        player.x = Math.max(30, Math.min(W - 30, player.x));
        player.y = Math.max(HORIZON_Y + 80, Math.min(H - 30, player.y));
    }

    function updateBullets(dt) {
        for (var i = bullets.length - 1; i >= 0; i--) {
            var b = bullets[i];
            b.x += (b.vx || 0) * (dt / 16);
            b.y -= BULLET_SPEED * (dt / 16);
            // Scale toward vanishing point as bullets go up
            if (b.y < HORIZON_Y - 20) {
                bullets.splice(i, 1);
            }
        }
    }

    function updateEnemies(dt) {
        // Spawn enemies
        if (enemies.length < 3 + level && !levelTransition) {
            if (Math.random() < 0.02 + level * 0.005) {
                enemies.push(createEnemy());
            }
        }

        for (var i = enemies.length - 1; i >= 0; i--) {
            var e = enemies[i];
            e.wobble += e.wobbleSpeed * (dt / 16);

            // Move toward player (3D perspective: increase y to come closer)
            e.y += e.speed * (dt / 16);
            e.z = Math.max(0, 1 - (e.y - HORIZON_Y) / (H - HORIZON_Y));

            // Wobble / zigzag movement
            if (e.zigzag) {
                e.x += Math.sin(e.wobble) * 3 * (dt / 16);
            } else {
                e.x += Math.sin(e.wobble) * 1.5 * (dt / 16);
            }

            e.x = Math.max(30, Math.min(W - 30, e.x));

            // Off screen
            if (e.y > H + 50) {
                enemies.splice(i, 1);
                comboCount = 0;
            }
        }
    }

    function updatePowerUps(dt) {
        var now = Date.now();
        if (now - lastPowerUpSpawn > POWERUP_SPAWN_INTERVAL && !levelTransition) {
            lastPowerUpSpawn = now;
            if (powerUps.length < 3) {
                powerUps.push(createPowerUp());
            }
        }

        for (var i = powerUps.length - 1; i >= 0; i--) {
            var pu = powerUps[i];
            pu.y += pu.speed * (dt / 16);
            pu.bobPhase += 0.05 * (dt / 16);

            if (pu.y > H + 30) {
                powerUps.splice(i, 1);
            }
        }

        // Timers
        if (shieldActive && Date.now() > shieldTimer) {
            shieldActive = false;
        }
        if (rapidFireActive && Date.now() > rapidFireTimer) {
            rapidFireActive = false;
        }
        if (spreadShotActive && Date.now() > spreadShotTimer) {
            spreadShotActive = false;
        }
    }

    function updateParticles(dt) {
        for (var i = particles.length - 1; i >= 0; i--) {
            var p = particles[i];
            p.x += p.vx * (dt / 16);
            p.y += p.vy * (dt / 16);
            p.life -= p.decay * (dt / 16);
            if (p.life <= 0) {
                particles.splice(i, 1);
            }
        }

        for (var j = floatingTexts.length - 1; j >= 0; j--) {
            var ft = floatingTexts[j];
            ft.y += ft.vy * (dt / 16);
            ft.life -= 0.015 * (dt / 16);
            if (ft.life <= 0) {
                floatingTexts.splice(j, 1);
            }
        }
    }

    function fireBullets() {
        var now = Date.now();
        var cooldown = rapidFireActive ? RAPID_FIRE_COOLDOWN : FIRE_COOLDOWN;
        if (now - lastFireTime < cooldown) return;
        lastFireTime = now;

        bullets.push({ x: player.x, y: player.y - 20, vx: 0 });

        if (spreadShotActive) {
            bullets.push({ x: player.x - 10, y: player.y - 15, vx: -2 });
            bullets.push({ x: player.x + 10, y: player.y - 15, vx: 2 });
        }
    }

    // --- Collision Detection ---
    function checkCollisions() {
        // Bullets vs Enemies
        for (var i = bullets.length - 1; i >= 0; i--) {
            var b = bullets[i];
            for (var j = enemies.length - 1; j >= 0; j--) {
                var e = enemies[j];
                var es = getDepthScale(e.y);
                var ew = e.width * es * 0.5;
                var eh = e.height * es * 0.5;

                if (b.x > e.x - ew && b.x < e.x + ew &&
                    b.y > e.y - eh && b.y < e.y + eh) {
                    bullets.splice(i, 1);
                    e.hp--;

                    if (e.hp <= 0) {
                        // Enemy destroyed
                        comboCount++;
                        comboTimer = Date.now() + 2000;
                        var bonus = comboCount > 1 ? Math.floor(e.points * comboCount * 0.5) : e.points;
                        score += bonus;
                        enemiesDefeated++;

                        var text = "+" + bonus;
                        if (comboCount > 1) text += " x" + comboCount;
                        spawnFloatingText(e.x, e.y, text, e.color);

                        spawnExplosion(e.x, e.y, e.color, 20);
                        screenShake = 5;
                        enemies.splice(j, 1);

                        // Check level progression
                        if (enemiesDefeated >= enemiesPerLevel) {
                            startLevelTransition();
                        }
                    } else {
                        spawnExplosion(b.x, b.y, "#fff", 5);
                        screenShake = 2;
                    }
                    break;
                }
            }
        }

        // Player vs Enemies
        for (var k = enemies.length - 1; k >= 0; k--) {
            var en = enemies[k];
            var enScale = getDepthScale(en.y);
            var enw = en.width * enScale * 0.4;
            var enh = en.height * enScale * 0.4;
            var ps = getDepthScale(player.y);
            var pw = player.width * ps * 0.4;
            var ph = player.height * ps * 0.4;

            if (Math.abs(player.x - en.x) < (pw + enw) &&
                Math.abs(player.y - en.y) < (ph + enh)) {
                spawnExplosion(en.x, en.y, en.color, 25);
                enemies.splice(k, 1);

                if (!shieldActive) {
                    lives--;
                    screenShake = 15;
                    spawnExplosion(player.x, player.y, "#0ff", 15);
                    if (lives <= 0) {
                        gameOver();
                        return;
                    }
                } else {
                    spawnFloatingText(player.x, player.y - 30, "BLOCKED!", "#0ff");
                    screenShake = 5;
                }
            }
        }

        // Player vs Power-Ups
        for (var m = powerUps.length - 1; m >= 0; m--) {
            var pu = powerUps[m];
            var puScale = getDepthScale(pu.y);
            var pus = pu.size * puScale;

            if (Math.abs(player.x - pu.x) < pus + 20 &&
                Math.abs(player.y - pu.y) < pus + 20) {
                collectPowerUp(pu);
                powerUps.splice(m, 1);
            }
        }

        // Combo timer
        if (comboCount > 0 && Date.now() > comboTimer) {
            comboCount = 0;
        }
    }

    function collectPowerUp(pu) {
        spawnExplosion(pu.x, pu.y, "#fff", 10);

        switch (pu.type) {
            case "shield":
                shieldActive = true;
                shieldTimer = Date.now() + SHIELD_DURATION;
                spawnFloatingText(pu.x, pu.y, "SHIELD!", "#0ff");
                break;
            case "rapidfire":
                rapidFireActive = true;
                rapidFireTimer = Date.now() + RAPID_FIRE_DURATION;
                spawnFloatingText(pu.x, pu.y, "RAPID FIRE!", "#f80");
                break;
            case "spread":
                spreadShotActive = true;
                spreadShotTimer = Date.now() + SPREAD_DURATION;
                spawnFloatingText(pu.x, pu.y, "SPREAD!", "#f0f");
                break;
            case "life":
                lives = Math.min(MAX_LIVES, lives + 1);
                spawnFloatingText(pu.x, pu.y, "+1 LIFE!", "#0f0");
                break;
            case "bomb":
                // Destroy all enemies on screen
                for (var i = enemies.length - 1; i >= 0; i--) {
                    score += enemies[i].points;
                    spawnExplosion(enemies[i].x, enemies[i].y, enemies[i].color, 15);
                }
                enemies = [];
                screenShake = 20;
                spawnFloatingText(pu.x, pu.y, "BOOM!", "#ff0");
                break;
        }
    }

    // --- Level System ---
    function startLevelTransition() {
        levelTransition = true;
        levelTransitionTimer = Date.now() + 2500;
        level++;
        enemiesDefeated = 0;
        enemiesPerLevel = 10 + level * 3;

        levelDisplay.textContent = "LEVEL " + level;
        levelDisplay.style.display = "block";
        setTimeout(function () {
            levelDisplay.style.display = "none";
            levelTransition = false;
        }, 2500);
    }

    // --- UI Updates ---
    function updateUI() {
        scoreDisplay.textContent = "SCORE: " + score;
        if (comboCount > 1) {
            scoreDisplay.textContent += " | COMBO x" + comboCount;
        }

        // Lives as ship icons
        var livesStr = "";
        for (var i = 0; i < lives; i++) {
            livesStr += "\u25C6 "; // diamond symbol as ship icon
        }
        livesDisplay.textContent = livesStr;

        // Active power-ups
        var puTexts = [];
        if (shieldActive) {
            var sr = Math.max(0, Math.ceil((shieldTimer - Date.now()) / 1000));
            puTexts.push("SHIELD: " + sr + "s");
        }
        if (rapidFireActive) {
            var rr = Math.max(0, Math.ceil((rapidFireTimer - Date.now()) / 1000));
            puTexts.push("RAPID: " + rr + "s");
        }
        if (spreadShotActive) {
            var spr = Math.max(0, Math.ceil((spreadShotTimer - Date.now()) / 1000));
            puTexts.push("SPREAD: " + spr + "s");
        }
        powerUpDisplay.textContent = puTexts.join(" | ");
    }

    // --- Game State Management ---
    function startGame() {
        gameState = "playing";
        startScreen.style.display = "none";
        gameOverScreen.style.display = "none";
        initStars();
        lastPowerUpSpawn = Date.now();
    }

    function resetGame() {
        score = 0;
        lives = 3;
        level = 1;
        enemiesDefeated = 0;
        enemiesPerLevel = 10;
        comboCount = 0;
        shieldActive = false;
        rapidFireActive = false;
        spreadShotActive = false;
        bullets = [];
        enemies = [];
        particles = [];
        powerUps = [];
        floatingTexts = [];
        player.x = W / 2;
        player.y = H - 80;
        player.vx = 0;
        player.vy = 0;
        player.tilt = 0;
        screenShake = 0;
        levelTransition = false;
        gameOverScreen.style.display = "none";
        startGame();
    }

    function gameOver() {
        gameState = "gameover";
        gameOverScreen.style.display = "block";
        spawnExplosion(player.x, player.y, "#f00", 40);
        spawnExplosion(player.x, player.y, "#ff0", 30);
    }

    // --- Main Game Loop ---
    var lastTime = 0;

    function gameLoop(timestamp) {
        var dt = timestamp - lastTime;
        if (dt > 100) dt = 16; // cap for tab-switch
        lastTime = timestamp;

        // Clear
        ctx.fillStyle = "#000";
        ctx.fillRect(0, 0, W, H);

        // Screen shake
        if (screenShake > 0) {
            ctx.save();
            ctx.translate(
                (Math.random() - 0.5) * screenShake * 2,
                (Math.random() - 0.5) * screenShake * 2
            );
            screenShake *= 0.9;
            if (screenShake < 0.5) screenShake = 0;
        }

        // Always draw background
        updateAndDrawStars(dt);
        drawGrid();

        if (gameState === "playing") {
            // Input
            if (keys["Space"]) {
                fireBullets();
            }

            // Update
            updatePlayer(dt);
            updateBullets(dt);
            updateEnemies(dt);
            updatePowerUps(dt);
            checkCollisions();
            updateParticles(dt);
            updateUI();

            // Draw
            // Power-ups
            for (var p = 0; p < powerUps.length; p++) {
                drawPowerUp(powerUps[p]);
            }
            // Enemies (draw from top to bottom for depth ordering)
            enemies.sort(function (a, b) { return a.y - b.y; });
            for (var e = 0; e < enemies.length; e++) {
                drawEnemy(enemies[e]);
            }
            // Bullets
            for (var b = 0; b < bullets.length; b++) {
                drawBullet(bullets[b]);
            }
            // Player
            drawPlayer();
            // Particles on top
            drawParticles();
            drawFloatingTexts();
        } else if (gameState === "gameover") {
            // Still draw particles for the explosion
            updateParticles(dt);
            drawParticles();
            drawFloatingTexts();
            // Draw remaining enemies fading
            for (var re = 0; re < enemies.length; re++) {
                drawEnemy(enemies[re]);
            }
        }

        if (screenShake > 0) {
            ctx.restore();
        }

        requestAnimationFrame(gameLoop);
    }

    // --- Nebula Background ---
    function drawNebula() {
        var grd = ctx.createRadialGradient(W * 0.3, H * 0.2, 50, W * 0.3, H * 0.2, 300);
        grd.addColorStop(0, "rgba(30,0,60,0.3)");
        grd.addColorStop(1, "rgba(0,0,0,0)");
        ctx.fillStyle = grd;
        ctx.fillRect(0, 0, W, H);

        var grd2 = ctx.createRadialGradient(W * 0.7, H * 0.15, 30, W * 0.7, H * 0.15, 200);
        grd2.addColorStop(0, "rgba(0,30,60,0.2)");
        grd2.addColorStop(1, "rgba(0,0,0,0)");
        ctx.fillStyle = grd2;
        ctx.fillRect(0, 0, W, H);
    }

    // Enhance the star field draw to include nebula
    var originalStarDraw = updateAndDrawStars;
    updateAndDrawStars = function (dt) {
        originalStarDraw(dt);
        drawNebula();
    };

    // --- Initialize ---
    initStars();
    requestAnimationFrame(gameLoop);
})();

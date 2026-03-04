window.tanksClient = (() => {
    const state = {
        canvas: null,
        ctx: null,
        dotNetRef: null,
        joined: false,
        connectionId: null,
        latestSnapshot: null,
        keys: { w: false, a: false, s: false, d: false },
        mouseX: 0,
        mouseY: 0,
        firePressed: false,
        mouseDown: false,
        rafId: 0,
        inputTimer: 0,
        handlersBound: false,
    };

    const worldToCanvas = (x, y) => {
        const snap = state.latestSnapshot;
        if (!snap || !state.canvas) return { x, y };
        return {
            x: x * (state.canvas.width / snap.arenaWidth),
            y: y * (state.canvas.height / snap.arenaHeight)
        };
    };

    const canvasToWorld = (x, y) => {
        const snap = state.latestSnapshot;
        if (!snap || !state.canvas) return { x, y };
        return {
            x: x * (snap.arenaWidth / state.canvas.width),
            y: y * (snap.arenaHeight / state.canvas.height)
        };
    };

    const drawTank = (p, isSelf) => {
        const c = worldToCanvas(p.x, p.y);
        const radius = 18;
        const ctx = state.ctx;

        ctx.save();
        ctx.translate(c.x, c.y);
        ctx.rotate(p.bodyAngle);
        ctx.fillStyle = !p.isAlive ? "#777" : (isSelf ? "#1f77b4" : "#2ca02c");
        ctx.beginPath();
        ctx.arc(0, 0, radius, 0, Math.PI * 2);
        ctx.fill();

        ctx.fillStyle = "#222";
        ctx.fillRect(0, -4, radius + 8, 8);
        ctx.restore();

        ctx.save();
        ctx.translate(c.x, c.y);
        ctx.rotate(p.turretAngle);
        ctx.strokeStyle = "#111";
        ctx.lineWidth = 5;
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(28, 0);
        ctx.stroke();
        ctx.restore();

        ctx.fillStyle = "#111";
        ctx.font = "12px Arial";
        ctx.fillText(`${p.name} [${p.hp}]`, c.x - 30, c.y - 24);
    };

    const drawSnapshot = () => {
        if (!state.canvas || !state.ctx) return;
        const snap = state.latestSnapshot;
        const ctx = state.ctx;

        ctx.fillStyle = "#e8f4e8";
        ctx.fillRect(0, 0, state.canvas.width, state.canvas.height);

        if (!snap) {
            ctx.fillStyle = "#222";
            ctx.font = "18px Arial";
            ctx.fillText("Join to start playing Tanks", 20, 30);
            return;
        }

        ctx.fillStyle = "#888";
        for (const obstacle of snap.obstacles) {
            const tl = worldToCanvas(obstacle.x, obstacle.y);
            const br = worldToCanvas(obstacle.x + obstacle.width, obstacle.y + obstacle.height);
            ctx.fillRect(tl.x, tl.y, br.x - tl.x, br.y - tl.y);
        }

        for (const bullet of snap.bullets) {
            const c = worldToCanvas(bullet.x, bullet.y);
            ctx.fillStyle = "#000";
            ctx.beginPath();
            ctx.arc(c.x, c.y, 4, 0, Math.PI * 2);
            ctx.fill();
        }

        for (const p of snap.players) {
            drawTank(p, p.playerId === state.connectionId);
        }
    };

    const renderLoop = () => {
        drawSnapshot();
        state.rafId = requestAnimationFrame(renderLoop);
    };

    const sendInput = async () => {
        if (!state.dotNetRef || !state.joined) return;

        const worldMouse = canvasToWorld(state.mouseX, state.mouseY);
        const payload = {
            forward: state.keys.w,
            backward: state.keys.s,
            turnLeft: state.keys.a,
            turnRight: state.keys.d,
            firePressed: state.firePressed || state.mouseDown,
            mouseX: worldMouse.x,
            mouseY: worldMouse.y
        };

        state.firePressed = false;

        try {
            await state.dotNetRef.invokeMethodAsync("OnInputFrame", payload);
        } catch {
            // no-op
        }
    };

    const bindInput = () => {
        if (state.handlersBound || !state.canvas) {
            return;
        }

        window.addEventListener("keydown", e => {
            if (e.key === "w" || e.key === "W") state.keys.w = true;
            if (e.key === "a" || e.key === "A") state.keys.a = true;
            if (e.key === "s" || e.key === "S") state.keys.s = true;
            if (e.key === "d" || e.key === "D") state.keys.d = true;
        });

        window.addEventListener("keyup", e => {
            if (e.key === "w" || e.key === "W") state.keys.w = false;
            if (e.key === "a" || e.key === "A") state.keys.a = false;
            if (e.key === "s" || e.key === "S") state.keys.s = false;
            if (e.key === "d" || e.key === "D") state.keys.d = false;
        });

        state.canvas.addEventListener("mousemove", e => {
            const rect = state.canvas.getBoundingClientRect();
            state.mouseX = e.clientX - rect.left;
            state.mouseY = e.clientY - rect.top;
        });

        state.canvas.addEventListener("mousedown", e => {
            if (e.button === 0) {
                state.mouseDown = true;
                state.firePressed = true;
            }
        });

        state.canvas.addEventListener("mouseup", e => {
            if (e.button === 0) {
                state.mouseDown = false;
            }
        });

        state.handlersBound = true;
    };

    return {
        init: async (canvasId, dotNetRef) => {
            state.canvas = document.getElementById(canvasId);
            if (!state.canvas) return false;

            state.ctx = state.canvas.getContext("2d");
            state.dotNetRef = dotNetRef;
            state.joined = false;
            state.connectionId = null;
            bindInput();

            cancelAnimationFrame(state.rafId);
            state.rafId = requestAnimationFrame(renderLoop);
            clearInterval(state.inputTimer);
            state.inputTimer = setInterval(sendInput, 33);
            return true;
        },

        setJoined: (joined, connectionId) => {
            state.joined = !!joined;
            state.connectionId = connectionId || null;
        },

        setSnapshot: (snapshot) => {
            state.latestSnapshot = snapshot;
        },

        leaveGame: () => {
            state.joined = false;
            state.connectionId = null;
        },

        dispose: async () => {
            cancelAnimationFrame(state.rafId);
            clearInterval(state.inputTimer);
            state.joined = false;
            state.connectionId = null;
            state.dotNetRef = null;
        }
    };
})();

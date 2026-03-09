window.blazorCopyToClipboard = async (text) => {
    if (!text) return;

    if (navigator.clipboard && window.isSecureContext) {
        await navigator.clipboard.writeText(text);
        return;
    }

    const ta = document.createElement("textarea");
    ta.value = text;
    ta.style.position = "fixed";
    ta.style.left = "-9999px";
    document.body.appendChild(ta);
    ta.focus();
    ta.select();
    document.execCommand("copy");
    document.body.removeChild(ta);
};

window.adminFlappyBird = (() => {
    let canvas = null;
    let ctx = null;
    let dotNetRef = null;
    let animationId = null;
    let keyHandler = null;
    let canvasElementId = null;

    const gravity = 0.45;
    const flapStrength = -7.5;
    const pipeWidth = 64;
    const pipeGap = 125;
    const pipeSpeed = 2.4;
    const pipeSpacing = 210;
    const birdX = 95;
    const birdRadius = 14;

    let gameStarted = false;
    let gameOver = false;
    let score = 0;
    let birdY = 250;
    let birdVelocity = 0;
    let pipes = [];

    const resetState = () => {
        score = 0;
        birdY = (canvas?.height || 600) / 2;
        birdVelocity = 0;
        pipes = [];
        gameOver = false;
        gameStarted = true;

        const width = canvas.width;
        const height = canvas.height;

        for (let i = 0; i < 4; i++) {
            spawnPipe(width + i * pipeSpacing, height);
        }
    };

    const spawnPipe = (x, canvasHeight) => {
        const minGapY = 100;
        const maxGapY = canvasHeight - 100 - pipeGap;
        const gapY = Math.floor(Math.random() * (maxGapY - minGapY + 1)) + minGapY;

        pipes.push({
            x,
            gapTop: gapY,
            passed: false
        });
    };

    const flap = () => {
        if (!gameStarted || gameOver) {
            return;
        }

        birdVelocity = flapStrength;
    };

    const endGame = async () => {
        gameOver = true;
        if (dotNetRef) {
            await dotNetRef.invokeMethodAsync("OnFlappyGameOver", score);
        }
    };

    const checkCollision = () => {
        if (birdY - birdRadius <= 0 || birdY + birdRadius >= canvas.height) {
            return true;
        }

        for (const pipe of pipes) {
            const overlapsX = birdX + birdRadius > pipe.x && birdX - birdRadius < pipe.x + pipeWidth;
            if (!overlapsX) {
                continue;
            }

            const hitsTopPipe = birdY - birdRadius < pipe.gapTop;
            const hitsBottomPipe = birdY + birdRadius > pipe.gapTop + pipeGap;
            if (hitsTopPipe || hitsBottomPipe) {
                return true;
            }
        }

        return false;
    };

    const update = () => {
        if (!gameStarted || gameOver) {
            return;
        }

        birdVelocity += gravity;
        birdY += birdVelocity;

        for (const pipe of pipes) {
            pipe.x -= pipeSpeed;

            if (!pipe.passed && pipe.x + pipeWidth < birdX) {
                pipe.passed = true;
                score += 1;
            }
        }

        if (pipes.length && pipes[0].x + pipeWidth < 0) {
            pipes.shift();
            const lastPipeX = pipes[pipes.length - 1].x;
            spawnPipe(lastPipeX + pipeSpacing, canvas.height);
        }
    };

    const draw = () => {
        if (!ctx || !canvas) {
            return;
        }

        ctx.clearRect(0, 0, canvas.width, canvas.height);

        ctx.fillStyle = "#d9efff";
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        for (const pipe of pipes) {
            ctx.fillStyle = "#2e8b57";
            ctx.fillRect(pipe.x, 0, pipeWidth, pipe.gapTop);
            ctx.fillRect(pipe.x, pipe.gapTop + pipeGap, pipeWidth, canvas.height - (pipe.gapTop + pipeGap));
        }

        ctx.beginPath();
        ctx.fillStyle = "#f7c948";
        ctx.arc(birdX, birdY, birdRadius, 0, Math.PI * 2);
        ctx.fill();

        ctx.fillStyle = "#111";
        ctx.font = "bold 24px Arial";
        ctx.fillText(`Score: ${score}`, 14, 36);

        if (!gameStarted) {
            ctx.font = "bold 22px Arial";
            ctx.fillText("Press Start", 130, canvas.height / 2 - 10);
            ctx.font = "16px Arial";
            ctx.fillText("Use Spacebar to flap", 120, canvas.height / 2 + 22);
        }

        if (gameOver) {
            ctx.fillStyle = "rgba(0,0,0,0.55)";
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.fillStyle = "#fff";
            ctx.font = "bold 28px Arial";
            ctx.fillText("Game Over", 140, canvas.height / 2 - 12);
            ctx.font = "18px Arial";
            ctx.fillText(`Final Score: ${score}`, 145, canvas.height / 2 + 20);
        }
    };

    const tick = async () => {
        update();
        draw();

        if (!gameOver) {
            animationId = window.requestAnimationFrame(tick);
        } else {
            await endGame();
        }

        if (checkCollision() && !gameOver) {
            gameOver = true;
        }
    };

    const init = (canvasId, dotNetObjectRef) => {
        canvasElementId = canvasId;
        canvas = document.getElementById(canvasId);
        if (!canvas) {
            return false;
        }

        ctx = canvas.getContext("2d");
        dotNetRef = dotNetObjectRef;

        if (!keyHandler) {
            keyHandler = (e) => {
                if (e.code === "Space") {
                    e.preventDefault();
                    flap();
                }
            };

            window.addEventListener("keydown", keyHandler);
        }

        gameStarted = false;
        gameOver = false;
        pipes = [];
        birdY = canvas.height / 2;
        draw();
        return true;
    };

    const start = () => {
        if (!canvasElementId) {
            canvasElementId = "adminFlappyCanvas";
        }

        if (!canvas || !ctx) {
            canvas = document.getElementById(canvasElementId);
            ctx = canvas ? canvas.getContext("2d") : null;
        }

        if (!canvas || !ctx) {
            return false;
        }

        if (!keyHandler) {
            keyHandler = (e) => {
                if (e.code === "Space") {
                    e.preventDefault();
                    flap();
                }
            };

            window.addEventListener("keydown", keyHandler);
        }

        if (animationId) {
            window.cancelAnimationFrame(animationId);
            animationId = null;
        }

        resetState();
        draw();
        animationId = window.requestAnimationFrame(tick);
        return true;
    };

    const dispose = () => {
        if (animationId) {
            window.cancelAnimationFrame(animationId);
            animationId = null;
        }

        if (keyHandler) {
            window.removeEventListener("keydown", keyHandler);
            keyHandler = null;
        }

        canvas = null;
        ctx = null;
        dotNetRef = null;
        gameStarted = false;
        gameOver = false;
        pipes = [];
        canvasElementId = null;
    };

    return {
        init,
        start,
        dispose
    };
})();

window.trainingSchoolVideo = (() => {
    const listeners = new WeakMap();

    const isNearEnd = (videoElement, secondsThreshold) => {
        if (!videoElement || typeof videoElement.duration !== "number" || !Number.isFinite(videoElement.duration)) {
            return false;
        }

        const threshold = typeof secondsThreshold === "number" && Number.isFinite(secondsThreshold)
            ? Math.max(0, secondsThreshold)
            : 10;

        return (videoElement.duration - videoElement.currentTime) <= threshold;
    };

    const stopWatching = (videoElement) => {
        if (!videoElement) {
            return;
        }

        const entry = listeners.get(videoElement);
        if (!entry) {
            return;
        }

        videoElement.removeEventListener("timeupdate", entry.handler);
        videoElement.removeEventListener("ended", entry.handler);
        listeners.delete(videoElement);
    };

    const watchForNearEnd = (videoElement, dotNetRef, secondsThreshold) => {
        if (!videoElement || !dotNetRef) {
            return;
        }

        stopWatching(videoElement);

        const handler = () => {
            if (!isNearEnd(videoElement, secondsThreshold)) {
                return;
            }

            stopWatching(videoElement);
            dotNetRef.invokeMethodAsync("NotifyLaunchedVideoNearEndAsync");
        };

        videoElement.addEventListener("timeupdate", handler);
        videoElement.addEventListener("ended", handler);
        listeners.set(videoElement, { handler });

        handler();
    };

    return {
        watchForNearEnd,
        stopWatching
    };
})();

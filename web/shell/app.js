(() => {
  const KEY = {
    ENTER: 0x27,
    EXIT: 0x28,
    UP: 0x22,
    DOWN: 0x23,
    LEFT: 0x24,
    RIGHT: 0x25,
    PGUP: 0x20,
    PGDN: 0x21,
  };

  const canvas = document.getElementById("lcd");
  const statusEl = document.getElementById("status");
  const logEl = document.getElementById("log");
  const ctx = canvas.getContext("2d", { alpha: false });
  ctx.imageSmoothingEnabled = false;

  let frameWidth = 416;
  let frameHeight = 256;
  let frameData = ctx.createImageData(frameWidth, frameHeight);

  function setStatus(text) {
    statusEl.textContent = text;
  }

  function appendLog(text) {
    if (!text) {
      return;
    }
    logEl.textContent += `${text}\n`;
    logEl.scrollTop = logEl.scrollHeight;
  }

  function ensureFrameSize(width, height) {
    if (width === frameWidth && height === frameHeight) {
      return;
    }
    frameWidth = width;
    frameHeight = height;
    canvas.width = width;
    canvas.height = height;
    frameData = ctx.createImageData(width, height);
    appendLog(`[ui] resize framebuffer to ${width}x${height}`);
  }

  function sendKey(code) {
    const mod = window.Module;
    if (!mod || typeof mod._bayeSendKey !== "function") {
      return;
    }
    mod._bayeSendKey(code);
  }

  function mapDomKey(e) {
    if (e.key === "Enter") return KEY.ENTER;
    if (e.key === "Escape" || e.key === "Backspace" || e.key === " ") return KEY.EXIT;
    if (e.key === "ArrowUp") return KEY.UP;
    if (e.key === "ArrowDown") return KEY.DOWN;
    if (e.key === "ArrowLeft") return KEY.LEFT;
    if (e.key === "ArrowRight") return KEY.RIGHT;
    if (e.key === "PageUp") return KEY.PGUP;
    if (e.key === "PageDown") return KEY.PGDN;
    return 0;
  }

  document.addEventListener("keydown", (e) => {
    if (e.repeat) {
      return;
    }
    const code = mapDomKey(e);
    if (!code) {
      return;
    }
    e.preventDefault();
    sendKey(code);
  });

  document.querySelectorAll("button[data-key]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const code = KEY[btn.dataset.key] || 0;
      if (code) {
        sendKey(code);
      }
    });
  });

  window.bayeStart = () => {
    setStatus("engine booting");
    appendLog("[engine] bayeStart");
  };

  window.bayeExit = () => {
    setStatus("engine exited");
    appendLog("[engine] bayeExit");
  };

  window.bayeLoadFileContent = (filename) => {
    const val = window.localStorage.getItem(filename);
    return val || "";
  };

  window.bayeFlushLcdBuffer = (ptr) => {
    const mod = window.Module;
    if (!mod || !mod.HEAPU8) {
      return;
    }

    const bytes = frameWidth * frameHeight * 4;
    if (ptr <= 0 || ptr + bytes > mod.HEAPU8.length) {
      return;
    }

    try {
      const src32 = new Uint32Array(mod.HEAPU8.buffer, ptr, frameWidth * frameHeight);
      const dst32 = new Uint32Array(frameData.data.buffer);
      for (let i = 0; i < dst32.length; i += 1) {
        dst32[i] = src32[i] | 0xff000000;
      }
    } catch {
      const src = mod.HEAPU8.subarray(ptr, ptr + bytes);
      frameData.data.set(src);
      for (let i = 3; i < frameData.data.length; i += 4) {
        frameData.data[i] = 255;
      }
    }

    ctx.putImageData(frameData, 0, 0);
  };

  window.Module = {
    canvas,
    print: (text) => appendLog(text),
    printErr: (text) => appendLog(`[err] ${text}`),
    onRuntimeInitialized: () => {
      setStatus("runtime ready");
      appendLog("[wasm] runtime initialized");
      ensureFrameSize(416, 256);
      if (typeof window.Module._bayeSetLcdSize === "function") {
        window.Module._bayeSetLcdSize(208, 128);
      }
    },
  };
})();

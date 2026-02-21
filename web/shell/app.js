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
  let flushCount = 0;
  let droppedCount = 0;
  let firstFrameLogged = false;
  let bootingSince = 0;
  let pullStarted = false;
  let pullBufPtr = 0;
  let pullBufCap = 0;
  let wPtr = 0;
  let hPtr = 0;

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

  function writeFrameFromPtr(ptr, bytes) {
    const mod = window.Module;
    if (!mod || !mod.HEAPU8) {
      return false;
    }
    if (ptr <= 0 || bytes <= 0 || ptr + bytes > mod.HEAPU8.length) {
      return false;
    }
    const src = mod.HEAPU8.subarray(ptr, ptr + bytes);
    frameData.data.set(src);
    for (let i = 3; i < frameData.data.length; i += 4) {
      frameData.data[i] = 255;
    }
    flushCount += 1;
    if (!firstFrameLogged) {
      firstFrameLogged = true;
      setStatus("engine running");
      appendLog(`[render] first frame size=${frameWidth}x${frameHeight}`);
    }
    ctx.putImageData(frameData, 0, 0);
    return true;
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

  function handleEngineLog(text) {
    appendLog(text);
    const m = String(text).match(/realloc 32bit screen buffer size to \d+ \((\d+)x(\d+)@(\d+)\)/);
    if (!m) {
      return;
    }
    const baseW = Number(m[1]) || 0;
    const baseH = Number(m[2]) || 0;
    const scale = Number(m[3]) || 1;
    const outW = baseW * scale;
    const outH = baseH * scale;
    if (outW > 0 && outH > 0) {
      ensureFrameSize(outW, outH);
    }
  }

  globalThis.bayeStart = () => {
    bootingSince = Date.now();
    setStatus("engine booting");
    appendLog("[engine] bayeStart");
    window.setTimeout(() => {
      if (flushCount === 0 && bootingSince > 0) {
        appendLog("[warn] no frame callback received after 3s");
      }
    }, 3000);
  };

  globalThis.bayeExit = () => {
    setStatus("engine exited");
    appendLog("[engine] bayeExit");
  };

  globalThis.bayeLoadFileContent = (filename) => {
    const val = window.localStorage.getItem(filename);
    return val || "";
  };

  globalThis.bayeFlushLcdBuffer = (ptr) => {
    const mod = window.Module;
    if (!mod || !mod.HEAPU8) {
      return;
    }

    const bytes = frameWidth * frameHeight * 4;
    if (ptr <= 0 || ptr + bytes > mod.HEAPU8.length) {
      droppedCount += 1;
      if (droppedCount <= 5) {
        appendLog(`[warn] drop frame ptr=${ptr} bytes=${bytes} heap=${mod.HEAPU8.length}`);
      }
      return;
    }

    writeFrameFromPtr(ptr, bytes);
  };

  function startPullLoop() {
    if (pullStarted) {
      return;
    }
    pullStarted = true;

    const mod = window.Module;
    if (!mod || typeof mod._bayeCopyFrameRgba !== "function" || typeof mod._malloc !== "function") {
      appendLog("[warn] bayeCopyFrameRgba API missing");
      return;
    }

    wPtr = mod._malloc(4);
    hPtr = mod._malloc(4);
    appendLog("[render] pull loop started");

    const step = () => {
      const needRc = mod._bayeCopyFrameRgba(0, 0, wPtr, hPtr);
      const width = mod.getValue(wPtr, "i32");
      const height = mod.getValue(hPtr, "i32");
      const need = needRc < 0 ? -needRc : needRc;

      if (need > 0 && width > 0 && height > 0) {
        ensureFrameSize(width, height);
        if (pullBufCap < need) {
          if (pullBufPtr) {
            mod._free(pullBufPtr);
          }
          pullBufPtr = mod._malloc(need);
          pullBufCap = need;
        }
        const got = mod._bayeCopyFrameRgba(pullBufPtr, pullBufCap, wPtr, hPtr);
        if (got > 0) {
          writeFrameFromPtr(pullBufPtr, got);
        }
      }

      window.requestAnimationFrame(step);
    };

    window.requestAnimationFrame(step);
  }

  window.Module = {
    canvas,
    print: (text) => handleEngineLog(text),
    printErr: (text) => appendLog(`[err] ${text}`),
    onRuntimeInitialized: () => {
      setStatus("runtime ready");
      appendLog("[wasm] runtime initialized");
      ensureFrameSize(416, 256);
      if (typeof window.Module._bayeSetLcdSize === "function") {
        window.Module._bayeSetLcdSize(208, 128);
      }
      startPullLoop();
    },
  };
})();

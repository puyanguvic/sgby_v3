# Web Debug Workflow (Emscripten)

## 1. Prerequisites

- `emsdk` activated in current shell (`emcmake` is available)
- `cmake`, `ninja`, `python3`

Quick check:

```bash
command -v emcmake cmake ninja python3
```

## 2. Build

```bash
./scripts/build_web_emscripten.sh
```

Build output:

- `web/dist/index.html`
- `web/dist/app.js`
- `web/dist/styles.css`
- `web/dist/baye.js`
- `web/dist/baye.wasm`

## 3. Run

```bash
./scripts/serve_web_build.sh
```

Then open:

- `http://127.0.0.1:8008`

## 4. Why this path is stable for debugging

- JS platform now embeds `dat.lib` + font resources directly in `src/platform/js/fsys.c`
- No external `font24.*` / `dat.lib` runtime file lookup on the browser path
- Web shell has pull-based frame rendering (`bayeCopyFrameRgba`), so display is no longer blocked by callback timing
- Godot/Web now share `platform/common/frontend_api.*` for input + RGBA frame bridge behavior
- You can capture runtime behavior from browser console and the in-page log window

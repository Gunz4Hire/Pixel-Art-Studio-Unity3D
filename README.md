# Pixel Art Suite for Unity

A full-featured pixel art editor integrated directly into the Unity Editor.  
Designed for fast pixel workflow inside Unity: canvas, layers, frames, onion skin, brushes, shapes, reference images, export/import, and timeline thumbnails.

---

## Table of Contents
- [Features](#features)
  - [Core](#core)
  - [Layers & Frames](#layers--frames)
  - [Brushes & Tools](#brushes--tools)
  - [Preview & UX](#preview--ux)
  - [Performance & Reliability](#performance--reliability)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Tool Details](#tool-details)
  - [Canvas Settings](#canvas-settings)
  - [Tools](#tools)
  - [Brush Types](#brush-types)
  - [Layers & Frames](#layers--frames-1)
  - [Export / Import](#export--import)
- [Developer Notes](#developer-notes)
- [Performance Considerations](#performance-considerations)
- [Roadmap / Missing Features](#roadmap--missing-features)
- [License](#license)

---

## Features

### Core
- In-Editor window (**Tools → Pixel Art Suite for Unity**).
- Configurable square canvas (**8 → 256**).
- Zoomable pixel-grid canvas with checkerboard backdrop.
- Exact pixel coordinate readout and crosshair.
- Reference image import with on-canvas alpha.
- **Undo / Redo** *(NOTE: not implemented in v1 — roadmap item)*.
- Save current frame as PNG / export all frames as PNG sequence / import image to current frame or as new frame.

### Layers & Frames
- Multi-layer per frame; layers have **name, opacity, and visibility**.
- Add / clone / remove layers. Reorder layers.
- Per-frame timeline with **thumbnails** and ability to add/clone/remove frames.
- **Onion skin** (previous and next frames) with configurable opacity.

### Brushes & Tools
- Pencil, Eraser, Line, Fill, Dither, Circle, Square, Lighten, Darken, Spray, Dither Spray.
- Brush size (**1–16**) and brush types: Pencil / Soft / Textured.
- Pixel-perfect mode (for line/shape tools).
- Brush preview widget.
- Right-click **color picker** (samples composite image under cursor).
- Shape-dragging workflow: click to start, drag to preview, release to commit.

### Preview & UX
- Shape preview while dragging (wireframe + semi-transparent fill preview at **50% alpha**).
- Reference image overlay with adjustable opacity.
- “Are you sure?” confirmation when creating a new canvas or when attempting to close with unsaved edits.
- Modernized UI color styling for clearer sections.

### Performance & Reliability
- Layers store `Color[]` pixel data and a `Texture2D` for fast GPU drawing.
- Texture `Apply()` calls are **batched**, keeping drawing responsive.
- Composite texture generation is cached and re-generated **only when a layer changed**.

---

## Installation
1. Place **`PixelArtSuiteWindow.cs`** into your Unity project under `Assets/Editor/`.
2. Open the window from Unity’s top menu:  
   **`Tools → Pixel Art Suite for Unity`**
3. Ensure your project is in **Editor mode** (this is an Editor-only tool).

---

## Quick Start
1. Create a new canvas (**File → New**). Confirm dialog appears if unsaved data exists.
2. Use the left-side **Tools panel** to pick a tool and brush type.
3. Click and drag on the canvas to draw.  
   Right-click to **sample color**.
4. Use the **Reference panel** (right) to load an image and adjust opacity.
5. Use the **Timeline** (bottom) to navigate frames, play animation, and export frames.

---

## Tool Details

### Canvas Settings
- **Canvas Size**: Number of pixels per side. Resizes all frames/layers (preserving top-left).
- **Pixel Size**: Screen scaling for each pixel (**4–48**).
- **Grid**: Minor and major grid colors, spacing for major lines, checkerboard colors.

### Tools
- **Pencil / Eraser**: Place pixels with the active brush type. Eraser writes transparent.
- **Line**: Click to begin, release to end. Pixel-perfect mode uses Bresenham algorithm.
- **Circle / Square**: Click to start at center, drag to size, release to commit.  
  A semi-transparent (50%) preview shows while dragging.
- **Fill**: Classic flood fill.
- **Spray / Spray Dither**: Randomized spray behavior.
- **Dither**: Alternating pixel placement pattern.

### Brush Types
- **Pencil**: Hard square/round brush.
- **Soft**: Radial falloff blending with underlying pixels.
- **Textured**: Patterned placement; useful for detail/texture.

### Layers & Frames
- Layers are ordered, named, visible/hidden, and support opacity.  
- Frames can be added, removed, cloned, and previewed in the timeline.

### Export / Import
- Save **current frame** as PNG.
- Export **all frames** as a PNG sequence.
- Import an image into the current frame or as a new frame (scaled to canvas size).

Pixel Art Suite for Unity

A full-featured pixel art editor integrated directly into the Unity Editor.
Designed for fast pixel workflow inside Unity: canvas, layers, frames, onion skin, brushes, shapes, reference images, export/import, and timeline thumbnails.

Table of contents

Features (what it does)

Installation

Quick start

Tool overview (detailed)

Developer notes (data structures and important methods)

Performance considerations & debugging

Roadmap / missing features (ideas to add)

License

Features

Core

In-Editor window (Tools → Pixel Art Suite for Unity).

Configurable square canvas (8 → 256).

Zoomable pixel-grid canvas with checkerboard backdrop.

Exact pixel coordinate readout and crosshair.

Reference image import with on-canvas alpha.

Undo / Redo (NOTE: not implemented in v1 — roadmap item).

Save current frame as PNG / export all frames as PNG sequence / import image to current frame or as new frame.

Layers & Frames

Multi-layer per frame; layers have name, opacity, and visibility.

Add / clone / remove layers. Reorder layers.

Per-frame timeline with thumbnails and ability to add/clone/remove frames.

Onion skin (previous and next frames) with configurable opacity.

Brushes & Tools

Pencil, Eraser, Line, Fill, Dither, Circle, Square, Lighten, Darken, Spray, Dither Spray.

Brush size (1–16) and brush types: Pencil / Soft / Textured.

Pixel-perfect mode (for line/shape tools).

Brush preview widget.

Right-click color picker (samples composite image under cursor).

Shape-dragging workflow: click to start, drag to preview, release to commit.

Preview & UX

Shape preview while dragging (wireframe + now, semi-transparent fill preview at 50% alpha for shapes).

Reference image overlay with slider for opacity.

Are-you-sure confirmation when creating a new canvas or when attempting to close the window with unsaved edits.

Modernized UI color styling for clearer sections.

Performance & Reliability

Layers store Color[] pixel data and a Texture2D used for fast GPU drawing.

Editors calls are batched: texture Apply() calls are minimized so drawing is responsive when clicking/dragging.

Composite texture generation is cached and re-generated only when a layer changed.

Installation

Place PixelArtSuiteWindow.cs (the provided script) into your Unity project under Assets/Editor/.

Open it from Unity top menu: Tools → Pixel Art Suite for Unity.

Make sure your project is in Editor mode (this is an editor tool).

Quick start

Create a new canvas (File → New). Confirm dialog will appear if unsaved data exists.

Use the left-side Tools panel to pick a tool and a brush type.

Click and drag on the canvas to draw. Right-click to sample color.

Use the Reference panel (right) to load an image and adjust opacity.

Use the timeline at bottom to navigate frames, play animation, and export frames.

Tool details (detailed)
Canvas settings

Canvas Size: Number of pixels per side. When changed, all frames/layers are resized preserving top-left area.

Pixel Size: Screen scaling for each pixel (4–48).

Grid: Minor and major grid colors, spacing for "major" grid lines, checkerboard colors.

Tools

Pencil/Eraser: Place pixels using the active brush type (Pencil/Soft/Textured). Eraser writes transparent.

Line: Click to begin, release to end. Pixel-perfect toggles Bresenham algorithm.

Circle / Square: Click to start center, drag to size, release to commit. A preview is rendered with 50% opacity.

Fill: Classic flood fill.

Spray / Spray Dither: Randomized spray paint behavior.

Dither: Alternating pixel placement pattern.

Brush types

Pencil: Hard square/round brush (based on size).

Soft: Radial falloff blending with underlying pixels.

Textured: Patterned placement; useful for noise/detail.

Layers & frames

Layers are ordered, named, visible/hidden, have opacity. Each frame has its own layers.

Frames can be added, removed, cloned, and have thumbnails in the timeline.

Export / Import

Save current frame to PNG.

Export all frames as a PNG sequence.

Load an image into the current frame or import as a new frame (image gets scaled to the current canvas size).

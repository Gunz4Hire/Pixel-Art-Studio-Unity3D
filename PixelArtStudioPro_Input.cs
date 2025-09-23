#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public partial class PixelArtStudioPro
{
    private void HandleCanvasInput(Rect canvasRect)
    {
        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;

        if (canvasRect.Contains(mousePos))
        {
            Vector2Int pixel = ScreenToPixel(mousePos, canvasRect);
            hoverX = pixel.x;
            hoverY = pixel.y;

            // Handle panning with middle mouse button
            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                pan += e.delta;
                e.Use();
                Repaint();
            }

            // Handle zoom with scroll wheel
            if (e.type == EventType.ScrollWheel)
            {
                float oldZoom = zoom;
                zoom = Mathf.Clamp(zoom - e.delta.y * 0.03f, 0.1f, 5f);
                // Adjust pan to zoom toward mouse position
                Vector2 mouseNorm = new Vector2(
                    (mousePos.x - canvasRect.x) / canvasRect.width,
                    (mousePos.y - canvasRect.y) / canvasRect.height
                );
                Vector2 canvasCenter = new Vector2(canvasRect.x + canvasRect.width / 2, canvasRect.y + canvasRect.height / 2);
                pan += (mousePos - canvasCenter) * (zoom / oldZoom - 1f);
                e.Use();
                Repaint();
            }

            // Handle bone manipulation
            if (HandleBoneInput(canvasRect, e, pixel)) return;

            // Handle weight painting
            if (weightPaintMode && e.type == EventType.MouseDrag && e.button == 0)
            {
                PaintWeights(pixel.x, pixel.y);
                e.Use();
                Repaint();
            }

            // Handle drawing tools
            if (e.button == 0 || e.button == 1)
            {
                if (e.type == EventType.MouseDown)
                {
                    if (currentTool == Tool.Gradient)
                    {
                        gradientStart = pixel;
                        isDrawingGradient = true;
                        e.Use();
                    }
                    else if (currentTool == Tool.Select)
                    {
                        if (currentSelection.Contains(pixel) && !e.shift)
                        {
                            selectionMoving = true;
                            selectionMoveOffset = new Vector2(pixel.x - currentSelection.x, pixel.y - currentSelection.y);
                        }
                        else
                        {
                            isSelecting = true;
                            selectStart = pixel;
                            if (!e.shift) ClearSelection();
                        }
                        e.Use();
                    }
                    else if (currentTool == Tool.MagicWand)
                    {
                        DoMagicWandSelection(pixel);
                        e.Use();
                    }
                    else if (currentTool == Tool.Line || currentTool == Tool.Circle || currentTool == Tool.Square)
                    {
                        isDraggingShape = true;
                        shapeStart = pixel;
                        shapeCurrent = pixel;
                        e.Use();
                    }
                    else
                    {
                        // Regular drawing tools
                        if (e.button == 0) // Left click - draw
                        {
                            SaveUndoState();
                            DrawAtPixel(pixel.x, pixel.y);
                        }
                        else if (e.button == 1) // Right click - pick color
                        {
                            PickColorAtPixel(pixel.x, pixel.y);
                        }
                        e.Use();
                    }
                }
                else if (e.type == EventType.MouseDrag)
                {
                    if (selectionMoving)
                    {
                        MoveSelection(Mathf.RoundToInt(e.delta.x / (basePixelSize * zoom)),
                                     -Mathf.RoundToInt(e.delta.y / (basePixelSize * zoom)));
                        e.Use();
                    }
                    else if (isSelecting)
                    {
                        shapeCurrent = pixel;
                        UpdateSelectionRect();
                        e.Use();
                    }
                    else if (isDraggingShape)
                    {
                        shapeCurrent = pixel;
                        e.Use();
                    }
                    else if (isDrawingGradient)
                    {
                        gradientEnd = pixel;
                        e.Use();
                    }
                    else if (e.button == 0) // Left drag - continue drawing
                    {
                        DrawAtPixel(pixel.x, pixel.y);
                        e.Use();
                    }
                    Repaint();
                }
                else if (e.type == EventType.MouseUp)
                {
                    if (selectionMoving)
                    {
                        selectionMoving = false;
                        e.Use();
                    }
                    else if (isSelecting)
                    {
                        isSelecting = false;
                        UpdateSelectionRect();
                        e.Use();
                    }
                    else if (isDraggingShape)
                    {
                        isDraggingShape = false;
                        CompleteShapeDrawing();
                        e.Use();
                    }
                    else if (isDrawingGradient)
                    {
                        isDrawingGradient = false;
                        ApplyGradient();
                        e.Use();
                    }
                    Repaint();
                }
            }
        }
        else
        {
            hoverX = -1;
            hoverY = -1;
        }
    }

    private bool HandleBoneInput(Rect canvasRect, Event e, Vector2Int pixel)
    {
        if (boneAnim.Bones.Count == 0) return false;

        var frame = boneAnim.Frames.Count > anim.CurrentFrameIndex ? boneAnim.Frames[anim.CurrentFrameIndex] : null;
        if (frame == null) return false;

        // Check if clicking on a bone handle
        for (int i = 0; i < boneAnim.Bones.Count; i++)
        {
            var bone = boneAnim.Bones[i];
            var pose = frame.Poses.Count > i ? frame.Poses[i] : null;
            if (pose == null) continue;

            Vector2 screenPos = PixelToScreen(Vector2Int.RoundToInt(pose.Position), canvasRect);
            float handleRadius = boneHandleSize;

            if (Vector2.Distance(e.mousePosition, screenPos) <= handleRadius)
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    selectedBone = i;
                    isMovingBone = true;
                    boneDragOffset = screenPos - e.mousePosition;
                    e.Use();
                    return true;
                }
            }
        }

        // Add new bone if in add mode
        if (isAddingBone && e.type == EventType.MouseDown && e.button == 0)
        {
            AddNewBone(pixel);
            e.Use();
            return true;
        }

        // Move bone
        if (isMovingBone && e.type == EventType.MouseDrag && e.button == 0 && selectedBone >= 0)
        {
            Vector2 newPos = e.mousePosition + boneDragOffset;
            Vector2Int pixelPos = ScreenToPixel(newPos, canvasRect);
            frame.Poses[selectedBone].Position = pixelPos;
            e.Use();
            Repaint();
            return true;
        }

        if (isMovingBone && e.type == EventType.MouseUp && e.button == 0)
        {
            isMovingBone = false;
            e.Use();
            return true;
        }

        return false;
    }

    private void AddNewBone(Vector2Int position)
    {
        var newBone = new Bone
        {
            Name = newBoneName,
            ParentIndex = -1,
            Position = position,
            Length = 10f,
            Rotation = 0f
        };

        boneAnim.Bones.Add(newBone);

        // Add pose for this bone to all frames
        foreach (var frame in boneAnim.Frames)
        {
            frame.Poses.Add(new BonePose(position, 0f));
        }

        selectedBone = boneAnim.Bones.Count - 1;
        isAddingBone = false;
        Repaint();
    }

    private void DeleteSelectedBone()
    {
        if (selectedBone < 0 || selectedBone >= boneAnim.Bones.Count) return;

        boneAnim.Bones.RemoveAt(selectedBone);
        foreach (var frame in boneAnim.Frames)
        {
            if (frame.Poses.Count > selectedBone)
                frame.Poses.RemoveAt(selectedBone);
        }

        // Update parent indices
        foreach (var bone in boneAnim.Bones)
        {
            if (bone.ParentIndex == selectedBone) bone.ParentIndex = -1;
            else if (bone.ParentIndex > selectedBone) bone.ParentIndex--;
        }

        selectedBone = -1;
        Repaint();
    }

    private void ClearAllBones()
    {
        boneAnim.Bones.Clear();
        foreach (var frame in boneAnim.Frames)
        {
            frame.Poses.Clear();
        }
        selectedBone = -1;
        Repaint();
    }

    private void PaintWeights(int x, int y)
    {
        if (boneWeights == null)
        {
            boneWeights = new int[canvasSize, canvasSize];
            boneWeightsStrength = new float[canvasSize, canvasSize];
            for (int iy = 0; iy < canvasSize; iy++)
                for (int ix = 0; ix < canvasSize; ix++)
                    boneWeights[ix, iy] = -1;
        }

        int brushRadius = brushSize / 2;
        for (int dy = -brushRadius; dy <= brushRadius; dy++)
        {
            for (int dx = -brushRadius; dx <= brushRadius; dx++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px < 0 || px >= canvasSize || py < 0 || py >= canvasSize) continue;

                if (brushTipShape == BrushTipShape.Circle)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > brushRadius) continue;
                }

                boneWeights[px, py] = weightPaintBone;
                boneWeightsStrength[px, py] = weightPaintStrength;
            }
        }
    }

    private void DrawAtPixel(int x, int y)
    {
        if (currentTool == Tool.Eraser)
        {
            brushSys.ApplyBrush(anim.CurrentFrame, x, y, Color.clear, brushSize, currentBrush, brushTipShape,
                               symmetryX, symmetryY, radialSymmetry, radialFolds, pixelPerfect, brushFalloff);
        }
        else
        {
            brushSys.ApplyBrush(anim.CurrentFrame, x, y, currentColor, brushSize, currentBrush, brushTipShape,
                               symmetryX, symmetryY, radialSymmetry, radialFolds, pixelPerfect, brushFalloff);
        }
        anim.CurrentFrame.ApplyAllTextures();
        anim.MarkDirty();
        hasUnsavedChanges = true;
        Repaint();
    }

    private void PickColorAtPixel(int x, int y)
    {
        currentColor = anim.CurrentFrame.GetPixel(x, y);
        Repaint();
    }

    private void CompleteShapeDrawing()
    {
        if (shapeStart.x < 0 || shapeStart.y < 0 || shapeCurrent.x < 0 || shapeCurrent.y < 0) return;

        SaveUndoState();

        if (currentTool == Tool.Line)
        {
            brushSys.DrawLine(anim.CurrentFrame, shapeStart.x, shapeStart.y, shapeCurrent.x, shapeCurrent.y,
                             currentColor, brushSize, currentBrush, brushTipShape,
                             symmetryX, symmetryY, radialSymmetry, radialFolds);
        }
        else if (currentTool == Tool.Circle)
        {
            int radius = Mathf.RoundToInt(Vector2Int.Distance(shapeStart, shapeCurrent));
            brushSys.DrawCircle(anim.CurrentFrame, shapeStart.x, shapeStart.y, radius,
                               currentColor, brushSize, currentBrush, brushTipShape,
                               symmetryX, symmetryY, radialSymmetry, radialFolds);
        }
        else if (currentTool == Tool.Square)
        {
            Vector2Int start = new Vector2Int(Mathf.Min(shapeStart.x, shapeCurrent.x), Mathf.Min(shapeStart.y, shapeCurrent.y));
            Vector2Int end = new Vector2Int(Mathf.Max(shapeStart.x, shapeCurrent.x), Mathf.Max(shapeStart.y, shapeCurrent.y));
            brushSys.DrawRect(anim.CurrentFrame, start.x, start.y, end.x, end.y,
                             currentColor, brushSize, currentBrush, brushTipShape,
                             symmetryX, symmetryY, radialSymmetry, radialFolds);
        }

        anim.CurrentFrame.ApplyAllTextures();
        anim.MarkDirty();
        hasUnsavedChanges = true;
        shapeStart = new Vector2Int(-1, -1);
        shapeCurrent = new Vector2Int(-1, -1);
    }

    private void ApplyGradient()
    {
        if (gradientStart.x < 0 || gradientStart.y < 0 || gradientEnd.x < 0 || gradientEnd.y < 0) return;

        SaveUndoState();

        Vector2 start = gradientStart;
        Vector2 end = gradientEnd;
        float length = Vector2.Distance(start, end);
        if (length < 0.1f) return;

        Vector2 dir = (end - start).normalized;

        for (int y = 0; y < canvasSize; y++)
        {
            for (int x = 0; x < canvasSize; x++)
            {
                Vector2 point = new Vector2(x, y);
                float t = 0f;

                if (currentGradientType == GradientType.Linear)
                {
                    Vector2 proj = start + Vector2.Dot(point - start, dir) * dir;
                    t = Vector2.Distance(start, proj) / length;
                    if (Vector2.Dot(point - start, dir) < 0) t = 0f;
                    if (Vector2.Dot(point - end, -dir) < 0) t = 1f;
                }
                else if (currentGradientType == GradientType.Radial)
                {
                    t = Vector2.Distance(point, start) / length;
                    t = Mathf.Clamp01(t);
                }
                else if (currentGradientType == GradientType.Angular)
                {
                    float angle = Vector2.SignedAngle(dir, point - start);
                    t = (angle + 180f) / 360f;
                }
                else if (currentGradientType == GradientType.Diamond)
                {
                    Vector2 d = point - start;
                    t = (Mathf.Abs(d.x) + Mathf.Abs(d.y)) / (Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y));
                    t = Mathf.Clamp01(t);
                }
                else if (currentGradientType == GradientType.Reflected)
                {
                    Vector2 proj = start + Vector2.Dot(point - start, dir) * dir;
                    float dist = Vector2.Distance(start, proj);
                    t = dist / length;
                    t = Mathf.PingPong(t * 2f, 1f);
                }

                // Find gradient color for this t value
                Color color = EvaluateGradient(t);
                anim.CurrentFrame.SetPixel(x, y, color);
            }
        }

        anim.CurrentFrame.ApplyAllTextures();
        anim.MarkDirty();
        hasUnsavedChanges = true;
        gradientStart = new Vector2Int(-1, -1);
        gradientEnd = new Vector2Int(-1, -1);
    }

    private Color EvaluateGradient(float t)
    {
        if (gradientColors.Count == 0) return Color.black;
        if (gradientColors.Count == 1) return gradientColors[0];

        for (int i = 0; i < gradientColors.Count - 1; i++)
        {
            if (t >= gradientPositions[i] && t <= gradientPositions[i + 1])
            {
                float localT = (t - gradientPositions[i]) / (gradientPositions[i + 1] - gradientPositions[i]);
                return Color.Lerp(gradientColors[i], gradientColors[i + 1], localT);
            }
        }

        return t <= gradientPositions[0] ? gradientColors[0] : gradientColors[gradientColors.Count - 1];
    }

    private void DoMagicWandSelection(Vector2Int startPixel)
    {
        Color targetColor = anim.CurrentFrame.GetPixel(startPixel.x, startPixel.y);
        bool[,] visited = new bool[canvasSize, canvasSize];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        List<Vector2Int> selectedPixels = new List<Vector2Int>();

        queue.Enqueue(startPixel);
        visited[startPixel.x, startPixel.y] = true;

        while (queue.Count > 0)
        {
            Vector2Int pixel = queue.Dequeue();
            selectedPixels.Add(pixel);

            // Check adjacent pixels
            CheckPixelForMagicWand(pixel.x + 1, pixel.y, targetColor, visited, queue);
            CheckPixelForMagicWand(pixel.x - 1, pixel.y, targetColor, visited, queue);
            CheckPixelForMagicWand(pixel.x, pixel.y + 1, targetColor, visited, queue);
            CheckPixelForMagicWand(pixel.x, pixel.y - 1, targetColor, visited, queue);

            if (!magicWandContinuous)
            {
                CheckPixelForMagicWand(pixel.x + 1, pixel.y + 1, targetColor, visited, queue);
                CheckPixelForMagicWand(pixel.x - 1, pixel.y - 1, targetColor, visited, queue);
                CheckPixelForMagicWand(pixel.x + 1, pixel.y - 1, targetColor, visited, queue);
                CheckPixelForMagicWand(pixel.x - 1, pixel.y + 1, targetColor, visited, queue);
            }
        }

        if (selectedPixels.Count > 0)
        {
            // Create selection rect that contains all selected pixels
            int minX = selectedPixels.Min(p => p.x);
            int maxX = selectedPixels.Max(p => p.x);
            int minY = selectedPixels.Min(p => p.y);
            int maxY = selectedPixels.Max(p => p.y);

            currentSelection = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            selectionBuffer = new Color[currentSelection.width * currentSelection.height];

            // Copy selection to buffer
            for (int y = 0; y < currentSelection.height; y++)
            {
                for (int x = 0; x < currentSelection.width; x++)
                {
                    int px = currentSelection.x + x;
                    int py = currentSelection.y + y;
                    selectionBuffer[y * currentSelection.width + x] = anim.CurrentFrame.GetPixel(px, py);
                }
            }
        }

        Repaint();
    }

    private void CheckPixelForMagicWand(int x, int y, Color targetColor, bool[,] visited, Queue<Vector2Int> queue)
    {
        if (x < 0 || x >= canvasSize || y < 0 || y >= canvasSize || visited[x, y]) return;

        Color pixelColor = anim.CurrentFrame.GetPixel(x, y);
        float colorDiff = Mathf.Abs(pixelColor.r - targetColor.r) +
                         Mathf.Abs(pixelColor.g - targetColor.g) +
                         Mathf.Abs(pixelColor.b - targetColor.b) +
                         Mathf.Abs(pixelColor.a - targetColor.a);

        if (colorDiff <= magicWandTolerance * 4f) // 4 channels max difference
        {
            visited[x, y] = true;
            queue.Enqueue(new Vector2Int(x, y));
        }
    }

    private void UpdateSelectionRect()
    {
        int minX = Mathf.Min(selectStart.x, shapeCurrent.x);
        int maxX = Mathf.Max(selectStart.x, shapeCurrent.x);
        int minY = Mathf.Min(selectStart.y, shapeCurrent.y);
        int maxY = Mathf.Max(selectStart.y, shapeCurrent.y);

        currentSelection = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private void CopySelection()
    {
        if (currentSelection.width <= 0 || currentSelection.height <= 0) return;

        selectionBuffer = new Color[currentSelection.width * currentSelection.height];
        for (int y = 0; y < currentSelection.height; y++)
        {
            for (int x = 0; x < currentSelection.width; x++)
            {
                int px = currentSelection.x + x;
                int py = currentSelection.y + y;
                selectionBuffer[y * currentSelection.width + x] = anim.CurrentFrame.GetPixel(px, py);
            }
        }
    }

    private void PasteSelection()
    {
        if (selectionBuffer == null || selectionBuffer.Length == 0) return;

        SaveUndoState();

        int width = (int)Mathf.Sqrt(selectionBuffer.Length);
        int height = selectionBuffer.Length / width;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int px = currentSelection.x + x;
                int py = currentSelection.y + y;
                if (px >= 0 && px < canvasSize && py >= 0 && py < canvasSize)
                {
                    anim.CurrentFrame.SetPixel(px, py, selectionBuffer[y * width + x]);
                }
            }
        }

        anim.CurrentFrame.ApplyAllTextures();
        anim.MarkDirty();
        hasUnsavedChanges = true;
        Repaint();
    }

    private void ClearSelection()
    {
        currentSelection = new RectInt(0, 0, 0, 0);
        selectionBuffer = null;
        selectionMoving = false;
        Repaint();
    }

    private void MoveSelection(int dx, int dy)
    {
        if (currentSelection.width <= 0 || currentSelection.height <= 0 || selectionBuffer == null) return;

        SaveUndoState();

        // Clear original area
        for (int y = 0; y < currentSelection.height; y++)
        {
            for (int x = 0; x < currentSelection.width; x++)
            {
                int px = currentSelection.x + x;
                int py = currentSelection.y + y;
                if (px >= 0 && px < canvasSize && py >= 0 && py < canvasSize)
                {
                    anim.CurrentFrame.SetPixel(px, py, Color.clear);
                }
            }
        }

        // Update selection position
        currentSelection.x += dx;
        currentSelection.y += dy;

        // Paste at new position
        PasteSelection();
    }

    private void FlipSelectionH()
    {
        if (selectionBuffer == null || currentSelection.width <= 0) return;

        SaveUndoState();

        int width = currentSelection.width;
        int height = currentSelection.height;
        Color[] flipped = new Color[selectionBuffer.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flipped[y * width + x] = selectionBuffer[y * width + (width - 1 - x)];
            }
        }

        selectionBuffer = flipped;
        PasteSelection();
    }

    private void FlipSelectionV()
    {
        if (selectionBuffer == null || currentSelection.height <= 0) return;

        SaveUndoState();

        int width = currentSelection.width;
        int height = currentSelection.height;
        Color[] flipped = new Color[selectionBuffer.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flipped[y * width + x] = selectionBuffer[(height - 1 - y) * width + x];
            }
        }

        selectionBuffer = flipped;
        PasteSelection();
    }

    private void ApplyTransformToTopLayer(LayerTransform transform)
    {
        SaveUndoState();

        Color[] pixels = anim.CurrentFrame.GetPixels();
        Color[] transformed = new Color[pixels.Length];

        for (int y = 0; y < canvasSize; y++)
        {
            for (int x = 0; x < canvasSize; x++)
            {
                int srcIndex = y * canvasSize + x;
                int dstIndex = 0;

                if (transform == LayerTransform.FlipH)
                    dstIndex = y * canvasSize + (canvasSize - 1 - x);
                else if (transform == LayerTransform.FlipV)
                    dstIndex = (canvasSize - 1 - y) * canvasSize + x;
                else if (transform == LayerTransform.Rotate90)
                    dstIndex = x * canvasSize + (canvasSize - 1 - y);

                transformed[dstIndex] = pixels[srcIndex];
            }
        }

        anim.CurrentFrame.SetPixels(transformed);
        anim.CurrentFrame.ApplyAllTextures();
        anim.MarkDirty();
        hasUnsavedChanges = true;
        Repaint();
    }
}
#endif
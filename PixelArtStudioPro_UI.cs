#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public partial class PixelArtStudioPro
{
    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("New", EditorStyles.toolbarButton)) ShowNewCanvasDialog();

        if (GUILayout.Button("Save PNG", EditorStyles.toolbarButton)) DoSavePNG();
        if (GUILayout.Button("Load Image", EditorStyles.toolbarButton)) DoLoadImage();
        if (GUILayout.Button("Save Project", EditorStyles.toolbarButton)) DoSaveProject();
        if (GUILayout.Button("Load Project", EditorStyles.toolbarButton)) DoLoadProject();
        if (GUILayout.Button("Undo", EditorStyles.toolbarButton)) LayerUndo();
        if (GUILayout.Button("Redo", EditorStyles.toolbarButton)) LayerRedo();

        if (GUILayout.Button("Clear Frame", EditorStyles.toolbarButton))
        {
            anim.CurrentFrame.Clear();
            anim.CurrentFrame.ApplyAllTextures();
            anim.MarkDirty();
            hasUnsavedChanges = true;
        }

        GUILayout.Space(6);
        GUILayout.FlexibleSpace();

        autosaveEnabled = EditorGUILayout.Toggle("Enable Autosave", autosaveEnabled);
        autoSaveIntervalSeconds = EditorGUILayout.Slider("Autosave Interval (sec)", (float)autoSaveIntervalSeconds, 10f, 600f);

        GUILayout.EndHorizontal();
    }

    private void DrawLeftPanel()
    {
        GUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth - 16));
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll, false, false);

        DrawPanelHeader("Tools", uiAccent);
        GUILayout.Space(4);

        GUILayout.BeginHorizontal(GUILayout.MaxWidth(LeftPanelWidth - 32));

        // Left column tools
        GUILayout.BeginVertical();
        if (GUILayout.Toggle(currentTool == Tool.Pencil, "Pencil (B)", "Button", GUILayout.Height(30), GUILayout.MaxWidth((LeftPanelWidth - 32) / 2))) currentTool = Tool.Pencil;
        if (GUILayout.Toggle(currentTool == Tool.Fill, "Fill (F)", "Button", GUILayout.Height(30), GUILayout.MaxWidth((LeftPanelWidth - 32) / 2))) currentTool = Tool.Fill;
        if (GUILayout.Toggle(currentTool == Tool.Circle, "Circle (C)", "Button", GUILayout.Height(30), GUILayout.MaxWidth((LeftPanelWidth - 32) / 2))) currentTool = Tool.Circle;
        if (GUILayout.Toggle(currentTool == Tool.Square, "Square (R)", "Button", GUILayout.Height(30), GUILayout.MaxWidth((LeftPanelWidth - 32) / 2))) currentTool = Tool.Square;
        GUILayout.EndVertical();

        // Right column tools
        GUILayout.BeginVertical();
        if (GUILayout.Toggle(currentTool == Tool.Eraser, "Eraser (E)", "Button", GUILayout.Height(30), GUILayout.MaxWidth((LeftPanelWidth - 32) / 2))) currentTool = Tool.Eraser;
        if (GUILayout.Toggle(currentTool == Tool.Gradient, "Gradient (G)", "Button", GUILayout.Height(30), GUILayout.MaxWidth((LeftPanelWidth - 32) / 2))) currentTool = Tool.Gradient;
        if (GUILayout.Toggle(currentTool == Tool.Line, "Line (L)", "Button", GUILayout.Height(30), GUILayout.MaxWidth((LeftPanelWidth - 32) / 2))) currentTool = Tool.Line;
        if (GUILayout.Toggle(currentTool == Tool.Select, "Select (S)", "Button", GUILayout.Height(30), GUILayout.MaxWidth((LeftPanelWidth - 32) / 2))) currentTool = Tool.Select;
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        // Magic Wand
        if (GUILayout.Toggle(currentTool == Tool.MagicWand, "Magic Wand (M)", "Button", GUILayout.Height(34), GUILayout.MaxWidth(LeftPanelWidth - 32)))
            currentTool = Tool.MagicWand;

        if (currentTool == Tool.MagicWand)
        {
            GUILayout.Space(4);
            DrawPanelHeader("Magic Wand Settings", uiAccent);
            magicWandTolerance = EditorGUILayout.Slider("Tolerance", magicWandTolerance, 0f, 1f, GUILayout.MaxWidth(LeftPanelWidth - 32));
            magicWandContinuous = EditorGUILayout.Toggle("Continuous", magicWandContinuous, GUILayout.MaxWidth(LeftPanelWidth - 32));
        }

        // Gradient options
        if (currentTool == Tool.Gradient)
        {
            GUILayout.Space(8);
            DrawPanelHeader("Gradient Tool", uiAccent);

            gradientScroll = GUILayout.BeginScrollView(gradientScroll, GUILayout.Height(120));

            // Gradient type selection
            currentGradientType = (GradientType)EditorGUILayout.EnumPopup("Gradient Type", currentGradientType, GUILayout.MaxWidth(LeftPanelWidth - 32));

            Rect colorCountRect = GUILayoutUtility.GetRect(200, 20);
            gradientColorCount = EditorGUI.IntSlider(colorCountRect, "Colors", gradientColorCount, 2, 8);

            while (gradientColors.Count < gradientColorCount)
            {
                gradientColors.Add(Color.white);
                gradientPositions.Add(1f);
            }
            while (gradientColors.Count > gradientColorCount)
            {
                gradientColors.RemoveAt(gradientColors.Count - 1);
                gradientPositions.RemoveAt(gradientPositions.Count - 1);
            }

            for (int i = 0; i < gradientColorCount; i++)
            {
                GUILayout.BeginHorizontal();
                gradientColors[i] = EditorGUILayout.ColorField(GUIContent.none, gradientColors[i], GUILayout.Width(80));
                gradientPositions[i] = EditorGUILayout.Slider(gradientPositions[i], 0f, 1f, GUILayout.Width(120));
                GUILayout.EndHorizontal();
            }

            // Add gradient interpolation options
            GUILayout.Space(4);
            GUILayout.Label("Interpolation: RGB (Smooth)");

            GUILayout.EndScrollView();

            // Sort gradient stops
            var zipped = gradientColors.Zip(gradientPositions, (c, p) => new { c, p }).OrderBy(z => z.p).ToList();
            for (int i = 0; i < gradientColorCount; i++)
            {
                gradientColors[i] = zipped[i].c;
                gradientPositions[i] = zipped[i].p;
            }

            GUILayout.Label("Drag a line on canvas to apply gradient.", EditorStyles.miniLabel);
        }

        GUILayout.Space(8);
        DrawPanelHeader("Brush", uiAccent);

        currentBrush = (BrushSystem.BrushType)EditorGUILayout.EnumPopup("Brush Type", currentBrush, GUILayout.MaxWidth(LeftPanelWidth - 32));
        brushSize = EditorGUILayout.IntSlider("Brush Size", brushSize, 1, 16, GUILayout.MaxWidth(LeftPanelWidth - 32));
        brushTipShape = (BrushTipShape)EditorGUILayout.EnumPopup("Brush Tip", brushTipShape, GUILayout.MaxWidth(LeftPanelWidth - 32));
        pixelPerfect = EditorGUILayout.Toggle("Pixel Perfect", pixelPerfect, GUILayout.MaxWidth(LeftPanelWidth - 32));
        brushFalloff = EditorGUILayout.Toggle("Brush Falloff", brushFalloff, GUILayout.MaxWidth(LeftPanelWidth - 32));

        DrawPanelHeader("Symmetry Tools", uiAccent);
        symmetryX = EditorGUILayout.Toggle("Symmetry X", symmetryX, GUILayout.MaxWidth(LeftPanelWidth - 32));
        symmetryY = EditorGUILayout.Toggle("Symmetry Y", symmetryY, GUILayout.MaxWidth(LeftPanelWidth - 32));

        radialSymmetry = EditorGUILayout.Toggle("Radial Symmetry", radialSymmetry, GUILayout.MaxWidth(LeftPanelWidth - 32));
        if (radialSymmetry)
        {
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(LeftPanelWidth - 32));
            GUILayout.Label("   Folds", GUILayout.Width(60));
            radialFolds = EditorGUILayout.IntSlider(radialFolds, 2, MaxRadialFolds, GUILayout.Width(120 - 16));
            GUILayout.EndHorizontal();
        }

        DrawPanelHeader("Transforms", uiAccent);
        GUILayout.BeginHorizontal(GUILayout.MaxWidth(RightPanelWidth - 32));
        if (GUILayout.Button("Flip H", GUILayout.Width(60))) ApplyTransformToTopLayer(LayerTransform.FlipH);
        if (GUILayout.Button("Flip V", GUILayout.Width(60))) ApplyTransformToTopLayer(LayerTransform.FlipV);
        if (GUILayout.Button("Rotate 90", GUILayout.Width(80))) ApplyTransformToTopLayer(LayerTransform.Rotate90);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        DrawPanelHeader("Custom Brushes", uiAccent);

        GUILayout.BeginVertical(GUILayout.MaxWidth(LeftPanelWidth - 32));
        DrawBrushPanel();
        GUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawCanvasPanel()
    {
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawPanelHeader("Canvas", uiAccent);

        float areaW = Mathf.Max(200, position.width - LeftPanelWidth - RightPanelWidth - 40);
        float areaH = Mathf.Max(200, position.height - 180);
        Rect areaRect = GUILayoutUtility.GetRect(areaW, areaH, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        float displaySize = canvasSize * basePixelSize * zoom;
        float cx = areaRect.x + (areaRect.width - displaySize) * 0.5f + pan.x;
        float cy = areaRect.y + (areaRect.height - displaySize) * 0.5f + pan.y;
        Rect canvasRect = new Rect(cx, cy, displaySize, displaySize);

        EditorGUI.DrawRect(areaRect, new Color(0.08f, 0.08f, 0.09f, 1f));

        DrawCheckerboard(canvasRect);

        if (showTilingPreview) DrawTilingPreview(canvasRect);
        if (showIsometricGrid) DrawIsometricGrid(canvasRect);

        if (showReference && referenceImage != null)
        {
            Color prev = GUI.color;
            GUI.color = new Color(1, 1, 1, referenceOpacity);
            GUI.DrawTexture(canvasRect, referenceImage, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        int pixelScale = Mathf.Max(1, Mathf.RoundToInt(basePixelSize * zoom));
        Texture2D composite = anim.GetCompositeTexture(pixelScale, onionEnabled, true, prevOnionOpacity, true, nextOnionOpacity, false, -1);
        if (composite != null) GUI.DrawTexture(canvasRect, composite, ScaleMode.StretchToFill);

        DrawBonesOnCanvas(canvasRect);
        HandleCanvasInput(canvasRect);

        if (isDraggingShape) DrawShapePreview(canvasRect);
        if (currentSelection.width > 0 && currentSelection.height > 0) DrawSelectionOverlay(canvasRect);
        DrawGridClamped(canvasRect);

        if (hoverX >= 0 && hoverY >= 0)
            GUI.Box(new Rect(canvasRect.x + 6, canvasRect.y + canvasRect.height + 6, 160, 20), $"Pixel: ({hoverX},{hoverY})");

        if (weightPaintMode) DrawWeightPaintOverlay(canvasRect);

        if (!isDraggingShape && !isSelecting && currentTool != Tool.Gradient && hoverX >= 0 && hoverY >= 0)
        {
            Handles.BeginGUI();
            Color prev = Handles.color;
            Handles.color = new Color(1, 1, 1, 0.18f);
            float size = brushSize * basePixelSize * zoom;
            Vector2 center = PixelToScreen(new Vector2Int(hoverX, hoverY), canvasRect) + new Vector2(basePixelSize * zoom / 2, basePixelSize * zoom / 2);
            if (brushTipShape == BrushTipShape.Circle)
                Handles.DrawWireDisc(center, Vector3.forward, size / 2f);
            else
                Handles.DrawWireCube(center, new Vector3(size, size, 0));
            Handles.color = prev;
            Handles.EndGUI();
        }

        GUILayout.EndVertical();
    }

    private void DrawTilingPreview(Rect canvasRect)
    {
        if (!showTilingPreview) return;

        int repetitions = tilingGridSize;
        Handles.BeginGUI();
        Color prev = Handles.color;
        Handles.color = new Color(1f, 1f, 1f, 0.1f);

        for (int x = -repetitions; x <= repetitions; x++)
        {
            for (int y = -repetitions; y <= repetitions; y++)
            {
                if (x == 0 && y == 0) continue;

                Rect tileRect = new Rect(
                    canvasRect.x + x * canvasRect.width,
                    canvasRect.y + y * canvasRect.height,
                    canvasRect.width,
                    canvasRect.height
                );

                Texture2D composite = anim.GetCompositeTexture(Mathf.Max(1, Mathf.RoundToInt(basePixelSize * zoom)),
                    false, false, 0f, false, 0f, false, anim.CurrentFrameIndex);

                if (composite != null)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.2f);
                    GUI.DrawTexture(tileRect, composite, ScaleMode.StretchToFill);
                    GUI.color = Color.white;
                }
            }
        }

        Handles.color = prev;
        Handles.EndGUI();
    }

    private void DrawIsometricGrid(Rect canvasRect)
    {
        if (!showIsometricGrid) return;

        Handles.BeginGUI();
        Color prev = Handles.color;
        Handles.color = isometricGridColor;

        float cellSize = basePixelSize * zoom;
        float gridWidth = canvasSize * cellSize;
        float gridHeight = canvasSize * cellSize;

        for (int i = 0; i <= canvasSize; i++)
        {
            float x1 = canvasRect.x + i * cellSize;
            float y1 = canvasRect.y;
            float x2 = canvasRect.x + i * cellSize + gridHeight * Mathf.Tan(isometricGridAngle * Mathf.Deg2Rad);
            float y2 = canvasRect.y + gridHeight;
            Handles.DrawLine(new Vector3(x1, y1, 0), new Vector3(x2, y2, 0));

            x1 = canvasRect.x;
            y1 = canvasRect.y + i * cellSize;
            x2 = canvasRect.x + gridWidth;
            y2 = canvasRect.y + i * cellSize - gridWidth * Mathf.Tan(isometricGridAngle * Mathf.Deg2Rad);
            Handles.DrawLine(new Vector3(x1, y1, 0), new Vector3(x2, y2, 0));
        }

        Handles.color = prev;
        Handles.EndGUI();
    }

    private void DrawBonesOnCanvas(Rect canvasRect)
    {
        if (boneAnim.Bones == null || boneAnim.Bones.Count == 0) return;

        Handles.BeginGUI();
        var frame = boneAnim.Frames.Count > anim.CurrentFrameIndex ? boneAnim.Frames[anim.CurrentFrameIndex] : null;
        if (frame == null) { Handles.EndGUI(); return; }

        for (int i = 0; i < boneAnim.Bones.Count; i++)
        {
            var bone = boneAnim.Bones[i];
            var pose = frame.Poses.Count > i ? frame.Poses[i] : null;
            if (pose == null) continue;
            if (bone.ParentIndex >= 0 && bone.ParentIndex < boneAnim.Bones.Count)
            {
                var parentPose = frame.Poses[bone.ParentIndex];
                Vector2 parentPos = PixelToScreen(Vector2Int.RoundToInt(parentPose.Position), canvasRect);
                Vector2 childPos = PixelToScreen(Vector2Int.RoundToInt(pose.Position), canvasRect);
                Color prev = Handles.color;
                Handles.color = new Color(0.8f, 0.5f, 0.1f, 0.7f);
                Handles.DrawAAPolyLine(5f, parentPos, childPos);
                Handles.color = prev;
            }
        }

        for (int i = 0; i < boneAnim.Bones.Count; i++)
        {
            var bone = boneAnim.Bones[i];
            var pose = frame.Poses.Count > i ? frame.Poses[i] : null;
            if (pose == null) continue;

            Vector2 start = PixelToScreen(Vector2Int.RoundToInt(pose.Position), canvasRect);
            Vector2 end = start + new Vector2(Mathf.Cos(pose.Rotation), Mathf.Sin(pose.Rotation)) * bone.Length * basePixelSize * zoom;

            Color prevColor = Handles.color;
            Handles.color = (i == selectedBone) ? Color.yellow : (bone.ParentIndex >= 0 ? new Color(0.2f, 0.7f, 1f, 1f) : Color.magenta);
            Handles.DrawAAPolyLine(6f, start, end);

            Handles.color = Color.black;
            Handles.DrawSolidDisc(start, Vector3.forward, boneHandleSize * 0.7f);
            Handles.color = (i == selectedBone) ? Color.yellow : Color.white;
            Handles.DrawSolidDisc(start, Vector3.forward, boneHandleSize * 0.5f);
            Handles.color = prevColor;

            Handles.Label(start + Vector2.up * 14, bone.Name, EditorStyles.boldLabel);
        }

        Handles.EndGUI();
    }

    private void DrawWeightPaintOverlay(Rect canvasRect)
    {
        if (boneWeights == null || boneWeightsStrength == null) return;

        for (int y = 0; y < canvasSize; y++)
        {
            for (int x = 0; x < canvasSize; x++)
            {
                int b = boneWeights[x, y];
                float w = boneWeightsStrength[x, y];
                if (b < 0 || w <= 0f) continue;

                Color c = Color.Lerp(Color.clear, Color.HSVToRGB((b * 0.17f) % 1f, 0.7f, 1f), w * 0.7f);
                float px = canvasRect.x + x * basePixelSize * zoom;
                float py = canvasRect.y + (canvasSize - 1 - y) * basePixelSize * zoom;
                EditorGUI.DrawRect(new Rect(px, py, basePixelSize * zoom, basePixelSize * zoom), c);
            }
        }
    }

    private void DrawRightPanel()
    {
        GUILayout.BeginVertical(GUILayout.Width(RightPanelWidth));
        rightScroll = EditorGUILayout.BeginScrollView(rightScroll, false, false);

        // --- Color Section ---
        DrawPanelHeader("Color", uiAccent);
        EditorGUI.BeginChangeCheck();
        Color newColor = EditorGUILayout.ColorField("Current Color", currentColor, GUILayout.MaxWidth(RightPanelWidth - 32));
        if (EditorGUI.EndChangeCheck())
        {
            currentColor = newColor;
            Repaint();
        }

        GUILayout.Space(8);

        // --- Palette Section ---
        DrawPanelHeader("Palette", uiAccent);
        GUILayout.BeginVertical(GUILayout.MaxWidth(RightPanelWidth - 32));
        DrawPalettePanel();
        GUILayout.EndVertical();

        GUILayout.Space(8);

        // --- Selection Section ---
        DrawPanelHeader("Selection", uiAccent);
        GUILayout.Label("Select with Select tool (S). Copy/Paste with buttons.", EditorStyles.miniLabel, GUILayout.MaxWidth(RightPanelWidth - 32));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy", GUILayout.Width(50))) CopySelection();
        if (GUILayout.Button("Paste", GUILayout.Width(50))) PasteSelection();
        if (GUILayout.Button("Clear", GUILayout.Width(50))) ClearSelection();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("←", GUILayout.Width(24))) MoveSelection(-1, 0);
        if (GUILayout.Button("→", GUILayout.Width(24))) MoveSelection(1, 0);
        if (GUILayout.Button("↑", GUILayout.Width(24))) MoveSelection(0, 1);
        if (GUILayout.Button("↓", GUILayout.Width(24))) MoveSelection(0, -1);
        if (GUILayout.Button("Flip H", GUILayout.Width(44))) FlipSelectionH();
        if (GUILayout.Button("Flip V", GUILayout.Width(44))) FlipSelectionV();
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // --- Reference Section ---
        DrawPanelHeader("Reference", uiAccent);
        showReference = GUILayout.Toggle(showReference, "Show Reference", GUILayout.MaxWidth(RightPanelWidth - 32));
        referenceOpacity = EditorGUILayout.Slider("Opacity", referenceOpacity, 0f, 1f, GUILayout.MaxWidth(RightPanelWidth - 32));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load", GUILayout.Width(60)))
        {
            string p = EditorUtility.OpenFilePanel("Load reference", Application.dataPath, "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(p)) referenceImage = LoadTextureFromFile(p);
        }
        if (referenceImage != null && GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            referenceImage = null;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // --- Tiling Section ---
        showTilingPreview = GUILayout.Toggle(showTilingPreview, "Enable Tiling", GUILayout.MaxWidth(RightPanelWidth - 32));
        if (showTilingPreview)
        {
            tilingGridSize = EditorGUILayout.IntSlider("Grid Size", tilingGridSize, 1, 5, GUILayout.MaxWidth(RightPanelWidth - 32));
            tilingMode = (TilingMode)EditorGUILayout.EnumPopup("Tiling Mode", tilingMode, GUILayout.MaxWidth(RightPanelWidth - 32));
        }

        GUILayout.Space(8);

        // --- Isometric Grid Section ---
        showIsometricGrid = GUILayout.Toggle(showIsometricGrid, "Show Grid", GUILayout.MaxWidth(RightPanelWidth - 32));
        if (showIsometricGrid)
        {
            isometricGridAngle = EditorGUILayout.Slider("Grid Angle", isometricGridAngle, 15f, 45f, GUILayout.MaxWidth(RightPanelWidth - 32));
            isometricGridColor = EditorGUILayout.ColorField("Grid Color", isometricGridColor, GUILayout.MaxWidth(RightPanelWidth - 32));
        }

        GUILayout.Space(8);

        // --- Export / Import Section ---
        DrawPanelHeader("Export / Import", uiAccent);
        exportPreset = (ExportPreset)EditorGUILayout.EnumPopup("Export Preset", exportPreset, GUILayout.MaxWidth(RightPanelWidth - 32));
        if (exportPreset == ExportPreset.Custom)
        {
            exportPadding = EditorGUILayout.IntField("Padding", exportPadding, GUILayout.MaxWidth(RightPanelWidth - 32));
            exportTrim = EditorGUILayout.Toggle("Trim Empty", exportTrim, GUILayout.MaxWidth(RightPanelWidth - 32));
        }

        if (GUILayout.Button("Save Frame as PNG", GUILayout.MaxWidth(RightPanelWidth - 32))) DoSavePNG();
        if (GUILayout.Button("Export All Frames", GUILayout.MaxWidth(RightPanelWidth - 32))) DoExportFrames();
        if (GUILayout.Button("Export Sprite Sheet", GUILayout.MaxWidth(RightPanelWidth - 32))) ExportSpriteSheet();
        if (GUILayout.Button("Import Image to Frame", GUILayout.MaxWidth(RightPanelWidth - 32))) DoLoadImage();
        if (GUILayout.Button("Export Bone Data", GUILayout.MaxWidth(RightPanelWidth - 32))) ExportBoneData();

        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawBottomPanel()
    {
        GUILayout.BeginVertical(GUILayout.Height(300));
        GUILayout.BeginHorizontal();

        // Animation
        GUILayout.BeginVertical(GUILayout.Width(240));
        DrawPanelHeader("Animation", uiAccent);
        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Name:", GUILayout.Width(40));
        animationName = GUILayout.TextField(animationName, GUILayout.Width(150));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("FPS", GUILayout.Width(32));
        anim.FrameRate = EditorGUILayout.IntSlider(anim.FrameRate, 1, 60, GUILayout.Width(160));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<<", GUILayout.Width(30))) anim.GoToFirstFrame();
        if (GUILayout.Button("<", GUILayout.Width(30))) anim.GoToPrevFrame();
        if (GUILayout.Button(">", GUILayout.Width(30))) anim.GoToNextFrame();
        if (GUILayout.Button(">>", GUILayout.Width(30))) anim.GoToLastFrame();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(anim.IsPlaying ? "Stop" : "Play", GUILayout.Width(60))) anim.TogglePlayback();
        if (GUILayout.Button("Add Frame", GUILayout.Width(80))) anim.AddFrame();
        if (GUILayout.Button("Delete Frame", GUILayout.Width(80))) anim.RemoveCurrentFrame();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy Frame", GUILayout.Width(80))) anim.CopyFrame();
        if (GUILayout.Button("Paste Frame", GUILayout.Width(80))) anim.PasteFrame();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Loop:", GUILayout.Width(40));
        loopStart = EditorGUILayout.IntField(loopStart, GUILayout.Width(30));
        GUILayout.Label("to", GUILayout.Width(20));
        loopEnd = EditorGUILayout.IntField(loopEnd, GUILayout.Width(30));
        pingPong = GUILayout.Toggle(pingPong, "Ping Pong", GUILayout.Width(80));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        onionEnabled = GUILayout.Toggle(onionEnabled, "Onion Skin", GUILayout.Width(80));
        frameBlending = GUILayout.Toggle(frameBlending, "Frame Blend", GUILayout.Width(80));
        GUILayout.EndHorizontal();

        if (onionEnabled)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Prev:", GUILayout.Width(40));
            prevOnionOpacity = EditorGUILayout.Slider(prevOnionOpacity, 0f, 1f, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Next:", GUILayout.Width(40));
            nextOnionOpacity = EditorGUILayout.Slider(nextOnionOpacity, 0f, 1f, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Range:", GUILayout.Width(40));
            onionPrevRangeUI = EditorGUILayout.IntField(onionPrevRangeUI, GUILayout.Width(30));
            onionNextRangeUI = EditorGUILayout.IntField(onionNextRangeUI, GUILayout.Width(30));
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();

        // Timeline
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        DrawPanelHeader("Timeline", uiAccent);
        timelineScroll = GUILayout.BeginScrollView(timelineScroll, false, false, GUILayout.Height(100));

        GUILayout.BeginHorizontal();
        for (int i = 0; i < anim.FrameCount; i++)
        {
            bool isCurrent = i == anim.CurrentFrameIndex;
            GUIStyle style = isCurrent ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
            if (GUILayout.Button($"Frame {i + 1}", style, GUILayout.Width(60), GUILayout.Height(60)))
            {
                anim.GoToFrame(i);
            }
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        // Bone Animation
        GUILayout.BeginVertical();
        DrawPanelHeader("Bone Animation", uiAccent);
        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        weightPaintMode = GUILayout.Toggle(weightPaintMode, "Weight Paint", GUILayout.Width(90));
        if (weightPaintMode)
        {
            weightPaintBone = EditorGUILayout.IntField("Bone ID", weightPaintBone, GUILayout.Width(60));
            weightPaintStrength = EditorGUILayout.Slider("Strength", weightPaintStrength, 0f, 1f, GUILayout.Width(120));
        }
        else
        {
            if (GUILayout.Button("Add Bone", GUILayout.Width(70))) isAddingBone = true;
            if (GUILayout.Button("Delete Bone", GUILayout.Width(80))) DeleteSelectedBone();
            if (GUILayout.Button("Clear All", GUILayout.Width(70))) ClearAllBones();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Bones:", GUILayout.Width(40));
        for (int i = 0; i < boneAnim.Bones.Count; i++)
        {
            var bone = boneAnim.Bones[i];
            bool isSelected = i == selectedBone;
            if (GUILayout.Toggle(isSelected, bone.Name, "Button", GUILayout.Width(60)))
            {
                selectedBone = i;
            }
        }
        GUILayout.EndHorizontal();

        if (selectedBone >= 0 && selectedBone < boneAnim.Bones.Count)
        {
            var bone = boneAnim.Bones[selectedBone];
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(40));
            bone.Name = GUILayout.TextField(bone.Name, GUILayout.Width(100));
            GUILayout.Label("Length:", GUILayout.Width(45));
            bone.Length = EditorGUILayout.FloatField(bone.Length, GUILayout.Width(40));
            GUILayout.Label("Parent:", GUILayout.Width(45));
            bone.ParentIndex = EditorGUILayout.IntField(bone.ParentIndex, GUILayout.Width(30));
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
        GUILayout.EndVertical();
    }

    private void DrawPanelHeader(string title, Color color)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = color;
        GUILayout.Label(title, style, GUILayout.MaxWidth(LeftPanelWidth - 32));
    }

    private void DrawCheckerboard(Rect canvasRect)
    {
        float pixelSize = basePixelSize * zoom;
        int checkerSize = Mathf.Max(1, Mathf.RoundToInt(pixelSize / 2f));

        for (int y = 0; y < canvasSize; y++)
        {
            for (int x = 0; x < canvasSize; x++)
            {
                bool isChecker = ((x / checkerSize) + (y / checkerSize)) % 2 == 0;
                if (isChecker)
                {
                    float px = canvasRect.x + x * pixelSize;
                    float py = canvasRect.y + (canvasSize - 1 - y) * pixelSize;
                    EditorGUI.DrawRect(new Rect(px, py, pixelSize, pixelSize), new Color(0.2f, 0.2f, 0.2f, 1f));
                }
            }
        }
    }

    private void DrawGridClamped(Rect canvasRect)
    {
        float pixelSize = basePixelSize * zoom;
        if (pixelSize < 4f) return;

        Handles.BeginGUI();
        Color prev = Handles.color;
        Handles.color = new Color(1f, 1f, 1f, 0.15f);

        for (int i = 0; i <= canvasSize; i++)
        {
            float x = canvasRect.x + i * pixelSize;
            float y1 = canvasRect.y;
            float y2 = canvasRect.y + canvasRect.height;
            Handles.DrawLine(new Vector3(x, y1, 0), new Vector3(x, y2, 0));

            float y = canvasRect.y + i * pixelSize;
            float x1 = canvasRect.x;
            float x2 = canvasRect.x + canvasRect.width;
            Handles.DrawLine(new Vector3(x1, y, 0), new Vector3(x2, y, 0));
        }

        Handles.color = prev;
        Handles.EndGUI();
    }

    private void DrawShapePreview(Rect canvasRect)
    {
        if (shapeStart.x < 0 || shapeStart.y < 0 || shapeCurrent.x < 0 || shapeCurrent.y < 0) return;

        Vector2Int start = new Vector2Int(Mathf.Min(shapeStart.x, shapeCurrent.x), Mathf.Min(shapeStart.y, shapeCurrent.y));
        Vector2Int end = new Vector2Int(Mathf.Max(shapeStart.x, shapeCurrent.x), Mathf.Max(shapeStart.y, shapeCurrent.y));

        Handles.BeginGUI();
        Color prev = Handles.color;
        Handles.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0.5f);

        if (currentTool == Tool.Line)
        {
            Vector2 p1 = PixelToScreen(shapeStart, canvasRect) + new Vector2(basePixelSize * zoom / 2, basePixelSize * zoom / 2);
            Vector2 p2 = PixelToScreen(shapeCurrent, canvasRect) + new Vector2(basePixelSize * zoom / 2, basePixelSize * zoom / 2);
            Handles.DrawLine(p1, p2);
        }
        else if (currentTool == Tool.Circle)
        {
            int radius = Mathf.RoundToInt(Vector2Int.Distance(shapeStart, shapeCurrent));
            Vector2 center = PixelToScreen(shapeStart, canvasRect) + new Vector2(basePixelSize * zoom / 2, basePixelSize * zoom / 2);
            Handles.DrawWireDisc(center, Vector3.forward, radius * basePixelSize * zoom);
        }
        else if (currentTool == Tool.Square)
        {
            Rect rect = new Rect(
                canvasRect.x + start.x * basePixelSize * zoom,
                canvasRect.y + (canvasSize - 1 - end.y) * basePixelSize * zoom,
                (end.x - start.x + 1) * basePixelSize * zoom,
                (end.y - start.y + 1) * basePixelSize * zoom
            );
            Handles.DrawSolidRectangleWithOutline(rect, new Color(0, 0, 0, 0), new Color(currentColor.r, currentColor.g, currentColor.b, 0.8f));
        }

        Handles.color = prev;
        Handles.EndGUI();
    }

    private void DrawSelectionOverlay(Rect canvasRect)
    {
        if (currentSelection.width <= 0 || currentSelection.height <= 0) return;

        Rect rect = new Rect(
            canvasRect.x + currentSelection.x * basePixelSize * zoom,
            canvasRect.y + (canvasSize - 1 - currentSelection.y - currentSelection.height + 1) * basePixelSize * zoom,
            currentSelection.width * basePixelSize * zoom,
            currentSelection.height * basePixelSize * zoom
        );

        Handles.BeginGUI();
        Color prev = Handles.color;
        Handles.color = new Color(0.2f, 0.6f, 1f, 0.2f);
        Handles.DrawSolidRectangleWithOutline(rect, new Color(0.2f, 0.6f, 1f, 0.1f), new Color(0.2f, 0.6f, 1f, 0.8f));
        Handles.color = prev;
        Handles.EndGUI();
    }

    private void DrawPalettePanel()
    {
        paletteScroll = GUILayout.BeginScrollView(paletteScroll, false, false, GUILayout.Height(120));

        GUILayout.BeginHorizontal();
        GUILayout.Label("Current Palette", EditorStyles.boldLabel, GUILayout.Width(100));
        paletteLocked = GUILayout.Toggle(paletteLocked, "Locked", GUILayout.Width(50));
        GUILayout.EndHorizontal();

        int cols = Mathf.FloorToInt((RightPanelWidth - 32) / 24f);
        int rows = Mathf.CeilToInt(palette.Count / (float)cols);

        for (int y = 0; y < rows; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < cols; x++)
            {
                int i = y * cols + x;
                if (i >= palette.Count) break;

                Color c = palette[i];
                Rect r = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
                EditorGUI.DrawRect(r, c);

                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0)
                    {
                        currentColor = c;
                        Repaint();
                    }
                    else if (Event.current.button == 1)
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Edit"), false, () => EditPaletteColor(i));
                        menu.AddItem(new GUIContent("Remove"), false, () => RemovePaletteColor(i));
                        menu.ShowAsContext();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Current", GUILayout.Width(90)))
        {
            if (!palette.Contains(currentColor))
            {
                palette.Add(currentColor);
                hasUnsavedChanges = true;
            }
        }
        if (GUILayout.Button("Sort", GUILayout.Width(50)))
        {
            palette = palette.OrderBy(c => c.r * 0.3f + c.g * 0.59f + c.b * 0.11f).ToList();
            hasUnsavedChanges = true;
        }
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            palette.Clear();
            hasUnsavedChanges = true;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Width(50)))
        {
            string path = EditorUtility.SaveFilePanel("Save Palette", Application.dataPath, "palette", "pal");
            if (!string.IsNullOrEmpty(path)) SavePalette(path);
        }
        if (GUILayout.Button("Load", GUILayout.Width(50)))
        {
            string path = EditorUtility.OpenFilePanel("Load Palette", Application.dataPath, "pal");
            if (!string.IsNullOrEmpty(path)) LoadPalette(path);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawBrushPanel()
    {
        GUILayout.BeginVertical(GUILayout.MaxWidth(LeftPanelWidth - 32));

        for (int i = 0; i < customBrushes.Count; i++)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(selectedCustomBrush == i, customBrushes[i].Name, "Button", GUILayout.Width(LeftPanelWidth - 80)))
            {
                selectedCustomBrush = i;
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                customBrushes.RemoveAt(i);
                selectedCustomBrush = -1;
                i--;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(8);

        newBrushName = GUILayout.TextField(newBrushName, GUILayout.Width(LeftPanelWidth - 32));
        if (GUILayout.Button("Create New Brush", GUILayout.Width(LeftPanelWidth - 32)))
        {
            if (!string.IsNullOrEmpty(newBrushName))
            {
                var brush = new CustomBrush(newBrushName, brushSize, currentBrush, brushTipShape);
                customBrushes.Add(brush);
                newBrushName = "";
            }
        }

        if (selectedCustomBrush >= 0 && selectedCustomBrush < customBrushes.Count)
        {
            var brush = customBrushes[selectedCustomBrush];
            if (GUILayout.Button("Apply Brush", GUILayout.Width(LeftPanelWidth - 32)))
            {
                brushSize = brush.Size;
                currentBrush = brush.BrushType;
                brushTipShape = (BrushTipShape)brush.TipShape;
            }
        }

        GUILayout.EndVertical();
    }
}
#endif
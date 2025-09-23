#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public partial class PixelArtStudioPro
{
    // Canvas properties
    private int canvasSize = 32;
    private float basePixelSize = 8f;
    private float zoom = 1f;
    private Vector2 pan = Vector2.zero;

    private void ShowNewCanvasDialog()
    {
        int newSize = EditorGUILayout.IntField("Canvas Size", canvasSize);
        if (newSize < 8) newSize = 8;
        if (newSize > 512) newSize = 512;

        if (EditorUtility.DisplayDialog("New Canvas", $"Create a new {newSize}x{newSize} canvas?", "Yes", "No"))
        {
            canvasSize = newSize;
            anim = new AnimationManager(canvasSize);
            anim.EnsureAtLeastOneFrame();
            core = new PixelArtCore();
            brushSys = new BrushSystem();
            pan = Vector2.zero;
            zoom = 1f;
            hasUnsavedChanges = false;
            Repaint();
        }
    }

    private void DoSavePNG()
    {
        string path = EditorUtility.SaveFilePanel("Save PNG", Application.dataPath, "pixel_art", "png");
        if (!string.IsNullOrEmpty(path))
        {
            Texture2D tex = anim.GetCompositeTexture(1, false, false, 0f, false, 0f, false, anim.CurrentFrameIndex);
            if (tex != null)
            {
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Saved", $"Image saved to {path}", "OK");
            }
        }
    }

    private void DoLoadImage()
    {
        string path = EditorUtility.OpenFilePanel("Load Image", Application.dataPath, "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
        {
            Texture2D tex = LoadTextureFromFile(path);
            if (tex != null)
            {
                // Resize to fit canvas if needed
                if (tex.width != canvasSize || tex.height != canvasSize)
                {
                    Texture2D resized = ResizeTexture(tex, canvasSize, canvasSize);
                    anim.CurrentFrame.SetPixels(resized.GetPixels());
                    anim.CurrentFrame.ApplyAllTextures();
                }
                else
                {
                    anim.CurrentFrame.SetPixels(tex.GetPixels());
                    anim.CurrentFrame.ApplyAllTextures();
                }
                anim.MarkDirty();
                hasUnsavedChanges = true;
                Repaint();
            }
        }
    }

    private Texture2D LoadTextureFromFile(string path)
    {
        if (!File.Exists(path)) return null;

        byte[] data = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(data))
        {
            tex.filterMode = FilterMode.Point;
            return tex;
        }
        return null;
    }

    private Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(width, height);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    private void DoSaveProject()
    {
        string path = EditorUtility.SaveFilePanel("Save Project", Application.dataPath, "pixel_project", "paproj");
        if (!string.IsNullOrEmpty(path))
        {
            SaveProject(path);
            hasUnsavedChanges = false;
        }
    }

    private void DoLoadProject()
    {
        string path = EditorUtility.OpenFilePanel("Load Project", Application.dataPath, "paproj");
        if (!string.IsNullOrEmpty(path))
        {
            LoadProject(path);
            Repaint();
        }
    }

    private void SaveProject(string path)
    {
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // Header
                writer.Write("PAPROJ");
                writer.Write(1); // Version

                // Canvas info
                writer.Write(canvasSize);

                // Animation data
                writer.Write(anim.FrameCount);
                for (int i = 0; i < anim.FrameCount; i++)
                {
                    Frame frame = anim.GetFrame(i);
                    Color[] pixels = frame.GetPixels();
                    writer.Write(pixels.Length);
                    for (int j = 0; j < pixels.Length; j++)
                    {
                        writer.Write(pixels[j].r);
                        writer.Write(pixels[j].g);
                        writer.Write(pixels[j].b);
                        writer.Write(pixels[j].a);
                    }
                }

                // Palette
                writer.Write(palette.Count);
                foreach (Color c in palette)
                {
                    writer.Write(c.r);
                    writer.Write(c.g);
                    writer.Write(c.b);
                    writer.Write(c.a);
                }

                // Bone animation data
                writer.Write(boneAnim.Bones.Count);
                foreach (var bone in boneAnim.Bones)
                {
                    writer.Write(bone.Name);
                    writer.Write(bone.ParentIndex);
                    writer.Write(bone.Length);
                    writer.Write(bone.Position.x);
                    writer.Write(bone.Position.y);
                    writer.Write(bone.Rotation);
                }

                writer.Write(boneAnim.Frames.Count);
                foreach (var frame in boneAnim.Frames)
                {
                    writer.Write(frame.Poses.Count);
                    foreach (var pose in frame.Poses)
                    {
                        writer.Write(pose.Position.x);
                        writer.Write(pose.Position.y);
                        writer.Write(pose.Rotation);
                    }
                }

                // Custom brushes
                writer.Write(customBrushes.Count);
                foreach (var brush in customBrushes)
                {
                    writer.Write(brush.Name);
                    writer.Write(brush.Size);
                    writer.Write((int)brush.BrushType);
                    writer.Write((int)brush.TipShape);
                }

                // Animation settings
                writer.Write(anim.FrameRate);
                writer.Write(animationName);
                writer.Write(loopStart);
                writer.Write(loopEnd);
                writer.Write(pingPong);
                writer.Write(onionEnabled);
                writer.Write(prevOnionOpacity);
                writer.Write(nextOnionOpacity);
                writer.Write(onionPrevRange);
                writer.Write(onionNextRange);
                writer.Write(frameBlending);

                // Tiling settings
                writer.Write(showTilingPreview);
                writer.Write(tilingGridSize);
                writer.Write((int)tilingMode);

                // Isometric grid settings
                writer.Write(showIsometricGrid);
                writer.Write(isometricGridAngle);
                writer.Write(isometricGridColor.r);
                writer.Write(isometricGridColor.g);
                writer.Write(isometricGridColor.b);
                writer.Write(isometricGridColor.a);

                // Export settings
                writer.Write((int)exportPreset);
                writer.Write(exportPadding);
                writer.Write(exportTrim);

                // Reference image (if any)
                if (referenceImage != null)
                {
                    byte[] refData = referenceImage.EncodeToPNG();
                    writer.Write(refData.Length);
                    writer.Write(refData);
                }
                else
                {
                    writer.Write(0);
                }
            }

            EditorUtility.DisplayDialog("Saved", $"Project saved to {path}", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to save project: {e.Message}", "OK");
        }
    }

    private void LoadProject(string path)
    {
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // Header
                string header = reader.ReadString();
                if (header != "PAPROJ")
                {
                    EditorUtility.DisplayDialog("Error", "Invalid project file", "OK");
                    return;
                }

                int version = reader.ReadInt32();

                // Canvas info
                canvasSize = reader.ReadInt32();
                anim = new AnimationManager(canvasSize);

                // Animation data
                int frameCount = reader.ReadInt32();
                for (int i = 0; i < frameCount; i++)
                {
                    Frame frame = i == 0 ? anim.CurrentFrame : anim.AddFrame();
                    int pixelCount = reader.ReadInt32();
                    Color[] pixels = new Color[pixelCount];
                    for (int j = 0; j < pixelCount; j++)
                    {
                        float r = reader.ReadSingle();
                        float g = reader.ReadSingle();
                        float b = reader.ReadSingle();
                        float a = reader.ReadSingle();
                        pixels[j] = new Color(r, g, b, a);
                    }
                    frame.SetPixels(pixels);
                    frame.ApplyAllTextures();
                }

                // Palette
                palette.Clear();
                int paletteCount = reader.ReadInt32();
                for (int i = 0; i < paletteCount; i++)
                {
                    float r = reader.ReadSingle();
                    float g = reader.ReadSingle();
                    float b = reader.ReadSingle();
                    float a = reader.ReadSingle();
                    palette.Add(new Color(r, g, b, a));
                }

                // Bone animation data
                boneAnim.Bones.Clear();
                int boneCount = reader.ReadInt32();
                for (int i = 0; i < boneCount; i++)
                {
                    string name = reader.ReadString();
                    int parentIndex = reader.ReadInt32();
                    float length = reader.ReadSingle();
                    float posX = reader.ReadSingle();
                    float posY = reader.ReadSingle();
                    float rotation = reader.ReadSingle();
                    boneAnim.Bones.Add(new Bone
                    {
                        Name = name,
                        ParentIndex = parentIndex,
                        Length = length,
                        Position = new Vector2(posX, posY),
                        Rotation = rotation
                    });
                }

                boneAnim.Frames.Clear();
                int framePoseCount = reader.ReadInt32();
                for (int i = 0; i < framePoseCount; i++)
                {
                    var frame = new BoneAnimationFrame();
                    int poseCount = reader.ReadInt32();
                    for (int j = 0; j < poseCount; j++)
                    {
                        float posX = reader.ReadSingle();
                        float posY = reader.ReadSingle();
                        float rotation = reader.ReadSingle();
                        frame.Poses.Add(new BonePose(new Vector2(posX, posY), rotation));
                    }
                    boneAnim.Frames.Add(frame);
                }

                // Custom brushes
                customBrushes.Clear();
                int brushCount = reader.ReadInt32();
                for (int i = 0; i < brushCount; i++)
                {
                    string name = reader.ReadString();
                    int size = reader.ReadInt32();
                    BrushSystem.BrushType type = (BrushSystem.BrushType)reader.ReadInt32();
                    BrushTipShape tipShape = (BrushTipShape)reader.ReadInt32();
                    customBrushes.Add(new CustomBrush(name, size, type, tipShape));
                }

                // Animation settings
                anim.FrameRate = reader.ReadInt32();
                animationName = reader.ReadString();
                loopStart = reader.ReadInt32();
                loopEnd = reader.ReadInt32();
                pingPong = reader.ReadBoolean();
                onionEnabled = reader.ReadBoolean();
                prevOnionOpacity = reader.ReadSingle();
                nextOnionOpacity = reader.ReadSingle();
                onionPrevRange = reader.ReadInt32();
                onionNextRange = reader.ReadInt32();
                frameBlending = reader.ReadBoolean();

                // Tiling settings
                showTilingPreview = reader.ReadBoolean();
                tilingGridSize = reader.ReadInt32();
                tilingMode = (TilingMode)reader.ReadInt32();

                // Isometric grid settings
                showIsometricGrid = reader.ReadBoolean();
                isometricGridAngle = reader.ReadSingle();
                isometricGridColor = new Color(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );

                // Export settings
                exportPreset = (ExportPreset)reader.ReadInt32();
                exportPadding = reader.ReadInt32();
                exportTrim = reader.ReadBoolean();

                // Reference image
                int refDataLength = reader.ReadInt32();
                if (refDataLength > 0)
                {
                    byte[] refData = reader.ReadBytes(refDataLength);
                    referenceImage = new Texture2D(2, 2);
                    referenceImage.LoadImage(refData);
                    referenceImage.filterMode = FilterMode.Point;
                }
                else
                {
                    referenceImage = null;
                }
            }

            anim.GoToFrame(0);
            hasUnsavedChanges = false;
            EditorUtility.DisplayDialog("Loaded", "Project loaded successfully", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to load project: {e.Message}", "OK");
        }
    }

    private void TryAutoSave(bool force = false)
    {
        if (!autosaveEnabled && !force) return;

        try
        {
            using (FileStream fs = new FileStream(autosavePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // Save minimal data for autosave
                writer.Write(canvasSize);
                writer.Write(anim.FrameCount);
                for (int i = 0; i < anim.FrameCount; i++)
                {
                    Frame frame = anim.GetFrame(i);
                    Color[] pixels = frame.GetPixels();
                    writer.Write(pixels.Length);
                    for (int j = 0; j < pixels.Length; j++)
                    {
                        writer.Write(pixels[j].r);
                        writer.Write(pixels[j].g);
                        writer.Write(pixels[j].b);
                        writer.Write(pixels[j].a);
                    }
                }
            }

            if (force)
                EditorUtility.DisplayDialog("Autosaved", $"Project autosaved to {autosavePath}", "OK");
        }
        catch (Exception e)
        {
            Debug.LogError($"Autosave failed: {e.Message}");
        }
    }

    private void DoExportFrames()
    {
        string folder = EditorUtility.SaveFolderPanel("Export Frames", Application.dataPath, "");
        if (!string.IsNullOrEmpty(folder))
        {
            for (int i = 0; i < anim.FrameCount; i++)
            {
                Texture2D tex = anim.GetCompositeTexture(1, false, false, 0f, false, 0f, false, i);
                if (tex != null)
                {
                    byte[] bytes = tex.EncodeToPNG();
                    string path = Path.Combine(folder, $"frame_{i:000}.png");
                    File.WriteAllBytes(path, bytes);
                }
            }
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Exported", $"Exported {anim.FrameCount} frames to {folder}", "OK");
        }
    }

    private void ExportSpriteSheet()
    {
        string path = EditorUtility.SaveFilePanel("Export Sprite Sheet", Application.dataPath, "sprite_sheet", "png");
        if (!string.IsNullOrEmpty(path))
        {
            int cols = Mathf.CeilToInt(Mathf.Sqrt(anim.FrameCount));
            int rows = Mathf.CeilToInt(anim.FrameCount / (float)cols);
            int sheetWidth = cols * canvasSize;
            int sheetHeight = rows * canvasSize;

            Texture2D sheet = new Texture2D(sheetWidth, sheetHeight);
            Color[] clear = new Color[sheetWidth * sheetHeight];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            sheet.SetPixels(clear);

            for (int i = 0; i < anim.FrameCount; i++)
            {
                Texture2D frame = anim.GetCompositeTexture(1, false, false, 0f, false, 0f, false, i);
                int x = (i % cols) * canvasSize;
                int y = (rows - 1 - i / cols) * canvasSize;
                sheet.SetPixels(x, y, canvasSize, canvasSize, frame.GetPixels());
            }

            sheet.Apply();
            byte[] bytes = sheet.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Exported", $"Sprite sheet exported to {path}", "OK");
        }
    }

    private void ExportBoneData()
    {
        string path = EditorUtility.SaveFilePanel("Export Bone Data", Application.dataPath, "bone_data", "json");
        if (!string.IsNullOrEmpty(path))
        {
            string json = JsonUtility.ToJson(boneAnim, true);
            File.WriteAllText(path, json);
            EditorUtility.DisplayDialog("Exported", $"Bone data exported to {path}", "OK");
        }
    }

    private void LoadPresetPalettes()
    {
        // Default palette if empty
        if (palette.Count == 0)
        {
            palette.AddRange(new Color[] {
                Color.black, Color.white, Color.red, Color.green, Color.blue,
                Color.yellow, Color.cyan, Color.magenta, new Color(0.5f, 0.5f, 0.5f, 1f)
            });
        }
    }

    private void EditPaletteColor(int index)
    {
        Color newColor = EditorGUILayout.ColorField("Edit Palette Color", palette[index]);
        palette[index] = newColor;
        hasUnsavedChanges = true;
        Repaint();
    }

    private void RemovePaletteColor(int index)
    {
        palette.RemoveAt(index);
        hasUnsavedChanges = true;
        Repaint();
    }

    private void SavePalette(string path)
    {
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(palette.Count);
                foreach (Color c in palette)
                {
                    writer.Write(c.r);
                    writer.Write(c.g);
                    writer.Write(c.b);
                    writer.Write(c.a);
                }
            }
            paletteFilePath = path;
            EditorUtility.DisplayDialog("Saved", $"Palette saved to {path}", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to save palette: {e.Message}", "OK");
        }
    }

    private void LoadPalette(string path)
    {
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                palette.Clear();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    float r = reader.ReadSingle();
                    float g = reader.ReadSingle();
                    float b = reader.ReadSingle();
                    float a = reader.ReadSingle();
                    palette.Add(new Color(r, g, b, a));
                }
            }
            paletteFilePath = path;
            Repaint();
            EditorUtility.DisplayDialog("Loaded", $"Palette loaded from {path}", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to load palette: {e.Message}", "OK");
        }
    }

    private Vector2Int ScreenToPixel(Vector2 screenPos, Rect canvasRect)
    {
        float pixelSize = basePixelSize * zoom;
        int x = Mathf.FloorToInt((screenPos.x - canvasRect.x) / pixelSize);
        int y = canvasSize - 1 - Mathf.FloorToInt((screenPos.y - canvasRect.y) / pixelSize);
        return new Vector2Int(Mathf.Clamp(x, 0, canvasSize - 1), Mathf.Clamp(y, 0, canvasSize - 1));
    }

    private Vector2 PixelToScreen(Vector2Int pixel, Rect canvasRect)
    {
        float pixelSize = basePixelSize * zoom;
        float x = canvasRect.x + pixel.x * pixelSize;
        float y = canvasRect.y + (canvasSize - 1 - pixel.y) * pixelSize;
        return new Vector2(x, y);
    }

    private Vector2 PixelToScreen(Vector2 pixel, Rect canvasRect)
    {
        float pixelSize = basePixelSize * zoom;
        float x = canvasRect.x + pixel.x * pixelSize;
        float y = canvasRect.y + (canvasSize - 1 - pixel.y) * pixelSize;
        return new Vector2(x, y);
    }

    private void HandleHotkeys(Event e)
    {
        if (e.type == EventType.KeyDown)
        {
            // Tool selection
            if (e.keyCode == KeyCode.B) { currentTool = Tool.Pencil; e.Use(); }
            if (e.keyCode == KeyCode.E) { currentTool = Tool.Eraser; e.Use(); }
            if (e.keyCode == KeyCode.F) { currentTool = Tool.Fill; e.Use(); }
            if (e.keyCode == KeyCode.C) { currentTool = Tool.Circle; e.Use(); }
            if (e.keyCode == KeyCode.R) { currentTool = Tool.Square; e.Use(); }
            if (e.keyCode == KeyCode.L) { currentTool = Tool.Line; e.Use(); }
            if (e.keyCode == KeyCode.G) { currentTool = Tool.Gradient; e.Use(); }
            if (e.keyCode == KeyCode.S) { currentTool = Tool.Select; e.Use(); }
            if (e.keyCode == KeyCode.M) { currentTool = Tool.MagicWand; e.Use(); }

            // Brush size
            if (e.keyCode == KeyCode.Equals || e.keyCode == KeyCode.Plus) { brushSize = Mathf.Min(brushSize + 1, 16); e.Use(); }
            if (e.keyCode == KeyCode.Minus) { brushSize = Mathf.Max(brushSize - 1, 1); e.Use(); }

            // Zoom
            if (e.keyCode == KeyCode.Z && e.control) { zoom = Mathf.Min(zoom + 0.1f, 5f); e.Use(); }
            if (e.keyCode == KeyCode.X && e.control) { zoom = Mathf.Max(zoom - 0.1f, 0.1f); e.Use(); }

            // Frame navigation
            if (e.keyCode == KeyCode.Comma || e.keyCode == KeyCode.LeftArrow) { anim.GoToPrevFrame(); e.Use(); }
            if (e.keyCode == KeyCode.Period || e.keyCode == KeyCode.RightArrow) { anim.GoToNextFrame(); e.Use(); }

            // Playback
            if (e.keyCode == KeyCode.Space) { anim.TogglePlayback(); e.Use(); }

            // Undo/Redo
            if (e.keyCode == KeyCode.Z && e.control) { LayerUndo(); e.Use(); }
            if (e.keyCode == KeyCode.Y && e.control) { LayerRedo(); e.Use(); }

            // Copy/Paste
            if (e.keyCode == KeyCode.C && e.control) { CopySelection(); e.Use(); }
            if (e.keyCode == KeyCode.V && e.control) { PasteSelection(); e.Use(); }

            // Deselect
            if (e.keyCode == KeyCode.Escape) { ClearSelection(); e.Use(); }

            // Toggle symmetry
            if (e.keyCode == KeyCode.T) { symmetryX = !symmetryX; e.Use(); }
        }
    }
}
#endif
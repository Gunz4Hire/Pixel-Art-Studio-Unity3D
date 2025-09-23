#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

// Frame class representing a single animation frame
[System.Serializable]
public class Frame
{
    public List<Layer> Layers = new List<Layer>();
    public int ActiveLayerIndex = 0;
    private Texture2D compositeTexture;
    private bool isDirty = true;
    public Frame(int canvasSize)
    {
        Layers.Add(new Layer(canvasSize, "Layer 1"));
        compositeTexture = new Texture2D(canvasSize, canvasSize, TextureFormat.RGBA32, false);
        compositeTexture.filterMode = FilterMode.Point;
        compositeTexture.wrapMode = TextureWrapMode.Clamp;
    }

    public Color GetPixel(int x, int y)
    {
        return Layers[ActiveLayerIndex].GetPixel(x, y);
    }

    public void SetPixel(int x, int y, Color color)
    {
        Layers[ActiveLayerIndex].SetPixel(x, y, color);
        isDirty = true;
    }

    public void SetPixels(Color[] pixels)
    {
        Layers[ActiveLayerIndex].SetPixels(pixels);
        isDirty = true;
    }

    public Color[] GetPixels()
    {
        return Layers[ActiveLayerIndex].GetPixels();
    }

    public void Clear()
    {
        Layers[ActiveLayerIndex].Clear();
        isDirty = true;
    }

    public void ApplyAllTextures()
    {
        foreach (var layer in Layers)
        {
            layer.ApplyTexture();
        }
        isDirty = true;
    }

    public Texture2D GetCompositeTexture(int pixelScale = 1, bool includeOnionSkin = false,
                                       bool includePrev = false, float prevOpacity = 0.25f,
                                       bool includeNext = false, float nextOpacity = 0.25f,
                                       bool frameBlending = false, int frameIndex = -1)
    {
        if (isDirty || compositeTexture == null)
        {
            RebuildCompositeTexture();
        }
        return compositeTexture;
    }

    private void RebuildCompositeTexture()
    {
        int size = Layers[0].Size;
        Color[] compositePixels = new Color[size * size];

        // Start with transparent background
        for (int i = 0; i < compositePixels.Length; i++)
            compositePixels[i] = Color.clear;

        // Blend all visible layers from bottom to top
        foreach (var layer in Layers)
        {
            if (!layer.Visible) continue;

            Color[] layerPixels = layer.GetPixels();
            for (int i = 0; i < compositePixels.Length; i++)
            {
                compositePixels[i] = BlendColors(compositePixels[i], layerPixels[i]);
            }
        }

        if (compositeTexture == null || compositeTexture.width != size)
        {
            compositeTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            compositeTexture.filterMode = FilterMode.Point;
            compositeTexture.wrapMode = TextureWrapMode.Clamp;
        }

        compositeTexture.SetPixels(compositePixels);
        compositeTexture.Apply();
        isDirty = false;
    }

    private Color BlendColors(Color bottom, Color top)
    {
        // Standard alpha blending
        float alpha = top.a + bottom.a * (1 - top.a);
        if (alpha <= 0) return Color.clear;

        Color result = new Color(
            (top.r * top.a + bottom.r * bottom.a * (1 - top.a)) / alpha,
            (top.g * top.a + bottom.g * bottom.a * (1 - top.a)) / alpha,
            (top.b * top.a + bottom.b * bottom.a * (1 - top.a)) / alpha,
            alpha
        );

        return result;
    }
}

// Layer class representing a single layer in a frame
[System.Serializable]
public class Layer
{
    public string Name = "Layer";
    public bool Visible = true;
    public float Opacity = 1.0f;
    public int Size { get; private set; }

    private Color[] pixels;
    private Texture2D texture;
    private bool isDirty = true;

    public Layer(int size, string name = "Layer")
    {
        Size = size;
        Name = name;
        pixels = new Color[size * size];
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Clear();
    }

    public Color GetPixel(int x, int y)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
            return Color.clear;
        return pixels[y * Size + x];
    }

    public void SetPixel(int x, int y, Color color)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
            return;
        pixels[y * Size + x] = color;
        isDirty = true;
    }

    public void SetPixels(Color[] newPixels)
    {
        if (newPixels.Length != pixels.Length)
            return;
        Array.Copy(newPixels, pixels, pixels.Length);
        isDirty = true;
    }

    public Color[] GetPixels()
    {
        return pixels.ToArray();
    }

    public void Clear()
    {
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;
        isDirty = true;
    }

    public void ApplyTexture()
    {
        if (isDirty)
        {
            texture.SetPixels(pixels);
            texture.Apply();
            isDirty = false;
        }
    }

    public Texture2D GetTexture()
    {
        ApplyTexture();
        return texture;
    }
}

// Animation manager class
[System.Serializable]
public class AnimationManager
{
    public int FrameRate = 12;
    public bool IsPlaying = false;
    private List<Frame> frames = new List<Frame>();
    private int currentFrameIndex = 0;
    private double lastFrameTime = 0;
    private int canvasSize;

    // Copy/paste buffer
    private Frame copiedFrame = null;

    public AnimationManager(int canvasSize)
    {
        this.canvasSize = canvasSize;
        frames.Add(new Frame(canvasSize));
    }

    public int FrameCount => frames.Count;
    public int CurrentFrameIndex => currentFrameIndex;
    public Frame CurrentFrame => frames[currentFrameIndex];

    public Frame GetFrame(int index)
    {
        if (index < 0 || index >= frames.Count)
            return null;
        return frames[index];
    }

    public void GoToFrame(int index)
    {
        if (index < 0 || index >= frames.Count)
            return;
        currentFrameIndex = index;
    }

    public void GoToFirstFrame() => GoToFrame(0);
    public void GoToLastFrame() => GoToFrame(frames.Count - 1);

    public void GoToPrevFrame()
    {
        int newIndex = (currentFrameIndex - 1 + frames.Count) % frames.Count;
        GoToFrame(newIndex);
    }

    public void GoToNextFrame()
    {
        int newIndex = (currentFrameIndex + 1) % frames.Count;
        GoToFrame(newIndex);
    }

    public Frame AddFrame()
    {
        Frame newFrame = new Frame(canvasSize);
        frames.Insert(currentFrameIndex + 1, newFrame);
        currentFrameIndex++;
        return newFrame;
    }

    public void RemoveCurrentFrame()
    {
        if (frames.Count <= 1) return;
        frames.RemoveAt(currentFrameIndex);
        if (currentFrameIndex >= frames.Count)
            currentFrameIndex = frames.Count - 1;
    }

    public void EnsureAtLeastOneFrame()
    {
        if (frames.Count == 0)
            frames.Add(new Frame(canvasSize));
    }

    public void TogglePlayback()
    {
        IsPlaying = !IsPlaying;
        lastFrameTime = Time.realtimeSinceStartup;
    }

    public void UpdatePlayback(double currentTime)
    {
        if (!IsPlaying) return;

        double frameDuration = 1.0 / FrameRate;
        double elapsed = currentTime - lastFrameTime;

        if (elapsed >= frameDuration)
        {
            int framesToAdvance = Mathf.FloorToInt((float)(elapsed / frameDuration));
            currentFrameIndex = (currentFrameIndex + framesToAdvance) % frames.Count;
            lastFrameTime = currentTime;
        }
    }

    public void CopyFrame()
    {
        copiedFrame = new Frame(canvasSize);
        Color[] pixels = CurrentFrame.GetPixels();
        copiedFrame.SetPixels(pixels);
    }

    public void PasteFrame()
    {
        if (copiedFrame == null) return;
        Color[] pixels = copiedFrame.GetPixels();
        CurrentFrame.SetPixels(pixels);
        CurrentFrame.ApplyAllTextures();
    }

    public void MarkDirty()
    {
        foreach (var frame in frames)
        {
            // Force recomposite on next access
        }
    }

    public Texture2D GetCompositeTexture(int pixelScale = 1, bool includeOnionSkin = false,
                                       bool includePrev = false, float prevOpacity = 0.25f,
                                       bool includeNext = false, float nextOpacity = 0.25f,
                                       bool frameBlending = false, int frameIndex = -1)
    {
        int targetFrame = frameIndex >= 0 ? frameIndex : currentFrameIndex;
        return frames[targetFrame].GetCompositeTexture(pixelScale);
    }
}

// Brush system class
public class BrushSystem
{
    public enum BrushType { Pencil, Spray, SprayDither }

    public void ApplyBrush(Frame frame, int centerX, int centerY, Color color, int size,
                          BrushType brushType, BrushTipShape tipShape, bool symmetryX,
                          bool symmetryY, bool radialSymmetry, int radialFolds,
                          bool pixelPerfect, bool brushFalloff)
    {
        int radius = size / 2;

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (tipShape == BrushTipShape.Circle)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > radius) continue;
                }

                ApplyBrushToPixel(frame, centerX + dx, centerY + dy, color, brushType,
                                 brushFalloff ? 1f - (Mathf.Abs(dx) + Mathf.Abs(dy)) / (float)(size) : 1f);

                // Apply symmetry
                if (symmetryX) ApplyBrushToPixel(frame, centerX - dx, centerY + dy, color, brushType,
                                               brushFalloff ? 1f - (Mathf.Abs(dx) + Mathf.Abs(dy)) / (float)(size) : 1f);
                if (symmetryY) ApplyBrushToPixel(frame, centerX + dx, centerY - dy, color, brushType,
                                               brushFalloff ? 1f - (Mathf.Abs(dx) + Mathf.Abs(dy)) / (float)(size) : 1f);
                if (symmetryX && symmetryY) ApplyBrushToPixel(frame, centerX - dx, centerY - dy, color, brushType,
                                                           brushFalloff ? 1f - (Mathf.Abs(dx) + Mathf.Abs(dy)) / (float)(size) : 1f);

                // Apply radial symmetry
                if (radialSymmetry && radialFolds > 1)
                {
                    float angleStep = 360f / radialFolds;
                    Vector2 center = new Vector2(centerX, centerY);
                    Vector2 offset = new Vector2(dx, dy);

                    for (int i = 1; i < radialFolds; i++)
                    {
                        float angle = angleStep * i;
                        Vector2 rotated = RotateVector(offset, angle);
                        int rx = Mathf.RoundToInt(center.x + rotated.x);
                        int ry = Mathf.RoundToInt(center.y + rotated.y);
                        ApplyBrushToPixel(frame, rx, ry, color, brushType,
                                        brushFalloff ? 1f - (Mathf.Abs(dx) + Mathf.Abs(dy)) / (float)(size) : 1f);
                    }
                }
            }
        }
    }

    private void ApplyBrushToPixel(Frame frame, int x, int y, Color color, BrushType brushType, float strength = 1f)
    {
        if (x < 0 || x >= frame.GetPixels().Length || y < 0 || y >= frame.GetPixels().Length)
            return;

        Color finalColor = color;
        if (brushType == BrushType.Spray)
        {
            if (UnityEngine.Random.value > strength) return;
        }
        else if (brushType == BrushType.SprayDither)
        {
            // Simple dithering pattern
            bool shouldDraw = (x + y) % 2 == 0;
            if (!shouldDraw) return;
        }

        if (strength < 1f && brushType == BrushType.Pencil)
        {
            Color existing = frame.GetPixel(x, y);
            finalColor = Color.Lerp(existing, color, strength);
        }

        frame.SetPixel(x, y, finalColor);
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    public void DrawLine(Frame frame, int x1, int y1, int x2, int y2, Color color, int size,
                        BrushType brushType, BrushTipShape tipShape, bool symmetryX,
                        bool symmetryY, bool radialSymmetry, int radialFolds)
    {
        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);
        int sx = (x1 < x2) ? 1 : -1;
        int sy = (y1 < y2) ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            ApplyBrush(frame, x1, y1, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds, false, false);

            if (x1 == x2 && y1 == y2) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x1 += sx; }
            if (e2 < dx) { err += dx; y1 += sy; }
        }
    }

    public void DrawCircle(Frame frame, int centerX, int centerY, int radius, Color color, int size,
                          BrushType brushType, BrushTipShape tipShape, bool symmetryX,
                          bool symmetryY, bool radialSymmetry, int radialFolds)
    {
        int x = radius;
        int y = 0;
        int err = 0;

        while (x >= y)
        {
            ApplyBrush(frame, centerX + x, centerY + y, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds, false, false);
            ApplyBrush(frame, centerX + y, centerY + x, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds, false, false);
            ApplyBrush(frame, centerX - y, centerY + x, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds, false, false);
            ApplyBrush(frame, centerX - x, centerY + y, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds, false, false);
            ApplyBrush(frame, centerX - x, centerY - y, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds, false, false);
            ApplyBrush(frame, centerX - y, centerY - x, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds, false, false);
            ApplyBrush(frame, centerX + y, centerY - x, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds, false, false);
            ApplyBrush(frame, centerX + x, centerY - y, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds, false, false);

            if (err <= 0)
            {
                y += 1;
                err += 2 * y + 1;
            }
            if (err > 0)
            {
                x -= 1;
                err -= 2 * x + 1;
            }
        }
    }

    public void DrawRect(Frame frame, int x1, int y1, int x2, int y2, Color color, int size,
                        BrushType brushType, BrushTipShape tipShape, bool symmetryX,
                        bool symmetryY, bool radialSymmetry, int radialFolds)
    {
        DrawLine(frame, x1, y1, x2, y1, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds);
        DrawLine(frame, x2, y1, x2, y2, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds);
        DrawLine(frame, x2, y2, x1, y2, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds);
        DrawLine(frame, x1, y2, x1, y1, color, size, brushType, tipShape, symmetryX, symmetryY, radialSymmetry, radialFolds);
    }
}

// Custom brush class
[System.Serializable]
public class CustomBrush
{
    public string Name;
    public int Size;
    public BrushSystem.BrushType BrushType;
    public BrushTipShape TipShape;

    public CustomBrush(string name, int size, BrushSystem.BrushType brushType, BrushTipShape tipShape)
    {
        Name = name;
        Size = size;
        BrushType = brushType;
        TipShape = tipShape;
    }
}

// Bone animation classes
[System.Serializable]
public class Bone
{
    public string Name = "Bone";
    public int ParentIndex = -1;
    public float Length = 10f;
    public Vector2 Position = Vector2.zero;
    public float Rotation = 0f;
}

[System.Serializable]
public class BonePose
{
    public Vector2 Position = Vector2.zero;
    public float Rotation = 0f;

    public BonePose(Vector2 position, float rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}

[System.Serializable]
public class BoneAnimationFrame
{
    public List<BonePose> Poses = new List<BonePose>();
}

[System.Serializable]
public class BoneAnimationManager
{
    public List<Bone> Bones = new List<Bone>();
    public List<BoneAnimationFrame> Frames = new List<BoneAnimationFrame>();
}

// Pixel art core system
public class PixelArtCore
{
    // Core pixel art functionality can be added here
    // This class serves as a placeholder for future expansion
}
#endif
// Pixel Art Studio Pro - A comprehensive pixel art and animation editor for Unity
// Created by Gunz4HireXDK

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public partial class PixelArtStudioPro : EditorWindow
{
    // UI state
    private Vector2 leftScroll, rightScroll, timelineScroll, paletteScroll, gradientScroll;
    private Color uiAccent = new Color(0.12f, 0.6f, 0.95f, 1f);
    private Color panelBG = new Color(0.12f, 0.12f, 0.12f, 1f);

    // Managers & core
    private AnimationManager anim;
    private PixelArtCore core;
    private BrushSystem brushSys;

    // Tool state
    private Tool currentTool = Tool.Pencil;
    private BrushSystem.BrushType currentBrush = BrushSystem.BrushType.Pencil;
    private int brushSize = 1;
    private bool pixelPerfect = false;
    private bool brushFalloff = false;
    private bool symmetryX = false, symmetryY = false;
    private bool radialSymmetry = false;
    private int radialFolds = 6;
    private const int MaxRadialFolds = 32;

    private Color currentColor = Color.black;

    // Shape preview
    private bool isDraggingShape = false;
    private Vector2Int shapeStart = new Vector2Int(-1, -1);
    private Vector2Int shapeCurrent = new Vector2Int(-1, -1);

    // Selection
    private bool isSelecting = false;
    private Vector2Int selectStart = new Vector2Int(-1, -1);
    private RectInt currentSelection = new RectInt(0, 0, 0, 0);
    private Color[] selectionBuffer = null;
    private bool selectionMoving = false;
    private Vector2 selectionMoveOffset = Vector2.zero;

    // Undo/Redo per layer
    private const int UNDO_LIMIT = 60;
    private Dictionary<Layer, LimitedStack<Color[]>> undoStacks = new Dictionary<Layer, LimitedStack<Color[]>>();
    private Dictionary<Layer, LimitedStack<Color[]>> redoStacks = new Dictionary<Layer, LimitedStack<Color[]>>();

    // Palettes
    private List<Color> palette = new List<Color>();
    private bool paletteLocked = false;
    private string paletteFilePath = "";

    // Reference image
    private Texture2D referenceImage = null;
    private bool showReference = false;
    private float referenceOpacity = 0.5f;

    // Onion skin
    private bool onionEnabled = true;
    private float prevOnionOpacity = 0.25f;
    private float nextOnionOpacity = 0.25f;
    private int onionPrevRange = 1, onionNextRange = 1;

    // Autosave
    private string autosavePath;
    private double lastAutoSaveTime = 0;
    private double autoSaveIntervalSeconds = 60.0;
    private bool hasUnsavedChanges = false;

    // Hover pixel
    private int hoverX = -1, hoverY = -1;

    // Notification timing
    private double lastNotificationTime = 0;
    private const double notificationCooldown = 1.0;

    // Panel widths
    private const int LeftPanelWidth = 300;
    private const int RightPanelWidth = 275;

    // Custom brushes
    private List<CustomBrush> customBrushes = new List<CustomBrush>();
    private int selectedCustomBrush = -1;
    private string newBrushName = "";

    // Bone Animation System Fields
    private BoneAnimationManager boneAnim = new BoneAnimationManager();
    private int selectedBone = -1;
    private bool isAddingBone = false;
    private bool isMovingBone = false;
    private Vector2 boneDragOffset;
    private float boneHandleSize => 10f * zoom;
    private string newBoneName = "Bone";

    // Weight Painting Fields
    private int[,] boneWeights = null;
    private float[,] boneWeightsStrength = null;
    private bool weightPaintMode = false;
    private int weightPaintBone = 0;
    private float weightPaintStrength = 1f;

    // Gradient Tool Implementation
    private int gradientColorCount = 2;
    private List<Color> gradientColors = new List<Color> { Color.black, Color.white };
    private List<float> gradientPositions = new List<float> { 0f, 1f };
    private Vector2Int gradientStart = new Vector2Int(-1, -1);
    private Vector2Int gradientEnd = new Vector2Int(-1, -1);
    private bool isDrawingGradient = false;
    private GradientType currentGradientType = GradientType.Linear;

    // Additional fields
    private bool autosaveEnabled = true;
    private bool showAutosaveSettings = false;

    // Brush tip shape option
    private BrushTipShape brushTipShape = BrushTipShape.Square;

    // Animation naming, organization, loop points, ping pong, onion skin, frame blending
    private string animationName = "Untitled";
    private int loopStart = 0, loopEnd = 0;
    private bool pingPong = false;
    private int onionPrevRangeUI = 1, onionNextRangeUI = 1;
    private Color onionPrevColor = new Color(1f, 0.7f, 0.2f, 0.25f);
    private Color onionNextColor = new Color(0.2f, 0.7f, 1f, 0.25f);
    private bool frameBlending = false;

    // FEATURE 1: Tiling/Pattern Preview
    private bool showTilingPreview = false;
    private int tilingGridSize = 3;
    private TilingMode tilingMode = TilingMode.None;

    // FEATURE 2: Magic Wand Selection
    private float magicWandTolerance = 0.1f;
    private bool magicWandContinuous = true;

    // FEATURE 4: Isometric Grid
    private bool showIsometricGrid = false;
    private float isometricGridAngle = 30f;
    private Color isometricGridColor = new Color(1f, 1f, 1f, 0.2f);

    // FEATURE 5: Custom Export Presets
    private ExportPreset exportPreset = ExportPreset.Unity;
    private int exportPadding = 1;
    private bool exportTrim = true;

    // -------------------- Unity lifecycle --------------------
    [MenuItem("Window/2D/Pixel Studio Pro")]
    public static void Open()
    {
        var window = GetWindow<PixelArtStudioPro>("Pixel Studio Pro");
        window.maximized = true;
        window.Focus();
    }

    private void OnEnable()
    {
        core = new PixelArtCore();
        brushSys = new BrushSystem();
        anim = new AnimationManager(canvasSize);
        anim.EnsureAtLeastOneFrame();
        autosavePath = Path.Combine(Application.dataPath, "PixelArtSuite_Autosave.bytes");
        LoadPresetPalettes();
        EditorApplication.update -= EditorTick;
        EditorApplication.update += EditorTick;
        minSize = new Vector2(1000, 700);
        boneAnim.Bones ??= new List<Bone>();
        boneAnim.Frames ??= new List<BoneAnimationFrame>();
        SyncBoneFrames();
    }

    private void OnDisable()
    {
        EditorApplication.update -= EditorTick;
    }

    private void OnDestroy()
    {
        if (hasUnsavedChanges)
        {
            bool close = EditorUtility.DisplayDialog(
                "Unsaved changes",
                "You have unsaved changes. Close anyway?",
                "Yes", "No");

            if (!close)
            {
                // Cancel the close by reopening AND keeping the state
                EditorApplication.delayCall += () =>
                {
                    var window = GetWindow<PixelArtStudioPro>("Pixel Studio Pro");
                    window.hasUnsavedChanges = this.hasUnsavedChanges;
                    window.canvasSize = this.canvasSize;
                    window.anim = this.anim;
                    window.core = this.core;
                    window.brushSys = this.brushSys;
                    window.Repaint();
                };
            }
        }
    }

    private void EditorTick()
    {
        if (anim.IsPlaying) anim.UpdatePlayback(Time.realtimeSinceStartup);

        if (autosaveEnabled && EditorApplication.timeSinceStartup - lastAutoSaveTime > autoSaveIntervalSeconds)
        {
            TryAutoSave(false);
            lastAutoSaveTime = EditorApplication.timeSinceStartup;
        }

        SyncBoneFrames();
    }

    private void SyncBoneFrames()
    {
        while (boneAnim.Frames.Count < anim.FrameCount)
        {
            var frame = new BoneAnimationFrame();
            foreach (var bone in boneAnim.Bones)
                frame.Poses.Add(new BonePose(bone.Position, bone.Rotation));
            boneAnim.Frames.Add(frame);
        }
        while (boneAnim.Frames.Count > anim.FrameCount)
            boneAnim.Frames.RemoveAt(boneAnim.Frames.Count - 1);
    }

    private void OnGUI()
    {
        HandleHotkeys(Event.current);
        DrawToolbar();
        GUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawCanvasPanel();
        DrawRightPanel();
        GUILayout.EndHorizontal();
        DrawBottomPanel();
    }
}
#endif
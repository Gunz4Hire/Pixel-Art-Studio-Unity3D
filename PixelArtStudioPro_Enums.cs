#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public enum BrushTipShape { Square, Circle }
public enum Tool { Pencil, Eraser, Line, Fill, Gradient, Circle, Square, Spray, SprayDither, Select, MagicWand }
public enum GradientType { Linear, Radial, Angular, Diamond, Reflected }
public enum TilingMode { None, Horizontal, Vertical, Both }
public enum ExportPreset { Unity, Unreal, Godot, Custom }
public enum LayerTransform { FlipH, FlipV, Rotate90 }
#endif
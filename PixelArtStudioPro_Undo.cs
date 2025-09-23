#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public partial class PixelArtStudioPro
{
    private void SaveUndoState()
    {
        Layer currentLayer = anim.CurrentFrame.Layers[anim.CurrentFrame.ActiveLayerIndex];

        if (!undoStacks.ContainsKey(currentLayer))
            undoStacks[currentLayer] = new LimitedStack<Color[]>(UNDO_LIMIT);

        if (!redoStacks.ContainsKey(currentLayer))
            redoStacks[currentLayer] = new LimitedStack<Color[]>(UNDO_LIMIT);

        // Save current state to undo stack
        undoStacks[currentLayer].Push(currentLayer.GetPixels().ToArray());

        // Clear redo stack when new action is performed
        redoStacks[currentLayer].Clear();
    }

    private void LayerUndo()
    {
        Layer currentLayer = anim.CurrentFrame.Layers[anim.CurrentFrame.ActiveLayerIndex];

        if (!undoStacks.ContainsKey(currentLayer) || undoStacks[currentLayer].Count == 0)
            return;

        // Save current state to redo stack
        if (!redoStacks.ContainsKey(currentLayer))
            redoStacks[currentLayer] = new LimitedStack<Color[]>(UNDO_LIMIT);

        redoStacks[currentLayer].Push(currentLayer.GetPixels().ToArray());

        // Restore previous state from undo stack
        Color[] previousState = undoStacks[currentLayer].Pop();
        currentLayer.SetPixels(previousState);
        currentLayer.ApplyTexture();

        anim.MarkDirty();
        hasUnsavedChanges = true;
        Repaint();
    }

    private void LayerRedo()
    {
        Layer currentLayer = anim.CurrentFrame.Layers[anim.CurrentFrame.ActiveLayerIndex];

        if (!redoStacks.ContainsKey(currentLayer) || redoStacks[currentLayer].Count == 0)
            return;

        // Save current state to undo stack
        if (!undoStacks.ContainsKey(currentLayer))
            undoStacks[currentLayer] = new LimitedStack<Color[]>(UNDO_LIMIT);

        undoStacks[currentLayer].Push(currentLayer.GetPixels().ToArray());

        // Restore next state from redo stack
        Color[] nextState = redoStacks[currentLayer].Pop();
        currentLayer.SetPixels(nextState);
        currentLayer.ApplyTexture();

        anim.MarkDirty();
        hasUnsavedChanges = true;
        Repaint();
    }
}

// Limited stack implementation for undo/redo
public class LimitedStack<T>
{
    private LinkedList<T> list = new LinkedList<T>();
    private int capacity;

    public LimitedStack(int capacity)
    {
        this.capacity = capacity;
    }

    public void Push(T item)
    {
        if (list.Count == capacity)
            list.RemoveLast();
        list.AddFirst(item);
    }

    public T Pop()
    {
        if (list.Count == 0)
            throw new InvalidOperationException("Stack is empty");
        T item = list.First.Value;
        list.RemoveFirst();
        return item;
    }

    public void Clear()
    {
        list.Clear();
    }

    public int Count => list.Count;
}
#endif
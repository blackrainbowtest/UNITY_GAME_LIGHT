
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace ChebDoorStudio.Editor.Hierarchy
{
  public static class HierarchyLines
  {
    private static readonly List<Color> _lineColors = new()
    {
      new Color32(255, 99, 71, 255),
      new Color32(60, 179, 113, 255),
      new Color32(30, 144, 255, 255),
      new Color32(255, 215, 0, 255),
      new Color32(255, 105, 180, 255),
      new Color32(138, 43, 226, 255),
      new Color32(255, 165, 0, 255),
      new Color32(0, 255, 255, 255),
      new Color32(124, 252, 0, 255),
      new Color32(0, 191, 255, 255)
    };

    public static void Initialize()
    {
      EditorApplication.hierarchyWindowItemOnGUI -= DrawHierarchyLines;
      EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyLines;
    }

    private static void DrawHierarchyLines(int instanceID, Rect rect)
    {
      Object obj = EditorUtility.InstanceIDToObject(instanceID);
      if (obj is not GameObject go)
      {
        return;
      }

      Transform t = go.transform;
      Transform p = t.parent;
      bool hasParent = p;
      bool isLastChild = hasParent && t.GetSiblingIndex() == p.childCount - 1;
      bool hasChildren = t.childCount > 0;

      List<bool> middleLines = new();
      List<int> depthIndexes = new();
      int depth = 0;

      while (p)
      {
        middleLines.Insert(0, t.GetSiblingIndex() != p.childCount - 1);
        depthIndexes.Insert(0, depth);
        depth++;
        t = p;
        p = p.parent;
      }

      int marginWidth = hasChildren ? 14 : 2;
      int lineWidth = (14 * depth) + 34;
      Rect lineRect = new(rect.x - lineWidth, rect.y, lineWidth - marginWidth, rect.height);

      if (hasParent)
      {
        int extraWidth = hasChildren ? 0 : 12;

        static void Line(Vector3 start, Vector3 end, Color color)
        {
          Handles.color = color;
          Handles.DrawAAPolyLine(2, 2, start, end);
        }

        GUI.BeginClip(lineRect);
        float basef = lineWidth - marginWidth;
        Vector3 startingPoint = new(basef, rect.height / 2);
        Vector3 middlePoint = new(basef - extraWidth - 8, rect.height / 2);
        Line(startingPoint, middlePoint, GetColor(depth - 1));

        if (isLastChild)
        {
          Vector3 connectionPoint = new(basef - extraWidth - 8, 0);
          Line(middlePoint, connectionPoint, GetColor(depth - 1));
        }

        for (int i = 0; i < middleLines.Count; i++)
        {
          if (!middleLines[i])
          {
            continue;
          }

          float x = lineRect.x + (14 * i);
          Vector3 topConnection = new(x, 0);
          Vector3 bottomConnection = new(x, rect.height);
          int levelDepth = i;
          Line(topConnection, bottomConnection, GetColor(levelDepth));
        }

        GUI.EndClip();
      }
    }

    private static Color GetColor(int depth)
    {
      return _lineColors[depth % _lineColors.Count];
    }
  }
}
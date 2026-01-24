
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace ChebDoorStudio.Editor.Hierarchy
{
  public static class HierarchyIcons
  {
    private static readonly Dictionary<Type, Texture2D> _iconCache = new();

    private static readonly MethodInfo _objectContentMethod = typeof(EditorGUIUtility).GetMethod(
      "ObjectContent",
      BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
      null,
      new[] { typeof(Object), typeof(Type) },
      null
    );

    private static readonly Dictionary<int, Texture2D> _objectIcons = new();

    public static void Initialize()
    {
      EditorApplication.hierarchyChanged -= UpdateIcons;
      EditorApplication.hierarchyWindowItemOnGUI -= DrawHierarchyIcon;

      EditorApplication.hierarchyChanged += UpdateIcons;
      EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyIcon;
      UpdateIcons();
    }

    private static void DrawHierarchyIcon(int instanceID, Rect selectionRect)
    {
      if (_objectIcons.TryGetValue(instanceID, out Texture2D icon) && icon != null)
      {
        Rect iconRect = new(selectionRect.x, selectionRect.y + 1f, 15f, 15f);

        Color bgColor = EditorGUIUtility.isProSkin
          ? new Color(0.22f, 0.22f, 0.22f)
          : new Color(0.76f, 0.76f, 0.76f);

        EditorGUI.DrawRect(iconRect, bgColor);
        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
      }
    }

    private static Texture2D GetIconForType(Type type)
    {
      if (_iconCache.TryGetValue(type, out Texture2D icon))
      {
        return icon;
      }

      if (_objectContentMethod != null)
      {
        GUIContent content = (GUIContent)_objectContentMethod.Invoke(null, new object[] { null, type });
        icon = content?.image as Texture2D;
        if (icon != null)
        {
          _iconCache[type] = icon;
          return icon;
        }
      }

      return null;
    }

    private static void UpdateIcons()
    {
      _objectIcons.Clear();

      foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
      {
        Component[] components = go.GetComponents<Component>();

        for (int i = 1; i < components.Length; i++)
        {
          Component comp = components[i];
          if (comp is null or Transform or RectTransform or CanvasRenderer)
          {
            continue;
          }

          Texture2D icon = GetIconForType(comp.GetType());
          if (icon != null)
          {
            _objectIcons[go.GetInstanceID()] = icon;
            break;
          }
        }
      }
    }
  }
}
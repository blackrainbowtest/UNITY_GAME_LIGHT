
using System;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace ChebDoorStudio.Editor.Hierarchy
{
  public static class HierarchyObjectHider
  {
    private static GameObject _hoveredObject;

    public static void Initialize()
    {
      EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
      EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
      Event e = Event.current;

      Object obj = EditorUtility.InstanceIDToObject(instanceID);
      if (obj is not GameObject go)
      {
        return;
      }

      Rect toggleRect = new(32f, selectionRect.y, 16f, 16f);
      bool isHovered = selectionRect.Contains(e.mousePosition) || toggleRect.Contains(e.mousePosition);

      if (isHovered)
      {
        _hoveredObject = go;

        if (e.type == EventType.MouseMove)
        {
          EditorWindow.focusedWindow.Repaint();
        }
      }
      else if (_hoveredObject == go && !isHovered)
      {
        _hoveredObject = null;

        if (e.type == EventType.MouseMove)
        {
          EditorWindow.focusedWindow.Repaint();
        }
      }

      if (_hoveredObject == go)
      {
        bool isActive = go.activeSelf;

        bool clicked = GUI.Toggle(toggleRect, go.activeSelf, GUIContent.none);

        if (clicked != go.activeSelf)
        {
          Undo.RecordObject(go, "Toggle Active State");
          go.SetActive(!isActive);
          EditorUtility.SetDirty(go);
          e.Use();
        }
      }
    }
  }
}
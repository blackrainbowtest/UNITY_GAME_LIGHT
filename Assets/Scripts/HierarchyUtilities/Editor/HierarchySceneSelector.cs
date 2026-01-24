
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ChebDoorStudio.Editor.Hierarchy
{
  public static class HierarchySceneSelector
  {
    public static void Initialize()
    {
      EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
      EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
      if (selectionRect.y > 10f)
      {
        return;
      }

      Rect buttonRect = new(selectionRect.xMax - 60f, selectionRect.y - 2, 56, 20);

      if (GUI.Button(buttonRect, "Scenes", EditorStyles.miniButton))
      {
        SceneDropdown.Show(buttonRect);
      }
    }

    private class SceneDropdown : PopupWindowContent
    {
      private readonly List<string> _sceneNames;
      private readonly List<string> _scenePaths;

      private Vector2 _scrollPos;

      public SceneDropdown()
      {
        this._scenePaths = AssetDatabase.FindAssets("t:Scene")
          .Select(AssetDatabase.GUIDToAssetPath)
          .Where(path => path.StartsWith("Assets/"))
          .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
          .Where(path => Path.GetFileNameWithoutExtension(path).IndexOf("Scene", StringComparison.OrdinalIgnoreCase) >= 0)
          .Where(File.Exists)
          .OrderBy(Path.GetFileNameWithoutExtension)
          .ToList();

        this._sceneNames = this._scenePaths
          .Select(Path.GetFileNameWithoutExtension)
          .ToList();
      }

      public static void Show(Rect activatorRect)
      {
        PopupWindow.Show(activatorRect, new SceneDropdown());
      }

      public override Vector2 GetWindowSize()
      {
        return new Vector2(156, Mathf.Min(400, (this._scenePaths.Count * 22) + 10));
      }

      public override void OnGUI(Rect rect)
      {
        this._scrollPos = EditorGUILayout.BeginScrollView(this._scrollPos);
        for (int i = 0; i < this._scenePaths.Count; i++)
        {
          if (GUILayout.Button(this._sceneNames[i], EditorStyles.miniButton))
          {
            this.TryOpenScene(this._scenePaths[i]);
            this.editorWindow.Close();
          }
        }

        EditorGUILayout.EndScrollView();
      }

      private void TryOpenScene(string scenePath)
      {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
          Debug.LogWarning("[HierarchySceneSelector] Cannot open scenes during Play Mode. Stop Play Mode first.");
          return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
          _ = EditorSceneManager.OpenScene(scenePath);
        }
      }
    }
  }
}
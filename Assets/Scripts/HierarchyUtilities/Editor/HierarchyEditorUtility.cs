
using System;
using UnityEngine;
using UnityEditor;

namespace ChebDoorStudio.Editor.Hierarchy
{
  [InitializeOnLoad]
  public static class HierarchyEditorUtility
  {
    static HierarchyEditorUtility()
    {
      HierarchyIcons.Initialize();
      HierarchyObjectHider.Initialize();
      HierarchySceneSelector.Initialize();
      HierarchyLines.Initialize();
    }
  }
}
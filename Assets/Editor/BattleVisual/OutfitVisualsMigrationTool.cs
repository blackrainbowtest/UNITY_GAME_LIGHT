using UnityEditor;
using UnityEngine;

namespace UDA2.EditorTools.BattleVisual
{
    public static class OutfitVisualsMigrationTool
    {
        [MenuItem("Tools/Battle Visual/OutfitVisuals Variations Info")]
        public static void ShowInfo()
        {
            EditorUtility.DisplayDialog(
                "OutfitVisuals",
                "Single animation fields were removed. Use *Variations arrays on OutfitVisuals (Idle/Hit/Attacks/etc).\n\nIf you had old data in single fields, reassign it into the corresponding variations arrays.",
                "OK"
            );

            Debug.Log("OutfitVisuals: Singles removed; use variations arrays.");
        }
    }
}

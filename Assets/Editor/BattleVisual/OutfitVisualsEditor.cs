using Game.Battle.Visual;
using UnityEditor;

[CustomEditor(typeof(OutfitVisuals))]
[CanEditMultipleObjects]
public sealed class OutfitVisualsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script");

        serializedObject.ApplyModifiedProperties();
    }
}

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom inspector for ItemDefinition.
/// - Provides a dropdown for equip slot instead of free-form string.
/// - Prioritizes localization keys (name/description).
/// - Keeps raw displayName/description as optional fallbacks.
/// </summary>
[CustomEditor(typeof(ItemDefinition))]
public sealed class ItemDefinitionEditor : Editor
{
    private static readonly string[] EquipSlotOptions =
    {
        "<None>",
        "Bag",
        "Ring",
        "Ring1",
        "Ring2",
        "Amulet",
        "Weapon",
        "Helmet",
        "Armor",
        "Pants",
        "Boots",
    };

    private SerializedProperty _id;
    private SerializedProperty _type;

    private SerializedProperty _displayName;
    private SerializedProperty _icon;

    private SerializedProperty _displayNameKey;
    private SerializedProperty _descriptionKey;
    private SerializedProperty _description;

    private SerializedProperty _stackable;
    private SerializedProperty _maxStack;

    private SerializedProperty _equipSlotId;
    private SerializedProperty _inventorySlotsBonus;

    private SerializedProperty _effect;
    private SerializedProperty _value;

    private bool _showFallbackUi;

    private void OnEnable()
    {
        _id = serializedObject.FindProperty("id");
        _type = serializedObject.FindProperty("type");

        _displayName = serializedObject.FindProperty("displayName");
        _icon = serializedObject.FindProperty("icon");

        _displayNameKey = serializedObject.FindProperty("displayNameKey");
        _descriptionKey = serializedObject.FindProperty("descriptionKey");
        _description = serializedObject.FindProperty("description");

        _stackable = serializedObject.FindProperty("stackable");
        _maxStack = serializedObject.FindProperty("maxStack");

        _equipSlotId = serializedObject.FindProperty("equipSlotId");
        _inventorySlotsBonus = serializedObject.FindProperty("inventorySlotsBonus");

        _effect = serializedObject.FindProperty("effect");
        _value = serializedObject.FindProperty("value");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_id);
        EditorGUILayout.PropertyField(_type);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Localization", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_displayNameKey, new GUIContent("Name Key"));
        EditorGUILayout.PropertyField(_descriptionKey, new GUIContent("Description Key"));

        EditorGUILayout.Space(6);
        _showFallbackUi = EditorGUILayout.Foldout(_showFallbackUi, "Fallback UI (optional)", true);
        if (_showFallbackUi)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_displayName, new GUIContent("Display Name (fallback)"));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description (fallback)"));
            EditorGUILayout.PropertyField(_icon);
            EditorGUI.indentLevel--;
        }
        else
        {
            // Still allow icon without expanding, since it's frequently needed.
            EditorGUILayout.PropertyField(_icon);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Stacking", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_stackable);
        using (new EditorGUI.DisabledScope(!_stackable.boolValue))
        {
            EditorGUILayout.PropertyField(_maxStack);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Equipment", EditorStyles.boldLabel);
        var itemType = (ItemType)_type.enumValueIndex;
        using (new EditorGUI.DisabledScope(itemType != ItemType.Equipment))
        {
            DrawEquipSlotDropdown();
            EditorGUILayout.PropertyField(_inventorySlotsBonus, new GUIContent("Inventory Slots Bonus"));
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Consumable", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(itemType != ItemType.Consumable))
        {
            EditorGUILayout.PropertyField(_effect);
            EditorGUILayout.PropertyField(_value);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEquipSlotDropdown()
    {
        string current = _equipSlotId.stringValue ?? string.Empty;
        int index = 0;
        for (int i = 0; i < EquipSlotOptions.Length; i++)
        {
            var option = EquipSlotOptions[i];
            if (option == "<None>" && string.IsNullOrEmpty(current))
            {
                index = i;
                break;
            }

            if (option != "<None>" && string.Equals(option, current, System.StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        int newIndex = EditorGUILayout.Popup("Equip Slot", index, EquipSlotOptions);
        string newValue = EquipSlotOptions[newIndex];
        _equipSlotId.stringValue = newValue == "<None>" ? string.Empty : newValue;
    }
}
#endif

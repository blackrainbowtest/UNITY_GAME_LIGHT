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

    private SerializedProperty _equipmentStats;

    private SerializedProperty _rarity;
    private SerializedProperty _rarityId;
    private SerializedProperty _weight;

    private SerializedProperty _useType;
    private SerializedProperty _consumable;
    private SerializedProperty _cooldown;

    private SerializedProperty _hp;
    private SerializedProperty _mp;
    private SerializedProperty _sp;
    private SerializedProperty _lp;
    private SerializedProperty _statusEffects;
    private SerializedProperty _statusesLegacy;

    private SerializedProperty _hasCombatDamage;
    private SerializedProperty _combatDamage;
    private SerializedProperty _hasCombatRange;
    private SerializedProperty _combatRange;
    private SerializedProperty _hasCombatSpeed;
    private SerializedProperty _combatSpeed;
    private SerializedProperty _combatTags;

    private SerializedProperty _canDrop;
    private SerializedProperty _canDestroy;
    private SerializedProperty _hasContainerSize;
    private SerializedProperty _containerSize;

    private SerializedProperty _flags;

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

        _equipmentStats = serializedObject.FindProperty("equipmentStats");

        _rarity = serializedObject.FindProperty("rarity");
        _rarityId = serializedObject.FindProperty("rarityId");
        _weight = serializedObject.FindProperty("weight");

        _useType = serializedObject.FindProperty("useType");
        _consumable = serializedObject.FindProperty("consumable");
        _cooldown = serializedObject.FindProperty("cooldown");

        _hp = serializedObject.FindProperty("hp");
        _mp = serializedObject.FindProperty("mp");
        _sp = serializedObject.FindProperty("sp");
        _lp = serializedObject.FindProperty("lp");
        _statusEffects = serializedObject.FindProperty("statusEffects");
        _statusesLegacy = serializedObject.FindProperty("statusesLegacy");

        _hasCombatDamage = serializedObject.FindProperty("hasCombatDamage");
        _combatDamage = serializedObject.FindProperty("combatDamage");
        _hasCombatRange = serializedObject.FindProperty("hasCombatRange");
        _combatRange = serializedObject.FindProperty("combatRange");
        _hasCombatSpeed = serializedObject.FindProperty("hasCombatSpeed");
        _combatSpeed = serializedObject.FindProperty("combatSpeed");
        _combatTags = serializedObject.FindProperty("combatTags");

        _canDrop = serializedObject.FindProperty("canDrop");
        _canDestroy = serializedObject.FindProperty("canDestroy");
        _hasContainerSize = serializedObject.FindProperty("hasContainerSize");
        _containerSize = serializedObject.FindProperty("containerSize");

        _flags = serializedObject.FindProperty("flags");
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
        EditorGUILayout.LabelField("Meta", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_rarity);
        if (_rarityId != null && !string.IsNullOrWhiteSpace(_rarityId.stringValue))
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(_rarityId, new GUIContent("Rarity Id (from JSON)"));
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Economy", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_value, new GUIContent("Value"));
        EditorGUILayout.PropertyField(_weight);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Usage", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_useType);
        EditorGUILayout.PropertyField(_consumable);
        EditorGUILayout.PropertyField(_cooldown);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_hp);
        EditorGUILayout.PropertyField(_mp);
        EditorGUILayout.PropertyField(_sp);
        EditorGUILayout.PropertyField(_lp);
        if (_statusEffects != null)
            EditorGUILayout.PropertyField(_statusEffects, new GUIContent("Statuses"), includeChildren: true);

        if (_statusesLegacy != null && _statusesLegacy.isArray && _statusesLegacy.arraySize > 0)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(_statusesLegacy, new GUIContent("Legacy Status Ids"), includeChildren: true);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Equipment", EditorStyles.boldLabel);
        var itemType = (ItemType)_type.enumValueIndex;
        using (new EditorGUI.DisabledScope(itemType != ItemType.Equipment))
        {
            DrawEquipSlotDropdown();
            EditorGUILayout.PropertyField(_inventorySlotsBonus, new GUIContent("Inventory Slots Bonus"));
            EditorGUILayout.PropertyField(_equipmentStats, includeChildren: true);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Consumable", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(itemType != ItemType.Consumable))
        {
            EditorGUILayout.PropertyField(_effect);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Combat", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_hasCombatDamage);
        using (new EditorGUI.DisabledScope(!_hasCombatDamage.boolValue))
            EditorGUILayout.PropertyField(_combatDamage);

        EditorGUILayout.PropertyField(_hasCombatRange);
        using (new EditorGUI.DisabledScope(!_hasCombatRange.boolValue))
            EditorGUILayout.PropertyField(_combatRange);

        EditorGUILayout.PropertyField(_hasCombatSpeed);
        using (new EditorGUI.DisabledScope(!_hasCombatSpeed.boolValue))
            EditorGUILayout.PropertyField(_combatSpeed);

        EditorGUILayout.PropertyField(_combatTags, includeChildren: true);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("World", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_canDrop);
        EditorGUILayout.PropertyField(_canDestroy);
        EditorGUILayout.PropertyField(_hasContainerSize);
        using (new EditorGUI.DisabledScope(!_hasContainerSize.boolValue))
            EditorGUILayout.PropertyField(_containerSize);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Flags", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_flags, includeChildren: true);

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

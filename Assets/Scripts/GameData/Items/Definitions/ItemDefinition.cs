using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum ItemType
{
    Currency,
    Consumable,
    Resource,
    Equipment
}

public enum ConsumableEffect
{
    DoingNothing,
    HealHP,
    RestoreMana,
    RestoreStamina
}

[CreateAssetMenu(menuName = "Game/Item")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id;
    [SerializeField] private ItemType type;

    [Header("UI")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Header("Localization")]
    [Tooltip("Localization key for display name. If empty, DisplayName is used as fallback.")]
    [SerializeField] private string displayNameKey;

    [Tooltip("Localization key for description.")]
    [SerializeField] private string descriptionKey;

    [TextArea(2, 6)]
    [SerializeField] private string description;

    [Header("Stacking")]
    [SerializeField] private bool stackable = true;
    [Min(1)]
    [SerializeField] private int maxStack = 99;

    [Header("Equipment")]
    [Tooltip("For equipment items: which slot they can be equipped to (e.g. 'Bag', 'Ring1', 'Amulet', 'Weapon', 'Helmet', 'Armor', 'Pants', 'Boots').")]
    [SerializeField] private string equipSlotId;

    [Tooltip("For bags: how many inventory slots this item adds. Example: +10.")]
    [SerializeField] private int inventorySlotsBonus;

    [Header("Consumable")]
    [SerializeField] private ConsumableEffect effect;
    [SerializeField] private int value;

    public string Id => id;
    public ItemType Type => type;
    public string DisplayName => displayName;
    public Sprite Icon => icon;

    public string DisplayNameKey => displayNameKey;
    public string DescriptionKey => descriptionKey;
    public string Description => description;

    public bool Stackable => stackable;
    public int MaxStack => maxStack;

    public string EquipSlotId => equipSlotId;
    public int InventorySlotsBonus => inventorySlotsBonus;

    public bool IsEquipable => type == ItemType.Equipment && !string.IsNullOrWhiteSpace(equipSlotId);

    public bool CanEquipTo(string slotId)
    {
        if (!IsEquipable)
            return false;

        if (string.IsNullOrWhiteSpace(slotId))
            return false;

        // Allow a single item definition to support multiple "sub-slots".
        // Example: EquipSlotId = "Ring" should work for both "Ring1" and "Ring2".
        if (string.Equals(equipSlotId, "Ring", System.StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(slotId, "Ring", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(slotId, "Ring1", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(slotId, "Ring2", System.StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(equipSlotId, slotId, System.StringComparison.OrdinalIgnoreCase);
    }
    public ConsumableEffect Effect => effect;
    public int Value => value;

#if UNITY_EDITOR
    public bool EditorApplyDefinition(
        string newId,
        ItemType newType,
        string newDisplayName,
        Sprite newIcon,
        ConsumableEffect newEffect,
        int newValue,
        out string error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(newId))
        {
            error = "Item id is null or empty.";
            return false;
        }

        id = newId.Trim();
        type = newType;
        displayName = newDisplayName ?? string.Empty;
        icon = newIcon;
        effect = newEffect;
        value = newValue;

        // Leave new fields as-is to avoid breaking older importers.

        EditorUtility.SetDirty(this);
        return true;
    }

    public bool EditorApplyDefinition(
        string newId,
        ItemType newType,
        string newDisplayName,
        string newDisplayNameKey,
        string newDescriptionKey,
        string newDescription,
        Sprite newIcon,
        bool newStackable,
        int newMaxStack,
        string newEquipSlotId,
        int newInventorySlotsBonus,
        ConsumableEffect newEffect,
        int newValue,
        out string error)
    {
        if (!EditorApplyDefinition(
                newId,
                newType,
                newDisplayName,
                newIcon,
                newEffect,
                newValue,
                out error))
        {
            return false;
        }

        displayNameKey = newDisplayNameKey ?? string.Empty;
        descriptionKey = newDescriptionKey ?? string.Empty;
        description = newDescription ?? string.Empty;

        stackable = newStackable;
        maxStack = Mathf.Max(1, newMaxStack);

        equipSlotId = newEquipSlotId ?? string.Empty;
        inventorySlotsBonus = newInventorySlotsBonus;

        EditorUtility.SetDirty(this);
        return true;
    }
#endif
}

using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif
using Game.Battle.Statuses;

public enum ItemType
{
    Currency,
    Consumable,
    Resource,
    Equipment
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic,
    Unique
}

// FIXME: Consumable effects
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

    [System.Serializable]
    public struct StatEntry
    {
        public string key;
        public float value;
    }

    [Tooltip("Optional equipment stats imported from JSON. Stored as key/value pairs for Unity serialization.")]
    [SerializeField] private StatEntry[] equipmentStats;

    [Header("Meta")]
    [FormerlySerializedAs("rarity")]
    [SerializeField] private string rarityId;

    [SerializeField] private ItemRarity rarity = ItemRarity.Common;

    [Tooltip("Item weight (for future use).")]
    [SerializeField] private float weight;

    [Header("Usage")]
    [SerializeField] private string useType;
    [SerializeField] private bool consumable;
    [SerializeField] private int cooldown;

    [Header("Effects")]
    [SerializeField] private int hp;
    [SerializeField] private int mp;
    [SerializeField] private int sp;
    [SerializeField] private int lp;

    [System.Serializable]
    public struct StatusEffectGrant
    {
        public StatusEffectId id;
        [Min(0)] public int turns;
    }

    [Tooltip("Status effects granted by using this item (data-driven).")]
    [SerializeField] private StatusEffectGrant[] statusEffects;

    [FormerlySerializedAs("statuses")]
    [Tooltip("Legacy string list of statuses (kept for backward compatibility).")]
    [SerializeField] private string[] statusesLegacy;

    [Header("Combat")]
    [SerializeField] private bool hasCombatDamage;
    [SerializeField] private float combatDamage;
    [SerializeField] private bool hasCombatRange;
    [SerializeField] private float combatRange;
    [SerializeField] private bool hasCombatSpeed;
    [SerializeField] private float combatSpeed;
    [SerializeField] private string[] combatTags;

    [Header("World")]
    [SerializeField] private bool canDrop = true;
    [SerializeField] private bool canDestroy;
    [SerializeField] private bool hasContainerSize;
    [SerializeField] private int containerSize;

    [Header("Flags")]
    [SerializeField] private string[] flags;

    [Header("Consumable")]
    [SerializeField] private ConsumableEffect effect;
    [Tooltip("Economy value in gold. Used as SELL price. Buying price is SellPrice * BuyPriceMultiplier.")]
    [SerializeField] private int value;

    public const int BuyPriceMultiplier = 3;

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

    public StatEntry[] EquipmentStats => equipmentStats;
    public string RarityId => rarityId;
    public ItemRarity Rarity => rarity;
    public float Weight => weight;

    private void OnValidate()
    {
        // Keep string id in a reasonable state for newly created assets.
        if (string.IsNullOrWhiteSpace(rarityId))
            rarityId = rarity.ToString();
    }

    private void ApplyRarityFromString(string rarityString)
    {
        rarityId = rarityString ?? string.Empty;

        if (string.IsNullOrWhiteSpace(rarityId))
        {
            rarity = ItemRarity.Common;
            return;
        }

        if (System.Enum.TryParse(rarityId.Trim(), ignoreCase: true, out ItemRarity parsed))
        {
            rarity = parsed;
            return;
        }

        // Unknown rarity string -> default.
        rarity = ItemRarity.Common;
    }

    public string UseType => useType;
    public bool Consumable => consumable;
    public int Cooldown => cooldown;

    public int HP => hp;
    public int MP => mp;
    public int SP => sp;
    public int LP => lp;

    public StatusEffectGrant[] StatusEffects
    {
        get
        {
            if (statusEffects != null && statusEffects.Length > 0)
                return statusEffects;

            // Backward compatibility: parse legacy string ids into enum entries.
            if (statusesLegacy == null || statusesLegacy.Length == 0)
                return System.Array.Empty<StatusEffectGrant>();

            var converted = new StatusEffectGrant[statusesLegacy.Length];
            int count = 0;
            for (int i = 0; i < statusesLegacy.Length; i++)
            {
                var raw = statusesLegacy[i];
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                if (!System.Enum.TryParse(raw.Trim(), ignoreCase: true, out StatusEffectId parsed))
                    continue;

                converted[count++] = new StatusEffectGrant { id = parsed, turns = 1 };
            }

            if (count == converted.Length)
                return converted;

            var trimmed = new StatusEffectGrant[count];
            for (int i = 0; i < count; i++)
                trimmed[i] = converted[i];
            return trimmed;
        }
    }

    public string[] StatusesLegacy => statusesLegacy;

    public bool HasCombatDamage => hasCombatDamage;
    public float CombatDamage => combatDamage;
    public bool HasCombatRange => hasCombatRange;
    public float CombatRange => combatRange;
    public bool HasCombatSpeed => hasCombatSpeed;
    public float CombatSpeed => combatSpeed;
    public string[] CombatTags => combatTags;

    public bool CanDrop => canDrop;
    public bool CanDestroy => canDestroy;
    public bool HasContainerSize => hasContainerSize;
    public int ContainerSize => containerSize;

    public string[] Flags => flags;

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

    /// <summary>
    /// Sell price in gold.
    /// </summary>
    public int SellPrice => Mathf.Max(0, value);

    /// <summary>
    /// Buy price in gold.
    /// Rule: buy = sell * 3.
    /// </summary>
    public int BuyPrice => SellPrice * BuyPriceMultiplier;

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
        StatEntry[] newEquipmentStats,
        ConsumableEffect newEffect,
        int newValue,
        string newRarity,
        float newWeight,
        string newUseType,
        bool newConsumable,
        int newCooldown,
        int newHp,
        int newMp,
        int newSp,
        int newLp,
        StatusEffectGrant[] newStatusEffects,
        bool newHasCombatDamage,
        float newCombatDamage,
        bool newHasCombatRange,
        float newCombatRange,
        bool newHasCombatSpeed,
        float newCombatSpeed,
        string[] newCombatTags,
        bool newCanDrop,
        bool newCanDestroy,
        bool newHasContainerSize,
        int newContainerSize,
        string[] newFlags,
        out string error)
    {
        if (!EditorApplyDefinition(
                newId,
                newType,
                newDisplayName,
                newDisplayNameKey,
                newDescriptionKey,
                newDescription,
                newIcon,
                newStackable,
                newMaxStack,
                newEquipSlotId,
                newInventorySlotsBonus,
                newEffect,
                newValue,
                out error))
        {
            return false;
        }

        equipmentStats = newEquipmentStats;

        ApplyRarityFromString(newRarity);
        weight = newWeight;

        useType = newUseType ?? string.Empty;
        consumable = newConsumable;
        cooldown = newCooldown;

        hp = newHp;
        mp = newMp;
        sp = newSp;
        lp = newLp;

        statusEffects = newStatusEffects ?? System.Array.Empty<StatusEffectGrant>();
        // Keep legacy ids populated for readability and migration.
        if (statusEffects.Length == 0)
        {
            statusesLegacy = System.Array.Empty<string>();
        }
        else
        {
            var ids = new string[statusEffects.Length];
            for (int i = 0; i < statusEffects.Length; i++)
                ids[i] = statusEffects[i].id.ToString();
            statusesLegacy = ids;
        }

        hasCombatDamage = newHasCombatDamage;
        combatDamage = newCombatDamage;
        hasCombatRange = newHasCombatRange;
        combatRange = newCombatRange;
        hasCombatSpeed = newHasCombatSpeed;
        combatSpeed = newCombatSpeed;
        combatTags = newCombatTags;

        canDrop = newCanDrop;
        canDestroy = newCanDestroy;
        hasContainerSize = newHasContainerSize;
        containerSize = newContainerSize;

        flags = newFlags;

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

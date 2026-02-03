//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
// 
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Editor/ItemDefinitionJsonImporter.cs                                                      */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/21 13:22:04 by UDA                                                                    */
/*   Updated: 2026/01/21 13:22:04 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Game.Battle.Statuses;

/// <summary>
/// Editor utility for importing ItemDefinition assets from JSON.
/// 
/// This tool converts external item data into ScriptableObjects
/// and is intentionally isolated from runtime logic.
/// </summary>
public class ItemDefinitionJsonImporter : EditorWindow
{
    private string jsonPath =
        "Assets/GameData/Items/Json/items.json";

    private string iconsPath =
        "Assets/Art/Sprites/Items/";

    private string outputPath =
        "Assets/GameData/Items/Definitions/";

    [MenuItem("Tools/Items/Import Items from JSON")]
    public static void ShowWindow()
    {
        GetWindow<ItemDefinitionJsonImporter>(
            "Item JSON Importer"
        );
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "Import ItemDefinitions from JSON",
            EditorStyles.boldLabel
        );

        jsonPath = EditorGUILayout.TextField(
            "JSON Path",
            jsonPath
        );

        iconsPath = EditorGUILayout.TextField(
            "Icons Path",
            iconsPath
        );

        outputPath = EditorGUILayout.TextField(
            "Output Path",
            outputPath
        );

        if (GUILayout.Button("Import"))
        {
            ImportItems();
        }
    }

    /// <summary>
    /// Main import pipeline.
    /// </summary>
    private void ImportItems()
    {
        if (!File.Exists(jsonPath))
        {
            Debug.LogError(
                $"ItemDefinitionJsonImporter: JSON file not found at '{jsonPath}'."
            );
            return;
        }

        string json = File.ReadAllText(jsonPath);
        List<ItemJson> itemList;
        try
        {
            itemList = JsonConvert.DeserializeObject<List<ItemJson>>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ItemDefinitionJsonImporter: Failed to parse JSON: {ex.Message}");
            return;
        }

        if (itemList == null)
        {
            Debug.LogError("ItemDefinitionJsonImporter: JSON parsed to null list.");
            return;
        }

        foreach (var item in itemList)
        {
            CreateItemAsset(item);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"ItemDefinitionJsonImporter: Imported {itemList.Count} items."
        );
    }

    /// <summary>
    /// Creates and initializes a single ItemDefinition asset.
    /// </summary>
    private void CreateItemAsset(ItemJson item)
    {
        if (item == null)
        {
            Debug.LogError("ItemDefinitionJsonImporter: Item JSON is null.");
            return;
        }

        if (!TryBuildEditorDefinition(item, out var definition, out string error))
        {
            Debug.LogError($"ItemDefinitionJsonImporter: Failed to parse '{item?.id}': {error}");
            return;
        }

        string assetDir = EnsureTypeFolder(outputPath, definition.Type);
        string assetPath = $"{assetDir}{definition.Id}.asset";

        // If asset exists elsewhere (legacy path or other folder), move it.
        var existingPath = FindExistingAssetPath(definition.Id);
        if (!string.IsNullOrEmpty(existingPath) && existingPath != assetPath)
        {
            EnsureAssetFolderExists(assetDir);
            var moveResult = AssetDatabase.MoveAsset(existingPath, assetPath);
            if (!string.IsNullOrWhiteSpace(moveResult))
                Debug.LogWarning($"ItemDefinitionJsonImporter: Failed to move '{definition.Id}' to '{assetPath}': {moveResult}");
        }

        var asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
        if (asset == null)
        {
            EnsureAssetFolderExists(assetDir);
            asset = ScriptableObject.CreateInstance<ItemDefinition>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        var icon = LoadIcon(definition.IconName);

        if (!asset.EditorApplyDefinition(
            definition.Id,
            definition.Type,
            definition.DisplayNameFallback,
            definition.DisplayNameKey,
            definition.DescriptionKey,
            definition.DescriptionFallback,
            icon,
            definition.Stackable,
            definition.MaxStack,
            definition.EquipSlotId,
            definition.InventorySlotsBonus,
            definition.EquipmentStats,
            definition.Effect,
            definition.Value,
            definition.Rarity,
            definition.Weight,
            definition.UseType,
            definition.Consumable,
            definition.Cooldown,
            definition.HP,
            definition.MP,
            definition.SP,
            definition.LP,
            definition.StatusEffects,
            definition.HasCombatDamage,
            definition.CombatDamage,
            definition.HasCombatRange,
            definition.CombatRange,
            definition.HasCombatSpeed,
            definition.CombatSpeed,
            definition.CombatTags,
            definition.CanDrop,
            definition.CanDestroy,
            definition.HasContainerSize,
            definition.ContainerSize,
            definition.Flags,
            out error))
        {
            Debug.LogError($"ItemDefinitionJsonImporter: Failed to apply '{definition.Id}': {error}");
        }
    }

    private static void EnsureAssetFolderExists(string assetDir)
    {
        if (string.IsNullOrWhiteSpace(assetDir))
            return;

        assetDir = NormalizeAssetPath(assetDir);
        if (AssetDatabase.IsValidFolder(assetDir.TrimEnd('/')))
            return;

        // Create nested folders under Assets/...
        var parts = assetDir.Trim('/').Split('/');
        if (parts.Length == 0)
            return;

        string current = parts[0]; // Should be "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string EnsureTypeFolder(string baseOutputPath, ItemType type)
    {
        baseOutputPath = NormalizeAssetPath(baseOutputPath);
        if (!baseOutputPath.EndsWith("/"))
            baseOutputPath += "/";

        var typeFolder = $"{baseOutputPath}{type}/";
        EnsureAssetFolderExists(typeFolder);
        return typeFolder;
    }

    private static string NormalizeAssetPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Assets/";
        return path.Replace('\\', '/');
    }

    private static string FindExistingAssetPath(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        // Fast path: legacy root path.
        // NOTE: We don't know outputPath here; use a broader search.
        var guids = AssetDatabase.FindAssets($"{id} t:ItemDefinition");
        foreach (var guid in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith($"/{id}.asset"))
                return p;
        }

        return null;
    }

    private Sprite LoadIcon(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            return null;

        var basePath = iconsPath ?? string.Empty;
        if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/"))
            basePath += "/";

        // First try: treat iconName as a file name under iconsPath (legacy behavior).
        // Support both with and without extension.
        string iconAssetPath = $"{basePath}{iconName}";
        if (!iconAssetPath.EndsWith(".png")
            && !iconAssetPath.EndsWith(".jpg")
            && !iconAssetPath.EndsWith(".jpeg")
            && !iconAssetPath.EndsWith(".tga")
            && !iconAssetPath.EndsWith(".psd"))
        {
            iconAssetPath += ".png";
        }

        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPath);
        if (icon != null)
            return icon;

        // Fallback: search by name under iconsPath (supports nested folders like Items/Potions/...).
        // We prefer sprites, but allow textures too.
        string nameNoExt = Path.GetFileNameWithoutExtension(iconName);
        string searchRoot = NormalizeAssetPath(iconsPath);
        if (string.IsNullOrWhiteSpace(searchRoot))
            searchRoot = "Assets";

        var guids = AssetDatabase.FindAssets($"{nameNoExt} t:Sprite", new[] { searchRoot.TrimEnd('/') });
        if (guids != null && guids.Length > 0)
        {
            var p = AssetDatabase.GUIDToAssetPath(guids[0]);
            icon = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (icon != null)
                return icon;
        }

        Debug.LogWarning($"ItemDefinitionJsonImporter: Icon '{iconName}' not found under '{searchRoot}'. Tried '{iconAssetPath}' and name search.");
        return null;
    }

    private static bool TryBuildEditorDefinition(
        ItemJson item,
        out ItemEditorDefinition result,
        out string error)
    {
        result = default;
        error = null;

        if (item == null)
        {
            error = "Item JSON is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.id))
        {
            error = "Missing 'id'.";
            return false;
        }

        if (!System.Enum.TryParse(item.type, ignoreCase: true, out ItemType itemType))
        {
            // Allow a more generic external schema.
            if (string.Equals(item.type, "Generic", System.StringComparison.OrdinalIgnoreCase))
                itemType = ItemType.Resource;
            else
            {
                error = $"Unknown item type '{item.type}'.";
                return false;
            }
        }

        ConsumableEffect effect = ConsumableEffect.DoingNothing;
        int value = item.value;

        // Prefer explicit effect/value (legacy), otherwise derive from effects block (new schema).
        if (itemType == ItemType.Consumable)
        {
            // Old JSON may contain null-like values.
            if (!string.IsNullOrWhiteSpace(item.effect)
                && item.effect != "null")
            {
                if (!System.Enum.TryParse(item.effect, ignoreCase: true, out effect))
                {
                    error = $"Unknown consumable effect '{item.effect}'.";
                    return false;
                }
            }
            else if (item.effects != null)
            {
                if (item.effects.hp.HasValue && item.effects.hp.Value != 0)
                {
                    effect = ConsumableEffect.HealHP;
                    value = item.effects.hp.Value;
                }
                else if (item.effects.mp.HasValue && item.effects.mp.Value != 0)
                {
                    effect = ConsumableEffect.RestoreMana;
                    value = item.effects.mp.Value;
                }
                else if (item.effects.sp.HasValue && item.effects.sp.Value != 0)
                {
                    effect = ConsumableEffect.RestoreStamina;
                    value = item.effects.sp.Value;
                }
            }
        }

        // Backwards-compatible field mapping.
        // Legacy JSON used 'displayName' as a key (e.g. item.gold.name).
        // New JSON can store keys in meta.nameKey/meta.descriptionKey.
        var nameKey = (item.meta != null && !string.IsNullOrWhiteSpace(item.meta.nameKey))
            ? item.meta.nameKey
            : (item.displayName ?? string.Empty);

        var descKey = (item.meta != null && !string.IsNullOrWhiteSpace(item.meta.descriptionKey))
            ? item.meta.descriptionKey
            : string.Empty;

        var descFallback = (item.meta != null ? item.meta.description : null) ?? string.Empty;

        var iconName = (item.meta != null && !string.IsNullOrWhiteSpace(item.meta.icon))
            ? item.meta.icon
            : item.icon;

        bool stackable = item.meta != null ? item.meta.stackable : (itemType != ItemType.Equipment);
        int maxStack = item.meta != null && item.meta.maxStack > 0 ? item.meta.maxStack : 99;

        string equipSlotId = item.equipment != null ? (item.equipment.slot ?? string.Empty) : string.Empty;
        int inventorySlotsBonus = 0;
        if (item.equipment != null)
        {
            if (item.equipment.inventorySlotsBonus != 0)
                inventorySlotsBonus = item.equipment.inventorySlotsBonus;
        }
        // Alternate bag capacity location from a more "world"-oriented schema.
        if (inventorySlotsBonus == 0 && item.world != null && item.world.containerSize.HasValue && item.world.containerSize.Value > 0)
            inventorySlotsBonus = item.world.containerSize.Value;

        // Prefer economy.value if present.
        if (item.economy != null && item.economy.value.HasValue)
            value = item.economy.value.Value;

        string rarity = item.meta != null ? (item.meta.rarity ?? string.Empty) : string.Empty;
        float weight = item.economy != null && item.economy.weight.HasValue ? item.economy.weight.Value : 0f;

        string useType = item.usage != null ? (item.usage.useType ?? string.Empty) : string.Empty;
        bool consumable = item.usage != null && item.usage.consumable.HasValue ? item.usage.consumable.Value : (itemType == ItemType.Consumable);
        int cooldown = item.usage != null && item.usage.cooldown.HasValue ? item.usage.cooldown.Value : 0;

        int hp = item.effects != null && item.effects.hp.HasValue ? item.effects.hp.Value : 0;
        int mp = item.effects != null && item.effects.mp.HasValue ? item.effects.mp.Value : 0;
        int sp = item.effects != null && item.effects.sp.HasValue ? item.effects.sp.Value : 0;
        int lp = item.effects != null && item.effects.lp.HasValue ? item.effects.lp.Value : 0;
        var statusEffects = ParseStatusEffects(item.id, item.effects != null ? item.effects.statuses : null);

        bool hasCombatDamage = item.combat != null && item.combat.damage.HasValue;
        float combatDamage = item.combat != null && item.combat.damage.HasValue ? item.combat.damage.Value : 0f;
        bool hasCombatRange = item.combat != null && item.combat.range.HasValue;
        float combatRange = item.combat != null && item.combat.range.HasValue ? item.combat.range.Value : 0f;
        bool hasCombatSpeed = item.combat != null && item.combat.speed.HasValue;
        float combatSpeed = item.combat != null && item.combat.speed.HasValue ? item.combat.speed.Value : 0f;
        string[] combatTags = item.combat != null && item.combat.tags != null ? item.combat.tags.ToArray() : new string[0];

        bool canDrop = item.world == null || !item.world.canDrop.HasValue ? true : item.world.canDrop.Value;
        bool canDestroy = item.world != null && item.world.canDestroy.HasValue ? item.world.canDestroy.Value : false;
        bool hasContainerSize = item.world != null && item.world.containerSize.HasValue;
        int containerSize = item.world != null && item.world.containerSize.HasValue ? item.world.containerSize.Value : 0;

        // Equipment stats (dictionary -> serializable array)
        var equipmentStats = new List<ItemDefinition.StatEntry>();
        if (item.equipment != null && item.equipment.stats != null)
        {
            foreach (var kv in item.equipment.stats)
            {
                equipmentStats.Add(new ItemDefinition.StatEntry
                {
                    key = kv.Key,
                    value = kv.Value
                });
            }
        }

        string[] flags = item.flags != null ? item.flags.ToArray() : new string[0];

        result = new ItemEditorDefinition(
            id: item.id,
            type: itemType,
            displayNameKey: nameKey,
            descriptionKey: descKey,
            descriptionFallback: descFallback,
            iconName: iconName,
            stackable: stackable,
            maxStack: maxStack,
            equipSlotId: equipSlotId,
            inventorySlotsBonus: inventorySlotsBonus,
            equipmentStats: equipmentStats.ToArray(),
            effect: effect,
            value: value,
            rarity: rarity,
            weight: weight,
            useType: useType,
            consumable: consumable,
            cooldown: cooldown,
            hp: hp,
            mp: mp,
            sp: sp,
            lp: lp,
            statusEffects: statusEffects,
            hasCombatDamage: hasCombatDamage,
            combatDamage: combatDamage,
            hasCombatRange: hasCombatRange,
            combatRange: combatRange,
            hasCombatSpeed: hasCombatSpeed,
            combatSpeed: combatSpeed,
            combatTags: combatTags,
            canDrop: canDrop,
            canDestroy: canDestroy,
            hasContainerSize: hasContainerSize,
            containerSize: containerSize,
            flags: flags
        );
        return true;
    }

    private static ItemDefinition.StatusEffectGrant[] ParseStatusEffects(string itemId, JToken statusesToken)
    {
        if (statusesToken == null || statusesToken.Type == JTokenType.Null)
            return System.Array.Empty<ItemDefinition.StatusEffectGrant>();

        if (statusesToken.Type != JTokenType.Array)
        {
            Debug.LogWarning($"ItemDefinitionJsonImporter: effects.statuses for '{itemId}' is not an array; ignoring.");
            return System.Array.Empty<ItemDefinition.StatusEffectGrant>();
        }

        var array = (JArray)statusesToken;
        if (array.Count == 0)
            return System.Array.Empty<ItemDefinition.StatusEffectGrant>();

        var grants = new List<ItemDefinition.StatusEffectGrant>(array.Count);

        for (int i = 0; i < array.Count; i++)
        {
            var token = array[i];
            if (token == null || token.Type == JTokenType.Null)
                continue;

            // Supported formats:
            // 1) "Burning" (legacy shorthand)
            // 2) { "id": "Burning", "turns": 2 }
            // 3) { "status": "Burning", "duration": 2 } (alternative keys)
            string idRaw = null;
            int turns = 1;

            if (token.Type == JTokenType.String)
            {
                idRaw = token.Value<string>();
            }
            else if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                idRaw = (string)obj["id"] ?? (string)obj["status"] ?? (string)obj["type"];

                var turnsToken = obj["turns"] ?? obj["duration"] ?? obj["durationTurns"];
                if (turnsToken != null && turnsToken.Type == JTokenType.Integer)
                    turns = turnsToken.Value<int>();
            }
            else
            {
                Debug.LogWarning($"ItemDefinitionJsonImporter: Unknown statuses entry type for '{itemId}' at index {i}; ignoring.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(idRaw))
                continue;

            // FIXME: If battle statuses change/expand, verify JSON ids still map correctly.
            if (!System.Enum.TryParse(idRaw.Trim(), ignoreCase: true, out StatusEffectId parsed))
            {
                Debug.LogWarning($"ItemDefinitionJsonImporter: Unknown StatusEffectId '{idRaw}' for '{itemId}'; ignoring.");
                continue;
            }

            if (turns < 0)
                turns = 0;

            grants.Add(new ItemDefinition.StatusEffectGrant { id = parsed, turns = turns });
        }

        return grants.Count == 0 ? System.Array.Empty<ItemDefinition.StatusEffectGrant>() : grants.ToArray();
    }

    private readonly struct ItemEditorDefinition
    {
        public string Id { get; }
        public ItemType Type { get; }
        public string DisplayNameKey { get; }
        public string DescriptionKey { get; }
        public string DescriptionFallback { get; }
        public string DisplayNameFallback { get; }
        public string IconName { get; }
        public bool Stackable { get; }
        public int MaxStack { get; }
        public string EquipSlotId { get; }
        public int InventorySlotsBonus { get; }
        public ItemDefinition.StatEntry[] EquipmentStats { get; }
        public ConsumableEffect Effect { get; }
        public int Value { get; }

        public string Rarity { get; }
        public float Weight { get; }

        public string UseType { get; }
        public bool Consumable { get; }
        public int Cooldown { get; }

        public int HP { get; }
        public int MP { get; }
        public int SP { get; }
        public int LP { get; }
        public ItemDefinition.StatusEffectGrant[] StatusEffects { get; }

        public bool HasCombatDamage { get; }
        public float CombatDamage { get; }
        public bool HasCombatRange { get; }
        public float CombatRange { get; }
        public bool HasCombatSpeed { get; }
        public float CombatSpeed { get; }
        public string[] CombatTags { get; }

        public bool CanDrop { get; }
        public bool CanDestroy { get; }
        public bool HasContainerSize { get; }
        public int ContainerSize { get; }

        public string[] Flags { get; }

        public ItemEditorDefinition(
            string id,
            ItemType type,
            string displayNameKey,
            string descriptionKey,
            string descriptionFallback,
            string displayNameFallback,
            string iconName,
            bool stackable,
            int maxStack,
            string equipSlotId,
            int inventorySlotsBonus,
            ItemDefinition.StatEntry[] equipmentStats,
            ConsumableEffect effect,
            int value,
            string rarity,
            float weight,
            string useType,
            bool consumable,
            int cooldown,
            int hp,
            int mp,
            int sp,
            int lp,
            ItemDefinition.StatusEffectGrant[] statusEffects,
            bool hasCombatDamage,
            float combatDamage,
            bool hasCombatRange,
            float combatRange,
            bool hasCombatSpeed,
            float combatSpeed,
            string[] combatTags,
            bool canDrop,
            bool canDestroy,
            bool hasContainerSize,
            int containerSize,
            string[] flags)
        {
            Id = id;
            Type = type;
            DisplayNameKey = displayNameKey ?? string.Empty;
            DescriptionKey = descriptionKey ?? string.Empty;
            DescriptionFallback = descriptionFallback ?? string.Empty;
            DisplayNameFallback = displayNameFallback ?? string.Empty;
            IconName = iconName;
            Stackable = stackable;
            MaxStack = maxStack;
            EquipSlotId = equipSlotId ?? string.Empty;
            InventorySlotsBonus = inventorySlotsBonus;
            EquipmentStats = equipmentStats;
            Effect = effect;
            Value = value;

            Rarity = rarity ?? string.Empty;
            Weight = weight;

            UseType = useType ?? string.Empty;
            Consumable = consumable;
            Cooldown = cooldown;

            HP = hp;
            MP = mp;
            SP = sp;
            LP = lp;
            StatusEffects = statusEffects ?? System.Array.Empty<ItemDefinition.StatusEffectGrant>();

            HasCombatDamage = hasCombatDamage;
            CombatDamage = combatDamage;
            HasCombatRange = hasCombatRange;
            CombatRange = combatRange;
            HasCombatSpeed = hasCombatSpeed;
            CombatSpeed = combatSpeed;
            CombatTags = combatTags;

            CanDrop = canDrop;
            CanDestroy = canDestroy;
            HasContainerSize = hasContainerSize;
            ContainerSize = containerSize;

            Flags = flags;
        }

        public ItemEditorDefinition(
            string id,
            ItemType type,
            string displayNameKey,
            string descriptionKey,
            string descriptionFallback,
            string iconName,
            bool stackable,
            int maxStack,
            string equipSlotId,
            int inventorySlotsBonus,
            ItemDefinition.StatEntry[] equipmentStats,
            ConsumableEffect effect,
            int value,
            string rarity,
            float weight,
            string useType,
            bool consumable,
            int cooldown,
            int hp,
            int mp,
            int sp,
            int lp,
            ItemDefinition.StatusEffectGrant[] statusEffects,
            bool hasCombatDamage,
            float combatDamage,
            bool hasCombatRange,
            float combatRange,
            bool hasCombatSpeed,
            float combatSpeed,
            string[] combatTags,
            bool canDrop,
            bool canDestroy,
            bool hasContainerSize,
            int containerSize,
            string[] flags)
            : this(
                id,
                type,
                displayNameKey,
                descriptionKey,
                descriptionFallback,
                displayNameFallback: string.Empty,
                iconName,
                stackable,
                maxStack,
                equipSlotId,
                inventorySlotsBonus,
                equipmentStats,
                effect,
                value,
                rarity,
                weight,
                useType,
                consumable,
                cooldown,
                hp,
                mp,
                sp,
                lp,
                statusEffects,
                hasCombatDamage,
                combatDamage,
                hasCombatRange,
                combatRange,
                hasCombatSpeed,
                combatSpeed,
                combatTags,
                canDrop,
                canDestroy,
                hasContainerSize,
                containerSize,
                flags)
        {
        }
    }

    #region JSON DTOs

    [System.Serializable]
    public class ItemJson
    {
        public string id;
        public string type;

        // Legacy fields (still supported)
        public string displayName;
        public string icon;
        public string effect;
        public int value;

        // New schema (optional)
        public ItemMetaJson meta;
        public ItemEconomyJson economy;
        public ItemUsageJson usage;
        public ItemEffectsJson effects;
        public ItemEquipmentJson equipment;
        public ItemCombatJson combat;
        public ItemWorldJson world;
        public List<string> flags;
    }

    [System.Serializable]
    public class ItemMetaJson
    {
        public string nameKey;
        public string descriptionKey;
        public string icon;
        public string rarity;
        public bool stackable = true;
        public int maxStack = 99;
        public string description;
    }

    [System.Serializable]
    public class ItemEconomyJson
    {
        public int? value;
        public float? weight;
    }

    [System.Serializable]
    public class ItemUsageJson
    {
        public string useType;
        public bool? consumable;
        public int? cooldown;
    }

    [System.Serializable]
    public class ItemEffectsJson
    {
        public int? hp;
        public int? mp;
        public int? sp;
        public int? lp;
        public JToken statuses;
    }

    [System.Serializable]
    public class ItemEquipmentJson
    {
        public string slot;

        // For bags
        public int inventorySlotsBonus;

        // Full stats support via Json.NET; Unity can't serialize dictionaries directly, so we convert to StatEntry[]
        public Dictionary<string, float> stats;
    }

    [System.Serializable]
    public class ItemCombatJson
    {
        public float? damage;
        public float? range;
        public float? speed;
        public List<string> tags;
    }

    [System.Serializable]
    public class ItemWorldJson
    {
        public bool? canDrop;
        public bool? canDestroy;
        public int? containerSize;
    }

    #endregion
}

/*
#if UNITY_EDITOR
public void EditorApplyDefinition(ItemDefinitionData data)
{
    ...
}
#endif
*/
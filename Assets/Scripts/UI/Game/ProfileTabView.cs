using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    public sealed class ProfileTabView : MonoBehaviour
    {
        [Serializable]
        public sealed class OutfitSpriteEntry
        {
            public string outfitId;
            public Sprite sprite;
        }

        [Header("Wiring")]
        [SerializeField] private Image characterImage;
        [SerializeField] private EquipmentSlotView[] equipmentSlots;

        [Header("Optional: data")]
        [Tooltip("Optional. Assign the ItemDatabase asset to resolve names/icons. Kept as Object to avoid assembly reference coupling.")]
        [SerializeField] private UnityEngine.Object itemDatabase;

        [Tooltip("Optional. If assigned, overrides the outfit sprite mapping below (default + outfitId->sprite table in an asset).")]
        [SerializeField] private ProfileOutfitSpriteConfig outfitSpriteConfig;

        [Header("Outfit")]
        [Tooltip("Sprite shown when outfit sprite is missing/unresolved.")]
        [SerializeField] private Sprite defaultCharacterSprite;
        [Tooltip("OutfitId -> Sprite mapping for the center character image.")]
        [SerializeField] private OutfitSpriteEntry[] outfitSprites;

        [Header("Debug")]
        [SerializeField] private bool logSlotClicks = true;

        public event Action<EquipmentSlotId> SlotClicked;

        private void Awake()
        {
            if (equipmentSlots != null)
            {
                for (int i = 0; i < equipmentSlots.Length; i++)
                {
                    var slot = equipmentSlots[i];
                    if (slot == null)
                        continue;

                    slot.Clicked += OnSlotClicked;
                }
            }
        }

        private void OnDestroy()
        {
            if (equipmentSlots != null)
            {
                for (int i = 0; i < equipmentSlots.Length; i++)
                {
                    var slot = equipmentSlots[i];
                    if (slot == null)
                        continue;

                    slot.Clicked -= OnSlotClicked;
                }
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            var save = global::GameState.Instance != null ? global::GameState.Instance.CurrentSave : null;
            var player = save != null ? save.player : null;

            RefreshCharacter(player);

            if (equipmentSlots == null)
                return;

            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                var slotView = equipmentSlots[i];
                if (slotView == null)
                    continue;

                if (player == null || player.equipment == null)
                {
                    slotView.RenderEmpty();
                    continue;
                }

                string equippedItemId = GetEquippedItemId(player.equipment, slotView.SlotId);
                if (string.IsNullOrEmpty(equippedItemId))
                {
                    slotView.RenderEmpty();
                    continue;
                }

                if (itemDatabase != null)
                {
                    if (TryResolveItemFromDatabase(itemDatabase, equippedItemId, out var displayName, out var icon))
                        slotView.RenderItem(displayName, icon);
                    else
                        slotView.RenderItem(equippedItemId, null);
                }
                else
                {
                    slotView.RenderItem(equippedItemId, null);
                }
            }
        }

        private void RefreshCharacter(SaveData.Player player)
        {
            if (characterImage == null)
                return;

            var outfitId = player != null ? player.outfitId : null;
            var sprite = ResolveOutfitSprite(outfitId);
            characterImage.sprite = sprite;
            characterImage.enabled = sprite != null;
        }

        private Sprite ResolveOutfitSprite(string outfitId)
        {
            if (outfitSpriteConfig != null)
                return outfitSpriteConfig.Resolve(outfitId);

            if (!string.IsNullOrEmpty(outfitId) && outfitSprites != null)
            {
                for (int i = 0; i < outfitSprites.Length; i++)
                {
                    var entry = outfitSprites[i];
                    if (entry == null)
                        continue;

                    if (!string.IsNullOrEmpty(entry.outfitId)
                        && string.Equals(entry.outfitId, outfitId, StringComparison.OrdinalIgnoreCase)
                        && entry.sprite != null)
                    {
                        return entry.sprite;
                    }
                }
            }

            return defaultCharacterSprite;
        }

        // Cached reflection for itemDatabase — avoids per-slot GetMethod/GetProperty on every Refresh.
        private static Type s_dbType;
        private static MethodInfo s_getByIdMethod;
        private static Type s_defType;
        private static PropertyInfo s_displayNameProp;
        private static PropertyInfo s_iconProp;

        private static bool TryResolveItemFromDatabase(UnityEngine.Object db, string itemId, out string displayName, out Sprite icon)
        {
            displayName = null;
            icon = null;

            if (db == null || string.IsNullOrEmpty(itemId))
                return false;

            try
            {
                var dbType = db.GetType();
                if (s_dbType != dbType)
                {
                    s_dbType = dbType;
                    s_getByIdMethod = dbType.GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public);
                    s_defType = null;
                    s_displayNameProp = null;
                    s_iconProp = null;
                }

                if (s_getByIdMethod == null)
                    return false;

                var def = s_getByIdMethod.Invoke(db, new object[] { itemId });
                if (def == null)
                    return false;

                var defType = def.GetType();
                if (s_defType != defType)
                {
                    s_defType = defType;
                    s_displayNameProp = defType.GetProperty("DisplayName", BindingFlags.Instance | BindingFlags.Public);
                    s_iconProp = defType.GetProperty("Icon", BindingFlags.Instance | BindingFlags.Public);
                }

                displayName = s_displayNameProp != null ? s_displayNameProp.GetValue(def) as string : null;
                icon = s_iconProp != null ? s_iconProp.GetValue(def) as Sprite : null;

                if (string.IsNullOrEmpty(displayName))
                    displayName = itemId;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetEquippedItemId(SaveData.Player.Equipment equipment, EquipmentSlotId slotId)
        {
            if (equipment == null)
                return null;

            return slotId switch
            {
                EquipmentSlotId.Bag => equipment.bagItemId,
                EquipmentSlotId.Ring1 => equipment.ring1ItemId,
                EquipmentSlotId.Ring2 => equipment.ring2ItemId,
                EquipmentSlotId.Amulet => equipment.amuletItemId,
                EquipmentSlotId.Weapon => equipment.weaponItemId,
                EquipmentSlotId.Helmet => equipment.helmetItemId,
                EquipmentSlotId.Armor => equipment.armorItemId,
                EquipmentSlotId.Pants => equipment.pantsItemId,
                EquipmentSlotId.Boots => equipment.bootsItemId,
                _ => null,
            };
        }

        private void OnSlotClicked(EquipmentSlotId slotId)
        {
            if (logSlotClicks)
            {
                var save = global::GameState.Instance != null ? global::GameState.Instance.CurrentSave : null;
                var player = save != null ? save.player : null;
                var equipped = player != null && player.equipment != null
                    ? GetEquippedItemId(player.equipment, slotId)
                    : null;
                UDA2.Logging.Logger.LogInfo($"[ProfileTabView] Slot clicked: {slotId} (equipped='{(string.IsNullOrEmpty(equipped) ? "<empty>" : equipped)}')", UDA2.Logging.LogChannel.UI);
            }
            SlotClicked?.Invoke(slotId);
        }
    }
}

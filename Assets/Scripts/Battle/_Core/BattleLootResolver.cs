using System;
using System.Collections.Generic;
using UnityEngine;
using UDA2.GameTime;

namespace Game.Battle
{
    public static class BattleLootResolver
    {
        public readonly struct LootResult
        {
            public int GoldGained { get; }
            public int ManaCrystalsGained { get; }
            public int DemonCrystalsGained { get; }
            public int ExpGained { get; }
            public IReadOnlyList<BattleResultData.ItemReward> Items { get; }

            public LootResult(int goldGained, int manaCrystalsGained, int demonCrystalsGained, int expGained, IReadOnlyList<BattleResultData.ItemReward> items)
            {
                GoldGained = goldGained;
                ManaCrystalsGained = manaCrystalsGained;
                DemonCrystalsGained = demonCrystalsGained;
                ExpGained = expGained;
                Items = items;
            }
        }

        public static LootResult Resolve(EnemyData enemy, SaveData save, System.Random rng = null)
        {
            if (rng == null)
                rng = new System.Random();

            int level = Mathf.Max(1, save?.player?.level ?? 1);
            float multiplier = ComputeLootMultiplier(save, level);

            int gold = 0;
            int manaCrystals = 0;
            int demonCrystals = 0;
            var items = new List<BattleResultData.ItemReward>(8);

            if (enemy != null && enemy.lootTable != null)
            {
                for (int i = 0; i < enemy.lootTable.Length; i++)
                {
                    var entry = enemy.lootTable[i];
                    if (entry == null)
                        continue;

                    if (entry.dropChance <= 0f)
                        continue;

                    if (entry.dropChance < 1f)
                    {
                        double roll = rng.NextDouble();
                        if (roll > entry.dropChance)
                            continue;
                    }

                    string itemId = ResolveItemId(entry);
                    if (string.IsNullOrWhiteSpace(itemId))
                        continue;

                    int min = Mathf.Max(0, entry.minCount);
                    int max = Mathf.Max(min, entry.maxCount);
                    int baseCount = rng.Next(min, max + 1);

                    // Apply multiplier after base roll.
                    int finalCount = Mathf.FloorToInt(baseCount * multiplier);

                    if (TryApplyCurrency(itemId, finalCount, ref gold, ref manaCrystals, ref demonCrystals))
                    {
                        continue;
                    }

                    // Non-currency: if the item was rolled (baseCount>0), never reduce to 0.
                    if (baseCount > 0)
                        finalCount = Mathf.Max(1, finalCount);

                    if (finalCount <= 0)
                        continue;

                    items.Add(new BattleResultData.ItemReward(itemId, finalCount));
                }
            }

            int exp = Mathf.Max(0, enemy != null ? enemy.expReward : 0);
            if (exp > 0)
                exp = Mathf.FloorToInt(exp * ComputeExpMultiplier(level));

            return new LootResult(gold, manaCrystals, demonCrystals, exp, items);
        }

        private static bool TryApplyCurrency(string itemId, int amount, ref int gold, ref int manaCrystals, ref int demonCrystals)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            // Currency can be reduced to 0 by multipliers.
            if (amount <= 0)
                return IsCurrencyId(itemId);

            if (IsGoldId(itemId))
            {
                gold += amount;
                return true;
            }

            if (IsManaCrystalId(itemId))
            {
                manaCrystals += amount;
                return true;
            }

            if (IsDemonCrystalId(itemId))
            {
                demonCrystals += amount;
                return true;
            }

            return false;
        }

        private static bool IsCurrencyId(string itemId)
        {
            return IsGoldId(itemId) || IsManaCrystalId(itemId) || IsDemonCrystalId(itemId);
        }

        private static bool IsGoldId(string itemId)
        {
            return string.Equals(itemId, "gold", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsManaCrystalId(string itemId)
        {
            return string.Equals(itemId, "mana_crystal", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(itemId, "mana_crystals", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(itemId, "manaCrystal", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDemonCrystalId(string itemId)
        {
            return string.Equals(itemId, "demon_crystal", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(itemId, "demon_crystals", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(itemId, "demonic_crystal", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(itemId, "demonic_crystals", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveItemId(EnemyData.LootDrop entry)
        {
            if (entry == null)
                return null;

            if (!string.IsNullOrWhiteSpace(entry.itemId))
                return entry.itemId.Trim();

            if (entry.item == null)
                return null;

            // Reflection to avoid assembly reference coupling.
            try
            {
                var t = entry.item.GetType();
                var prop = t.GetProperty("Id");
                if (prop == null)
                    return null;

                var value = prop.GetValue(entry.item) as string;
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static float ComputeLootMultiplier(SaveData save, int level)
        {
            float mult = 1f;

            // Level multiplier (player up to 20 lvl).
            mult *= ComputeLevelLootMultiplier(level);

            // Time-of-day multiplier: 22:00..08:00 => bonus.
            var minute = save?.time != null ? Mathf.Clamp(save.time.minuteOfDay, 0, 1439) : 8 * 60;
            var phase = GameTimePhaseResolver.GetTimeOfDayPhase(minute);
            if (phase == TimeOfDayPhase.Night)
            {
                // FIXME: tune night loot multiplier.
                mult *= 1.25f;
            }

            // Special night (00:00..04:59, every 7 days) => extra bonus.
            var day = save?.time != null ? Mathf.Max(1, save.time.day) : 1;
            var special = GameTimePhaseResolver.GetNightSpecialPhase(day, minute);
            if (special != NightSpecialPhase.None)
            {
                // FIXME: tune special night loot multiplier (and maybe depend on special type).
                mult *= 1.5f;
            }

            return Mathf.Max(0f, mult);
        }

        private static float ComputeLevelLootMultiplier(int level)
        {
            // Example: level 1 => 0.5, level 20 => 1.0 (linear).
            // FIXME: convert to a curve/table if needed.
            level = Mathf.Clamp(level, 1, 20);
            float t = (level - 1) / 19f;
            return Mathf.Lerp(0.5f, 1.0f, t);
        }

        private static float ComputeExpMultiplier(int level)
        {
            // Keep EXP scaling separate in case you want different rules later.
            return 1f;
        }
    }
}

using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Player EXP/level progression rules.
    ///
    /// SaveData stores:
    /// - player.level: current level (1..MaxLevel)
    /// - player.exp: EXP progress within the current level (0..ExpToNextLevel-1)
    /// </summary>
    public static class PlayerExperience
    {
        public const int MaxLevel = 20;

        // Index is the current level. Value is EXP required to reach next level.
        // Level 20 is a cap (0 EXP to next).
        // Slower curve (tune as needed). Value is EXP required to reach next level.
        private static readonly int[] ExpToNextByLevel =
        {
            0,    // unused (level 0)
            200,   // 1 -> 2
            300,   // 2 -> 3
            450,   // 3 -> 4
            650,   // 4 -> 5
            900,   // 5 -> 6
            1200,  // 6 -> 7
            1600,  // 7 -> 8
            2050,  // 8 -> 9
            2600,  // 9 -> 10
            3250,  // 10 -> 11
            4000,  // 11 -> 12
            4850,  // 12 -> 13
            5800,  // 13 -> 14
            6850,  // 14 -> 15
            8000,  // 15 -> 16
            9250,  // 16 -> 17
            10600, // 17 -> 18
            12050, // 18 -> 19
            13600, // 19 -> 20
            0     // 20 -> cap
        };

        public static int ClampLevel(int level)
        {
            return Mathf.Clamp(level, 1, MaxLevel);
        }

        public static int GetExpToNextLevel(int level)
        {
            level = ClampLevel(level);
            return ExpToNextByLevel[level];
        }

        public static int GetExpMaxForLevel(int level)
        {
            return GetExpToNextLevel(level);
        }

        /// <summary>
        /// Adds EXP to the player, performing level-ups up to MaxLevel.
        /// Returns EXP actually applied (can be lower if already at level cap).
        /// </summary>
        public static int AddExp(ref int level, ref int exp, int expToAdd, out int levelsGained)
        {
            levelsGained = 0;

            if (expToAdd <= 0)
            {
                Normalize(ref level, ref exp);
                return 0;
            }

            Normalize(ref level, ref exp);

            int applied = 0;

            while (expToAdd > 0)
            {
                if (level >= MaxLevel)
                {
                    // Level cap.
                    level = MaxLevel;
                    exp = 0;
                    break;
                }

                int expToNext = GetExpToNextLevel(level);
                if (expToNext <= 0)
                {
                    level = MaxLevel;
                    exp = 0;
                    break;
                }

                int remainingInLevel = Mathf.Max(0, expToNext - exp);

                if (expToAdd < remainingInLevel)
                {
                    exp += expToAdd;
                    applied += expToAdd;
                    expToAdd = 0;
                }
                else
                {
                    // Fill current level and level-up.
                    exp += remainingInLevel;
                    applied += remainingInLevel;
                    expToAdd -= remainingInLevel;

                    level++;
                    levelsGained++;
                    exp = 0;

                    // Safety clamp in case of weird values.
                    level = ClampLevel(level);
                }
            }

            Normalize(ref level, ref exp);
            return applied;
        }

        /// <summary>
        /// Ensures level/exp are within expected ranges.
        /// Also converts overflow EXP into level-ups (if any).
        /// </summary>
        public static void Normalize(ref int level, ref int exp)
        {
            level = ClampLevel(level);
            if (exp < 0)
                exp = 0;

            // Convert overflow EXP into level-ups.
            while (level < MaxLevel)
            {
                int expToNext = GetExpToNextLevel(level);
                if (expToNext <= 0)
                    break;

                if (exp < expToNext)
                    break;

                exp -= expToNext;
                level++;
                level = ClampLevel(level);
            }

            // At cap: keep exp at 0.
            if (level >= MaxLevel)
            {
                level = MaxLevel;
                exp = 0;
            }

            // Clamp exp to valid range for UI (0..expToNext-1).
            int max = GetExpToNextLevel(level);
            if (max > 0)
                exp = Mathf.Clamp(exp, 0, max - 1);
            else
                exp = 0;
        }
    }
}

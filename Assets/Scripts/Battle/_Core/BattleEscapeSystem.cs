//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//

using UnityEngine;
using Game.Battle.Combat;

namespace Game.Battle
{
    public sealed class BattleEscapeSystem
    {
        private readonly float minEscapeChance;
        private readonly float maxEscapeChance;
        private readonly float escapeStaminaWeight;
        private readonly float escapeLustWeight;

        public BattleEscapeSystem(float minEscapeChance, float maxEscapeChance, float escapeStaminaWeight, float escapeLustWeight)
        {
            this.minEscapeChance = minEscapeChance;
            this.maxEscapeChance = maxEscapeChance;
            this.escapeStaminaWeight = escapeStaminaWeight;
            this.escapeLustWeight = escapeLustWeight;
        }

        public float CalculateEscapeChance01(BattleContext context, CombatState combatState)
        {
            if (context == null || combatState == null)
                return Mathf.Clamp01(minEscapeChance);

            static float Safe01(int value, int max)
            {
                if (max <= 0)
                    return 0f;
                return Mathf.Clamp01((float)value / max);
            }

            float playerStamina01 = Safe01(combatState.PlayerSp, context.Player != null ? context.Player.MaxSP : 0);
            float enemyStamina01 = Safe01(combatState.EnemySp, context.Enemy != null ? context.Enemy.maxSp : 0);

            float playerLust01 = Safe01(combatState.PlayerLp, context.Player != null ? context.Player.MaxLP : 0);
            float enemyLust01 = Safe01(combatState.EnemyLp, context.Enemy != null ? context.Enemy.maxLp : 0);

            float staminaScore01 = Mathf.Clamp01(0.5f + 0.5f * (playerStamina01 - enemyStamina01));
            float lustScore01 = Mathf.Clamp01(1f - 0.5f * (playerLust01 + enemyLust01));

            float wSum = Mathf.Max(0.0001f, escapeStaminaWeight + escapeLustWeight);
            float combined01 = (escapeStaminaWeight * staminaScore01 + escapeLustWeight * lustScore01) / wSum;

            float lo = Mathf.Clamp01(minEscapeChance);
            float hi = Mathf.Clamp01(Mathf.Max(minEscapeChance, maxEscapeChance));
            return Mathf.Clamp01(Mathf.Lerp(lo, hi, combined01));
        }

        public bool TryRollEscape(System.Random rng, BattleContext context, CombatState combatState, out float chance, out float roll)
        {
            chance = CalculateEscapeChance01(context, combatState);
            roll = rng != null ? (float)rng.NextDouble() : 1f;
            return roll <= chance;
        }

        public CombatState ApplyFailedEscapePenalty(BattleContext context, CombatState combatState, int lpPenalty)
        {
            if (combatState == null)
                return null;

            int maxLp = context?.Player != null ? Mathf.Max(0, context.Player.MaxLP) : 0;
            int nextLp = combatState.PlayerLp + Mathf.Max(0, lpPenalty);
            if (maxLp > 0)
                nextLp = Mathf.Min(nextLp, maxLp);

            return combatState.WithPlayerLp(nextLp);
        }

        public bool IsPlayerLpDefeat(BattleContext context, CombatState combatState)
        {
            int maxLp = context?.Player != null ? Mathf.Max(0, context.Player.MaxLP) : 0;
            return maxLp > 0 && combatState != null && combatState.PlayerLp >= maxLp;
        }
    }
}

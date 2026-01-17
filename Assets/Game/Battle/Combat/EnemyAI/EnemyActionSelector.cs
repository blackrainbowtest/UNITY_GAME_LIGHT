using System;
using System.Collections.Generic;
using Game.Battle.Combat.Actions;

namespace Game.Battle.Combat.EnemyAI
{
    public static class EnemyActionSelector
    {
        public static CombatActionId? SelectEnemyAction(
            EnemyDifficulty difficulty,
            EnemyData enemy,
            CombatActionRegistry registry,
            CombatState state,
            Random rng)
        {
            if (enemy == null || registry == null || state == null)
                return null;

            var allowed = enemy.allowedActions;
            if (allowed == null || allowed.Length == 0)
            {
                allowed = new[]
                {
                    CombatActionId.FastAttack,
                    CombatActionId.NormalAttack,
                    CombatActionId.HeavyAttack
                };
            }

            var candidates = new List<CombatActionData>(allowed.Length);
            foreach (var id in allowed)
            {
                var action = registry.Get(id);
                if (action == null)
                    continue;

                // Enemy turn: only meaningful if it affects the player for now.
                if (action.HpDamage <= 0)
                    continue;

                // Don't let enemy use "player-only" gated actions.
                if (action.RequiresPlayerBlockedLastTurn)
                    continue;

                // Must be affordable for enemy.
                if (state.EnemyMp < action.MpCost || state.EnemySp < action.SpCost || state.EnemyLp < action.LpCost)
                    continue;

                candidates.Add(action);
            }

            if (candidates.Count == 0)
                return null;

            // Partition into weak/strong by min/max damage.
            // This matches the design: "weak quick" vs "strong".
            var minDamage = int.MaxValue;
            var maxDamage = int.MinValue;
            foreach (var c in candidates)
            {
                if (c.HpDamage < minDamage) minDamage = c.HpDamage;
                if (c.HpDamage > maxDamage) maxDamage = c.HpDamage;
            }

            var weak = new List<CombatActionData>();
            var strong = new List<CombatActionData>();
            foreach (var c in candidates)
            {
                if (c.HpDamage == minDamage)
                    weak.Add(c);
                if (c.HpDamage == maxDamage)
                    strong.Add(c);
            }

            switch (difficulty)
            {
                case EnemyDifficulty.Easy:
                    return ChooseWeighted(rng, weak, strong, weakChance: 0.80f)?.Id;

                case EnemyDifficulty.Normal:
                    return ChooseWeighted(rng, weak, strong, weakChance: 0.60f)?.Id;

                case EnemyDifficulty.Hard:
                    return ChooseMostEffective(candidates)?.Id;

                default:
                    return ChooseMostEffective(candidates)?.Id;
            }
        }

        private static CombatActionData ChooseWeighted(Random rng, List<CombatActionData> weak, List<CombatActionData> strong, float weakChance)
        {
            if (rng == null)
                rng = new Random();

            // If one side is empty, fallback to the other.
            if (weak.Count == 0)
                return ChooseRandom(rng, strong);
            if (strong.Count == 0)
                return ChooseRandom(rng, weak);

            var roll = rng.NextDouble();
            if (roll < weakChance)
                return ChooseRandom(rng, weak);

            return ChooseRandom(rng, strong);
        }

        private static CombatActionData ChooseMostEffective(List<CombatActionData> candidates)
        {
            CombatActionData best = null;
            var bestScore = int.MinValue;

            foreach (var c in candidates)
            {
                // "Maximally effective" for MVP: maximize raw damage.
                // Later we can improve with status effects, resistances, etc.
                var score = c.HpDamage;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            return best;
        }

        private static CombatActionData ChooseRandom(Random rng, List<CombatActionData> list)
        {
            if (list == null || list.Count == 0)
                return null;

            var index = rng.Next(0, list.Count);
            return list[index];
        }
    }
}

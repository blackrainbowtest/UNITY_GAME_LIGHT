//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\Combat\EnemyAI\EnemyActionSelector.cs                                      */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:38:20 by UDA                                                                    */
/*   Updated: 2026/01/23 01:38:20 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

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
                    return ChooseMostEffective(enemy, state, candidates, rng)?.Id;

                default:
                    return ChooseMostEffective(enemy, state, candidates, rng)?.Id;
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

        private static CombatActionData ChooseMostEffective(EnemyData enemy, CombatState state, List<CombatActionData> candidates, Random rng)
        {
            if (candidates == null || candidates.Count == 0)
                return null;

            // 1) If we can kill the player now, do it.
            // Among lethal options, prefer the lowest relative cost (so hard AI doesn't waste resources).
            CombatActionData bestLethal = null;
            var bestLethalScore = double.NegativeInfinity;
            foreach (var c in candidates)
            {
                if (c.HpDamage < state.PlayerHp)
                    continue;

                var score = -RelativeCost(enemy, c);
                if (score > bestLethalScore)
                {
                    bestLethalScore = score;
                    bestLethal = c;
                }
            }
            if (bestLethal != null)
                return bestLethal;

            // 2) Otherwise, maximize damage-per-cost with scarcity.
            // Score = damage / (epsilon + relativeCost), where relativeCost is normalized by enemy max resources.
            CombatActionData best = null;
            var bestScore = double.NegativeInfinity;

            foreach (var c in candidates)
            {
                var cost = RelativeCost(enemy, c);
                var score = c.HpDamage / (0.10 + cost);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
                else if (Math.Abs(score - bestScore) < 0.0001 && rng != null)
                {
                    // Tie-breaker: randomize a bit to avoid always repeating the same action.
                    if (rng.NextDouble() < 0.5)
                        best = c;
                }
            }

            return best;
        }

        private static double RelativeCost(EnemyData enemy, CombatActionData action)
        {
            // Normalize costs by max pools. If max is 0 (resource not used), treat any cost as very expensive.
            // Also apply scarcity: spending from a low current pool is effectively more costly.

            double MpTerm()
            {
                if (action.MpCost <= 0) return 0.0;
                if (enemy.maxMp <= 0) return 1000.0;
                return (double)action.MpCost / enemy.maxMp;
            }

            double SpTerm()
            {
                if (action.SpCost <= 0) return 0.0;
                if (enemy.maxSp <= 0) return 1000.0;
                return (double)action.SpCost / enemy.maxSp;
            }

            double LpTerm()
            {
                if (action.LpCost <= 0) return 0.0;
                if (enemy.maxLp <= 0) return 1000.0;
                return (double)action.LpCost / enemy.maxLp;
            }

            return MpTerm() + SpTerm() + LpTerm();
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

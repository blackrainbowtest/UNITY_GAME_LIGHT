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
            Random rng,
            bool allowHealActions = true)
        {
            if (enemy == null || registry == null || state == null)
                return null;

            var enemyBaseAttack = CombatDamageModel.NormalizeBaseAttack(enemy.attack);

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

            var hpRatio = enemy.maxHp > 0 ? (float)state.EnemyHp / enemy.maxHp : 1f;
            var healConsiderHpRatio = Clamp01(enemy.healConsiderHpRatio);

            if (TrySelectHolySpellByHpThreshold(difficulty, enemy, allowed, registry, state, hpRatio, rng, allowHealActions, out var holyAction))
                return holyAction.Id;

            var candidates = new List<CombatActionData>(allowed.Length);
            foreach (var id in allowed)
            {
                var action = registry.Get(id);
                if (action == null)
                    continue;

                // Special-case: allow defensive actions if they are meaningful.
                if (action.Id == CombatActionId.Block)
                {
                    // Use Block only when: enemy is under pressure AND doesn't already have armor.
                    // Keeps AI from spamming a 0-damage action.
                    if (state.EnemyBlockArmor > 0)
                        continue;

                    if (hpRatio >= 0.45f)
                        continue;

                    // Must be affordable for enemy.
                    if (state.EnemyMp < action.MpCost || state.EnemySp < action.SpCost || state.EnemyLp < action.LpCost)
                        continue;

                    candidates.Add(action);
                    continue;
                }

                if (action.Id == CombatActionId.CounterAttack)
                {
                    // CounterAttack is allowed only if the enemy actually blocked some damage on the last player action.
                    if (!state.EnemyBlockedLastTurn)
                        continue;

                    if (state.EnemyMp < action.MpCost || state.EnemySp < action.SpCost || state.EnemyLp < action.LpCost)
                        continue;

                    candidates.Add(action);
                    continue;
                }

                // Enemy turn: action is meaningful if it damages the player OR heals the enemy.
                var canDamagePlayer = CombatDamageModel.ComputeHpDamage(enemyBaseAttack, action) > 0;
                var canHealSelf = allowHealActions && action.HpHealSelf > 0 && hpRatio < healConsiderHpRatio;
                if (!canDamagePlayer && !canHealSelf)
                    continue;

                // NOTE: action.RequiresPlayerBlockedLastTurn is a player-side gate.
                // For enemy, we ignore it (CounterAttack eligibility handled above).

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
                var v = PrimaryValue(c, enemyBaseAttack);
                if (v < minDamage) minDamage = v;
                if (v > maxDamage) maxDamage = v;
            }

            var weak = new List<CombatActionData>();
            var strong = new List<CombatActionData>();
            foreach (var c in candidates)
            {
                if (PrimaryValue(c, enemyBaseAttack) == minDamage)
                    weak.Add(c);
                if (PrimaryValue(c, enemyBaseAttack) == maxDamage)
                    strong.Add(c);
            }

            switch (difficulty)
            {
                case EnemyDifficulty.Easy:
                    return ChooseWeighted(rng, weak, strong, weakChance: 0.80f)?.Id;

                case EnemyDifficulty.Normal:
                    return ChooseWeighted(rng, weak, strong, weakChance: 0.60f)?.Id;

                case EnemyDifficulty.Hard:
                    return ChooseMostEffective(enemy, enemyBaseAttack, state, candidates, rng)?.Id;

                default:
                    return ChooseMostEffective(enemy, enemyBaseAttack, state, candidates, rng)?.Id;
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

        private static CombatActionData ChooseMostEffective(EnemyData enemy, int enemyBaseAttack, CombatState state, List<CombatActionData> candidates, Random rng)
        {
            if (candidates == null || candidates.Count == 0)
                return null;

            // 1) If we can kill the player now, do it.
            // Among lethal options, prefer the lowest relative cost (so hard AI doesn't waste resources).
            CombatActionData bestLethal = null;
            var bestLethalScore = double.NegativeInfinity;
            foreach (var c in candidates)
            {
                var computedDamage = CombatDamageModel.ComputeHpDamage(enemyBaseAttack, c);
                if (computedDamage < state.PlayerHp)
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
            // Score = primaryValue / (epsilon + relativeCost), where primaryValue is damage to player OR heal to self.
            CombatActionData best = null;
            var bestScore = double.NegativeInfinity;

            foreach (var c in candidates)
            {
                var cost = RelativeCost(enemy, c);
                var score = PrimaryValue(c, enemyBaseAttack) / (0.10 + cost);

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

        private static bool TrySelectHolySpellByHpThreshold(
            EnemyDifficulty difficulty,
            EnemyData enemy,
            CombatActionId[] allowed,
            CombatActionRegistry registry,
            CombatState state,
            float hpRatio,
            Random rng,
            bool allowHealActions,
            out CombatActionData holyAction)
        {
            holyAction = null;

            if (enemy == null || allowed == null || registry == null || state == null)
                return false;

            if (!allowHealActions)
                return false;

            bool holyAllowed = false;
            for (int i = 0; i < allowed.Length; i++)
            {
                if (allowed[i] == CombatActionId.HolySpell)
                {
                    holyAllowed = true;
                    break;
                }
            }

            if (!holyAllowed)
                return false;

            holyAction = registry.Get(CombatActionId.HolySpell);
            if (holyAction == null)
                return false;

            if (state.EnemyMp < holyAction.MpCost || state.EnemySp < holyAction.SpCost || state.EnemyLp < holyAction.LpCost)
                return false;

            var stage1 = Clamp01(enemy.holyPriorityHpRatioStage1);
            var stage2 = Clamp01(enemy.holyPriorityHpRatioStage2);
            var stage3 = Clamp01(enemy.holyPriorityHpRatioStage3);

            if (stage2 > stage1)
                stage2 = stage1;

            if (stage3 > stage2)
                stage3 = stage2;

            if (hpRatio > stage1)
                return false;

            // Three HP stages are treated as deterministic
            // Holy priority windows to guarantee healing behavior when affordable.
            if (hpRatio <= stage3)
                return true;

            if (hpRatio <= stage2)
                return true;

            return true;
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;

            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private static int PrimaryValue(CombatActionData action, int enemyBaseAttack)
        {
            if (action == null)
                return 0;

            var computedDamage = CombatDamageModel.ComputeHpDamage(enemyBaseAttack, action);
            return computedDamage > 0 ? computedDamage : action.HpHealSelf;
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

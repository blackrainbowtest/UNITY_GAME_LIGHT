using System.Collections;
using UnityEngine;
using Game.Battle.Combat;
using Game.Battle.Combat.Actions;
using Game.Battle.Visual;
using Logger = UDA2.Logging.Logger;

namespace Game.Battle
{
    public sealed class BattleVisualExecutor
    {
        private readonly BattleCharacterView playerView;
        private readonly BattleCharacterView enemyView;
        private readonly BattleProjectileSpawner projectileSpawner;

        public BattleVisualExecutor(BattleCharacterView playerView, BattleCharacterView enemyView, Transform projectilesRoot)
        {
            this.playerView = playerView;
            this.enemyView = enemyView;
            projectileSpawner = new BattleProjectileSpawner(projectilesRoot);
        }

        public IEnumerator PlayEscapeSuccessAndWait(BattleVisualAnimId escapeSuccessAnim)
        {
            Logger.LogInfo($"[BattleVisualExecutor] Escape success sequence: anim={escapeSuccessAnim}");

            if (playerView == null)
                yield break;

            int moveStartFrame = -1;
            float moveSpeed = 0f;
            TryGetEscapeRunMotion(out moveStartFrame, out moveSpeed);

            bool finished = false;
            bool keepMoving = false;
            System.Action onImpact = null;

            if (moveStartFrame > 0 && moveSpeed > 0f)
                onImpact = () => keepMoving = true;

            playerView.PlayImmediate(
                escapeSuccessAnim,
                onFinished: () => finished = true,
                onImpact: onImpact,
                impactFrameIndexOverride: moveStartFrame);

            float timeout = 5f;
            while (!finished && timeout > 0f)
            {
                if (keepMoving)
                    playerView.transform.position += Vector3.left * moveSpeed * Time.deltaTime;

                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        public IEnumerator PlayCharacterAnimAndWait(BattleCharacterView view, BattleVisualAnimId animId)
        {
            if (view == null)
                yield break;

            bool finished = false;
            view.PlayImmediate(animId, onFinished: () => finished = true);

            float timeout = 5f;
            while (!finished && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        public IEnumerator PlayCharacterAnimImmediateAndWait(
            BattleCharacterView view,
            BattleVisualAnimId animId,
            System.Action onImpact = null,
            int impactFrameIndexOverride = -1)
        {
            if (view == null)
                yield break;

            bool finished = false;
            view.PlayImmediate(animId, onFinished: () => finished = true, onImpact: onImpact, impactFrameIndexOverride: impactFrameIndexOverride);

            float timeout = 5f;
            while (!finished && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        public IEnumerator PlayActionVisualAndWait(CombatActionId actionId, bool actorIsPlayer)
        {
            var view = actorIsPlayer ? playerView : enemyView;
            if (view == null)
                yield break;

            if (!TryGetVisualAnimId(actionId, out var animId))
                yield break;

            bool finished = false;
            view.RequestPlayAfterCurrent(animId, onFinished: () => finished = true);

            float timeout = 5f;
            while (!finished && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        public IEnumerator PlayActionWithTargetHitAndWait(
            CombatActionId actionId,
            bool actorIsPlayer,
            CombatState before,
            CombatState after)
        {
            var attackerView = actorIsPlayer ? playerView : enemyView;
            var targetView = actorIsPlayer ? enemyView : playerView;

            if (attackerView == null)
                yield break;

            if (!TryGetVisualAnimId(actionId, out var attackerAnimId))
                yield break;

            bool attackerFinished = false;
            bool targetFinished = true;

            OutfitVisuals.SpellProjectileConfig projectileConfig = default;
            bool hasProjectile = false;
            var attackerOutfit = attackerView.ResolveOutfitVisuals();
            if (attackerOutfit != null)
                hasProjectile = attackerOutfit.TryGetProjectileConfig(attackerAnimId, out projectileConfig);

            int hitAtFrame = 1;
            bool useLustHit = false;
            if (attackerOutfit != null && attackerOutfit.TryGetHitTiming(attackerAnimId, out var hitTiming))
            {
                hitAtFrame = hitTiming.hitAtFrame;
                useLustHit = hitTiming.useLustHit;
            }

            bool projectileSpawned = false;
            bool targetHitTriggered = false;
            BattleVisualAnimId targetHitAnimId = BattleVisualAnimId.Hit;
            bool willPlayTargetHit = false;

            void SpawnProjectileNow()
            {
                if (!hasProjectile)
                    return;
                if (projectileSpawned)
                    return;
                if (!projectileConfig.IsEnabled)
                    return;
                if (attackerView == null)
                    return;

                projectileSpawned = projectileSpawner.TrySpawnProjectile(
                    projectileConfig,
                    attackerView.transform,
                    actorIsPlayer,
                    onImpact: () => TriggerTargetHitNow());
            }

            void HandleOneShotStarted(BattleVisualAnimId id, IdleAnimation anim)
            {
                if (id != attackerAnimId)
                    return;

                if (hasProjectile && !projectileSpawned)
                {
                    bool hasImpactMarker = anim != null && anim.HasImpact;
                    if (!hasImpactMarker)
                        SpawnProjectileNow();
                }
            }

            attackerView.OnOneShotStarted += HandleOneShotStarted;

            bool targetTookHpDamage = false;
            if (before != null && after != null)
            {
                targetTookHpDamage = actorIsPlayer
                    ? after.EnemyHp < before.EnemyHp
                    : after.PlayerHp < before.PlayerHp;
            }

            bool targetTookLpDamage = false;
            if (before != null && after != null)
            {
                targetTookLpDamage = actorIsPlayer
                    ? after.EnemyLp > before.EnemyLp
                    : after.PlayerLp > before.PlayerLp;
            }

            if (useLustHit)
            {
                targetHitAnimId = BattleVisualAnimId.LustHit;
                willPlayTargetHit = targetTookLpDamage;
            }
            else
            {
                targetHitAnimId = BattleVisualAnimId.Hit;
                willPlayTargetHit = targetTookHpDamage;
            }

            if (hitAtFrame == -1)
                willPlayTargetHit = false;

            targetFinished = !willPlayTargetHit;

            void TriggerTargetHitNow()
            {
                if (!willPlayTargetHit)
                    return;
                if (targetHitTriggered)
                    return;
                if (targetView == null)
                    return;

                targetHitTriggered = true;
                targetFinished = false;

                targetView.PlayImmediate(targetHitAnimId, onFinished: () => targetFinished = true);
            }

            if (hasProjectile)
            {
                int spawnFrame = projectileConfig.spawnAtFrame;
                if (spawnFrame <= 1)
                    spawnFrame = 1;

                attackerView.PlayImmediate(
                    attackerAnimId,
                    onFinished: () => attackerFinished = true,
                    onImpact: () => SpawnProjectileNow(),
                    impactFrameIndexOverride: spawnFrame);
            }
            else
            {
                attackerView.PlayImmediate(
                    attackerAnimId,
                    onFinished: () => attackerFinished = true,
                    onImpact: () => TriggerTargetHitNow(),
                    impactFrameIndexOverride: -1);
            }

            float timeout = 5f;
            while ((!attackerFinished || !targetFinished) && timeout > 0f)
            {
                if (attackerFinished)
                {
                    if (hasProjectile && !projectileSpawned)
                        SpawnProjectileNow();

                    if (!hasProjectile && !targetHitTriggered)
                        TriggerTargetHitNow();
                }

                timeout -= Time.deltaTime;
                yield return null;
            }

            attackerView.OnOneShotStarted -= HandleOneShotStarted;
        }

        private bool TryGetEscapeRunMotion(out int startAtFrame, out float speedLeftUnitsPerSecond)
        {
            startAtFrame = -1;
            speedLeftUnitsPerSecond = 0f;

            var outfit = playerView != null ? playerView.ResolveOutfitVisuals() : null;
            if (outfit == null)
                return false;

            if (!outfit.TryGetEscapeRunMotion(out var cfg))
                return false;

            startAtFrame = cfg.startAtFrame;
            speedLeftUnitsPerSecond = cfg.speedLeftUnitsPerSecond;
            return true;
        }

        private static bool TryGetVisualAnimId(CombatActionId actionId, out BattleVisualAnimId animId)
        {
            switch (actionId)
            {
                case CombatActionId.FastAttack: animId = BattleVisualAnimId.FastAttack; return true;
                case CombatActionId.NormalAttack: animId = BattleVisualAnimId.NormalAttack; return true;
                case CombatActionId.HeavyAttack: animId = BattleVisualAnimId.HeavyAttack; return true;
                case CombatActionId.CounterAttack: animId = BattleVisualAnimId.CounterAttack; return true;
                case CombatActionId.Block: animId = BattleVisualAnimId.Block; return true;

                case CombatActionId.FireSpell: animId = BattleVisualAnimId.FireSpell; return true;
                case CombatActionId.IceSpell: animId = BattleVisualAnimId.IceSpell; return true;
                case CombatActionId.HolySpell: animId = BattleVisualAnimId.HolySpell; return true;
                case CombatActionId.DarkSpell: animId = BattleVisualAnimId.DarkSpell; return true;

                case CombatActionId.SeductionAct1: animId = BattleVisualAnimId.SeductionAct1; return true;
                case CombatActionId.SeductionAct2: animId = BattleVisualAnimId.SeductionAct2; return true;
                case CombatActionId.SeductionAct3: animId = BattleVisualAnimId.SeductionAct3; return true;
                case CombatActionId.SeductionAct4: animId = BattleVisualAnimId.SeductionAct4; return true;

                case CombatActionId.ActionAct1: animId = BattleVisualAnimId.ActionAct1; return true;
                case CombatActionId.ActionAct2: animId = BattleVisualAnimId.ActionAct2; return true;
                case CombatActionId.ActionAct3: animId = BattleVisualAnimId.ActionAct3; return true;
                case CombatActionId.ActionAct4: animId = BattleVisualAnimId.ActionAct4; return true;
            }

            animId = BattleVisualAnimId.Idle;
            return false;
        }
    }
}

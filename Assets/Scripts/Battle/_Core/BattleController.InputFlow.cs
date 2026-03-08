using System.Collections;
using Game.Battle.Combat.Actions;
using Game.Battle.Visual;
using Logger = UDA2.Logging.Logger;

namespace Game.Battle
{
    public partial class BattleController
    {
        #region UI entrypoints

        public void OnAttackPressed()
        {
            EnsureInputCommandHandler().OnAttackPressed();
        }

        public void OnCombatActionSelected(CombatActionId actionId)
        {
            EnsureInputCommandHandler().OnCombatActionSelected(actionId);
        }

        public void OnItemPressed()
        {
            EnsureInputCommandHandler().OnItemPressed();
        }

        public void OnRunPressed()
        {
            EnsureInputCommandHandler().OnRunPressed();
        }

        public void OnSurrenderPressed()
        {
            EnsureInputCommandHandler().OnSurrenderPressed();
        }

        public void OnSkipTurnPressed()
        {
            EnsureInputCommandHandler().OnSkipTurnPressed();
        }

        public void OnExitPressed()
        {
            EnsureInputCommandHandler().OnExitPressed();
        }

        #endregion

        #region Input handlers

        private void HandleAttackPressed()
        {
            ExecutePlayerAction(CombatActionId.NormalAttack);
        }

        private void HandleCombatActionSelected(CombatActionId actionId)
        {
            ExecutePlayerAction(actionId);
        }

        private void HandleItemPressed()
        {
            OpenInventoryForBattleItemUse();
        }

        private void HandleRunPressed()
        {
            if (!battleStarted)
                return;

            if (!EnsureTurnSystem().IsPlayerTurn)
                return;

            if (escapeFailedRoutine != null || escapeSuccessRoutine != null)
                return;

            bool success = EnsureEscapeSystem().TryRollEscape(rng, context, combatState, out float chance, out float roll);

            Logger.LogInfo($"[BattleController] Run pressed. EscapeChance={chance:0.000}, Roll={roll:0.000}");

            if (success)
            {
                EnsureTurnSystem().BeginEnemyTurn();
                hudController?.SetInputEnabled(false);
                escapeSuccessRoutine = StartCoroutine(EscapeSuccessSequenceRoutine());
            }
            else
            {
                EnsureTurnSystem().BeginEnemyTurn();
                hudController?.SetInputEnabled(false);
                escapeFailedRoutine = StartCoroutine(EscapeFailedSequenceRoutine());
            }
        }

        private void HandleSurrenderPressed()
        {
            if (!battleStarted)
                return;

            if (!EnsureTurnSystem().IsPlayerTurn)
                return;

            if (surrenderRoutine != null || escapeSuccessRoutine != null || escapeFailedRoutine != null)
                return;

            Logger.LogInfo("BattleController: Surrender pressed");

            EnsureTurnSystem().BeginEnemyTurn();
            hudController?.SetInputEnabled(false);
            surrenderRoutine = StartCoroutine(SurrenderSequenceRoutine());
        }

        private void HandleSkipTurnPressed()
        {
            if (!battleStarted)
                return;

            if (!EnsureTurnSystem().IsPlayerTurn)
                return;

            if (playerActionRoutine != null)
                return;

            EnsureTurnSystem().BeginEnemyTurn();
            hudController?.SetInputEnabled(false);
            playerActionRoutine = StartCoroutine(SkipTurnSequenceRoutine());
        }

        private void HandleExitPressed()
        {
            if (!battleStarted)
                return;

            Logger.LogInfo("BattleController: Exit pressed");
            battleStarted = false;
            ExitBattle();
        }

        #endregion

        #region Input sequences

        private IEnumerator EscapeSuccessSequenceRoutine()
        {
            yield return EnsureVisualExecutor().PlayEscapeSuccessAndWait(escapeSuccessAnim);

            escapeSuccessRoutine = null;
            FinishBattle(playerWon: false, reason: BattleFinishReason.EscapeSuccess, winningActionId: null);
        }

        private IEnumerator EscapeFailedSequenceRoutine()
        {
            playerView?.SetAutoIdleFallbackEnabled(false);

            var failAnimToPlay = escapeFailFallAnim == BattleVisualAnimId.ActionAct1
                ? BattleVisualAnimId.ActionActFail
                : escapeFailFallAnim;

            if (escapeFailFallAnim == BattleVisualAnimId.ActionAct1)
                Logger.LogWarning("[BattleController] Escape fail anim is legacy ActionAct1 in serialized data. Overriding to ActionActFail at runtime.");

            Logger.LogInfo($"[BattleController] Escape failed sequence: configured={escapeFailFallAnim}, playing={failAnimToPlay}");

            yield return EnsureVisualExecutor().PlayCharacterAnimAndWait(playerView, failAnimToPlay);

            bool modalClosed = false;
            if (outcomeAnimationModal != null)
            {
                PlayOutcomeMusic(BattleFinishReason.EscapeFailed, allowLegacyFallback: false);
                outcomeAnimationModal.Show(BattleFinishReason.EscapeFailed, playerWon: false, winningActionId: null, onClosed: () => modalClosed = true);
                while (!modalClosed)
                    yield return null;
            }

            combatState = escapeSystem.ApplyFailedEscapePenalty(context, combatState, escapeFailLpPenalty);
            PushHudState();

            escapeFailedRoutine = null;

            playerView?.SetAutoIdleFallbackEnabled(true);

            if (EnsureEscapeSystem().IsPlayerLpDefeat(context, combatState))
            {
                FinishBattle(playerWon: false, reason: BattleFinishReason.DefeatByLp, winningActionId: null);
                yield break;
            }

            // Escape failed modal path disables auto idle fallback while fail animation plays.
            // Restore visible idle immediately when battle continues.
            playerView?.PlayIdle();

            BeginEnemyTurn();
        }

        private IEnumerator SurrenderSequenceRoutine()
        {
            yield return EnsureVisualExecutor().PlayCharacterAnimImmediateAndWait(playerView, BattleVisualAnimId.ActionAct3);

            surrenderRoutine = null;
            FinishBattle(playerWon: false, reason: BattleFinishReason.Surrender, winningActionId: null);
        }

        private IEnumerator SkipTurnSequenceRoutine()
        {
            if (TryGetVisualAnimId(CombatActionId.ActionAct4, out var animId))
            {
                yield return EnsureVisualExecutor().PlayCharacterAnimImmediateAndWait(playerView, animId);
            }

            combatState = combatState
                .WithPlayerBlockedLastTurn(false)
                .WithPlayerBlockArmorAbsorbedLastEnemyAction(0);

            PushHudState();

            playerActionRoutine = null;

            BeginEnemyTurn();
        }

        #endregion

        #region Shared animation helpers

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

        private IEnumerator PlayCharacterAnimAndWait(BattleCharacterView view, BattleVisualAnimId animId)
        {
            yield return EnsureVisualExecutor().PlayCharacterAnimAndWait(view, animId);
        }

        private IEnumerator PlayCharacterAnimImmediateAndWait(
            BattleCharacterView view,
            BattleVisualAnimId animId,
            System.Action onImpact = null,
            int impactFrameIndexOverride = -1)
        {
            yield return EnsureVisualExecutor().PlayCharacterAnimImmediateAndWait(view, animId, onImpact, impactFrameIndexOverride);
        }

                #endregion
    }
}

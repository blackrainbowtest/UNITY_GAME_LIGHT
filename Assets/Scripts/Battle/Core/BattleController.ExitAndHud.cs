using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Battle.UI;
using UDA2.Logging;

namespace Game.Battle
{
    public partial class BattleController
    {
        private void FinishBattle(bool playerWon)
        {
            battleStarted = false;
            turnPhase = TurnPhase.BattleOver;
            hudController?.SetInputEnabled(false);

            // Rewards are not configured yet (no drop tables wired).
            var result = new BattleResultData(
                playerWon: playerWon,
                goldGained: 0,
                itemIds: Array.Empty<string>()
            );

            if (resultModal != null)
            {
                resultModal.Show(result, ExitBattle);
            }
            else
            {
                ExitBattle();
            }
        }

        private void ExitBattle()
        {
            var targetScene = ResolveReturnSceneName();
            if (string.IsNullOrEmpty(targetScene))
            {
                Logger.LogError("BattleController: Cannot exit battle, return scene is not set");
                return;
            }

            if (UDA2.SceneFlow.SceneFlowManager.Instance != null)
                UDA2.SceneFlow.SceneFlowManager.Instance.LoadScene(targetScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }

        private string ResolveReturnSceneName()
        {
            if (context != null && context.Mode == BattleMode.Tutorial)
                return tutorialReturnSceneName;

            var exit = BattleExitContext.Consume();
            if (!string.IsNullOrEmpty(exit?.ReturnSceneName))
                return exit.ReturnSceneName;

            // Fallbacks for cases when battle scene is launched directly (e.g. from editor) or
            // when the caller forgot to set BattleExitContext before loading the battle scene.
            var currentScene = SceneManager.GetActiveScene().name;

            var saveScene = GameState.Instance?.CurrentSave?.player?.sceneName;
            if (!string.IsNullOrEmpty(saveScene) && !string.Equals(saveScene, currentScene, StringComparison.Ordinal))
            {
                Logger.LogWarning($"BattleController: BattleExitContext was not set. Falling back to save.player.sceneName='{saveScene}'.");
                return saveScene;
            }

            if (!string.IsNullOrEmpty(defaultReturnSceneName) && !string.Equals(defaultReturnSceneName, currentScene, StringComparison.Ordinal))
            {
                Logger.LogWarning($"BattleController: BattleExitContext was not set. Falling back to defaultReturnSceneName='{defaultReturnSceneName}'.");
                return defaultReturnSceneName;
            }

            return null;
        }

        private void PushHudState(bool showDeltas = true)
        {
            var hudState = BattleHUDStateFactory.Create(
                context.Player,
                context.Enemy,
                combatState
            );

            hudController?.UpdateState(hudState, showDeltas);
            hudController?.UpdateStatuses(playerStatuses, enemyStatuses);
        }
    }
}

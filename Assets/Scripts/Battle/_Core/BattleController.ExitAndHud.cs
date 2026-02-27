//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\BattleController.ExitAndHud.cs                                       */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:34:34 by UDA                                                                    */
/*   Updated: 2026/01/23 01:34:34 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Battle.UI;
using Game.Battle.Combat.Actions;
using UDA2.Audio;
using UDA2.GameTime;
using Logger = UDA2.Logging.Logger;

namespace Game.Battle
{
    public partial class BattleController
    {
        private string resolvedReturnSceneName;

        [Header("Defeat Rewards")]
        [Tooltip("Gold granted to the player even on Defeat (inclusive).")]
        [SerializeField] private int defeatGoldMin = 1;
        [Tooltip("Gold granted to the player even on Defeat (inclusive).")]
        [SerializeField] private int defeatGoldMax = 3;

        [Header("Result Music (Optional)")]
        [Tooltip("If assigned, plays this music when the battle ends and the results modal is shown.")]
        [SerializeField] private AudioCue resultsMusicCue;
        [Tooltip("Fallback (legacy): if no cue is assigned, plays this clip when the battle ends and the results modal is shown.")]
        [SerializeField] private AudioClip resultsMusic;

        private void FinishBattle(bool playerWon, BattleFinishReason reason = BattleFinishReason.Defeat, CombatActionId? winningActionId = null)
        {
            battleStarted = false;
            turnSystem?.EndBattle();
            hudController?.SetInputEnabled(false);

            var showResult = UDA2.Core.SettingsContext.Current == null
                ? true
                : UDA2.Core.SettingsContext.Current.showBattleResultModal;

            // Battle is over: stop battle music immediately.
            if (UDA2.Audio.AudioManager.Instance != null)
            {
                UDA2.Audio.AudioManager.Instance.StopMusic();

                // Optional: play separate results music while the results modal is on screen.
                if (showResult)
                {
                    if (resultsMusicCue != null)
                        UDA2.Audio.AudioManager.Instance.Play(resultsMusicCue);
                    else if (resultsMusic != null)
                        UDA2.Audio.AudioManager.Instance.PlayMusic(resultsMusic);
                }
            }

            if (playerWon && reason == BattleFinishReason.Defeat)
                reason = BattleFinishReason.Victory;
            else if (!playerWon && reason == BattleFinishReason.Victory)
                reason = BattleFinishReason.Defeat;

            int battleMinutes = ResolveBattleDurationMinutes(reason, playerWon);
            if (battleMinutes > 0)
                GameTimeAPI.AddMinutes(battleMinutes);

            // Persist player resources from battle back to SaveData (so leaving battle doesn't "heal to full").
            // This must happen before we leave the battle scene.
            var saveForResources = global::GameState.Instance?.CurrentSave;
            bool resourcesChanged = ApplyPlayerResourcesToSave(saveForResources);

            // Tutorial flow: after victory we want an autosave when we arrive to StartCityScene.
            // Do NOT save here (we are still in battle scene and pendingBattle may still be set).
            if (playerWon && context != null && context.Mode == BattleMode.Tutorial)
            {
                var save = global::GameState.Instance?.CurrentSave;
                if (save?.sceneState != null)
                {
                    save.sceneState.RequestAutosave(tutorialReturnSceneName);
                }
            }

            int goldGained = 0;
            int manaCrystalsGained = 0;
            int demonCrystalsGained = 0;
            int expGained = 0;
            var itemsGained = Array.Empty<BattleResultData.ItemReward>();

            if (playerWon)
            {
                var save = global::GameState.Instance?.CurrentSave;
                var enemy = context != null ? context.Enemy : null;

                var loot = BattleLootResolver.Resolve(enemy, save, context != null ? context.EnemyLevel : -1);
                goldGained = loot.GoldGained;
                manaCrystalsGained = loot.ManaCrystalsGained;
                demonCrystalsGained = loot.DemonCrystalsGained;
                expGained = loot.ExpGained;
                itemsGained = loot.Items != null ? (loot.Items as BattleResultData.ItemReward[] ?? new System.Collections.Generic.List<BattleResultData.ItemReward>(loot.Items).ToArray()) : Array.Empty<BattleResultData.ItemReward>();

                ApplyRewardsToSave(save, goldGained, manaCrystalsGained, demonCrystalsGained, expGained, itemsGained);

                // Persist rewards by requesting a deferred autosave when we return to a non-battle scene.
                // We do not save immediately here because sceneState.pendingBattle is cleared on scene change.
                if (save?.sceneState != null)
                {
                    bool hasAnyReward = goldGained > 0 || manaCrystalsGained > 0 || demonCrystalsGained > 0 || expGained > 0 || (itemsGained != null && itemsGained.Length > 0);
                    if (hasAnyReward || resourcesChanged)
                    {
                        // SaveSystem will autosave slot 0 when entering the target scene.
                        save.sceneState.RequestAutosave(ResolveReturnSceneName());
                    }
                }
            }
            else
            {
                // Design requirement: even on defeat the player gets a small gold consolation prize.
                // (This also helps validate the result modal shows resources for loss outcomes.)
                if (reason == BattleFinishReason.Defeat || reason == BattleFinishReason.Surrender || reason == BattleFinishReason.EscapeFailed)
                {
                    int min = Mathf.Min(defeatGoldMin, defeatGoldMax);
                    int max = Mathf.Max(defeatGoldMin, defeatGoldMax);
                    min = Mathf.Max(0, min);
                    max = Mathf.Max(0, max);

                    if (max > 0)
                    {
                        // rng.Next upper bound is exclusive.
                        goldGained = rng.Next(min, max + 1);

                        var save = global::GameState.Instance?.CurrentSave;
                        ApplyRewardsToSave(save, goldGained, manaCrystalsGained, demonCrystalsGained, expGained, itemsGained);
                    }
                }

                // Even on non-victory outcomes we may need to persist state.
                // EscapeSuccess can have zero rewards and no detected resource delta,
                // but still should checkpoint battle completion.
                bool forceAutosave = reason == BattleFinishReason.EscapeSuccess || reason == BattleFinishReason.Surrender;
                if (resourcesChanged || goldGained > 0 || forceAutosave)
                {
                    var save = global::GameState.Instance?.CurrentSave;
                    if (save?.sceneState != null)
                        save.sceneState.RequestAutosave(ResolveReturnSceneName());
                }
            }

            var result = new BattleResultData(
                playerWon: playerWon,
                goldGained: goldGained,
                manaCrystalsGained: manaCrystalsGained,
                demonCrystalsGained: demonCrystalsGained,
                expGained: expGained,
                items: itemsGained
            );

            void ShowResultsOrExit()
            {
                if (reason == BattleFinishReason.EscapeSuccess)
                {
                    ExitBattle();
                    return;
                }

                if (resultModal != null && showResult)
                {
                    resultModal.SetItemDatabase(itemDatabase);
                    resultModal.Show(result, ExitBattle);
                }
                else
                {
                    ExitBattle();
                }
            }

            bool shouldShowOutcomeModal = reason == BattleFinishReason.Defeat
                || reason == BattleFinishReason.Victory
                || reason == BattleFinishReason.Surrender
                || reason == BattleFinishReason.EscapeFailed
                || reason == BattleFinishReason.EscapeSuccess
                || reason == BattleFinishReason.DefeatByLp
                || reason == BattleFinishReason.VictoryByLp;

            if (shouldShowOutcomeModal && outcomeAnimationModal == null)
            {
                Logger.LogWarning($"[BattleController] Outcome modal should be shown for reason={reason}, but outcomeAnimationModal is null. Assign a prefab (Project) or a scene instance (Hierarchy) to BattleController.outcomeAnimationModal.");
            }

            if (outcomeAnimationModal != null && shouldShowOutcomeModal)
            {
                ShowOutcomeModalOrResultsWithOptionalLoseAnimation();
            }
            else
            {
                ShowOutcomeModalOrResultsWithOptionalLoseAnimation();
            }

            void ShowOutcomeModalOrResultsWithOptionalLoseAnimation()
            {
                bool shouldPlayLoseAnim = !playerWon &&
                    (reason == BattleFinishReason.Defeat
                    || reason == BattleFinishReason.EscapeFailed
                    || reason == BattleFinishReason.DefeatByLp);

                if (!shouldPlayLoseAnim || playerView == null)
                {
                    ShowOutcomeModalOrResults();
                    return;
                }

                var loseAnimId = reason == BattleFinishReason.DefeatByLp
                    ? Game.Battle.Visual.BattleVisualAnimId.LustLose
                    : Game.Battle.Visual.BattleVisualAnimId.Lose;

                StartCoroutine(PlayLoseAnimThenContinue(loseAnimId));
            }

            IEnumerator PlayLoseAnimThenContinue(Game.Battle.Visual.BattleVisualAnimId animId)
            {
                yield return PlayCharacterAnimImmediateAndWait(playerView, animId);
                ShowOutcomeModalOrResults();
            }

            void ShowOutcomeModalOrResults()
            {
                if (outcomeAnimationModal != null && shouldShowOutcomeModal)
                    outcomeAnimationModal.Show(reason, playerWon, winningActionId, ShowResultsOrExit, hideOnClose: false);
                else
                    ShowResultsOrExit();
            }
        }

        private static int ResolveBattleDurationMinutes(BattleFinishReason reason, bool playerWon)
        {
            switch (reason)
            {
                case BattleFinishReason.EscapeSuccess:
                    return 30;

                case BattleFinishReason.Defeat:
                case BattleFinishReason.DefeatByLp:
                case BattleFinishReason.Surrender:
                    return 45;

                case BattleFinishReason.Victory:
                case BattleFinishReason.VictoryByLp:
                    return 60;

                case BattleFinishReason.EscapeFailed:
                    // Usually does not finish battle (battle continues).
                    return 0;

                default:
                    return playerWon ? 60 : 45;
            }
        }

        private static void ApplyRewardsToSave(SaveData save, int goldGained, int manaCrystalsGained, int demonCrystalsGained, int expGained, BattleResultData.ItemReward[] items)
        {
            if (save == null)
                return;

            if (save.inventory == null)
                save.inventory = new SaveData.Inventory();

            if (save.player == null)
                save.player = new SaveData.Player();

            if (goldGained > 0)
                save.inventory.gold += goldGained;

            // Keep gold mirrored as an inventory item too (for integrity checks / UI consistency).
            // We store TOTAL gold value as the gold-item count to guarantee sync.
            EnsureItemList(save.inventory);
            SetItemCount(save.inventory.items, "gold", save.inventory.gold);

            if (manaCrystalsGained > 0)
                save.inventory.manaCrystals += manaCrystalsGained;

            if (demonCrystalsGained > 0)
                save.inventory.demonCrystals += demonCrystalsGained;

            if (expGained > 0)
                Game.Progression.PlayerExperience.AddExp(ref save.player.level, ref save.player.exp, expGained, out _);

            if (items == null || items.Length == 0)
                return;

            EnsureItemList(save.inventory);

            for (int i = 0; i < items.Length; i++)
            {
                var r = items[i];
                if (string.IsNullOrWhiteSpace(r.ItemId) || r.Count <= 0)
                    continue;

                AddOrStack(save.inventory.items, r.ItemId.Trim(), r.Count);
            }
        }

        private static void EnsureItemList(SaveData.Inventory inv)
        {
            if (inv == null)
                return;

            if (inv.items == null)
                inv.items = new System.Collections.Generic.List<SaveData.Item>();
        }

        private static void SetItemCount(System.Collections.Generic.List<SaveData.Item> list, string itemId, int count)
        {
            if (list == null || string.IsNullOrWhiteSpace(itemId))
                return;

            count = Mathf.Max(0, count);

            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                if (it == null)
                    continue;

                if (string.Equals(it.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    it.count = count;
                    return;
                }
            }

            if (count > 0)
                list.Add(new SaveData.Item { itemId = itemId, count = count });
        }

        private static void AddOrStack(System.Collections.Generic.List<SaveData.Item> list, string itemId, int count)
        {
            if (list == null || string.IsNullOrWhiteSpace(itemId) || count <= 0)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                if (it == null)
                    continue;

                if (string.Equals(it.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    it.count += count;
                    return;
                }
            }

            list.Add(new SaveData.Item { itemId = itemId, count = count });
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

        private bool ApplyPlayerResourcesToSave(SaveData save)
        {
            if (save == null)
                return false;

            if (save.player == null)
                save.player = new SaveData.Player();

            if (save.player.stats == null)
                save.player.stats = new SaveData.Stats();

            if (context == null || combatState == null)
                return false;

            var stats = save.player.stats;

            int newHp = Mathf.Clamp(combatState.PlayerHp, 0, Mathf.Max(0, context.Player.MaxHP));
            int newMp = Mathf.Clamp(combatState.PlayerMp, 0, Mathf.Max(0, context.Player.MaxMP));
            int newSp = Mathf.Clamp(combatState.PlayerSp, 0, Mathf.Max(0, context.Player.MaxSP));
            int newLp = Mathf.Clamp(combatState.PlayerLp, 0, Mathf.Max(0, context.Player.MaxLP));

            bool changed = stats.hp != newHp || stats.mp != newMp || stats.sp != newSp || stats.lp != newLp;

            stats.hp = newHp;
            stats.mp = newMp;
            stats.sp = newSp;
            stats.lp = newLp;

            return changed;
        }

        private string ResolveReturnSceneName()
        {
            if (!string.IsNullOrEmpty(resolvedReturnSceneName))
                return resolvedReturnSceneName;

            if (context != null && context.Mode == BattleMode.Tutorial)
            {
                resolvedReturnSceneName = tutorialReturnSceneName;
                return resolvedReturnSceneName;
            }

            var exit = BattleExitContext.Consume();
            if (!string.IsNullOrEmpty(exit?.ReturnSceneName))
            {
                resolvedReturnSceneName = exit.ReturnSceneName;
                return resolvedReturnSceneName;
            }

            // Fallbacks for cases when battle scene is launched directly (e.g. from editor) or
            // when the caller forgot to set BattleExitContext before loading the battle scene.
            var currentScene = SceneManager.GetActiveScene().name;

            var saveScene = GameState.Instance?.CurrentSave?.player?.sceneName;
            if (!string.IsNullOrEmpty(saveScene) && !string.Equals(saveScene, currentScene, StringComparison.Ordinal))
            {
                Logger.LogWarning($"BattleController: BattleExitContext was not set. Falling back to save.player.sceneName='{saveScene}'.");
                resolvedReturnSceneName = saveScene;
                return resolvedReturnSceneName;
            }

            if (!string.IsNullOrEmpty(defaultReturnSceneName) && !string.Equals(defaultReturnSceneName, currentScene, StringComparison.Ordinal))
            {
                Logger.LogWarning($"BattleController: BattleExitContext was not set. Falling back to defaultReturnSceneName='{defaultReturnSceneName}'.");
                resolvedReturnSceneName = defaultReturnSceneName;
                return resolvedReturnSceneName;
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

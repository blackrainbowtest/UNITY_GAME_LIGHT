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

        [Header("Result Music (Legacy Optional)")]
        [Tooltip("Legacy fallback: used when no per-outcome cue is assigned.")]
        [SerializeField] private AudioCue resultsMusicCue;
        [Tooltip("Legacy fallback clip: used when no per-outcome cue and no legacy cue are assigned.")]
        [SerializeField] private AudioClip resultsMusic;

        [Header("Outcome Music (Optional)")]
        [Tooltip("Played for Victory result flow.")]
        [SerializeField] private AudioCue victoryResultMusicCue;
        [Tooltip("Played for Defeat/Surrender result flow.")]
        [SerializeField] private AudioCue defeatResultMusicCue;
        [Tooltip("Played for VictoryByLp (seduction victory) result flow. Falls back to Victory cue.")]
        [SerializeField] private AudioCue victoryByLpResultMusicCue;
        [Tooltip("Played for DefeatByLp (seduction defeat) result flow. Falls back to Defeat cue.")]
        [SerializeField] private AudioCue defeatByLpResultMusicCue;
        [Tooltip("Played when EscapeFailed outcome modal is opened (battle continues).")]
        [SerializeField] private AudioCue escapeFailedModalMusicCue;

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

            RecordBattleStats(global::GameState.Instance?.CurrentSave, context, reason, goldGained, expGained);

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
                    // Play outcome-specific music when entering results stage (after outcome visuals),
                    // to avoid audio-switch hitch before defeat/victory presentation.
                    PlayOutcomeMusic(reason, allowLegacyFallback: true);

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
                ShowOutcomeModalOrResultsWithOptionalEndAnimation();
            }
            else
            {
                ShowOutcomeModalOrResultsWithOptionalEndAnimation();
            }

            void ShowOutcomeModalOrResultsWithOptionalEndAnimation()
            {
                bool shouldPlayPlayerLoseAnim = false;

                bool shouldPlayEnemyDefeatAnim = playerWon &&
                    (reason == BattleFinishReason.Victory
                    || reason == BattleFinishReason.VictoryByLp);

                if (!shouldPlayPlayerLoseAnim && !shouldPlayEnemyDefeatAnim)
                {
                    ShowOutcomeModalOrResults();
                    return;
                }

                if (shouldPlayPlayerLoseAnim)
                {
                    if (playerView == null)
                    {
                        ShowOutcomeModalOrResults();
                        return;
                    }

                    var loseAnimId = reason == BattleFinishReason.DefeatByLp
                        ? Game.Battle.Visual.BattleVisualAnimId.LustLose
                        : Game.Battle.Visual.BattleVisualAnimId.Lose;

                    StartCoroutine(PlayEndAnimThenContinue(playerView, loseAnimId));
                    return;
                }

                if (enemyView == null)
                {
                    ShowOutcomeModalOrResults();
                    return;
                }

                StartCoroutine(PlayEndAnimThenContinue(enemyView, Game.Battle.Visual.BattleVisualAnimId.Death));
            }

            IEnumerator PlayEndAnimThenContinue(Game.Battle.Visual.BattleCharacterView view, Game.Battle.Visual.BattleVisualAnimId animId)
            {
                yield return PlayCharacterAnimImmediateAndWait(view, animId);
                ShowOutcomeModalOrResults();
            }

            void ShowOutcomeModalOrResults()
            {
                if (outcomeAnimationModal != null && shouldShowOutcomeModal)
                    outcomeAnimationModal.Show(
                        reason,
                        playerWon,
                        winningActionId,
                        ShowResultsOrExit,
                        hideOnClose: false,
                        enemyId: context?.Enemy != null ? ResolveEnemyId(context.Enemy) : null,
                        locationId: context?.Location != null ? context.Location.id : null,
                        sourceLocationId: global::GameState.Instance?.CurrentSave?.sceneState?.pendingBattle?.locationId);
                else
                    ShowResultsOrExit();
            }
        }

        private void PlayOutcomeMusic(BattleFinishReason reason, bool allowLegacyFallback)
        {
            var audio = UDA2.Audio.AudioManager.Instance;
            if (audio == null)
                return;

            var cue = ResolveOutcomeMusicCue(reason);
            if (cue != null)
            {
                audio.Play(cue);
                return;
            }

            if (!allowLegacyFallback)
                return;

            if (resultsMusicCue != null)
            {
                audio.Play(resultsMusicCue);
                return;
            }

            if (resultsMusic != null)
                audio.PlayMusic(resultsMusic);
        }

        private AudioCue ResolveOutcomeMusicCue(BattleFinishReason reason)
        {
            switch (reason)
            {
                case BattleFinishReason.Victory:
                    return victoryResultMusicCue;

                case BattleFinishReason.Defeat:
                case BattleFinishReason.Surrender:
                    return defeatResultMusicCue;

                case BattleFinishReason.VictoryByLp:
                    return victoryByLpResultMusicCue != null ? victoryByLpResultMusicCue : victoryResultMusicCue;

                case BattleFinishReason.DefeatByLp:
                    return defeatByLpResultMusicCue != null ? defeatByLpResultMusicCue : defeatResultMusicCue;

                case BattleFinishReason.EscapeFailed:
                    return escapeFailedModalMusicCue;

                default:
                    return null;
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

        private static void RecordBattleStats(SaveData save, BattleContext battleContext, BattleFinishReason reason, int goldGained, int expGained)
        {
            if (save == null)
                return;

            if (save.achievementStats == null)
                save.achievementStats = new SaveData.AchievementStats();

            var stats = save.achievementStats;
            stats.battlesFinished = Mathf.Max(0, stats.battlesFinished) + 1;

            if (goldGained > 0)
                stats.totalGoldEarned = Mathf.Max(0, stats.totalGoldEarned) + goldGained;

            if (expGained > 0)
                stats.totalExpEarned = Mathf.Max(0, stats.totalExpEarned) + expGained;

            switch (reason)
            {
                case BattleFinishReason.Victory:
                case BattleFinishReason.VictoryByLp:
                    stats.battlesWon = Mathf.Max(0, stats.battlesWon) + 1;
                    RegisterEnemyKill(stats, battleContext?.Enemy);
                    break;

                case BattleFinishReason.Defeat:
                case BattleFinishReason.DefeatByLp:
                    stats.battlesLost = Mathf.Max(0, stats.battlesLost) + 1;
                    break;

                case BattleFinishReason.Surrender:
                    stats.battlesSurrendered = Mathf.Max(0, stats.battlesSurrendered) + 1;
                    break;

                case BattleFinishReason.EscapeSuccess:
                    stats.escapesSuccessful = Mathf.Max(0, stats.escapesSuccessful) + 1;
                    break;

                case BattleFinishReason.EscapeFailed:
                    stats.escapesFailed = Mathf.Max(0, stats.escapesFailed) + 1;
                    break;
            }
        }

        private static void RegisterEnemyKill(SaveData.AchievementStats stats, EnemyData enemy)
        {
            if (stats == null || enemy == null)
                return;

            var enemyId = ResolveEnemyId(enemy);
            if (string.IsNullOrWhiteSpace(enemyId))
                return;

            stats.totalMobKills = Mathf.Max(0, stats.totalMobKills) + 1;

            if (stats.mobKillsByEnemyId == null)
                stats.mobKillsByEnemyId = new System.Collections.Generic.List<SaveData.MobKillEntry>();

            for (int i = 0; i < stats.mobKillsByEnemyId.Count; i++)
            {
                var entry = stats.mobKillsByEnemyId[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.enemyId))
                    continue;

                if (!string.Equals(entry.enemyId, enemyId, StringComparison.OrdinalIgnoreCase))
                    continue;

                entry.kills = Mathf.Max(0, entry.kills) + 1;
                return;
            }

            stats.mobKillsByEnemyId.Add(new SaveData.MobKillEntry
            {
                enemyId = enemyId,
                kills = 1
            });
        }

        private static string ResolveEnemyId(EnemyData enemy)
        {
            if (enemy == null)
                return null;

            if (!string.IsNullOrWhiteSpace(enemy.id))
                return enemy.id.Trim();

            if (!string.IsNullOrWhiteSpace(enemy.enemyName))
                return enemy.enemyName.Trim();

            return enemy.name;
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

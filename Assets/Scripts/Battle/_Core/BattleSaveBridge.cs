using System;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// Applies battle entry contexts from SaveData (pending battle marker).
    /// This enables autosaves taken before entering battle (e.g. tutorial) to be loadable into battle.
    /// </summary>
    public static class BattleSaveBridge
    {
        public static bool TryApplyPendingBattle(SaveData save)
        {
            var pending = save?.sceneState?.pendingBattle;
            if (pending == null || !pending.isPending)
                return false;

            var battleScene = string.IsNullOrEmpty(pending.battleSceneName) ? "FightScene" : pending.battleSceneName;
            if (!string.Equals(battleScene, "FightScene", StringComparison.Ordinal))
            {
                // Not an error; just not supported by our hard-coded battle scene name assumptions yet.
#if UNITY_EDITOR
                Debug.LogWarning($"[BattleSaveBridge] pendingBattle.battleSceneName='{battleScene}' (expected 'FightScene'). Contexts will still be applied.");
#endif
            }

            // Mode
            var mode = ParseBattleMode(pending.battleMode);
            BattleEntryContext.Set(mode);

            // Difficulty
            if (!string.IsNullOrEmpty(pending.enemyDifficulty) && Enum.TryParse(pending.enemyDifficulty, ignoreCase: true, out EnemyDifficulty diff))
                BattleEnemyDifficultyContext.Set(diff);

            // Return scene
            if (!string.IsNullOrEmpty(pending.returnSceneName))
                BattleExitContext.SetReturnToScene(pending.returnSceneName);

            // Optional enemy/location resolution
            bool needsEnemyResolve = !string.IsNullOrEmpty(pending.enemyId);
            bool needsLocationResolve = !string.IsNullOrEmpty(pending.locationId);
            if (needsEnemyResolve || needsLocationResolve)
            {
                var db = BattleContentDatabaseProvider.GetOrLoad();
                if (db != null)
                {
                    if (needsEnemyResolve && db.TryGetEnemy(pending.enemyId, out var enemy) && enemy != null)
                        BattleEnemyContext.Set(enemy);

                    if (needsLocationResolve && db.TryGetLocation(pending.locationId, out var location) && location != null)
                        BattleLocationContext.Set(location);
                }
            }

            return true;
        }

        private static BattleMode ParseBattleMode(string value)
        {
            if (!string.IsNullOrEmpty(value) && Enum.TryParse(value, ignoreCase: true, out BattleMode mode))
                return mode;

            return BattleMode.Normal;
        }
    }
}

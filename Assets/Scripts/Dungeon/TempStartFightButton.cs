using Game.Battle;
using UDA2.SceneFlow;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Dungeon
{
    /// <summary>
    /// TEMP: Minimal bridge button for MonsterCaveScene -> FightScene.
    /// TODO: Remove this component once the real dungeon location UI/flow is in place.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TempStartFightButton : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button button;

        [Header("Battle")]
        [SerializeField] private string fightSceneName = "FightScene";

        [Tooltip("Optional explicit enemy. If not set, we will resolve from enemyTable.")]
        [SerializeField] private EnemyData enemy;

        [Tooltip("Optional fallback table if enemy is not set.")]
        [SerializeField] private EnemySpawnTable enemyTable;

        [Tooltip("Optional battle location (background/music/etc).")]
        [SerializeField] private BattleLocationData location;

        [Tooltip("If enabled, sets BattleExitContext to return to the current scene.")]
        [SerializeField] private bool returnToActiveSceneAfterBattle = true;

        private bool isStartingBattle;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
                button.onClick.AddListener(OnClick);
            }
            else
            {
                Debug.LogError("[TempStartFightButton] Button reference is missing.");
            }

            // Start background preloading FightScene immediately when this button
            // becomes active — the player is likely to click it soon.
            // The scene loads quietly at low priority in the background (up to 90%),
            // so when the player actually clicks, activation is near-instant.
            if (!string.IsNullOrEmpty(fightSceneName) && SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.PreloadScene(fightSceneName);
        }

        private void OnDestroy()
        {
            // If the player leaves the scene without starting a battle,
            // release the preloaded FightScene from memory.
            if (!isStartingBattle && !string.IsNullOrEmpty(fightSceneName) && SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.CancelPreload(fightSceneName);
        }

        private void OnClick()
        {
            if (isStartingBattle)
                return;

            if (!HasAlivePlayer())
            {
                Debug.LogWarning("[TempStartFightButton] Cannot start battle: player HP is 0. Restore HP before entering battle.");
                if (button != null)
                    button.interactable = false;
                return;
            }

            if (string.IsNullOrEmpty(fightSceneName))
            {
                Debug.LogError("[TempStartFightButton] fightSceneName is empty.");
                return;
            }

            var resolvedEnemy = enemy;
            int resolvedEnemyLevel = Mathf.Max(1, resolvedEnemy != null ? resolvedEnemy.minSpawnLevel : 1);
            int resolvedEnemyRankTier = Mathf.Max(0, resolvedEnemy != null ? resolvedEnemy.minSpawnRankTier : 0);
            if (resolvedEnemy == null && enemyTable != null)
            {
                var resolver = new EnemySpawnResolver();
                if (!resolver.Resolve(enemyTable, EnemySpawnConstraints.Default, out resolvedEnemy, out resolvedEnemyLevel, out resolvedEnemyRankTier))
                    resolvedEnemy = null;
            }

            if (resolvedEnemy == null)
            {
                Debug.LogError("[TempStartFightButton] No enemy assigned and enemyTable resolved to null.");
                return;
            }

            // Ensure a save container exists so pending battle can be restored after reloads.
            if (GameState.Instance != null && GameState.Instance.CurrentSave == null)
                GameState.Instance.CurrentSave = SaveData.CreateDefault(Application.version);

            BattleEntryContext.Set(BattleMode.Normal);

            if (location != null)
                BattleLocationContext.Set(location);

            BattleEnemyContext.Set(resolvedEnemy, resolvedEnemyLevel, resolvedEnemyRankTier);

            if (returnToActiveSceneAfterBattle)
                BattleExitContext.SetReturnToActiveScene();

            // Mark pending battle so Load can reconstruct contexts.
            var save = GameState.Instance != null ? GameState.Instance.CurrentSave : null;
            if (save != null && save.sceneState != null && save.sceneState.pendingBattle != null)
            {
                var pending = save.sceneState.pendingBattle;
                pending.isPending = true;
                pending.battleSceneName = fightSceneName;
                pending.battleMode = "Normal";
                pending.returnSceneName = returnToActiveSceneAfterBattle ? SceneManager.GetActiveScene().name : null;
                pending.enemyDifficulty = "Normal";
                pending.enemyId = resolvedEnemy != null ? resolvedEnemy.id : null;
                pending.locationId = location != null ? location.id : null;
            }

            // Note: save.player.sceneName is updated automatically by SceneStateRuntimeTracker
            // when FightScene becomes active. No need to set it here.

            isStartingBattle = true;
            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.LoadScene(
                    fightSceneName,
                    new SceneTransitionData
                    {
                        SkipSceneLoadTasks = true,
                        SkipSceneReadyWait = true,
                        SkipMusicWait = true,
                        DisableFakeProgressEnvelope = true
                    },
                    0f);
            else
                SceneManager.LoadSceneAsync(fightSceneName);
        }

        private static bool HasAlivePlayer()
        {
            var stats = GameState.Instance?.CurrentSave?.player?.stats;
            return stats != null && stats.hp > 0;
        }
    }
}

using System;
using Game.Battle;
using Game.Progression;
using UDA2.SceneFlow;
using Logger = UDA2.Logging.Logger;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Dungeon
{
    public sealed class DungeonLocationSelectController : MonoBehaviour
    {
        [Serializable]
        public sealed class ButtonBinding
        {
            public Button button;
            public DungeonLocationDefinition location;
            [Header("Requirements")]
            public AdventurerRank requiredRank = AdventurerRank.None;
            [Min(1)] public int requiredPlayerLevel = 1;

            [NonSerialized] public Image cachedImage;
        }

        [Header("Buttons")]
        [SerializeField] private ButtonBinding[] buttons;

        [Header("UI Style")]
        [Range(0.1f,1f)]
        [SerializeField] private float locationButtonAlpha = 1f;

        [Header("Debug")]
        [SerializeField] private bool debugIgnoreRankLock;
        [SerializeField] private bool debugIgnoreLevelLock;

        [Header("Startup")]
        [SerializeField, Min(0)] private int deferFirstRefreshFrames = 1;

        private Coroutine deferredRefreshRoutine;

        private void Awake()
        {
            WireButtons();
            CacheButtonImages();
            ApplyLocationButtonAlpha();
        }

        private void OnDisable()
        {
            if (deferredRefreshRoutine != null)
            {
                StopCoroutine(deferredRefreshRoutine);
                deferredRefreshRoutine = null;
            }
        }

        private void ApplyLocationButtonAlpha()
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                var binding = buttons[i];
                if (binding?.button == null) continue;
                var img = binding.cachedImage;
                if (img != null)
                {
                    var c = img.color;
                    c.a = locationButtonAlpha;
                    img.color = c;
                }
            }
        }

        private void OnEnable()
        {
            if (deferredRefreshRoutine != null)
                StopCoroutine(deferredRefreshRoutine);

            deferredRefreshRoutine = StartCoroutine(DeferredFirstRefreshRoutine());
        }

        private System.Collections.IEnumerator DeferredFirstRefreshRoutine()
        {
            int frames = Mathf.Max(0, deferFirstRefreshFrames);
            while (frames > 0)
            {
                frames--;
                yield return null;
            }

            deferredRefreshRoutine = null;
            RefreshInteractable();
        }

        private void CacheButtonImages()
        {
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                var binding = buttons[i];
                if (binding == null || binding.button == null)
                    continue;

                binding.cachedImage = binding.button.GetComponent<Image>();
            }
        }

        private void WireButtons()
        {
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                var binding = buttons[i];
                if (binding == null || binding.button == null)
                    continue;

                binding.button.onClick.RemoveListener(OnAnyButtonClicked);
                binding.button.onClick.AddListener(OnAnyButtonClicked);
            }
        }

        private void OnAnyButtonClicked()
        {
            // Find which button fired.
            var clicked = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            if (clicked == null)
                return;

            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                var binding = buttons[i];
                if (binding == null || binding.button == null || binding.location == null)
                    continue;

                if (binding.button.gameObject == clicked)
                {
                    TryStartLocation(binding);
                    return;
                }
            }
        }

        public void RefreshInteractable()
        {
            var playerRank = GetPlayerRank();
            var playerLevel = GetPlayerLevel();
            bool canEnterBattle = HasAlivePlayer();

            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                var binding = buttons[i];
                if (binding == null || binding.button == null)
                    continue;

                if (binding.location == null)
                {
                    binding.button.interactable = false;
                    continue;
                }

                binding.button.interactable = canEnterBattle && IsBindingAvailableFor(binding, playerRank, playerLevel);
            }
        }

        private bool IsBindingAvailableFor(ButtonBinding binding, AdventurerRank playerRank, int playerLevel)
        {
            if (binding == null || binding.location == null)
                return false;

            var requiredRank = binding.requiredRank;
            var requiredLevel = Mathf.Max(1, binding.requiredPlayerLevel);

            if (binding.location.requiredRank > requiredRank)
                requiredRank = binding.location.requiredRank;

            requiredLevel = Mathf.Max(requiredLevel, binding.location.requiredPlayerLevel);

            var rankOk = debugIgnoreRankLock || playerRank >= requiredRank;
            var levelOk = debugIgnoreLevelLock || playerLevel >= requiredLevel;
            return rankOk && levelOk;
        }

        private AdventurerRank GetPlayerRank()
        {
            var save = GameState.Instance != null ? GameState.Instance.CurrentSave : null;
            if (save?.progress == null)
                return AdventurerRank.None;

            return save.progress.adventurerRank;
        }

        private int GetPlayerLevel()
        {
            var save = GameState.Instance != null ? GameState.Instance.CurrentSave : null;
            return Mathf.Max(1, save?.player?.level ?? 1);
        }

        private void TryStartLocation(ButtonBinding binding)
        {
            if (binding == null)
                return;

            if (!HasAlivePlayer())
            {
                Debug.LogWarning("[Dungeon] Cannot start battle: player HP is 0. Restore HP before entering battle.");
                RefreshInteractable();
                return;
            }

            var location = binding.location;
            if (location == null)
                return;

            var playerRank = GetPlayerRank();
            var playerLevel = GetPlayerLevel();
            if (!IsBindingAvailableFor(binding, playerRank, playerLevel))
            {
#if UNITY_EDITOR
                var requiredRank = binding.requiredRank > location.requiredRank ? binding.requiredRank : location.requiredRank;
                var requiredLevel = Mathf.Max(Mathf.Max(1, binding.requiredPlayerLevel), location.requiredPlayerLevel);
                Debug.LogWarning($"[Dungeon] Location '{location.name}' locked. RequiredRank={requiredRank}, PlayerRank={playerRank}, RequiredLevel={requiredLevel}, PlayerLevel={playerLevel}");
#endif
                return;
            }

            if (string.IsNullOrEmpty(location.fightSceneName))
            {
                Debug.LogError("[Dungeon] fightSceneName is empty. Cannot start fight.");
                return;
            }

            if (!TryResolveEncounter(location, playerRank, playerLevel, out var battleLocation, out var enemy, out var enemyLevel, out var enemyRankTier))
                return;

            // Ensure we have a save container so pending battle can be restored after reloads.
            if (GameState.Instance != null && GameState.Instance.CurrentSave == null)
                GameState.Instance.CurrentSave = SaveData.CreateDefault(Application.version);

            // Set battle contexts.
            BattleEntryContext.Set(BattleMode.Normal);

            if (battleLocation != null)
                BattleLocationContext.Set(battleLocation);

            if (enemy != null)
                BattleEnemyContext.Set(enemy, enemyLevel, enemyRankTier);

            if (location.returnToActiveSceneAfterBattle)
                BattleExitContext.SetReturnToActiveScene();

            // Mark pending battle so saves/autosaves can restore battle contexts.
            var save = GameState.Instance != null ? GameState.Instance.CurrentSave : null;
            if (save != null && save.sceneState != null && save.sceneState.pendingBattle != null)
            {
                var pending = save.sceneState.pendingBattle;
                pending.isPending = true;
                pending.battleSceneName = location.fightSceneName;
                pending.battleMode = "Normal";
                pending.returnSceneName = location.returnToActiveSceneAfterBattle ? SceneManager.GetActiveScene().name : null;
                pending.enemyDifficulty = "Normal";
                pending.enemyId = enemy != null ? enemy.id : null;
                // Keep source world location id (dld_*) for outcome presentation matching.
                pending.locationId = location != null ? location.id : null;
            }

            // Keep SaveData's current scene name aligned with the transition.
            if (save?.player != null)
                save.player.SetSceneName(location.fightSceneName);

            // Load FightScene.
            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.LoadScene(location.fightSceneName);
            else
                SceneManager.LoadScene(location.fightSceneName);
        }

        private static bool TryResolveEncounter(
            DungeonLocationDefinition location,
            AdventurerRank playerRank,
            int playerLevel,
            out BattleLocationData resolvedBattleLocation,
            out EnemyData resolvedEnemy,
            out int resolvedEnemyLevel,
            out int resolvedEnemyRankTier)
        {
            resolvedBattleLocation = null;
            resolvedEnemy = null;
            resolvedEnemyLevel = 1;
            resolvedEnemyRankTier = 0;

            if (location.encounterPools == null || location.encounterPools.Length == 0)
            {
                Debug.LogError($"[Dungeon] Location '{location.name}' has no encounterPools.");
                return false;
            }

            int totalWeight = 0;
            for (int i = 0; i < location.encounterPools.Length; i++)
            {
                var pool = location.encounterPools[i];
                if (pool == null)
                    continue;

                if (pool.weight <= 0)
                    continue;

                if (pool.battleLocation == null || pool.enemyTable == null)
                    continue;

                totalWeight += pool.weight;
            }

            if (totalWeight <= 0)
            {
                Debug.LogError($"[Dungeon] Location '{location.name}' encounterPools have no valid weighted entries (need battleLocation+enemyTable+weight>0).");
                return false;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            DungeonEncounterPool chosen = null;

            for (int i = 0; i < location.encounterPools.Length; i++)
            {
                var pool = location.encounterPools[i];
                if (pool == null)
                    continue;

                if (pool.weight <= 0)
                    continue;

                if (pool.battleLocation == null || pool.enemyTable == null)
                    continue;

                roll -= pool.weight;
                if (roll < 0)
                {
                    chosen = pool;
                    break;
                }
            }

            if (chosen == null)
            {
                Debug.LogError($"[Dungeon] Location '{location.name}' failed to choose encounter pool.");
                return false;
            }

            resolvedBattleLocation = chosen.battleLocation;

            var resolver = new EnemySpawnResolver();

            // Clamp encounter bounds by current player progression to prevent out-of-tier early spawns.
            var effectiveMinLevel = Mathf.Max(1, location.minEnemyLevel);
            var effectiveMaxLevel = Mathf.Clamp(playerLevel, effectiveMinLevel, location.maxEnemyLevel);

            var locationMinRank = (int)location.minEnemyRank;
            var locationMaxRank = (int)location.maxEnemyRank;
            var effectiveMaxRank = Mathf.Clamp((int)playerRank, locationMinRank, locationMaxRank);

            var constraints = new EnemySpawnConstraints(
                effectiveMinLevel,
                effectiveMaxLevel,
                locationMinRank,
                effectiveMaxRank);

            Logger.LogInfo(
                $"[Dungeon] Resolve encounter location='{location.name}' playerLevel={playerLevel} playerRank={(int)playerRank} " +
                $"locationBounds(level={location.minEnemyLevel}-{location.maxEnemyLevel}, rank={(int)location.minEnemyRank}-{(int)location.maxEnemyRank}) " +
                $"effectiveBounds(level={effectiveMinLevel}-{effectiveMaxLevel}, rank={locationMinRank}-{effectiveMaxRank})",
                UDA2.Logging.LogChannel.AI);
            Logger.FlushToFile();

            if (!resolver.Resolve(chosen.enemyTable, constraints, out resolvedEnemy, out resolvedEnemyLevel, out resolvedEnemyRankTier))
            {
                Debug.LogError($"[Dungeon] Location '{location.name}' chosen pool has enemyTable '{chosen.enemyTable.name}', but resolver failed with constraints.");
                return false;
            }

            if (resolvedEnemy == null)
            {
                Debug.LogError($"[Dungeon] Location '{location.name}' chosen pool has enemyTable '{chosen.enemyTable.name}', but it resolved to null enemy.");
                return false;
            }

            return true;
        }

        private static bool HasAlivePlayer()
        {
            var stats = GameState.Instance?.CurrentSave?.player?.stats;
            return stats != null && stats.hp > 0;
        }
    }
}

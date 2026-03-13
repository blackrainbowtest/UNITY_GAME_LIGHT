using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UDA2.SaveSystem
{
    /// <summary>
    /// Keeps SaveData scene fields in sync with the active scene.
    /// Created automatically at runtime (no scene setup required).
    /// </summary>
    public sealed class SceneStateRuntimeTracker : MonoBehaviour
    {
        private static bool _created;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCreated()
        {
            if (_created)
                return;

            _created = true;
            var go = new GameObject("[SceneStateRuntimeTracker]");
            DontDestroyOnLoad(go);
            go.AddComponent<SceneStateRuntimeTracker>();
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void Start()
        {
            ApplyScene(SceneManager.GetActiveScene().name);
        }

        private void OnActiveSceneChanged(Scene from, Scene to)
        {
            StorePreviousScene(from, to);
            ApplyScene(to.name);
        }

        private static void StorePreviousScene(Scene from, Scene to)
        {
            var save = global::GameState.Instance.CurrentSave;
            if (save == null || save.sceneState == null)
                return;

            if (!from.IsValid() || string.IsNullOrEmpty(from.name))
                return;

            if (string.Equals(from.name, to.name, StringComparison.Ordinal))
                return;

            // LoadingScene is a technical transit scene and should not become a back target.
            if (string.Equals(from.name, "LoadingScene", StringComparison.Ordinal))
                return;

            save.sceneState.previousSceneName = from.name;
        }

        private static void ApplyScene(string sceneName)
        {
            var save = global::GameState.Instance.CurrentSave;
            if (save == null || save.player == null)
                return;

            // Never persist transit loading scene into save state.
            if (string.Equals(sceneName, "LoadingScene", StringComparison.Ordinal))
                return;

            // Keep current scene up to date.
            save.player.SetSceneName(sceneName);

            // If we have sceneState, maintain it.
            if (save.sceneState == null)
                return;

            var category = SceneCategoryResolver.GetCategory(sceneName);

            // Leaving battle: clear pending battle marker so future saves are clean.
            if (category != SceneCategory.Battle && save.sceneState.pendingBattle != null && save.sceneState.pendingBattle.isPending)
            {
                save.sceneState.pendingBattle.Clear();
            }

            // Track last main city (requires explicit setup later; currently Unknown will not overwrite).
            if (category == SceneCategory.Main)
            {
                save.sceneState.lastMainSceneName = sceneName;
            }

            if (SceneCategoryResolver.IsShelterScene(sceneName))
            {
                save.sceneState.lastShelterSceneName = sceneName;
            }

            // Deferred autosave request (e.g. after tutorial battle victory).
            // We do it here so that:
            // - sceneName is already updated
            // - pendingBattle is already cleared when leaving battle
            if (save.sceneState.requestAutosaveOnSceneEnter)
            {
                var filter = save.sceneState.requestAutosaveSceneName;
                bool sceneMatches = string.IsNullOrEmpty(filter) || string.Equals(filter, sceneName, StringComparison.Ordinal);
                bool allowed = SceneCategoryResolver.IsSaveAllowed(sceneName);

                if (sceneMatches && allowed)
                {
                    global::SaveSlotsManager.SaveToSlot(0, save);
                    save.sceneState.ClearAutosaveRequest();
                }
            }
        }
    }
}

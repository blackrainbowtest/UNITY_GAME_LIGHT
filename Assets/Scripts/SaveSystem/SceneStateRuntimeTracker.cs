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
            ApplyScene(to.name);
        }

        private static void ApplyScene(string sceneName)
        {
            var save = global::GameState.Instance.CurrentSave;
            if (save == null || save.player == null)
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
        }
    }
}

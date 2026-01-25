using System;

namespace UDA2.SaveSystem
{
    public static class SceneCategoryResolver
    {
        public static SceneCategory GetCategory(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return SceneCategory.Unknown;

            // Hard-coded known scenes
            if (string.Equals(sceneName, "FightScene", StringComparison.Ordinal))
                return SceneCategory.Battle;

            if (string.Equals(sceneName, "IntroScene", StringComparison.Ordinal)
                || string.Equals(sceneName, "MainMenuScene", StringComparison.Ordinal)
                || string.Equals(sceneName, "SplashScene", StringComparison.Ordinal)
                || string.Equals(sceneName, "LoadingScene", StringComparison.Ordinal))
                return SceneCategory.View;

            if (string.Equals(sceneName, "StartCityScene", StringComparison.Ordinal))
                return SceneCategory.Main;

            // Heuristics (can be replaced later by a proper catalog)
            if (sceneName.IndexOf("cutscene", StringComparison.OrdinalIgnoreCase) >= 0
                || sceneName.IndexOf("view", StringComparison.OrdinalIgnoreCase) >= 0)
                return SceneCategory.View;

            if (sceneName.IndexOf("fight", StringComparison.OrdinalIgnoreCase) >= 0
                || sceneName.IndexOf("battle", StringComparison.OrdinalIgnoreCase) >= 0)
                return SceneCategory.Battle;

            // Basic gameplay heuristics
            if (sceneName.IndexOf("city", StringComparison.OrdinalIgnoreCase) >= 0)
                return SceneCategory.Main;

            if (sceneName.IndexOf("interior", StringComparison.OrdinalIgnoreCase) >= 0
                || sceneName.IndexOf("shop", StringComparison.OrdinalIgnoreCase) >= 0
                || sceneName.IndexOf("craft", StringComparison.OrdinalIgnoreCase) >= 0
                || sceneName.IndexOf("house", StringComparison.OrdinalIgnoreCase) >= 0
                || sceneName.IndexOf("room", StringComparison.OrdinalIgnoreCase) >= 0)
                return SceneCategory.Secondary;

            return SceneCategory.Unknown;
        }

        public static bool IsSaveAllowed(string sceneName)
        {
            var category = GetCategory(sceneName);
            return category != SceneCategory.Battle && category != SceneCategory.View;
        }

        public static bool IsMainScene(string sceneName)
        {
            return GetCategory(sceneName) == SceneCategory.Main;
        }
    }
}

using UnityEngine.SceneManagement;

namespace Game.Battle
{
    /// <summary>
    /// Runtime context for passing "where to return after battle" between scenes.
    /// One-time use by default (Consume).
    /// </summary>
    public static class BattleExitContext
    {
        private static BattleExitData data;

        public static void SetReturnToScene(string sceneName)
        {
            Set(new BattleExitData(sceneName));
        }

        public static void SetReturnToActiveScene()
        {
            SetReturnToScene(SceneManager.GetActiveScene().name);
        }

        public static void Set(BattleExitData exitData)
        {
            data = exitData;
        }

        public static BattleExitData Peek() => data;

        public static BattleExitData Consume()
        {
            var result = data;
            data = null;
            return result;
        }

        public static void Clear()
        {
            data = null;
        }
    }
}

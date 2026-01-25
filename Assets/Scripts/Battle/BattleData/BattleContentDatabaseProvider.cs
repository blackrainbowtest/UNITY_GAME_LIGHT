using UnityEngine;

namespace Game.Battle
{
    public static class BattleContentDatabaseProvider
    {
        private const string ResourcesPath = "Game/Battle/BattleContentDatabase";
        private static BattleContentDatabase _cached;

        public static BattleContentDatabase GetOrLoad()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<BattleContentDatabase>(ResourcesPath);
            if (_cached == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[BattleContentDatabaseProvider] Missing Resources asset at 'Assets/Resources/{ResourcesPath}.asset'. Pending battle loads may not restore enemy/location.");
#endif
            }

            return _cached;
        }
    }
}

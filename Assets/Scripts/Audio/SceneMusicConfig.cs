using System;
using System.Collections.Generic;
using UnityEngine;

namespace UDA2.Audio
{
    [CreateAssetMenu(menuName = "_Audio/Scene Music Config", fileName = "SceneMusicConfig")]
    public sealed class SceneMusicConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string sceneName;
            public AudioCue musicCue;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        private Dictionary<string, Entry> _byScene;

        private void OnEnable()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        private void RebuildLookup()
        {
            _byScene = new Dictionary<string, Entry>(StringComparer.Ordinal);
            if (entries == null)
                return;

            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (string.IsNullOrWhiteSpace(e.sceneName))
                    continue;

                _byScene[e.sceneName] = e;
            }
        }

        public bool TryGet(string sceneName, out Entry entry)
        {
            entry = default;
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            if (_byScene == null)
                RebuildLookup();

            return _byScene.TryGetValue(sceneName, out entry);
        }
    }
}

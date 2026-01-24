using System;
using System.Collections.Generic;
using UnityEngine;

namespace UDA2.Audio
{
    [CreateAssetMenu(menuName = "_Audio/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
		[SerializeField] private AudioCue[] cueAssets = Array.Empty<AudioCue>();

        private Dictionary<(AudioCategory category, string key), AudioClip> _lookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        public void BuildLookup()
        {
            _lookup = new Dictionary<(AudioCategory, string), AudioClip>();

            if (cueAssets == null)
                return;

            for (int i = 0; i < cueAssets.Length; i++)
            {
                var cue = cueAssets[i];
                if (cue == null)
                    continue;

                if (string.IsNullOrWhiteSpace(cue.Key))
                    continue;

                if (cue.Clip == null)
                    continue;

                _lookup[(cue.Category, cue.Key.Trim())] = cue.Clip;
            }
        }

        public bool TryGetClip(AudioCategory category, string key, out AudioClip clip)
        {
            clip = null;

            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (_lookup == null)
                BuildLookup();

            return _lookup.TryGetValue((category, key.Trim()), out clip) && clip != null;
        }

        public AudioClip GetOrNull(AudioCategory category, string key)
        {
            return TryGetClip(category, key, out var clip) ? clip : null;
        }
    }
}

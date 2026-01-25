using UnityEngine;
using UDA2.Audio;

namespace Game.Battle
{
    [CreateAssetMenu(menuName = "Game/Battle/Location")]
    public class BattleLocationData : ScriptableObject
    {
        [Header("Meta")]
        [Tooltip("Stable identifier used for saves (runtime). If empty, will be auto-filled from asset name.")]
        public string id;

        [Header("Visuals")]
        public Sprite background;

        [Header("Audio")]
        public AudioCue musicCue;

        // Backward compatibility: older locations may still use a raw clip.
        public AudioClip music;

        private void OnEnable()
        {
            EnsureId();
        }

        private void OnValidate()
        {
            EnsureId();
        }

        private void EnsureId()
        {
            if (!string.IsNullOrEmpty(id))
                return;

            id = SanitizeId(name);
        }

        private static string SanitizeId(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "location";

            value = value.Trim().ToLowerInvariant();
            value = value.Replace(' ', '_');
            return value;
        }
    }
}

using UnityEngine;

namespace UDA2.Audio
{
    [CreateAssetMenu(menuName = "_Audio/Audio Cue", fileName = "AudioCue")]
    public sealed class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioCategory category = AudioCategory.Sfx;
        [SerializeField] private string key;
        [SerializeField] private AudioClip clip;

        [Header("Optional Defaults")]
        [SerializeField, Range(0f, 1f)] private float defaultVolume = 1f;
        [SerializeField] private Vector2 pitchRange = new Vector2(1f, 1f);

        public AudioCategory Category => category;
        public string Key => key;
        public AudioClip Clip => clip;
        public float DefaultVolume => defaultVolume;
        public Vector2 PitchRange => pitchRange;
    }
}

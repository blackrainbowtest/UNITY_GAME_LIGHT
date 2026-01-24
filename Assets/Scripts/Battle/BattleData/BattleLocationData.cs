using UnityEngine;
using UDA2.Audio;

namespace Game.Battle
{
    [CreateAssetMenu(menuName = "Game/Battle/Location")]
    public class BattleLocationData : ScriptableObject
    {
        [Header("Visuals")]
        public Sprite background;

        [Header("Audio")]
        public AudioCue musicCue;

        // Backward compatibility: older locations may still use a raw clip.
        public AudioClip music;
    }
}

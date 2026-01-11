using UnityEngine;

namespace Game.Battle
{
    [CreateAssetMenu(menuName = "Game/Battle/Location")]
    public class BattleLocationData : ScriptableObject
    {
        [Header("Visuals")]
        public Sprite background;

        [Header("Audio")]
        public AudioClip music;
    }
}

using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// Applies battle environment visuals and audio based on BattleLocationData.
    /// </summary>
    public class BattleEnvironmentController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private SpriteRenderer backgroundRenderer;


        public void Apply(BattleLocationData location)
        {
            if (location == null)
            {
                Debug.LogError("BattleEnvironmentController: Location is null");
                return;
            }

            ApplyBackground(location);
            ApplyMusic(location);
        }

        private void ApplyBackground(BattleLocationData location)
        {
            if (backgroundRenderer == null)
                return;

            backgroundRenderer.sprite = location.background;
        }

        private void ApplyMusic(BattleLocationData location)
        {
            // Используем глобальный AudioManager для музыки
            if (UDA2.Audio.AudioManager.Instance == null)
            {
                Debug.LogWarning("BattleEnvironmentController: AudioManager.Instance is null. Skipping music setup (likely running battle scene directly). ");
                return;
            }

            if (location.musicCue != null)
            {
                UDA2.Audio.AudioManager.Instance.Play(location.musicCue);
                return;
            }

            if (location.music != null)
            {
                UDA2.Audio.AudioManager.Instance.PlayMusic(location.music);
                return;
            }

            UDA2.Audio.AudioManager.Instance.StopMusic();
        }
    }
}

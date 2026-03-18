using UnityEngine;

namespace UDA2.UI.Game
{
    public sealed class ProfileStatusView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private StatBarView hpBar;
        [SerializeField] private StatBarView mpBar;
        [SerializeField] private StatBarView spBar;
        [SerializeField] private StatBarView lpBar;

        [Header("Behavior")]
        [SerializeField] private bool refreshOnEnable = true;

        private void OnEnable()
        {
            if (refreshOnEnable)
                RefreshFromCurrentSave();
        }

        public void RefreshFromCurrentSave()
        {
            var save = global::GameState.Instance != null ? global::GameState.Instance.CurrentSave : null;
            Refresh(save);
        }

        public void Refresh(SaveData save)
        {
            var stats = save?.player?.stats;
            if (stats == null)
            {
                ApplyNoData();
                return;
            }

            Apply(hpBar, stats.hp, stats.hpMax);
            Apply(mpBar, stats.mp, stats.mpMax);
            Apply(spBar, stats.sp, stats.spMax);
            Apply(lpBar, stats.lp, stats.lpMax);
        }

        private static void Apply(StatBarView bar, int current, int max)
        {
            if (bar == null)
                return;

            int maxSafe = Mathf.Max(0, max);
            int curSafe = Mathf.Clamp(current, 0, maxSafe);
            float normalized = maxSafe <= 0 ? 0f : Mathf.Clamp01(curSafe / (float)maxSafe);

            bar.SetNormalized(normalized);
            bar.SetValue(curSafe, maxSafe);
        }

        private void ApplyNoData()
        {
            Apply(hpBar, 0, 0);
            Apply(mpBar, 0, 0);
            Apply(spBar, 0, 0);
            Apply(lpBar, 0, 0);
        }
    }
}

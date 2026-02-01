using System;
using UnityEngine;
using UnityEngine.UI;
using UDA2.GameTime;

namespace UDA2.UI.Game
{
    public sealed class WorldTintOverlayController : MonoBehaviour
    {
        [Serializable]
        private struct Tint
        {
            public Color color;
            [Range(0f, 1f)] public float alpha;
        }

        [Header("Target")]
        [SerializeField] private Image overlayImage;

        [Header("Transition")]
        [Tooltip("Seconds to smoothly transition between tints. Set to 0 for instant.")]
        [SerializeField] private float transitionSeconds = 0.6f;

        [Tooltip("Use unscaled time so the tint still animates when Time.timeScale=0 (menus/pause).")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Normal Phases")]
        [SerializeField] private Tint dawn = new Tint { color = new Color(1.00f, 0.88f, 0.75f, 1f), alpha = 0.10f };
        [SerializeField] private Tint morning = new Tint { color = new Color(1.00f, 0.98f, 0.90f, 1f), alpha = 0.00f };
        [SerializeField] private Tint noon = new Tint { color = new Color(1.00f, 1.00f, 1.00f, 1f), alpha = 0.00f };
        [SerializeField] private Tint afternoon = new Tint { color = new Color(1.00f, 0.97f, 0.92f, 1f), alpha = 0.02f };
        [SerializeField] private Tint evening = new Tint { color = new Color(1.00f, 0.78f, 0.62f, 1f), alpha = 0.12f };
        [SerializeField] private Tint dusk = new Tint { color = new Color(0.70f, 0.50f, 0.95f, 1f), alpha = 0.18f };
        [SerializeField] private Tint night = new Tint { color = new Color(0.18f, 0.30f, 0.75f, 1f), alpha = 0.25f };

        [Header("Special Nights (00:00..04:59)")]
        [SerializeField] private Tint crystalNight = new Tint { color = new Color(0.55f, 0.25f, 0.95f, 1f), alpha = 0.28f };
        [SerializeField] private Tint lustNight = new Tint { color = new Color(1.00f, 0.25f, 0.65f, 1f), alpha = 0.26f };
        [SerializeField] private Tint fullMoon = new Tint { color = new Color(0.70f, 0.85f, 1.00f, 1f), alpha = 0.18f };

        private Color _currentColor = Color.clear;
        private float _currentAlpha;
        private Color _targetColor = Color.clear;
        private float _targetAlpha;

        private void Awake()
        {
            if (overlayImage == null)
                overlayImage = GetComponent<Image>();

            if (overlayImage != null)
                overlayImage.raycastTarget = false;

            RefreshTargets();
            ApplyImmediate();
        }

        private void OnEnable()
        {
            if (GameTimeService.Instance != null)
                GameTimeService.Instance.TimeChanged += HandleTimeChanged;

            RefreshTargets();
            ApplyImmediate();
        }

        private void OnDisable()
        {
            if (GameTimeService.Instance != null)
                GameTimeService.Instance.TimeChanged -= HandleTimeChanged;
        }

        private void Update()
        {
            if (overlayImage == null)
                return;

            if (transitionSeconds <= 0f)
            {
                ApplyImmediate();
                return;
            }

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0f)
                return;

            float t = 1f - Mathf.Exp(-dt / Mathf.Max(0.0001f, transitionSeconds));

            _currentColor = Color.Lerp(_currentColor, _targetColor, t);
            _currentAlpha = Mathf.Lerp(_currentAlpha, _targetAlpha, t);

            overlayImage.color = new Color(_currentColor.r, _currentColor.g, _currentColor.b, _currentAlpha);
        }

        private void HandleTimeChanged(int dayValue, int minuteOfDay)
        {
            RefreshTargets();
        }

        private void RefreshTargets()
        {
            if (overlayImage == null)
                return;

            // Special night has priority, but only exists in 00:00..04:59.
            var special = GameTimeAPI.NightSpecialPhase;
            if (special != NightSpecialPhase.None)
            {
                Tint s = ResolveSpecialTint(special);
                _targetColor = s.color;
                _targetAlpha = s.alpha;
                return;
            }

            Tint p = ResolvePhaseTint(GameTimeAPI.TimeOfDayPhase);
            _targetColor = p.color;
            _targetAlpha = p.alpha;
        }

        private void ApplyImmediate()
        {
            if (overlayImage == null)
                return;

            _currentColor = _targetColor;
            _currentAlpha = _targetAlpha;
            overlayImage.color = new Color(_currentColor.r, _currentColor.g, _currentColor.b, _currentAlpha);
        }

        private Tint ResolveSpecialTint(NightSpecialPhase phase)
        {
            switch (phase)
            {
                case NightSpecialPhase.CrystalNight: return crystalNight;
                case NightSpecialPhase.LustNight: return lustNight;
                case NightSpecialPhase.FullMoon: return fullMoon;
                default: return night;
            }
        }

        private Tint ResolvePhaseTint(TimeOfDayPhase phase)
        {
            switch (phase)
            {
                case TimeOfDayPhase.Dawn: return dawn;
                case TimeOfDayPhase.Morning: return morning;
                case TimeOfDayPhase.Noon: return noon;
                case TimeOfDayPhase.Afternoon: return afternoon;
                case TimeOfDayPhase.Evening: return evening;
                case TimeOfDayPhase.Dusk: return dusk;
                default: return night;
            }
        }
    }
}

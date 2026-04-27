using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UDA2.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuBackgroundCycleController : MonoBehaviour, IPointerClickHandler
    {
        [Serializable]
        public sealed class ImageBinding
        {
            public Image targetImage;
            public Sprite sprite;
            [Range(0f, 1f)] public float targetAlpha = 1f;
        }

        [Serializable]
        public sealed class BackgroundPreset
        {
            public string name;
            public List<ImageBinding> images = new List<ImageBinding>();
        }

        [Header("Presets")]
        [SerializeField] private List<BackgroundPreset> presets = new List<BackgroundPreset>();
        [SerializeField] private int startPresetIndex;

        [Header("Input")]
        [SerializeField] private bool switchOnPointerClick = true;

        [Header("Transition")]
        [SerializeField, Min(0f)] private float transitionSeconds = 0.35f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Startup")]
        [SerializeField] private bool applyStartPresetOnEnable = true;

        private readonly List<ImageTransitionState> imageTransitionStates = new List<ImageTransitionState>();

        private Coroutine transitionRoutine;
        private int currentPresetIndex = -1;

        private sealed class ImageTransitionState
        {
            public Image target;
            public Image overlay;
            public float startTargetAlpha;
            public float endTargetAlpha;
            public float startOverlayAlpha;
        }

        private void OnEnable()
        {
            if (!applyStartPresetOnEnable || presets.Count == 0)
                return;

            int index = Mathf.Clamp(startPresetIndex, 0, presets.Count - 1);
            ApplyPreset(index, instant: true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!switchOnPointerClick)
                return;

            NextPreset();
        }

        public void NextPreset()
        {
            if (presets.Count == 0)
                return;

            int next = currentPresetIndex < 0
                ? Mathf.Clamp(startPresetIndex, 0, presets.Count - 1)
                : (currentPresetIndex + 1) % presets.Count;

            ApplyPreset(next, instant: false);
        }

        public void PreviousPreset()
        {
            if (presets.Count == 0)
                return;

            int prev;
            if (currentPresetIndex < 0)
            {
                prev = Mathf.Clamp(startPresetIndex, 0, presets.Count - 1);
            }
            else
            {
                prev = (currentPresetIndex - 1 + presets.Count) % presets.Count;
            }

            ApplyPreset(prev, instant: false);
        }

        public void SetPresetByIndex(int index)
        {
            if (presets.Count == 0)
                return;

            int clamped = Mathf.Clamp(index, 0, presets.Count - 1);
            ApplyPreset(clamped, instant: false);
        }

        public void SetPresetByName(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName) || presets.Count == 0)
                return;

            for (int i = 0; i < presets.Count; i++)
            {
                if (string.Equals(presets[i].name, presetName, StringComparison.OrdinalIgnoreCase))
                {
                    ApplyPreset(i, instant: false);
                    return;
                }
            }
        }

        private void ApplyPreset(int index, bool instant)
        {
            if (index < 0 || index >= presets.Count)
                return;

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
                CleanupOverlays();
            }

            var preset = presets[index];
            currentPresetIndex = index;

            if (instant || transitionSeconds <= 0f)
            {
                ApplyImagesInstant(preset);
                return;
            }

            transitionRoutine = StartCoroutine(TransitionToPresetRoutine(preset));
        }

        private void ApplyImagesInstant(BackgroundPreset preset)
        {
            if (preset.images == null)
                return;

            for (int i = 0; i < preset.images.Count; i++)
            {
                var binding = preset.images[i];
                if (binding == null || binding.targetImage == null)
                    continue;

                binding.targetImage.sprite = binding.sprite;
                var c = binding.targetImage.color;
                c.a = Mathf.Clamp01(binding.targetAlpha);
                binding.targetImage.color = c;
            }
        }

        private IEnumerator TransitionToPresetRoutine(BackgroundPreset preset)
        {
            BuildImageTransitionStates(preset);

            if (imageTransitionStates.Count == 0)
            {
                transitionRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.0001f, transitionSeconds);

            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);

                for (int i = 0; i < imageTransitionStates.Count; i++)
                {
                    var state = imageTransitionStates[i];
                    if (state.target != null)
                    {
                        var tc = state.target.color;
                        tc.a = Mathf.Lerp(state.startTargetAlpha, state.endTargetAlpha, eased);
                        state.target.color = tc;
                    }

                    if (state.overlay != null)
                    {
                        var oc = state.overlay.color;
                        oc.a = Mathf.Lerp(state.startOverlayAlpha, 0f, eased);
                        state.overlay.color = oc;
                    }
                }

                yield return null;
            }

            for (int i = 0; i < imageTransitionStates.Count; i++)
            {
                var state = imageTransitionStates[i];
                if (state.target != null)
                {
                    var c = state.target.color;
                    c.a = state.endTargetAlpha;
                    state.target.color = c;
                }
            }

            CleanupOverlays();
            transitionRoutine = null;
        }

        private void BuildImageTransitionStates(BackgroundPreset preset)
        {
            CleanupOverlays();

            if (preset.images != null)
            {
                for (int i = 0; i < preset.images.Count; i++)
                {
                    var binding = preset.images[i];
                    if (binding == null || binding.targetImage == null)
                        continue;

                    var target = binding.targetImage;
                    float endAlpha = Mathf.Clamp01(binding.targetAlpha);
                    float startTargetAlpha = target.color.a;

                    Image overlay = CreateOverlayFrom(target);
                    if (overlay != null)
                    {
                        overlay.sprite = target.sprite;
                        var oc = overlay.color;
                        oc.a = target.color.a;
                        overlay.color = oc;
                    }

                    target.sprite = binding.sprite;
                    var tc = target.color;
                    tc.a = 0f;
                    target.color = tc;

                    imageTransitionStates.Add(new ImageTransitionState
                    {
                        target = target,
                        overlay = overlay,
                        startTargetAlpha = 0f,
                        endTargetAlpha = endAlpha,
                        startOverlayAlpha = overlay != null ? overlay.color.a : 0f
                    });
                }
            }
        }

        private void CleanupOverlays()
        {
            for (int i = 0; i < imageTransitionStates.Count; i++)
            {
                var overlay = imageTransitionStates[i].overlay;
                if (overlay != null)
                    Destroy(overlay.gameObject);
            }

            imageTransitionStates.Clear();
        }

        private void OnDisable()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            CleanupOverlays();
        }

        private static Image CreateOverlayFrom(Image source)
        {
            if (source == null)
                return null;

            var sourceRect = source.rectTransform;
            var parent = sourceRect.parent;
            if (parent == null)
                return null;

            var go = new GameObject(source.name + "_TransitionOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = sourceRect.anchorMin;
            rect.anchorMax = sourceRect.anchorMax;
            rect.pivot = sourceRect.pivot;
            rect.sizeDelta = sourceRect.sizeDelta;
            rect.anchoredPosition = sourceRect.anchoredPosition;
            rect.localRotation = sourceRect.localRotation;
            rect.localScale = sourceRect.localScale;
            rect.SetSiblingIndex(sourceRect.GetSiblingIndex() + 1);

            var overlay = go.GetComponent<Image>();
            overlay.material = source.material;
            overlay.color = source.color;
            overlay.type = source.type;
            overlay.preserveAspect = source.preserveAspect;
            overlay.fillCenter = source.fillCenter;
            overlay.fillMethod = source.fillMethod;
            overlay.fillAmount = source.fillAmount;
            overlay.fillClockwise = source.fillClockwise;
            overlay.fillOrigin = source.fillOrigin;
            overlay.useSpriteMesh = source.useSpriteMesh;
            overlay.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            overlay.maskable = source.maskable;
            overlay.raycastTarget = false;
            return overlay;
        }

        private void OnValidate()
        {
            if (startPresetIndex < 0)
                startPresetIndex = 0;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UDA2.UI.Common
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class RectSizeByParent : MonoBehaviour
    {
        public enum ResizeMode
        {
            PercentOfParent,
            ReferenceScale,
        }

        public enum UniformScaleSource
        {
            Min,
            Width,
            Height,
            Max,
        }

        [Header("References")]
        [SerializeField] private RectTransform parentRect;
        [SerializeField] private RectTransform targetRect;

        [Header("Mode")]
        [SerializeField] private ResizeMode resizeMode = ResizeMode.ReferenceScale;

        [Header("Percent Of Parent")]
        [SerializeField] private Vector2 sizePercent = new Vector2(0.5f, 0.5f);
        [SerializeField] private Vector2 pixelOffset = Vector2.zero;

        [Header("Reference Scale")]
        [Tooltip("Parent size used by your original fixed layout (design resolution).")]
        [SerializeField] private Vector2 referenceParentSize = new Vector2(1920f, 1080f);

        [Tooltip("Target fixed size from your original layout.")]
        [SerializeField] private Vector2 referenceTargetSize = new Vector2(600f, 400f);

        [SerializeField] private bool uniformScale = true;
        [SerializeField] private UniformScaleSource uniformScaleSource = UniformScaleSource.Min;
        [SerializeField] private bool onlyShrinkInReferenceScale = true;
        [SerializeField] private bool clampToParentPercent;
        [SerializeField] private Vector2 maxParentPercent = new Vector2(0.5f, 0.5f);
        [SerializeField] private bool preserveAspectWhenClamped = true;
        [SerializeField] private bool autoCaptureReferenceSizes = true;
        [SerializeField] private bool recaptureReferenceOnPlayStart = true;
        [SerializeField] private bool autoSetNativeSizeOnPlayStart = true;
        [SerializeField, Min(0)] private int nativeSizeDelayFrames = 2;

        [SerializeField, HideInInspector] private int capturedParentId;
        [SerializeField, HideInInspector] private int capturedTargetId;
        [SerializeField, HideInInspector] private bool nativeSizeAppliedThisPlay;

        [Header("Appearance")]
        [SerializeField] private bool smoothAppearOnPlayStart = true;
        [SerializeField] private bool hideUntilFirstResize = true;
        [SerializeField, Min(0f)] private float appearDuration = 0.12f;
        [SerializeField] private bool useUnscaledTimeForAppear = true;
        [SerializeField] private bool autoAddCanvasGroupOnPlay = true;
        [SerializeField] private CanvasGroup appearCanvasGroup;

        private int pendingNativeSizeFrames;
        private bool isApplying;
        private bool hasAppliedSizeThisEnable;
        private bool appearanceInitDone;
        private bool isFadingIn;
        private float fade01;
    #if UNITY_EDITOR
        private bool editorApplyQueued;
    #endif

        [Header("Clamp")]
        [SerializeField] private Vector2 minSize = new Vector2(1f, 1f);
        [SerializeField] private Vector2 maxSize = new Vector2(10000f, 10000f);

        private RectTransform Parent => parentRect != null ? parentRect : transform.parent as RectTransform;
        private RectTransform Target => targetRect != null ? targetRect : transform as RectTransform;

        private void Reset()
        {
            EnsureDefaultReferences();
            CaptureCurrentSizesAsReference();
        }

        private void OnEnable()
        {
            EnsureDefaultReferences();
            hasAppliedSizeThisEnable = false;
            appearanceInitDone = false;
            isFadingIn = false;
            fade01 = 0f;

            if (Application.isPlaying)
            {
                nativeSizeAppliedThisPlay = !autoSetNativeSizeOnPlayStart;
                pendingNativeSizeFrames = Mathf.Max(0, nativeSizeDelayFrames);

                PrepareAppearanceForPlayStart();

                if (recaptureReferenceOnPlayStart)
                    CaptureReferenceSizesNow();
            }
            else
            {
                TryAutoCaptureReferenceSizes();
            }

            ApplyNow();
            TryBeginAppearance();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;

            if (autoSetNativeSizeOnPlayStart && !nativeSizeAppliedThisPlay)
            {
                if (pendingNativeSizeFrames > 0)
                {
                    pendingNativeSizeFrames--;
                }
                else
                {
                    var target = Target;
                    if (target != null)
                    {
                        TrySetNativeSize(target);
                        CaptureReferenceSizesNow();
                        ApplyNow();
                    }

                    nativeSizeAppliedThisPlay = true;
                    TryBeginAppearance();
                }
            }

            if (!isFadingIn)
                return;

            var group = ResolveAppearGroup(createIfMissing: false);
            if (group == null)
            {
                isFadingIn = false;
                appearanceInitDone = true;
                return;
            }

            float dt = useUnscaledTimeForAppear ? Time.unscaledDeltaTime : Time.deltaTime;
            if (appearDuration <= 0f)
            {
                fade01 = 1f;
            }
            else
            {
                fade01 = Mathf.Clamp01(fade01 + (dt / appearDuration));
            }

            group.alpha = fade01;
            if (fade01 >= 1f)
            {
                group.interactable = true;
                group.blocksRaycasts = true;
                isFadingIn = false;
                appearanceInitDone = true;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isApplying)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorApply();
                return;
            }
#endif

            ApplyNow();

            if (Application.isPlaying)
                TryBeginAppearance();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureDefaultReferences();
            TryAutoCaptureReferenceSizes();
            QueueEditorApply();
        }

        private void QueueEditorApply()
        {
            if (editorApplyQueued)
                return;

            editorApplyQueued = true;
            EditorApplication.delayCall += ApplyNowDelayed;
        }

        private void ApplyNowDelayed()
        {
            editorApplyQueued = false;
            if (this == null)
                return;

            ApplyNow();
        }
#endif

        [ContextMenu("Capture Reference Sizes Now")]
        public void CaptureReferenceSizesNow()
        {
            var parent = Parent;
            var target = Target;
            if (parent == null || target == null)
                return;

            var parentSize = parent.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
                return;

            var targetSize = target.rect.size;
            if (targetSize.x <= 0f || targetSize.y <= 0f)
                return;

            referenceParentSize = parentSize;
            referenceTargetSize = targetSize;
            capturedParentId = parent.GetInstanceID();
            capturedTargetId = target.GetInstanceID();
        }

        [ContextMenu("Set Native Size And Capture Reference")]
        public void SetNativeSizeAndCaptureReference()
        {
            var target = Target;
            if (target == null)
                return;

            TrySetNativeSize(target);
            CaptureReferenceSizesNow();
        }

        [ContextMenu("Apply Now")]
        public void ApplyNow()
        {
            if (isApplying)
                return;

            var parent = Parent;
            var target = Target;
            if (parent == null || target == null)
                return;

            var parentSize = parent.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
                return;

            Vector2 resultSize;
            switch (resizeMode)
            {
                case ResizeMode.PercentOfParent:
                    resultSize = new Vector2(
                        parentSize.x * Mathf.Max(0f, sizePercent.x) + pixelOffset.x,
                        parentSize.y * Mathf.Max(0f, sizePercent.y) + pixelOffset.y);
                    break;

                default:
                    var refParentX = Mathf.Max(1f, referenceParentSize.x);
                    var refParentY = Mathf.Max(1f, referenceParentSize.y);

                    float sx = parentSize.x / refParentX;
                    float sy = parentSize.y / refParentY;

                    if (uniformScale)
                    {
                        float s;
                        switch (uniformScaleSource)
                        {
                            case UniformScaleSource.Width:
                                s = sx;
                                break;
                            case UniformScaleSource.Height:
                                s = sy;
                                break;
                            case UniformScaleSource.Max:
                                s = Mathf.Max(sx, sy);
                                break;
                            default:
                                s = Mathf.Min(sx, sy);
                                break;
                        }

                        resultSize = referenceTargetSize * s;
                    }
                    else
                    {
                        resultSize = new Vector2(referenceTargetSize.x * sx, referenceTargetSize.y * sy);
                    }

                    if (onlyShrinkInReferenceScale)
                    {
                        resultSize.x = Mathf.Min(resultSize.x, referenceTargetSize.x);
                        resultSize.y = Mathf.Min(resultSize.y, referenceTargetSize.y);
                    }
                    break;
            }

            if (clampToParentPercent)
            {
                Vector2 maxAllowed = new Vector2(
                    parentSize.x * Mathf.Max(0f, maxParentPercent.x),
                    parentSize.y * Mathf.Max(0f, maxParentPercent.y));

                if (preserveAspectWhenClamped)
                {
                    float kx = maxAllowed.x > 0f ? maxAllowed.x / Mathf.Max(0.0001f, resultSize.x) : 1f;
                    float ky = maxAllowed.y > 0f ? maxAllowed.y / Mathf.Max(0.0001f, resultSize.y) : 1f;
                    float k = Mathf.Min(1f, kx, ky);
                    resultSize *= k;
                }
                else
                {
                    resultSize.x = Mathf.Min(resultSize.x, maxAllowed.x);
                    resultSize.y = Mathf.Min(resultSize.y, maxAllowed.y);
                }
            }

            resultSize.x = Mathf.Clamp(resultSize.x, Mathf.Max(0f, minSize.x), Mathf.Max(minSize.x, maxSize.x));
            resultSize.y = Mathf.Clamp(resultSize.y, Mathf.Max(0f, minSize.y), Mathf.Max(minSize.y, maxSize.y));

            isApplying = true;
            try
            {
                target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, resultSize.x);
                target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, resultSize.y);
                hasAppliedSizeThisEnable = true;
            }
            finally
            {
                isApplying = false;
            }
        }

        private void PrepareAppearanceForPlayStart()
        {
            if (!smoothAppearOnPlayStart || !hideUntilFirstResize)
                return;

            if (!CanApplyCurrentSize())
                return;

            var group = ResolveAppearGroup(createIfMissing: autoAddCanvasGroupOnPlay);
            if (group == null)
                return;

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private bool CanApplyCurrentSize()
        {
            var parent = Parent;
            var target = Target;
            if (parent == null || target == null)
                return false;

            var parentSize = parent.rect.size;
            return parentSize.x > 0f && parentSize.y > 0f;
        }

        private void TryBeginAppearance()
        {
            if (!Application.isPlaying)
                return;

            if (appearanceInitDone || !hasAppliedSizeThisEnable || !nativeSizeAppliedThisPlay)
                return;

            var group = ResolveAppearGroup(createIfMissing: autoAddCanvasGroupOnPlay);
            if (!smoothAppearOnPlayStart || group == null)
            {
                if (group != null)
                {
                    group.alpha = 1f;
                    group.interactable = true;
                    group.blocksRaycasts = true;
                }

                appearanceInitDone = true;
                return;
            }

            if (!hideUntilFirstResize)
                group.alpha = 0f;

            fade01 = Mathf.Clamp01(group.alpha);
            isFadingIn = true;
        }

        private CanvasGroup ResolveAppearGroup(bool createIfMissing)
        {
            if (appearCanvasGroup != null)
                return appearCanvasGroup;

            var target = Target;
            if (target == null)
                return null;

            var group = target.GetComponent<CanvasGroup>();
            if (group == null && createIfMissing && Application.isPlaying)
                group = target.gameObject.AddComponent<CanvasGroup>();

            appearCanvasGroup = group;
            return appearCanvasGroup;
        }

        private void TryAutoCaptureReferenceSizes()
        {
            if (!autoCaptureReferenceSizes)
                return;

            if (Application.isPlaying)
                return;

            var parent = Parent;
            var target = Target;
            if (parent == null || target == null)
                return;

            int parentId = parent.GetInstanceID();
            int targetId = target.GetInstanceID();

            bool refsChanged = parentId != capturedParentId || targetId != capturedTargetId;
            bool refsInvalid = referenceParentSize.x <= 0f || referenceParentSize.y <= 0f || referenceTargetSize.x <= 0f || referenceTargetSize.y <= 0f;

            if (!refsChanged && !refsInvalid)
                return;

            CaptureReferenceSizesNow();
        }

        private void EnsureDefaultReferences()
        {
            if (targetRect == null)
                targetRect = transform as RectTransform;

            if (parentRect == null)
                parentRect = transform.parent as RectTransform;
        }

        private void CaptureCurrentSizesAsReference()
        {
            var parent = Parent;
            var target = Target;
            if (target == null)
                return;

            var targetSize = target.rect.size;
            if (targetSize.x > 0f && targetSize.y > 0f)
                referenceTargetSize = targetSize;

            if (parent != null)
            {
                var parentSize = parent.rect.size;
                if (parentSize.x > 0f && parentSize.y > 0f)
                    referenceParentSize = parentSize;

                capturedParentId = parent.GetInstanceID();
            }
            else
            {
                capturedParentId = 0;
            }

            capturedTargetId = target.GetInstanceID();
        }

        private static void TrySetNativeSize(RectTransform target)
        {
            if (target == null)
                return;

            var image = target.GetComponent<Image>();
            if (image != null)
            {
                image.SetNativeSize();
                return;
            }

            var rawImage = target.GetComponent<RawImage>();
            if (rawImage != null)
                rawImage.SetNativeSize();
        }
    }
}

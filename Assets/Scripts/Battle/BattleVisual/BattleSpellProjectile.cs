using System;
using UnityEngine;

namespace Game.Battle.Visual
{
    public sealed class BattleSpellProjectile : MonoBehaviour
    {
        [Header("Auto-Refs")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteFrameAnimator animator;

        [Header("Visual Scale (Optional)")]
        [Tooltip("If set, scaling is applied to this transform instead of the projectile root. Defaults to the SpriteRenderer transform.")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("If enabled, the projectile visual will scale from Start Scale to End Scale over the travel time.")]
        [SerializeField] private bool scaleOverTravel;
        [SerializeField] private Vector3 startScale = Vector3.one;
        [SerializeField] private Vector3 endScale = Vector3.one;

        private Vector3 startPos;
        private Vector3 endPos;
        private float travelTime;
        private float t;
        private bool initialized;
        private Action impact;
        private bool impactInvoked;

        private void Reset()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<SpriteFrameAnimator>();
            visualRoot = spriteRenderer != null ? spriteRenderer.transform : null;
        }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (animator == null)
                animator = GetComponentInChildren<SpriteFrameAnimator>();

            if (visualRoot == null)
                visualRoot = spriteRenderer != null ? spriteRenderer.transform : transform;

            if (animator != null && spriteRenderer != null)
                animator.SetTarget(spriteRenderer);
        }

        public void Initialize(
            Vector3 start,
            Vector3 end,
            float travelTimeSeconds,
            IdleAnimation projectileAnimation,
            bool flipX,
            int impactAtFrame = -1,
            Action onImpact = null)
        {
            startPos = start;
            endPos = end;
            travelTime = Mathf.Max(0.01f, travelTimeSeconds);
            t = 0f;
            initialized = true;
            impact = onImpact;
            impactInvoked = false;

            transform.position = startPos;

            if (scaleOverTravel && visualRoot != null)
                visualRoot.localScale = startScale;

            if (spriteRenderer != null)
                spriteRenderer.flipX = flipX;

            if (projectileAnimation != null && projectileAnimation.IsValid())
            {
                if (animator == null)
                    animator = gameObject.AddComponent<SpriteFrameAnimator>();

                if (spriteRenderer == null)
                {
                    spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                    if (spriteRenderer == null)
                        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }

                animator.SetTarget(spriteRenderer);
                animator.SetFramesPerSecond(projectileAnimation.FrameRate);

                int impactFrame1Based = -1;
                var frames = projectileAnimation.FramesArray;
                int frameCount = frames != null ? frames.Length : 0;

                if (frameCount > 0)
                {
                    if (impactAtFrame == -1)
                        impactFrame1Based = frameCount; // last frame
                    else if (impactAtFrame > 0)
                        impactFrame1Based = Mathf.Clamp(impactAtFrame, 1, frameCount);
                }

                void InvokeImpactOnce()
                {
                    if (impactInvoked)
                        return;
                    impactInvoked = true;
                    impact?.Invoke();
                }

                if (impactFrame1Based > 0)
                {
                    animator.PlayOnce(
                        frames,
                        finished: null,
                        impactFrameIndex: impactFrame1Based,
                        onImpact: InvokeImpactOnce);
                }
                else
                {
                    // If impact frame is not configured, fire impact at the end of the projectile animation.
                    animator.PlayOnce(frames, finished: InvokeImpactOnce);
                }
            }
            else
            {
                // No projectile animation configured -> trigger impact immediately on spawn.
                if (!impactInvoked)
                {
                    impactInvoked = true;
                    impact?.Invoke();
                }
            }
        }

        private void Update()
        {
            if (!initialized)
                return;

            t += Time.deltaTime / travelTime;
            if (t >= 1f)
            {
                transform.position = endPos;

                if (scaleOverTravel && visualRoot != null)
                    visualRoot.localScale = endScale;

                // Safety: ensure impact is not lost if the projectile is destroyed before its animation finishes.
                if (!impactInvoked)
                {
                    impactInvoked = true;
                    impact?.Invoke();
                }

                Destroy(gameObject);
                return;
            }

            transform.position = Vector3.LerpUnclamped(startPos, endPos, t);

            if (scaleOverTravel && visualRoot != null)
                visualRoot.localScale = Vector3.LerpUnclamped(startScale, endScale, t);
        }
    }
}

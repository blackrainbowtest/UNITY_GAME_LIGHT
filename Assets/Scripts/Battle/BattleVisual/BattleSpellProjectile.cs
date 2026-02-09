using UnityEngine;

namespace Game.Battle.Visual
{
    public sealed class BattleSpellProjectile : MonoBehaviour
    {
        [Header("Auto-Refs")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteFrameAnimator animator;

        private Vector3 startPos;
        private Vector3 endPos;
        private float travelTime;
        private float t;
        private bool initialized;

        private void Reset()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<SpriteFrameAnimator>();
        }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (animator == null)
                animator = GetComponentInChildren<SpriteFrameAnimator>();

            if (animator != null && spriteRenderer != null)
                animator.SetTarget(spriteRenderer);
        }

        public void Initialize(
            Vector3 start,
            Vector3 end,
            float travelTimeSeconds,
            IdleAnimation projectileAnimation,
            bool flipX)
        {
            startPos = start;
            endPos = end;
            travelTime = Mathf.Max(0.01f, travelTimeSeconds);
            t = 0f;
            initialized = true;

            transform.position = startPos;

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
                animator.PlayLoop(projectileAnimation.FramesArray);
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
                Destroy(gameObject);
                return;
            }

            transform.position = Vector3.LerpUnclamped(startPos, endPos, t);
        }
    }
}

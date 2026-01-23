using UnityEngine;

namespace Game.Battle.Visual
{
    /// <summary>
    /// Positions player/enemy transforms using camera viewport coordinates (0..1),
    /// similar to Canvas anchors but for world-space SpriteRenderers.
    /// </summary>
    public sealed class BattleStageAnchors : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform enemy;

        [Header("Camera")]
        [SerializeField] private Camera targetCamera;

        [Header("Viewport Anchors (0..1)")]
        [Range(0f, 1f)]
        [SerializeField] private float playerX = 0.18f;
        [Range(0f, 1f)]
        [SerializeField] private float enemyX = 0.82f;
        [Range(0f, 1f)]
        [SerializeField] private float y = 0.20f;

        [Header("Offsets (world units)")]
        [SerializeField] private Vector3 playerOffset;
        [SerializeField] private Vector3 enemyOffset;

        [Header("Runtime")]
        [SerializeField] private bool updateEveryFrame;

        private void Reset()
        {
            targetCamera = Camera.main;
        }

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void Start()
        {
            Apply();
        }

        private void LateUpdate()
        {
            if (updateEveryFrame)
                Apply();
        }

        [ContextMenu("Apply")]
        public void Apply()
        {
            if (targetCamera == null)
                return;

            ApplyOne(player, playerX, y, playerOffset);
            ApplyOne(enemy, enemyX, y, enemyOffset);
        }

        private void ApplyOne(Transform target, float vx, float vy, Vector3 offset)
        {
            if (target == null)
                return;

            // For ViewportToWorldPoint we must supply distance from camera.
            // Keep current depth relative to camera.
            var camTransform = targetCamera.transform;
            var toTarget = target.position - camTransform.position;
            float depth = Vector3.Dot(toTarget, camTransform.forward);

            // Ensure a sane depth (in case object is exactly on camera plane).
            if (depth < 0.01f)
                depth = 10f;

            var world = targetCamera.ViewportToWorldPoint(new Vector3(vx, vy, depth));
            world += offset;

            // Preserve Z if you want strict 2D sorting; otherwise keep computed.
            world.z = target.position.z;
            target.position = world;
        }
    }
}

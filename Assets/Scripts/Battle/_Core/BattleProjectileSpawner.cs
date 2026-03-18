using UnityEngine;
using Game.Battle.Visual;

namespace Game.Battle
{
    public sealed class BattleProjectileSpawner
    {
        private readonly Transform projectilesRoot;

        public BattleProjectileSpawner(Transform projectilesRoot)
        {
            this.projectilesRoot = projectilesRoot;
        }

        public bool TrySpawnProjectile(
            OutfitVisuals.SpellProjectileConfig projectileConfig,
            Transform attackerTransform,
            bool actorIsPlayer,
            System.Action onImpact)
        {
            if (attackerTransform == null)
                return false;

            if (!projectileConfig.IsEnabled || projectileConfig.projectilePrefab == null)
                return false;

            int dir = actorIsPlayer ? 1 : -1;
            var spawnOffsetUnits = new Vector3(
                projectileConfig.ToUnits(projectileConfig.spawnOffsetPixels.x) * dir,
                projectileConfig.ToUnits(projectileConfig.spawnOffsetPixels.y),
                0f);

            var start = attackerTransform.position + spawnOffsetUnits;
            var end = start + Vector3.right * (projectileConfig.ToUnits(projectileConfig.travelDistancePixels) * dir);

            var go = Object.Instantiate(projectileConfig.projectilePrefab, start, Quaternion.identity, projectilesRoot);
            var proj = go.GetComponent<BattleSpellProjectile>();
            if (proj == null)
                proj = go.AddComponent<BattleSpellProjectile>();

            bool flipX = dir < 0;
            proj.Initialize(
                start,
                end,
                projectileConfig.travelTimeSeconds,
                projectileConfig.projectileAnimation,
                flipX,
                impactAtFrame: projectileConfig.impactAtFrame,
                onImpact: onImpact);

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Battle.Statuses;

namespace Game.Battle.UI
{
    public sealed class BattleStatusPanelView : MonoBehaviour
    {
        [Serializable]
        private struct StatusIconMapping
        {
            public StatusEffectId id;
            public Sprite icon;
        }

        [Header("Roots")]
        [SerializeField] private Transform playerGridRoot;
        [SerializeField] private Transform enemyGridRoot;

        [Header("Prefab")]
        [SerializeField] private StatusIconView iconPrefab;

        [Header("Config")]
        [SerializeField] private int maxIconsPerSide = 20;
        [SerializeField] private BattleStatusCatalog statusCatalog;
        [Tooltip("Legacy fallback icon mapping. Prefer statusCatalog for centralized setup.")]
        [SerializeField] private StatusIconMapping[] icons;

        private readonly List<StatusIconView> playerPool = new List<StatusIconView>(20);
        private readonly List<StatusIconView> enemyPool = new List<StatusIconView>(20);

        public BattleStatusCatalog StatusCatalog => statusCatalog;

        public void Render(IReadOnlyList<StatusInstance> player, IReadOnlyList<StatusInstance> enemy)
        {
            RenderSide(playerGridRoot, playerPool, player);
            RenderSide(enemyGridRoot, enemyPool, enemy);
        }

        private void RenderSide(Transform root, List<StatusIconView> pool, IReadOnlyList<StatusInstance> data)
        {
            if (root == null)
                return;

            if (iconPrefab == null)
            {
                Debug.LogError("BattleStatusPanelView: iconPrefab is not set");
                return;
            }

            var count = data == null ? 0 : Mathf.Min(maxIconsPerSide, data.Count);

            EnsurePool(root, pool, count);

            for (var i = 0; i < pool.Count; i++)
            {
                var view = pool[i];
                var active = i < count;
                if (view != null)
                    view.gameObject.SetActive(active);

                if (!active || view == null)
                    continue;

                var s = data[i];
                view.Set(GetIcon(s.Id), s.TurnsLeft);
            }
        }

        private void EnsurePool(Transform root, List<StatusIconView> pool, int needed)
        {
            while (pool.Count < needed)
            {
                var view = Instantiate(iconPrefab, root);
                pool.Add(view);
            }
        }

        private Sprite GetIcon(StatusEffectId id)
        {
            if (statusCatalog != null && statusCatalog.TryGet(id, out var def) && def.icon != null)
                return def.icon;

            if (icons == null)
                return null;

            for (var i = 0; i < icons.Length; i++)
            {
                if (icons[i].id == id)
                    return icons[i].icon;
            }

            return null;
        }
    }
}

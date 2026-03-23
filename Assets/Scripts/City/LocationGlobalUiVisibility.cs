using System.Collections.Generic;
using UnityEngine;

namespace UDA2.City
{
    internal static class LocationGlobalUiVisibility
    {
        private static readonly List<object> Requesters = new List<object>(8);
        private static readonly Dictionary<GameObject, bool> OriginalStates = new Dictionary<GameObject, bool>(8);
        private static RestoreRunner restoreRunner;

        public static void RequestHide(object owner)
        {
            if (owner == null)
                return;

            if (Requesters.Contains(owner))
                return;

            if (Requesters.Count == 0)
            {
                OriginalStates.Clear();
                var roots = FindGlobalUiRoots();
                for (var i = 0; i < roots.Count; i++)
                {
                    var root = roots[i];
                    if (root == null)
                        continue;

                    if (!OriginalStates.ContainsKey(root))
                        OriginalStates[root] = root.activeSelf;

                    root.SetActive(false);
                }
            }

            Requesters.Add(owner);
        }

        public static void ReleaseHide(object owner)
        {
            if (owner == null)
                return;

            if (!Requesters.Remove(owner))
                return;

            if (Requesters.Count > 0)
                return;

            var restorePairs = new List<KeyValuePair<GameObject, bool>>(OriginalStates.Count);
            foreach (var pair in OriginalStates)
                restorePairs.Add(pair);

            OriginalStates.Clear();
            RestoreSafely(restorePairs);
        }

        private static void RestoreSafely(List<KeyValuePair<GameObject, bool>> restorePairs)
        {
            if (restorePairs == null || restorePairs.Count == 0)
                return;

            if (!Application.isPlaying)
            {
                ApplyRestore(restorePairs);
                return;
            }

            var runner = GetRunner();
            if (runner == null)
            {
                ApplyRestore(restorePairs);
                return;
            }

            runner.RunRestoreNextFrame(restorePairs);
        }

        private static void ApplyRestore(List<KeyValuePair<GameObject, bool>> restorePairs)
        {
            for (var i = 0; i < restorePairs.Count; i++)
            {
                var pair = restorePairs[i];
                var root = pair.Key;
                if (root == null)
                    continue;

                if (root.activeSelf == pair.Value)
                    continue;

                root.SetActive(pair.Value);
            }
        }

        private static RestoreRunner GetRunner()
        {
            if (restoreRunner != null)
                return restoreRunner;

            var go = new GameObject("LocationGlobalUiVisibility_RestoreRunner");
            go.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(go);
            restoreRunner = go.AddComponent<RestoreRunner>();
            return restoreRunner;
        }

        private sealed class RestoreRunner : MonoBehaviour
        {
            private Coroutine restoreRoutine;
            private List<KeyValuePair<GameObject, bool>> pendingRestore;

            public void RunRestoreNextFrame(List<KeyValuePair<GameObject, bool>> restorePairs)
            {
                pendingRestore = restorePairs;

                if (restoreRoutine != null)
                    StopCoroutine(restoreRoutine);

                restoreRoutine = StartCoroutine(RestoreAtEndOfFrame());
            }

            private System.Collections.IEnumerator RestoreAtEndOfFrame()
            {
                yield return null;

                var toRestore = pendingRestore;
                pendingRestore = null;
                restoreRoutine = null;

                if (toRestore == null || toRestore.Count == 0)
                    yield break;

                ApplyRestore(toRestore);
            }
        }

        private static List<GameObject> FindGlobalUiRoots()
        {
            var roots = new List<GameObject>(4);
            MonoBehaviour[] all;
#if UNITY_2023_1_OR_NEWER || UNITY_2022_2_OR_NEWER
            all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            all = Object.FindObjectsOfType<MonoBehaviour>(true);
#pragma warning restore CS0618
#endif

            if (all == null || all.Length == 0)
                return roots;

            for (var i = 0; i < all.Length; i++)
            {
                var component = all[i];
                if (component == null)
                    continue;

                var type = component.GetType();
                if (type == null)
                    continue;

                if (!string.Equals(type.FullName, "UDA2.UI.Game.GlobalUISceneBinder", System.StringComparison.Ordinal))
                    continue;

                var go = component.gameObject;
                var root = go != null && go.transform != null && go.transform.root != null
                    ? go.transform.root.gameObject
                    : go;

                if (root == null)
                    continue;

                var exists = false;
                for (var j = 0; j < roots.Count; j++)
                {
                    if (roots[j] == root)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    roots.Add(root);
            }

            return roots;
        }
    }
}

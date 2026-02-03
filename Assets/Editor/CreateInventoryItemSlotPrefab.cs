#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UDA2.UI.EditorTools
{
    public static class CreateInventoryItemSlotPrefab
    {
        private const string DefaultPrefabPath = "Assets/Resources/Prefabs/UI/Profile/Inventory/InventoryItemSlotView.prefab";

        [MenuItem("Tools/UI/Create Inventory Item Slot Prefab")]
        public static void Create()
        {
            EnsureFolders(DefaultPrefabPath);

            var root = new GameObject("InventoryItemSlotView", typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(96, 96);
            rt.localScale = Vector3.one;

            // Background (optional)
            var bg = root.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.08f);
            bg.raycastTarget = true;

            // Filled state root
            var filled = new GameObject("Filled", typeof(RectTransform));
            filled.transform.SetParent(root.transform, false);
            var filledRt = filled.GetComponent<RectTransform>();
            filledRt.anchorMin = Vector2.zero;
            filledRt.anchorMax = Vector2.one;
            filledRt.offsetMin = Vector2.zero;
            filledRt.offsetMax = Vector2.zero;

            // Empty state root
            var empty = new GameObject("Empty", typeof(RectTransform));
            empty.transform.SetParent(root.transform, false);
            var emptyRt = empty.GetComponent<RectTransform>();
            emptyRt.anchorMin = Vector2.zero;
            emptyRt.anchorMax = Vector2.one;
            emptyRt.offsetMin = Vector2.zero;
            emptyRt.offsetMax = Vector2.zero;

            // Icon
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(filled.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // Count text
            var countGo = new GameObject("Count", typeof(RectTransform));
            countGo.transform.SetParent(filled.transform, false);
            var countRt = countGo.GetComponent<RectTransform>();
            countRt.anchorMin = new Vector2(0f, 0f);
            countRt.anchorMax = new Vector2(1f, 0f);
            countRt.pivot = new Vector2(0.5f, 0f);
            countRt.anchoredPosition = new Vector2(0, 6);
            countRt.sizeDelta = new Vector2(0, 28);

            var countText = countGo.AddComponent<TextMeshProUGUI>();
            countText.text = "";
            countText.fontSize = 18;
            countText.alignment = TextAlignmentOptions.BottomRight;
            countText.raycastTarget = false;
            countText.color = Color.white;

            // Hook script
            var view = root.AddComponent<UDA2.UI.Game.InventoryItemSlotView>();
            SetPrivateSerializedField(view, "iconImage", icon);
            SetPrivateSerializedField(view, "countText", countText);
            SetPrivateSerializedField(view, "emptyStateRoot", empty);
            SetPrivateSerializedField(view, "filledStateRoot", filled);

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(root, DefaultPrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created prefab at: {DefaultPrefabPath}");
        }

        private static void EnsureFolders(string assetPath)
        {
            assetPath = assetPath.Replace('\\', '/');
            var folder = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder))
                return;

            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parts = folder.Split('/');
            string current = parts[0]; // Assets
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void SetPrivateSerializedField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"Property '{fieldName}' not found on {target.GetType().Name}");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif

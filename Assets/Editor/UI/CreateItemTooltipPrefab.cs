#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.Editor.UI
{
    public static class CreateItemTooltipPrefab
    {
        private const string PrefabPath = "Assets/Resources/Prefabs/UI/Common/ItemTooltipModal.prefab";

        [MenuItem("Tools/UDA2/UI/Create Item Tooltip Prefab")]
        public static void CreateOrReplace()
        {
            var dir = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            // Build a temporary hierarchy.
            var root = new GameObject("ItemTooltipModal",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(UDA2.UI.Game.ItemTooltipModalController));

            try
            {
                ConfigureRectStretch(root.GetComponent<RectTransform>());

                var backdropImg = root.GetComponent<Image>();
                backdropImg.color = new Color(0f, 0f, 0f, 0.45f);
                backdropImg.raycastTarget = true;

                var backdropBtn = root.GetComponent<Button>();
                backdropBtn.transition = Selectable.Transition.None;

                var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                var panelRt = panel.GetComponent<RectTransform>();
                panelRt.sizeDelta = new Vector2(620f, 360f);
                panelRt.anchorMin = new Vector2(0.5f, 0.5f);
                panelRt.anchorMax = new Vector2(0.5f, 0.5f);
                panelRt.pivot = new Vector2(0.5f, 0.5f);
                panelRt.anchoredPosition = Vector2.zero;

                var panelImg = panel.GetComponent<Image>();
                panelImg.color = new Color(0.12f, 0.12f, 0.12f, 0.98f);
                panelImg.raycastTarget = true; // block clicks to backdrop when clicking inside.

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.transform.SetParent(panel.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 1f);
                iconRt.anchorMax = new Vector2(0f, 1f);
                iconRt.pivot = new Vector2(0f, 1f);
                iconRt.anchoredPosition = new Vector2(18f, -18f);
                iconRt.sizeDelta = new Vector2(96f, 96f);
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.raycastTarget = false;

                var title = CreateText(panel.transform, "Title", new Vector2(130f, -20f), new Vector2(470f, 40f), 28);
                title.fontStyle = FontStyles.Bold;

                var desc = CreateText(panel.transform, "Description", new Vector2(18f, -130f), new Vector2(584f, 150f), 20);

                var type = CreateText(panel.transform, "Type", new Vector2(18f, -300f), new Vector2(280f, 30f), 18);
                var rarity = CreateText(panel.transform, "Rarity", new Vector2(320f, -300f), new Vector2(282f, 30f), 18);
                var buy = CreateText(panel.transform, "Buy", new Vector2(18f, -330f), new Vector2(280f, 30f), 18);
                var sell = CreateText(panel.transform, "Sell", new Vector2(320f, -330f), new Vector2(282f, 30f), 18);

                var controller = root.GetComponent<UDA2.UI.Game.ItemTooltipModalController>();

                // Assign serialized fields safely.
                var so = new SerializedObject(controller);
                so.FindProperty("backdropButton").objectReferenceValue = backdropBtn;
                so.FindProperty("panel").objectReferenceValue = panelRt;
                so.FindProperty("iconImage").objectReferenceValue = iconImg;
                so.FindProperty("titleText").objectReferenceValue = title;
                so.FindProperty("descriptionText").objectReferenceValue = desc;
                so.FindProperty("typeText").objectReferenceValue = type;
                so.FindProperty("rarityText").objectReferenceValue = rarity;
                so.FindProperty("buyPriceText").objectReferenceValue = buy;
                so.FindProperty("sellPriceText").objectReferenceValue = sell;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Save / replace.
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null)
                    throw new System.Exception("PrefabUtility.SaveAsPrefabAsset returned null.");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[CreateItemTooltipPrefab] Created prefab: {PrefabPath}");
                Selection.activeObject = prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureRectStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchoredPos, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.color = Color.white;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            t.text = string.Empty;

            return t;
        }
    }
}
#endif

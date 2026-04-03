// ============================================================================
// FormationUIPrefabsGenerator.cs — Автоматическое создание UI префабов
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 14:30:00 UTC
// ============================================================================
//
// Меню: Tools → Formation UI → Generate UI Prefabs
// Создаёт префабы для FormationUI:
// - FormationListItem
// - ActiveFormationItem
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace CultivationGame.Editor
{
    /// <summary>
    /// Генератор UI префабов для системы формаций.
    /// </summary>
    public static class FormationUIPrefabsGenerator
    {
        private const string PREFABS_PATH = "Assets/Prefabs/UI/Formation";

        #region Menu Items

        [MenuItem("Tools/Formation UI/Generate All UI Prefabs", false, 1)]
        public static void GenerateAllPrefabs()
        {
            EnsureDirectory(PREFABS_PATH);

            int count = 0;
            count += CreateFormationListItemPrefab() ? 1 : 0;
            count += CreateActiveFormationItemPrefab() ? 1 : 0;
            count += CreatePlacementPreviewPrefab() ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[FormationUIPrefabs] Created {count} prefabs in {PREFABS_PATH}");
        }

        [MenuItem("Tools/Formation UI/Generate Formation List Item", false, 2)]
        public static void GenerateFormationListItem()
        {
            EnsureDirectory(PREFABS_PATH);
            if (CreateFormationListItemPrefab())
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Tools/Formation UI/Generate Active Formation Item", false, 3)]
        public static void GenerateActiveFormationItem()
        {
            EnsureDirectory(PREFABS_PATH);
            if (CreateActiveFormationItemPrefab())
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Tools/Formation UI/Generate Placement Preview", false, 4)]
        public static void GeneratePlacementPreview()
        {
            EnsureDirectory(PREFABS_PATH);
            if (CreatePlacementPreviewPrefab())
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        #endregion

        #region Prefab Creation

        /// <summary>
        /// Создать префаб элемента списка формаций.
        /// 
        /// ┌─────────────────────────────────────────┐
        /// │ [Icon] Название          L{level}       │
        /// │         Тип формации                    │
        /// └─────────────────────────────────────────┘
        /// </summary>
        private static bool CreateFormationListItemPrefab()
        {
            string path = System.IO.Path.Combine(PREFABS_PATH, "FormationListItem.prefab");

            // Проверяем существование
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log($"[FormationUIPrefabs] FormationListItem already exists: {path}");
                return false;
            }

            // Создаём корневой объект
            var root = new GameObject("FormationListItem");

            try
            {
                // Добавляем RectTransform
                var rectTransform = root.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(250, 60);

                // Добавляем Image (фон)
                var backgroundImage = root.AddComponent<Image>();
                backgroundImage.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

                // Добавляем Button
                var button = root.AddComponent<Button>();
                var colors = button.colors;
                colors.normalColor = new Color(0.25f, 0.25f, 0.35f);
                colors.highlightedColor = new Color(0.3f, 0.3f, 0.45f);
                colors.pressedColor = new Color(0.2f, 0.2f, 0.3f);
                button.colors = colors;

                // Создаём иконку
                var iconObj = CreateChildObject(root, "Icon");
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0, 0.5f);
                iconRect.anchorMax = new Vector2(0, 0.5f);
                iconRect.pivot = new Vector2(0, 0.5f);
                iconRect.anchoredPosition = new Vector2(5, 0);
                iconRect.sizeDelta = new Vector2(50, 50);
                var iconImage = iconObj.AddComponent<Image>();
                iconImage.color = Color.white;

                // Создаём название
                var nameObj = CreateChildObject(root, "NameText");
                var nameRect = nameObj.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0.5f);
                nameRect.anchorMax = new Vector2(1, 1);
                nameRect.pivot = new Vector2(0, 1);
                nameRect.offsetMin = new Vector2(60, 0);
                nameRect.offsetMax = new Vector2(-50, -5);
                var nameText = nameObj.AddComponent<TextMeshProUGUI>();
                nameText.text = "Название формации";
                nameText.fontSize = 14;
                nameText.fontStyle = FontStyles.Bold;
                nameText.alignment = TextAlignmentOptions.Left;
                nameText.color = Color.white;

                // Создаём уровень
                var levelObj = CreateChildObject(root, "LevelText");
                var levelRect = levelObj.GetComponent<RectTransform>();
                levelRect.anchorMin = new Vector2(1, 0.5f);
                levelRect.anchorMax = new Vector2(1, 0.5f);
                levelRect.pivot = new Vector2(1, 0.5f);
                levelRect.anchoredPosition = new Vector2(-5, 0);
                levelRect.sizeDelta = new Vector2(40, 20);
                var levelText = levelObj.AddComponent<TextMeshProUGUI>();
                levelText.text = "L1";
                levelText.fontSize = 12;
                levelText.alignment = TextAlignmentOptions.Right;
                levelText.color = new Color(0.7f, 0.9f, 1f);

                // Создаём тип формации
                var typeObj = CreateChildObject(root, "TypeText");
                var typeRect = typeObj.GetComponent<RectTransform>();
                typeRect.anchorMin = new Vector2(0, 0);
                typeRect.anchorMax = new Vector2(1, 0.5f);
                typeRect.pivot = new Vector2(0, 0);
                typeRect.offsetMin = new Vector2(60, 5);
                typeRect.offsetMax = new Vector2(-50, 0);
                var typeText = typeObj.AddComponent<TextMeshProUGUI>();
                typeText.text = "Тип формации";
                typeText.fontSize = 11;
                typeText.alignment = TextAlignmentOptions.Left;
                typeText.color = new Color(0.7f, 0.7f, 0.7f);

                // Сохраняем как префаб
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);

                Debug.Log($"[FormationUIPrefabs] Created FormationListItem: {path}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FormationUIPrefabs] Error creating FormationListItem: {e.Message}");
                Object.DestroyImmediate(root);
                return false;
            }
        }

        /// <summary>
        /// Создать префаб элемента активной формации.
        /// 
        /// ┌─────────────────────────────────────────┐
        /// │ [Icon] Название          Статус         │
        /// │ ████████░░░░░░░░  Ци: 1000/5000        │
        /// └─────────────────────────────────────────┘
        /// </summary>
        private static bool CreateActiveFormationItemPrefab()
        {
            string path = System.IO.Path.Combine(PREFABS_PATH, "ActiveFormationItem.prefab");

            // Проверяем существование
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log($"[FormationUIPrefabs] ActiveFormationItem already exists: {path}");
                return false;
            }

            // Создаём корневой объект
            var root = new GameObject("ActiveFormationItem");

            try
            {
                // Добавляем RectTransform
                var rectTransform = root.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(300, 70);

                // Добавляем Image (фон)
                var backgroundImage = root.AddComponent<Image>();
                backgroundImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

                // Добавляем Button
                var button = root.AddComponent<Button>();
                var colors = button.colors;
                colors.normalColor = new Color(0.2f, 0.2f, 0.25f);
                colors.highlightedColor = new Color(0.25f, 0.25f, 0.35f);
                colors.pressedColor = new Color(0.15f, 0.15f, 0.2f);
                button.colors = colors;

                // Создаём иконку
                var iconObj = CreateChildObject(root, "Icon");
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0, 0.5f);
                iconRect.anchorMax = new Vector2(0, 0.5f);
                iconRect.pivot = new Vector2(0, 0.5f);
                iconRect.anchoredPosition = new Vector2(5, 0);
                iconRect.sizeDelta = new Vector2(50, 50);
                var iconImage = iconObj.AddComponent<Image>();
                iconImage.color = Color.cyan;

                // Создаём название
                var nameObj = CreateChildObject(root, "NameText");
                var nameRect = nameObj.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0.6f);
                nameRect.anchorMax = new Vector2(0.7f, 1);
                nameRect.pivot = new Vector2(0, 1);
                nameRect.offsetMin = new Vector2(60, 0);
                nameRect.offsetMax = new Vector2(-5, -3);
                var nameText = nameObj.AddComponent<TextMeshProUGUI>();
                nameText.text = "Активная формация";
                nameText.fontSize = 13;
                nameText.fontStyle = FontStyles.Bold;
                nameText.alignment = TextAlignmentOptions.Left;
                nameText.color = Color.white;

                // Создаём статус
                var statusObj = CreateChildObject(root, "StatusText");
                var statusRect = statusObj.GetComponent<RectTransform>();
                statusRect.anchorMin = new Vector2(0.7f, 0.6f);
                statusRect.anchorMax = new Vector2(1, 1);
                statusRect.pivot = new Vector2(1, 1);
                statusRect.offsetMin = new Vector2(0, 0);
                statusRect.offsetMax = new Vector2(-5, -3);
                var statusText = statusObj.AddComponent<TextMeshProUGUI>();
                statusText.text = "Активна";
                statusText.fontSize = 12;
                statusText.alignment = TextAlignmentOptions.Right;
                statusText.color = new Color(0.5f, 1f, 0.5f);

                // Создаём слайдер Ци
                var sliderObj = CreateChildObject(root, "QiSlider");
                var sliderRect = sliderObj.GetComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(0, 0.3f);
                sliderRect.anchorMax = new Vector2(1, 0.55f);
                sliderRect.offsetMin = new Vector2(60, 0);
                sliderRect.offsetMax = new Vector2(-5, 0);
                var slider = sliderObj.AddComponent<Slider>();
                slider.minValue = 0;
                slider.maxValue = 1;
                slider.value = 0.6f;

                // Background слайдера
                var bgObj = CreateChildObject(sliderObj, "Background");
                var bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                var bgImage = bgObj.AddComponent<Image>();
                bgImage.color = new Color(0.1f, 0.1f, 0.15f);
                slider.targetGraphic = bgImage;

                // Fill Area слайдера
                var fillAreaObj = CreateChildObject(sliderObj, "Fill Area");
                var fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.sizeDelta = Vector2.zero;

                // Fill слайдера
                var fillObj = CreateChildObject(fillAreaObj, "Fill");
                var fillRect = fillObj.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(0.6f, 1);
                fillRect.sizeDelta = Vector2.zero;
                var fillImage = fillObj.AddComponent<Image>();
                fillImage.color = new Color(0.2f, 0.7f, 1f);
                slider.fillRect = fillRect;

                // Handle Slide Area
                var handleAreaObj = CreateChildObject(sliderObj, "Handle Slide Area");
                var handleAreaRect = handleAreaObj.GetComponent<RectTransform>();
                handleAreaRect.anchorMin = Vector2.zero;
                handleAreaRect.anchorMax = Vector2.one;
                handleAreaRect.sizeDelta = Vector2.zero;

                // Handle
                var handleObj = CreateChildObject(handleAreaObj, "Handle");
                var handleRect = handleObj.GetComponent<RectTransform>();
                handleRect.anchorMin = new Vector2(0.6f, 0.5f);
                handleRect.anchorMax = new Vector2(0.6f, 0.5f);
                handleRect.sizeDelta = new Vector2(10, 20);
                // Без handle для простоты (interactable = false)

                slider.interactable = false;

                // Создаём текст Ци
                var qiObj = CreateChildObject(root, "QiText");
                var qiRect = qiObj.GetComponent<RectTransform>();
                qiRect.anchorMin = new Vector2(0, 0);
                qiRect.anchorMax = new Vector2(1, 0.3f);
                qiRect.pivot = new Vector2(0, 0);
                qiRect.offsetMin = new Vector2(60, 2);
                qiRect.offsetMax = new Vector2(-5, 0);
                var qiText = qiObj.AddComponent<TextMeshProUGUI>();
                qiText.text = "Ци: 1,000 / 5,000 | До истощения: 2ч 30м";
                qiText.fontSize = 10;
                qiText.alignment = TextAlignmentOptions.Left;
                qiText.color = new Color(0.6f, 0.6f, 0.6f);

                // Сохраняем как префаб
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);

                Debug.Log($"[FormationUIPrefabs] Created ActiveFormationItem: {path}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FormationUIPrefabs] Error creating ActiveFormationItem: {e.Message}");
                Object.DestroyImmediate(root);
                return false;
            }
        }

        /// <summary>
        /// Создать префаб превью размещения.
        /// </summary>
        private static bool CreatePlacementPreviewPrefab()
        {
            string path = System.IO.Path.Combine(PREFABS_PATH, "PlacementPreview.prefab");

            // Проверяем существование
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log($"[FormationUIPrefabs] PlacementPreview already exists: {path}");
                return false;
            }

            // Создаём корневой объект
            var root = new GameObject("PlacementPreview");

            try
            {
                // Добавляем SpriteRenderer
                var sr = root.AddComponent<SpriteRenderer>();

                // Создаём текстуру круга
                int size = 64;
                Texture2D tex = new Texture2D(size, size);
                Color[] colors = new Color[size * size];

                Vector2 center = new Vector2(size / 2, size / 2);
                float radius = size / 2 - 2;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist <= radius && dist >= radius - 4)
                            colors[y * size + x] = new Color(0, 1, 1, 0.6f);
                        else
                            colors[y * size + x] = Color.clear;
                    }
                }

                tex.SetPixels(colors);
                tex.Apply();

                // Создаём спрайт
                var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                sr.sprite = sprite;
                sr.sortingOrder = 1000;

                // Сохраняем как префаб
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);

                Debug.Log($"[FormationUIPrefabs] Created PlacementPreview: {path}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FormationUIPrefabs] Error creating PlacementPreview: {e.Message}");
                Object.DestroyImmediate(root);
                return false;
            }
        }

        #endregion

        #region Utility

        private static GameObject CreateChildObject(GameObject parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path);
                string folder = System.IO.Path.GetFileName(path);

                if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                {
                    EnsureDirectory(parent);
                }

                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        #endregion
    }
}
#endif

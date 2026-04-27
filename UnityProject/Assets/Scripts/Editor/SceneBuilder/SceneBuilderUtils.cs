// ============================================================================
// SceneBuilderUtils.cs — Общие утилиты для всех фаз сборки
// Cultivation World Simulator
// Версия: 2.0
// ============================================================================
// Вынесено из FullSceneBuilder.cs для использования в отдельных фазах.
// Включает: EnsureSceneOpen, LoadTagManager, SetProperty, EnsureDirectory,
// CleanMissingPrefabs, EnsureSortingLayers и др.
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using TMPro;
using System.IO;
using System.Collections.Generic;

namespace CultivationGame.Editor.SceneBuilder
{
    /// <summary>
    /// Общие утилиты для всех фаз сборки сцены.
    /// Раньше находились внутри FullSceneBuilder — теперь доступны всем фазам.
    /// </summary>
    public static class SceneBuilderUtils
    {
        // ====================================================================
        //  SCENE MANAGEMENT
        // ====================================================================

        /// <summary>
        /// Убедиться что нужная сцена открыта. Если нет — открыть.
        /// </summary>
        public static void EnsureSceneOpen()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == SceneBuilderConstants.SCENE_PATH)
            {
                CleanMissingPrefabs();
                return;
            }

            if (System.IO.File.Exists(SceneBuilderConstants.SCENE_PATH))
            {
                EditorSceneManager.OpenScene(SceneBuilderConstants.SCENE_PATH);
                CleanMissingPrefabs();
            }
            else
            {
                // Сцена не существует — создаём
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var defaultCamera = Camera.main;
                if (defaultCamera != null)
                    Object.DestroyImmediate(defaultCamera.gameObject);

                EnsureDirectory("Assets/Scenes");
                EditorSceneManager.SaveScene(scene, SceneBuilderConstants.SCENE_PATH);
            }
        }

        // ====================================================================
        //  TAG MANAGER
        // ====================================================================

        /// <summary>
        /// Безопасная загрузка TagManager.asset. Защита от IndexOutOfRangeException.
        /// </summary>
        public static SerializedObject LoadTagManager()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[SceneBuilder] TagManager.asset не найден или пуст!");
                return null;
            }
            return new SerializedObject(assets[0]);
        }

        // ====================================================================
        //  SORTING LAYERS
        // ====================================================================

        /// <summary>
        /// Создание Sorting Layers для 2D рендеринга и перестановка в правильный порядок.
        /// Без Sorting Layer "Objects" все SpriteRenderer с sortingLayerName="Objects"
        /// игнорируются в Unity 6+ → спрайты невидимы!
        ///
        /// РЕДАКТИРОВАНО 2026-04-21: uniqueID теперь назначаются ДЕТЕРМИНИРОВАННО
        /// по индексу слоя (0, 1, 2, ...). Это гарантирует одинаковый порядок
        /// Sorting Layers на разных ПК при сборке проекта.
        ///
        /// ПРИЧИНА БАГА: Старый код сохранял старые uniqueID при перестановке,
        /// но на разных ПК uniqueID могли отличаться → разный порядок слоёв.
        /// </summary>
        public static void EnsureSortingLayers()
        {
            string[] requiredSortingLayers = SceneBuilderConstants.REQUIRED_SORTING_LAYERS;

            SerializedObject tagManager = LoadTagManager();
            if (tagManager == null) return;

            var sortingLayersProp = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayersProp == null)
            {
                Debug.LogError("[SceneBuilder] FIX-SORT: m_SortingLayers не найден в TagManager!");
                return;
            }

            // ШАГ 1: Собираем существующие слои
            HashSet<string> existingLayers = new HashSet<string>();
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var nameProp = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                if (nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue))
                    existingLayers.Add(nameProp.stringValue);
            }

            // ШАГ 2: Добавляем недостающие слои (uniqueID пока не важен — переназначим в ШАГЕ 3)
            bool addedNew = false;

            foreach (string layerName in requiredSortingLayers)
            {
                if (existingLayers.Contains(layerName)) continue;

                if (layerName == "Default" && sortingLayersProp.arraySize > 0)
                {
                    var first = sortingLayersProp.GetArrayElementAtIndex(0).FindPropertyRelative("name");
                    if (first != null && first.stringValue == "Default") continue;
                }

                int insertIndex = sortingLayersProp.arraySize;
                sortingLayersProp.InsertArrayElementAtIndex(insertIndex);

                var newElement = sortingLayersProp.GetArrayElementAtIndex(insertIndex);
                var nameProp = newElement.FindPropertyRelative("name");
                if (nameProp != null) nameProp.stringValue = layerName;
                var lockedProp = newElement.FindPropertyRelative("locked");
                if (lockedProp != null) lockedProp.boolValue = false;

                addedNew = true;
                Debug.Log($"[SceneBuilder] Sorting Layer \"{layerName}\" создан");
            }

            if (addedNew)
            {
                tagManager.ApplyModifiedProperties();
                tagManager.Update();
            }

            // ШАГ 3: Собираем имена слоёв, переставляем в правильный порядок
            var layerNames = new List<string>();
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var nProp = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                if (nProp != null && !string.IsNullOrEmpty(nProp.stringValue))
                    layerNames.Add(nProp.stringValue);
            }

            // Строим правильный порядок: сначала required, потом любые дополнительные
            var newOrder = new List<string>();
            var usedNames = new HashSet<string>();

            foreach (var expectedName in requiredSortingLayers)
            {
                if (layerNames.Contains(expectedName))
                {
                    newOrder.Add(expectedName);
                    usedNames.Add(expectedName);
                }
            }

            foreach (var name in layerNames)
            {
                if (!usedNames.Contains(name))
                    newOrder.Add(name);
            }

            // Проверяем, нужен ли реордер
            bool needsReorder = false;
            for (int i = 0; i < layerNames.Count && i < newOrder.Count; i++)
            {
                if (layerNames[i] != newOrder[i]) { needsReorder = true; break; }
            }

            // Проверяем, нужно ли переназначить uniqueID (ДЕТЕРМИНИРОВАННО)
            bool needsIdReassign = false;
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var idProp = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID");
                if (idProp != null && idProp.intValue != i)
                {
                    needsIdReassign = true;
                    break;
                }
            }

            if (!needsReorder && !needsIdReassign && !addedNew)
            {
                Debug.Log("[SceneBuilder] Sorting Layers уже в правильном порядке с корректными ID");
                return;
            }

            // ШАГ 4: Перестраиваем массив с ДЕТЕРМИНИРОВАННЫМИ uniqueID
            // uniqueID = индекс слоя (0, 1, 2, ...) — гарантирует одинаковый результат на любом ПК
            sortingLayersProp.ClearArray();
            for (int i = 0; i < newOrder.Count; i++)
            {
                sortingLayersProp.InsertArrayElementAtIndex(i);
                var elem = sortingLayersProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("name").stringValue = newOrder[i];
                elem.FindPropertyRelative("uniqueID").intValue = i;  // ДЕТЕРМИНИРОВАННЫЙ ID
                elem.FindPropertyRelative("locked").boolValue = false;
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log("[SceneBuilder] Sorting Layers итоговое состояние:");
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var elem = sortingLayersProp.GetArrayElementAtIndex(i);
                var n = elem.FindPropertyRelative("name");
                var id = elem.FindPropertyRelative("uniqueID");
                Debug.Log($"  [{i}] \"{n?.stringValue ?? "?"}\" (id={id?.intValue ?? -1})");
            }

            if (needsReorder)
                Debug.Log("[SceneBuilder] ✅ Порядок Sorting Layers ИСПРАВЛЕН! Terrain < Objects < Player");
            if (needsIdReassign)
                Debug.Log("[SceneBuilder] ✅ uniqueID переназначены детерминированно (0, 1, 2, ...)");
            if (!needsReorder && !needsIdReassign)
                Debug.Log("[SceneBuilder] ✅ Sorting Layers созданы в правильном порядке");
        }

        // ====================================================================
        //  PROPERTY HELPERS
        // ====================================================================

        /// <summary>
        /// Безопасная установка свойства SerializedObject.
        /// Поддерживает: int, float, bool, string, long, double, enum.
        /// </summary>
        public static void SetProperty(SerializedObject so, string propertyName, object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneBuilder] Property '{propertyName}' not found");
                return;
            }

            // Enum
            if (prop.propertyType == SerializedPropertyType.Enum && value is int enumRawValue)
            {
                var enumNames = prop.enumNames;
                if (enumRawValue >= 0 && enumRawValue < enumNames.Length)
                    prop.enumValueIndex = enumRawValue;
                else
                    prop.enumValueIndex = System.Math.Min(enumRawValue, enumNames.Length - 1);
                return;
            }

            // Float from int
            if (prop.propertyType == SerializedPropertyType.Float && value is int intAsFloat)
            {
                prop.floatValue = (float)intAsFloat;
                return;
            }

            switch (value)
            {
                case int intVal: prop.intValue = intVal; break;
                case float floatVal: prop.floatValue = floatVal; break;
                case bool boolVal: prop.boolValue = boolVal; break;
                case string strVal: prop.stringValue = strVal; break;
                case long longVal: prop.longValue = longVal; break;
                case double doubleVal: prop.doubleValue = doubleVal; break;
                default:
                    Debug.LogWarning($"[SceneBuilder] SetProperty: тип '{value.GetType().Name}' не поддерживается для '{propertyName}'");
                    break;
            }
        }

        /// <summary>
        /// Настроить компонент через callback.
        /// </summary>
        public static void SetupComponent<T>(GameObject go, System.Action<T> setup) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null) setup(component);
        }

        // ====================================================================
        //  DIRECTORY HELPERS
        // ====================================================================

        /// <summary>
        /// Рекурсивно создать папку (аналог mkdir -p).
        /// </summary>
        public static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path);
            string folder = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureDirectory(parent);
            }

            if (AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder(parent, folder);
            else
                Directory.CreateDirectory(path);
        }

        // ====================================================================
        //  MISSING PREFABS & SCRIPTS CLEANUP
        // ====================================================================

        /// <summary>
        /// Удалить объекты с Missing Prefab из активной сцены.
        /// </summary>
        public static void CleanMissingPrefabs()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            int removed = 0;
            var rootObjects = scene.GetRootGameObjects();
            var toRemove = new List<GameObject>();

            foreach (var rootObj in rootObjects)
            {
                CheckForMissingPrefab(rootObj, toRemove);
                var transforms = rootObj.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t != null && t.gameObject != null)
                        CheckForMissingPrefab(t.gameObject, toRemove);
                }
            }

            foreach (var go in toRemove)
            {
                Debug.Log($"[SceneBuilder] Removing missing/broken prefab: {go.name}");
                Undo.DestroyObjectImmediate(go);
                removed++;
            }

            if (removed > 0)
            {
                Debug.Log($"[SceneBuilder] Cleaned {removed} missing/broken prefabs");
                EditorSceneManager.SaveScene(scene);
            }

            CleanMissingScripts();
        }

        private static void CheckForMissingPrefab(GameObject go, List<GameObject> toRemove)
        {
            if (go == null || toRemove.Contains(go)) return;

            if (go.name.Contains("Missing Prefab") || go.name.Contains("(Missing)"))
            {
                toRemove.Add(go);
                return;
            }

            if (PrefabUtility.IsPrefabAssetMissing(go))
            {
                toRemove.Add(go);
                return;
            }

            try
            {
                var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(go);
                if (prefabStatus == PrefabInstanceStatus.MissingAsset)
                {
                    toRemove.Add(go);
                    return;
                }
            }
            catch { }
        }

        private static void CleanMissingScripts()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            int totalRemoved = 0;
            var rootObjects = scene.GetRootGameObjects();

            foreach (var rootObj in rootObjects)
            {
                var allTransforms = rootObj.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t == null || t.gameObject == null) continue;
                    var go = t.gameObject;
                    int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    if (removed > 0)
                    {
                        Debug.Log($"[SceneBuilder] Removed {removed} missing script(s) from: {go.name}");
                        totalRemoved += removed;
                    }
                }
            }

            if (totalRemoved > 0)
            {
                Debug.Log($"[SceneBuilder] Total missing scripts removed: {totalRemoved}");
                EditorSceneManager.SaveScene(scene);
            }
        }

        // ====================================================================
        //  UI HELPERS
        // ====================================================================

        /// <summary>
        /// Создать TMP текстовый элемент.
        /// </summary>
        public static TextMeshProUGUI CreateTMPText(
            GameObject parent, string name, string text,
            Vector2 position, int fontSize,
            FontStyles fontStyle, Color color)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent.transform, false);

            RectTransform rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280, fontSize + 10);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;

            return tmp;
        }

        /// <summary>
        /// Создать прогресс-бар (Slider).
        /// </summary>
        public static UnityEngine.UI.Slider CreateBar(
            GameObject parent, string name,
            Vector2 position, float width, float height,
            Color fillColor)
        {
            GameObject sliderGO = new GameObject(name);
            sliderGO.transform.SetParent(parent.transform, false);

            RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 1);
            sliderRect.anchorMax = new Vector2(0, 1);
            sliderRect.pivot = new Vector2(0, 1);
            sliderRect.anchoredPosition = position;
            sliderRect.sizeDelta = new Vector2(width, height);

            var slider = sliderGO.AddComponent<UnityEngine.UI.Slider>();

            // Background
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bgGO.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.25f);

            // Fill Area
            GameObject fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            // Fill
            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            RectTransform fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            var fillImage = fillGO.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = fillColor;

            slider.targetGraphic = bgImage;
            slider.fillRect = fillRect;
            slider.handleRect = null;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 100;
            slider.interactable = false;

            return slider;
        }

        // ====================================================================
        //  ASSET HELPERS
        // ====================================================================

        /// <summary>
        /// Проверить: есть ли ассеты в папке.
        /// </summary>
        public static bool HasAssetsInFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath)) return false;
            var guids = AssetDatabase.FindAssets("", new[] { folderPath });
            return guids.Length > 0;
        }

        /// <summary>
        /// Назначить свойство TileBase из asset файла.
        /// </summary>
        public static void AssignTileProperty(SerializedObject so, string propertyName, string assetPath)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneBuilder] Свойство '{propertyName}' не найдено");
                return;
            }

            var tileAsset = AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
            if (tileAsset != null)
                prop.objectReferenceValue = tileAsset;
            else
                Debug.LogWarning($"[SceneBuilder] Asset не найден: {assetPath}");
        }

        // ====================================================================
        //  TILE SPRITE REIMPORT
        // ====================================================================

        /// <summary>
        /// Реимпорт спрайтов тайлов с правильными PPU и прозрачностью.
        /// Все спрайты: PPU=32 Point.
        /// </summary>
        public static void ReimportTileSprites()
        {
            string[] spriteDirs = new string[] { "Assets/Sprites/Tiles" };
            int reimportCount = 0;

            foreach (string dir in spriteDirs)
            {
                if (!Directory.Exists(dir)) continue;

                string[] pngFiles = Directory.GetFiles(dir, "*.png");
                foreach (string pngPath in pngFiles)
                {
                    var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
                    if (importer == null) continue;

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 32;
                    importer.filterMode = FilterMode.Point;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.spriteBorder = Vector4.zero;
                    importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;

                    AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
                    reimportCount++;
                }
            }

            if (reimportCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[SceneBuilder] ReimportTileSprites: переимпортировано {reimportCount} спрайтов (PPU=32 Point)");
            }
        }
    }
}
#endif

// ============================================================================
// HarvestableLayerSetup.cs — Автоматическая настройка слоя и тега "Harvestable"
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-16
// Чекпоинт: 04_15_harvest_system_plan.md §7
// ============================================================================
//
// Editor-only скрипт для добавления слоя "Harvestable" и тега "Harvestable"
// в Project Settings → Tags and Layers.
//
// ИСПОЛЬЗОВАНИЕ:
// 1. Откройте Unity Editor
// 2. Перейдите в Edit → Project Settings → Tags and Layers
// 3. В разделе Layers добавьте слой "Harvestable" (рекомендуется User Layer 8 или выше)
// 4. В разделе Tags добавьте тег "Harvestable"
//
// ИЛИ используйте этот Editor скрипт:
// - Добавьте этот файл в папку Assets/Scripts/Tile/Editor/
// - В меню Unity появится: Tools → Setup Harvestable Layer
// - Нажмите для автоматической настройки
//
// ВАЖНО: Этот файл ТОЛЬКО для Editor. НЕ включайте в Build.
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace CultivationGame.TileSystem.Editor
{
    /// <summary>
    /// Автоматическая настройка Physics-слоя и тега "Harvestable".
    /// Чекпоинт §7.
    /// </summary>
    public static class HarvestableLayerSetup
    {
        private const string HARVESTABLE_LAYER = "Harvestable";
        private const string HARVESTABLE_TAG = "Harvestable";

        [MenuItem("Tools/Setup Harvestable Layer")]
        public static void SetupHarvestableLayer()
        {
            // Добавить слой "Harvestable"
            AddLayer(HARVESTABLE_LAYER);

            // Добавить тег "Harvestable"
            AddTag(HARVESTABLE_TAG);

            Debug.Log("[HarvestableLayerSetup] Слой и тег \"Harvestable\" настроены.");
        }

        private static void AddLayer(string layerName)
        {
            var tagManager = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset");
            if (tagManager == null)
            {
                Debug.LogError("[HarvestableLayerSetup] TagManager.asset не найден!");
                return;
            }

            var serializedObject = new SerializedObject(tagManager);
            var layersProp = serializedObject.FindProperty("layers");

            if (layersProp == null)
            {
                Debug.LogError("[HarvestableLayerSetup] Свойство 'layers' не найдено в TagManager!");
                return;
            }

            // Проверить, существует ли уже слой
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                var layer = layersProp.GetArrayElementAtIndex(i);
                if (layer.stringValue == layerName)
                {
                    Debug.Log($"[HarvestableLayerSetup] Слой \"{layerName}\" уже существует (index {i}).");
                    return;
                }
            }

            // Найти пустой слот (User Layer 8+)
            for (int i = 8; i < layersProp.arraySize; i++)
            {
                var layer = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log($"[HarvestableLayerSetup] Слой \"{layerName}\" добавлен (index {i}).");
                    return;
                }
            }

            Debug.LogWarning("[HarvestableLayerSetup] Нет свободных слотов для слоя! Освободите один User Layer.");
        }

        private static void AddTag(string tagName)
        {
            var tagManager = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset");
            if (tagManager == null) return;

            var serializedObject = new SerializedObject(tagManager);
            var tagsProp = serializedObject.FindProperty("tags");

            if (tagsProp == null) return;

            // Проверить, существует ли уже тег
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                {
                    Debug.Log($"[HarvestableLayerSetup] Тег \"{tagName}\" уже существует.");
                    return;
                }
            }

            // Добавить тег
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
            serializedObject.ApplyModifiedProperties();
            Debug.Log($"[HarvestableLayerSetup] Тег \"{tagName}\" добавлен.");
        }
    }
}
#endif

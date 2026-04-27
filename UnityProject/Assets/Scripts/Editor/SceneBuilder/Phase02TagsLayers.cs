// ============================================================================
// Phase02TagsLayers.cs — Фаза 02: Теги, слои и Sorting Layers
// Cultivation World Simulator
// ============================================================================
// Объединяет: Phase 02 (FullSceneBuilder) + PATCH-001, PATCH-011, PATCH-012
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase02TagsLayers : IScenePhase
    {
        public string Name => "Tags & Layers";
        public string MenuPath => "Phase 02: Tags and Layers";
        public int Order => 2;

        public bool IsNeeded()
        {
            // Проверяем теги
            var tags = UnityEditorInternal.InternalEditorUtility.tags;
            foreach (var requiredTag in SceneBuilderConstants.REQUIRED_TAGS)
            {
                bool found = false;
                foreach (var existingTag in tags)
                {
                    if (existingTag == requiredTag) { found = true; break; }
                }
                if (!found) return true;
            }

            // Проверяем Physics Layers
            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            foreach (var kvp in SceneBuilderConstants.REQUIRED_LAYERS)
            {
                bool found = false;
                foreach (var existingLayer in layers)
                {
                    if (existingLayer == kvp.Key) { found = true; break; }
                }
                if (!found) return true;
            }

            // Проверяем Sorting Layers — существование, порядок И uniqueID
            var sortingLayers = SortingLayer.layers;
            string[] requiredOrder = SceneBuilderConstants.REQUIRED_SORTING_LAYERS;

            int[] indices = new int[requiredOrder.Length];
            for (int i = 0; i < requiredOrder.Length; i++)
            {
                indices[i] = -1;
                for (int j = 0; j < sortingLayers.Length; j++)
                {
                    if (sortingLayers[j].name == requiredOrder[i])
                    {
                        indices[i] = j;
                        break;
                    }
                }
                if (indices[i] < 0) return true; // Слой не найден — НУЖНО создать
            }

            // Строгий порядок
            for (int i = 1; i < indices.Length; i++)
            {
                if (indices[i] <= indices[i - 1])
                    return true;
            }

            // Проверяем, что uniqueID совпадают с индексами (детерминированность)
            for (int i = 0; i < sortingLayers.Length; i++)
            {
                if (sortingLayers[i].id != i)
                    return true; // uniqueID не детерминирован — нужно переназначить
            }

            // PATCH-012: Layer 11 не должен называться "UI" (конфликт с built-in layer 5)
            var tagManager = SceneBuilderUtils.LoadTagManager();
            if (tagManager != null)
            {
                var layersProp = tagManager.FindProperty("layers");
                if (layersProp != null && 11 < layersProp.arraySize)
                {
                    var layer11 = layersProp.GetArrayElementAtIndex(11);
                    if (layer11 != null && layer11.stringValue == "UI")
                        return true; // Нужен PATCH-012
                }
            }

            return false;
        }

        public void Execute()
        {
            ExecuteTags();
            ExecuteLayers();
            SceneBuilderUtils.EnsureSortingLayers();
            ExecutePatch012();
        }

        private void ExecuteTags()
        {
            var tags = new List<string>(UnityEditorInternal.InternalEditorUtility.tags);
            bool tagsChanged = false;

            foreach (var requiredTag in SceneBuilderConstants.REQUIRED_TAGS)
            {
                if (!tags.Contains(requiredTag))
                {
                    tags.Add(requiredTag);
                    tagsChanged = true;
                }
            }

            if (tagsChanged)
            {
                SerializedObject tagManager = SceneBuilderUtils.LoadTagManager();
                if (tagManager == null) return;

                var tagsProp = tagManager.FindProperty("tags");
                if (tagsProp != null)
                {
                    tagsProp.ClearArray();
                    for (int i = 0; i < tags.Count; i++)
                    {
                        tagsProp.InsertArrayElementAtIndex(i);
                        tagsProp.GetArrayElementAtIndex(i).stringValue = tags[i];
                    }
                    tagManager.ApplyModifiedProperties();
                }
                Debug.Log($"[Phase02] Tags updated: {string.Join(", ", SceneBuilderConstants.REQUIRED_TAGS)}");
            }
        }

        private void ExecuteLayers()
        {
            SerializedObject layerManager = SceneBuilderUtils.LoadTagManager();
            if (layerManager == null) return;

            var layersProp = layerManager.FindProperty("layers");
            bool layersChanged = false;

            if (layersProp != null)
            {
                foreach (var kvp in SceneBuilderConstants.REQUIRED_LAYERS)
                {
                    var element = layersProp.GetArrayElementAtIndex(kvp.Value);
                    if (element != null)
                    {
                        if (string.IsNullOrEmpty(element.stringValue))
                        {
                            element.stringValue = kvp.Key;
                            layersChanged = true;
                        }
                        else if (element.stringValue != kvp.Key)
                        {
                            Debug.LogWarning($"[Phase02] Слой {kvp.Value} занят '{element.stringValue}', ожидается '{kvp.Key}'");
                        }
                    }
                }

                if (layersChanged)
                {
                    layerManager.ApplyModifiedProperties();
                    Debug.Log("[Phase02] Layers updated");
                }
            }
        }

        /// <summary>
        /// PATCH-012: Layer 11 "UI" → "GameUI" — избегаем конфликта с Unity built-in layer 5 "UI"
        /// </summary>
        private void ExecutePatch012()
        {
            var tagManager = SceneBuilderUtils.LoadTagManager();
            if (tagManager == null) return;

            var layersProp = tagManager.FindProperty("layers");
            if (layersProp == null) return;

            if (11 < layersProp.arraySize)
            {
                var layer11 = layersProp.GetArrayElementAtIndex(11);
                if (layer11 != null && layer11.stringValue == "UI")
                {
                    layer11.stringValue = "GameUI";
                    tagManager.ApplyModifiedProperties();
                    Debug.Log("[Phase02] PATCH-012: Layer 11 переименован из 'UI' в 'GameUI'");
                }
            }
        }
    }
}
#endif

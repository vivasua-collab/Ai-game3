// ============================================================================
// Phase00URPSetup.cs — Фаза 00: URP Asset Setup
// Cultivation World Simulator
// ============================================================================
// Создаёт Universal Render Pipeline Asset и Renderer2D Data Asset.
// Назначает URP Asset как текущий Render Pipeline в GraphicsSettings.
//
// Без этой фазы при удалении Assets/ папки URP ассеты пропадают,
// Unity fallback на Built-in renderer → спрайты рендерятся неправильно.
//
// Создано: 2026-04-25 13:45:00 UTC
// Редактировано: 2026-04-26 — FIX: CS0103 GraphicsSettings + CS0136 variable conflicts
//   - Добавлен using UnityEngine.Rendering (GraphicsSettings находится там)
//   - currentRenderPipeline — read-only в Unity 6.3, назначение через SerializedObject
//   - Переименованы конфликтующие переменные so/rendererListProp
//   - FIX-v2: GraphicsSettings.currentRenderPipeline read-only → SerializedObject подход
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase00URPSetup : IScenePhase
    {
        public string Name => "URP Setup";
        public string MenuPath => "Phase 00: URP Asset Setup";
        public int Order => 0;

        private const string SETTINGS_FOLDER = "Assets/Settings";
        private const string URP_ASSET_PATH = "Assets/Settings/UniversalRP.asset";
        private const string RENDERER2D_PATH = "Assets/Settings/Renderer2D.asset";

        public bool IsNeeded()
        {
            // Нужна если URP Asset или Renderer2D не существуют,
            // или если GraphicsSettings не назначен
            bool hasURPAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URP_ASSET_PATH) != null;
            bool hasRenderer2D = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RENDERER2D_PATH) != null;
            bool hasGraphicsSettings = GraphicsSettings.currentRenderPipeline != null;

            return !hasURPAsset || !hasRenderer2D || !hasGraphicsSettings;
        }

        public void Execute()
        {
            SceneBuilderUtils.EnsureDirectory(SETTINGS_FOLDER);

            // Шаг 1: Создать Renderer2D Data Asset
            var renderer2DData = EnsureRenderer2DAsset();

            // Шаг 2: Создать UniversalRenderPipelineAsset
            var urpAsset = EnsureURPAsset(renderer2DData);

            // Шаг 3: Назначить URP Asset в GraphicsSettings
            AssignToGraphicsSettings(urpAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase00] ✅ URP Setup завершён: UniversalRP.asset + Renderer2D.asset + GraphicsSettings");
        }

        /// <summary>
        /// Создать или загрузить Renderer2D Data Asset.
        /// Это 2D рендерер для URP — без него Light2D не работает.
        /// </summary>
        private Renderer2DData EnsureRenderer2DAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RENDERER2D_PATH);
            if (existing != null)
            {
                Debug.Log("[Phase00] Renderer2D.asset уже существует");
                return existing;
            }

            // Создаём через ScriptableObject (Reflection не нужен — тип в прямой ссылке)
            var rendererData = ScriptableObject.CreateInstance<Renderer2DData>();

            // Настройки по умолчанию
            // Редактировано: 2026-04-25 14:33:00 MSK — m_DefaultMaterialType = 1 (Unlit)
            // Причина: Lit default рендерит ЧЁРНЫМ без Light2D в сцене.
            // Unlit не требует Light2D — спрайты видны сразу.
            // Если Light2D будет добавлен позже — переключить обратно на 0 (Lit).
            var rendererSo = new SerializedObject(rendererData);

            // m_DefaultMaterialType: 0 = Lit, 1 = Unlit, 2 = Custom
            var defaultMatTypeProp = rendererSo.FindProperty("m_DefaultMaterialType");
            if (defaultMatTypeProp != null)
                defaultMatTypeProp.intValue = 1; // Unlit — не требует Light2D

            // m_HDREmulationScale
            var hdrProp = rendererSo.FindProperty("m_HDREmulationScale");
            if (hdrProp != null) hdrProp.floatValue = 1f;

            // m_LightRenderTextureScale
            var lightRTScale = rendererSo.FindProperty("m_LightRenderTextureScale");
            if (lightRTScale != null) lightRTScale.floatValue = 0.5f;

            // m_UseDepthStencilBuffer
            var depthProp = rendererSo.FindProperty("m_UseDepthStencilBuffer");
            if (depthProp != null) depthProp.boolValue = true;

            rendererSo.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(rendererData, RENDERER2D_PATH);
            Debug.Log($"[Phase00] Создан Renderer2D.asset: {RENDERER2D_PATH}");

            return rendererData;
        }

        /// <summary>
        /// Создать или загрузить UniversalRenderPipelineAsset.
        /// Это главный ассет URP — назначается в GraphicsSettings.
        /// </summary>
        private UniversalRenderPipelineAsset EnsureURPAsset(Renderer2DData renderer2DData)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URP_ASSET_PATH);
            if (existing != null)
            {
                // Проверяем, что renderer2D назначен в существующий ассет
                var existingSo = new SerializedObject(existing);
                var existingRendererList = existingSo.FindProperty("m_RendererDataList");
                if (existingRendererList != null && existingRendererList.arraySize > 0)
                {
                    var firstRenderer = existingRendererList.GetArrayElementAtIndex(0);
                    if (firstRenderer.objectReferenceValue == null && renderer2DData != null)
                    {
                        firstRenderer.objectReferenceValue = renderer2DData;
                        existingSo.ApplyModifiedProperties();
                        Debug.Log("[Phase00] Renderer2D назначен в существующий UniversalRP.asset");
                    }
                }
                Debug.Log("[Phase00] UniversalRP.asset уже существует");
                return existing;
            }

            // Создаём UniversalRenderPipelineAsset
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();

            var urpSo = new SerializedObject(urpAsset);

            // Renderer Type = 1 (2D Renderer)
            var rendererTypeProp = urpSo.FindProperty("m_RendererType");
            if (rendererTypeProp != null)
                rendererTypeProp.intValue = 1; // 1 = 2D Renderer

            // Назначить Renderer2D в список рендереров
            var urpRendererList = urpSo.FindProperty("m_RendererDataList");
            if (urpRendererList != null && renderer2DData != null)
            {
                urpRendererList.ClearArray();
                urpRendererList.InsertArrayElementAtIndex(0);
                urpRendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer2DData;
            }

            // Default renderer index = 0
            var defaultRendererProp = urpSo.FindProperty("m_DefaultRendererIndex");
            if (defaultRendererProp != null)
                defaultRendererProp.intValue = 0;

            // SRP Batcher
            var srpBatcherProp = urpSo.FindProperty("m_UseSRPBatcher");
            if (srpBatcherProp != null)
                srpBatcherProp.boolValue = true;

            // MSAA = 1 (disabled for 2D)
            var msaaProp = urpSo.FindProperty("m_MSAA");
            if (msaaProp != null)
                msaaProp.intValue = 1;

            // Render Scale = 1
            var renderScaleProp = urpSo.FindProperty("m_RenderScale");
            if (renderScaleProp != null)
                renderScaleProp.floatValue = 1f;

            // HDR
            var hdrProp = urpSo.FindProperty("m_SupportsHDR");
            if (hdrProp != null)
                hdrProp.boolValue = true;

            urpSo.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(urpAsset, URP_ASSET_PATH);
            Debug.Log($"[Phase00] Создан UniversalRP.asset: {URP_ASSET_PATH}");

            return urpAsset;
        }

        /// <summary>
        /// Назначить UniversalRenderPipelineAsset как текущий Render Pipeline.
        /// Без этого Unity использует Built-in renderer.
        ///
        /// Unity 6.3: GraphicsSettings.currentRenderPipeline — READ-ONLY,
        /// прямое присвоение невозможно (CS0200).
        /// Единственный способ — SerializedObject на GraphicsSettings.asset.
        /// Свойство в сериализации: m_CustomRenderPipeline.
        /// </summary>
        private void AssignToGraphicsSettings(UniversalRenderPipelineAsset urpAsset)
        {
            if (urpAsset == null)
            {
                Debug.LogError("[Phase00] Не удалось назначить URP — asset null!");
                return;
            }

            // Проверяем через read-only свойство (чтение доступно)
            if (GraphicsSettings.currentRenderPipeline == urpAsset)
            {
                Debug.Log("[Phase00] GraphicsSettings уже назначен");
                return;
            }

            // Unity 6.3: currentRenderPipeline read-only → назначаем через SerializedObject
            // GraphicsSettings хранится как ProjectSettings/GraphicsSettings.asset
            const string graphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(graphicsSettingsPath);

            bool assigned = false;
            foreach (var asset in allAssets)
            {
                // Ищем именно UnityEngine.Rendering.GraphicsSettings
                if (asset.GetType().Name != "GraphicsSettings") continue;

                var so = new SerializedObject(asset);
                var rpProp = so.FindProperty("m_CustomRenderPipeline");
                if (rpProp == null)
                {
                    Debug.LogWarning("[Phase00] m_CustomRenderPipeline property не найден в GraphicsSettings.asset");
                    continue;
                }

                if (rpProp.objectReferenceValue == urpAsset)
                {
                    Debug.Log("[Phase00] GraphicsSettings уже назначен (через SerializedObject)");
                    assigned = true;
                    break;
                }

                rpProp.objectReferenceValue = urpAsset;
                so.ApplyModifiedProperties();
                Debug.Log($"[Phase00] ✅ GraphicsSettings.m_CustomRenderPipeline назначен: {urpAsset.name} (через SerializedObject)");
                assigned = true;
                break;
            }

            if (!assigned)
            {
                Debug.LogError(
                    "[Phase00] ❌ Не удалось назначить URP в GraphicsSettings! " +
                    "GraphicsSettings.asset не найден или m_CustomRenderPipeline недоступен. " +
                    "Назначьте вручную: Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings");
            }

            // Также проверить QualitySettings
            int qualityLevel = QualitySettings.GetQualityLevel();
            var qualityRPAsset = QualitySettings.GetRenderPipelineAssetAt(qualityLevel);
            if (qualityRPAsset == null)
            {
                Debug.Log("[Phase00] QualitySettings Render Pipeline не назначен — используется GraphicsSettings default");
            }
            else if (qualityRPAsset != urpAsset)
            {
                Debug.LogWarning($"[Phase00] QualitySettings[{qualityLevel}] использует другой RP asset: {qualityRPAsset.name}");
            }
        }
    }
}
#endif

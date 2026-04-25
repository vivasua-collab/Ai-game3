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
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
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

            // Настройки по умолчанию (совпадают с существующим Renderer2D.asset)
            // m_DefaultMaterialType = 0 (Lit) — чтобы Light2D работал
            var so = new SerializedObject(rendererData);

            // m_DefaultMaterialType: 0 = Lit, 1 = Unlit, 2 = Custom
            var defaultMatTypeProp = so.FindProperty("m_DefaultMaterialType");
            if (defaultMatTypeProp != null)
                defaultMatTypeProp.intValue = 0; // Lit — Light2D будет освещать спрайты

            // m_HDREmulationScale
            var hdrProp = so.FindProperty("m_HDREmulationScale");
            if (hdrProp != null) hdrProp.floatValue = 1f;

            // m_LightRenderTextureScale
            var lightRTScale = so.FindProperty("m_LightRenderTextureScale");
            if (lightRTScale != null) lightRTScale.floatValue = 0.5f;

            // m_UseDepthStencilBuffer
            var depthProp = so.FindProperty("m_UseDepthStencilBuffer");
            if (depthProp != null) depthProp.boolValue = true;

            so.ApplyModifiedProperties();

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
                // Проверяем, что renderer2D назначен
                var so = new SerializedObject(existing);
                var rendererListProp = so.FindProperty("m_RendererDataList");
                if (rendererListProp != null && rendererListProp.arraySize > 0)
                {
                    var firstRenderer = rendererListProp.GetArrayElementAtIndex(0);
                    if (firstRenderer.objectReferenceValue == null && renderer2DData != null)
                    {
                        firstRenderer.objectReferenceValue = renderer2DData;
                        so.ApplyModifiedProperties();
                        Debug.Log("[Phase00] Renderer2D назначен в существующий UniversalRP.asset");
                    }
                }
                Debug.Log("[Phase00] UniversalRP.asset уже существует");
                return existing;
            }

            // Создаём UniversalRenderPipelineAsset
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();

            var so = new SerializedObject(urpAsset);

            // Renderer Type = 1 (2D Renderer)
            var rendererTypeProp = so.FindProperty("m_RendererType");
            if (rendererTypeProp != null)
                rendererTypeProp.intValue = 1; // 1 = 2D Renderer

            // Назначить Renderer2D в список рендереров
            var rendererListProp = so.FindProperty("m_RendererDataList");
            if (rendererListProp != null && renderer2DData != null)
            {
                rendererListProp.ClearArray();
                rendererListProp.InsertArrayElementAtIndex(0);
                rendererListProp.GetArrayElementAtIndex(0).objectReferenceValue = renderer2DData;
            }

            // Default renderer index = 0
            var defaultRendererProp = so.FindProperty("m_DefaultRendererIndex");
            if (defaultRendererProp != null)
                defaultRendererProp.intValue = 0;

            // SRP Batcher
            var srpBatcherProp = so.FindProperty("m_UseSRPBatcher");
            if (srpBatcherProp != null)
                srpBatcherProp.boolValue = true;

            // MSAA = 1 (disabled for 2D)
            var msaaProp = so.FindProperty("m_MSAA");
            if (msaaProp != null)
                msaaProp.intValue = 1;

            // Render Scale = 1
            var renderScaleProp = so.FindProperty("m_RenderScale");
            if (renderScaleProp != null)
                renderScaleProp.floatValue = 1f;

            // HDR
            var hdrProp = so.FindProperty("m_SupportsHDR");
            if (hdrProp != null)
                hdrProp.boolValue = true;

            so.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(urpAsset, URP_ASSET_PATH);
            Debug.Log($"[Phase00] Создан UniversalRP.asset: {URP_ASSET_PATH}");

            return urpAsset;
        }

        /// <summary>
        /// Назначить UniversalRenderPipelineAsset как текущий Render Pipeline.
        /// Без этого Unity использует Built-in renderer.
        /// </summary>
        private void AssignToGraphicsSettings(UniversalRenderPipelineAsset urpAsset)
        {
            if (urpAsset == null)
            {
                Debug.LogError("[Phase00] Не удалось назначить URP — asset null!");
                return;
            }

            if (GraphicsSettings.currentRenderPipeline == urpAsset)
            {
                Debug.Log("[Phase00] GraphicsSettings уже назначен");
                return;
            }

            GraphicsSettings.currentRenderPipeline = urpAsset;
            Debug.Log($"[Phase00] ✅ GraphicsSettings.currentRenderPipeline назначен: {urpAsset.name}");

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

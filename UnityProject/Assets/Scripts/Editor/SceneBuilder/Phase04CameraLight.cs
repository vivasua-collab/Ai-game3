// ============================================================================
// Phase04CameraLight.cs — Фаза 04: Камера и освещение
// Cultivation World Simulator
// ============================================================================
// Объединяет: Phase 04 (FullSceneBuilder) + PATCH-004, PATCH-009, PATCH-010
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Core;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase04CameraLight : IScenePhase
    {
        public string Name => "Camera & Light";
        public string MenuPath => "Phase 04: Camera and Light";
        public int Order => 4;

        public bool IsNeeded()
        {
            SceneBuilderUtils.EnsureSceneOpen();
            var cam = Camera.main;
            bool hasLight2D = GameObject.Find("GlobalLight2D") != null;
            return cam == null || !hasLight2D;
        }

        public void Execute()
        {
            SceneBuilderUtils.EnsureSceneOpen();

            ExecuteCamera();
            ExecuteDirectionalLight();
            ExecuteGlobalLight2D();
            ExecuteDiagnostics();
        }

        private void ExecuteCamera()
        {
            var camObj = Camera.main?.gameObject;
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            camObj.transform.position = new Vector3(0, 0, -10);
            var cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.18f, 0.08f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 8f;

            // Camera2DSetup — камера следит за игроком с ограничением границами карты
            var camera2D = camObj.GetComponent<Camera2DSetup>();
            if (camera2D == null)
                camera2D = camObj.AddComponent<Camera2DSetup>();

            var camSo = new SerializedObject(camera2D);
            camSo.FindProperty("orthographicSize").floatValue = 8f;
            camSo.FindProperty("followEnabled").boolValue = true;
            camSo.FindProperty("followSmoothness").floatValue = 0.08f;
            camSo.FindProperty("useBounds").boolValue = true;
            camSo.FindProperty("boundsMin").vector2Value = Vector2.zero; // PATCH-009
            camSo.FindProperty("boundsMax").vector2Value = new Vector2(200f, 160f);
            camSo.ApplyModifiedProperties();

            // PATCH-010: Undo registration
            Undo.RegisterCreatedObjectUndo(camObj, "Create Camera");
            Undo.RecordObject(cam, "Configure Camera");
            Undo.RecordObject(cam.transform, "Configure Camera Transform");

            Debug.Log("[Phase04] Camera configured with Camera2DSetup");
        }

        private void ExecuteDirectionalLight()
        {
            var lightObj = GameObject.Find("Directional Light");
            if (lightObj == null)
            {
                lightObj = new GameObject("Directional Light");
                var light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                light.color = Color.white;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                Undo.RegisterCreatedObjectUndo(lightObj, "Create Light");
            }
        }

        /// <summary>
        /// PATCH-004: Global Light2D для URP 2D Renderer.
        /// Без Light2D все спрайты рендерятся как чёрные при Sprite-Lit-Default.
        /// </summary>
        private void ExecuteGlobalLight2D()
        {
            var light2DObj = GameObject.Find("GlobalLight2D");
            if (light2DObj == null)
            {
                light2DObj = new GameObject("GlobalLight2D");
                var light2DType = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.2D.RenderPipeline.Runtime");
                if (light2DType != null)
                {
                    var light2D = light2DObj.AddComponent(light2DType);
                    var lightTypeProp = light2DType.GetProperty("lightType");
                    if (lightTypeProp != null) lightTypeProp.SetValue(light2D, 1);
                    var intensityProp = light2DType.GetProperty("intensity");
                    if (intensityProp != null) intensityProp.SetValue(light2D, 1f);
                    var colorProp = light2DType.GetProperty("color");
                    if (colorProp != null) colorProp.SetValue(light2D, Color.white);

                    Undo.RegisterCreatedObjectUndo(light2DObj, "Create Global Light2D");
                    Debug.Log("[Phase04] Global Light2D создан");
                }
                else
                {
                    Object.DestroyImmediate(light2DObj);
                    Debug.LogWarning("[Phase04] Light2D тип не найден через Reflection");
                }
            }
        }

        private void ExecuteDiagnostics()
        {
            RenderPipelineLogger.LogCameraState();
            RenderPipelineLogger.LogLightState();
        }
    }
}
#endif

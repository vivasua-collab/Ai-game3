// ============================================================================
// Phase04CameraLight.cs — Фаза 04: Камера и освещение
// Cultivation World Simulator
// ============================================================================
// Объединяет: Phase 04 (FullSceneBuilder) + PATCH-004, PATCH-009, PATCH-010
// Редактировано: 2026-04-25 10:20:00 UTC — FIX: GlobalLight2D reflection fallback для Unity 6.3
// Редактировано: 2026-04-25 13:30:00 UTC — FIX-v2: Добавлена 4-я сборка Unity.RenderPipeline.Universal.Runtime
//   + short-name fallback + диагностические логи загруженных сборок
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
        ///
        /// Редактировано: 2026-04-25 v2 — Расширен поиск Light2D типа:
        ///   Unity 2022.3: Unity.2D.RenderPipeline.Runtime
        ///   Unity 6.x (old): Unity.RenderPipeline.Universal.2D.Runtime
        ///   Unity 6.3 (NEW): Unity.RenderPipeline.Universal.Runtime  ← ДОБАВЛЕНО
        ///   Fallback 1: поиск по fullName во всех AppDomain сборках
        ///   Fallback 2: поиск по короткому имени "Light2D" во всех сборках
        ///   Fallback 3: прямой AddComponent если тип доступен через typeof
        /// </summary>
        private void ExecuteGlobalLight2D()
        {
            var light2DObj = GameObject.Find("GlobalLight2D");
            if (light2DObj != null) return; // Уже существует

            light2DObj = new GameObject("GlobalLight2D");

            // Пробуем несколько имён сборок — в разных версиях Unity тип Light2D
            // находится в разных сборках:
            var light2DType = ResolveLight2DType();

            if (light2DType != null)
            {
                var light2D = light2DObj.AddComponent(light2DType);

                // lightType = Global (1)
                var lightTypeProp = light2DType.GetProperty("lightType")
                                ?? light2DType.GetProperty("m_LightType");
                if (lightTypeProp != null) lightTypeProp.SetValue(light2D, 1);

                var intensityProp = light2DType.GetProperty("intensity")
                                ?? light2DType.GetProperty("m_Intensity");
                if (intensityProp != null) intensityProp.SetValue(light2D, 1f);

                var colorProp = light2DType.GetProperty("color")
                            ?? light2DType.GetProperty("m_Color");
                if (colorProp != null) colorProp.SetValue(light2D, Color.white);

                Undo.RegisterCreatedObjectUndo(light2DObj, "Create Global Light2D");
                Debug.Log($"[Phase04] ✅ Global Light2D создан (сборка: {light2DType.Assembly.GetName().Name}, тип: {light2DType.FullName})");
            }
            else
            {
                Object.DestroyImmediate(light2DObj);
                Debug.LogError(
                    "[Phase04] ❌ Light2D тип НЕ НАЙДЕН ни в одной сборке! " +
                    "Установите пакет com.unity.render-pipelines.universal и убедитесь, " +
                    "что 2D Renderer включён в URP Asset. " +
                    "Без Light2D спрайты будут чёрными при Sprite-Lit-Default.");
                LogAssemblyDiagnostics();
            }
        }

        /// <summary>
        /// Поиск типа Light2D всеми доступными способами.
        /// Возвращает найденный тип или null.
        /// </summary>
        public static System.Type ResolveLight2DType()
        {
            // Шаг 1: Типичные имена сборок для разных версий Unity
            string[] assemblyNames = new string[]
            {
                "Unity.2D.RenderPipeline.Runtime",                     // Unity 2022.3
                "Unity.RenderPipeline.Universal.2D.Runtime",           // Unity 6.x (старый путь)
                "Unity.RenderPipeline.Universal.Runtime",               // Unity 6.3 (НОВЫЙ!)
            };

            string fullTypeName = "UnityEngine.Rendering.Universal.Light2D";

            foreach (var asmName in assemblyNames)
            {
                var type = System.Type.GetType($"{fullTypeName}, {asmName}");
                if (type != null)
                {
                    Debug.Log($"[Phase04] Light2D найден через сборку: {asmName}");
                    return type;
                }
                Debug.Log($"[Phase04] Light2D НЕ в сборке: {asmName}");
            }

            // Шаг 2: Fallback — поиск по fullName во ВСЕХ загруженных сборках
            var foundByFullName = FindTypeByName(fullTypeName);
            if (foundByFullName != null)
            {
                Debug.Log($"[Phase04] Light2D найден через AppDomain search: {foundByFullName.Assembly.GetName().Name}");
                return foundByFullName;
            }

            // Шаг 3: Fallback — поиск по короткому имени "Light2D"
            // В Unity 6.x тип может быть без namespace или с другим namespace
            var foundByShortName = FindTypeByShortName("Light2D");
            if (foundByShortName != null)
            {
                Debug.Log($"[Phase04] Light2D найден по короткому имени: {foundByShortName.FullName} (сборка: {foundByShortName.Assembly.GetName().Name})");
                return foundByShortName;
            }

            Debug.LogWarning("[Phase04] Light2D тип НЕ НАЙДЕН никаким способом");
            return null;
        }

        /// <summary>
        /// Поиск типа по fullname во всех загруженных сборках (fallback).
        /// </summary>
        private static System.Type FindTypeByName(string fullName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullName);
                    if (type != null) return type;
                }
                catch (System.Exception)
                {
                    // Некоторые сборки могут выбрасывать исключения при GetType()
                }
            }
            return null;
        }

        /// <summary>
        /// Поиск типа по короткому имени во всех загруженных сборках.
        /// Fallback для случая, когда namespace изменился.
        /// </summary>
        private static System.Type FindTypeByShortName(string shortName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == shortName && type.IsSubclassOf(typeof(Component)))
                            return type;
                    }
                }
                catch (System.Reflection.ReflectionTypeLoadException)
                {
                    // Некоторые сборки не позволяют загрузить все типы
                }
                catch (System.Exception)
                {
                    // Игнорируем другие ошибки
                }
            }
            return null;
        }

        /// <summary>
        /// Диагностика: логировать все загруженные сборки, содержащие "Render" или "2D".
        /// Вызывается только если Light2D тип НЕ НАЙДЕН.
        /// </summary>
        private static void LogAssemblyDiagnostics()
        {
            Debug.LogWarning("[Phase04] === ДИАГНОСТИКА СБОРОК ===");
            int renderRelated = 0;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (name.Contains("Render") || name.Contains("2D") || name.Contains("2d") ||
                    name.Contains("Universal") || name.Contains("Pipeline"))
                {
                    Debug.Log($"[Phase04]   Сборка: {name}");
                    renderRelated++;
                }
            }
            Debug.Log($"[Phase04] Всего render-related сборок: {renderRelated}");

            // Также пробуем найти любой тип с именем Light2D
            Debug.Log("[Phase04] Поиск любого типа с именем 'Light2D'...");
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("Light2D") || type.Name.Contains("Light2d"))
                        {
                            Debug.Log($"[Phase04]   Найден тип: {type.FullName} в сборке {assembly.GetName().Name}");
                        }
                    }
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    // Логируем типы которые удалось загрузить
                    foreach (var type in ex.Types)
                    {
                        if (type != null && (type.Name.Contains("Light2D") || type.Name.Contains("Light2d")))
                        {
                            Debug.Log($"[Phase04]   Найден тип (partial): {type.FullName} в сборке {assembly.GetName().Name}");
                        }
                    }
                }
                catch (System.Exception) { }
            }
            Debug.LogWarning("[Phase04] === КОНЕЦ ДИАГНОСТИКИ ===");
        }

        private void ExecuteDiagnostics()
        {
            RenderPipelineLogger.LogCameraState();
            RenderPipelineLogger.LogLightState();
        }
    }
}
#endif

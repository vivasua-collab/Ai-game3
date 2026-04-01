// ============================================================================
// Camera2DSetup.cs — Автоматическая настройка камеры для 2D
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-03-31 09:54:21 UTC
// ============================================================================

using UnityEngine;

namespace CultivationGame.Core
{
    /// <summary>
    /// Автоматически настраивает камеру для 2D игры.
    /// Добавь к Main Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class Camera2DSetup : MonoBehaviour
    {
        [Header("2D Camera Settings")]
        [Tooltip("Z позиция камеры (должна быть отрицательной)")]
        public float cameraZ = -10f;
        
        [Tooltip("Размер ортографической проекции")]
        public float orthographicSize = 5f;
        
        [Tooltip("Цвет фона")]
        public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Тёмно-серый
        
        [Tooltip("Настраивать при старте")]
        public bool setupOnStart = true;
        
        private Camera cam;
        
        private void Awake()
        {
            cam = GetComponent<Camera>();
            
            if (setupOnStart)
            {
                SetupCamera();
            }
        }
        
        [ContextMenu("Setup Camera Now")]
        public void SetupCamera()
        {
            if (cam == null)
                cam = GetComponent<Camera>();
            
            // Ортографическая проекция
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            
            // Z позиция
            Vector3 pos = transform.position;
            pos.z = cameraZ;
            transform.position = pos;
            
            // Цвет фона
            cam.backgroundColor = backgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            
            // Глубина
            cam.depth = 0;
            
            Debug.Log($"Camera 2D Setup Complete:\n" +
                      $"  Position: {transform.position}\n" +
                      $"  Orthographic: {cam.orthographic}\n" +
                      $"  Size: {cam.orthographicSize}\n" +
                      $"  Background: {backgroundColor}");
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Автоприменение в редакторе при изменении значений
            if (cam != null && !Application.isPlaying)
            {
                SetupCamera();
            }
        }
#endif
    }
}

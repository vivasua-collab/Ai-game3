// ============================================================================
// PlayerVisual.cs — Визуальное отображение игрока
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:17:18 UTC
// ============================================================================

using UnityEngine;

namespace CultivationGame.Player
{
    /// <summary>
    /// Автоматически создаёт визуал для игрока.
    /// Добавь этот компонент к Player.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerVisual : MonoBehaviour
    {
        [Header("Visual Settings")]
        [Tooltip("Цвет игрока")]
        public Color playerColor = new Color(0.2f, 0.8f, 0.3f); // Зелёный
        
        [Tooltip("Размер игрока")]
        public float size = 0.8f;
        
        [Tooltip("Создать тень")]
        public bool createShadow = true;
        
        [Tooltip("Цвет тени")]
        public Color shadowColor = new Color(0f, 0f, 0f, 0.3f);
        
        [Header("Debug")]
        [Tooltip("Показывать Gizmos в Scene View")]
        public bool showGizmos = true;
        
        private SpriteRenderer mainSprite;
        private SpriteRenderer shadowSprite;
        private GameObject visualObj;
        private GameObject shadowObj;
        
        private void Awake()
        {
            CreateVisual();
        }
        
        private void CreateVisual()
        {
            // Убеждаемся, что игрок на Z = 0
            Vector3 pos = transform.position;
            pos.z = 0f;
            transform.position = pos;
            
            // Создаём визуальный объект
            visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(transform);
            visualObj.transform.localPosition = new Vector3(0f, 0f, 0f); // Z = 0 для 2D
            visualObj.transform.localRotation = Quaternion.identity;
            visualObj.transform.localScale = new Vector3(size, size, 1f);
            
            // Добавляем SpriteRenderer
            mainSprite = visualObj.AddComponent<SpriteRenderer>();
            
            // Создаём текстуру программно
            mainSprite.sprite = CreateCircleSprite();
            mainSprite.color = playerColor;
            mainSprite.sortingOrder = 10;
            
            // ВАЖНО: Устанавливаем материал для URP
            mainSprite.material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            if (mainSprite.material == null)
            {
                mainSprite.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            // Создаём тень
            if (createShadow)
            {
                shadowObj = new GameObject("Shadow");
                shadowObj.transform.SetParent(transform);
                shadowObj.transform.localPosition = new Vector3(0.05f, -0.05f, 0f);
                shadowObj.transform.localRotation = Quaternion.identity;
                shadowObj.transform.localScale = new Vector3(size * 1.1f, size * 1.1f, 1f);
                
                shadowSprite = shadowObj.AddComponent<SpriteRenderer>();
                shadowSprite.sprite = CreateCircleSprite();
                shadowSprite.color = shadowColor;
                shadowSprite.sortingOrder = 9;
                
                // Тот же материал
                shadowSprite.material = mainSprite.material;
            }
            
            Debug.Log($"Player visual created: Color={playerColor}, Size={size}");
        }
        
        /// <summary>
        /// Создаёт спрайт круга программно
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            int resolution = 64;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            
            Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
            float radius = resolution / 2f - 2f;
            
            Color[] pixels = new Color[resolution * resolution];
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    int index = y * resolution + x;
                    
                    if (dist <= radius)
                    {
                        // Плавные края
                        float alpha = Mathf.SmoothStep(radius, radius - 2f, dist);
                        pixels[index] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        pixels[index] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution
            );
        }
        
        /// <summary>
        /// Изменить цвет игрока
        /// </summary>
        public void SetColor(Color color)
        {
            playerColor = color;
            if (mainSprite != null)
            {
                mainSprite.color = color;
            }
        }
        
        /// <summary>
        /// Мигнуть цветом (для урона и т.д.)
        /// </summary>
        public void Flash(Color flashColor, float duration = 0.1f)
        {
            if (mainSprite != null)
            {
                StartCoroutine(FlashCoroutine(flashColor, duration));
            }
        }
        
        private System.Collections.IEnumerator FlashCoroutine(Color flashColor, float duration)
        {
            Color originalColor = mainSprite.color;
            mainSprite.color = flashColor;
            yield return new WaitForSeconds(duration);
            mainSprite.color = originalColor;
        }
        
        /// <summary>
        /// Скрыть/показать визуал
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (mainSprite != null)
                mainSprite.enabled = visible;
            if (shadowSprite != null)
                shadowSprite.enabled = visible;
        }
        
        /// <summary>
        /// Проверить, что камера настроена правильно для 2D
        /// </summary>
        public static void EnsureCamera2DSetup()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                // Камера должна быть ортографической
                cam.orthographic = true;
                
                // Камера должна быть на Z = -10
                Vector3 camPos = cam.transform.position;
                if (camPos.z >= 0)
                {
                    camPos.z = -10f;
                    cam.transform.position = camPos;
                    Debug.Log("Camera Z position adjusted to -10 for 2D rendering");
                }
                
                // Размер ортографической проекции
                if (cam.orthographicSize < 1f)
                {
                    cam.orthographicSize = 5f;
                }
                
                Debug.Log($"Camera setup: orthographic={cam.orthographic}, z={cam.transform.position.z}, size={cam.orthographicSize}");
            }
            else
            {
                Debug.LogWarning("No Main Camera found!");
            }
        }
        
        private void Start()
        {
            // Проверяем настройку камеры
            EnsureCamera2DSetup();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Обновляем цвет в редакторе
            if (mainSprite != null)
            {
                mainSprite.color = playerColor;
            }
        }
        
        // Рисуем Gizmos в Scene View (видно даже без Play)
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            // Рисуем круг игрока
            Gizmos.color = playerColor;
            Gizmos.DrawSphere(transform.position, size * 0.5f);
            
            // Рисуем направление
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, Vector3.right * size * 0.5f);
        }
        
        // Рисуем Gizmos при выделении (крупнее и с подписью)
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, size * 0.6f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * size, "Player");
            #endif
        }
#endif
    }
}

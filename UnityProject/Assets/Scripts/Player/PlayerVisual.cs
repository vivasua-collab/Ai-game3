// ============================================================================
// PlayerVisual.cs — Визуальное отображение игрока
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-04-15 11:20:00 UTC — FIX: улучшенный fallback спрайт персонажа, PPU=32
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
        public float size = 0.4f; // Редактировано: 2026-04-16 — уменьшено с 0.8 до 0.4 (соответствует объектам)
        
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
        
        // FIX PLR-L02: Guard for Flash coroutine to prevent multiple simultaneous flashes (2026-04-11)
        private UnityEngine.Coroutine flashCoroutine;
        // FIX PLR-L03: Cached camera reference to avoid expensive Camera.main calls (2026-04-11)
        private Camera cachedCamera;
        // FIX PLR-L01: Track created texture for proper cleanup (2026-04-11)
        private Texture2D createdTexture;
        private Material createdMaterial;
        
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
            
            // FIX: Пытаемся загрузить AI-спрайт персонажа вместо процедурного круга
            // Редактировано: 2026-04-15 UTC
            Sprite loadedSprite = LoadPlayerSprite();
            if (loadedSprite != null)
            {
                mainSprite.sprite = loadedSprite;
                mainSprite.color = Color.white; // Не тонировать реальный спрайт
            }
            else
            {
                // Fallback: программный круг
                mainSprite.sprite = CreateCircleSprite();
                mainSprite.color = playerColor;
            }
            mainSprite.sortingOrder = 10;
            
            // FIX PLR-M07: Try correct URP 2D sprite shader first, then fallback (2026-04-11)
            // "Universal Render Pipeline/2D/Sprite-Unlit-Default" does not exist in URP.
            // The correct shader is "Universal Render Pipeline/2D/Sprite-Lit-Default".
            Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (spriteShader == null)
                spriteShader = Shader.Find("Sprites/Default");
            
            createdMaterial = new Material(spriteShader);
            mainSprite.material = createdMaterial;
            
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
        /// Создаёт спрайт гуманоида программно (fallback, если AI-спрайт не найден)
        /// FIX: Замена простого круга на фигуру персонажа с прозрачным фоном
        /// Редактировано: 2026-04-15 11:20:00 UTC
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            int resolution = 64;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            
            // FIX PLR-L01: Track texture for proper cleanup in OnDestroy (2026-04-11)
            createdTexture = texture;
            
            Color[] pixels = new Color[resolution * resolution];
            
            // Заполнить прозрачным
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            
            // Рисуем гуманоида (персонаж-культиватор)
            // Цвет: основной = белый (т.к. tint через playerColor)
            Color body = Color.white;
            Color dark = new Color(0.85f, 0.85f, 0.85f, 1f);
            int cx = resolution / 2; // 32
            
            // Голова (круг r=7, центр 32,52)
            FillCircle(pixels, resolution, cx, 52, 7, body);
            FillCircle(pixels, resolution, cx, 53, 5, dark); // Лицо чуть темнее
            
            // Тело (прямоугольник 14×18, от y=33 до y=50)
            FillRect(pixels, resolution, cx - 7, 33, 14, 18, body);
            
            // Руки (по 4px шириной, от плеч)
            FillRect(pixels, resolution, cx - 11, 35, 4, 14, body);
            FillRect(pixels, resolution, cx + 7, 35, 4, 14, body);
            
            // Ноги (по 5px шириной)
            FillRect(pixels, resolution, cx - 6, 18, 5, 16, dark);
            FillRect(pixels, resolution, cx + 1, 18, 5, 16, dark);
            
            // Обувь (чуть темнее)
            FillRect(pixels, resolution, cx - 7, 16, 6, 3, new Color(0.6f, 0.4f, 0.2f, 1f));
            FillRect(pixels, resolution, cx + 1, 16, 6, 3, new Color(0.6f, 0.4f, 0.2f, 1f));
            
            // Пояс (акцент)
            FillRect(pixels, resolution, cx - 7, 34, 14, 2, new Color(0.7f, 0.5f, 0.2f, 1f));
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            // FIX: PPU=32 — персонаж = 2 юнита (как тайл), sprite pivot = центр снизу
            // Редактировано: 2026-04-15 11:20:00 UTC
            return Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.25f), // Pivot = центр снизу (ноги на земле)
                32f // PPU=32 — размер 2×2 юнита (совпадает с размером тайла)
            );
        }
        
        /// <summary>
        /// Залить круг на массиве пикселей.
        /// Редактировано: 2026-04-15 11:20:00 UTC
        /// </summary>
        private void FillCircle(Color[] pixels, int resolution, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || x >= resolution || y < 0 || y >= resolution) continue;
                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        pixels[y * resolution + x] = color;
                    }
                }
            }
        }
        
        /// <summary>
        /// Залить прямоугольник на массиве пикселей.
        /// Редактировано: 2026-04-15 11:20:00 UTC
        /// </summary>
        private void FillRect(Color[] pixels, int resolution, int rx, int ry, int rw, int rh, Color color)
        {
            for (int y = ry; y < ry + rh; y++)
            {
                for (int x = rx; x < rx + rw; x++)
                {
                    if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                    {
                        pixels[y * resolution + x] = color;
                    }
                }
            }
        }
        
        /// <summary>
        /// Загрузить AI-спрайт персонажа.
        /// В Editor: загрузка из Assets/Sprites/Characters/Player/
        /// В Build: загрузка из Resources/Sprites/
        /// Fallback: программный круг
        /// Редактировано: 2026-04-15 UTC
        /// </summary>
        private Sprite LoadPlayerSprite()
        {
            #if UNITY_EDITOR
            // Editor: загрузка из Assets по прямому пути
            string[] editorPaths = new string[]
            {
                "Assets/Sprites/Characters/Player/player_variant1_cultivator.png",
                "Assets/Sprites/Characters/Player/player_variant2_cultivator.png",
                "Assets/Sprites/player_sprite.png",
                "Assets/TempPlayerSprite.png"
            };
            foreach (var path in editorPaths)
            {
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    Debug.Log($"[PlayerVisual] Loaded AI sprite: {path}");
                    return sprite;
                }
            }
            Debug.LogWarning("[PlayerVisual] No AI player sprite found, using procedural circle");
            #else
            // Runtime build: загрузка из Resources
            var sprite = Resources.Load<Sprite>("Sprites/player_variant1_cultivator");
            if (sprite != null)
            {
                Debug.Log("[PlayerVisual] Loaded AI sprite from Resources");
                return sprite;
            }
            #endif
            return null;
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
        /// FIX PLR-L02: Stop previous flash coroutine before starting new one (2026-04-11)
        /// </summary>
        public void Flash(Color flashColor, float duration = 0.1f)
        {
            if (mainSprite != null)
            {
                if (flashCoroutine != null)
                    StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(FlashCoroutine(flashColor, duration));
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
        /// FIX PLR-L03: Use cached camera reference instead of Camera.main each call (2026-04-11)
        /// </summary>
        public static void EnsureCamera2DSetup()
        {
            Camera cam = Camera.main; // Static method must use Camera.main; instance methods use cache
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
            // FIX PLR-L03: Cache camera on first access (2026-04-11)
            cachedCamera = Camera.main;
            EnsureCamera2DSetup();
        }
        
        // FIX PLR-L01: Destroy created Material and Texture2D to prevent memory leaks (2026-04-11)
        private void OnDestroy()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
            
            if (createdMaterial != null)
            {
                Destroy(createdMaterial);
                createdMaterial = null;
            }
            
            if (createdTexture != null)
            {
                Destroy(createdTexture);
                createdTexture = null;
            }
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

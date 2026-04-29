// ============================================================================
// PlayerVisual.cs — Визуальное отображение игрока
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-04-17 12:38 UTC — FIX-SORT: EnsureCorrectSortingLayer() теперь проверяет ПОРЯДОК слоёв + fallback на Y-сортировку
// ============================================================================

using UnityEngine;
using CultivationGame.Core;

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
        // FIX 2C: Красно-оранжевый — контрастный на любом фоне
        // Редактировано: 2026-04-15 17:31:49 UTC
        public Color playerColor = new Color(1f, 0.3f, 0.15f);
        
        [Tooltip("Размер игрока")]
        // FIX: size=1.0 — при PPU=64, 128/64=2.0 юнита × 1.0 = 2.0 юнита (1 тайл)
        // Редактировано: 2026-04-15 17:31:49 UTC
        public float size = 1.0f;
        
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
            
            // FIX 1A: Загружаем обработанный AI-спрайт (128×128 RGBA, без белого фона)
            // Редактировано: 2026-04-15 17:31:49 UTC
            Sprite loadedSprite = LoadPlayerSprite();
            if (loadedSprite != null)
            {
                mainSprite.sprite = loadedSprite;
                mainSprite.color = Color.white; // Не тонировать AI-спрайт (у него свой цвет)
                Debug.Log($"[PlayerVisual] AI-спрайт загружен: size={loadedSprite.bounds.size}, PPU={loadedSprite.pixelsPerUnit}");
            }
            else
            {
                // Fallback: программный круг
                mainSprite.sprite = CreateCircleSprite();
                mainSprite.color = playerColor;
            }
            // FIX-V2-5: Sorting layer "Player" — игрок на собственном слое, выше всех объектов.
            // При sortingLayerName="Objects" (когда слой не существовал) Unity 6+ игнорировал →
            // спрайт НЕ рендерился. Теперь Sorting Layer "Player" создаётся в FullSceneBuilder.
            // Редактировано: 2026-04-16 11:37 UTC
            mainSprite.sortingLayerName = "Player";
            mainSprite.sortingOrder = 0;

            // FIX 2A-alt: Сначала пробуем Unlit шейдер — рендерит БЕЗ Light2D (гарантированно виден).
            // Sprite-Lit-Default требует Light2D для видимости — без него спрайт чёрный.
            // Sprite-Unlit-Default рендерит без освещения — всегда виден.
            // Редактировано: 2026-04-15 16:53:48 UTC
            Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (spriteShader == null)
                spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
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
                // FIX-V2-5: Тень на слое "Player", ниже основного спрайта (order=-1 < 0)
                // Редактировано: 2026-04-16 11:37 UTC
                shadowSprite.sortingLayerName = "Player";
                shadowSprite.sortingOrder = -1;

                // Тот же материал
                shadowSprite.material = mainSprite.material;
            }
            
            // FIX-V2-6: Диагностика Player Visual
            // Редактировано: 2026-04-16 11:37 UTC
            CultivationGame.Core.RenderPipelineLogger.LogPlayerVisualState(mainSprite, shadowSprite);
            
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
            
            // FIX: PPU=64 — персонаж = 1 юнит, sprite pivot = центр снизу
            // Редактировано: 2026-04-15 17:31:49 UTC
            return Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.25f), // Pivot = центр снизу (ноги на земле)
                64f // PPU=64 — 64/64 = 1.0 юнит × size=1.0 = 1.0 юнита
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
        /// В Editor: загрузка из Assets/Sprites/Characters/Player/ с автонастройкой PPU=64
        /// В Build: загрузка из Resources/Sprites/
        /// Fallback: программный круг
        /// Редактировано: 2026-04-15 17:31:49 UTC
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
                // FIX SPRITE-01: Сначала проверяем, существует ли файл вообще.
                // Затем вызываем EnsurePlayerSpritePPU ВНЕ зависимости от LoadAssetAtPath<Sprite>.
                // Причина: при первом импорте Unity создаёт .meta с textureType=Default,
                // LoadAssetAtPath<Sprite> возвращает null, EnsurePlayerSpritePPU НЕ вызывался —
                // спрайт навсегда оставался «неспрайтом».
                // Редактировано: 2026-05-02
                if (!System.IO.File.Exists(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), path)))
                    continue;

                // Всегда пытаемся исправить настройки импорта
                EnsurePlayerSpritePPU(path);

                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    Debug.Log($"[PlayerVisual] AI-спрайт загружен: {path}, PPU={sprite.pixelsPerUnit}, bounds={sprite.bounds.size}");
                    return sprite;
                }
            }
            Debug.LogWarning("[PlayerVisual] AI-спрайт не найден, используется программный fallback");
            #else
            // Runtime build: загрузка из Resources
            var sprite = Resources.Load<Sprite>("Sprites/player_variant1_cultivator");
            if (sprite != null)
            {
                Debug.Log("[PlayerVisual] AI-спрайт загружен из Resources");
                return sprite;
            }
            #endif
            return null;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Убедиться, что спрайт персонажа импортирован с PPU=64 и alphaIsTransparency=true.
        /// При первом запуске Unity может использовать дефолтный PPU=100 и отсутствующий alpha.
        /// FIX: Принудительный реимпорт ВСЕГДА (не только при PPU!=64) — гарантирует
        /// правильные настройки даже если только alphaIsTransparency был сброшен.
        /// Редактировано: 2026-04-15 17:51:32 UTC
        /// </summary>
        private void EnsurePlayerSpritePPU(string assetPath)
        {
            var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
            if (importer == null) return;

            // FIX: Проверяем ВСЕ настройки, не только PPU.
            // alphaIsTransparency критичен для RGBA спрайтов — без него белый фон!
            // Редактировано: 2026-04-15 17:51:32 UTC
            bool needsReimport = importer.spritePixelsPerUnit != 64
                || importer.alphaIsTransparency != true
                || importer.textureType != UnityEditor.TextureImporterType.Sprite
                || importer.spriteImportMode != UnityEditor.SpriteImportMode.Single;

            if (needsReimport)
            {
                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Bilinear;
                // FIX: alphaIsTransparency = true — КРИТИЧНО для RGBA спрайтов!
                // Без этого Unity игнорирует альфа-канал → белый фон.
                // Редактировано: 2026-04-15 17:51:32 UTC
                importer.alphaIsTransparency = true;
                importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate | UnityEditor.ImportAssetOptions.DontDownloadFromCacheServer);
                // FIX: Refresh + задержка для корректной перезагрузки спрайта
                // Редактировано: 2026-04-15 17:51:32 UTC
                UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate);
                Debug.Log($"[PlayerVisual] Спрайт реимпортирован: {assetPath} → PPU=64, alphaIsTransparency=true");
            }
        }
        #endif
        
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

            // FIX-SORT-RECHECK: Повторная проверка и установка Sorting Layer.
            // Awake() вызывает CreateVisual(), которая устанавливает sortingLayerName="Player".
            // Но если PlayerVisual.Awake() выполнился ДО TileMapController.Awake(),
            // слой "Player" ещё не существовал → Unity 6+ молча игнорирует невалидное имя →
            // спрайт остаётся на "Default" (id=0), который НИЖЕ "Terrain" (id=2) и "Objects" (id=3).
            // Start() выполняется ПОСЛЕ всех Awake() → сортировочные слои уже созданы.
            // Редактировано: 2026-04-18 UTC
            EnsureCorrectSortingLayer();

            // FIX-V2-7: Runtime диагностика Camera и Light.
            // Без L7/L8 логов невозможно понять, правильно ли настроены камера и свет.
            RenderPipelineLogger.LogCameraState();
            RenderPipelineLogger.LogLightState();
        }

        /// <summary>
        /// FIX-SORT: Гарантировать, что спрайт игрока рендерится ПОВЕРХ terrain и objects.
        /// Проверяет: (1) существование слоя "Player", (2) ПОРЯДОК слоёв (Player > Objects > Terrain),
        /// (3) fallback на "Objects" с высоким sortingOrder если Player-слой отсутствует.
        /// Корневая причина бага: если слой "Player" не существует в момент Awake(),
        /// или если порядок слоёв неправильный (Player ниже Terrain),
        /// игрок рендерится ПОЗАДИ terrain → спрайт поверхности поверх персонажа.
        /// Редактировано: 2026-04-17 12:38 UTC
        /// </summary>
        private void EnsureCorrectSortingLayer()
        {
            if (mainSprite != null)
            {
                string currentLayer = mainSprite.sortingLayerName;
                int currentLayerId = mainSprite.sortingLayerID;

                // Проверяем, существует ли слой "Player"
                bool playerLayerExists = false;
                int playerLayerIndex = -1;
                int terrainLayerIndex = -1;
                int objectsLayerIndex = -1;
                var layers = SortingLayer.layers;

                for (int i = 0; i < layers.Length; i++)
                {
                    if (layers[i].name == "Player") { playerLayerExists = true; playerLayerIndex = i; }
                    if (layers[i].name == "Terrain") terrainLayerIndex = i;
                    if (layers[i].name == "Objects") objectsLayerIndex = i;
                }

                // FIX-SORT: Проверяем ПОРЯДОК слоёв — Player должен быть ВЫШЕ Terrain и Objects
                // Если порядок неправильный, используем fallback на "Objects" с высоким order
                bool playerLayerOrderCorrect = playerLayerExists &&
                    playerLayerIndex > terrainLayerIndex &&
                    playerLayerIndex > objectsLayerIndex;

                if (playerLayerExists && playerLayerOrderCorrect)
                {
                    // Слой существует И в правильном порядке — устанавливаем принудительно
                    mainSprite.sortingLayerName = "Player";
                    mainSprite.sortingOrder = 0;
                    Debug.Log($"[PlayerVisual] FIX-SORT: mainSprite → layer=\"Player\" " +
                        $"(было: \"{currentLayer}\" id={currentLayerId}) → сейчас: id={mainSprite.sortingLayerID}");
                }
                else if (playerLayerExists && !playerLayerOrderCorrect)
                {
                    // Слой существует, но в НЕПРАВИЛЬНОМ порядке (Player ниже Terrain/Objects)
                    // Это критический баг — Terrain рендерится поверх игрока!
                    Debug.LogError($"[PlayerVisual] FIX-SORT: Слой \"Player\" (индекс={playerLayerIndex}) " +
                        $"НИЖЕ чем Terrain({terrainLayerIndex}) или Objects({objectsLayerIndex})! " +
                        $"Terrain будет рендериться ПОВЕРХ игрока! Fallback на \"Objects\" с order=100");
                    mainSprite.sortingLayerName = "Objects";
                    mainSprite.sortingOrder = 100; // Выше деревьев(5), камней(3), ресурсов(5)
                }
                else
                {
                    // Слой НЕ существует — fallback на "Objects" с высоким sortingOrder
                    Debug.LogWarning($"[PlayerVisual] FIX-SORT: Слой \"Player\" НЕ найден! " +
                        $"Fallback: sortingLayerName=\"Objects\", sortingOrder=100");
                    mainSprite.sortingLayerName = "Objects";
                    mainSprite.sortingOrder = 100;
                }

                // Также проверяем тень
                if (shadowSprite != null)
                {
                    if (playerLayerExists && playerLayerOrderCorrect)
                    {
                        shadowSprite.sortingLayerName = "Player";
                        shadowSprite.sortingOrder = -1;
                    }
                    else
                    {
                        shadowSprite.sortingLayerName = "Objects";
                        shadowSprite.sortingOrder = 99;
                    }
                }

                // Логируем итоговое состояние
                Debug.Log($"[PlayerVisual] Sorting: main=\"{mainSprite.sortingLayerName}\"(id={mainSprite.sortingLayerID}) " +
                    $"order={mainSprite.sortingOrder}, " +
                    $"shadow=\"{(shadowSprite != null ? shadowSprite.sortingLayerName : "null")}\" " +
                    $"order={shadowSprite?.sortingOrder ?? 0}");

                // Логируем порядок слоёв для диагностики
                Debug.Log($"[PlayerVisual] Sorting Layers: Terrain={terrainLayerIndex}, " +
                    $"Objects={objectsLayerIndex}, Player={playerLayerIndex} " +
                    $"(правильный порядок: Terrain < Objects < Player)");
            }
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

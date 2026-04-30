// ============================================================================
// NPCVisual.cs — Визуальное отображение NPC
// Cultivation World Simulator
// Создано: 2026-04-30 07:41:00 UTC
// ============================================================================
//
// Создаёт визуал NPC: спрайт по роли, имя + HP-бар.
// Шаблон: PlayerVisual.cs
//
// Компоненты-дочерние объекты:
//   Visual (SpriteRenderer) — спрайт NPC
//   NameLabel (Canvas + TextMeshPro) — имя + уровень культивации
//   HealthBar (Canvas + Slider) — полоска HP
// ============================================================================

using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Generators;

namespace CultivationGame.NPC
{
    /// <summary>
    /// Визуальное отображение NPC.
    /// Создаёт спрайт по роли, плавающее имя и HP-бар.
    /// </summary>
    [RequireComponent(typeof(NPCController))]
    public class NPCVisual : MonoBehaviour
    {
        [Header("Visual Settings")]
        [Tooltip("Размер NPC")]
        public float size = 1.0f;

        [Header("Name Label")]
        [Tooltip("Показывать имя")]
        public bool showName = true;
        [Tooltip("Высота имени над NPC")]
        public float nameHeight = 1.2f;
        [Tooltip("Размер шрифта имени")]
        public int nameFontSize = 24;

        [Header("Health Bar")]
        [Tooltip("Показывать HP-бар")]
        public bool showHealthBar = true;
        [Tooltip("Высота HP-бара над NPC")]
        public float healthBarHeight = 0.8f;
        [Tooltip("Ширина HP-бара")]
        public float healthBarWidth = 1.5f;
        [Tooltip("Высота HP-бара")]
        public float healthBarHeightSize = 0.15f;

        // === Runtime references ===
        private NPCController npcController;
        private SpriteRenderer mainSprite;
        private GameObject visualObj;
        private GameObject nameLabelObj;
        private GameObject healthBarObj;
        private UnityEngine.UI.Slider healthSlider;
        private TMPro.TextMeshPro nameLabel;

        // === Resources ===
        private Texture2D createdTexture;
        private Material createdMaterial;

        // === Маппинг роль → спрайт ===
        private static readonly System.Collections.Generic.Dictionary<NPCRole, string[]> RoleSpritePaths =
            new System.Collections.Generic.Dictionary<NPCRole, string[]>
        {
            { NPCRole.Monster,    new[] { "Assets/Sprites/Characters/NPC/npc_rogue.png", "Assets/Sprites/Characters/NPC/npc_beast_cultivator.png" } },
            { NPCRole.Guard,      new[] { "Assets/Sprites/Characters/NPC/npc_guard.png" } },
            { NPCRole.Merchant,   new[] { "Assets/Sprites/Characters/NPC/npc_merchant.png" } },
            { NPCRole.Cultivator, new[] { "Assets/Sprites/Characters/NPC/npc_disciple_male.png", "Assets/Sprites/Characters/NPC/npc_disciple_female.png" } },
            { NPCRole.Elder,      new[] { "Assets/Sprites/Characters/NPC/npc_village_elder.png", "Assets/Sprites/Characters/NPC/npc_elder_master.png" } },
            { NPCRole.Enemy,      new[] { "Assets/Sprites/Characters/NPC/npc_enemy_demonic.png", "Assets/Sprites/Characters/NPC/npc_rival.png" } },
            { NPCRole.Disciple,   new[] { "Assets/Sprites/Characters/NPC/npc_disciple_male.png" } },
            { NPCRole.Passerby,   new[] { "Assets/Sprites/Characters/NPC/npc_villager_male.png" } }
        };

        // === Цвета по Attitude ===
        private static readonly Color ColorHostile = new Color(1f, 0.2f, 0.2f);
        private static readonly Color ColorUnfriendly = new Color(1f, 0.5f, 0.2f);
        private static readonly Color ColorNeutral = Color.white;
        private static readonly Color ColorFriendly = new Color(0.3f, 1f, 0.3f);
        private static readonly Color ColorAllied = new Color(0.2f, 0.8f, 1f);
        private static readonly Color ColorHatred = new Color(1f, 0f, 0f);
        private static readonly Color ColorSwornAlly = new Color(1f, 0.85f, 0f);

        private void Awake()
        {
            npcController = GetComponent<NPCController>();
            CreateVisual();
        }

        private void Start()
        {
            // Обновляем визуал после инициализации NPCController
            UpdateVisualFromState();
        }

        private void LateUpdate()
        {
            // Обновляем HP-бар каждый кадр (если виден)
            if (showHealthBar && healthSlider != null && npcController != null)
            {
                var state = npcController.State;
                if (state != null && state.MaxHealth > 0)
                {
                    float ratio = (float)state.CurrentHealth / state.MaxHealth;
                    healthSlider.value = ratio;
                }
            }

            // Имя всегда смотрит на камеру (billboard)
            if (nameLabelObj != null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    nameLabelObj.transform.rotation = cam.transform.rotation;
                }
            }
            if (healthBarObj != null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    healthBarObj.transform.rotation = cam.transform.rotation;
                }
            }
        }

        // === Создание визуала ===

        private void CreateVisual()
        {
            // Убеждаемся, что NPC на Z = 0
            Vector3 pos = transform.position;
            pos.z = 0f;
            transform.position = pos;

            // 1. Визуальный объект (спрайт)
            visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(transform);
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localRotation = Quaternion.identity;
            visualObj.transform.localScale = new Vector3(size, size, 1f);

            mainSprite = visualObj.AddComponent<SpriteRenderer>();

            // Пробуем загрузить спрайт по роли (пока неизвестна — fallback)
            Sprite loadedSprite = LoadNPCSprite(NPCRole.Passerby);
            if (loadedSprite != null)
            {
                mainSprite.sprite = loadedSprite;
                mainSprite.color = Color.white;
            }
            else
            {
                mainSprite.sprite = CreateHumanoidSprite();
                mainSprite.color = Color.gray;
            }

            // Sorting Layer: Objects, order=50 (выше terrain, ниже Player)
            mainSprite.sortingLayerName = "Objects";
            mainSprite.sortingOrder = 50;

            // Unlit шейдер — виден без Light2D
            Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (spriteShader == null)
                spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (spriteShader == null)
                spriteShader = Shader.Find("Sprites/Default");

            createdMaterial = new Material(spriteShader);
            mainSprite.material = createdMaterial;

            // 2. Name Label
            if (showName)
            {
                CreateNameLabel();
            }

            // 3. Health Bar
            if (showHealthBar)
            {
                CreateHealthBar();
            }

            Debug.Log("[NPCVisual] Визуал NPC создан");
        }

        /// <summary>
        /// Создать плавающее имя над NPC.
        /// </summary>
        private void CreateNameLabel()
        {
            nameLabelObj = new GameObject("NameLabel");
            nameLabelObj.transform.SetParent(transform);
            nameLabelObj.transform.localPosition = new Vector3(0f, nameHeight, 0f);
            nameLabelObj.transform.localRotation = Quaternion.identity;
            nameLabelObj.transform.localScale = Vector3.one;

            var canvas = nameLabelObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 100;

            var rectTransform = nameLabelObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(3f, 0.5f);

            // CanvasScaler для постоянного размера
            var scaler = nameLabelObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;

            // TextMeshPro
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(nameLabelObj.transform, false);
            nameLabel = textObj.AddComponent<TMPro.TextMeshPro>();
            nameLabel.text = "NPC";
            nameLabel.fontSize = nameFontSize;
            nameLabel.alignment = TMPro.TextAlignmentOptions.Center;
            nameLabel.color = Color.white;
            nameLabel.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            nameLabel.enableWordWrapping = false;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Создать HP-бар над NPC.
        /// </summary>
        private void CreateHealthBar()
        {
            healthBarObj = new GameObject("HealthBar");
            healthBarObj.transform.SetParent(transform);
            healthBarObj.transform.localPosition = new Vector3(0f, healthBarHeight, 0f);
            healthBarObj.transform.localRotation = Quaternion.identity;
            healthBarObj.transform.localScale = Vector3.one;

            var canvas = healthBarObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 99;

            var rectTransform = healthBarObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(healthBarWidth, healthBarHeightSize);

            var scaler = healthBarObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;

            // Background (красная подложка)
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(healthBarObj.transform, false);
            var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.3f, 0f, 0f, 0.8f);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Fill area (зелёная полоска)
            var fillAreaObj = new GameObject("Fill");
            fillAreaObj.transform.SetParent(healthBarObj.transform, false);
            var fillImage = fillAreaObj.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0f, 0.8f, 0f, 0.9f);
            var fillRect = fillAreaObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // Slider
            healthSlider = healthBarObj.AddComponent<UnityEngine.UI.Slider>();
            healthSlider.interactable = false;
            healthSlider.transition = UnityEngine.UI.Selectable.Transition.None;
            healthSlider.fillRect = fillRect;
            healthSlider.targetGraphic = fillImage;
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
            healthSlider.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        }

        // === Публичные методы ===

        /// <summary>
        /// Установить спрайт по роли NPC.
        /// </summary>
        public void SetSpriteByRole(NPCRole role)
        {
            if (mainSprite == null) return;

            Sprite loaded = LoadNPCSprite(role);
            if (loaded != null)
            {
                mainSprite.sprite = loaded;
                mainSprite.color = Color.white;
            }
            else
            {
                // Fallback — программный гуманоид с цветом по роли
                mainSprite.sprite = CreateHumanoidSprite();
                mainSprite.color = RoleToFallbackColor(role);
            }
        }

        /// <summary>
        /// Установить цвет имени по Attitude.
        /// </summary>
        public void SetAttitudeColor(Attitude attitude)
        {
            if (nameLabel == null) return;
            nameLabel.color = AttitudeToColor(attitude);
        }

        /// <summary>
        /// Обновить HP-бар.
        /// </summary>
        public void UpdateHealthBar(int current, int max)
        {
            if (healthSlider == null) return;
            healthSlider.value = max > 0 ? (float)current / max : 0f;
        }

        /// <summary>
        /// Установить визуальное состояние AI.
        /// </summary>
        public void SetAIState(NPCAIState state)
        {
            // Визуальная индикация AI-состояния через tint спрайта
            if (mainSprite == null) return;

            // Лёгкий tint для индикации состояния
            Color tint = state switch
            {
                NPCAIState.Idle => Color.white,
                NPCAIState.Wandering => new Color(0.9f, 0.95f, 1f),
                NPCAIState.Patrolling => new Color(0.85f, 0.9f, 1f),
                NPCAIState.Attacking => new Color(1f, 0.8f, 0.8f),
                NPCAIState.Fleeing => new Color(1f, 1f, 0.7f),
                NPCAIState.Cultivating => new Color(0.8f, 0.85f, 1f),
                NPCAIState.Resting => new Color(0.85f, 1f, 0.85f),
                NPCAIState.Trading => new Color(1f, 0.95f, 0.8f),
                NPCAIState.Talking => new Color(0.9f, 0.95f, 1f),
                _ => Color.white
            };

            // Применяем только если нет загруженного спрайта (иначе белые спрайты тонировать не нужно)
            if (mainSprite.sprite != null && mainSprite.color == Color.white)
            {
                // Не перекрываем цвет загруженного спрайта
            }
        }

        /// <summary>
        /// Полное обновление визуала из состояния NPCController.
        /// </summary>
        public void UpdateVisualFromState()
        {
            if (npcController == null) return;

            var state = npcController.State;
            if (state == null) return;

            // Имя + уровень культивации
            if (nameLabel != null)
            {
                string levelStr = state.CultivationLevel != CultivationLevel.None
                    ? $" L{(int)state.CultivationLevel}"
                    : "";
                nameLabel.text = $"{state.Name}{levelStr}";
            }

            // Цвет имени по Attitude
            SetAttitudeColor(state.Attitude);

            // HP-бар
            UpdateHealthBar(state.CurrentHealth, state.MaxHealth);
        }

        // === Спрайт загрузка ===

        /// <summary>
        /// Загрузить спрайт NPC по роли.
        /// Editor: из Assets/Sprites/Characters/NPC/
        /// Runtime: из Resources/Sprites/NPC/
        /// Fallback: программный гуманоид
        /// </summary>
        private Sprite LoadNPCSprite(NPCRole role)
        {
#if UNITY_EDITOR
            if (RoleSpritePaths.TryGetValue(role, out var paths))
            {
                foreach (var path in paths)
                {
                    // Проверяем существование файла
                    if (!System.IO.File.Exists(System.IO.Path.Combine(
                        System.IO.Directory.GetCurrentDirectory(), path)))
                        continue;

                    // Пробуем настроить PPU
                    EnsureSpritePPU(path);

                    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                    {
                        Debug.Log($"[NPCVisual] Спрайт загружен: {path}");
                        return sprite;
                    }
                }
            }
#else
            // Runtime build: загрузка из Resources
            string resourceName = $"Sprites/NPC/{role.ToString().ToLower()}";
            var sprite = Resources.Load<Sprite>(resourceName);
            if (sprite != null) return sprite;
#endif
            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Убедиться, что спрайт импортирован с PPU=64 и sprite-настройками.
        /// </summary>
        private void EnsureSpritePPU(string assetPath)
        {
            var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
            if (importer == null) return;

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
                importer.alphaIsTransparency = true;
                importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                UnityEditor.AssetDatabase.ImportAsset(assetPath,
                    UnityEditor.ImportAssetOptions.ForceUpdate |
                    UnityEditor.ImportAssetOptions.DontDownloadFromCacheServer);
                UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate);
            }
        }
#endif

        /// <summary>
        /// Создать программный гуманоид (fallback).
        /// Аналог PlayerVisual.CreateCircleSprite()
        /// </summary>
        private Sprite CreateHumanoidSprite()
        {
            int resolution = 64;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            createdTexture = texture;

            Color[] pixels = new Color[resolution * resolution];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Рисуем гуманоида
            Color body = Color.white;
            Color dark = new Color(0.85f, 0.85f, 0.85f, 1f);
            int cx = resolution / 2;

            // Голова
            FillCircle(pixels, resolution, cx, 52, 7, body);
            FillCircle(pixels, resolution, cx, 53, 5, dark);

            // Тело
            FillRect(pixels, resolution, cx - 7, 33, 14, 18, body);

            // Руки
            FillRect(pixels, resolution, cx - 11, 35, 4, 14, body);
            FillRect(pixels, resolution, cx + 7, 35, 4, 14, body);

            // Ноги
            FillRect(pixels, resolution, cx - 6, 18, 5, 16, dark);
            FillRect(pixels, resolution, cx + 1, 18, 5, 16, dark);

            // Обувь
            FillRect(pixels, resolution, cx - 7, 16, 6, 3, new Color(0.6f, 0.4f, 0.2f, 1f));
            FillRect(pixels, resolution, cx + 1, 16, 6, 3, new Color(0.6f, 0.4f, 0.2f, 1f));

            // Пояс
            FillRect(pixels, resolution, cx - 7, 34, 14, 2, new Color(0.7f, 0.5f, 0.2f, 1f));

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.25f),
                64f
            );
        }

        // === Утилиты рисования ===

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
                        pixels[y * resolution + x] = color;
                }
            }
        }

        private void FillRect(Color[] pixels, int resolution, int rx, int ry, int rw, int rh, Color color)
        {
            for (int y = ry; y < ry + rh; y++)
            {
                for (int x = rx; x < rx + rw; x++)
                {
                    if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                        pixels[y * resolution + x] = color;
                }
            }
        }

        // === Конвертеры цветов ===

        private static Color AttitudeToColor(Attitude attitude)
        {
            return attitude switch
            {
                Attitude.Hatred => ColorHatred,
                Attitude.Hostile => ColorHostile,
                Attitude.Unfriendly => ColorUnfriendly,
                Attitude.Friendly => ColorFriendly,
                Attitude.Allied => ColorAllied,
                Attitude.SwornAlly => ColorSwornAlly,
                _ => ColorNeutral
            };
        }

        private static Color RoleToFallbackColor(NPCRole role)
        {
            return role switch
            {
                NPCRole.Monster => new Color(0.8f, 0.2f, 0.2f),
                NPCRole.Guard => new Color(0.2f, 0.4f, 0.8f),
                NPCRole.Merchant => new Color(0.9f, 0.7f, 0.2f),
                NPCRole.Cultivator => new Color(0.5f, 0.2f, 0.8f),
                NPCRole.Elder => new Color(0.8f, 0.6f, 0.2f),
                NPCRole.Enemy => new Color(1f, 0.1f, 0.1f),
                NPCRole.Disciple => new Color(0.3f, 0.6f, 0.8f),
                NPCRole.Passerby => new Color(0.6f, 0.6f, 0.6f),
                _ => Color.gray
            };
        }

        // === Cleanup ===

        private void OnDestroy()
        {
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
        private void OnDrawGizmos()
        {
            Gizmos.color = RoleToFallbackColor(NPCRole.Passerby);
            Gizmos.DrawSphere(transform.position, size * 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, size * 0.6f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * size, "NPC");
        }
#endif
    }
}

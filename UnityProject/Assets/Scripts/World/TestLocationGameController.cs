// ============================================================================
// TestLocationGameController.cs — Контроллер тестовой локации
// Cultivation World Simulator
// Создано: 2026-04-08 06:17:46 UTC
// Редактировано: 2026-04-11 14:50:00 UTC — FIX CS1503: FindObjectsByType — добавлен FindObjectsSortMode
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.TileSystem;
using CultivationGame.Player;
using CultivationGame.Core;
using CultivationGame.Qi;
using CultivationGame.Body;
using CultivationGame.Inventory;
using CultivationGame.Combat;
using CultivationGame.Interaction;

namespace CultivationGame.World
{
    /// <summary>
    /// Контроллер тестовой локации.
    /// Управляет спавном игрока, связью с тайловой системой и UI.
    /// </summary>
    public class TestLocationGameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TileMapController tileMapController;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private GameObject playerPrefab;
        
        [Header("UI References")]
        [SerializeField] private UnityEngine.UI.Slider healthBar;
        [SerializeField] private UnityEngine.UI.Slider qiBar;
        [SerializeField] private UnityEngine.UI.Slider staminaBar;
        [SerializeField] private TMPro.TMP_Text healthText;
        [SerializeField] private TMPro.TMP_Text qiText;
        [SerializeField] private TMPro.TMP_Text locationText;
        [SerializeField] private TMPro.TMP_Text positionText;
        
        [Header("Settings")]
        [SerializeField] private bool spawnPlayerOnStart = true;
        [SerializeField] private bool showDebugInfo = true;
        
        // === Runtime ===
        private GameObject spawnedPlayer;
        private PlayerController playerController;
        private QiController qiController;
        private BodyController bodyController;
        
        // === Properties ===
        public GameObject SpawnedPlayer => spawnedPlayer;
        public PlayerController PlayerController => playerController;
        
        // === Events ===
        public event Action<GameObject> OnPlayerSpawned;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            // Найти TileMapController если не назначен
            if (tileMapController == null)
                tileMapController = FindFirstObjectByType<TileMapController>();
            
            // Автоматически найти UI элементы если не назначены
            AutoFindUIElements();
        }
        
        private void AutoFindUIElements()
        {
            // Найти UI элементы по именам
            var allSliders = FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None); // FIX CS1503 (2026-04-11)
            var allTexts = FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None); // FIX CS1503 (2026-04-11)
            
            foreach (var slider in allSliders)
            {
                if (slider.name == "HealthBar" && healthBar == null)
                    healthBar = slider;
                else if (slider.name == "QiBar" && qiBar == null)
                    qiBar = slider;
                else if (slider.name == "StaminaBar" && staminaBar == null)
                    staminaBar = slider;
            }
            
            foreach (var text in allTexts)
            {
                if (text.name == "HealthText" && healthText == null)
                    healthText = text;
                else if (text.name == "QiText" && qiText == null)
                    qiText = text;
                else if (text.name == "LocationText" && locationText == null)
                    locationText = text;
                else if (text.name == "PositionText" && positionText == null)
                    positionText = text;
            }
            
            // Установить начальные значения UI
            if (healthBar != null)
            {
                healthBar.maxValue = 100;
                healthBar.value = 100;
            }
            
            if (qiBar != null)
            {
                qiBar.maxValue = 1000;
                qiBar.value = 1000;
            }
            
            if (staminaBar != null)
            {
                staminaBar.maxValue = 100;
                staminaBar.value = 100;
            }
        }
        
        private void Start()
        {
            // Подписаться на генерацию карты
            if (tileMapController != null)
            {
                tileMapController.OnMapGenerated += OnMapGenerated;
            }
            
            // Спавнить игрока при старте
            if (spawnPlayerOnStart)
            {
                SpawnPlayer();
            }
        }
        
        private void OnDestroy()
        {
            if (tileMapController != null)
            {
                tileMapController.OnMapGenerated -= OnMapGenerated;
            }
            
            // Отписаться от событий игрока
            UnsubscribeFromPlayer();
        }
        
        private void Update()
        {
            UpdateUI();
            UpdateDebugInfo();
        }
        
        // === Player Spawn ===
        
        /// <summary>
        /// Заспавнить игрока.
        /// </summary>
        public GameObject SpawnPlayer()
        {
            // Определить позицию спавна
            Vector3 spawnPosition = GetSpawnPosition();
            
            // Создать игрока
            if (playerPrefab != null)
            {
                spawnedPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                spawnedPlayer.name = "Player";
            }
            else
            {
                // Создать базового игрока
                spawnedPlayer = CreateBasicPlayer(spawnPosition);
            }
            
            // Получить контроллеры
            playerController = spawnedPlayer.GetComponent<PlayerController>();
            qiController = spawnedPlayer.GetComponent<QiController>();
            bodyController = spawnedPlayer.GetComponent<BodyController>();
            
            // Подписаться на события
            SubscribeToPlayer();
            
            // Установить локацию
            if (playerController != null && tileMapController != null)
            {
                playerController.SetLocation(tileMapController.MapData?.mapName ?? "Test Location");
            }
            
            OnPlayerSpawned?.Invoke(spawnedPlayer);
            
            Debug.Log($"[TestLocationGameController] Player spawned at {spawnPosition}");
            
            return spawnedPlayer;
        }
        
        /// <summary>
        /// Получить позицию спавна.
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            // Если есть точка спавна
            if (playerSpawnPoint != null)
            {
                return playerSpawnPoint.position;
            }
            
            // Если есть TileMapController - найти проходимый тайл в центре
            if (tileMapController != null && tileMapController.MapData != null)
            {
                int centerX = tileMapController.Width / 2;
                int centerY = tileMapController.Height / 2;
                
                var tile = tileMapController.GetTile(centerX, centerY);
                if (tile != null && tile.IsPassable())
                {
                    return tile.GetWorldPosition();
                }
                
                // Найти ближайший проходимый тайл
                var passableTile = tileMapController.MapData.FindPassableNearby(centerX, centerY, 10);
                if (passableTile != null)
                {
                    return passableTile.GetWorldPosition();
                }
            }
            
            // Дефолтная позиция
            return new Vector3(30, 20, 0); // Центр карты 30×20 тайлов
        }
        
        /// <summary>
        /// Создать базового игрока без префаба.
        /// </summary>
        private GameObject CreateBasicPlayer(Vector3 position)
        {
            GameObject player = new GameObject("Player");
            player.transform.position = position;
            
            // Rigidbody2D
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            // Collider
            CircleCollider2D col = player.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            
            // Контроллеры
            player.AddComponent<PlayerController>();
            player.AddComponent<BodyController>();
            player.AddComponent<QiController>();
            
            // FIX: Добавлены недостающие компоненты для полной инициализации игрока
            // Без SleepSystem — сон не работает, без InventoryController — инвентарь
            // Редактировано: 2026-04-14 06:13:00 UTC
            player.AddComponent<SleepSystem>();
            player.AddComponent<InventoryController>();
            player.AddComponent<EquipmentController>();
            player.AddComponent<TechniqueController>();
            player.AddComponent<InteractionController>();
            
            // Sprite Renderer (временный визуал)
            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = CreateTempSprite();
            sr.sortingOrder = 10;
            
            return player;
        }
        
        /// <summary>
        /// Создать временный спрайт.
        /// </summary>
        private Sprite CreateTempSprite()
        {
            Texture2D texture = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];
            
            // Простой круг
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    float dx = x - 32;
                    float dy = y - 32;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist < 28)
                    {
                        // Тело - голубой
                        colors[y * 64 + x] = new Color(0.3f, 0.5f, 0.8f);
                    }
                    else if (dist < 32)
                    {
                        // Контур - тёмный
                        colors[y * 64 + x] = new Color(0.2f, 0.3f, 0.5f);
                    }
                    else
                    {
                        colors[y * 64 + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        }
        
        // === Event Subscriptions ===
        
        private void SubscribeToPlayer()
        {
            if (playerController != null)
            {
                playerController.OnHealthChanged += OnHealthChanged;
                playerController.OnQiChanged += OnQiChanged;
            }
        }
        
        private void UnsubscribeFromPlayer()
        {
            if (playerController != null)
            {
                playerController.OnHealthChanged -= OnHealthChanged;
                playerController.OnQiChanged -= OnQiChanged;
            }
        }
        
        // === Event Handlers ===
        
        private void OnMapGenerated(TileMapData mapData)
        {
            Debug.Log($"[TestLocationGameController] Map generated: {mapData.width}x{mapData.height}");
            
            // Обновить позицию спавна если игрок уже есть
            if (spawnedPlayer != null)
            {
                Vector3 newSpawnPos = GetSpawnPosition();
                spawnedPlayer.transform.position = newSpawnPos;
            }
            
            // Обновить UI локации
            if (locationText != null)
            {
                locationText.text = mapData.mapName;
            }
        }
        
        private void OnHealthChanged(int current, int max)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = max;
                healthBar.value = current;
            }
            
            if (healthText != null)
            {
                healthText.text = $"HP: {current}/{max}";
            }
        }
        
        private void OnQiChanged(long current, long max)
        {
            if (qiBar != null)
            {
                qiBar.maxValue = max;
                qiBar.value = current;
            }
            
            if (qiText != null)
            {
                qiText.text = $"Ци: {FormatNumber(current)}/{FormatNumber(max)}";
            }
        }
        
        // === UI Updates ===
        
        private void UpdateUI()
        {
            if (playerController == null) return;
            
            // Выносливость
            if (staminaBar != null)
            {
                staminaBar.maxValue = playerController.State.MaxStamina;
                staminaBar.value = playerController.State.CurrentStamina;
            }
        }
        
        private void UpdateDebugInfo()
        {
            if (!showDebugInfo) return;
            if (positionText == null || spawnedPlayer == null) return;
            
            // Показать позицию в тайлах
            Vector2 worldPos = spawnedPlayer.transform.position;
            if (tileMapController != null && tileMapController.MapData != null)
            {
                var tilePos = tileMapController.MapData.WorldToTile(worldPos);
                var tile = tileMapController.GetTileAtWorld(worldPos);
                string terrainInfo = tile != null ? tile.terrain.ToString() : "Unknown";
                
                positionText.text = $"Позиция: ({tilePos.x}, {tilePos.y}) | {terrainInfo}";
            }
            else
            {
                positionText.text = $"Позиция: ({worldPos.x:F1}, {worldPos.y:F1})";
            }
        }
        
        // === Utility ===
        
        private string FormatNumber(long number)
        {
            if (number >= 1000000)
                return $"{number / 1000000f:F1}M";
            if (number >= 1000)
                return $"{number / 1000f:F1}K";
            return number.ToString();
        }
        
        // === Context Menu ===
        
        [ContextMenu("Respawn Player")]
        public void RespawnPlayer()
        {
            if (spawnedPlayer != null)
            {
                UnsubscribeFromPlayer();
                Destroy(spawnedPlayer);
            }
            
            SpawnPlayer();
        }
        
        [ContextMenu("Move Player To Center")]
        public void MovePlayerToCenter()
        {
            if (spawnedPlayer == null || tileMapController == null) return;
            
            Vector3 centerPos = GetSpawnPosition();
            spawnedPlayer.transform.position = centerPos;
        }
    }
}

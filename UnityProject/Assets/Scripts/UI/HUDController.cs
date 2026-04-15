// ============================================================================
// HUDController.cs — Игровой HUD
// Cultivation World Simulator
// Версия: 1.1 — Fix-12: ServiceLocator, Qi safe cast, divide-by-zero guard, FormatQi note
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;
using CultivationGame.World;

namespace CultivationGame.UI
{
    /// <summary>
    /// Контроллер игрового HUD — отображает информацию во время игры.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TimeController timeController;
        
        [Header("Health & Qi")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Slider qiBar;
        [SerializeField] private TMP_Text qiText;
        [SerializeField] private Slider staminaBar;
        
        [Header("Cultivation")]
        [SerializeField] private TMP_Text cultivationLevelText;
        [SerializeField] private Slider cultivationProgressBar;
        [SerializeField] private TMP_Text cultivationProgressText;
        
        [Header("Time")]
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private Image seasonIcon;
        [SerializeField] private Sprite[] seasonIcons; // 0=Spring, 1=Summer, 2=Autumn, 3=Winter
        
        [Header("Location")]
        [SerializeField] private TMP_Text locationNameText;
        [SerializeField] private TMP_Text locationTypeText;
        
        [Header("Quick Slots")]
        [SerializeField] private Transform quickSlotsContainer;
        [SerializeField] private GameObject quickSlotPrefab;
        [SerializeField] private int quickSlotCount = 10;
        
        [Header("Notifications")]
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private float notificationDuration = 3f;
        [SerializeField] private int maxNotifications = 5;
        
        [Header("Minimap")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private Transform minimapPlayerIcon;
        
        // === Runtime ===
        private CultivationLevel currentCultivationLevel = CultivationLevel.None;
        private string currentLocationName = "";
        
        // === Events ===
#pragma warning disable CS0067
        public event Action<int> OnQuickSlotUsed;
#pragma warning restore CS0067
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            if (timeController == null)
                timeController = ServiceLocator.GetOrFind<TimeController>(); // FIX UI-H03 (2026-04-12)
        }
        
        private void Start()
        {
            InitializeHUD();
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void Update()
        {
            UpdateTimeDisplay();
        }
        
        // === Initialization ===
        
        private void InitializeHUD()
        {
            // Инициализация слотов
            if (quickSlotsContainer != null && quickSlotPrefab != null)
            {
                for (int i = 0; i < quickSlotCount; i++)
                {
                    Instantiate(quickSlotPrefab, quickSlotsContainer);
                }
            }
            
            // Начальные значения
            UpdateHealth(100, 100);
            UpdateQi(1000, 1000);
            UpdateCultivationLevel(CultivationLevel.None, 0);
        }
        
        private void SubscribeToEvents()
        {
            if (timeController != null)
            {
                timeController.OnHourPassed += OnHourPassed;
                timeController.OnDayPassed += OnDayPassed;
                timeController.OnSeasonChanged += OnSeasonChanged;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (timeController != null)
            {
                timeController.OnHourPassed -= OnHourPassed;
                timeController.OnDayPassed -= OnDayPassed;
                timeController.OnSeasonChanged -= OnSeasonChanged;
            }
        }
        
        // === Health & Qi Updates ===
        
        /// <summary>
        /// Обновить полоску здоровья.
        /// </summary>
        public void UpdateHealth(int current, int max)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = max;
                healthBar.value = current;
            }
            
            if (healthText != null)
            {
                healthText.text = $"{current}/{max}";
            }
        }
        
        /// <summary>
        /// Обновить полоску Ци.
        /// </summary>
        public void UpdateQi(long current, long max)
        {
            if (qiBar != null)
            {
                // FIX UI-H04: Qi long→float safe cast — prevents overflow for large Qi values (2026-04-12)
                qiBar.maxValue = 1f; // Normalized: slider works in 0..1 range
                qiBar.value = max > 0 ? (double)current / max > 1.0 ? 1f : (float)((double)current / max) : 0f;
            }
            
            if (qiText != null)
            {
                qiText.text = $"{FormatNumber(current)}/{FormatNumber(max)}";
            }
        }
        
        /// <summary>
        /// Обновить полоску выносливости.
        /// </summary>
        public void UpdateStamina(float current, float max)
        {
            if (staminaBar != null)
            {
                staminaBar.maxValue = max;
                staminaBar.value = current;
            }
        }
        
        /// <summary>
        /// Обновить уровень культивации.
        /// </summary>
        public void UpdateCultivationLevel(CultivationLevel level, float progress)
        {
            currentCultivationLevel = level;
            
            if (cultivationLevelText != null)
            {
                cultivationLevelText.text = GetCultivationLevelName(level);
            }
            
            if (cultivationProgressBar != null)
            {
                cultivationProgressBar.value = progress;
            }
            
            if (cultivationProgressText != null)
            {
                cultivationProgressText.text = $"{progress:F1}%";
            }
        }
        
        private string GetCultivationLevelName(CultivationLevel level)
        {
            return level switch
            {
                CultivationLevel.None => "Смертный",
                CultivationLevel.AwakenedCore => "Пробуждённое Ядро",
                CultivationLevel.LifeFlow => "Течение Жизни",
                CultivationLevel.InternalFire => "Пламя Внутреннего Огня",
                CultivationLevel.BodySpiritUnion => "Объединение Тела и Духа",
                CultivationLevel.HeartOfHeaven => "Сердце Небес",
                CultivationLevel.VeilBreaker => "Разрыв Пелены",
                CultivationLevel.EternalRing => "Вечное Кольцо",
                CultivationLevel.VoiceOfHeaven => "Глас Небес",
                CultivationLevel.ImmortalCore => "Бессмертное Ядро",
                CultivationLevel.Ascension => "Вознесение",
                _ => "Неизвестно"
            };
        }
        
        // === Time Display ===
        
        private void UpdateTimeDisplay()
        {
            if (timeController == null) return;
            
            if (timeText != null)
            {
                timeText.text = timeController.FormattedTime;
            }
            
            if (dateText != null)
            {
                dateText.text = timeController.FormattedDate;
            }
        }
        
        private void OnHourPassed(int hour)
        {
            // Обновляем иконку времени суток
        }
        
        private void OnDayPassed(int day)
        {
            // Анимация нового дня
        }
        
        private void OnSeasonChanged(Season season)
        {
            if (seasonIcon != null && seasonIcons != null && (int)season < seasonIcons.Length)
            {
                seasonIcon.sprite = seasonIcons[(int)season];
            }
        }
        
        // === Location Display ===
        
        /// <summary>
        /// Обновить отображение локации.
        /// </summary>
        public void UpdateLocation(string name, string type = "")
        {
            currentLocationName = name;
            
            if (locationNameText != null)
            {
                locationNameText.text = name;
            }
            
            if (locationTypeText != null)
            {
                locationTypeText.text = type;
            }
        }
        
        // === Quick Slots ===
        
        /// <summary>
        /// Обновить слот быстрого доступа.
        /// </summary>
        public void UpdateQuickSlot(int slot, string techniqueName, Sprite icon)
        {
            if (quickSlotsContainer == null || slot < 0 || slot >= quickSlotCount) return;
            
            Transform slotTransform = quickSlotsContainer.GetChild(slot);
            if (slotTransform != null)
            {
                Image iconImage = slotTransform.Find("Icon")?.GetComponent<Image>();
                TMP_Text nameText = slotTransform.Find("Name")?.GetComponent<TMP_Text>();
                
                if (iconImage != null)
                {
                    iconImage.sprite = icon;
                }
                
                if (nameText != null)
                {
                    nameText.text = techniqueName;
                }
            }
        }
        
        /// <summary>
        /// Установить кулдаун слота.
        /// </summary>
        public void SetQuickSlotCooldown(int slot, float remaining, float total)
        {
            if (quickSlotsContainer == null || slot < 0 || slot >= quickSlotCount) return;
            
            Transform slotTransform = quickSlotsContainer.GetChild(slot);
            if (slotTransform != null)
            {
                Image cooldownOverlay = slotTransform.Find("Cooldown")?.GetComponent<Image>();
                TMP_Text cooldownText = slotTransform.Find("CooldownText")?.GetComponent<TMP_Text>();
                
                if (cooldownOverlay != null)
                {
                    // FIX UI-M03: Divide by zero guard when total == 0 (2026-04-12)
                    cooldownOverlay.fillAmount = total > 0 ? remaining / total : 0f;
                }
                
                if (cooldownText != null)
                {
                    cooldownText.text = remaining > 0 ? $"{remaining:F1}" : "";
                }
            }
        }
        
        // === Notifications ===
        
        /// <summary>
        /// Показать уведомление.
        /// </summary>
        public void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            if (notificationContainer == null || notificationPrefab == null) return;
            
            // Ограничиваем количество уведомлений
            while (notificationContainer.childCount >= maxNotifications)
            {
                Destroy(notificationContainer.GetChild(0).gameObject);
            }
            
            GameObject notification = Instantiate(notificationPrefab, notificationContainer);
            
            TMP_Text textComponent = notification.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = message;
            }
            
            // Цвет в зависимости от типа
            Image backgroundImage = notification.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = GetNotificationColor(type);
            }
            
            // Автоудаление
            Destroy(notification, notificationDuration);
        }
        
        private Color GetNotificationColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Info => new Color(0.2f, 0.4f, 0.6f, 0.9f),
                NotificationType.Success => new Color(0.2f, 0.6f, 0.2f, 0.9f),
                NotificationType.Warning => new Color(0.8f, 0.6f, 0.1f, 0.9f),
                NotificationType.Error => new Color(0.7f, 0.2f, 0.2f, 0.9f),
                NotificationType.Cultivation => new Color(0.5f, 0.2f, 0.7f, 0.9f),
                _ => new Color(0.3f, 0.3f, 0.3f, 0.9f)
            };
        }
        
        // === Minimap ===
        
        /// <summary>
        /// Обновить позицию игрока на миникарте.
        /// </summary>
        public void UpdateMinimapPosition(Vector2 position)
        {
            if (minimapPlayerIcon != null)
            {
                minimapPlayerIcon.localPosition = new Vector3(position.x, position.y, 0);
            }
        }
        
        /// <summary>
        /// Обновить текстуру миникарты.
        /// </summary>
        public void UpdateMinimapTexture(Texture texture)
        {
            if (minimapImage != null)
            {
                minimapImage.texture = texture;
            }
        }
        
        // === Utility ===
        
        // NOTE UI-L03: Duplicate FormatNumber/FormatQi helper — also in CultivationProgressBar, CharacterPanelUI (2026-04-12)
        private string FormatNumber(long number)
        {
            if (number >= 1000000)
            {
                return $"{number / 1000000f:F1}M";
            }
            if (number >= 1000)
            {
                return $"{number / 1000f:F1}K";
            }
            return number.ToString();
        }
    }
    
    /// <summary>
    /// Тип уведомления.
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Cultivation
    }
}

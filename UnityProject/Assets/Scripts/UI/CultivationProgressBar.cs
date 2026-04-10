// ============================================================================
// CultivationProgressBar.cs — Улучшенная полоска прогресса культивации
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-31
// Этап: 5 - UI Enhancement
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Qi;

namespace CultivationGame.UI
{
    /// <summary>
    /// Улучшенная полоска прогресса культивации.
    /// Отображает текущий уровень, под-уровень, прогресс до прорыва.
    /// </summary>
    public class CultivationProgressBar : MonoBehaviour
    {
        #region Configuration

        [Header("References")]
        [SerializeField] private QiController qiController;

        [Header("Level Display")]
        [SerializeField] private TMP_Text levelNameText;
        [SerializeField] private TMP_Text levelNumberText;
        [SerializeField] private TMP_Text subLevelText;

        [Header("Progress Bars")]
        [SerializeField] private Slider mainProgressBar;
        [SerializeField] private Slider subLevelProgressBar;
        [SerializeField] private Image progressFillImage;

        [Header("Qi Display")]
        [SerializeField] private TMP_Text qiText;
        [SerializeField] private Slider qiSlider;
        [SerializeField] private TMP_Text qiDensityText;

        [Header("Breakthrough")]
        [SerializeField] private GameObject breakthroughReadyIndicator;
        [SerializeField] private Button breakthroughButton;
        [SerializeField] private TMP_Text breakthroughCostText;

        [Header("Colors")]
        [SerializeField] private Gradient progressGradient;
        [SerializeField] private Color breakthroughReadyColor = Color.yellow;
        [SerializeField] private Color normalColor = new Color(0.3f, 0.6f, 0.9f);

        [Header("Animation")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMinAlpha = 0.7f;
        [SerializeField] private float pulseMaxAlpha = 1f;

        #endregion

        #region Runtime Data

        private int currentLevel = 0;
        private int currentSubLevel = 0;
        private bool canBreakthrough = false;
        private bool isPulsing = false;

        #endregion

        #region Events

        public event Action OnBreakthroughRequested;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeUI();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            RefreshDisplay();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (isPulsing && breakthroughReadyIndicator != null)
            {
                UpdatePulseAnimation();
            }
        }

        #endregion

        #region Initialization

        private void InitializeUI()
        {
            if (qiController == null)
                qiController = FindFirstObjectByType<QiController>();

            if (breakthroughButton != null)
            {
                breakthroughButton.onClick.AddListener(OnBreakthroughClicked);
            }

            // Настройка градиента по умолчанию
            if (progressGradient == null || progressGradient.colorKeys.Length == 0)
            {
                progressGradient = new Gradient();
                progressGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.2f, 0.4f, 0.6f), 0f),
                        new GradientColorKey(new Color(0.4f, 0.7f, 0.9f), 0.5f),
                        new GradientColorKey(new Color(0.6f, 0.9f, 1f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
            }
        }

        private void SubscribeToEvents()
        {
            if (qiController != null)
            {
                qiController.OnQiChanged += OnQiChanged;
                qiController.OnCultivationLevelChanged += OnCultivationLevelChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (qiController != null)
            {
                qiController.OnQiChanged -= OnQiChanged;
                qiController.OnCultivationLevelChanged -= OnCultivationLevelChanged;
            }
        }

        #endregion

        #region Refresh

        public void RefreshDisplay()
        {
            if (qiController == null) return;

            currentLevel = qiController.CultivationLevel;
            // subLevel не доступен напрямую, используем 0

            // Название уровня
            UpdateLevelDisplay();

            // Прогресс бары
            UpdateProgressBars();

            // Qi
            UpdateQiDisplay();

            // Прорыв
            UpdateBreakthroughStatus();
        }

        private void UpdateLevelDisplay()
        {
            if (levelNameText != null)
            {
                levelNameText.text = GetLevelName(currentLevel);
            }

            if (levelNumberText != null)
            {
                levelNumberText.text = $"L{currentLevel}";
            }

            if (subLevelText != null)
            {
                subLevelText.text = $".{currentSubLevel}";
            }
        }

        private void UpdateProgressBars()
        {
            // Прогресс до следующего под-уровня = QiPercent
            float subProgress = qiController.QiPercent;

            if (subLevelProgressBar != null)
            {
                subLevelProgressBar.value = subProgress;
            }

            // Общий прогресс до большого прорыва
            float totalProgress = qiController.QiPercent;

            if (mainProgressBar != null)
            {
                mainProgressBar.value = totalProgress;
            }

            if (progressFillImage != null && progressGradient != null)
            {
                progressFillImage.color = progressGradient.Evaluate(totalProgress);
            }
        }

        private void UpdateQiDisplay()
        {
            if (qiText != null)
            {
                qiText.text = $"{FormatQi(qiController.CurrentQi)}/{FormatQi(qiController.MaxQi)}";
            }

            if (qiSlider != null)
            {
                qiSlider.maxValue = qiController.MaxQi;
                qiSlider.value = qiController.CurrentQi;
            }

            if (qiDensityText != null)
            {
                int density = GameConstants.QiDensityByLevel[Mathf.Clamp(currentLevel - 1, 0, 9)];
                qiDensityText.text = $"Плотность: ×{density}";
            }
        }

        private void UpdateBreakthroughStatus()
        {
            canBreakthrough = qiController.CanBreakthrough(false);

            if (breakthroughReadyIndicator != null)
            {
                breakthroughReadyIndicator.SetActive(canBreakthrough);
                isPulsing = canBreakthrough;
            }

            if (breakthroughButton != null)
            {
                breakthroughButton.interactable = canBreakthrough;
            }

            if (breakthroughCostText != null)
            {
                // FIX: Модель В — стоимость прорыва = capacity(next) × density
                long cost = qiController.CalculateBreakthroughRequirement(false);
                breakthroughCostText.text = $"Прорыв: {FormatQi(cost)} Ци";
            }
        }

        #endregion

        #region Event Handlers

        private void OnQiChanged(long current, long max)
        {
            UpdateQiDisplay();
            UpdateProgressBars();
            UpdateBreakthroughStatus();
        }

        private void OnCultivationLevelChanged(int level)
        {
            currentLevel = level;
            currentSubLevel = 0;
            RefreshDisplay();

            // Визуальный эффект прорыва
            PlayBreakthroughEffect();
        }

        private void OnBreakthroughClicked()
        {
            OnBreakthroughRequested?.Invoke();
        }

        #endregion

        #region Effects

        private void UpdatePulseAnimation()
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, t);

            var color = breakthroughReadyColor;
            color.a = alpha;

            var image = breakthroughReadyIndicator.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private void PlayBreakthroughEffect()
        {
            // TODO: Добавить эффект прорыва
            // - Анимация вспышки
            // - Звук
            // - Частицы
            Debug.Log($"Breakthrough effect: Level {currentLevel}!");
        }

        #endregion

        #region Utility

        private string GetLevelName(int level)
        {
            return level switch
            {
                0 => "Смертный",
                1 => "Пробуждённое Ядро",
                2 => "Течение Жизни",
                3 => "Пламя Внутреннего Огня",
                4 => "Объединение Тела и Духа",
                5 => "Сердце Небес",
                6 => "Разрыв Пелены",
                7 => "Вечное Кольцо",
                8 => "Глас Небес",
                9 => "Бессмертное Ядро",
                10 => "Вознесение",
                _ => "Неизвестно"
            };
        }

        private string FormatQi(long qi)
        {
            if (qi >= 1000000000)
                return $"{qi / 1000000000f:F1}B";
            if (qi >= 1000000)
                return $"{qi / 1000000f:F1}M";
            if (qi >= 1000)
                return $"{qi / 1000f:F1}K";
            return qi.ToString();
        }

        #endregion
    }

    // ============================================================================
    // QuickSlotPanel — Панель быстрых слотов техник
    // ============================================================================

    public class QuickSlotPanel : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField] private QuickSlotUI[] quickSlots;
        [SerializeField] private int selectedSlot = 0;

        [Header("References")]
        [SerializeField] private Combat.TechniqueController techniqueController;

        public event Action<int> OnSlotSelected;

        private void Awake()
        {
            if (techniqueController == null)
                techniqueController = FindFirstObjectByType<Combat.TechniqueController>();

            for (int i = 0; i < quickSlots.Length; i++)
            {
                int slotIndex = i;
                if (quickSlots[i] != null)
                {
                    quickSlots[i].OnClicked += () => SelectSlot(slotIndex);
                }
            }
        }

        private void Update()
        {
            HandleInput();
            UpdateCooldowns();
        }

        private void HandleInput()
        {
            // Цифровые клавиши 1-9, 0 для слотов
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    int slot = i == 9 ? 9 : i;
                    if (slot < quickSlots.Length)
                    {
                        SelectSlot(slot);
                        UseSlot(slot);
                    }
                }
            }
        }

        public void SelectSlot(int slot)
        {
            // Снимаем выделение со старого
            if (selectedSlot >= 0 && selectedSlot < quickSlots.Length && quickSlots[selectedSlot] != null)
            {
                quickSlots[selectedSlot].SetSelected(false);
            }

            // Выделяем новый
            selectedSlot = slot;
            if (selectedSlot >= 0 && selectedSlot < quickSlots.Length && quickSlots[selectedSlot] != null)
            {
                quickSlots[selectedSlot].SetSelected(true);
            }

            OnSlotSelected?.Invoke(slot);
        }

        public void UseSlot(int slot)
        {
            if (techniqueController != null)
            {
                techniqueController.UseQuickSlot(slot);
            }
        }

        private void UpdateCooldowns()
        {
            // Обновляем отображение кулдаунов
            for (int i = 0; i < quickSlots.Length; i++)
            {
                if (quickSlots[i] != null)
                {
                    var tech = techniqueController?.GetQuickSlotTechnique(i);
                    if (tech != null)
                    {
                        float remaining = tech.CooldownRemaining;
                        float total = tech.Data?.cooldown * 60f ?? 60f;
                        quickSlots[i].UpdateCooldown(remaining, total);
                    }
                }
            }
        }

        public void RefreshSlots()
        {
            if (techniqueController == null) return;

            for (int i = 0; i < quickSlots.Length; i++)
            {
                if (quickSlots[i] != null)
                {
                    var tech = techniqueController.GetQuickSlotTechnique(i);
                    quickSlots[i].SetTechnique(tech);
                }
            }
        }
    }

    // ============================================================================
    // QuickSlotUI — Отдельный быстрый слот
    // ============================================================================

    public class QuickSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private Image selectionBorder;
        [SerializeField] private GameObject emptyOverlay;

        private Combat.LearnedTechnique technique;

        public event Action OnClicked;

        public void SetTechnique(Combat.LearnedTechnique tech)
        {
            technique = tech;

            if (tech != null && tech.Data != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = tech.Data.icon;
                    iconImage.enabled = true;
                }

                if (emptyOverlay != null)
                    emptyOverlay.SetActive(false);
            }
            else
            {
                Clear();
            }
        }

        public void Clear()
        {
            technique = null;

            if (iconImage != null)
                iconImage.enabled = false;

            if (cooldownOverlay != null)
                cooldownOverlay.fillAmount = 0f;

            if (cooldownText != null)
                cooldownText.text = "";

            if (emptyOverlay != null)
                emptyOverlay.SetActive(true);
        }

        public void SetSelected(bool selected)
        {
            if (selectionBorder != null)
            {
                selectionBorder.enabled = selected;
                selectionBorder.color = selected ? Color.yellow : Color.white;
            }
        }

        public void UpdateCooldown(float remaining, float total)
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = total > 0 ? remaining / total : 0f;
            }

            if (cooldownText != null)
            {
                cooldownText.text = remaining > 0 ? $"{remaining:F1}" : "";
            }
        }

        public void SetKeyText(string text)
        {
            if (keyText != null)
                keyText.text = text;
        }

        public void OnPointerClick()
        {
            OnClicked?.Invoke();
        }
    }

    // ============================================================================
    // MinimapUI — Миникарта (заглушка)
    // ============================================================================

    public class MinimapUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private Transform playerIcon;
        [SerializeField] private TMP_Text locationNameText;

        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private Vector2 mapSize = new Vector2(100, 100);

        private Transform playerTransform;
        private float lastUpdateTime;

        private void Start()
        {
            var player = FindFirstObjectByType<Player.PlayerController>();
            if (player != null)
                playerTransform = player.transform;
        }

        private void Update()
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateMinimap();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdateMinimap()
        {
            if (playerTransform == null || playerIcon == null) return;

            // Обновляем позицию игрока на карте
            Vector2 mapPos = WorldToMapPosition(playerTransform.position);
            playerIcon.localPosition = new Vector3(mapPos.x, mapPos.y, 0);
        }

        private Vector2 WorldToMapPosition(Vector3 worldPos)
        {
            // Простое преобразование (заглушка)
            return new Vector2(
                (worldPos.x / mapSize.x) * 100,
                (worldPos.y / mapSize.y) * 100
            );
        }

        public void SetLocationName(string name)
        {
            if (locationNameText != null)
                locationNameText.text = name;
        }
    }
}

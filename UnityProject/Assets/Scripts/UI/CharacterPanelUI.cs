// ============================================================================
// CharacterPanelUI.cs — Панель персонажа
// Cultivation World Simulator
// Версия: 1.1 — Fix-12: ServiceLocator, Qi safe cast, Input note, FormatQi note
// ============================================================================
// Создан: 2026-03-31
// Этап: 5 - UI Enhancement
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Body;
using CultivationGame.Player;
using CultivationGame.Inventory;
using CultivationGame.Qi;

namespace CultivationGame.UI
{
    /// <summary>
    /// Панель персонажа — отображает части тела, HP, экипировку и статы.
    /// </summary>
    public class CharacterPanelUI : MonoBehaviour
    {
        #region Configuration

        [Header("Player Reference")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private BodyController bodyController;
        [SerializeField] private QiController qiController;
        [SerializeField] private EquipmentController equipmentController;

        [Header("Character Info")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text cultivationLevelText;
        [SerializeField] private Slider cultivationProgressSlider;
        [SerializeField] private TMP_Text cultivationProgressText;

        [Header("Health Bars")]
        [SerializeField] private Slider totalHealthSlider;
        [SerializeField] private TMP_Text totalHealthText;
        [SerializeField] private Slider qiSlider;
        [SerializeField] private TMP_Text qiText;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private TMP_Text staminaText;

        [Header("Body Parts Display")]
        [SerializeField] private Transform bodyPartsContainer;
        [SerializeField] private GameObject bodyPartUIPrefab;

        [Header("Equipment Slots")]
        [SerializeField] private EquipmentSlotUI[] equipmentSlots;

        [Header("Stats Display")]
        [SerializeField] private Transform statsContainer;
        [SerializeField] private GameObject statRowPrefab;

        [Header("Body Visualization")]
        [SerializeField] private Image bodyDiagram;
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private Color severedColor = Color.gray;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button meditationButton;
        [SerializeField] private Button breakthroughButton;

        #endregion

        #region Runtime Data

        private Dictionary<BodyPartType, BodyPartUI> bodyPartUIs = new Dictionary<BodyPartType, BodyPartUI>();
        private Dictionary<string, StatRowUI> statRows = new Dictionary<string, StatRowUI>();
        private bool isInitialized = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeUI();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            RefreshAll();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateDynamicElements();
        }

        #endregion

        #region Initialization

        private void InitializeUI()
        {
            // Находим контроллеры если не назначены
            if (playerController == null)
                playerController = ServiceLocator.GetOrFind<PlayerController>(); // FIX UI-H03 (2026-04-12)

            if (bodyController == null && playerController != null)
                bodyController = playerController.GetComponent<BodyController>();

            if (qiController == null && playerController != null)
                qiController = playerController.GetComponent<QiController>();

            if (equipmentController == null && playerController != null)
                equipmentController = playerController.GetComponent<EquipmentController>();

            // Подписываемся на кнопки
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);

            if (meditationButton != null)
                meditationButton.onClick.AddListener(StartMeditation);

            if (breakthroughButton != null)
                breakthroughButton.onClick.AddListener(AttemptBreakthrough);

            // Инициализируем слоты экипировки
            InitializeEquipmentSlots();

            isInitialized = true;
        }

        private void InitializeEquipmentSlots()
        {
            foreach (var slotUI in equipmentSlots)
            {
                if (slotUI != null)
                {
                    slotUI.OnSlotClicked += OnEquipmentSlotClicked;
                }
            }
        }

        private void SubscribeToEvents()
        {
            if (bodyController != null)
            {
                bodyController.OnDamageTaken += OnDamageTaken;
                bodyController.OnPartSevered += OnPartSevered;
                bodyController.OnDeath += OnDeath;
            }

            if (equipmentController != null)
            {
                equipmentController.OnEquipmentEquipped += OnEquipmentEquipped;
                equipmentController.OnEquipmentUnequipped += OnEquipmentUnequipped;
                equipmentController.OnStatsChanged += OnEquipmentStatsChanged;
            }

            if (playerController != null)
            {
                playerController.OnHealthChanged += OnHealthChanged;
                playerController.OnQiChanged += OnQiChanged;
                playerController.OnCultivationLevelChanged += OnCultivationLevelChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (bodyController != null)
            {
                bodyController.OnDamageTaken -= OnDamageTaken;
                bodyController.OnPartSevered -= OnPartSevered;
                bodyController.OnDeath -= OnDeath;
            }

            if (equipmentController != null)
            {
                equipmentController.OnEquipmentEquipped -= OnEquipmentEquipped;
                equipmentController.OnEquipmentUnequipped -= OnEquipmentUnequipped;
                equipmentController.OnStatsChanged -= OnEquipmentStatsChanged;
            }

            if (playerController != null)
            {
                playerController.OnHealthChanged -= OnHealthChanged;
                playerController.OnQiChanged -= OnQiChanged;
                playerController.OnCultivationLevelChanged -= OnCultivationLevelChanged;
            }
        }

        #endregion

        #region Refresh

        public void RefreshAll()
        {
            RefreshCharacterInfo();
            RefreshHealthBars();
            RefreshBodyParts();
            RefreshEquipment();
            RefreshStats();
        }

        private void RefreshCharacterInfo()
        {
            if (playerController != null)
            {
                if (playerNameText != null)
                    playerNameText.text = playerController.PlayerName;

                var level = playerController.CultivationLevel;
                if (cultivationLevelText != null)
                    cultivationLevelText.text = GetCultivationLevelName(level);
            }

            if (qiController != null)
            {
                // Прогресс = текущее Ци / максимальное Ци
                float progress = qiController.QiPercent;
                if (cultivationProgressSlider != null)
                    cultivationProgressSlider.value = progress;

                if (cultivationProgressText != null)
                    cultivationProgressText.text = $"{progress * 100:F1}%";
            }
        }

        private void RefreshHealthBars()
        {
            if (bodyController != null)
            {
                float healthPercent = bodyController.HealthPercent;
                int healthValue = Mathf.RoundToInt(healthPercent * 100);

                if (totalHealthSlider != null)
                    totalHealthSlider.value = healthPercent;

                if (totalHealthText != null)
                    totalHealthText.text = $"{healthValue}%";
            }

            if (qiController != null)
            {
                if (qiSlider != null)
                {
                    // FIX UI-H04: Qi long→float safe cast — prevents overflow for large Qi values (2026-04-12)
                    long maxQi = qiController.MaxQi;
                    long currentQi = qiController.CurrentQi;
                    qiSlider.maxValue = 1f; // Normalized: slider works in 0..1 range
                    qiSlider.value = maxQi > 0 ? (double)currentQi / maxQi > 1.0 ? 1f : (float)((double)currentQi / maxQi) : 0f;
                }

                if (qiText != null)
                {
                    qiText.text = $"{FormatQi(qiController.CurrentQi)}/{FormatQi(qiController.MaxQi)}";
                }
            }

            if (playerController != null)
            {
                var state = playerController.State;
                if (staminaSlider != null)
                {
                    staminaSlider.maxValue = state.MaxStamina;
                    staminaSlider.value = state.CurrentStamina;
                }

                if (staminaText != null)
                {
                    staminaText.text = $"{state.CurrentStamina:F0}/{state.MaxStamina:F0}";
                }
            }
        }

        private void RefreshBodyParts()
        {
            if (bodyController == null || bodyPartsContainer == null || bodyPartUIPrefab == null) return;

            // Очищаем старые
            foreach (var kvp in bodyPartUIs)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                    Destroy(kvp.Value.gameObject);
            }
            bodyPartUIs.Clear();

            // Создаём новые
            foreach (var part in bodyController.BodyParts)
            {
                CreateBodyPartUI(part);
            }
        }

        private void CreateBodyPartUI(BodyPart part)
        {
            var partGO = Instantiate(bodyPartUIPrefab, bodyPartsContainer);
            var partUI = partGO.GetComponent<BodyPartUI>();

            if (partUI == null)
                partUI = partGO.AddComponent<BodyPartUI>();

            partUI.Initialize(part);
            bodyPartUIs[part.PartType] = partUI;
        }

        private void RefreshEquipment()
        {
            if (equipmentController == null) return;

            foreach (var slotUI in equipmentSlots)
            {
                if (slotUI != null)
                {
                    var equipped = equipmentController.GetEquipment(slotUI.SlotType);
                    slotUI.SetEquipment(equipped);
                }
            }
        }

        private void RefreshStats()
        {
            if (statsContainer == null || statRowPrefab == null) return;

            // Очищаем старые
            foreach (var kvp in statRows)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                    Destroy(kvp.Value.gameObject);
            }
            statRows.Clear();

            // Базовые статы
            if (playerController != null && playerController.State != null)
            {
                AddStatRow("cultivation_level", "Уровень культивации", ((int)playerController.CultivationLevel).ToString());
                AddStatRow("qi_current", "Текущее Ци", FormatQi(qiController?.CurrentQi ?? 0));
                AddStatRow("qi_max", "Максимальное Ци", FormatQi(qiController?.MaxQi ?? 0));
            }

            // Статы от экипировки
            if (equipmentController != null)
            {
                var equipStats = equipmentController.CurrentStats;

                if (equipStats.totalDamage > 0)
                    AddStatRow("damage", "Урон", equipStats.totalDamage.ToString());

                if (equipStats.totalDefense > 0)
                    AddStatRow("defense", "Защита", equipStats.totalDefense.ToString());

                if (equipStats.strength != 0)
                    AddStatRow("str", "Сила", $"+{equipStats.strength:F0}");

                if (equipStats.agility != 0)
                    AddStatRow("agi", "Ловкость", $"+{equipStats.agility:F0}");

                if (equipStats.constitution != 0)
                    AddStatRow("con", "Телосложение", $"+{equipStats.constitution:F0}");

                if (equipStats.intelligence != 0)
                    AddStatRow("int", "Интеллект", $"+{equipStats.intelligence:F0}");
            }

            // Штраф от повреждений
            if (bodyController != null)
            {
                float penalty = bodyController.DamagePenalty;
                if (penalty > 0)
                {
                    AddStatRow("damage_penalty", "Штраф от ран", $"-{penalty * 100:F0}%");
                }
            }
        }

        private void AddStatRow(string id, string name, string value)
        {
            var rowGO = Instantiate(statRowPrefab, statsContainer);
            var rowUI = rowGO.GetComponent<StatRowUI>();

            if (rowUI == null)
            {
                // Создаём простой компонент если нет префаба
                var texts = rowGO.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 2)
                {
                    texts[0].text = name;
                    texts[1].text = value;
                }
            }
            else
            {
                rowUI.SetData(name, value);
            }

            statRows[id] = rowUI;
        }

        #endregion

        #region Event Handlers

        private void OnDamageTaken(BodyPart part, BodyDamageResult result)
        {
            if (bodyPartUIs.TryGetValue(part.PartType, out var partUI))
            {
                partUI.UpdateDisplay();
            }

            RefreshHealthBars();
            RefreshStats();
        }

        private void OnPartSevered(BodyPart part)
        {
            if (bodyPartUIs.TryGetValue(part.PartType, out var partUI))
            {
                partUI.UpdateDisplay();
            }

            RefreshStats();
        }

        private void OnDeath()
        {
            RefreshAll();
        }

        private void OnHealthChanged(int current, int max)
        {
            RefreshHealthBars();
        }

        private void OnQiChanged(long current, long max)
        {
            RefreshHealthBars();
        }

        private void OnCultivationLevelChanged(CultivationLevel level)
        {
            RefreshCharacterInfo();
            RefreshStats();
        }

        private void OnEquipmentEquipped(EquipmentSlot slot, EquipmentInstance instance)
        {
            RefreshEquipment();
            RefreshStats();
        }

        private void OnEquipmentUnequipped(EquipmentSlot slot, EquipmentInstance instance)
        {
            RefreshEquipment();
            RefreshStats();
        }

        private void OnEquipmentStatsChanged(EquipmentStats stats)
        {
            RefreshStats();
        }

        private void OnEquipmentSlotClicked(EquipmentSlot slot)
        {
            if (equipmentController == null) return;

            // Снимаем экипировку
            var unequipped = equipmentController.Unequip(slot);
            if (unequipped != null)
            {
                // TODO: Добавить обратно в инвентарь
                Debug.Log($"Unequipped: {unequipped.Name}");
            }
        }

        #endregion

        #region Dynamic Updates

        private void UpdateDynamicElements()
        {
            // Обновляем только динамические элементы каждый кадр
            UpdateBodyVisualization();
        }

        private void UpdateBodyVisualization()
        {
            if (bodyController == null || bodyDiagram == null) return;

            // Обновляем цвета частей тела на диаграмме
            foreach (var part in bodyController.BodyParts)
            {
                UpdateBodyPartVisualization(part);
            }
        }

        private void UpdateBodyPartVisualization(BodyPart part)
        {
            // Ищем соответствующий элемент на диаграмме по имени
            if (bodyDiagram == null) return;

            var transforms = bodyDiagram.GetComponentsInChildren<Transform>();
            foreach (var t in transforms)
            {
                if (t.name.ToLower().Contains(part.PartType.ToString().ToLower()))
                {
                    var image = t.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = GetPartHealthColor(part);
                    }
                }
            }
        }

        private Color GetPartHealthColor(BodyPart part)
        {
            if (part.IsSevered())
                return severedColor;

            float healthPercent = part.GetRedHPPercent();

            if (healthPercent > 0.7f)
                return Color.Lerp(damagedColor, healthyColor, (healthPercent - 0.7f) / 0.3f);
            else if (healthPercent > 0.3f)
                return Color.Lerp(criticalColor, damagedColor, (healthPercent - 0.3f) / 0.4f);
            else
                return criticalColor;
        }

        #endregion

        #region Actions

        private void StartMeditation()
        {
            if (playerController != null)
            {
                playerController.StartMeditation();
            }
        }

        private void AttemptBreakthrough()
        {
            if (playerController != null)
            {
                bool success = playerController.AttemptBreakthrough(isMajor: false);
                if (success)
                {
                    Debug.Log("Breakthrough successful!");
                }
                else
                {
                    Debug.Log("Breakthrough failed - not enough Qi");
                }
            }
        }

        private void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Utility

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

        // NOTE UI-L03: Duplicate FormatQi helper — also in CultivationProgressBar, HUDController (2026-04-12)
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
    // BodyPartUI — Визуализация части тела
    // ============================================================================

    public class BodyPartUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text partNameText;
        [SerializeField] private Slider redHPSlider;
        [SerializeField] private Slider blackHPSlider;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private Image stateIcon;
        [SerializeField] private GameObject severedOverlay;

        private BodyPart bodyPart;

        public void Initialize(BodyPart part)
        {
            bodyPart = part;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (bodyPart == null) return;

            // Название
            if (partNameText != null)
            {
                partNameText.text = GetPartName(bodyPart.PartType);
            }

            // HP слайдеры
            if (redHPSlider != null)
            {
                redHPSlider.maxValue = bodyPart.MaxRedHP;
                redHPSlider.value = bodyPart.CurrentRedHP;
            }

            if (blackHPSlider != null)
            {
                blackHPSlider.maxValue = bodyPart.MaxBlackHP;
                blackHPSlider.value = bodyPart.CurrentBlackHP;
            }

            // Текст HP
            if (hpText != null)
            {
                if (bodyPart.IsSevered())
                {
                    hpText.text = "Отсечено";
                }
                else
                {
                    hpText.text = $"{bodyPart.CurrentRedHP:F0}/{bodyPart.MaxRedHP:F0}";
                }
            }

            // Оверлей отсечения
            if (severedOverlay != null)
            {
                severedOverlay.SetActive(bodyPart.IsSevered());
            }

            // Иконка состояния
            UpdateStateIcon();
        }

        private void UpdateStateIcon()
        {
            if (stateIcon == null) return;

            stateIcon.color = bodyPart.State switch
            {
                BodyPartState.Healthy => Color.green,
                BodyPartState.Bruised => Color.yellow,
                BodyPartState.Wounded => Color.orange,
                BodyPartState.Disabled => Color.red,
                BodyPartState.Severed => Color.gray,
                BodyPartState.Destroyed => Color.black,
                _ => Color.white
            };
        }

        private string GetPartName(BodyPartType type)
        {
            return type switch
            {
                BodyPartType.Head => "Голова",
                BodyPartType.Torso => "Торс",
                BodyPartType.Heart => "Сердце",
                BodyPartType.LeftArm => "Левая рука",
                BodyPartType.RightArm => "Правая рука",
                BodyPartType.LeftLeg => "Левая нога",
                BodyPartType.RightLeg => "Правая нога",
                BodyPartType.LeftHand => "Левая кисть",
                BodyPartType.RightHand => "Правая кисть",
                BodyPartType.LeftFoot => "Левая стопа",
                BodyPartType.RightFoot => "Правая стопа",
                _ => type.ToString()
            };
        }
    }

    // ============================================================================
    // EquipmentSlotUI — Слот экипировки
    // ============================================================================

    public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private EquipmentSlot slotType;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image durabilityBar;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject emptyOverlay;

        private EquipmentInstance currentEquipment;

        public event Action<EquipmentSlot> OnSlotClicked;
        public EquipmentSlot SlotType => slotType;

        public void SetEquipment(EquipmentInstance instance)
        {
            currentEquipment = instance;

            if (instance != null && instance.equipmentData != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = instance.equipmentData.icon;
                    iconImage.enabled = true;
                }

                if (nameText != null)
                {
                    nameText.text = instance.Name;
                }

                if (durabilityBar != null)
                {
                    durabilityBar.fillAmount = instance.DurabilityPercent;
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
            currentEquipment = null;

            if (iconImage != null)
                iconImage.enabled = false;

            if (nameText != null)
                nameText.text = GetSlotName();

            if (durabilityBar != null)
                durabilityBar.fillAmount = 1f;

            if (emptyOverlay != null)
                emptyOverlay.SetActive(true);
        }

        private string GetSlotName()
        {
            // FIX CORE-M02: EquipmentSlot обновлён по INVENTORY_SYSTEM.md
            return slotType switch
            {
                EquipmentSlot.WeaponMain => "Основное оружие",
                EquipmentSlot.WeaponOff => "Вторичное оружие",
                EquipmentSlot.Armor => "Броня",
                EquipmentSlot.Clothing => "Одежда",
                EquipmentSlot.Charger => "Зарядник",
                EquipmentSlot.RingLeft => "Кольцо (левое)",
                EquipmentSlot.RingRight => "Кольцо (правое)",
                EquipmentSlot.Accessory => "Аксессуар",
                EquipmentSlot.Backpack => "Рюкзак",
                _ => slotType.ToString()
            };
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnSlotClicked?.Invoke(slotType);
            }
        }
    }

    // ============================================================================
    // StatRowUI — Строка характеристики
    // ============================================================================

    public class StatRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text valueText;

        public void SetData(string name, string value)
        {
            if (nameText != null)
                nameText.text = name;

            if (valueText != null)
                valueText.text = value;
        }
    }
}

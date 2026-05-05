// ============================================================================
// TechniqueAssignmentUI.cs — Панель назначения техник в слоты быстрого доступа
// Cultivation World Simulator
// Создано: 2026-05-07 11:00:00 UTC
// ФАЗА 5: Назначение техник в quickslots
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Combat;

namespace CultivationGame.UI
{
    /// <summary>
    /// Панель назначения техник в слоты быстрого доступа (quickslots 0-8).
    /// Открывается из CharacterPanelUI (вкладка «Техники»).
    /// 
    /// Пайплайн:
    /// 1. Игрок нажимает на технику из списка → выбирается
    /// 2. Игрок нажимает на слот (1-9) → техника назначается в этот слот
    /// 3. TechniqueController.AssignToQuickSlot() — фактическое назначение
    /// </summary>
    public class TechniqueAssignmentUI : MonoBehaviour
    {
        #region UI References

        [Header("Technique List")]
        [Tooltip("Контейнер списка изученных техник")]
        public Transform techniqueListContainer;

        [Tooltip("Префаб строки техники")]
        public GameObject techniqueEntryPrefab;

        [Header("Quick Slots")]
        [Tooltip("Массив кнопок слотов (1-9)")]
        public Button[] quickSlotButtons = new Button[9];

        [Tooltip("Массив текстов слотов (название техники)")]
        public TMP_Text[] quickSlotTexts = new TMP_Text[9];

        [Header("Info")]
        [Tooltip("Текст описания выбранной техники")]
        public TMP_Text selectedTechniqueInfo;

        [Header("Buttons")]
        [Tooltip("Кнопка закрытия панели")]
        public Button closeButton;

        [Tooltip("Кнопка очистки слота")]
        public Button clearSlotButton;

        #endregion

        #region Runtime Data

        private TechniqueController techniqueController;
        private CultivationGame.Inventory.EquipmentController equipmentController;
        private LearnedTechnique selectedTechnique;
        private int selectedSlot = -1;
        private List<TechniqueEntryUI> entries = new List<TechniqueEntryUI>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Кнопка закрытия
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            // Кнопка очистки слота
            if (clearSlotButton != null)
                clearSlotButton.onClick.AddListener(ClearSelectedSlot);

            // Кнопки слотов
            for (int i = 0; i < quickSlotButtons.Length; i++)
            {
                int slotIndex = i; // Замыкание
                if (quickSlotButtons[i] != null)
                    quickSlotButtons[i].onClick.AddListener(() => OnQuickSlotClicked(slotIndex));
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (clearSlotButton != null)
                clearSlotButton.onClick.RemoveListener(ClearSelectedSlot);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Инициализировать панель из TechniqueController и EquipmentController.
        /// </summary>
        public void Initialize(TechniqueController techCtrl, CultivationGame.Inventory.EquipmentController eqCtrl)
        {
            techniqueController = techCtrl;
            equipmentController = eqCtrl;
            RefreshAll();
        }

        /// <summary>
        /// Показать панель.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            RefreshAll();
        }

        /// <summary>
        /// Скрыть панель.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Полное обновление: список техник + слоты.
        /// </summary>
        public void RefreshAll()
        {
            RefreshTechniqueList();
            RefreshQuickSlots();
            selectedTechnique = null;
            selectedSlot = -1;
            UpdateInfoText();
        }

        #endregion

        #region Technique List

        /// <summary>
        /// Обновить список изученных техник.
        /// </summary>
        private void RefreshTechniqueList()
        {
            // Очистить старые записи
            foreach (var entry in entries)
            {
                if (entry != null && entry.gameObject != null)
                    Destroy(entry.gameObject);
            }
            entries.Clear();

            if (techniqueController == null || techniqueListContainer == null) return;

            foreach (var tech in techniqueController.Techniques)
            {
                if (tech == null || tech.Data == null) continue;

                var entryObj = Instantiate(techniqueEntryPrefab, techniqueListContainer);
                var entryUI = entryObj.GetComponent<TechniqueEntryUI>();
                if (entryUI == null) entryUI = entryObj.AddComponent<TechniqueEntryUI>();

                // Инициализируем запись
                string weaponReq = tech.Data.combatSubtype == CombatSubtype.MeleeWeapon ? " [Треб. оружие]" : "";
                entryUI.Setup(tech, tech.Data.nameRu + weaponReq, tech.Mastery);
                entryUI.OnClicked += OnTechniqueClicked;

                entries.Add(entryUI);
            }
        }

        /// <summary>
        /// Обработчик нажатия на технику в списке.
        /// </summary>
        private void OnTechniqueClicked(LearnedTechnique technique)
        {
            selectedTechnique = technique;
            UpdateInfoText();
        }

        #endregion

        #region Quick Slots

        /// <summary>
        /// Обновить отображение слотов быстрого доступа.
        /// </summary>
        private void RefreshQuickSlots()
        {
            if (techniqueController == null) return;

            for (int i = 0; i < 9; i++)
            {
                var tech = techniqueController.GetQuickSlotTechnique(i);
                if (quickSlotTexts[i] != null)
                {
                    if (tech != null && tech.Data != null)
                    {
                        quickSlotTexts[i].text = $"{i + 1}: {tech.Data.nameRu}";
                    }
                    else
                    {
                        quickSlotTexts[i].text = $"{i + 1}: —";
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия на слот быстрого доступа.
        /// </summary>
        private void OnQuickSlotClicked(int slot)
        {
            // Если техника выбрана → назначить в слот
            if (selectedTechnique != null && techniqueController != null)
            {
                techniqueController.AssignToQuickSlot(selectedTechnique, slot);
                selectedSlot = slot;
                RefreshQuickSlots();
                UpdateInfoText();
                Debug.Log($"[TechniqueAssignmentUI] {selectedTechnique.Data.nameRu} → слот {slot + 1}");
            }
            // Если техника не выбрана → показать текущую в слоте
            else
            {
                selectedSlot = slot;
                var tech = techniqueController?.GetQuickSlotTechnique(slot);
                if (tech != null)
                {
                    selectedTechnique = tech;
                }
                UpdateInfoText();
            }
        }

        /// <summary>
        /// Очистить выбранный слот.
        /// </summary>
        private void ClearSelectedSlot()
        {
            if (selectedSlot < 0 || techniqueController == null) return;

            var tech = techniqueController.GetQuickSlotTechnique(selectedSlot);
            if (tech != null)
            {
                // Снимаем назначение (QuickSlot = -1)
                techniqueController.AssignToQuickSlot(tech, -1);
            }

            selectedSlot = -1;
            RefreshQuickSlots();
            UpdateInfoText();
        }

        #endregion

        #region Info

        /// <summary>
        /// Обновить текст описания выбранной техники.
        /// </summary>
        private void UpdateInfoText()
        {
            if (selectedTechniqueInfo == null) return;

            if (selectedTechnique != null && selectedTechnique.Data != null)
            {
                var data = selectedTechnique.Data;
                string info = $"<b>{data.nameRu}</b>\n";
                info += $"Тип: {data.techniqueType} | Элемент: {data.element}\n";
                info += $"Мастерство: {selectedTechnique.Mastery:F1}%\n";
                info += $"Ци: {data.baseQiCost} | Кулдаун: {data.cooldown:F1}с\n";

                if (data.combatSubtype == CombatSubtype.MeleeWeapon)
                {
                    bool hasWeapon = equipmentController != null && equipmentController.GetMainWeapon() != null;
                    info += hasWeapon ? "\n<color=green>✓ Оружие экипировано</color>"
                                     : "\n<color=red>✗ Требуется оружие</color>";
                }

                if (selectedSlot >= 0)
                    info += $"\nСлот: {selectedSlot + 1}";

                selectedTechniqueInfo.text = info;
            }
            else
            {
                selectedTechniqueInfo.text = "Выберите технику из списка,\nзатем нажмите на слот (1-9) для назначения.";
            }
        }

        #endregion

        #region Helpers

        private void Close()
        {
            Hide();
        }

        #endregion
    }

    /// <summary>
    /// Запись техники в списке (простой компонент).
    /// </summary>
    public class TechniqueEntryUI : MonoBehaviour
    {
        public event Action<LearnedTechnique> OnClicked;

        private LearnedTechnique technique;
        private TMP_Text nameText;
        private TMP_Text masteryText;

        /// <summary>
        /// Настроить запись.
        /// </summary>
        public void Setup(LearnedTechnique tech, string displayName, float mastery)
        {
            technique = tech;

            // Найти текстовые элементы
            var texts = GetComponentsInChildren<TMP_Text>();
            if (texts.Length > 0) nameText = texts[0];
            if (texts.Length > 1) masteryText = texts[1];

            if (nameText != null) nameText.text = displayName;
            if (masteryText != null) masteryText.text = $"{mastery:F0}%";

            // Добавить кнопку если нет
            var button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => OnClicked?.Invoke(technique));
        }
    }
}

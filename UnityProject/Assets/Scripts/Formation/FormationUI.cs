// ============================================================================
// FormationUI.cs — Интерфейс управления формациями
// Cultivation World Simulator
// Версия: 1.1 — Замена UnityEngine.Input на Input System
// ============================================================================
// Создано: 2026-04-03 13:45:00 UTC
// Редактировано: 2026-04-13 12:08:04 UTC — замена Input.mousePosition на Mouse.current.position
// ============================================================================
//
// Источник: docs/FORMATION_SYSTEM.md
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Formation
{
    /// <summary>
    /// Состояние UI формаций.
    /// </summary>
    [Serializable]
    public struct FormationUIState
    {
        public bool isPlacingMode;
        public string selectedFormationId;
        public int activeFormationsCount;
        public int maxActiveFormations;
        public List<FormationInfo> activeFormationsInfo;
    }

    /// <summary>
    /// Информация об активной формации для UI.
    /// </summary>
    [Serializable]
    public struct FormationInfo
    {
        public string name;
        public FormationStage stage;
        public float fillPercent;
        public long currentQi;
        public long capacity;
        public string timeUntilDepleted;
    }

    /// <summary>
    /// Интерфейс управления формациями.
    /// 
    /// ┌─────────────────────────────────────────────────────────────────────────┐
    /// │                     ЭЛЕМЕНТЫ UI                                         │
    /// ├─────────────────────────────────────────────────────────────────────────┤
    /// │                                                                          │
    /// │   ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐   │
    /// │   │ Formation List  │     │ Formation Info  │     │ Active List     │   │
    /// │   │ (изученные)     │     │ (детали)        │     │ (в мире)        │   │
    /// │   └─────────────────┘     └─────────────────┘     └─────────────────┘   │
    /// │                                                                          │
    /// │   ┌─────────────────────────────────────────────────────────────────┐   │
    /// │   │ Placement Preview                                                │   │
    /// │   │ (превью при размещении)                                          │   │
    /// │   └─────────────────────────────────────────────────────────────────┘   │
    /// │                                                                          │
    /// │   Controls:                                                              │
    /// │   - Select Formation → показывает детали                                │
    /// │   - Place Button → входит в режим размещения                            │
    /// │   - Cancel Button → выходит из режима                                   │
    /// │   - Contribute Button → вносит Ци в выбранную                           │
    /// │                                                                          │
    /// └─────────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public class FormationUI : MonoBehaviour
    {
        #region UI References

        [Header("Main Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject placementPanel;
        [SerializeField] private GameObject infoPanel;

        [Header("Formation List")]
        [SerializeField] private Transform formationListContainer;
        [SerializeField] private GameObject formationListItemPrefab;

        [Header("Formation Info")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text sizeText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text radiusText;
        [SerializeField] private Image iconImage;

        [Header("Qi Info")]
        [SerializeField] private Slider qiFillSlider;
        [SerializeField] private TMP_Text qiText;
        [SerializeField] private TMP_Text timeText;

        [Header("Active Formations")]
        [SerializeField] private Transform activeListContainer;
        [SerializeField] private GameObject activeListItemPrefab;

        [Header("Buttons")]
        [SerializeField] private Button placeButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button contributeButton;
        [SerializeField] private Button closeButton;

        [Header("Placement Preview")]
        [SerializeField] private GameObject placementPreviewPrefab;
        [SerializeField] private Color previewColor = new Color(0, 1, 1, 0.3f);

        #endregion

        #region State

        private FormationController controller;
        private FormationData selectedFormation;
        private FormationCore selectedActiveCore;
        private bool isPlacingMode = false;
        private GameObject placementPreview;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            controller = FormationController.Instance;

            if (controller != null)
            {
                controller.OnSelectionChanged += HandleSelectionChanged;
                controller.OnFormationCreated += HandleFormationCreated;
                controller.OnFormationActivated += HandleFormationActivated;
                controller.OnFormationDepleted += HandleFormationDepleted;
            }
        }

        private void Start()
        {
            SetupButtons();
            RefreshFormationList();
            RefreshActiveList();

            if (mainPanel != null)
                mainPanel.SetActive(false);

            if (placementPanel != null)
                placementPanel.SetActive(false);
        }

        private void Update()
        {
            if (isPlacingMode)
            {
                UpdatePlacementPreview();
            }

            UpdateActiveFormationInfo();
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.OnSelectionChanged -= HandleSelectionChanged;
                controller.OnFormationCreated -= HandleFormationCreated;
                controller.OnFormationActivated -= HandleFormationActivated;
                controller.OnFormationDepleted -= HandleFormationDepleted;
            }
        }

        #endregion

        #region Setup

        private void SetupButtons()
        {
            if (placeButton != null)
                placeButton.onClick.AddListener(OnPlaceButtonClicked);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelButtonClicked);

            if (contributeButton != null)
                contributeButton.onClick.AddListener(OnContributeButtonClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        #endregion

        #region Panel Control

        /// <summary>
        /// Показать главный панель.
        /// </summary>
        public void Show()
        {
            if (mainPanel != null)
                mainPanel.SetActive(true);

            RefreshFormationList();
            RefreshActiveList();
        }

        /// <summary>
        /// Скрыть главный панель.
        /// </summary>
        public void Hide()
        {
            if (mainPanel != null)
                mainPanel.SetActive(false);

            ExitPlacementMode();
        }

        /// <summary>
        /// Переключить видимость.
        /// </summary>
        public void Toggle()
        {
            if (mainPanel != null && mainPanel.activeSelf)
                Hide();
            else
                Show();
        }

        #endregion

        #region Formation List

        /// <summary>
        /// Обновить список изученных формаций.
        /// </summary>
        public void RefreshFormationList()
        {
            if (controller == null || formationListContainer == null) return;

            // Очищаем список
            foreach (Transform child in formationListContainer)
            {
                Destroy(child.gameObject);
            }

            // Заполняем список
            foreach (var known in controller.KnownFormations)
            {
                CreateFormationListItem(known.data);
            }
        }

        private void CreateFormationListItem(FormationData data)
        {
            if (formationListItemPrefab == null) return;

            GameObject item = Instantiate(formationListItemPrefab, formationListContainer);

            // Настраиваем текст
            var nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = data.displayName;

            var levelText = item.transform.Find("LevelText")?.GetComponent<TMP_Text>();
            if (levelText != null)
                levelText.text = $"L{data.level}";

            var iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && data.icon != null)
                iconImage.sprite = data.icon;

            // Добавляем кнопку
            var button = item.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SelectFormation(data));
            }
        }

        #endregion

        #region Selection

        /// <summary>
        /// Выбрать формацию.
        /// </summary>
        public void SelectFormation(FormationData data)
        {
            selectedFormation = data;
            controller?.SelectFormation(data);
            UpdateFormationInfo(data);
        }

        private void HandleSelectionChanged(FormationData data)
        {
            selectedFormation = data;
            UpdateFormationInfo(data);
        }

        private void UpdateFormationInfo(FormationData data)
        {
            if (data == null)
            {
                if (infoPanel != null) infoPanel.SetActive(false);
                return;
            }

            if (infoPanel != null) infoPanel.SetActive(true);

            if (nameText != null)
                nameText.text = data.displayName;

            if (descriptionText != null)
                descriptionText.text = data.description;

            if (levelText != null)
                levelText.text = $"Уровень: {data.level}";

            if (typeText != null)
                typeText.text = $"Тип: {GetTypeDisplayName(data.formationType)}";

            if (sizeText != null)
                sizeText.text = $"Размер: {GetSizeDisplayName(data.size)}";

            if (costText != null)
                costText.text = $"Стоимость: {data.ContourQi:N0} Ци";

            if (radiusText != null)
                radiusText.text = $"Радиус: {data.effectRadius}м";

            if (iconImage != null && data.icon != null)
                iconImage.sprite = data.icon;

            // Кнопка размещения
            if (placeButton != null)
            {
                placeButton.interactable = !data.requiresCore;
            }
        }

        #endregion

        #region Active Formations List

        /// <summary>
        /// Обновить список активных формаций.
        /// </summary>
        public void RefreshActiveList()
        {
            if (controller == null || activeListContainer == null) return;

            // Очищаем список
            foreach (Transform child in activeListContainer)
            {
                Destroy(child.gameObject);
            }

            // Заполняем список
            foreach (var core in controller.ActiveCores)
            {
                if (core != null)
                {
                    CreateActiveListItem(core);
                }
            }
        }

        private void CreateActiveListItem(FormationCore core)
        {
            if (activeListItemPrefab == null) return;

            GameObject item = Instantiate(activeListItemPrefab, activeListContainer);

            // Настраиваем текст
            var nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = core.Data?.displayName ?? "Unknown";

            var statusText = item.transform.Find("StatusText")?.GetComponent<TMP_Text>();
            if (statusText != null)
                statusText.text = GetStageDisplayName(core.Stage);

            var slider = item.transform.Find("QiSlider")?.GetComponent<Slider>();
            if (slider != null)
                slider.value = core.QiPool.FillPercent;

            // Добавляем кнопку
            var button = item.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SelectActiveFormation(core));
            }
        }

        private void SelectActiveFormation(FormationCore core)
        {
            selectedActiveCore = core;
            controller?.SelectActiveFormation(core);
            UpdateActiveFormationDetails(core);
        }

        private void UpdateActiveFormationDetails(FormationCore core)
        {
            if (core == null) return;

            if (nameText != null)
                nameText.text = core.Data?.displayName ?? "Unknown";

            if (qiFillSlider != null)
                qiFillSlider.value = core.QiPool.FillPercent;

            if (qiText != null)
                qiText.text = $"{core.QiPool.currentQi:N0} / {core.QiPool.capacity:N0}";

            if (timeText != null)
                timeText.text = $"До истощения: {core.QiPool.GetTimeUntilDepletedFormatted()}";
        }

        private void UpdateActiveFormationInfo()
        {
            if (selectedActiveCore != null && selectedActiveCore.IsActive)
            {
                UpdateActiveFormationDetails(selectedActiveCore);
            }
        }

        #endregion

        #region Placement Mode

        /// <summary>
        /// Войти в режим размещения.
        /// </summary>
        public void EnterPlacementMode()
        {
            if (selectedFormation == null) return;

            isPlacingMode = true;

            if (placementPanel != null)
                placementPanel.SetActive(true);

            // Создаём превью
            if (placementPreviewPrefab != null)
            {
                placementPreview = Instantiate(placementPreviewPrefab);
            }
            else
            {
                // Создаём простой круг
                placementPreview = CreateDefaultPreview();
            }

            Debug.Log($"[FormationUI] Режим размещения: {selectedFormation.displayName}");
        }

        /// <summary>
        /// Выйти из режима размещения.
        /// </summary>
        public void ExitPlacementMode()
        {
            isPlacingMode = false;

            if (placementPanel != null)
                placementPanel.SetActive(false);

            if (placementPreview != null)
            {
                Destroy(placementPreview);
                placementPreview = null;
            }
        }

        private void UpdatePlacementPreview()
        {
            if (placementPreview == null) return;

            // Получаем позицию мыши (Input System)
            Vector3 mousePos = Mouse.current != null ? (Vector3)Mouse.current.position.value : Vector3.zero;
            mousePos.z = Camera.main.nearClipPlane;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0;

            // Перемещаем превью
            placementPreview.transform.position = worldPos;

            // Обновляем размер
            var sr = placementPreview.GetComponent<SpriteRenderer>();
            if (sr != null && selectedFormation != null)
            {
                float scale = selectedFormation.effectRadius * 2 / sr.bounds.size.x;
                placementPreview.transform.localScale = new Vector3(scale, scale, 1);
            }

            // Проверяем клик
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                PlaceFormation(worldPos);
            }

            // Проверяем правый клик / Escape
            if ((Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ||
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                ExitPlacementMode();
            }
        }

        private void PlaceFormation(Vector2 position)
        {
            if (selectedFormation == null || controller == null) return;

            FormationCore core = controller.CreateWithoutCore(selectedFormation, position);

            if (core != null)
            {
                // Начинаем наполнение
                controller.StartFilling(core);
                ExitPlacementMode();
            }
        }

        private GameObject CreateDefaultPreview()
        {
            GameObject preview = new GameObject("PlacementPreview");

            // Добавляем SpriteRenderer с кругом
            var sr = preview.AddComponent<SpriteRenderer>();

            // Создаём текстуру круга
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            Color[] colors = new Color[size * size];

            Vector2 center = new Vector2(size / 2, size / 2);
            float radius = size / 2 - 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius && dist >= radius - 2)
                        colors[y * size + x] = previewColor;
                    else
                        colors[y * size + x] = Color.clear;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            sr.sortingOrder = 1000;

            return preview;
        }

        #endregion

        #region Button Handlers

        private void OnPlaceButtonClicked()
        {
            if (selectedFormation != null && !selectedFormation.requiresCore)
            {
                EnterPlacementMode();
            }
        }

        private void OnCancelButtonClicked()
        {
            ExitPlacementMode();
        }

        private void OnContributeButtonClicked()
        {
            if (selectedActiveCore == null || controller == null) return;

            // Вносим Ци
            long maxContribution = 1000; // Максимум за клик
            long contributed = controller.ContributeQi(selectedActiveCore, maxContribution); // FIX: long

            Debug.Log($"[FormationUI] Внесено Ци: {contributed}");
        }

        private void OnCloseButtonClicked()
        {
            Hide();
        }

        #endregion

        #region Event Handlers

        private void HandleFormationCreated(FormationCore core)
        {
            RefreshActiveList();
        }

        private void HandleFormationActivated(FormationCore core)
        {
            RefreshActiveList();
        }

        private void HandleFormationDepleted(FormationCore core)
        {
            RefreshActiveList();
        }

        #endregion

        #region Utility

        private string GetTypeDisplayName(FormationType type)
        {
            return type switch
            {
                FormationType.Barrier => "Барьер",
                FormationType.Trap => "Ловушка",
                FormationType.Amplification => "Усиление",
                FormationType.Suppression => "Подавление",
                FormationType.Gathering => "Сбор",
                FormationType.Detection => "Обнаружение",
                FormationType.Teleportation => "Телепортация",
                FormationType.Summoning => "Призыв",
                _ => type.ToString()
            };
        }

        private string GetSizeDisplayName(FormationSize size)
        {
            return size switch
            {
                FormationSize.Small => "Малый",
                FormationSize.Medium => "Средний",
                FormationSize.Large => "Большой",
                FormationSize.Great => "Великий",
                FormationSize.Heavy => "Тяжёлый",
                _ => size.ToString()
            };
        }

        private string GetStageDisplayName(FormationStage stage)
        {
            return stage switch
            {
                FormationStage.None => "Нет",
                FormationStage.Drawing => "Прорисовка",
                FormationStage.Filling => "Наполнение",
                FormationStage.Active => "Активна",
                FormationStage.Depleted => "Истощена",
                _ => stage.ToString()
            };
        }

        #endregion
    }
}

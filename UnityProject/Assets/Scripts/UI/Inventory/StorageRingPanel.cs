// ============================================================================
// StorageRingPanel.cs — Панель кольца хранения (каталогизатор)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-19 15:00:00 UTC
// ============================================================================
// Отображает содержимое кольца хранения как каталог (список с группировкой).
// Объём-ограниченное хранилище, стоимость Qi = qiCostBase + volume × qiCostPerUnit.
// Поддерживает: фильтрацию, поиск, извлечение в инвентарь.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Inventory;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.UI.Inventory
{
    /// <summary>
    /// Панель кольца хранения — каталогизатор содержимого.
    /// Переключение между слотами колец (RingLeft1/2, RingRight1/2).
    /// </summary>
    public class StorageRingPanel : MonoBehaviour
    {
        #region Configuration

        [Header("Ring Info")]
        [SerializeField] private TMP_Text ringNameText;
        [SerializeField] private TMP_Text volumeText;
        [SerializeField] private Slider volumeBar;
        [SerializeField] private TMP_Text ringSlotLabel;

        [Header("Filters")]
        [SerializeField] private TMP_Dropdown categoryFilter;
        [SerializeField] private TMP_Dropdown rarityFilter;
        [SerializeField] private TMP_InputField searchField;

        [Header("Content")]
        [SerializeField] private Transform contentContainer;
        [SerializeField] private GameObject entryRowPrefab;
        [SerializeField] private GameObject categoryHeaderPrefab;

        [Header("Actions")]
        [SerializeField] private Button retrieveButton;
        [SerializeField] private Button retrieveAllButton;

        [Header("Cost Display")]
        [SerializeField] private TMP_Text qiCostText;
        [SerializeField] private TMP_Text accessTimeText;

        [Header("Messages")]
        [SerializeField] private GameObject noRingMessage;
        [SerializeField] private GameObject emptyRingMessage;

        [Header("Ring Slot Selector")]
        [SerializeField] private Transform ringSlotSelectorContainer;
        [SerializeField] private Button ringSlotButtonPrefab;

        #endregion

        #region Runtime Data

        private StorageRingController ringController;
        private InventoryController inventoryController;
        private EquipmentSlot selectedRingSlot = EquipmentSlot.None;
        private StorageRingEntry selectedEntry = null;
        private List<StorageRingEntry> displayedEntries = new List<StorageRingEntry>();
        private Dictionary<EquipmentSlot, Button> ringSlotButtons = new Dictionary<EquipmentSlot, Button>();
        private bool isInitialized = false;

        #endregion

        #region Properties

        /// <summary>Текущий выбранный слот кольца</summary>
        public EquipmentSlot SelectedRingSlot => selectedRingSlot;

        /// <summary>Есть ли активное кольцо хранения</summary>
        public bool HasActiveRing => selectedRingSlot != EquipmentSlot.None &&
                                      ringController != null &&
                                      ringController.IsRingSlotActive(selectedRingSlot);

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализирует панель кольца хранения.
        /// </summary>
        public void Initialize(StorageRingController ringCtrl, InventoryController invCtrl)
        {
            ringController = ringCtrl;
            inventoryController = invCtrl;

            SetupFilters();
            SetupRingSlotButtons();
            SubscribeToEvents();
            SetupButtons();

            isInitialized = true;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SetupFilters()
        {
            // Категории
            if (categoryFilter != null)
            {
                categoryFilter.ClearOptions();
                categoryFilter.options.Add(new TMP_Dropdown.OptionData("Все"));
                foreach (ItemCategory cat in Enum.GetValues(typeof(ItemCategory)))
                {
                    if (cat != ItemCategory.Misc || true) // Показываем все
                        categoryFilter.options.Add(new TMP_Dropdown.OptionData(GetCategoryDisplayName(cat)));
                }
                categoryFilter.value = 0;
                categoryFilter.onValueChanged.AddListener(_ => RefreshContents());
            }

            // Редкость
            if (rarityFilter != null)
            {
                rarityFilter.ClearOptions();
                rarityFilter.options.Add(new TMP_Dropdown.OptionData("Все"));
                foreach (ItemRarity rar in Enum.GetValues(typeof(ItemRarity)))
                {
                    rarityFilter.options.Add(new TMP_Dropdown.OptionData(GetRarityDisplayName(rar)));
                }
                rarityFilter.value = 0;
                rarityFilter.onValueChanged.AddListener(_ => RefreshContents());
            }

            // Поиск
            if (searchField != null)
            {
                searchField.onValueChanged.AddListener(_ => RefreshContents());
            }
        }

        private void SetupRingSlotButtons()
        {
            if (ringSlotSelectorContainer == null || ringSlotButtonPrefab == null) return;

            // Удаляем старые кнопки
            for (int i = ringSlotSelectorContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(ringSlotSelectorContainer.GetChild(i).gameObject);
            }
            ringSlotButtons.Clear();

            // Создаём кнопки для каждого слота кольца
            foreach (var slot in StorageRingController.RingSlots)
            {
                var btnGO = Instantiate(ringSlotButtonPrefab.gameObject, ringSlotSelectorContainer);
                var btn = btnGO.GetComponent<Button>();
                var btnText = btnGO.GetComponentInChildren<TMP_Text>();

                if (btnText != null)
                    btnText.text = GetRingSlotDisplayName(slot);

                btn.onClick.AddListener(() => SelectRingSlot(slot));
                ringSlotButtons[slot] = btn;
            }
        }

        private void SetupButtons()
        {
            if (retrieveButton != null)
                retrieveButton.onClick.AddListener(OnRetrieveClicked);

            if (retrieveAllButton != null)
                retrieveAllButton.onClick.AddListener(OnRetrieveAllClicked);
        }

        private void SubscribeToEvents()
        {
            if (ringController == null) return;

            ringController.OnItemStored += OnItemStored;
            ringController.OnItemRetrieved += OnItemRetrieved;
            ringController.OnRingStorageActivated += OnRingStorageActivated;
            ringController.OnRingStorageDeactivated += OnRingStorageDeactivated;
            ringController.OnContentsChanged += OnContentsChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (ringController == null) return;

            ringController.OnItemStored -= OnItemStored;
            ringController.OnItemRetrieved -= OnItemRetrieved;
            ringController.OnRingStorageActivated -= OnRingStorageActivated;
            ringController.OnRingStorageDeactivated -= OnRingStorageDeactivated;
            ringController.OnContentsChanged -= OnContentsChanged;
        }

        #endregion

        #region Ring Slot Selection

        /// <summary>
        /// Выбрать слот кольца для отображения.
        /// </summary>
        public void SelectRingSlot(EquipmentSlot slot)
        {
            selectedRingSlot = slot;
            selectedEntry = null;

            // Обновляем подсветку кнопок
            foreach (var kvp in ringSlotButtons)
            {
                var colors = kvp.Value.colors;
                colors.normalColor = kvp.Key == slot ? new Color(0.3f, 0.6f, 0.9f) : Color.white;
                kvp.Value.colors = colors;
            }

            RefreshContents();
        }

        /// <summary>
        /// Получить список активных слотов колец.
        /// </summary>
        public List<EquipmentSlot> GetActiveRingSlots()
        {
            var result = new List<EquipmentSlot>();
            if (ringController == null) return result;

            foreach (var slot in StorageRingController.RingSlots)
            {
                if (ringController.IsRingSlotActive(slot))
                    result.Add(slot);
            }
            return result;
        }

        /// <summary>
        /// Автоматически выбрать первый активный слот кольца.
        /// </summary>
        public void AutoSelectFirstActiveRing()
        {
            var activeSlots = GetActiveRingSlots();
            if (activeSlots.Count > 0)
            {
                SelectRingSlot(activeSlots[0]);
            }
            else
            {
                selectedRingSlot = EquipmentSlot.None;
                selectedEntry = null;
                RefreshContents();
            }
        }

        #endregion

        #region Refresh

        /// <summary>
        /// Обновляет всё содержимое панели.
        /// </summary>
        public void RefreshContents()
        {
            if (!isInitialized) return;

            UpdateRingInfo();
            UpdateVolumeDisplay();
            ApplyFilters();
            UpdateActionButtons();
            UpdateCostDisplay();
        }

        private void UpdateRingInfo()
        {
            bool hasRing = selectedRingSlot != EquipmentSlot.None &&
                           ringController != null &&
                           ringController.IsRingSlotActive(selectedRingSlot);

            // Сообщение "нет кольца"
            if (noRingMessage != null)
                noRingMessage.SetActive(!hasRing);

            // Сообщение "пустое кольцо"
            if (emptyRingMessage != null)
                emptyRingMessage.SetActive(hasRing && ringController.GetEntries(selectedRingSlot).Count == 0);

            // Информация о кольце
            if (hasRing)
            {
                var ringData = ringController.GetActiveRing(selectedRingSlot);

                if (ringNameText != null)
                    ringNameText.text = ringData?.nameRu ?? "Кольцо хранения";

                if (ringSlotLabel != null)
                    ringSlotLabel.text = GetRingSlotDisplayName(selectedRingSlot);
            }
            else
            {
                if (ringNameText != null)
                    ringNameText.text = "Нет кольца";

                if (ringSlotLabel != null)
                    ringSlotLabel.text = "";
            }
        }

        private void UpdateVolumeDisplay()
        {
            if (ringController == null || selectedRingSlot == EquipmentSlot.None) return;

            float currentVol = ringController.GetCurrentVolume(selectedRingSlot);
            float maxVol = ringController.GetMaxVolume(selectedRingSlot);

            if (volumeText != null)
                volumeText.text = $"{currentVol:F1} / {maxVol:F1} об.";

            if (volumeBar != null)
            {
                volumeBar.maxValue = maxVol > 0 ? maxVol : 1f;
                volumeBar.value = currentVol;
            }

            // Цвет полосы объёма
            if (volumeText != null)
            {
                float percent = maxVol > 0 ? currentVol / maxVol : 0f;
                if (percent > 0.9f)
                    volumeText.color = new Color(1f, 0.3f, 0.3f); // Красный
                else if (percent > 0.7f)
                    volumeText.color = new Color(1f, 0.7f, 0.2f); // Оранжевый
                else
                    volumeText.color = Color.white;
            }
        }

        private void UpdateActionButtons()
        {
            if (retrieveButton != null)
                retrieveButton.interactable = selectedEntry != null && HasActiveRing;

            if (retrieveAllButton != null)
                retrieveAllButton.interactable = selectedEntry != null && HasActiveRing;
        }

        private void UpdateCostDisplay()
        {
            if (!HasActiveRing)
            {
                if (qiCostText != null) qiCostText.text = "";
                if (accessTimeText != null) accessTimeText.text = "";
                return;
            }

            if (selectedEntry != null)
            {
                long cost = ringController.GetRetrievalCost(selectedRingSlot, selectedEntry.ItemVolume);
                if (qiCostText != null)
                    qiCostText.text = $"Стоимость: {cost} Ци";
            }
            else
            {
                if (qiCostText != null)
                    qiCostText.text = "Выберите предмет";
            }

            float accessTime = ringController.GetAccessTime(selectedRingSlot);
            if (accessTimeText != null)
                accessTimeText.text = $"Время доступа: {accessTime:F1} сек";
        }

        #endregion

        #region Content Building

        /// <summary>
        /// Применяет фильтры и перестраивает список предметов.
        /// </summary>
        public void ApplyFilters()
        {
            if (contentContainer != null)
            {
                for (int i = contentContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(contentContainer.GetChild(i).gameObject);
                }
            }

            displayedEntries.Clear();

            if (!HasActiveRing) return;

            // Получаем содержимое с учётом фильтров
            var entries = GetFilteredEntries();

            if (entries.Count == 0) return;

            // Группируем по категории
            var grouped = new Dictionary<ItemCategory, List<StorageRingEntry>>();
            foreach (var entry in entries)
            {
                var cat = entry.Category;
                if (!grouped.ContainsKey(cat))
                    grouped[cat] = new List<StorageRingEntry>();
                grouped[cat].Add(entry);
            }

            // Создаём визуальные элементы
            foreach (var group in grouped)
            {
                // Заголовок категории
                CreateCategoryHeader(group.Key, group.Value.Count);

                // Записи
                foreach (var entry in group.Value)
                {
                    CreateEntryRow(entry);
                    displayedEntries.Add(entry);
                }
            }
        }

        private List<StorageRingEntry> GetFilteredEntries()
        {
            if (ringController == null) return new List<StorageRingEntry>();

            // Начинаем с поиска (если есть текст)
            string query = searchField != null ? searchField.text.Trim() : "";
            List<StorageRingEntry> entries;

            if (!string.IsNullOrEmpty(query))
            {
                entries = ringController.Search(selectedRingSlot, query);
            }
            else
            {
                entries = ringController.GetContents(selectedRingSlot);
            }

            // Фильтр по категории
            if (categoryFilter != null && categoryFilter.value > 0)
            {
                int catIndex = categoryFilter.value - 1;
                ItemCategory[] categories = (ItemCategory[])Enum.GetValues(typeof(ItemCategory));
                if (catIndex < categories.Length)
                {
                    ItemCategory targetCat = categories[catIndex];
                    entries = entries.Where(e => e.Category == targetCat).ToList();
                }
            }

            // Фильтр по редкости
            if (rarityFilter != null && rarityFilter.value > 0)
            {
                int rarIndex = rarityFilter.value - 1;
                ItemRarity[] rarities = (ItemRarity[])Enum.GetValues(typeof(ItemRarity));
                if (rarIndex < rarities.Length)
                {
                    ItemRarity targetRar = rarities[rarIndex];
                    entries = entries.Where(e => e.Rarity == targetRar).ToList();
                }
            }

            return entries;
        }

        private void CreateCategoryHeader(ItemCategory category, int count)
        {
            if (contentContainer == null) return;

            GameObject headerGO;
            if (categoryHeaderPrefab != null)
            {
                headerGO = Instantiate(categoryHeaderPrefab, contentContainer);
            }
            else
            {
                headerGO = new GameObject("CategoryHeader");
                headerGO.transform.SetParent(contentContainer, false);
                var text = headerGO.AddComponent<TMP_Text>();
                // TMP_Text без prefab — нужен_TMP component
            }

            var headerText = headerGO.GetComponentInChildren<TMP_Text>();
            if (headerText != null)
                headerText.text = $"─ {GetCategoryDisplayName(category)} ({count}) ─";
        }

        private void CreateEntryRow(StorageRingEntry entry)
        {
            if (contentContainer == null) return;

            GameObject rowGO;
            if (entryRowPrefab != null)
            {
                rowGO = Instantiate(entryRowPrefab, contentContainer);
            }
            else
            {
                rowGO = new GameObject("EntryRow");
                rowGO.transform.SetParent(contentContainer, false);
            }

            // Иконка
            var iconImage = rowGO.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && entry.Icon != null)
            {
                iconImage.sprite = entry.Icon;
                iconImage.enabled = true;
            }

            // Название
            var nameText = rowGO.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = entry.NameRu;
                nameText.color = InventorySlotUI.GetRarityColor(entry.Rarity);
            }

            // Количество
            var countText = rowGO.transform.Find("CountText")?.GetComponent<TMP_Text>();
            if (countText != null)
                countText.text = $"x{entry.count}";

            // Объём
            var volumeTextRow = rowGO.transform.Find("VolumeText")?.GetComponent<TMP_Text>();
            if (volumeTextRow != null)
                volumeTextRow.text = $"{entry.totalVolume:F1} об.";

            // Вес
            var weightTextRow = rowGO.transform.Find("WeightText")?.GetComponent<TMP_Text>();
            if (weightTextRow != null)
                weightTextRow.text = $"{entry.totalWeight:F1} кг";

            // Клик по записи
            var button = rowGO.GetComponent<Button>();
            if (button == null)
                button = rowGO.AddComponent<Button>();

            button.onClick.AddListener(() => OnEntryClicked(entry));

            // Подсветка выбранного
            if (selectedEntry != null && selectedEntry.entryId == entry.entryId)
            {
                var images = rowGO.GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    if (img.gameObject == rowGO)
                    {
                        img.color = new Color(0.2f, 0.4f, 0.7f, 0.3f);
                        break;
                    }
                }
            }
        }

        #endregion

        #region Event Handlers (from StorageRingController)

        private void OnItemStored(StorageRingEntry entry)
        {
            RefreshContents();
        }

        private void OnItemRetrieved(StorageRingEntry entry, int count)
        {
            // Если извлечён выбранный предмет — сбрасываем выделение
            if (selectedEntry != null && selectedEntry.entryId == entry.entryId)
                selectedEntry = null;

            RefreshContents();
        }

        private void OnRingStorageActivated(EquipmentSlot slot, StorageRingData ringData)
        {
            // Если это первый кольцо — автоматически выбираем его
            if (selectedRingSlot == EquipmentSlot.None)
                SelectRingSlot(slot);

            RefreshContents();
        }

        private void OnRingStorageDeactivated(EquipmentSlot slot)
        {
            if (selectedRingSlot == slot)
            {
                AutoSelectFirstActiveRing();
            }
            else
            {
                RefreshContents();
            }
        }

        private void OnContentsChanged(EquipmentSlot slot)
        {
            if (slot == selectedRingSlot)
                RefreshContents();
        }

        #endregion

        #region Entry Interaction

        private void OnEntryClicked(StorageRingEntry entry)
        {
            selectedEntry = entry;
            RefreshContents();
        }

        #endregion

        #region Actions

        /// <summary>
        /// Извлечь выбранный предмет в инвентарь.
        /// </summary>
        public void OnRetrieveClicked()
        {
            if (selectedEntry == null || ringController == null || !HasActiveRing) return;

            var result = ringController.RetrieveToInventory(selectedRingSlot, selectedEntry.entryId);
            if (result != null)
            {
                Debug.Log($"[StorageRingPanel] Извлечено: {selectedEntry.NameRu} x{selectedEntry.count}");
            }

            selectedEntry = null;
        }

        /// <summary>
        /// Извлечь все предметы того же типа в инвентарь.
        /// </summary>
        public void OnRetrieveAllClicked()
        {
            if (selectedEntry == null || ringController == null || !HasActiveRing) return;

            string itemId = selectedEntry.itemId;
            var allEntries = ringController.FindEntriesByItemId(selectedRingSlot, itemId);

            foreach (var entry in allEntries)
            {
                ringController.RetrieveToInventory(selectedRingSlot, entry.entryId);
            }

            selectedEntry = null;
        }

        #endregion

        #region Show / Hide

        /// <summary>
        /// Показать панель.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            AutoSelectFirstActiveRing();
        }

        /// <summary>
        /// Скрыть панель.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            selectedEntry = null;
        }

        #endregion

        #region Display Name Helpers

        private string GetRingSlotDisplayName(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.RingLeft1 => "Кольцо Л1",
                EquipmentSlot.RingLeft2 => "Кольцо Л2",
                EquipmentSlot.RingRight1 => "Кольцо П1",
                EquipmentSlot.RingRight2 => "Кольцо П2",
                _ => slot.ToString()
            };
        }

        private string GetCategoryDisplayName(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Weapon => "Оружие",
                ItemCategory.Armor => "Броня",
                ItemCategory.Accessory => "Аксессуар",
                ItemCategory.Consumable => "Расходники",
                ItemCategory.Material => "Материалы",
                ItemCategory.Technique => "Свитки",
                ItemCategory.Quest => "Квестовые",
                ItemCategory.Misc => "Разное",
                _ => category.ToString()
            };
        }

        private string GetRarityDisplayName(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => "Обычный",
                ItemRarity.Uncommon => "Необычный",
                ItemRarity.Rare => "Редкий",
                ItemRarity.Epic => "Эпический",
                ItemRarity.Legendary => "Легендарный",
                ItemRarity.Mythic => "Мифический",
                _ => rarity.ToString()
            };
        }

        #endregion
    }
}

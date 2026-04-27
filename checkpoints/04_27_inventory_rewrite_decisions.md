# ⚖️ Чекпоинт: Решения — полная перепись vs редактирование

**Дата:** 2026-04-27 18:01 UTC
**Статус:** 📋 Решения приняты (код НЕ писать до отдельной задачи)
**Цель:** Определить для каждого модуля — полная перепись или редактирование
**Принцип:** Легаси-код неприемлем — неизвестно, когда он будет переработан

---

## 🎯 КРИТЕРИЙ РЕШЕНИЯ

| Условие | Вердикт |
|---------|---------|
| Сетка = ядро модели данных, вырезание = удаление 40%+ | 🔄 ПОЛНАЯ ПЕРЕПИСЬ |
| Визуальная парадигма меняется (сетка → список) | 🔄 ПОЛНАЯ ПЕРЕПИСЬ |
| Критические баги + TODO + неполный функционал | 🔄 ПОЛНАЯ ПЕРЕПИСЬ |
| Сеточные строки < 15%, чистое отделение | ✏️ РЕДАКТИРОВАНИЕ |
| 0% сетки, только минорные фиксы | ✏️ РЕДАКТИРОВАНИЕ |
| 0% сетки, нет багов, не связано с рюкзаком | 🚫 НЕ ТРОГАТЬ |

---

## 📊 СВОДНАЯ ТАБЛИЦА РЕШЕНИЙ

| # | Файл | Строк | Сетка % | Вердикт | Обоснование |
|---|------|------:|--------:|---------|-------------|
| 1 | InventoryController.cs | 1003 | 42% | 🔄 ПЕРЕПИСЬ | gridSlotIds[,] — ядро модели, сетка пронизывает все операции |
| 2 | BackpackPanel.cs | 504 | 65% | 🔄 ПЕРЕПИСЬ | Визуальная парадигма: GridLayoutGroup → VerticalLayoutGroup |
| 3 | InventorySlotUI.cs | 391 | 14% | 🔄 ПЕРЕПИСЬ | Концепт меняется: ячейка сетки → строка списка (иная форма, иные данные) |
| 4 | DragDropHandler.cs | 657 | 14% | 🔄 ПЕРЕПИСЬ | BUG-3+BUG-6+BUG-7, TODO, координатная система меняется, отсутствует drag между вкладками |
| 5 | ItemData.cs | 146 | 7% | ✏️ РЕДАКТИРОВАНИЕ | Удалить sizeWidth/sizeHeight (10 строк) |
| 6 | BackpackData.cs | 50 | 36% | ✏️ РЕДАКТИРОВАНИЕ | Заменить 3 поля, добавить 3 поля (малый файл) |
| 7 | EquipmentData.cs | 77 | 0% | ✏️ РЕДАКТИРОВАНИЕ | Удалить layers (1 поле) |
| 8 | EquipmentController.cs | 931 | 0% | ✏️ РЕДАКТИРОВАНИЕ | Удалить EquipmentSlotsUI (мёртвый код) + BUG-8 фикс |
| 9 | InventoryScreen.cs | 343 | 4% | ✏️ РЕДАКТИРОВАНИЕ | RebuildGrid() → RefreshList(), типизировать spiritStoragePanel |
| 10 | TooltipPanel.cs | 536 | 0% | ✏️ РЕДАКТИРОВАНИЕ | Убрать отображение sizeWidth/sizeHeight |
| 11 | SpiritStorageController.cs | 883 | 1% | ✏️ РЕДАКТИРОВАНИЕ | HasFreeSpace(w,h) → CanFitItem() (2 вызова) |
| 12 | StorageRingController.cs | 960 | 1% | ✏️ РЕДАКТИРОВАНИЕ | HasFreeSpace(w,h) → CanFitItem() (2 вызова) |
| 13 | CraftingController.cs | 730 | 1% | ✏️ РЕДАКТИРОВАНИЕ | FreeCells + sizeW*sizeH → CanFitItem() (2 строки) |
| 14 | Phase16InventoryData.cs | 632 | 14% | ✏️ РЕДАКТИРОВАНИЕ | Обновить генераторы ассетов (grid→weight/volume) |
| 15 | Phase17InventoryUI.cs | ~1000 | 25% | ✏️ СУЩ.РЕДАКТ. | Переписать блок Backpack, остальное сохранить |
| 16 | Phase18InventoryComponents.cs | 164 | 0% | ✏️ РЕДАКТИРОВАНИЕ | Обновить параметры стартового рюкзака |
| 17 | SaveManager.cs | 841 | 0% | ✏️ РЕДАКТИРОВАНИЕ | Добавить 3 поля + вызовы (критический фикс BUG-1/2) |
| 18 | SaveDataTypes.cs | 513 | 0% | ✏️ РЕДАКТИРОВАНИЕ | Добавить типы данных для инвентаря/экипировки/крафта |
| 19 | Enums.cs | 736 | 0% | ✏️ РЕДАКТИРОВАНИЕ | Удалить SizeClass (если есть) |
| 20 | BodyDollPanel.cs | 560 | 0% | 🚫 НЕ ТРОГАТЬ | Кукла — не связана с рюкзаком |
| 21 | StorageRingPanel.cs | 729 | 0% | ✏️ РЕДАКТИРОВАНИЕ | BUG-12: TMP_Text → TextMeshProUGUI |
| 22 | StorageRingData.cs | 47 | 0% | 🚫 НЕ ТРОГАТЬ | На объёме, не сетке |
| 23 | MaterialSystem.cs | 549 | 0% | 🚫 НЕ ТРОГАТЬ | Совершенно не связано |
| 24 | InventoryUI.cs (legacy) | 874 | 15% | 🗑️ УДАЛИТЬ | Устаревший v1 UI, заменён v2 |

---

## 🔄 ДЕТАЛИ ПОЛНОЙ ПЕРЕПИСИ (4 файла)

### 1. InventoryController.cs — 🔄 ПЕРЕПИСЬ (1003 → ~600 строк)

**Почему перепись, а не редактирование:**
- `gridSlotIds[,]` — фундаментальная структура данных, пронизывает 15+ методов
- Вырезать сетку = удалить 420 строк + переписать оставшиеся 580 (все они вызывают сеточные методы)
- Результат редактирования = лоскутное одеяло из старого + нового кода
- BUG-5 (O(N) поиск) — архитектурная проблема, решается при переписи

**Что СОХРАНИТЬ при переписи (перенести в новый файл):**
- Весовая система: EffectiveWeight, MaxWeight, WeightPercent, IsOverencumbered
- Система событий: OnItemAdded/Removed/StackChanged/WeightChanged/BackpackChanged
- Стекование: логика добавления в существующий стак
- Запросы: FindSlotById (→ Dictionary), CountItem, HasItem
- Прочность/грейд на InventorySlot
- Save/Load формат (без gridX/gridY)

**Что УБРАТЬ навсегда:**
- gridSlotIds[,] и всё, что с ним работает (15 методов)
- InventorySlot.GridX, GridY, ItemWidth, ItemHeight, SetPosition()
- AddItemAt (координатный), MoveItem(slotId, x, y), SwapSlots
- FindFreePosition, IsAreaFree, OccupyGrid, ClearSlotFromGrid, RebuildGridFromSlots

**Что ДОБАВИТЬ:**
- CanFitItem(ItemData, count) → проверка по mass+volume
- totalVolume, maxVolume (вычисляемые из BackpackData)
- SwapRows(index1, index2)
- Dictionary<int, InventorySlot> для O(1) поиска
- rowIndex вместо GridX/GridY

**Новая модель InventorySlot:**
```
SlotId: int, ItemData, Count, durability, grade, rowIndex
// УБРАНО: GridX, GridY, ItemWidth, ItemHeight, SetPosition()
```

---

### 2. BackpackPanel.cs — 🔄 ПЕРЕПИСЬ (504 → ~350 строк)

**Почему перепись:**
- 65% кода — сеточная визуализация (gridCells, PlaceItemInGrid, ScreenToGridPosition)
- Парадигма UI полностью меняется: GridLayoutGroup → VerticalLayoutGroup + ScrollRect
- BUG-4: RebuildGrid Destroy+Instantiate → нормальный RefreshList
- «Скрытие ячеек под предметом» — концепция сетки, не нужна в списке

**Что СОХРАНИТЬ:**
- Подписки на события InventoryController
- Отображение веса/названия рюкзака
- Цветовая схема (редкость)

**Что УБРАТЬ:**
- gridCells (Dictionary<string, InventorySlotUI> по "x,y")
- RebuildGrid, ClearGrid, CreateGridCells
- PlaceItemInGrid, RemoveItemFromGrid
- CalculateCellPosition, CalculateItemSize
- ScreenToGridPosition, IsAreaFree
- HighlightValidDrop, ClearAllHighlights
- cellSize, cellSpacing, padding, gridContainer, gridBackground

**Что ДОБАВИТЬ:**
- RefreshList() — delta-update или pool
- Строка списка: иконка + название + кол-во + вес + объём
- Индикаторы: вес (текущий/макс) + объём (текущий/макс)
- Подсветка при наведении (hover)
- Цвет строки по редкости

---

### 3. InventorySlotUI.cs — 🔄 ПЕРЕПИСЬ (391 → ~250 строк)

**Почему перепись:**
- Концепт меняется: «ячейка сетки с позицией» → «строка списка с данными»
- GridX/GridY свойства → rowIndex
- Визуальное отображение: квадратная ячейка с иконкой → горизонтальная строка (иконка + текст + вес + объём)
- SetPosition/SetSize (RectTransform для сетки) → VerticalLayoutGroup управляет позицией

**Что СОХРАНИТЬ:**
- IPointerEnterHandler, IPointerExitHandler (tooltip)
- IBeginDragHandler, IDragHandler, IEndDragHandler (drag)
- IPointerClickHandler (контекстное меню)
- GetRarityColor() — цвет рамки/фона
- CanvasGroup для drag-состояния
- Иконка, рамка, текст количества

**Что УБРАТЬ:**
- GridX, GridY свойства
- InitializeEmpty(gridX, gridY) → InitializeEmpty()
- SetPosition(), SetSize() (RectTransform-манипуляции для сетки)
- emptyIcon (фон пустой ячейки) → в списке нет пустых строк

**Что ДОБАВИТЬ:**
- itemNameText (TextMeshProUGUI)
- weightText (TextMeshProUGUI)
- volumeText (TextMeshProUGUI)
- rowIndex: int
- Initialize(ItemData, count, weight, volume, rowIndex)

---

### 4. DragDropHandler.cs — 🔄 ПЕРЕПИСЬ (657 → ~500 строк)

**Почему перепись:**
- BUG-3: SplitStack теряет durability/grade — нужно переписать SplitStack
- BUG-6: HandleDropOnBackpack для DollSlot — TODO, не реализовано
- BUG-7: DragSource не включает StorageRing/SpiritStorage
- Координатная система: ScreenToGridPosition → индекс строки
- Отсутствует drag между вкладками — нужен новый код

**Что СОХРАНИТЬ:**
- Определение DropTarget (DetermineDropTarget) — расширить
- Обработка куклы (HandleDropOnDoll, DropOnDollSlot)
- Drag-визуал (SetupDragVisual)
- Контекстное меню (ShowContextMenu, GetInventoryContextMenuOptions)
- Tooltip-интеграция

**Что УБРАТЬ:**
- HandleDropOnBackpack с координатами (ScreenToGridPosition, MoveItem(x,y))
- OnCellHover/OnCellExit (сеточные подсветки)
- HighlightValidDrop с ItemWidth/ItemHeight

**Что ДОБАВИТЬ:**
- DragSource: StorageRing, SpiritStorage
- DropTarget: SpiritStorageList, StorageRingList
- HandleDropOnBackpack для DollSlot (реализация TODO)
- SwapRows вместо MoveItem по координатам
- SplitStack с сохранением durability/grade
- Перетаскивание между вкладками (Backpack ↔ Spirit ↔ Ring)

---

## ✏️ ДЕТАЛИ РЕДАКТИРОВАНИЯ (14 файлов)

### 5. ItemData.cs — ✏️ Удалить sizeWidth/sizeHeight
- Удалить поле `sizeWidth` (Range 1..2, default 1)
- Удалить поле `sizeHeight` (Range 1..2, default 1)
- ~10 строк. volume уже существует, allowNesting уже существует

### 6. BackpackData.cs — ✏️ Заменить сетку на массу/объём
- Удалить: `gridWidth`, `gridHeight`, `TotalSlots`
- Добавить: `maxWeight: float`, `maxVolume: float`, `ownWeight: float`
- Сохранить: `weightReduction`, `maxWeightBonus`, `beltSlots`

### 7. EquipmentData.cs — ✏️ Удалить layers
- Удалить: `layers: List<EquipmentLayer>` (легаси Матрёшка v1)

### 8. EquipmentController.cs — ✏️ Очистка + фикс
- Удалить: EquipmentSlotsUI (мёртвый код, строка 772+)
- Фикс BUG-8: кэшировать ServiceLocator.GetOrFind в CanEquip

### 9. InventoryScreen.cs — ✏️ Минорные обновления
- Заменить: `backpackPanel.RebuildGrid()` → `backpackPanel.RefreshList()`
- Типизировать: `spiritStoragePanel: GameObject` → `SpiritStoragePanel`
- Подключить: SpiritStoragePanel к вкладке

### 10. TooltipPanel.cs — ✏️ Убрать сеточные поля
- Убрать: отображение sizeWidth/sizeHeight
- Оставить: volume, nesting — уже отображаются

### 11-12. SpiritStorageController + StorageRingController — ✏️ Замена HasFreeSpace
- Заменить: `inventoryController.HasFreeSpace(entry.ItemData.sizeWidth, entry.ItemData.sizeHeight)`
- На: `inventoryController.CanFitItem(entry.ItemData, entry.count)`
- 2 места в каждом файле

### 13. CraftingController.cs — ✏️ Замена проверки места
- Заменить: `FreeCells` + `sizeWidth * sizeHeight`
- На: `CanFitItem(resultItem, 1)`

### 14. Phase16InventoryData.cs — ✏️ Обновление генераторов
- CreateBackpackData(): gridWidth/gridHeight → maxWeight/maxVolume/ownWeight
- UpdateExistingItemData(): убрать sizeWidth/sizeHeight
- CreateTestEquipment(): убрать sizeWidth/sizeHeight
- ~90 строк изменений из 632

### 15. Phase17InventoryUI.cs — ✏️ Существенное редактирование
- **Переписать блок Backpack**: VerticalLayoutGroup + ScrollRect, новый SlotUIPrefab
- **Сохранить**: BodyDollPanel, TooltipPanel, DragDropHandler wiring, ContextMenu
- ~250 строк изменений из ~1000

### 16. Phase18InventoryComponents.cs — ✏️ Обновление параметров
- SetupStarterBackpack: новые параметры BackpackData (maxWeight/maxVolume)

### 17-18. SaveManager.cs + SaveDataTypes.cs — ✏️ Критический фикс
- Добавить: GameSaveData.inventoryData, equipmentData, craftingData
- Добавить: CollectSaveData/ApplySaveData вызовы для 3 контроллеров
- Фикс BUG-2: передавать РЕАЛЬНЫЙ itemDatabase вместо пустого

### 19. Enums.cs — ✏️ Минор
- Удалить SizeClass (если существует)

### 21. StorageRingPanel.cs — ✏️ BUG-12 фикс
- CreateCategoryHeader: AddComponent<TMP_Text> → TextMeshProUGUI

---

## 🗑️ УДАЛЕНИЕ

### InventoryUI.cs (legacy v1, 874 строки)
- Устаревший UI, полностью заменён BackpackPanel + InventoryScreen
- Не используется в SceneBuilder
- Удалить без сожаления

---

## 🆕 НОВЫЕ ФАЙЛЫ

### ItemDatabase.cs (~80 строк)
- Сервис загрузки ItemData/EquipmentData по itemId
- Используется SaveManager для восстановления предметов
- Решает BUG-2 (пустой Dictionary при загрузке)

### SpiritStoragePanel.cs (~200 строк)
- Типизированная панель дух. хранилища (вместо GameObject)
- Список с фильтрами и Qi-стоимостью

---

## 📊 ИТОГОВАЯ СТАТИСТИКА

| Категория | Файлов | Строк до | Строк после (оценка) |
|-----------|:------:|--------:|--------------------:|
| 🔄 Полная перепись | 4 | 2 555 | ~1 700 |
| ✏️ Редактирование | 14 | 7 272 | ~7 070 |
| ✏️ Сущ. редактирование | 1 | ~1 000 | ~950 |
| 🚫 Не трогать | 3 | 1 356 | 1 356 |
| 🗑️ Удаление | 1 | 874 | 0 |
| 🆕 Новые файлы | 2 | 0 | ~280 |
| **Итого** | **25** | **13 057** | **~11 356** |

**Чистый результат:** −1 701 строка (−13%) при полной ликвидации легаси-сетки.

---

## 🔄 ПОРЯДОК ПЕРЕПИСИ (4 файла)

```
1. InventoryController.cs    ← ядро, от него зависят все остальные
2. BackpackPanel.cs          ← зависит от InventoryController API
3. InventorySlotUI.cs        ← зависит от BackpackPanel
4. DragDropHandler.cs        ← зависит от BackpackPanel + InventorySlotUI
```

**Параллелизм:** BackpackPanel + InventorySlotUI можно писать параллельно (после InventoryController).

---

*Создано: 2026-04-27 18:01 UTC*
*Аудит кода: 04_27_inventory_code_audit.md*
*План внедрения: 04_27_inventory_line_model_implementation_plan.md*
*Предпосылка: 04_27_inventory_line_model_plan.md*

# 📐 Чекпоинт: План внедрения строчной модели инвентаря

**Дата:** 2026-05-20
**Статус:** 📋 План (код НЕ писать до отдельной задачи)
**Цель:** Перевести рюкзак с сеточной (grid posX/posY, sizeWidth×sizeHeight) на строчную модель (список + масса/объём)
**Предпосылка:** checkpoints/04_27_inventory_line_model_plan.md + checkpoints/04_20_inventory_code_audit.md

---

## 🎯 Принятые решения

1. **Строчная модель рюкзака** — вместо координатной сетки (posX/posY 0-6) использовать строчный список предметов
2. **Два ограничителя:** масса (weight) и объём (volume) — определяют вместимость
3. **Нет необходимости рисовать сетку** — строчный интерфейс (ScrollRect + VerticalLayoutGroup)
4. **Кукла экипировки НЕ меняется** — слоты EquipmentSlot остаются
5. **Духовное хранилище и кольца — НЕ меняются** — уже работают как каталогизатор/объём
6. **Матрёшка v2 (слои) — ОТЛОЖЕНА** — текущая реализация: один предмет на слот

---

## 📋 Что НЕ меняется

| Компонент | Причина |
|-----------|---------|
| EquipmentSlot enum (15 значений) | Кукла → не связана с рюкзаком |
| EquipmentController | Кукла + экипировка |
| BodyDollPanel | UI куклы |
| EquipmentData (кроме layers) | Данные экипировки |
| Матрёшка (база+материал+грейд+зачарование) | Слой экипировки |
| Grade (5 уровней) | Слой экипировки |
| Материалы (5 тиров) | Слой экипировки |
| Прочность (5 состояний) | Слой экипировки |
| SpiritStorageController | Каталогизатор, не сетка |
| StorageRingController | На объёме, не сетке |
| MaterialSystem.cs (548 строк) | НЕ ТРОГАТЬ |
| 6 уровней редкости | Классификация |
| ItemCategory (8 значений) | Классификация |
| WeaponHandType, NestingFlag | Уже добавлены |
| volume + allowNesting в ItemData | Уже добавлены |

---

## 📋 Что УБИРАЕТСЯ (сетка)

| Поле/Метод | Файл | Причина |
|------------|------|---------|
| `ItemData.sizeWidth` | ItemData.cs | Нет сетки — каждый предмет = 1 строка |
| `ItemData.sizeHeight` | ItemData.cs | Нет сетки |
| `InventorySlot.GridX` | InventoryController.cs | Нет координат |
| `InventorySlot.GridY` | InventoryController.cs | Нет координат |
| `InventorySlot.SetPosition()` | InventoryController.cs | Нет позиционирования |
| `gridSlotIds[,]` | InventoryController.cs | Весь 2D-массив |
| `OccupyGrid()` | InventoryController.cs | — |
| `ClearSlotFromGrid()` | InventoryController.cs | — |
| `RebuildGridFromSlots()` | InventoryController.cs | — |
| `FindFreePosition()` | InventoryController.cs | — |
| `FindFreePositionFrom()` | InventoryController.cs | — |
| `IsAreaFree()` | InventoryController.cs | — |
| `MoveItem(slotId, x, y)` | InventoryController.cs | — |
| `SwapSlots(s1, s2)` | InventoryController.cs | → SwapRows |
| `InventorySlotSaveData.gridX` | InventoryController.cs | — |
| `InventorySlotSaveData.gridY` | InventoryController.cs | — |
| `BackpackData.gridWidth` | BackpackData.cs | → maxVolume |
| `BackpackData.gridHeight` | BackpackData.cs | → maxWeight |
| `BackpackData.TotalSlots` | BackpackData.cs | — |
| `BackpackPanel.gridCells` | BackpackPanel.cs | — |
| `BackpackPanel.PlaceItemInGrid()` | BackpackPanel.cs | — |
| `BackpackPanel.RemoveItemFromGrid()` | BackpackPanel.cs | — |
| `BackpackPanel.ScreenToGridPosition()` | BackpackPanel.cs | — |
| `EquipmentData.layers` | EquipmentData.cs | Легаси Матрёшка v1 |
| `EquipmentSlotsUI` | EquipmentController.cs | Мёртвый код |

---

## 📋 Что ДОБАВЛЯЕТСЯ

| Поле/Метод | Файл | Описание |
|------------|------|----------|
| `BackpackData.maxWeight` | BackpackData.cs | Максимальная масса (кг) |
| `BackpackData.maxVolume` | BackpackData.cs | Максимальный объём (литры) |
| `BackpackData.ownWeight` | BackpackData.cs | Собственный вес рюкзака |
| `InventoryController.totalVolume` | InventoryController.cs | Текущий объём |
| `InventoryController.maxWeight` | InventoryController.cs | Доступный лимит массы |
| `InventoryController.maxVolume` | InventoryController.cs | Доступный лимит объёма |
| `SwapRows(index1, index2)` | InventoryController.cs | Перестановка строк |
| `CanFitItem(ItemData, count)` | InventoryController.cs | Проверка по массе+объём |
| `GameSaveData.inventoryData` | SaveManager.cs | Сохранение инвентаря |
| `GameSaveData.equipmentData` | SaveManager.cs | Сохранение экипировки |
| `GameSaveData.craftingData` | SaveManager.cs | Сохранение крафта |
| `ItemDatabase` сервис | Новый файл | Загрузка ItemData по itemId |

---

## 🏗️ НОВАЯ МОДЕЛЬ BackpackData

```
BackpackData (ScriptableObject):
  id: string
  nameRu: string
  maxWeight: float              // Максимальная масса (кг)
  maxVolume: float              // Максимальный объём (литры)
  weightReduction: float        // Снижение веса содержимого (0..50%)
  maxWeightBonus: float         // Бонус к базовому лимиту массы
  beltSlots: int                // Слоты пояса (0-4)
  ownWeight: float              // Собственный вес рюкзака
```

**Новые ассеты:**

| ID | maxWeight | maxVolume | weightRed% | beltSlots | ownWeight |
|----|-----------|-----------|-----------|-----------|-----------|
| ClothSack | 30 кг | 50 л | 0% | 0 | 0.5 |
| LeatherPack | 50 кг | 80 л | 10% | 1 | 2.0 |
| IronContainer | 80 кг | 120 л | 15% | 2 | 5.0 |
| SpiritBag | 120 кг | 200 л | 25% | 2 | 3.0 |
| SpatialChest | 200 кг | 500 л | 40% | 4 | 1.0 |

---

## 🏗️ НОВАЯ МОДЕЛЬ InventorySlot

```
InventorySlot (упрощённый):
  SlotId: int                   — уникальный ID
  ItemData: ItemData            — ссылка на данные
  Count: int                    — количество в стаке
  durability: int               — текущая прочность
  grade: EquipmentGrade         — грейд предмета
  rowIndex: int                 — ← НОВОЕ: позиция в списке

  // УБРАНО: GridX, GridY, ItemWidth, ItemHeight, SetPosition()
```

---

## 🏗️ НОВАЯ МОДЕЛЬ InventorySlotSaveData

```
InventorySlotSaveData:
  itemId: string
  count: int
  durability: int
  grade: EquipmentGrade

  // УБРАНО: gridX, gridY
  // rowIndex НЕ сохраняется — восстанавливается по порядку
```

---

## 🔄 ЭТАПЫ ВНЕДРЕНИЯ

### Этап 0: Подготовка данных (Phase16)

**Затрагивает:**
- `BackpackData.cs` — заменить gridWidth/gridHeight → maxWeight/maxVolume
- `Phase16InventoryData.cs` — новые параметры ассетов
- `ItemData.cs` — убрать sizeWidth/sizeHeight (заменить на константу 1)
- `EquipmentData.cs` — убрать layers
- `Enums.cs` — убрать SizeClass (если есть)

**Задачи:**
- [ ] 0.1 BackpackData: gridWidth/gridHeight → maxWeight/maxVolume/ownWeight
- [ ] 0.2 BackpackData: TotalSlots → убрать (нет слотов)
- [ ] 0.3 ItemData: sizeWidth/sizeHeight → удалить поля (Range, defaultValue)
- [ ] 0.4 EquipmentData: layers → удалить поле
- [ ] 0.5 Phase16: CreateBackpackData() → новые параметры ассетов (5 шт.)
- [ ] 0.6 Phase16: UpdateExistingItemData() → убрать sizeWidth/sizeHeight
- [ ] 0.7 Phase16: AddTestEquipmentSet() → убрать sizeWidth/sizeHeight из тестовых
- [ ] 0.8 Документация: 17_InventoryData.md → обновить таблицы

**Критерий готовности:** Компиляция без ошибок, Phase16 создаёт корректные .asset

---

### Этап 1: Ядро инвентаря (InventoryController)

**Затрагивает:**
- `InventoryController.cs` — полная переработка

**Задачи:**
- [ ] 1.1 Удалить gridSlotIds[,] и все методы сетки (OccupyGrid, ClearSlotFromGrid, FindFreePosition, IsAreaFree, RebuildGridFromSlots)
- [ ] 1.2 Удалить InventorySlot.GridX, GridY, ItemWidth, ItemHeight, SetPosition()
- [ ] 1.3 Добавить CanFitItem(ItemData, count) → проверка по mass+volume
- [ ] 1.4 Добавить totalVolume, maxWeight, maxVolume (вычисляемые из BackpackData)
- [ ] 1.5 Переделать AddItem() → без posX/posY, проверка CanFitItem
- [ ] 1.6 Переделать AddItemAt() → без координат (или убрать, оставить AddItem)
- [ ] 1.7 Переделать RemoveItem() → без ClearSlotFromGrid
- [ ] 1.8 Заменить MoveItem(slotId, x, y) → SwapRows(index1, index2)
- [ ] 1.9 Переделать SortInventory() → сортировка List<InventorySlot> + обновление rowIndex
- [ ] 1.10 Переделать SetBackpack() → без ApplyGridSize, проверка CanFitItem
- [ ] 1.11 Переделать GetSaveData()/LoadSaveData() → без gridX/gridY
- [ ] 1.12 Добавить FindSlotById → Dictionary<int, InventorySlot> вместо List.Find
- [ ] 1.13 Исправить BUG-5 (линейный поиск)
- [ ] 1.14 Удалить SwapSlots() → заменить на SwapRows()

**Критерий готовности:** InventoryController компилируется, все публичные методы работают без сетки

---

### Этап 2: Save/Load — КРИТИЧЕСКИЙ

**Затрагивает:**
- `SaveManager.cs`
- `SaveDataTypes.cs`

**Задачи:**
- [ ] 2.1 Добавить GameSaveData.inventoryData (List<InventorySlotSaveData>)
- [ ] 2.2 Добавить GameSaveData.equipmentData (Dictionary<string, EquipmentSaveData>)
- [ ] 2.3 Добавить GameSaveData.craftingData (CraftingSaveData)
- [ ] 2.4 SaveManager.CollectSaveData → вызвать inventoryController.GetSaveData()
- [ ] 2.5 SaveManager.CollectSaveData → вызвать equipmentController.GetSaveData()
- [ ] 2.6 SaveManager.CollectSaveData → вызвать craftingController.GetSaveData()
- [ ] 2.7 SaveManager.ApplySaveData → передавать РЕАЛЬНЫЙ itemDatabase (не пустой)
- [ ] 2.8 SaveManager.ApplySaveData → вызвать inventoryController.LoadSaveData()
- [ ] 2.9 SaveManager.ApplySaveData → вызвать equipmentController.LoadSaveData()
- [ ] 2.10 SaveManager.ApplySaveData → вызвать craftingController.LoadSaveData()
- [ ] 2.11 Создать ItemDatabase — сервис загрузки ItemData/EquipmentData по itemId

**Критерий готовности:** Инвентарь, экипировка и крафт сохраняются и восстанавливаются корректно

---

### Этап 3: UI рюкзака (BackpackPanel)

**Затрагивает:**
- `BackpackPanel.cs`
- `InventorySlotUI.cs`

**Задачи:**
- [ ] 3.1 Удалить gridCells (Dictionary<string, InventorySlotUI> по "x,y")
- [ ] 3.2 Заменить GridLayoutGroup → VerticalLayoutGroup + ScrollRect
- [ ] 3.3 Каждая строка = 1 предмет (иконка + название + кол-во + вес + объём)
- [ ] 3.4 Убрать PlaceItemInGrid/RemoveItemFromGrid
- [ ] 3.5 Реализовать RefreshList() — обновление списка без Destroy/Instantiate (pool или delta-update)
- [ ] 3.6 Убрать ScreenToGridPosition()
- [ ] 3.7 Индикаторы: вес (текущий/макс) + объём (текущий/макс) вместо "0/12 слотов"
- [ ] 3.8 Цвет строки по редкости (фон или рамка)
- [ ] 3.9 Подсветка при наведении (hover)

**Критерий готовности:** BackpackPanel отображает список предметов с массой/объёмом

---

### Этап 4: Drag & Drop

**Затрагивает:**
- `DragDropHandler.cs`

**Задачи:**
- [ ] 4.1 DragSource: добавить StorageRing, SpiritStorage
- [ ] 4.2 DropTarget: добавить SpiritStorageList, StorageRingList
- [ ] 4.3 Реализовать HandleDropOnBackpack для DollSlot (BUG-6)
- [ ] 4.4 SwapRows вместо MoveItem по координатам
- [ ] 4.5 Исправить SplitStack: сохранять durability/grade (BUG-3)
- [ ] 4.6 Перетаскивание между вкладками (Backpack ↔ Spirit ↔ Ring)

**Критерий готовности:** Drag & Drop работает между всеми панелями

---

### Этап 5: Phase17 (SceneBuilder UI)

**Затрагивает:**
- `Phase17InventoryUI.cs` (1700 строк)

**Задачи:**
- [ ] 5.1 BackpackPanel layout: VerticalLayoutGroup + ScrollRect вместо GridLayoutGroup
- [ ] 5.2 SlotUIPrefab: строка списка (горизонтальный layout: иконка + текст + вес + объём)
- [ ] 5.3 Убрать логику GridContainer/GridBackground
- [ ] 5.4 WireBackpackPanelReferences: weightBar → weight+volume индикаторы
- [ ] 5.5 TooltipPanel: убрать sizeWidth/sizeHeight, оставить volume/nesting
- [ ] 5.6 DragDropHandler wiring: обновить для строчной модели
- [ ] 5.7 Контекстное меню: адаптировать для строчной модели

**Критерий готовности:** SceneBuilder Phase17 создаёт строчный UI

---

### Этап 6: Phase18 + подключение

**Затрагивает:**
- `Phase18InventoryComponents.cs`
- `InventoryScreen.cs`

**Задачи:**
- [ ] 6.1 SetupStarterBackpack → новые параметры BackpackData (maxWeight/maxVolume)
- [ ] 6.2 SpiritStorageController: qiCostPerKg → можно оставить (масса остаётся)
- [ ] 6.3 InventoryScreen: типизировать spiritStoragePanel
- [ ] 6.4 Создать SpiritStoragePanel.cs (типизированная панель)
- [ ] 6.5 Подключить все панели к InventoryScreen

**Критерий готовности:** Полный цикл: открыть инвентарь → увидеть список → перетащить → экипировать → закрыть → сохранить → загрузить

---

### Этап 7: Очистка и легаси

**Задачи:**
- [ ] 7.1 Удалить EquipmentSlotsUI (мёртвый код)
- [ ] 7.2 Удалить ItemStack (если не используется)
- [ ] 7.3 Удалить BackpackPanel.PlaceItemInGrid fallback (AddComponent)
- [ ] 7.4 Исправить StorageRingPanel.CreateCategoryHeader (TMP_Text → TextMeshProUGUI)
- [ ] 7.5 CanEquip: кэшировать ServiceLocator-зависимости (BUG-8)
- [ ] 7.6 Убрать EquipmentData.layers из Phase16 генерации

---

### Этап 8: Документация

**Затрагивает:**
- `docs/INVENTORY_SYSTEM.md`
- `docs/DATA_MODELS.md`
- `docs_asset_setup/17_InventoryData.md`
- `docs_asset_setup/18_InventoryUI.md`
- `docs_asset_setup/SCENE_BUILDER_ARCHITECTURE.md`

**Задачи:**
- [ ] 8.1 INVENTORY_SYSTEM.md: убрать сетку, описать строчную модель (масса+объём)
- [ ] 8.2 INVENTORY_SYSTEM.md: пометить строчную модель как текущую реализацию
- [ ] 8.3 INVENTORY_SYSTEM.md: добавить раздел «Будущая разработка» с описанием полноценной строчной модели (по 04_27 плану)
- [ ] 8.4 DATA_MODELS.md: убрать posX/posY/sizeWidth/sizeHeight, добавить volume в InventoryItem
- [ ] 8.5 17_InventoryData.md: обновить таблицы BackpackData (5 ассетов с maxWeight/maxVolume)
- [ ] 8.6 18_InventoryUI.md: описать строчный UI (VerticalLayoutGroup)
- [ ] 8.7 SCENE_BUILDER_ARCHITECTURE.md: обновить описание Phase16-18

---

## 📊 ОЦЕНКА ТРУДОЁМКОСТИ

| Этап | Описание | Строк кода | Сложность | Зависит от |
|------|----------|:----------:|:---------:|:----------:|
| 0 | Подготовка данных | ~200 | 🟡 Средняя | — |
| 1 | Ядро InventoryController | ~500 | 🔴 Высокая | 0 |
| 2 | Save/Load (критический) | ~300 | 🔴 Высокая | 1 |
| 3 | UI BackpackPanel | ~400 | 🟡 Средняя | 1 |
| 4 | Drag & Drop | ~300 | 🟡 Средняя | 1, 3 |
| 5 | Phase17 SceneBuilder | ~600 | 🔴 Высокая | 3 |
| 6 | Phase18 + подключение | ~200 | 🟢 Низкая | 2, 5 |
| 7 | Очистка | ~100 | 🟢 Низкая | 6 |
| 8 | Документация | ~50 (текст) | 🟢 Низкая | 7 |
| | **Итого** | **~2 650** | | |

**Порядок:** 0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8

**Этапы 0-2 можно делать параллельно с 3-4** (ядро + UI независимы, но интеграция на этапе 6).

---

## ⚠️ РИСКИ

| Риск | Вероятность | Влияние | Митигация |
|------|:----------:|:-------:|-----------|
| Phase17 перелом UI (1700 строк) | Высокая | Высокое | Полная перегенерация через SceneBuilder — не ручной wiring |
| Save/Load itemDatabase пустой | Высокая | Критическое | Создать ItemDatabase сервис ДО этапа 2 |
| Обратная совместимость .asset | Средняя | Среднее | Phase16 пересоздаёт ассеты — нет .asset файлов с int-индексами |
| Потеря контекста между сессиями | Высокая | Среднее | Чекпоинты после каждого этапа |
| DragDrop переработка | Средняя | Среднее | Упростить: только swap + equip/unequip |

---

## 📝 ЗАМЕЧАНИЕ: «Будущая разработка» vs «Текущая реализация»

Текущий план — **упрощённая строчная модель** для отображения и тестирования:
- Предмет = 1 строка (нет многострочных предметов)
- Ограничители: масса + объём
- Drag: swap строк + equip/unequip

**Полноценная строчная модель** (по 04_27_inventory_line_model_plan.md):
- Предметы могут занимать несколько строк (span)
- Визуальная группировка по категориям
- Расширенные фильтры и поиск
- Qi-стоимость перемещения в дух. хранилище
- Кольца хранения с Qi-формулами

→ В документации (INVENTORY_SYSTEM.md) пометить полноценную модель как **«🔧 Будущая разработка»** с ссылкой на 04_27 план.

---

*Создано: 2026-05-20*
*Аудит кода: 04_20_inventory_code_audit.md*
*Предпосылка: 04_27_inventory_line_model_plan.md*

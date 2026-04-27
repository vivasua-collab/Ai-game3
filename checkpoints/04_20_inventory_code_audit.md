# 🔍 Чекпоинт: Аудит текущего кода системы инвентаря

**Дата:** 2026-05-20
**Статус:** 📋 Аудит завершён (код НЕ менять до отдельной задачи)
**Цель:** Зафиксировать текущее состояние кода для планирования переработки

---

## 📊 Объём кода

| Область | Файлов | Строк | Контроллеры | UI |
|---------|:------:|------:|:-----------:|:--:|
| Scripts/Inventory/ | 6 | ~5 052 | 5 | — |
| Scripts/UI/Inventory/ | 7 | ~3 714 | — | 7 |
| Scripts/Data/ScriptableObjects/ | 5 | ~384 | — | — |
| Scripts/Core/Enums.cs (часть) | 1 | ~80 | — | — |
| Scripts/Save/ (часть) | 2 | ~200 | — | — |
| SceneBuilder Phase16-18 | 3 | ~2 494 | — | — |
| **Итого** | **24** | **~11 924** | **5** | **7** |

---

## 1. МОДЕЛИ ДАННЫХ

### 1.1 ItemData (ScriptableObject, 145 строк)

```
itemId, nameRu, nameEn, description, itemType
category: ItemCategory | rarity: ItemRarity | icon: Sprite
stackable: bool | maxStack: int = 99
sizeWidth: int = 1 (Range 1..2)     ← ⚠️ СВЯЗАНО С СЕТКОЙ
sizeHeight: int = 1 (Range 1..2)    ← ⚠️ СВЯЗАНО С СЕТКОЙ
weight: float = 0.1f | value: int = 1
hasDurability: bool | maxDurability: int = 100
effects: List<ItemEffect>
requiredCultivationLevel: int | statRequirements: List<StatRequirement>
volume: float = 1.0f                 ← ✅ Нужно для строчной модели
allowNesting: NestingFlag = Any      ← ✅ Нужно для строчной модели
```

### 1.2 EquipmentData (наследует ItemData, 76 строк)

```
slot: EquipmentSlot
handType: WeaponHandType = OneHand
layers: List<EquipmentLayer>         ← ⚠️ ЛЕГАСИ (Матрёшка v1, не используется в v2)
damage, defense: int
coverage, damageReduction, dodgeBonus: float
materialId: string | materialTier: int
grade: EquipmentGrade | itemLevel: int
statBonuses: List<StatBonus>
specialEffects: List<SpecialEffect>
```

### 1.3 BackpackData (ScriptableObject, 49 строк)

```
gridWidth: int = 3 (Range 3..10)    ← ⚠️ СВЯЗАНО С СЕТКОЙ
gridHeight: int = 4 (Range 3..8)    ← ⚠️ СВЯЗАНО С СЕТКОЙ
weightReduction: float = 0 (0..50%)
maxWeightBonus: float = 0
beltSlots: int = 0 (0..4)
TotalSlots => gridWidth * gridHeight  ← ⚠️ СВЯЗАНО С СЕТКОЙ
```

**Создаваемые ассеты (Phase16):**

| ID | grid | weightRed% | beltSlots |
|----|------|-----------|-----------|
| ClothSack | 3×4 | 0 | 0 |
| LeatherPack | 4×5 | 10 | 1 |
| IronContainer | 5×5 | 15 | 2 |
| SpiritBag | 6×6 | 25 | 2 |
| SpatialChest | 8×7 | 40 | 4 |

### 1.4 StorageRingData (наследует EquipmentData, 46 строк)

```
maxVolume: float = 5f
qiCostBase: int = 5
qiCostPerUnit: float = 2f
accessTime: float = 1.5f
```

**Создаваемые ассеты (Phase16):**

| ID | maxVolume | qiCostPerUnit |
|----|-----------|---------------|
| Slit | 5 | 3.0 |
| Pocket | 15 | 2.0 |
| Vault | 30 | 1.0 |
| Space | 60 | 0.5 |

### 1.5 Enum'ы (Enums.cs)

```
EquipmentSlot: None, Head, Torso, Belt, Legs, Feet,
               WeaponMain, WeaponOff, Amulet,
               RingLeft1, RingLeft2, RingRight1, RingRight2,
               Charger, Hands, Back                    (15 значений)
ItemCategory: Weapon, Armor, Accessory, Consumable,
              Material, Technique, Quest, Misc         (8 значений)
ItemRarity: Common, Uncommon, Rare, Epic, Legendary, Mythic
EquipmentGrade: Damaged(×0.5), Common(×1.0), Refined(×1.5),
                Perfect(×2.5), Transcendent(×4.0)
DurabilityCondition: Pristine, Excellent, Good, Worn, Damaged, Broken
NestingFlag: None, Spirit, Ring, Any
WeaponHandType: OneHand, TwoHand
MaterialCategory: Metal, Leather, Cloth, Wood, Bone,
                  Crystal, Gem, Organic, Spirit, Void
```

---

## 2. КОНТРОЛЛЕРЫ

### 2.1 InventoryController (1003 строки)

**Ключевая структура:**

```
slots: List<InventorySlot>                    — все слоты рюкзака
gridSlotIds: int[,]                           — 2D-массив [gridWidth, gridHeight]
                                              -1 = свободно, >=0 = slotId
backpackData: BackpackData                    — текущий рюкзак
baseMaxWeight: float = 50f                    — базовый лимит веса
useWeightLimit: bool = true                   — весовой лимит включён
nextSlotId: int = 0                           — генератор ID слотов
```

**Сетка posX/posY — реализация:**

| Метод | Операция | Сложность |
|-------|----------|-----------|
| FindFreePosition(w, h) | Сканирование (0,0)→(W,H) | O(W×H) |
| IsAreaFree(x, y, w, h) | Проверка gridSlotIds | O(w×h) |
| OccupyGrid(slotId, x, y, w, h) | Заполнение ячеек | O(w×h) |
| ClearSlotFromGrid(slotId) | **ПОЛНЫЙ СКАН** gridSlotIds | O(W×H) |
| RebuildGridFromSlots() | Перестройка из slots | O(W×H + N) |
| MoveItem(slotId, x, y) | Clear + Occupy | O(W×H + w×h) |
| SwapSlots(s1, s2) | Проверка + обмен | O(W×H) |

**Публичный API:**

```
AddItem(ItemData, count) → int slotId | -1
AddItemAt(ItemData, count, x, y) → int slotId | -1
RemoveItem(slotId, count?) → bool
MoveItem(slotId, newX, newY) → bool
SwapSlots(slot1, slot2) → bool
SortInventory() → void
SetBackpack(BackpackData) → bool
GetTotalWeight() → float
IsOverencumbered → bool
FindSlotById(id) → InventorySlot | null     ← O(N) линейный поиск!
GetSaveData() → List<InventorySlotSaveData>
LoadSaveData(data, itemDatabase) → void
```

**InventorySlot (внутренний класс):**
```
SlotId, ItemData, Count, GridX, GridY         ← ⚠️ GridX/Y — СЕТКА
durability (internal), grade
ItemWidth, ItemHeight                         ← ⚠️ СВЯЗАНО С СЕТКОЙ
Durability, HasDurability, Condition
Damage(), Repair(), SetPosition()
```

**InventorySlotSaveData:**
```
itemId, count, gridX, gridY, durability, grade   ← ⚠️ gridX/gridY — СЕТКА
```

### 2.2 EquipmentController (930 строк)

**Ключевая структура:**

```
equippedItems: Dictionary<EquipmentSlot, EquipmentInstance>
isWeaponOffBlocked: bool                      — блок 2H-оружием
cachedStats: EquipmentStats
statsDirty: bool                              — ленивый пересчёт
```

**EquipmentInstance:**
```
equipmentData: EquipmentData
grade: EquipmentGrade
durability: int
Slot, HandType, IsTwoHand
DurabilityPercent, Condition
```

**Публичный API:**

```
Equip(EquipmentData, grade, durability) → EquipmentInstance
Unequip(slot) → EquipmentData | null
CanEquip(data) → (bool, string)              ← ServiceLocator.GetOrFind КАЖДЫЙ вызов
GetEquippedItem(slot) → EquipmentInstance
GetAllEquipped() → Dictionary
GetEquipmentStats() → EquipmentStats          ← lazy recalculated
GetSaveData() → Dictionary<string, EquipmentSaveData>
LoadSaveData(data, itemDatabase) → void
```

**Легаси:**
- `EquipmentSlotsUI` (строка 772) — мёртвый код
- `EquipmentData.layers` — поле Матрёшки v1, не используется

### 2.3 SpiritStorageController (882 строки)

```
entries: List<SpiritStorageEntry>
isUnlocked: bool = false
nextEntryId: int = 0
baseQiCost, qiCostPerKg, retrievalQiCost, qiCostPerKgRetrieval
requiredCultivationLevel: int = 1
```

**SpiritStorageEntry:**
```
entryId, itemId, _itemData(NonSerialized), count
durability, grade, totalWeight, totalVolume
```

### 2.4 StorageRingController (960 строк)

```
ringSlots: Dictionary<EquipmentSlot, List<StorageRingEntry>>
nextEntryId: int = 0
```

### 2.5 CraftingController (729 строк)

```
recipes: List<CraftingRecipe>
knownRecipeIds: HashSet<string>
craftingExperience: Dictionary<CraftingType, float>
```

### 2.6 MaterialSystem (548 строк) — НЕ ТРОГАТЬ

---

## 3. UI КОМПОНЕНТЫ

### 3.1 InventoryScreen (342 строки)

Главный экран: вкладки (Backpack / SpiritStorage / StorageRing), кнопки Sort/Close, панели.

```
backpackPanel, bodyDollPanel, dragDropHandler, tooltipPanel,
storageRingPanel, spiritStoragePanel (← ⚠️ GameObject, нет типизации!)
```

### 3.2 BackpackPanel (504 строки)

```
gridCells: Dictionary<string, InventorySlotUI>      ← ⚠️ ключ "x,y" — СЕТКА
itemSlotUIs: Dictionary<int, InventorySlotUI>       — slotId → UI
slotUIPrefab, gridContainer, gridBackground
backpackNameText, weightText, weightBar, slotsText
```

**Критичные методы:**
- `RebuildGrid()` — ⚠️ ПОЛНОСТЬЮ Destroy + Instantiate всех ячеек при любом изменении
- `PlaceItemInGrid()` — скрывает фоновые ячейки под предметом
- `RemoveItemFromGrid()` — возвращает фоновые ячейки
- `ScreenToGridPosition()` — конвертация экранных координат → (gridX, gridY)

### 3.3 BodyDollPanel (559 строк)

```
DollSlotUI: iconImage, slotBorder, slotLabel, itemNameText,
            durabilityBar, blockedOverlay, emptyIcon, slotType
11 слотов: 7 видимых + 4 скрытых (RingLeft1/2, RingRight1/2)
Двухколоночный layout (лево: одежда, право: оружие+кольца)
Процедурный силуэт тела 80×220
StatsPanel: totalDamage, totalDefense, summaryText
```

### 3.4 DragDropHandler (656 строк)

```
DragSource: None, Backpack, DollSlot           ← ⚠️ НЕТ StorageRing/SpiritStorage!
DropTarget: None, BackpackGrid, DollSlot, Outside
```

**Проблемы:**
- HandleDropOnBackpack для DragSource.DollSlot — TODO (строка 267)
- SplitStack теряет durability/grade при разделении
- Drag работает ТОЛЬКО Backpack ↔ Doll

### 3.5 InventorySlotUI (390 строк)

```
IPointerEnterHandler, IPointerExitHandler
IBeginDragHandler, IDragHandler, IEndDragHandler
IPointerClickHandler
CanvasGroup для drag-состояния
GetRarityColor() — цвета рамки по редкости
```

### 3.6 TooltipPanel (535 строк)

```
7 секций: Header, Combat, Physical (volume/nesting),
          Bonuses, Material/Grade, Requirements, Description
PositionTooltip() — смещение + ограничения по краям
```

### 3.7 StorageRingPanel (728 строк)

```
Селектор кольца (4 кнопки)
Фильтры: категория, редкость, текстовый поиск
Группировка по категориям
Retrieve / RetrieveAll + Qi-стоимость
⚠️ CreateCategoryHeader: AddComponent<TMP_Text> — не работает корректно
```

---

## 4. SAVE/LOAD — 🔴 КРИТИЧЕСКАЯ ПРОБЛЕМА

| Контроллер | GetSaveData() | LoadSaveData() | Вызывается из SaveManager | В GameSaveData |
|-----------|:---:|:---:|:---:|:---:|
| InventoryController | ✅ | ✅ | ❌ НЕТ | ❌ НЕТ ПОЛЯ |
| EquipmentController | ✅ | ✅ | ❌ НЕТ | ❌ НЕТ ПОЛЯ |
| SpiritStorageController | ✅ | ✅ | ✅ | ✅ |
| StorageRingController | ✅ | ✅ | ✅ | ✅ |
| CraftingController | ✅ | ✅ | ❌ НЕТ | ❌ НЕТ ПОЛЯ |

**Дополнительно:** SaveManager.ApplySaveData передаёт **пустой** `new Dictionary<string, ItemData>()` — даже SpiritStorage/StorageRing не восстановят предметы!

---

## 5. SCENEBUILDER — ФАЗЫ ИНВЕНТАРЯ

### Phase16InventoryData (631 строка)
- Создаёт 5 BackpackData .asset (gridWidth×gridHeight)
- Создаёт 4 StorageRingData .asset (maxVolume)
- Обновляет ItemData полями volume + allowNesting
- Создаёт 10 тестовых предметов экипировки
- Генерирует 32×32 иконки (пиксельные)

### Phase17InventoryUI (1700 строк)
- Создаёт полную UI-иерархию инвентаря
- ~159 SerializeField-подключений через SerializedObject
- BackpackPanel с GridLayoutGroup
- BodyDollPanel с двухколоночным layout
- TooltipPanel, DragDropHandler, ContextMenu

### Phase18InventoryComponents (163 строки)
- Добавляет SpiritStorageController + StorageRingController на Player
- Настраивает qi-cost параметры
- Подключает стартовый рюкзак (ClothSack)

---

## 6. ЗАФИКСИРОВАННЫЕ БАГИ

### 🔴 Критические (блокирующие)

| ID | Описание | Файл | Строка |
|----|----------|------|--------|
| BUG-1 | Инвентарь и экипировка НЕ сохраняются/загружаются | SaveManager.cs | — |
| BUG-2 | itemDatabase при загрузке = пустой Dictionary | SaveManager.cs | 513 |
| BUG-3 | SplitStack теряет durability/grade | DragDropHandler.cs | 568-575 |

### 🟠 Высокие (существенные)

| ID | Описание | Файл |
|----|----------|------|
| BUG-4 | RebuildGrid: Destroy+Instantiate всех ячеек | BackpackPanel.cs |
| BUG-5 | FindSlotById: линейный O(N) поиск | InventoryController.cs |
| BUG-6 | DragSource.DollSlot → Backpack: TODO (не реализовано) | DragDropHandler.cs:267 |
| BUG-7 | DragSource не включает StorageRing/SpiritStorage | DragDropHandler.cs |
| BUG-8 | CanEquip: ServiceLocator.GetOrFind при каждом вызове | EquipmentController.cs:337-346 |

### 🟡 Средние

| ID | Описание | Файл |
|----|----------|------|
| BUG-9 | EquipmentData.layers — легаси-поле | EquipmentData.cs:33 |
| BUG-10 | EquipmentSlotsUI — мёртвый код | EquipmentController.cs:772 |
| BUG-11 | spiritStoragePanel = GameObject (нет типизации) | InventoryScreen.cs |
| BUG-12 | StorageRingPanel: AddComponent<TMP_Text> не работает | StorageRingPanel.cs:488 |
| BUG-13 | SortInventory не гарантирует размещение (нет rollback) | InventoryController.cs |

---

## 7. КАРТА ЗАВИСИМОСТЕЙ ПРИ ПЕРЕРАБОТКЕ

```
InventoryController ← BackpackPanel, DragDropHandler, BodyDollPanel,
                       CraftingController, StorageRingCtrl, SpiritStorageCtrl,
                       InventoryScreen
EquipmentController ← BodyDollPanel, DragDropHandler, InventoryCtrl,
                       StorageRingCtrl
ItemData            ← InventorySlot, TooltipPanel, DragDropHandler, ALL controllers
BackpackData        ← InventoryController, BackpackPanel, Phase16
EquipmentSlot enum  ← EquipmentController, BodyDollPanel, StorageRingCtrl, DragDropHandler
```

**Вывод:** Переработка сетки затрагивает **6 контроллеров + 7 UI-компонентов + 3 фазы SceneBuilder**.

---

*Создано: 2026-05-20*
*Для планирования переработки: см. 04_20_inventory_line_model_implementation_plan.md*

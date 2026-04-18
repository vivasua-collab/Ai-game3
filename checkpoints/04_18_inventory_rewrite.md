# Чекпоинт: Полная переделка инвентаря

**Дата:** 2026-04-18 18:20:58 UTC
**Редактировано:** 2026-04-19 12:30:00 UTC
**Статус:** in_progress — Этап 0 ✅, Этап 1 ✅, Этап 2 ✅, Этап 3 ✅, Этап 4 ✅, Этап 5 ✅

## Контекст

Решение: ПОЛНАЯ ПЕРЕДЕЛКА кода инвентаря. Старый код содержит ошибки и не соответствует v2.0 дизайну.

**Ключевое условие (добавлено 18:39):** Папка `Assets/` удаляется и пересоздаётся генератором (Phase09). Нет существующих .asset файлов, нет save-данных. Все риски совместимости = 0. EquipmentSlot enum переписывается ПОЛНОСТЬЮ.

## GitHub
- Репозиторий: `vivasua-collab/Ai-game3.git`, ветвь `main`
- Токен: (в контексте сессии, НЕ коммитить в файлы!)

## Аудит — выполнен ✅

| Документ | Статус | Дата |
|----------|--------|------|
| `docs_temp/INVENTORY_FLAGS_AUDIT.md` v2.0 | ✅ Завершён | 2026-04-18 18:35 |
| `checkpoints/04_18_inventory_implementation.md` | ⛔ Superseded | — |
| `checkpoints/04_18_data_model_rewrite.md` | 🆕 Создан | 2026-04-18 18:39 |

**Ключевые выводы аудита:**
- volume + allowNesting — 🟢 ПРОСТО (1-2 ч), нулевой риск
- EquipmentSlot полная замена — 🟢 ПРОСТО (30 мин), Assets пересоздаются
- BackpackData + StorageRingData — 🟢 ПРОСТО (2-3 ч)
- SpiritStorageController — 🔴 СЛОЖНО (5-8 ч), отложено
- StorageRingController — 🔴 СЛОЖНО (5-8 ч), отложено

---

## План (по этапам)

### Этап 0: Переделка моделей данных ⬜ → 🆕 Выделен в отдельный чекпоинт

**Подробный план:** `checkpoints/04_18_data_model_rewrite.md`

- [x] 0.1 Переписать EquipmentSlot enum (полная замена по драфту v2.0) ✅
- [x] 0.2 Добавить NestingFlag enum ✅
- [x] 0.3 Добавить volume + allowNesting в ItemData ✅
- [x] 0.4 Создать BackpackData.cs ScriptableObject ✅
- [x] 0.5 Создать StorageRingData.cs ScriptableObject ✅
- [x] 0.6 Добавить WeaponHandType enum + handType в EquipmentData ✅
- [x] 0.7 Обновить AssetGeneratorExtended под новый enum и новые SO ✅
- [x] 0.8 Обновить все файлы, ссылающиеся на EquipmentSlot ✅
- [x] 0.9 Проверить компиляцию ✅

### Этап 1: Базовая кукла (Doll) ✅

- [x] 1.1 Переписать EquipmentController.cs (7 видимых слотов, 1H/2H логика) ✅
- [x] 1.2 Переписать EquipmentInstance (убран currentLayer, +IsTwoHand) ✅
- [x] 1.3 Переписать EquipmentStats (раздельные GradeMultiplier, +conductivity, +vitality) ✅
- [x] 1.4 Интеграция с ServiceLocator + каскадные фиксы ✅
- [x] 1.5 Проверить компиляцию + git push ✅

**Коммит:** a41fb51 — EquipmentController v2.0
**Каскадные фиксы:** Phase06Player.cs, SceneSetupTools.cs, InventoryUI.cs

**Исправленные баги:**
- EQP-BUG-02: value бонусов рассчитывалось, но не использовалось
- EQP-BUG-04: нет логики двуручного оружия → добавлена
- EQP-BUG-05: GradeMultiplier не совпадает с EQUIPMENT_SYSTEM.md → разделены durability/effectiveness

### Этап 2: Рюкзак (Backpack) ✅

- [x] 2.1 Переписать InventoryController.cs (динамическая сетка от BackpackData) ✅
- [x] 2.2 Исправить все баги INV-BUG-01..07 ✅
- [x] 2.3 Добавить SetBackpack() + effectiveWeight ✅
- [x] 2.4 Добавить EquipFromInventory / UnequipToInventory мост ✅
- [x] 2.5 Обновить CraftingController.cs (CRA-BUG-01..03) ✅
- [x] 2.6 Проверить компиляцию + git push ✅

**Коммит:** f3a5683 — InventoryController v2.0 + CraftingController багфиксы
**Каскадные фиксы:** Phase06Player.cs, SceneSetupTools.cs, InventoryUI.cs

**Исправленные баги Inventory (7):**
- INV-BUG-01: Рекурсивный AddItem → итеративный
- INV-BUG-02: RemoveFromSlot — вес до модификации
- INV-BUG-03: Resize — полная перестройка occupancy
- INV-BUG-04: FreeGrid → RebuildGridFromSlots
- INV-BUG-05: HasDurability при durability=0 → true
- INV-BUG-06: LoadSaveData durability=0 → загружается
- INV-BUG-07: FreeSlots → FreeCells (ячейки, не предметы)

**Исправленные баги Crafting (3):**
- CRA-BUG-01: Уведомление о потере материалов
- CRA-BUG-02: (уже исправлен INV-H01)
- CRA-BUG-03: GetAvailableRecipes O(N²) → O(N)

### Этап 3: UI ✅

- [x] 3.1 InventoryScreen.cs — Canvas, открытие/закрытие по I ✅
- [x] 3.2 BodyDollPanel.cs — 7 видимых слотов + DollSlotUI ✅
- [x] 3.3 BackpackPanel.cs — динамическая сетка от BackpackData ✅
- [x] 3.4 InventorySlotUI.cs — визуальный слот предмета (v2) ✅
- [x] 3.5 DragDropHandler.cs — перетаскивание между зонами ✅
- [x] 3.6 TooltipPanel.cs — карточка предмета с volume/nesting ✅
- [x] 3.7 UIManager обновлён — интеграция InventoryScreen ✅
- [x] 3.8 Git push ✅

**Коммит:** 3e9d2c7 — Inventory UI v2.0 Этап 3

**Новые файлы (Assets/Scripts/UI/Inventory/):**
| Файл | Назначение |
|------|-----------|
| InventoryScreen.cs | Главный экран инвентаря, управляет всеми панелями |
| BackpackPanel.cs | Diablo-style сетка, размер от BackpackData |
| BodyDollPanel.cs | 7 видимых слотов экипировки + DollSlotUI |
| InventorySlotUI.cs | Визуальная ячейка предмета |
| DragDropHandler.cs | Централизованный drag & drop |
| TooltipPanel.cs | Карточка предмета с volume + allowNesting |

**Особенности реализации:**
- TooltipPanel показывает **объём** и **флаг вложения** (NestingFlag) — NEW v2.0
- DragDropHandler координирует перетаскивание между рюкзаком и куклой
- BackpackPanel динамически перестраивает сетку при смене рюкзака
- BodyDollPanel поддерживает блокировку WeaponOff двуручным оружием
- InventoryScreen интегрирован с UIManager через InventoryScreen component reference

### Этап 4: Духовное хранилище ✅

- [x] 4.1 Создать SpiritStorageController.cs ✅
- [x] 4.2 Каталогизатор: фильтры по категории, редкости, весу, текстовый поиск ✅
- [x] 4.3 Группировка по категории ✅
- [x] 4.4 Стоимость Qi: baseQiCost + weight × qiCostPerKg ✅
- [x] 4.5 Проверка NestingFlag: Any/Spirit → ✅, Ring/None → ❌ ✅
- [x] 4.6 Запрет StorageRingData (пространственная нестабильность) ✅
- [x] 4.7 Разблокировка на уровне культивации AwakenedCore (1) ✅
- [x] 4.8 Save/Load: SpiritStorageSaveData + интеграция в GameSaveData ✅
- [x] 4.9 Проверить компиляцию + git push ✅

**Новый файл:**
| Файл | Назначение |
|------|-----------|
| `Scripts/Inventory/SpiritStorageController.cs` | Духовное хранилище (Межмировая складка) |

**Ключевые классы:**
| Класс | Назначение |
|-------|-----------|
| `SpiritStorageController` | Безлимитное хранилище с каталогизатором |
| `SpiritStorageEntry` | Запись в хранилище (itemId, count, durability, grade) |
| `SpiritStorageSaveData` | Данные для сохранения |
| `SpiritStorageEntrySaveData` | Запись для сохранения |

**API:**
- `StoreFromInventory(slotId, count)` — из инвентаря в складку (списывает Qi)
- `RetrieveToInventory(entryId, count)` — из складки в инвентарь (списывает Qi)
- `StoreDirect(itemData, count, ...)` — напрямую в складку (для загрузки/событий)
- `CanStore(itemData)` — проверка NestingFlag
- `CanStoreWithQi(itemData, count)` — проверка NestingFlag + Qi
- `GetStorageCost(weight)` / `GetRetrievalCost(weight)` — расчёт Qi
- `FilterByCategory()`, `FilterByRarity()`, `FilterByWeight()`, `Search(query)` — каталогизатор
- `GetGroupedByCategory()` — группировка по категориям
- `GetSaveData()` / `LoadSaveData()` — сохранение/загрузка

**Изменённые файлы:**
- `Scripts/Save/SaveManager.cs` — +SpiritStorageController reference, +SpiritStorageData в GameSaveData

### Этап 5: Кольца хранения ✅

- [x] 5.1 Создать StorageRingController.cs ✅
- [x] 5.2 Объём-ограниченное хранилище (volume-based, не weight-based) ✅
- [x] 5.3 NestingFlag: Any/Ring → ✅, Spirit/None → ❌ ✅
- [x] 5.4 Запрет StorageRingData (пространственная нестабильность) ✅
- [x] 5.5 Автоактивация при экипировке StorageRingData на слот кольца ✅
- [x] 5.6 Каталогизатор: фильтры по категории, редкости, объёму, текстовый поиск ✅
- [x] 5.7 Стоимость Qi: qiCostBase + volume × qiCostPerUnit ✅
- [x] 5.8 Save/Load: StorageRingSaveData + интеграция в GameSaveData ✅
- [x] 5.9 EquipmentController: разрешить StorageRingData на слотах колец ✅
- [x] 5.10 StorageRingPanel.cs — каталогизатор UI ✅
- [x] 5.11 InventoryScreen — вкладка кольца хранения ✅
- [x] 5.12 DragDropHandler — контекстное меню «В кольцо хранения» ✅
- [x] 5.13 BodyDollPanel — слоты колец + display names ✅
- [x] 5.14 AssetGeneratorExtended — 4 кольца хранения ✅
- [x] 5.15 SaveManager — save/load StorageRingController ✅

**Новый файл:**
| Файл | Назначение |
|------|-----------|
| `Scripts/Inventory/StorageRingController.cs` | Контроллер кольца хранения |
| `Scripts/UI/Inventory/StorageRingPanel.cs` | UI панель каталогизатора кольца |

**Ключевые классы:**
| Класс | Назначение |
|-------|-----------|
| `StorageRingController` | Объём-ограниченное хранилище с каталогизатором |
| `StorageRingEntry` | Запись в кольце (itemId, count, durability, grade, totalVolume) |
| `StorageRingSaveData` | Данные для сохранения (все слоты) |
| `StorageRingSlotSaveData` | Данные одного слота кольца |
| `StorageRingEntrySaveData` | Запись для сохранения |
| `StorageRingPanel` | UI панель каталогизатора |

**API StorageRingController:**
- `ActivateRing(slot, ringData)` / `DeactivateRing(slot)` — управление кольцами
- `StoreFromInventory(ringSlot, inventorySlotId, count)` — из инвентаря в кольцо
- `RetrieveToInventory(ringSlot, entryId, count)` — из кольца в инвентарь
- `StoreDirect(ringSlot, itemData, count, ...)` — напрямую в кольцо
- `CanStore(ringSlot, itemData)` — проверка NestingFlag + объёма
- `CanStoreWithQi(ringSlot, itemData, count)` — проверка NestingFlag + Qi + объём
- `GetStorageCost(ringSlot, volume)` / `GetRetrievalCost(ringSlot, volume)` — расчёт Qi
- `GetCurrentVolume(slot)` / `GetMaxVolume(slot)` / `GetVolumePercent(slot)` — объём
- `FilterByCategory/FilterByRarity/Search/GetGroupedByCategory` — каталогизатор
- `GetSaveData()` / `LoadSaveData()` — сохранение/загрузка

**Изменённые файлы:**
- `Scripts/Inventory/EquipmentController.cs` — +IsRingSlot(), +RingSlots, разрешить StorageRingData на слотах колец
- `Scripts/UI/Inventory/InventoryScreen.cs` — +storageRingPanel, +StorageTab enum, +SwitchTab()
- `Scripts/UI/Inventory/DragDropHandler.cs` — +storageRingController, +«В кольцо хранения» в контекстном меню
- `Scripts/UI/Inventory/BodyDollPanel.cs` — +ringSlot UIs (4 слота), +ring display names
- `Scripts/Save/SaveManager.cs` — +storageRingController, +StorageRingData в GameSaveData
- `Scripts/Editor/AssetGeneratorExtended.cs` — +GenerateStorageRings() (4 кольца: щель, карман, кладовая, пространство)

**Сгенерированные кольца (4 шт.):**
| Кольцо | Объём | Qi (base+perUnit) | Редкость |
|--------|-------|-------------------|----------|
| Кольцо-щель | 5 | 5 + vol×3 | Common |
| Кольцо-карман | 15 | 5 + vol×2 | Uncommon |
| Кольцо-кладовая | 30 | 5 + vol×1 | Rare |
| Кольцо-пространство | 60 | 5 + vol×0.5 | Epic |

### Отложено (не в текущей сессии):
- Этап 5: Кольца хранения (StorageRingController + объём) ✅ ВЫПОЛНЕН
- Этап 6: Пояс + контекстное меню + анимации

---

## Карта зависимостей — что сломается при переделке

| Файл | Что менять | Сложность |
|------|-----------|-----------|
| PlayerController.cs | API вызовы Inventory — проверить | 🟢 Низкая |
| ResourcePickup.cs | API вызовы Inventory — проверить | 🟢 Низкая |
| QuestController.cs | API вызовы Inventory — проверить | 🟢 Низкая |
| Combatant.cs | EquipmentController (TODO, не реализовано) | 🟢 Без изменений |
| InventoryUI.cs | Полная замена | 🔴 ПЕРЕПИСАТЬ |
| CharacterPanelUI.cs | Полная замена | 🔴 ПЕРЕПИСАТЬ |
| CraftingController.cs | API совместим, багфиксы | 🟡 Средняя |
| AssetGeneratorExtended.cs | ParseEquipmentSlot + новые SO | 🟡 Средняя |
| NPCPresetData.cs | EquipmentSlot | 🟢 Обновить |

---

## Изменённые файлы (будут)

### Этап 0 (модели данных):
- `Scripts/Core/Enums.cs` — EquipmentSlot, NestingFlag, WeaponHandType
- `Scripts/Data/ScriptableObjects/ItemData.cs` — volume, allowNesting, fix sizeHeight
- `Scripts/Data/ScriptableObjects/BackpackData.cs` — НОВЫЙ
- `Scripts/Data/ScriptableObjects/StorageRingData.cs` — НОВЫЙ
- `Scripts/Data/ScriptableObjects/EquipmentData.cs` — +handType
- `Scripts/Generators/AssetGeneratorExtended.cs` — обновить генерацию

### Этап 1 (кукла):
- `Scripts/Inventory/EquipmentController.cs` — ПЕРЕПИСАТЬ

### Этап 2 (рюкзак):
- `Scripts/Inventory/InventoryController.cs` — ПЕРЕПИСАТЬ
- `Scripts/Inventory/CraftingController.cs` — багфиксы

### Этап 3 (UI):
- `Scripts/UI/Inventory/*` — НОВЫЕ файлы

---

## Следующий шаг

Начать Этап 6 — Пояс + контекстное меню + анимации.

---

*Чекпоинт создан: 2026-04-18 18:20:58 UTC*
*Редактировано: 2026-04-19 12:30:00 UTC*

# Чекпоинт: Аудит инвентаря — 5 прогонов + верификация
# Дата первичного аудита: 2026-05-05 14:54:45 MSK
# Дата верификации + прогонов 4-5: 2026-05-05 15:59:29 MSK
# Дата прогонов 6-7 + чекпоинты исправлений: 2026-05-05 16:07:41 MSK
# Статус: audit_complete — исправления в отдельных чекпоинтах
#
# ЧЕКПОИНТЫ ИСПРАВЛЕНИЙ:
#   05_05_combat_loot_fix.md  — Бой + Лут + Техники (13 исправлений)
#   05_05_inventory_ui_fix.md — Инвентарь UI + Хранение + События (19 исправлений)

## Симптом
Предметы генерируются, подбираются, но **не отображаются** в инвентаре.

---

## ВЕРИФИКАЦИЯ БАГОВ (прогоны 1-3)

Все 23 бага из прогонов 1-3 **подтверждены** чтением исходного кода:
- ✅ ПОДТВЕРЖДЁН: 23/23
- ⚠️ ЧАСТИЧНО: 0
- ❌ ОПРОВЕРГНУТ: 0

---

## 🔴 КРИТИЧЕСКИЕ БАГИ (прогоны 1-3)

### БАГ-ИНВ-01: Строки инвентаря неактивны ✅
- **Файл:** `UI/Inventory/BackpackPanel.cs:152`
- Instantiate() без SetActive(true). Префаб деактивирован Phase17:423.

### БАГ-ИНВ-02: Боевой лут не поступает в инвентарь ✅
- **Файл:** `Combat/CombatManager.cs:440`
- OnLootGenerated — 0 подписчиков. LootResult потерян.

### БАГ-ИНВ-12: Equip() уничтожает старый предмет ✅
- **Файл:** `Inventory/EquipmentController.cs:260-265, 282-293`
- oldItem = Unequip(slot) — возвращён, но НИКОГДА НЕ ИСПОЛЬЗУЕТСЯ.
- EquipTwoHand: Unequip(WeaponOff) + Unequip(WeaponMain) — оба теряются.

### БАГ-ИНВ-13: EquipmentData стакается ✅
- **Файл:** `Data/ScriptableObjects/ItemData.cs:58`
- stackable=true по умолчанию. EquipmentData наследует.
- EquipmentSOFactory ставит stackable=false, но ручные SO — нет.

---

## 🟠 ВЫСОКИЕ БАГИ (прогоны 1-3)

### БАГ-ИНВ-03: GameEvents.OnItemAdded никогда не вызывается ✅
- 5 событий: TriggerItemAdded/Removed/Equipped/Unequipped/Crafted — 0 вызовов, 0 подписчиков.

### БАГ-ИНВ-04: ServiceLocator.Get вместо GetOrFind ✅
- ResourcePickup.cs:147 — Get<> → null. InventoryController не зарегистрирован.

### БАГ-ИНВ-14: Предмет уничтожается без инвентаря ✅
- ResourcePickup.cs:199-201 — return true → Destroy(gameObject).

### БАГ-ИНВ-05: InventorySlotUI — 3 поля не подключены ✅
- background, border, durabilityBar — не созданы/не подключены в Phase17.

### БАГ-ИНВ-06: spiritStoragePanel не подключена ✅
- CreateSpiritStoragePanel() — void, не возвращает GO. WireInventoryScreenReferences не назначает.

### БАГ-ИНВ-15: InventoryController не зарегистрирован в ServiceLocator ✅
- Register<InventoryController> — 0 результатов. EquipmentController тоже.

### БАГ-ИНВ-16: OnInventoryFull — 0 подписчиков ✅
- InventoryController.cs:95 — событие стреляет, никто не слушает.

---

## 🟡 СРЕДНИЕ БАГИ (прогоны 1-3)

### БАГ-ИНВ-17: Дублирующиеся таблицы маппинга ✅
- PlayerController vs ResourcePickup — разные nameRu, maxStack для одного resourceId.

### БАГ-ИНВ-18: ResourceSpawner без ItemData ✅
- Initialize(resourceId, amount) — 2-параметровый overload без ItemData.

### БАГ-ИНВ-07: Hover сбрасывает Grade-тинт ✅
- SetHoverHighlight(false) → normalColor вместо grade tint.

### БАГ-ИНВ-19: AddItem — частичное добавление ✅
- Возвращает InventorySlot, а не addedCount. ResourcePickup Destroy даже при частичном подборе.

### БАГ-ИНВ-20: LoadSaveData теряет предметы через CanFitItem ✅
- AddItem() внутри LoadSaveData проверяет лимиты — предметы тихо теряются.

### БАГ-ИНВ-21: Texture2D утечка из CreateProceduralIcon ✅
- new Texture2D никогда не Destroy.

---

## 🟢 НИЗКИЕ БАГИ (прогоны 1-3)

### БАГ-ИНВ-22: HasFreeSpace() всегда true ✅
### БАГ-ИНВ-23: ScrollRect без Scrollbar ✅
### БАГ-ИНВ-24: Дублирующий ContextMenu GO ✅
### БАГ-ИНВ-25: Анимация открытия/закрытия не реализована ✅
### БАГ-ИНВ-26: Hard-coded "Player" тег ✅
### БАГ-ИНВ-27: LootEntry — string ItemId, нет ItemData ✅

---
---

# ПРОГОН 4: ПОТРЕБИТЕЛИ ИНВЕНТАРЯ

## Обнаруженные системы (10)

| Система | Статус | Связь с инвентарём |
|---------|--------|-------------------|
| Save/Load | ✅ Реальная | Прямая: GetSaveData/LoadSaveData |
| Crafting | ✅ Реальная | Прямая: AddItem/RemoveItemById |
| Quests | ✅ Реальная | Lazy ref, но мост сломан |
| Trading | ❌ Не существует | NPCType.Merchant определён, но нет реализации |
| Achievement | 🟡 Stub | Только AchievementSaveData, нет контроллера |
| Tutorial | 🟡 Stub | Только флаг ShowTutorials, нет системы |
| Consumable | 🟠 Частичная | UseItem() удаляет, но 0 эффектов |
| Drop/Discard | 🟠 Частичная | RemoveSlot() без спавна в мире |
| Sort/Filter | ✅ Частичная | SortInventory есть, Filter на Backpack — нет |
| DeathLoot | ✅ Реальная | Генерирует, но НЕ доставляет |

## 🔴 КРИТИЧЕСКИЕ БАГИ (прогон 4)

### БАГ-ИНВ-28: QuestController.NotifyItemCollected — никогда не вызывается
- **Файл:** `Quest/QuestController.cs:452-466`
- **Причина:** InventoryController.AddItem() не вызывает GameEvents.TriggerItemAdded(),
  а никто не вызывает NotifyItemCollected напрямую.
- **Следствие:** Квесты на сбор предметов **никогда не обновляются**.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-29: Квестовые награды теряются при Resources.Load
- **Файл:** `Quest/QuestController.cs:690`
- **Причина:** `Resources.Load<ItemData>($"Items/{itemId}")` — хардкод путь.
  Большинство предметов — runtime SO, не в Resources. Награда теряется.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-30: UseItem() удаляет предмет без эффекта
- **Файл:** `UI/Inventory/DragDropHandler.cs:579-584`
- **Причина:** RemoveItem() вызывается, но эффект (лечение, бафф) **не применяется**.
- **Следствие:** Расходники исчезают бесполезно.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-31: Нет системы ConsumableEffect
- **Причина:** ItemData не имеет полей для эффектов (healAmount, buff, duration).
  Вся механика расходников — no-op.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-32: Crafting LoadSaveData не восстанавливает рецепты
- **Файл:** `Crafting/CraftingController.cs:587-598`
- **Причина:** LoadSaveData восстанавливает craftingSkills, но **игнорирует** knownRecipeIds.
  Изученные рецепты теряются при загрузке.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-33: Craft objectives квестов никогда не триггерятся
- **Файл:** `Quest/QuestController.cs`
- **Причина:** QuestObjectiveType.Craft определён, но CraftingController
  никогда не вызывает GameEvents.TriggerItemCrafted() или аналогичный метод.
- **Статус:** ⏳ не исправлено

## 🟠 ВЫСОКИЕ БАГИ (прогон 4)

### БАГ-ИНВ-34: Quest.CheckRequirements — инвентарь не проверяется
- **Файл:** `Quest/QuestData.cs:93-134`
- **Причина:** CheckRequirements принимает Dictionary<string, int> inventoryItems,
  но **никто не собирает** этот словарь из InventoryController.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-35: Квестовые награды — золото/дух.камни не реализованы
- **Файл:** `Quest/QuestController.cs:664-675`
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-36: Drop — предметы уничтожаются безвозвратно
- **Файл:** `UI/Inventory/DragDropHandler.cs:620-625`
- **Причина:** RemoveSlot() удаляет, но ResourcePickup в мир не спавнится.
  Нет подтверждения перед удалением.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-37: Торговля — система отсутствует полностью
- NPCType.Merchant определён, но TradeController/ShopController не существует.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-38: Achievement — мёртвый stub
- AchievementSaveData существует, но нет AchievementController.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-39: Equipment Load — парсинг строки → enum может потерять предмет
- **Файл:** `Save/SaveManager.cs:855`
- **Причина:** EquipmentArrayToDict использует строковый ключ для парсинга EquipmentSlot.
  Ошибка парсинга → потеря экипировки.
- **Статус:** ⏳ не исправлено

## 🟡 СРЕДНИЕ БАГИ (прогон 4)

### БАГ-ИНВ-40: Crafting — материалы тратятся до броска успеха
- **Файл:** `Crafting/CraftingController.cs:170-172`
- Нет диалога подтверждения. materialsLost = true — по дизайну, но без UI.

### БАГ-ИНВ-41: Save — Formation/Buff/Tile/Charger сохранены, но не восстановлены
- **Файл:** `Save/SaveManager.cs:534-535`
- Собираются при сохранении, но при загрузке — отбрасываются.

### БАГ-ИНВ-42: NewGame() не сбрасывает инвентарь/экипировку/крафт
- **Файл:** `Save/SaveManager.cs:877-884`

### БАГ-ИНВ-43: SortInventory → OnInventoryRebuilt, но UI может не обновиться
- **Файл:** `Inventory/InventoryController.cs:663`

### БАГ-ИНВ-44: Нет фильтра/поиска для основного инвентаря
- SpiritStorage и StorageRing имеют Filter, Backpack — нет.

### БАГ-ИНВ-45: Drag-to-outside ничего не делает
- **Файл:** `UI/Inventory/DragDropHandler.cs:214-216`
- DropTarget.Outside → «пока не реализуем»

### БАГ-ИНВ-46: Tutorial flag — ShowTutorials сохраняется, но не проверяется
- **Файл:** `Save/SaveDataTypes.cs:192`

---
---

# ПРОГОН 5: КРОСС-СИСТЕМНЫЕ ВЗАИМОДЕЙСТВИЯ И ГРАНИЧНЫЕ СЛУЧАИ

## 🔴 КРИТИЧЕСКИЕ БАГИ (прогон 5)

### БАГ-ИНВ-47: NullRef crash при null ItemData в слоте
- **Файл:** `Inventory/InventoryController.cs:392, 617, 628, 638, 666`
- **Причина:** RemoveItemById, FindSlotWithItem, CountItem, SortInventory
  обращаются к `slot.ItemData.itemId` без null-проверки.
  Повреждённое сохранение → NullReferenceException.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-48: Пустой itemId — некорректное стекание
- **Файл:** `Inventory/InventoryController.cs:312`
- **Причина:** Если два разных предмета имеют itemId="" — они объединяются в один стек.
  CreateTemporaryItemData() не валидирует уникальность itemId.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-49: Resources.LoadAll<ItemData>() на КАЖДЫЙ подбор — катастрофа производительности
- **Файл:** `Tile/ResourcePickup.cs:213`
- **Причина:** FindItemDataById() вызывает Resources.LoadAll<ItemData>("Items") без кэша.
  50 предметов на карте → 50 вызовов LoadAll.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-50: Инвентарь теряется при смене сцены
- **Файл:** `GameManager.cs:110` vs Player
- **Причина:** GameManager: DontDestroyOnLoad. InventoryController на Player — НЕТ.
  При смене сцены Player уничтожается → инвентарь потерян.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-51: Silent data loss при загрузке — отсутствующий ItemData SO
- **Файл:** `Inventory/InventoryController.cs:754`
- **Причина:** LoadSaveData: itemDatabase.TryGetValue → false → предмет тихо пропущен.
  Нет warning, нет fallback.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-52: Silent equipment loss при загрузке — отсутствующий EquipmentData SO
- **Файл:** `Inventory/EquipmentController.cs:944`
- **Причина:** Аналогично БАГ-ИНВ-51 для экипировки.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-53: Множественные одновременные AddItem — нет защиты
- **Файл:** `Tile/ResourcePickup.cs:67` + `Inventory/InventoryController.cs:298`
- **Причина:** Игрок подбирает 20 предметов за 1 кадр → 20 AddItem().
  rawWeight/rawVolume инкрементально, события OnItemAdded могут вызвать RefreshList()
  в середине AddItem — данные в неконсистентном состоянии.
- **Статус:** ⏳ не исправлено

## 🟠 ВЫСОКИЕ БАГИ (прогон 5)

### БАГ-ИНВ-54: Сломанная экипировка (durability=0) даёт полные статы
- **Файл:** `Inventory/EquipmentController.cs:850-854`
- **Причина:** DamageEquipment() при durability=0 только Debug.Log.
  Предмет остаётся экипированным и продолжает давать статы.
  GetDurabilityMultiplier не проверяет durability, только grade.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-55: UnequipToInventory перезаписывает durability при merge в стек
- **Файл:** `Inventory/InventoryController.cs:586-592`
- **Причина:** AddItem() может объединить снятый предмет в существующий стек,
  затем `slot.durability = instance.durability` перезаписывает durability стека.
  Стек из 2 мечей получает durability только последнего.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-56: Runtime ScriptableObjects утечка памяти
- **Файл:** `Tile/ResourcePickup.cs:232`
- **Причина:** CreateTemporaryItemData() создаёт SO, которые никогда не Destroy.
  Сотни подборов за сессию → утечка.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-57: Предметы теряются при загрузке в меньший рюкзак
- **Файл:** `Inventory/InventoryController.cs:756`
- **Причина:** LoadSaveData вызывает AddItem() с проверкой CanFitItem.
  Если рюкзак изменился — предметы тихо отбрасываются.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-58: Stale drag reference — слот изменён во время перетаскивания
- **Файл:** `UI/Inventory/DragDropHandler.cs:286-296`
- **Причина:** draggedSlotUI.Slot указывает на удалённый InventorySlot.
  Если предмет удалён другим путём во время drag — операция на мёртвом объекте.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-59: maxStack=0 на runtime SO — потенциальный бесконечный цикл
- **Файл:** `Inventory/InventoryController.cs:339`
- **Причина:** [Range(1,999)] работает в Inspector, но runtime SO его не проверяют.
  maxStack=0 → toPlace=0 → while(remaining>0) может зависнуть.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-60: Отрицательный weight/volume — коррупция тоталов
- **Файл:** `Inventory/InventoryController.cs:327-328`
- **Причина:** Нет валидации ItemData.weight/volume. Отрицательные значения
  позволяют добавить бесконечное число предметов.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-61: WeaponOff потерян при экипировке 2H оружия
- **Файл:** `Inventory/EquipmentController.cs:283-286`
- **Причина:** Unequip(WeaponOff) — возвращаемое значение отброшено.
  (Дубль БАГ-ИНВ-12, но акцент на WeaponOff)
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-62: Хрупкий порядок инициализации
- **Файл:** `UI/Inventory/InventoryScreen.cs:91-92`
- **Причина:** Awake() → Initialize() → ServiceLocator.GetOrFind.
  Если InventoryController.Awake() ещё не выполнился — GetOrFind возвращает null.
  GameInitializer не инициализирует инвентарь явно.
- **Статус:** ⏳ не исправлено

## 🟡 СРЕДНИЕ БАГИ (прогон 5)

### БАГ-ИНВ-63: Нет cooldown/рейт-лимита на подбор
- Игрок в куче из 20 предметов → 20 OnTriggerEnter2D за кадр.

### БАГ-ИНВ-64: InventoryController растёт без ограничений
- Нет лимита на количество слотов (только вес/объём).

### БАГ-ИНВ-65: GameInitializer не инициализирует системы инвентаря
- **Файл:** `Core/GameInitializer.cs:174-240`
- 8 шагов инициализации, ни один не затрагивает Inventory/Equipment.

### БАГ-ИНВ-66: Боевая механика не снимает сломанную экипировку автоматически
- **Файл:** `Inventory/EquipmentController.cs:850`
- durability=0 → только Debug.Log, предмет остаётся надетым.

---
---

## ИТОГОВАЯ СВОДНАЯ СТАТИСТИКА

| Серьёзность | Прогоны 1-3 | Прогон 4 | Прогон 5 | ИТОГО |
|-------------|:-----------:|:--------:|:--------:|:-----:|
| 🔴 КРИТИЧЕСКИЕ | 4 | 6 | 7 | **17** |
| 🟠 ВЫСОКИЕ | 6 | 6 | 8 | **20** |
| 🟡 СРЕДНИЕ | 7 | 7 | 4 | **18** |
| 🟢 НИЗКИЕ | 6 | 0 | 0 | **6** |
| **ВСЕГО** | **23** | **19** | **19** | **61** |

*(Некоторые баги из прогонов 4-5 пересекаются с прогонами 1-3 по корневой причине,
но раскрывают разные аспекты)*

---

## ТОП-10 КРИТИЧЕСКИХ ИСПРАВЛЕНИЙ (P0)

| # | Баг | Влияние | Объём |
|---|-----|---------|-------|
| 1 | БАГ-ИНВ-01: SetActive(true) | Инвентарь невидим | 1 строка |
| 2 | БАГ-ИНВ-02: Боевой лут потерян | Бой без награды | ~30 строк |
| 3 | БАГ-ИНВ-12: Equip() теряет старый предмет | Потеря экипировки | ~15 строк |
| 4 | БАГ-ИНВ-13: EquipmentData стакается | Потеря grade/durability | ~5 строк |
| 5 | БАГ-ИНВ-28: Quest NotifyItemCollected | Квесты не работают | ~5 строк |
| 6 | БАГ-ИНВ-30: UseItem() без эффекта | Расходники бесполезны | ~20 строк |
| 7 | БАГ-ИНВ-47: NullRef на null ItemData | Краш при повреждённых данных | ~5 строк |
| 8 | БАГ-ИНВ-49: Resources.LoadAll каждый подбор | Катастрофа производительности | ~10 строк |
| 9 | БАГ-ИНВ-50: Инвентарь теряется при смене сцены | Полная потеря данных | ~3 строки |
| 10 | БАГ-ИНВ-51: Silent loss при загрузке | Предметы теряются бесследно | ~5 строк |

---

## ДИАГРАММА: ПОТРЕБИТЕЛИ ИНВЕНТАРЯ (прогон 4)

```
                    ┌─────────────────────┐
                    │  InventoryController │
                    │    (центральный)     │
                    └──┬──┬──┬──┬──┬──┬──┘
                       │  │  │  │  │  │
     ┌─────────────────┘  │  │  │  │  └───────────────┐
     │                    │  │  │  │                    │
 ┌───▼────┐  ┌───────────┘  │  │  └───────────┐  ┌────▼────┐
 │Crafting │  │              │  │              │  │ Resource│
 │  Ctrl   │  │              │  │              │  │  Pickup │
 │(R/W ✅) │  │              │  │              │  │ (W ✅)  │
 └─────────┘  │              │  │              │  └─────────┘
           ┌──▼──────┐  ┌───▼──▼──┐  ┌────────▼───────┐
           │  Quest   │  │  Save   │  │ DragDropHandler│
           │  Ctrl    │  │ Manager │  │  (R/W ✅)      │
           │(W ❌!!)  │  │(R/W ✅) │  └───────┬────────┘
           └──────────┘  └─────────┘          │
                                              ▼
                                    ┌─────────────────┐
                                    │ UseItem ❌       │
                                    │ DropItem ⚠️     │
                                    │ DragEquip ✅    │
                                    └─────────────────┘

     ─── ОТКЛЮЧЕННЫЕ ──────────────────────────────────

     GameEvents.OnItemAdded      ← 0 вызовов, 0 подписчиков
     QuestController.NotifyItem  ← 0 вызовов
     AchievementController       ← НЕ СУЩЕСТВУЕТ
     TradeController             ← НЕ СУЩЕСТВУЕТ
     ConsumableEffect            ← НЕ СУЩЕСТВУЕТ
     LootResult → Inventory     ← PIPELINE СЛОМАН
```

---

## ЗАМЕТКИ
- БАГ-ИНВ-01 — **прямая причина** симптома «предметы не отображаются» (1 строка!)
- БАГ-ИНВ-12 + БАГ-ИНВ-55 — потеря экипировки при замене + corruption durability при unequip
- БАГ-ИНВ-28 — блокирует ВСЕ квесты на сбор предметов
- БАГ-ИНВ-03 — корневая причина разрыва GameEvents (блокирует квесты, достижения, отслеживание)
- БАГ-ИНВ-49 — Resources.LoadAll без кэша — может вызвать фризы
- БАГ-ИНВ-50 — инвентарь не переживёт смену сцены
- ConsumableEffect (БАГ-ИНВ-31) — целая система отсутствует
- Trading (БАГ-ИНВ-37) — целая система отсутствует
- 5 систем GameEvents полностью мертвы (0 вызовов, 0 подписчиков)

---

# ПРОГОН 6: БАЗОВЫЙ ИГРОВОЙ ЦИКЛ (2026-05-05 16:07:41 MSK)

## Ключевые находки

### BUG-DMG-01: Оружие не даёт урон при базовой атаке 🔴
- `Combat/DamageCalculator.cs:203-221`
- PlayerController.GetAttackerParams() → CombatSubtype = MeleeStrike
- Полная формула оружия требует MeleeWeapon → оружие не влияет на урон

### BUG-LOOT-01: OnLootGenerated — 0 подписчиков 🔴
- Подтверждено: весь лут из боя потерян (аналог БАГ-ИНВ-02)

### BUG-STATS-01: Статы игрока = 10 🟡
- PlayerController.cs:120-123 — STR/AGI/INT/VIT захардкожены

### ✅ Armor → DefenseProcessor работает корректно
### ✅ NPC может атаковать (CombatAI + NPCAI)
### ✅ Техники заряжаются, cooldown работает

---

# ПРОГОН 7: NPC + ТЕХНИКИ + ИНТЕГРАЦИЯ (2026-05-05 16:07:41 MSK)

## Ключевые находки

### BUG-NPC-01: Мёртвые NPC не удаляются 🔴
- NPCController.Die() — GameObject остаётся в сцене

### BUG-NPC-02: CombatTrigger.ShouldEngage проверяет отношение ЦЕЛИ 🔴
- Враждебные NPC не могут автоматически напасть через CombatTrigger
- Workaround: NPCAI.CheckAggroRadius() работает

### BUG-TECH-01: Заряженные техники не наносят урон 🔴
- TechniqueChargeSystem.FireChargedTechnique() → CombatManager НЕ вызывается
- Влияет на ИГРОКА и NPC

### BUG-FORM-01: FormationBuffMultiplier всегда 1.0 🔴
- Формационные бонусы защиты не работают в бою

### BUG-NPC-06: Только 1v1 бой 🟡
- CombatManager.InitiateCombat() отклоняет, если бой уже идёт

### BUG-NPC-08: NPCAI + CombatAI — двойная атака 🟡
- Обе системы независимо вызывают ExecuteBasicAttack

---

## ИТОГ: 7 ПРОГОНОВ АУДИТА

| Прогон | Область | Багов |
|--------|---------|-------|
| 1-3 | Генерация/Подбор/Хранение/UI | 23 |
| 4 | Потребители инвентаря | 19 |
| 5 | Кросс-системные + граничные | 19 |
| 6 | Базовый игровой цикл | 7 |
| 7 | NPC + Техники + Интеграция | 12 |
| **ИТОГО** | | **80** |

## ЧЕКПОИНТЫ ИСПРАВЛЕНИЙ
- `05_05_combat_loot_fix.md` — 13 исправлений (бой + лут + техники)
- `05_05_inventory_ui_fix.md` — 19 исправлений (UI + хранение + события)
- **Итого исправлений:** 32

# Чекпоинт: Аудит инвентаря — 3 прогона (генерация/подбор → хранение/события → UI)
# Дата первичного аудита: 2026-05-05 14:54:45 MSK
# Дата 3 прогонов: 2026-05-05 14:54:45 MSK
# Статус: in_progress

## Симптом
Предметы генерируются, подбираются, но **не отображаются** в инвентаре.

---

## 🔴 КРИТИЧЕСКИЕ БАГИ

### БАГ-ИНВ-01: Строки инвентаря неактивны (SetActive(true) не вызывается)
- **Файл:** `UI/Inventory/BackpackPanel.cs:152`
- **Прогон:** 3 (UI)
- **Причина:** `Phase17InventoryUI.cs:423` создаёт префаб строки `slotGO.SetActive(false)`.
  При `Instantiate()` клон наследует inactive-состояние, но `rowGO.SetActive(true)` **нигде не вызывается**.
- **Следствие:** Все строки рюкзака существуют в иерархии, но невидимы.
  Инвентарь **полностью нефункционален** — пользователь видит пустую панель.
- **Исправление:** Добавить `rowGO.SetActive(true)` после `Instantiate()` в `CreateRowUI()`
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-02: Боевой лут не поступает в инвентарь игрока
- **Файл:** `Combat/CombatManager.cs:440`
- **Прогон:** 1 (Генерация/подбор)
- **Причина:** `OnLootGenerated` вызывается, но **нет ни одного подписчика**.
  `LootResult` (предметы + Qi + XP) генерируется и логируется, но никогда не передаётся
  в `InventoryController` или `QiController` игрока.
- **Уточнение:** `LootEntry` содержит только `string ItemId`, без `ItemData` —
  нужен резолвер для конвертации ItemId → ItemData.
- **Следствие:** Весь лут из боя потерян. QiAbsorbed и CultivationExp не начисляются.
- **Исправление:** Создать LootCollector на Player GO, подписать на OnLootGenerated,
  конвертировать LootEntry.ItemId → ItemData, вызвать AddItem(), применить Qi/Exp
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-12: Equip() уничтожает старый предмет при замене
- **Файл:** `Inventory/EquipmentController.cs:260-265, 282-293`
- **Прогон:** 2 (Хранение/события)
- **Причина:** При экипировке нового предмета в занятый слот:
  `oldItem = Unequip(slot)` — снятый предмет **возвращён, но НИКОГДА НЕ ИСПОЛЬЗУЕТСЯ**.
  Для 2H оружия: `Unequip(WeaponOff)` + `Unequip(WeaponMain)` — оба теряются.
- **Следствие:** Снятое оружие/броня при замене безвозвратно удаляется.
- **Исправление:** EquipmentController.Equip() должен возвращать oldItem вызывающему
  (InventoryController.EquipFromInventory), чтобы тот положил его в рюкзак
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-13: EquipmentData стакается (stackable=true по умолчанию)
- **Файл:** `Data/ScriptableObjects/ItemData.cs:58` + `InventoryController.cs:307-333`
- **Прогон:** 2 (Хранение/события)
- **Причина:** `ItemData.stackable` по умолчанию `true`. `EquipmentData` наследует это.
  При AddItem() два меча одного типа но разного грейда объединятся в один стек,
  grade/durability второго — **потеряны**.
- **Исправление:** Принудительно `stackable = false` для EquipmentData в фабрике,
  или проверка `is EquipmentData` в AddItem() → всегда новый слот
- **Статус:** ⏳ не исправлено

---

## 🟠 ВЫСОКИЕ БАГИ

### БАГ-ИНВ-03: GameEvents.OnItemAdded никогда не вызывается
- **Файл:** `Inventory/InventoryController.cs:298-364` vs `Core/GameEvents.cs:571-597`
- **Прогон:** 1+2 (Генерация + Хранение)
- **Причина:** InventoryController.AddItem() стреляет локальными событиями,
  но **никогда не вызывает** `GameEvents.TriggerItemAdded()`.
  Аналогично для TriggerItemRemoved/TriggerItemEquipped/TriggerItemUnequipped/TriggerItemCrafted.
- **Следствие:** Квесты, достижения, туториалы — полностью изолированы от инвентаря.
- **Исправление:** Построить мост — добавить GameEvents.TriggerXxx() в AddItem/RemoveFromSlot/Equip/Unequip
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-04: ResourcePickup — ServiceLocator.Get вместо GetOrFind
- **Файл:** `Tile/ResourcePickup.cs:147`
- **Прогон:** 1 (Генерация/подбор)
- **Причина:** InventoryController **не зарегистрирован** в ServiceLocator.
  `Get<InventoryController>()` → всегда null. `GetOrFind` нашёл бы через FindFirstObjectByType.
- **Следствие:** Fallback-путь подбора не работает, предмет теряется.
- **Исправление:** Заменить `Get` → `GetOrFind` (или зарегистрировать InventoryController)
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-14: ResourcePickup уничтожает предмет при отсутствии инвентаря
- **Файл:** `Tile/ResourcePickup.cs:199-201`
- **Прогон:** 1 (Генерация/подбор)
- **Причина:** Если InventoryController не найден, `TryAddToInventory` возвращает `true`,
  и Pickup() вызывает `Destroy(gameObject)`. Предмет исчезает из мира бесследно.
- **Исправление:** Возвращать `false` при отсутствии инвентаря — предмет остаётся в мире
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-05: Не подключены поля InventorySlotUI в Phase17
- **Файл:** `Editor/SceneBuilder/Phase17InventoryUI.cs:411-420`
- **Прогон:** 3 (UI)
- **Причина:** Из 8 `[SerializeField]` полей подключены только 5:
  - ✅ iconImage, nameText, countText, weightText, volumeText
  - ❌ background (Image на root GO) — не подключена
  - ❌ border (не создана)
  - ❌ durabilityBar (не создана)
- **Следствие:** Нет подсветки грейда, нет рамки редкости, нет полоски прочности.
  Hover-подсветка не работает (background = null).
- **Исправление:** Подключить background, создать и подключить border и durabilityBar
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-06: spiritStoragePanel не подключена к InventoryScreen
- **Файл:** `Editor/SceneBuilder/Phase17InventoryUI.cs:906,133-176`
- **Прогон:** 3 (UI)
- **Причина:** CreateSpiritStoragePanel() — void, не возвращает GO.
  WireInventoryScreenReferences() не назначает spiritStoragePanel.
- **Следствие:** Вкладка Spirit Storage **полностью нефункциональна** — кнопка есть, панель не показывается.
- **Исправление:** Вернуть GO из CreateSpiritStoragePanel, подключить в WireInventoryScreenReferences
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-15: InventoryController не зарегистрирован в ServiceLocator
- **Файл:** Вся кодовая база
- **Прогон:** 2 (Хранение/события)
- **Причина:** Ни в одном файле не вызывается `ServiceLocator.Register<InventoryController>()`.
  EquipmentController тоже не зарегистрирован.
- **Следствие:** ServiceLocator.Get<T>() → всегда null. Только GetOrFind работает.
- **Исправление:** Зарегистрировать через RegisteredBehaviour<T> или в GameInitializer
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-16: OnInventoryFull — нулевые подписчики
- **Файл:** `Inventory/InventoryController.cs:95`
- **Прогон:** 2 (Хранение/события)
- **Причина:** Событие стреляет при переполнении, но **ни один код не подписан**.
- **Следствие:** Пользователь не получает уведомления при полном инвентаре.
- **Исправление:** Создать ToastController для подписки на OnInventoryFull
- **Статус:** ⏳ не исправлено

---

## 🟡 СРЕДНИЕ БАГИ

### БАГ-ИНВ-17: Дублирующиеся таблицы маппинга resourceId → ItemData
- **Файлы:** `Player/PlayerController.cs:855-922`, `Tile/ResourcePickup.cs:228-260`
- **Прогон:** 1 (Генерация/подбор)
- **Причина:** PlayerController.CreateResourceItemData() и ResourcePickup.CreateTemporaryItemData()
  независимо маппят resourceId → ItemData с **разными результатами**:
  - herb: "Трава"/maxStack=50 vs "Целебная трава"/maxStack=99
  - berries: "Ягоды"/maxStack=30 vs "Ягоды"/maxStack=99
- **Следствие:** Предметы одного типа подобранные через разные пути — разные метаданные,
  проблемы со стаками, разные лимиты.
- **Исправление:** Единый ItemDatabase или статический метод для resourceId → ItemData
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-18: ResourceSpawner создаёт ResourcePickup без ItemData
- **Файл:** `Tile/ResourceSpawner.cs:299-303`
- **Прогон:** 1 (Генерация/подбор)
- **Причина:** Вызывает Initialize(resourceId, amount) — 2-параметровый overload без ItemData.
  При подборе — дорогой fallback: Resources.LoadAll<ItemData>() или CreateTemporaryItemData().
- **Исправление:** Передавать ItemData при создании ResourcePickup
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-07: Hover сбрасывает Grade-тинт
- **Файл:** `UI/Inventory/InventorySlotUI.cs:230-236`
- **Прогон:** 3 (UI)
- **Причина:** SetHoverHighlight(false) сбрасывает цвет в `normalColor` вместо
  оригинального grade-тинта. После наведения на экипировку подсветка грейда пропадает.
- **Исправление:** Сохранять текущий цвет фона, восстанавливать при un-hover
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-19: AddItem частичное добавление — вызывающий не знает сколько добавлено
- **Файл:** `Inventory/InventoryController.cs:298-364`
- **Прогон:** 2 (Хранение/события)
- **Причина:** AddItem(itemData, 50) при лимите 30 — добавит 30, вернёт lastSlot.
  Вызывающий не может узнать сколько реально добавлено.
  ResourcePickup при этом Destroy(gameObject) даже если подобрана только часть.
- **Исправление:** Изменить сигнатуру на `(int addedCount, InventorySlot lastSlot)` или TryAddItem с out
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-20: LoadSaveData теряет предметы через CanFitItem
- **Файл:** `Inventory/InventoryController.cs:746-770`
- **Прогон:** 2 (Хранение/события)
- **Причина:** LoadSaveData вызывает AddItem() для каждого слота. AddItem проверяет лимиты.
  Если рюкзак ещё не назначен (load order), лимиты базовые (30кг/50л),
  предметы сверх лимита **тихо отбрасываются**.
- **Исправление:** LoadSaveData: временно отключить лимиты при загрузке
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-21: Texture2D утечка из CreateProceduralIcon
- **Файл:** `Generators/EquipmentSOFactory.cs:278-307`
- **Прогон:** 1 (Генерация/подбор)
- **Причина:** Каждый CreateRuntimeFromWeapon/Armor вызывает CreateProceduralIcon() →
  new Texture2D, никогда не уничтожается. Утечка GPU памяти.
- **Исправление:** Отслеживать и Destroy текстуры при удалении предмета
- **Статус:** ⏳ не исправлено

---

## 🟢 НИЗКИЕ БАГИ

### БАГ-ИНВ-22: HasFreeSpace() всегда true
- **Файл:** `Inventory/InventoryController.cs:283-288`
- **Прогон:** 2
- **Причина:** `return true; // Легаси-совместимость`
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-23: ScrollRect без Scrollbar
- **Файл:** `Editor/SceneBuilder/Phase17InventoryUI.cs:767-771`
- **Прогон:** 3
- **Причина:** verticalScrollbarVisibility = AutoHide, но verticalScrollbar = null
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-24: Дублирующий ContextMenu GO
- **Файл:** `Editor/SceneBuilder/Phase17InventoryUI.cs:1550-1565`
- **Прогон:** 3
- **Причина:** Standalone ContextMenu GO создан, но никогда не используется.
  Реальное меню создаётся из contextMenuPrefab в DragDropHandler.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-25: Анимация открытия/закрытия не реализована
- **Файл:** `UI/Inventory/InventoryScreen.cs:60-62`
- **Прогон:** 3
- **Причина:** Поля openCloseDuration и openCurve существуют, но не используются.
  SetActive(true/false) мгновенное.
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-26: Hard-coded "Player" тег в ResourcePickup
- **Файл:** `Tile/ResourcePickup.cs:72`
- **Прогон:** 1
- **Причина:** `other.CompareTag("Player")` — NPC не могут подбирать предметы
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-27: LootEntry содержит string ItemId, нет ItemData
- **Файл:** `Combat/LootGenerator.cs:29-47`
- **Прогон:** 1
- **Причина:** Нет резолвера для конвертации ItemId → ItemData.
  Блокирует исправление БАГ-ИНВ-02.
- **Статус:** ⏳ не исправлено

---

## 📋 ДИАГРАММА: ПОЛНАЯ ЦЕПОЧКА ДАННЫХ (3 прогона)

```
═══════════════════ ПРОГОН 1: ГЕНЕРАЦИЯ И ПОДБОР ═══════════════════

Генерация предмета
  ├─ ResourceSpawner.SpawnSingleResource()
  │     └─ ResourcePickup.Initialize(resourceId, amount) ← БЕЗ ItemData! (БАГ-ИНВ-18)
  ├─ EquipmentRuntimeSpawner.SpawnEquipmentPickup()
  │     └─ LootGenerator → EquipmentSOFactory → ResourcePickup с EquipmentData ✅
  │        └─ Texture2D утечка (БАГ-ИНВ-21)
  ├─ EquipmentRuntimeSpawner.AddToPlayerInventory() → AddItem() ✅
  │     └─ Но: GameObject.Find("Player") O(n), без кэша
  └─ DeathLootGenerator.GenerateLoot() → OnLootGenerated
        └─ ❌ НОЛЬ ПОДПИСЧИКОВ (БАГ-ИНВ-02)
        └─ LootEntry.ItemId = string, нет ItemData (БАГ-ИНВ-27)

Подбор предмета
  ├─ ResourcePickup.OnTriggerEnter2D ("Player" tag — БАГ-ИНВ-26)
  │     └─ Pickup() → TryAddToInventory()
  │           ├─ picker.GetComponent<InventoryController>()
  │           ├─ ServiceLocator.Get<InventoryController>() → NULL! (БАГ-ИНВ-04)
  │           ├─ FindItemDataById() → Resources.LoadAll (дорого)
  │           ├─ CreateTemporaryItemData() → новый runtime SO (БАГ-ИНВ-17)
  │           └─ Нет инвентаря → return true → Destroy (БАГ-ИНВ-14)
  ├─ PlayerController.HarvestHit() → AddResourceToInventory() ✅
  │     └─ Но: CreateResourceItemData() — другое маппинг-таблица (БАГ-ИНВ-17)
  └─ InventoryController.AddItem() → OnItemAdded ✅
        └─ GameEvents.TriggerItemAdded ← НИКОГДА НЕ ВЫЗЫВАЕТСЯ (БАГ-ИНВ-03)

═══════════════════ ПРОГОН 2: ХРАНЕНИЕ И СОБЫТИЯ ═══════════════════

Хранение
  └─ InventoryController: List<InventorySlot>
       └─ InventorySlot: ItemData, Count, durability, grade
       ├─ AddItem(): стак для EquipmentData! (БАГ-ИНВ-13)
       ├─ AddItem(): частичное добавление без уведомления (БАГ-ИНВ-19)
       ├─ LoadSaveData: CanFitItem отбрасывает предметы (БАГ-ИНВ-20)
       ├─ HasFreeSpace() → всегда true (БАГ-ИНВ-22)
       └─ Не зарегистрирован в ServiceLocator (БАГ-ИНВ-15)

  └─ EquipmentController: Dictionary<EquipmentSlot, EquipmentInstance>
       ├─ Equip(): oldItem потерян (БАГ-ИНВ-12)
       ├─ EquipTwoHand(): оба слота потеряны (БАГ-ИНВ-12)
       └─ Не зарегистрирован в ServiceLocator (БАГ-ИНВ-15)

События
  └─ InventoryController локальные: OnItemAdded/Removed/StackChanged ✅
       └─ BackpackPanel подписан ✅
       └─ GameEvents.TriggerItemAdded ← 0 вызовов, 0 подписчиков (БАГ-ИНВ-03)

  └─ GameEvents (5 событий): ВСЕ 5 — 0 вызовов, 0 подписчиков (БАГ-ИНВ-03)
  └─ OnInventoryFull: 0 подписчиков (БАГ-ИНВ-16)

═══════════════════ ПРОГОН 3: UI ОТОБРАЖЕНИЕ ═══════════════════

Сцена (Phase17/18)
  ├─ BackpackPanel: 8/8 SerializeField подключены ✅
  ├─ InventorySlotUI: 5/8 подключены (БАГ-ИНВ-05)
  │     └─ ❌ background, border, durabilityBar
  ├─ InventoryScreen: 12/13 подключены (БАГ-ИНВ-06)
  │     └─ ❌ spiritStoragePanel
  └─ Phase18: фактически no-op (логирует, не подключает)

Отображение
  └─ InventoryScreen.Initialize() → BackpackPanel.Initialize()
       └─ SubscribeToEvents() ✅
       └─ RefreshList() → CreateRowUI() для каждого слота
            └─ Instantiate(slotRowPrefab, listContainer)
            └─ ❌ rowGO.SetActive(true) ОТСУТСТВУЕТ (БАГ-ИНВ-01)
            └─ rowUI.SetSlot() → UpdateItemDisplay()
                 ├─ iconImage ✅, nameText ✅, countText ✅
                 ├─ border.color → NULL (БАГ-ИНВ-05)
                 ├─ background.color → NULL (БАГ-ИНВ-05)
                 └─ durabilityBar → NULL (БАГ-ИНВ-05)
```

---

## ПРИОРИТЕТ ИСПРАВЛЕНИЙ (итоговый)

| Приоритет | # | Баг | Объём |
|-----------|---|-----|-------|
| P0 🔴 | 1 | БАГ-ИНВ-01: SetActive(true) | 1 строка |
| P0 🔴 | 2 | БАГ-ИНВ-02: Боевой лут | ~30 строк |
| P0 🔴 | 3 | БАГ-ИНВ-12: Equip() теряет старый предмет | ~15 строк |
| P0 🔴 | 4 | БАГ-ИНВ-13: EquipmentData стакается | ~5 строк |
| P1 🟠 | 5 | БАГ-ИНВ-04: GetOrFind | 1 слово |
| P1 🟠 | 6 | БАГ-ИНВ-14: Предмет уничтожается без инвентаря | 1 строка |
| P1 🟠 | 7 | БАГ-ИНВ-05: SlotUI поля | ~20 строк |
| P1 🟠 | 8 | БАГ-ИНВ-06: spiritStoragePanel | ~5 строк |
| P1 🟠 | 9 | БАГ-ИНВ-15: ServiceLocator регистрация | ~4 строки |
| P1 🟠 | 10 | БАГ-ИНВ-03: GameEvents мост | ~6 строк |
| P2 🟡 | 11 | БАГ-ИНВ-16: OnInventoryFull UI | ~10 строк |
| P2 🟡 | 12 | БАГ-ИНВ-17: Единый ItemDatabase | ~40 строк |
| P2 🟡 | 13 | БАГ-ИНВ-18: ItemData при спавне | ~5 строк |
| P2 🟡 | 14 | БАГ-ИНВ-07: Hover vs Grade-тинт | ~5 строк |
| P2 🟡 | 15 | БАГ-ИНВ-19: AddItem partial | ~10 строк |
| P2 🟡 | 16 | БАГ-ИНВ-20: LoadSaveData лимиты | ~5 строк |
| P2 🟡 | 17 | БАГ-ИНВ-21: Texture2D утечка | ~10 строк |
| P3 🟢 | 18-27 | БАГ-ИНВ-22..27: низкие | различный |

---

## СВОДНАЯ СТАТИСТИКА

| Серьёзность | Количество |
|-------------|-----------|
| 🔴 КРИТИЧЕСКИЕ | 4 |
| 🟠 ВЫСОКИЕ | 6 |
| 🟡 СРЕДНИЕ | 7 |
| 🟢 НИЗКИЕ | 6 |
| **ИТОГО** | **23** |

---

## ЗАМЕТКИ
- БАГ-ИНВ-01 — **прямая причина** симптома «предметы не отображаются» (1 строка!)
- БАГ-ИНВ-12 — самый опасный баг для пользователя: потеря экипировки при замене
- БАГ-ИНВ-02 + БАГ-ИНВ-27 — боевой лут полностью неработоспособен
- БАГ-ИНВ-03 — блокирует квесты на сбор предметов
- БАГ-ИНВ-13 — может привести к потере grade/durability крафтового оружия
- БАГ-ИНВ-17 — создаёт несогласованные данные между путями подбора
- TooltipPanel полностью функциональна ✅ (24/24 SerializeField подключены)
- DragDropHandler подключён корректно ✅, но не работает из-за БАГ-ИНВ-01

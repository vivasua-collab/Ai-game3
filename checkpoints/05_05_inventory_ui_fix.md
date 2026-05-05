# Чекпоинт: Исправления инвентаря — UI + хранение + события
# Дата: 2026-05-05 16:07:41 MSK
# Зависит от: 05_05_inventory_audit.md
# Статус: pending

## Цель
Сделать инвентарь **видимым и функциональным**: отображение предметов, экипировка/снятие, события, подбор.

---

## 🔴 P0 — КРИТИЧЕСКИЕ (инвентарь не работает)

### ИСП-ИНВ-01: Активировать строки инвентаря после Instantiate
- **Баг:** БАГ-ИНВ-01
- **Файл:** `UI/Inventory/BackpackPanel.cs:152`
- **Проблема:** `Instantiate(slotRowPrefab, listContainer)` — клон неактивен.
- **Решение:** Добавить `rowGO.SetActive(true);` после Instantiate
- **Объём:** 1 строка

### ИСП-ИНВ-02: EquipmentData — запретить стакание
- **Баг:** БАГ-ИНВ-13
- **Файл:** `Data/ScriptableObjects/ItemData.cs:58`, `Inventory/InventoryController.cs:307-333`
- **Проблема:** stackable=true по умолчанию. EquipmentData наследует.
  Два меча разного грейда объединяются, grade/durability потерян.
- **Решение (вариант A):** В `EquipmentSOFactory.CreateRuntimeFromWeapon/Armor()`:
  уже стоит `stackable = false` ✅. Добавить принудительно в `EquipmentData`:
  ```csharp
  // В EquipmentData.cs:
  protected virtual void OnEnable() { stackable = false; }
  ```
- **Решение (вариант B):** В `InventoryController.AddItem()`:
  ```csharp
  if (itemData is EquipmentData) { /* всегда новый слот, без стекания */ }
  ```
- **Объём:** ~5 строк

### ИСП-ИНВ-03: ServiceLocator.Get → GetOrFind для ResourcePickup
- **Баг:** БАГ-ИНВ-04
- **Файл:** `Tile/ResourcePickup.cs:147`
- **Решение:** `ServiceLocator.GetOrFind<InventoryController>()`
- **Объём:** 1 слово

### ИСП-ИНВ-04: Не уничтожать предмет при отсутствии инвентаря
- **Баг:** БАГ-ИНВ-14
- **Файл:** `Tile/ResourcePickup.cs:199-201`
- **Решение:** `return false;` вместо `return true;` — предмет останется в мире
- **Объём:** 1 строка

---

## 🟠 P1 — ВЫСОКИЕ (существенные функции сломаны)

### ИСП-ИНВ-05: Построить мост InventoryController → GameEvents
- **Баг:** БАГ-ИНВ-03
- **Файл:** `Inventory/InventoryController.cs`, `Inventory/EquipmentController.cs`
- **Проблема:** 5 GameEvents (ItemAdded/Removed/Equipped/Unequipped/Crafted) — 0 вызовов.
  Квесты, достижения — полностью изолированы.
- **Решение:**
  ```csharp
  // В InventoryController.AddItem(), после OnItemAdded:
  GameEvents.TriggerItemAdded(itemData.itemId, addedCount);
  
  // В RemoveFromSlot(), после OnItemRemoved:
  GameEvents.TriggerItemRemoved(slot.ItemData?.itemId ?? "", count);
  
  // В EquipmentController.Equip(), после OnEquipmentEquipped:
  GameEvents.TriggerItemEquipped(instance.equipmentData.itemId, slot.ToString());
  
  // В Unequip():
  GameEvents.TriggerItemUnequipped(instance.equipmentData.itemId, slot.ToString());
  ```
- **Объём:** ~6 строк

### ИСП-ИНВ-06: Подключить недостающие поля InventorySlotUI
- **Баг:** БАГ-ИНВ-05
- **Файл:** `Editor/SceneBuilder/Phase17InventoryUI.cs:411-420`
- **Проблема:** background, border, durabilityBar не подключены.
- **Решение:**
  1. Подключить `background` к root GO Image (уже создана)
  2. Создать child GO `Border` с Image → подключить к `border`
  3. Создать child GO `DurabilityBar` с Image → подключить к `durabilityBar`
- **Объём:** ~20 строк в Phase17

### ИСП-ИНВ-07: Подключить spiritStoragePanel к InventoryScreen
- **Баг:** БАГ-ИНВ-06
- **Файл:** `Editor/SceneBuilder/Phase17InventoryUI.cs:906,133-176`
- **Решение:** Изменить CreateSpiritStoragePanel() чтобы возвращать GO,
  добавить назначение в WireInventoryScreenReferences.
- **Объём:** ~5 строк

### ИСП-ИНВ-08: Зарегистрировать контроллеры в ServiceLocator
- **Баг:** БАГ-ИНВ-15
- **Файл:** `Inventory/InventoryController.cs`, `Inventory/EquipmentController.cs`
- **Решение:** Добавить в Awake():
  ```csharp
  ServiceLocator.Register(this);
  ```
  Или использовать `RegisteredBehaviour<T>`.
- **Объём:** ~4 строки

### ИСП-ИНВ-09: NullRef guard при null ItemData в слоте
- **Баг:** БАГ-ИНВ-47
- **Файл:** `Inventory/InventoryController.cs:392,617,628,638,666`
- **Решение:** Добавить null-проверку:
  ```csharp
  if (slot.ItemData == null) continue;
  ```
  в RemoveItemById, FindSlotWithItem, CountItem, SortInventory.
- **Объём:** ~5 строк

### ИСП-ИНВ-10: Предотвратить потерю предметов при загрузке (отсутствующий SO)
- **Баг:** БАГ-ИНВ-51, БАГ-ИНВ-52
- **Файл:** `Inventory/InventoryController.cs:754`, `Inventory/EquipmentController.cs:944`
- **Решение:** При TryGetValue=false — создать fallback ItemData, залогировать WARNING.
- **Объём:** ~8 строк

---

## 🟡 P2 — СРЕДНИЕ (улучшают качество и стабильность)

### ИСП-ИНВ-11: Кэшировать Resources.LoadAll в ResourcePickup
- **Баг:** БАГ-ИНВ-49
- **Файл:** `Tile/ResourcePickup.cs:213`
- **Решение:** Static Dictionary cache, заполняется один раз.
  (Или использовать ItemDatabase из ИСП-БЛ-06)
- **Объём:** ~10 строк

### ИСП-ИНВ-12: Исправить Hover vs Grade-тинт
- **Баг:** БАГ-ИНВ-07
- **Файл:** `UI/Inventory/InventorySlotUI.cs:230-236`
- **Решение:** Сохранять currentBgColor, восстанавливать при un-hover:
  ```csharp
  private Color savedBgColor;
  public void SetHoverHighlight(bool hovered) {
      if (background == null) return;
      if (hovered) savedBgColor = background.color;
      background.color = hovered ? hoverColor : savedBgColor;
  }
  ```
- **Объём:** ~5 строк

### ИСП-ИНВ-13: UnequipToInventory — не перезаписывать durability при merge
- **Баг:** БАГ-ИНВ-55
- **Файл:** `Inventory/InventoryController.cs:586-592`
- **Решение:** Если AddItem вернул существующий стек — НЕ перезаписывать durability.
  Проверить `returnedSlot.Count > 1` (был merge) — не трогать.
- **Объём:** ~5 строк

### ИСП-ИНВ-14: OnInventoryFull → UI уведомление
- **Баг:** БАГ-ИНВ-16
- **Решение:** Создать простой ToastController, подписать на OnInventoryFull.
- **Объём:** ~15 строк

### ИСП-ИНВ-15: Пустой itemId → некорректное стекание
- **Баг:** БАГ-ИНВ-48
- **Файл:** `Inventory/InventoryController.cs:312`
- **Решение:** Валидация itemId при AddItem:
  ```csharp
  if (string.IsNullOrEmpty(itemData.itemId)) return null;
  ```
- **Объём:** 2 строки

### ИСП-ИНВ-16: HasFreeSpace() → реальная проверка
- **Баг:** БАГ-ИНВ-22
- **Файл:** `Inventory/InventoryController.cs:283-288`
- **Решение:** Делегировать CanFitItem.
- **Объём:** 3 строки

### ИСП-ИНВ-17: Единый маппинг resourceId → ItemData
- **Баг:** БАГ-ИНВ-17
- **Файлы:** `Player/PlayerController.cs:855-922`, `Tile/ResourcePickup.cs:228-260`
- **Решение:** Перенести таблицу в ItemDatabase (ИСП-БЛ-06).
  Удалить дубликаты из PlayerController и ResourcePickup.
- **Объём:** ~30 строк (рефакторинг)

### ИСП-ИНВ-18: LoadSaveData — временно отключить лимиты при загрузке
- **Баг:** БАГ-ИНВ-20
- **Файл:** `Inventory/InventoryController.cs:746-770`
- **Решение:** Флаг `isLoading = true`, CanFitItem возвращает true при isLoading.
- **Объём:** ~5 строк

### ИСП-ИНВ-19: DontDestroyOnLoad для Player/Inventory
- **Баг:** БАГ-ИНВ-50
- **Файл:** `Player/PlayerController.cs`
- **Решение:** `DontDestroyOnLoad(gameObject);` в Awake() (если сцена одна — не критично).
- **Объём:** 1 строка

---

## ПОРЯДОК ИСПРАВЛЕНИЙ

```
Шаг 1: ИСП-ИНВ-01 (SetActive)           ← инвентарь становится видимым
Шаг 2: ИСП-ИНВ-02 (EquipmentData стак)   ← экипировка не объединяется
Шаг 3: ИСП-ИНВ-03 (GetOrFind)            ← подбор работает стабильно
Шаг 4: ИСП-ИНВ-04 (не уничтожать)        ← предметы не теряются
Шаг 5: ИСП-ИНВ-05 (GameEvents мост)      ← квесты/достижения подключаются
Шаг 6: ИСП-ИНВ-09 (NullRef guard)        ← нет крашей на повреждённых данных
```

После шагов 1-4 инвентарь **базово функционален**.
Шаги 5-6 — подключают экосистему вокруг инвентаря.

---

## ССЫЛКИ НА СМЕЖНЫЕ ЧЕКПОИНТЫ
- `05_05_combat_loot_fix.md` — бой + лут + техники
- `05_05_inventory_audit.md` — полный аудит (61 баг)

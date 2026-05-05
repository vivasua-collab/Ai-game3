# Чекпоинт: Аудит инвентаря — генерация → подбор → хранение → отображение
# Дата: 2026-05-05 14:54:45 MSK
# Статус: in_progress

## Симптом
Предметы генерируются, подбираются, но **не отображаются** в инвентаре.

---

## 🔴 КРИТИЧЕСКИЕ БАГИ

### БАГ-ИНВ-01: Строки инвентаря неактивны (SetActive(true) не вызывается)
- **Файл:** `UI/Inventory/BackpackPanel.cs:152`
- **Причина:** `Phase17InventoryUI.cs:423` создаёт префаб строки `slotGO.SetActive(false)`.
  При `Instantiate()` клон наследует inactive-состояние, но `rowGO.SetActive(true)` **нигде не вызывается**.
- **Следствие:** Все строки рюкзака существуют в иерархии, но невидимы.
- **Исправление:** Добавить `rowGO.SetActive(true)` после `Instantiate()` в `CreateRowUI()`
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-02: Боевой лут не поступает в инвентарь игрока
- **Файл:** `Combat/CombatManager.cs:440`
- **Причина:** `OnLootGenerated` вызывается, но **нет ни одного подписчика**.
  `LootResult` (предметы + Qi + XP) генерируется и логируется, но никогда не передаётся
  в `InventoryController` или `QiController` игрока.
- **Следствие:** Весь лут из боя потерян.
- **Исправление:** Создать подписчика (LootCollector на Player GO), который переводит LootResult в InventoryController.AddItem()
- **Статус:** ⏳ не исправлено

---

## 🟡 СРЕДНИЕ БАГИ

### БАГ-ИНВ-03: GameEvents.OnItemAdded никогда не вызывается
- **Файл:** `Inventory/InventoryController.cs:298-364` (AddItem) vs `Core/GameEvents.cs:257` (событие)
- **Причина:** `InventoryController.AddItem()` вызывает свои локальные события (`OnItemAdded` и т.д.),
  но **никогда не вызывает** `GameEvents.TriggerItemAdded()`.
- **Следствие:** Любая глобальная система (квесты, достижения), подписанная на GameEvents,
  не узнаёт об изменении инвентаря.
- **Исправление:** Добавить `GameEvents.TriggerItemAdded()` в AddItem(), `GameEvents.TriggerItemRemoved()` в RemoveFromSlot()
- **Статус:** ⏳ не исправлено

### БАГ-ИНВ-04: ResourcePickup — ServiceLocator.Get вместо GetOrFind
- **Файл:** `Tile/ResourcePickup.cs:147`
- **Причина:** Fallback использует `ServiceLocator.Get<InventoryController>()`,
  а не `GetOrFind`. Если InventoryController не зарегистрирован — вернёт null,
  хотя `GetOrFind` нашёл бы через `FindFirstObjectByType`.
- **Следствие:** Потенциальный тихий сбой при подборе предмета.
- **Исправление:** Заменить `Get` → `GetOrFind`
- **Статус:** ⏳ не исправлено

---

## 🟡 НИЗКИЕ / КОСМЕТИЧЕСКИЕ

### БАГ-ИНВ-05: Не подключены поля InventorySlotUI в Phase17
- **Файл:** `Editor/SceneBuilder/Phase17InventoryUI.cs:411-420`
- **Причина:** Из 8 `[SerializeField]` полей подключены только 5:
  - ✅ iconImage, nameText, countText, weightText, volumeText
  - ❌ background (Image на root GO) — не подключена
  - ❌ border (не создана)
  - ❌ durabilityBar (не создана)
- **Следствие:** Нет подсветки грейда, нет рамки редкости, нет полоски прочности.
  Hover-подсветка не работает (background = null).
- **Исправление:** Подключить background, создать и подключить border и durabilityBar
- **Статус:** ⏳ не исправлено

---

## 📋 ПОЛНАЯ ЦЕПОЧКА ДАННЫХ

```
Генерация предмета
  ├─ ResourceSpawner.SpawnSingleResource()     → ResourcePickup GO на карте
  ├─ EquipmentRuntimeSpawner.SpawnEquipmentPickup() → ResourcePickup GO
  ├─ EquipmentRuntimeSpawner.AddToPlayerInventory() → InventoryController.AddItem() ✅
  └─ DeathLootGenerator.GenerateLoot()         → OnLootGenerated event → ❌ НЕТ ПОДПИСЧИКА

Подбор предмета
  ├─ ResourcePickup.OnTriggerEnter2D → Pickup() → TryAddToInventory()
  │     └─ picker.GetComponent<InventoryController>()
  │        └─ fallback: ServiceLocator.Get() (⚠️ БАГ-ИНВ-04)
  ├─ PlayerController.HarvestHit() → AddResourceToInventory() ✅
  └─ InventoryController.AddItem() → OnItemAdded event ✅ (но GameEvents нет — БАГ-ИНВ-03)

Хранение
  └─ InventoryController: List<InventorySlot>
       └─ InventorySlot: ItemData, Count, durability, grade

Отображение
  └─ InventoryScreen.Initialize() → BackpackPanel.Initialize()
       └─ BackpackPanel подписывается на InventoryController.OnItemAdded
            └─ CreateRowUI() → Instantiate(slotRowPrefab, listContainer)
                 └─ ❌ rowGO.SetActive(true) ОТСУТСТВУЕТ (БАГ-ИНВ-01)
```

---

## ПРИОРИТЕТ ИСПРАВЛЕНИЙ

| # | Баг | Приоритет | Объём |
|---|-----|-----------|-------|
| 1 | БАГ-ИНВ-01: SetActive(true) | 🔴 КРИТ | 1 строка |
| 2 | БАГ-ИНВ-02: Боевой лут | 🔴 КРИТ | ~30 строк |
| 3 | БАГ-ИНВ-04: GetOrFind | 🟡 СРЕДН | 1 слово |
| 4 | БАГ-ИНВ-03: GameEvents мост | 🟡 СРЕДН | 2 строки |
| 5 | БАГ-ИНВ-05: SlotUI поля | 🟢 НИЗК | ~20 строк |

---

## ЗАМЕТКИ
- БАГ-ИНВ-01 — **прямая причина** симптома «предметы не отображаются»
- BackpackPanel.RefreshList() корректно перестраивает список при открытии,
  но все клоны неактивны — пользователь видит пустой список
- Событие OnInventoryRebuilt (вызываемое при RefreshList) также вызывает CreateRowUI
  для каждого слота, что не решает проблему — все клоны снова неактивны

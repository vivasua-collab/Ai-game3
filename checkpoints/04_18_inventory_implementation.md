# Чекпоинт: Поэтапное внедрение инвентаря

**Дата:** 2026-04-18 18:06:01 UTC
**Статус:** 📋 in_progress
**Цель:** Базовая кукла + рюкзак + интерфейс пользователя

---

## Аудит существующего кода

### InventoryController.cs (~750 строк)

**КРИТИЧЕСКИЕ БАГИ:**

| ID | Серьёзность | Описание | Строка |
|----|------------|----------|--------|
| INV-BUG-01 | 🔴 CRITICAL | `AddItem()` рекурсивный вызов при переполнении стака — если count > maxStack и нет свободного места, вес первого добавления уже учтён, но рекурсия возвращает null и вес не откатывается. Итог: weight рассинхронизируется | 175-179 |
| INV-BUG-02 | 🔴 CRITICAL | `RemoveFromSlot()` — `UpdateWeight(-slot.ItemData.weight * count)` вызывается ПОСЛЕ `slot.RemoveCount(count)`. Если RemoveCount изменяет Count, вес может считаться неправильно | 316-317 |
| INV-BUG-03 | 🟡 HIGH | `Resize()` — при уменьшении не проверяется, есть ли предметы в удаляемых ячейках. Проверяется только позиция слота, но не заполненность ячеек | 106-138 |
| INV-BUG-04 | 🟡 HIGH | `FreeGrid()` — если два предмета пересекаются (что не должно быть, но возможно при баге), очистка одной области может освободить ячейки другого предмета | 527-536 |
| INV-BUG-05 | 🟠 MEDIUM | `InventorySlot.HasDurability` = `durability > 0`, но `durability = -1` означает «нет системы прочности» → HasDurability вернёт false, ок. Но `durability = 0` = сломан → HasDurability вернёт false, хотя предмет имеет систему прочности и сломан | 652 |
| INV-BUG-06 | 🟠 MEDIUM | `LoadSaveData()` — `slotData.durability > 0` — предметы с durability=0 (сломанные) не восстановят прочность при загрузке | 616 |
| INV-BUG-07 | 🟢 LOW | `FreeSlots` = `TotalSlots - UsedSlots` — не учитывает многогабаритные предметы. Предмет 2×2 занимает 4 ячейки, но UsedSlots считает как 1 | 73 |

**АРХИТЕКТУРНЫЕ ПРОБЛЕМЫ:**

| ID | Описание |
|----|----------|
| INV-ARCH-01 | Фиксированный размер сетки (8×6) — не соответствует системе рюкзака. Нужно: динамический размер от BackpackData |
| INV-ARCH-02 | Нет системы рюкзака — gridWidth/gridHeight захардкожены |
| INV-ARCH-03 | Нет поля `volume` у предметов (нужно для колец хранения) |
| INV-ARCH-04 | Нет поля `allowNesting` у предметов (флаг вложения) |
| INV-ARCH-05 | `owner` (MonoBehaviour) — не используется нигде в коде |

---

### EquipmentController.cs (~750 строк)

**КРИТИЧЕСКИЕ БАГИ:**

| ID | Серьёзность | Описание | Строка |
|----|------------|----------|--------|
| EQP-BUG-01 | 🔴 CRITICAL | `EquipmentSlot` enum не соответствует дизайну! В enum: `None, WeaponMain, WeaponOff, Armor, Clothing, Charger, RingLeft, RingRight, Accessory, Backpack`. В дизайне: `head, torso, belt, legs, feet, weapon_main, weapon_off`. НЕТ слотов для head, torso, belt, legs, feet! | Enums.cs |
| EQP-BUG-02 | 🟡 HIGH | `AddEquipmentStats()` — switch по `bonus.statName.ToLower()` для STR/AGI/CON/INT — BUT `bonus.bonus` добавляется напрямую, игнорируя `gradeMultiplier` для не-percentage бонусов (кроме default case). Строки 395-415: `cachedStats.strength += bonus.bonus` — а должно быть `+= bonus.bonus * gradeMultiplier` для абсолютных бонусов | 395-415 |
| EQP-BUG-03 | 🟠 MEDIUM | `SwapSlots()` меняет списки, но не обновляет `currentLayer` у экземпляров | 228-247 |
| EQP-BUG-04 | 🟠 MEDIUM | Нет логики двуручного оружия — нет проверки/блокировки weapon_off при equip двуручного | — |
| EQP-BUG-05 | 🟠 MEDIUM | `GetGradeMultiplier()` — значения не совпадают с EQUIPMENT_SYSTEM.md. Документ: Refined ×1.3-1.5, Perfect ×1.7-2.5, Transcendent ×2.5-4.0. Код: Refined 1.5, Perfect 2.5, Transcendent 4.0 | 424-435 |

**АРХИТЕКТУРНЫЕ ПРОБЛЕМЫ:**

| ID | Описание |
|----|----------|
| EQP-ARCH-01 | EquipmentSlot enum нужен полный рефакторинг под новую куклу (7 видимых слотов) |
| EQP-ARCH-02 | Нет связи Inventory ↔ Equipment — нет метода «экипировать из инвентаря» или «снять в инвентарь» |
| EQP-ARCH-03 | `EquipmentSlotsUI` — UI-класс внутри бэкенд-файла — нарушение разделения |

---

### MaterialSystem.cs (~548 строк)

**Оценка:** Наиболее стабильный файл. Багов не обнаружено.

**Архитектурные заметки:**
- Не нужен для начального этапа (кукла + рюкзак + UI)
- Оставить как есть

---

### CraftingController.cs (~693 строк)

**БАГИ:**

| ID | Серьёзность | Описание | Строка |
|----|------------|----------|--------|
| CRA-BUG-01 | 🟡 HIGH | `Craft()` — при неудачном крафте (result.success = false) материалы уже потрачены (строка 171), но предмет не создан. Это CORRECT поведение для крафта, но нет события/уведомления о потере материалов | 169-220 |
| CRA-BUG-02 | 🟠 MEDIUM | `CraftCustom()` — `AddItem(baseItem, 1)` — baseItem это EquipmentData, но AddItem ожидает ItemData. EquipmentData наследует ItemData, так что работает, но grade применяется к InventorySlot, а EquipmentController.Equip() не знает об этом grade | 275-279 |
| CRA-BUG-03 | 🟠 MEDIUM | `GetAvailableRecipes()` — O(N) линейный поиск по всем рецептам с CanCraft() — каждый CanCraft() тоже O(N). При большом количестве рецептов — тормозит | 506-519 |

**Архитектурные заметки:**
- Не нужен для начального этапа
- Оставить как есть

---

### ItemData.cs / EquipmentData.cs

**ПРОБЛЕМЫ:**

| ID | Серьёзность | Описание |
|----|------------|----------|
| DATA-01 | 🔴 CRITICAL | Нет поля `volume` — необходимо для системы колец хранения |
| DATA-02 | 🟡 HIGH | Нет поля `allowNesting` — флаг вложения (none/spirit/ring/any) |
| DATA-03 | 🟡 HIGH | `sizeHeight` Range(1,3) — в дизайне макс 2, не 3 |
| DATA-04 | 🟠 MEDIUM | Нет `BackpackData` ScriptableObject — нужно для системы рюкзака |

---

## План внедрения

### Этап 1: Рефакторинг данных (CRITICAL)

**Цель:** Подготовить модели данных под новую архитектуру

**Задачи:**
- [ ] 1.1 Обновить `EquipmentSlot` enum: добавить `Head, Torso, Belt, Legs, Feet` + скрытые заглушки
- [ ] 1.2 Создать `BackpackData` ScriptableObject (gridWidth, gridHeight, weightReduction, maxWeightBonus, beltSlots)
- [ ] 1.3 Добавить `volume` (float) в ItemData
- [ ] 1.4 Добавить `allowNesting` (enum NestingPermission: None, Spirit, Ring, Any) в ItemData
- [ ] 1.5 Исправить `sizeHeight` Range(1,3) → Range(1,2)
- [ ] 1.6 Исправить INV-BUG-01 (рекурсивный AddItem — рассинхрон веса)

**Файлы:** Enums.cs, ItemData.cs, новый BackpackData.cs

---

### Этап 2: Рефакторинг InventoryController

**Цель:** Поддержка динамического размера рюкзака

**Задачи:**
- [ ] 2.1 Добавить `BackpackData currentBackpack` — текущий рюкзак
- [ ] 2.2 `SetBackpack(BackpackData)` — сменить рюкзак (с проверкой вмещаемости)
- [ ] 2.3 Пересчёт `effectiveWeight = totalWeight × (1 − backpack.weightReduction)`
- [ ] 2.4 Стартовый рюкзак: 3×4, weightReduction=0, maxWeightBonus=0
- [ ] 2.5 Исправить INV-BUG-02 (RemoveFromSlot порядок weight update)
- [ ] 2.6 Исправить INV-BUG-05 (HasDurability при durability=0)
- [ ] 2.7 Исправить INV-BUG-06 (LoadSaveData durability=0)
- [ ] 2.8 Удалить неиспользуемое поле `owner`

**Файлы:** InventoryController.cs

---

### Этап 3: Рефакторинг EquipmentController

**Цель:** Упрощение под новую куклу (7 видимых слотов)

**Задачи:**
- [ ] 3.1 Обновить под новый EquipmentSlot enum
- [ ] 3.2 Реализовать логику двуручного оружия (блокировка weapon_off)
- [ ] 3.3 Добавить `EquipFromInventory(InventorySlot, EquipmentSlot)` — экипировать из инвентаря
- [ ] 3.4 Добавить `UnequipToInventory(EquipmentSlot)` — снять в инвентарь
- [ ] 3.5 Исправить EQP-BUG-02 (gradeMultiplier для stat бонусов)
- [ ] 3.6 Исправить EQP-BUG-03 (SwapSlots — обновить currentLayer)
- [ ] 3.7 Вынести EquipmentSlotsUI в отдельный UI-файл

**Файлы:** EquipmentController.cs, новый EquipmentSlotsUI.cs

---

### Этап 4: UI — Базовая панель инвентаря

**Цель:** Открытие/закрытие + сетка рюкзака + кукла

**Задачи:**
- [ ] 4.1 `InventoryCanvas` — Canvas, Screen Space Overlay
- [ ] 4.2 `InventoryPanel` — основная панель (фон, заголовок, закрытие)
- [ ] 4.3 `BackpackPanel` — динамическая сетка от рюкзака (3×4)
- [ ] 4.4 `BodyDollPanel` — 7 видимых слотов (head, torso, belt, legs, feet, weapon_main, weapon_off)
- [ ] 4.5 Открытие/закрытие по клавише I (через PlayerController)
- [ ] 4.6 Состояние `GameState.Inventory` — пауза + разблокировка курсора
- [ ] 4.7 `InfoPanel` — вес, кол-во предметов, название рюкзака

**Файлы:** новые в Scripts/UI/Inventory/

---

### Этап 5: UI — Отображение предметов

**Цель:** Визуальные слоты с иконками и tooltip-ами

**Задачи:**
- [ ] 5.1 `InventorySlotUI` — визуальный слот (иконка, счётчик, рамка по редкости)
- [ ] 5.2 Заполнение слотов из InventoryController.Slots
- [ ] 5.3 Заполнение куклы из EquipmentController
- [ ] 5.4 Рамки по редкости (Common=серый, Uncommon=зелёный, ...)
- [ ] 5.5 `TooltipPanel` — карточка предмета при наведении
- [ ] 5.6 Процедурные иконки (цветной квадрат + первая буква) если нет спрайта
- [ ] 5.7 Обновление через события (OnItemAdded, OnItemRemoved, OnItemStackChanged)

**Файлы:** новые в Scripts/UI/Inventory/

---

### Этап 6: UI — Drag & Drop

**Цель:** Перетаскивание предметов

**Задачи:**
- [ ] 6.1 `DragItem` — визуальный элемент при перетаскивании
- [ ] 6.2 Drag & Drop между слотами рюкзака
- [ ] 6.3 Drag & Drop с рюкзака на куклу (экипировать)
- [ ] 6.4 Drag & Drop с куклы в рюкзак (снять)
- [ ] 6.5 Swap при помещении на занятый слот
- [ ] 6.6 Подсветка валидных/невалидных позиций
- [ ] 6.7 Двуручное оружие: визуальная блокировка off-слота
- [ ] 6.8 Выброс предмета (drag мимо панели → дроп на землю)

**Файлы:** новые в Scripts/UI/Inventory/

---

## Отложенные этапы (не в текущем чекпоинте)

- Пояс быстрого доступа (после этапа 6)
- Духовное хранилище + каталогизатор (после пояса)
- Кольца хранения (после дух. хранилища)
- Контекстное меню (правый клик)
- Разделение стека
- Анимации
- Крафт UI

---

## Изменённые файлы

| Файл | Действие |
|------|----------|
| `Scripts/Core/Enums.cs` | Обновить EquipmentSlot |
| `Scripts/Data/ScriptableObjects/ItemData.cs` | Добавить volume, allowNesting, исправить sizeHeight |
| `Scripts/Data/ScriptableObjects/BackpackData.cs` | НОВЫЙ — ScriptableObject рюкзака |
| `Scripts/Inventory/InventoryController.cs` | Рюкзак + багфиксы |
| `Scripts/Inventory/EquipmentController.cs` | Новые слоты + двуручное + багфиксы |
| `Scripts/UI/Inventory/InventoryCanvas.cs` | НОВЫЙ — Canvas инвентаря |
| `Scripts/UI/Inventory/InventoryPanel.cs` | НОВЫЙ — основная панель |
| `Scripts/UI/Inventory/BackpackPanel.cs` | НОВЫЙ — сетка рюкзака |
| `Scripts/UI/Inventory/BodyDollPanel.cs` | НОВЫЙ — кукла тела |
| `Scripts/UI/Inventory/InventorySlotUI.cs` | НОВЫЙ — визуальный слот |
| `Scripts/UI/Inventory/TooltipPanel.cs` | НОВЫЙ — карточка предмета |
| `Scripts/UI/Inventory/DragItem.cs` | НОВЫЙ — перетаскивание |

---

## Вариант B: Полная переделка кода инвентаря

**Редактировано:** 2026-04-18 18:09:12 UTC

### Обоснование

Текущий код имеет **фундаментальные архитектурные проблемы**, а не просто баги:

1. **EquipmentSlot enum полностью не соответствует дизайну** — нет Head, Torso, Belt, Legs, Feet. Вместо них абстрактные Armor/Clothing, которые маппят всё подряд. AssetGenerator хачит head→Armor, torso→Clothing, hands→WeaponOff (sic!)
2. **Inventory ↔ Equipment разорваны** — нет моста «экипировать из инвентаря» / «снять в инвентарь»
3. **InventoryController** — 2 критических бага с весом, рекурсивный AddItem, хардкод 8×6
4. **UI-классы внутри бэкенда** — EquipmentSlotsUI в EquipmentController.cs, EquipmentSlotUI в CharacterPanelUI.cs — всё это придётся переписать

Патчить поверх — значит тянуть хаки дальше. Полная переделка даёт чистую архитектуру с нуля.

### Карта зависимостей (что сломается)

При переделке InventoryController + EquipmentController сломаются:

| Файл | Что использует | Что менять |
|------|---------------|------------|
| PlayerController.cs | `InventoryController.AddItem()` для ресурсов | Обновить вызовы — API не поменяется |
| ResourcePickup.cs | `InventoryController.AddItem()`, `GetComponent<InventoryController>()` | Обновить вызовы |
| QuestController.cs | `InventoryController.AddItem()` для наград | Обновить вызовы |
| Combatant.cs | `EquipmentController` (TODO, не реализовано) | Без изменений |
| InventoryUI.cs | `InventoryController`, `EquipmentController`, события | ПЕРЕПИСАТЬ — будет заменён новым UI |
| CharacterPanelUI.cs | `EquipmentController`, `EquipmentSlot` enum, `EquipmentSlotUI` | ПЕРЕПИСАТЬ — будет заменён новым UI |
| CraftingController.cs | `InventoryController.AddItem/RemoveItemById/HasItem/CountItem` | Обновить вызовы — API не поменяется |
| AssetGeneratorExtended.cs | `ParseEquipmentSlot()` — маппит head→Armor | Обновить маппинг под новый enum |
| NPCPresetData.cs | `EquipmentSlot` | Обновить под новый enum |
| Phase06Player.cs | Добавляет `InventoryController`, `EquipmentController` на Player | Без изменений (компоненты те же) |
| TestLocationGameController.cs | `player.AddComponent<InventoryController>()`, `EquipmentController` | Без изменений |
| SaveDataTypes.cs | `CustomBonusEntry`, `CraftingSkillEntry` | Без изменений |

**Итого:** 4 файла нужно обновить (PlayerController, ResourcePickup, QuestController, AssetGeneratorExtended), 2 файла переписать полностью (InventoryUI, CharacterPanelUI), остальные без изменений.

### Новая архитектура

```
Assets/Scripts/Inventory/              ← БЭКЕНД
├── InventoryController.cs             ← ПЕРЕПИСАТЬ — рюкзак + Diablo-сетка
├── EquipmentController.cs             ← ПЕРЕПИСАТЬ — 7 видимых + скрытые слоты
├── CraftingController.cs              ← БЕЗ ИЗМЕНЕНИЙ
├── MaterialSystem.cs                  ← БЕЗ ИЗМЕНЕНИЙ
├── BackpackData.cs                    ← НОВЫЙ — ScriptableObject рюкзака
└── ItemInstance.cs                    ← НОВЫЙ — экземпляр предмета (вынести из InventorySlot)

Assets/Scripts/UI/Inventory/           ← UI (НОВЫЙ)
├── InventoryScreen.cs                 ← Главный экран (Canvas + панель)
├── BackpackGrid.cs                    ← Сетка рюкзака (динамическая)
├── BodyDollPanel.cs                   ← Кукла тела (7 слотов)
├── ItemSlotUI.cs                      ← Визуальный слот (иконка, рамка, счётчик)
├── TooltipPanel.cs                    ← Карточка предмета
├── DragHandler.cs                     ← Drag & Drop логика
└── ItemIconGenerator.cs              ← Процедурные иконки

Assets/Scripts/Core/Enums.cs           ← ОБНОВИТЬ EquipmentSlot
Assets/Scripts/Data/ScriptableObjects/
├── ItemData.cs                        ← ОБНОВИТЬ: +volume, +allowNesting, fix sizeHeight
└── EquipmentData.cs                   ← БЕЗ ИЗМЕНЕНИЙ
```

### Ключевые изменения

#### 1. EquipmentSlot enum (НОВЫЙ)

```csharp
public enum EquipmentSlot
{
    None,
    // Видимые слоты (7)
    Head,           // Голова
    Torso,          // Торс
    Belt,           // Пояс
    Legs,           // Ноги
    Feet,           // Ступни
    WeaponMain,     // Правая рука
    WeaponOff,      // Левая рука
    // Скрытые заглушки (не отображаются в UI)
    Hands,          // Перчатки (будущее)
    Back,           // Плащ/спина (будущее)
    Amulet,         // Амулет (будущее)
    RingLeft1,      // Кольцо левое 1 (будущее)
    RingLeft2,      // Кольцо левое 2 (будущее)
    RingRight1,     // Кольцо правое 1 (будущее)
    RingRight2,     // Кольцо правое 2 (будущее)
    Charger,        // Зарядник (будущее)
    Backpack        // Рюкзак (будущее)
}
```

**Важно:** Старые значения `Armor`, `Clothing`, `Accessory`, `RingLeft`, `RingRight` — УДАЛЕНЫ. Это breaking change для AssetGenerator и NPCPresetData.

#### 2. InventoryController (ПЕРЕПИСАТЬ)

**Что сохранить:**
- Diablo-сетка (gridOccupancy, FindFreePosition, IsAreaFree, OccupyGrid, FreeGrid)
- Drag & Drop (MoveItem, SwapSlots)
- Save/Load (GetSaveData, LoadSaveData — формат обновить)
- События (OnItemAdded, OnItemRemoved, OnItemStackChanged, OnWeightChanged, OnInventoryFull)

**Что переделать:**
- Рюкзак вместо фиксированного 8×6: `BackpackData currentBackpack`, динамический Resize
- `effectiveWeight = totalWeight × (1 − backpack.weightReduction)`
- Убрать рекурсивный AddItem → итеративный с очередью
- Вес считать ДО изменения, а не после (фикс INV-BUG-01, INV-BUG-02)
- `FreeSlots` → считать по ячейкам, а не по слотам (фикс INV-BUG-07)
- `HasDurability` → `durability >= 0` вместо `> 0` (фикс INV-BUG-05)
- `LoadSaveData` → убрать `durability > 0` проверку (фикс INV-BUG-06)
- Удалить `owner` (INV-ARCH-05)
- Добавить `EquipFromSlot(int slotId, EquipmentSlot targetSlot)` — мост к EquipmentController
- Добавить `OnItemDropped` событие (для выброса на землю)

#### 3. EquipmentController (ПЕРЕПИСАТЬ)

**Что сохранить:**
- Equip/Unequip логика
- Stats пересчёт (EquipmentStats, RecalculateStats)
- Слои (useLayerSystem, maxLayersPerSlot)
- Durability (DamageEquipment, RepairEquipment, RepairAll)
- Save/Load (GetSaveData, LoadSaveData — формат обновить)
- События (OnEquipmentEquipped, OnEquipmentUnequipped, OnStatsChanged)

**Что переделать:**
- Новый EquipmentSlot enum (7 видимых + заглушки)
- Логика двуручного оружия: при Equip с `itemType == "twohanded"` → блокировать WeaponOff
- При Unequip двуручного → освободить оба слота
- GradeMultiplier для stat бонусов (фикс EQP-BUG-02)
- SwapSlots — обновить currentLayer (фикс EQP-BUG-03)
- GradeMultiplier значения по EQUIPMENT_SYSTEM.md (фикс EQP-BUG-05)
- `UnequipToInventory(EquipmentSlot)` — вернуть ItemInstance в инвентарь
- Вынести EquipmentSlotsUI / EquipmentSlotUI в UI-файлы
- `CanEquip()` — проверка двуручности + свободен ли off-слот

#### 4. BackpackData (НОВЫЙ ScriptableObject)

```
BackpackData : ScriptableObject
├── itemId: string           // ID для ссылки
├── backpackName: string     // Название ("Тканевая сумка")
├── gridWidth: int           // 3 (стартовый)
├── gridHeight: int          // 4 (стартовый)
├── weightReduction: float   // 0% (стартовый)
├── maxWeightBonus: float    // +0 кг (стартовый)
├── beltSlots: int           // 0 (стартовый)
└── ownWeight: float         // Вес самого рюкзака
```

#### 5. ItemData (ОБНОВИТЬ)

Добавить:
- `volume: float = 1.0f` — объём для колец хранения
- `allowNesting: NestingPermission = NestingPermission.Any` — флаг вложения
- Исправить `sizeHeight` Range(1,3) → Range(1,2)

#### 6. NestingPermission enum (НОВЫЙ)

```csharp
public enum NestingPermission
{
    None,       // Нельзя ни в какое хранилище
    Spirit,     // Только духовное хранилище
    Ring,       // Только кольцо хранения
    Any         // В любое
}
```

### План переделки (3 этапа вместо 6)

#### Этап A: Данные + Бэкенд (заменяет этапы 1-3)

**Задачи:**
- [ ] A.1 Обновить `EquipmentSlot` enum (удалить Armor/Clothing/Accessory/RingLeft/RingRight, добавить Head/Torso/Belt/Legs/Feet/Hands/Back + заглушки)
- [ ] A.2 Создать `NestingPermission` enum в Enums.cs
- [ ] A.3 Обновить `ItemData.cs`: +volume, +allowNesting, fix sizeHeight Range
- [ ] A.4 Создать `BackpackData.cs` ScriptableObject
- [ ] A.5 Переписать `InventoryController.cs` с нуля (рюкзак, итеративный AddItem, багфиксы, EquipFromSlot)
- [ ] A.6 Переписать `EquipmentController.cs` с нуля (новые слоты, двуручное, багфиксы, UnequipToInventory)
- [ ] A.7 Обновить `AssetGeneratorExtended.cs` ParseEquipmentSlot под новый enum
- [ ] A.8 Обновить `NPCPresetData.cs` под новый EquipmentSlot
- [ ] A.9 Обновить `PlayerController.cs` — API вызовы не меняются, но проверить
- [ ] A.10 Обновить `ResourcePickup.cs` — проверить API совместимость
- [ ] A.11 Обновить `QuestController.cs` — проверить API совместимость
- [ ] A.12 Удалить старый `InventoryUI.cs` (будет заменён)
- [ ] A.13 Удалить старый `CharacterPanelUI.cs` (будет заменён)
- [ ] A.14 Проверить компиляцию

**Файлы:** Enums.cs, ItemData.cs, BackpackData.cs (новый), InventoryController.cs, EquipmentController.cs, AssetGeneratorExtended.cs, NPCPresetData.cs, PlayerController.cs, ResourcePickup.cs, QuestController.cs

#### Этап B: UI инвентаря (заменяет этапы 4-5)

**Задачи:**
- [ ] B.1 `InventoryScreen.cs` — Canvas, открытие/закрытие по I, GameState.Inventory
- [ ] B.2 `BackpackGrid.cs` — динамическая сетка рюкзака (3×4 → любой)
- [ ] B.3 `BodyDollPanel.cs` — 7 видимых слотов (head, torso, belt, legs, feet, weapon_main, weapon_off)
- [ ] B.4 `ItemSlotUI.cs` — визуальный слот (иконка, рамка по редкости, счётчик)
- [ ] B.5 `ItemIconGenerator.cs` — процедурные иконки (цветной квадрат + буква)
- [ ] B.6 `TooltipPanel.cs` — карточка предмета при наведении
- [ ] B.7 `InfoPanel` — вес, кол-во предметов, название рюкзака
- [ ] B.8 Подключение к InventoryController + EquipmentController через события
- [ ] B.9 Проверить компиляцию + визуал в Unity

**Файлы:** новые в Scripts/UI/Inventory/

#### Этап C: Drag & Drop (заменяет этап 6)

**Задачи:**
- [ ] C.1 `DragHandler.cs` — логика перетаскивания
- [ ] C.2 Drag между слотами рюкзака + Swap
- [ ] C.3 Drag на куклу = EquipFromSlot
- [ ] C.4 Drag с куклы = UnequipToInventory
- [ ] C.5 Двуручное: визуальная блокировка off-слота
- [ ] C.6 Выброс предмета (drag мимо панели)
- [ ] C.7 Подсветка валидных/невалидных позиций
- [ ] C.8 Проверить компиляцию + функционал в Unity

**Файлы:** DragHandler.cs (новый)

### Сравнение вариантов

| Критерий | Вариант A (6 этапов, патчи) | Вариант B (3 этапа, переделка) |
|----------|---------------------------|-------------------------------|
| Риск регрессии | НИЗКИЙ — постепенные изменения | СРЕДНИЙ — полное обновление |
| Время | ДОЛЬШЕ — 6 итераций | КОРОЧЕ — 3 итерации |
| Качество результата | СРЕДНЕЕ — хаки остаются | ВЫСОКОЕ — чистая архитектура |
| Совместимость | Полная (поэтапно) | Breaking changes в EquipmentSlot |
| Количество файлов | Меньше новых | Больше новых (но чище) |
| Откат | Простой (по этапам) | Сложнее (через git) |

### Рекомендация

**Вариант B (полная переделка)** — предпочтительнее, потому что:
1. EquipmentSlot enum всё равно придётся полностью переделать — это breaking change в любом варианте
2. Инвентарь ещё НЕ подключён к SaveManager — нет риска потери данных
3. Старый UI (InventoryUI.cs, CharacterPanelUI.cs) всё равно переписывается
4. Чистая архитектура с нуля = меньше багов в будущем

---

*Чекпоинт создан: 2026-04-18 18:06:01 UTC*
*Редактировано: 2026-04-18 18:09:12 UTC*

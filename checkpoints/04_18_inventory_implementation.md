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

*Чекпоинт создан: 2026-04-18 18:06:01 UTC*

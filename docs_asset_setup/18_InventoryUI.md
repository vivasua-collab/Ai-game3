# Настройка UI инвентаря (Вручную)

**Категория:** Инвентарь  
**Спецификация:** `docs_temp/INVENTORY_UI_DRAFT.md` v2.0  
**Зависимости:** Phase07UI (Canvas), Phase16InventoryData (ассеты), Phase06Player (Player)  
**Редактировано:** 2026-04-27

---

## Обзор

Создание UI инвентаря вручную включает:
1. Создание InventoryScreen в GameUI Canvas
2. Настройка BodyDollPanel с 7 слотами экипировки
3. Настройка BackpackPanel со списком строк (VerticalLayoutGroup + ScrollRect)
4. Добавление TooltipPanel, DragDropHandler, ContextMenu
5. Настройка TabBar для переключения между хранилищами
6. Добавление SpiritStoragePanel и StorageRingPanel
7. Подключение контроллеров к UI

---

## Шаг 1: Создать InventoryScreen

1. В Hierarchy найдите `GameUI` (Canvas)
2. Правый клик → Create Empty → назовите `InventoryScreen`
3. Добавьте компонент `InventoryScreen` (скрипт из `Scripts/UI/Inventory/InventoryScreen.cs`)
4. В Inspector: снимите галочку (скрыть) — InventoryScreen должен быть неактивен по умолчанию
5. RectTransform: Stretch/Stretch (заполняет весь Canvas)

**Компоненты InventoryScreen:**
- `RectTransform` — anchor: Stretch/Stretch
- `CanvasGroup` — для управления видимостью
- `InventoryScreen` скрипт — главный контроллер панели

---

## Шаг 2: BackgroundOverlay

Внутри `InventoryScreen`:

1. Создайте пустой объект `BackgroundOverlay`
2. Добавьте `Image` — цвет: `rgba(0, 0, 0, 150)` (полупрозрачный чёрный)
3. RectTransform: Stretch/Stretch

---

## Шаг 3: MainPanel

Внутри `InventoryScreen`:

1. Создайте пустой объект `MainPanel`
2. Добавьте `Image` — цвет: `rgba(26, 26, 46, 240)` (тёмно-синий фон)
3. RectTransform: Center, размер 900×600
4. Добавьте `VerticalLayoutGroup`:
   - Padding: 10, 10, 10, 10
   - Spacing: 10
   - Child Alignment: Upper Center
   - Child Force Expand: Width ✓, Height ✗

### 3.1 Header

Внутри `MainPanel`:

1. Создайте пустой объект `Header`
2. Добавьте `LayoutElement` — preferred height: 40
3. Добавьте `HorizontalLayoutGroup`
4. Добавьте TMP Text «ИНВЕНТАРЬ» (шрифт 24, bold, белый)
5. Добавьте Button «✕» (закрытие) — справа

### 3.2 ContentArea

Внутри `MainPanel`:

1. Создайте пустой объект `ContentArea`
2. Добавьте `LayoutElement` — flexible height: 1
3. Добавьте `HorizontalLayoutGroup`:
   - Spacing: 15
   - Child Force Expand: Width ✗, Height ✓

---

## Шаг 4: BodyDollPanel

Внутри `ContentArea`:

1. Создайте пустой объект `BodyDollPanel`
2. Добавьте `Image` — цвет: `rgba(34, 34, 51, 255)`
3. Добавьте `LayoutElement` — preferred width: 200, flexible width: 0
4. Добавьте `VerticalLayoutGroup` — spacing: 8, padding: 10
5. Добавьте скрипт `BodyDollPanel`

### 4.1 Заголовок «КУКЛА»

TMP Text: «КУКЛА», шрифт 18, bold

### 4.2 Слоты экипировки (7 шт.)

Для каждого слота создайте:

| Имя объекта | Тип слота | Позиция (Y) |
|-------------|-----------|-------------|
| HeadSlot | Голова | 0 |
| TorsoSlot | Торс | -50 |
| BeltSlot | Пояс | -100 |
| LegsSlot | Ноги | -150 |
| FeetSlot | Ступни | -200 |
| WeaponMainSlot | Правая рука | -260 |
| WeaponOffSlot | Левая рука | -310 |

**Каждый слот:**
1. Создайте пустой объект с именем слота
2. Добавьте `Image` — цвет: `rgba(51, 51, 68, 255)`, рамка: серая `#444`
3. Размер: 180×40
4. Добавьте `EquipmentSlotUI` скрипт
5. В Inspector: установите `slotType` в соответствующее значение `EquipmentSlot`
6. Добавьте TMP Text (имя слота) — слева
7. Добавьте Image (иконка предмета) — справа, скрыта по умолчанию
8. Добавьте Image (durability bar) — снизу, скрыта по умолчанию

---

## Шаг 5: BackpackPanel

Внутри `ContentArea`:

1. Создайте пустой объект `BackpackPanel`
2. Добавьте `Image` — цвет: `rgba(34, 34, 51, 255)` с коричневым оттенком
3. Добавьте `LayoutElement` — preferred width: 450, flexible width: 1
4. Добавьте `VerticalLayoutGroup` — spacing: 8, padding: 10
5. Добавьте скрипт `BackpackPanel`

### 5.1 BackpackHeader

TMP Text: «Тканевая сумка — 30 кг / 50 л» — шрифт 16, цвет: коричневый

> ⚠️ Старый формат «Тканевая сумка (3×4)» — устарел, заменён на массу/объём.

### 5.2 ListContainer (строчная модель)

1. Создайте пустой объект `ListContainer`
2. Добавьте `ScrollRect`:
   - Horizontal: отключено
   - Vertical: включено
   - Movement Type: Elastic
   - Inertia: ✓
   - Scrollbar: создать вертикальную полосу прокрутки
3. Внутри Content (Viewport/Content) добавьте `VerticalLayoutGroup`:
   - Spacing: 2
   - Child Alignment: Upper Center
   - Child Force Expand: Width ✓, Height ✗
   - Padding: 4, 4, 4, 4
4. Строки создаются динамически из префаба `SlotRowPrefab` (скрипт `InventorySlotUI`)
5. Каждая строка содержит: nameText, weightText, volumeText

> ⚠️ Старый GridLayoutGroup с ячейками 50×50 — устарел. Теперь используется VerticalLayoutGroup + ScrollRect (Phase17).

### 5.3 WeightBar + VolumeBar

1. Создайте Slider «WeightBar»
2. Background: тёмно-серый
3. Fill: жёлтый (переход в красный при >80%)
4. TMP Text: «Вес: 0 / 30 кг»
5. Создайте Slider «VolumeBar»
6. Fill: голубой (переход в красный при >80%)
7. TMP Text: «Объём: 0 / 50 л»

---

## Шаг 6: BeltPanel

Внутри `MainPanel` (после ContentArea):

1. Создайте пустой объект `BeltPanel`
2. Добавьте `LayoutElement` — preferred height: 50
3. Добавьте `HorizontalLayoutGroup` — spacing: 5
4. TMP Text: «Пояс» — если beltSlots=0, показать «Нет пояса быстрого доступа»
5. Слоты пояса создаются динамически при надевании пояса

---

## Шаг 7: TabBar

Внутри `MainPanel` (после BeltPanel):

1. Создайте пустой объект `TabBar`
2. Добавьте `LayoutElement` — preferred height: 35
3. Добавьте `HorizontalLayoutGroup` — spacing: 5

### Вкладки:

| Кнопка | Текст | Условие видимости |
|--------|-------|-------------------|
| BackpackTab | 🎒 Рюкзак | Всегда |
| SpiritStorageTab | 👻 Дух. хранилище | Уровень культивации ≥ 3 |
| StorageRingTab | 💍 Кольцо | Есть кольцо хранения на кукле |

---

## Шаг 8: TooltipPanel

Внутри `InventoryScreen`:

1. Создайте пустой объект `TooltipPanel`
2. Добавьте `Image` — фон: `rgba(20, 20, 35, 245)`, рамка: по редкости
3. RectTransform: размер 250×300, pivot: (0, 1)
4. Добавьте скрипт `TooltipPanel`
5. Скрыть по умолчанию

**Содержимое Tooltip:**
- NameText (TMP) — название, цвет по редкости
- TypeText (TMP) — тип + редкость
- Separator (Image) — линия
- StatsText (TMP) — характеристики
- Separator (Image) — линия
- VolumeText (TMP) — «Объём: 2 | Вес: 2.5 кг»
- NestingText (TMP) — «Вложение: Любое»
- Separator (Image) — линия
- BonusesText (TMP) — бонусы
- RequirementsText (TMP) — требования
- Separator (Image) — линия
- DescriptionText (TMP) — описание (курсив)
- ValueText (TMP) — «Стоимость: 500 камней»

---

## Шаг 9: DragDropLayer

Внутри `InventoryScreen`:

1. Создайте пустой объект `DragDropLayer`
2. RectTransform: Stretch/Stretch
3. Добавьте скрипт `DragDropHandler`
4. Скрыть по умолчанию (активируется при перетаскивании)

---

## Шаг 10: ContextMenu

Внутри `InventoryScreen`:

1. Создайте пустой объект `ContextMenu`
2. Добавьте `VerticalLayoutGroup`
3. Добавьте скрипт `ContextMenuUI`
4. Скрыть по умолчанию

**Пункты меню (создаются динамически):**
- Экипировать / Снять
- Использовать
- В духовное хранилище
- В кольцо хранения
- Разделить стек
- Выбросить
- Подробнее

---

## Шаг 11: SpiritStoragePanel

Внутри `InventoryScreen`:

1. Создайте пустой объект `SpiritStoragePanel`
2. Аналогичен BackpackPanel, но вместо сетки — список (каталогизатор)
3. Добавьте `ScrollRect` с `VerticalLayoutGroup`
4. Добавьте скрипт `SpiritStoragePanel`
5. Скрыть по умолчанию (показывается при переключении вкладки)

**Содержимое:**
- FilterBar (Dropdown: Все / Оружие / Броня / Материалы / Расходники)
- SearchField (TMP InputField)
- CategoryGroups (динамические группы по категории)
- CostInfo: «Извлечение: 15 Ци + 2 сек»

---

## Шаг 12: StorageRingPanel

Внутри `InventoryScreen`:

1. Создайте пустой объект `StorageRingPanel`
2. Добавьте скрипт `StorageRingPanel`
3. Скрыть по умолчанию

**Содержимое:**
- VolumeBar (Slider: текущий / макс объём)
- ItemList (ScrollRect с предметами)
- CostInfo: «Извлечение: 7 Ци + 1.5 сек»

---

## Шаг 13: Подключение контроллеров

### 13.1 На Player

Убедитесь, что на объекте Player есть:
- `InventoryController` — уже добавлен Phase06
- `EquipmentController` — уже добавлен Phase06
- `SpiritStorageController` — **добавить вручную** (скрипт из `Scripts/Inventory/SpiritStorageController.cs`)
- `StorageRingController` — **добавить вручную** (скрипт из `Scripts/Inventory/StorageRingController.cs`)

### 13.2 В InventoryScreen Inspector

Перетащите ссылки:
- `inventoryController` → Player/InventoryController
- `equipmentController` → Player/EquipmentController
- `spiritStorageController` → Player/SpiritStorageController
- `storageRingController` → Player/StorageRingController

### 13.3 В BodyDollPanel Inspector

Перетащите:
- `equipmentController` → Player/EquipmentController
- Массив `equipmentSlots` → 7 объектов слотов (HeadSlot..WeaponOffSlot)

### 13.4 В BackpackPanel Inspector

Перетащите:
- `inventoryController` → Player/InventoryController

---

## Шаг 14: Настройка UIManager

В `UIManager` Inspector добавьте:
- `inventoryPanel` → InventoryScreen

Это обеспечит переключение GameState.Inventory по клавише I.

---

## Иерархия объектов (итого)

```
GameUI (Canvas)
├── HUD (существующий)
└── InventoryScreen (скрыт)
    ├── BackgroundOverlay
    ├── MainPanel
    │   ├── Header
    │   ├── ContentArea
    │   │   ├── BodyDollPanel
    │   │   │   ├── HeadSlot
    │   │   │   ├── TorsoSlot
    │   │   │   ├── BeltSlot
    │   │   │   ├── LegsSlot
    │   │   │   ├── FeetSlot
    │   │   │   ├── WeaponMainSlot
    │   │   │   └── WeaponOffSlot
    │   │   └── BackpackPanel
    │   │       ├── BackpackHeader
    │   │       ├── ListContainer (ScrollRect + VerticalLayoutGroup)
    │   │       │   └── SlotRowPrefab (динамические строки)
    │   │       ├── WeightBar
    │   │       └── VolumeBar
    │   ├── BeltPanel
    │   └── TabBar
    │       ├── BackpackTab
    │       ├── SpiritStorageTab
    │       └── StorageRingTab
    ├── TooltipPanel
    ├── DragDropLayer
    ├── ContextMenu
    ├── SpiritStoragePanel
    └── StorageRingPanel
```

---

*Документ создано: 2026-04-19 06:25:00 UTC*  
*Редактировано: 2026-04-27 — Переход на строчную модель: GridLayoutGroup → VerticalLayoutGroup+ScrollRect, строковый UI (nameText/weightText/volumeText), добавление VolumeBar*

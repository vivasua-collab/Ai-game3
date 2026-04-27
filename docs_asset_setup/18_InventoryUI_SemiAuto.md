# Настройка UI инвентаря (Полуавтомат)

**Инструмент:** `Tools → Full Scene Builder → Phase 17: Inventory UI`
**Спецификация:** `docs_temp/INVENTORY_UI_DRAFT.md` v2.0
**Зависимости:** Phase07UI (Canvas), Phase16InventoryData (ассеты)

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание InventoryScreen в GameUI Canvas | ✅ Автоматически |
| Создание BackgroundOverlay | ✅ Автоматически |
| Создание MainPanel + Header (900×600) | ✅ Автоматически |
| Создание ContentArea (HorizontalLayout, spacing 15) | ✅ Автоматически |
| Создание BodyDollPanel (200px, TitleText, BodySilhouette) | ✅ Автоматически |
| Создание 7 видимых DollSlotUI (180×40px) | ✅ Автоматически |
| Создание 4 скрытых DollSlotUI (кольца) | ✅ Автоматически |
| Создание StatsPanel (Урон, Защита, Статы) | ✅ Автоматически |
| Создание RingStorageIndicator (скрыт) | ✅ Автоматически |
| Wiring BodyDollPanel: 17 SerializeField | ✅ Автоматически |
| Wiring каждого DollSlotUI: 8 SerializeField | ✅ Автоматически |
| Создание BackpackPanel (450px, GridContainer) | ✅ Автоматически |
| Создание InventorySlotUI prefab (6 дочерних) | ✅ Автоматически |
| Создание WeightBar, BackpackNameText, WeightText, SlotsText | ✅ Автоматически |
| Wiring BackpackPanel: 7 SerializeField | ✅ Автоматически |
| Создание BeltPanel | ✅ Автоматически |
| Создание TabBar (3 вкладки) | ✅ Автоматически |
| Создание TooltipPanel (24 SerializeField wiring) | ✅ Автоматически |
| Создание DragDropLayer (5 SerializeField wiring) | ✅ Автоматически |
| Создание ContextMenu | ✅ Автоматически |
| Создание SpiritStoragePanel | ✅ Автоматически |
| Создание StorageRingPanel | ✅ Автоматически |
| Wiring InventoryScreen: все панели + кнопки | ✅ Автоматически |
| Wiring UIManager: inventoryPanel + inventoryScreen | ✅ Автоматически |
| Добавление SpiritStorageController/StorageRingController на Player | ❌ Phase 18 |
| Добавление спрайтов иконок | ❌ Руками |
| Добавление тестовых предметов | ❌ Руками (см. Шаг 3) |

---

## Шаг 1: Запуск генератора (АВТОМАТИЧЕСКИ)

**Меню:** `Tools → Full Scene Builder → Phase 17: Inventory UI`

**Предусловие:** Фазы 07 (UI) и 16 (Inventory Data) уже выполнены.

**Результат:** Создаётся полная иерархия объектов с подключёнными ссылками:

```
GameUI/
└── InventoryScreen (скрыт)
    ├── BackgroundOverlay
    ├── MainPanel (900×600)
    │   ├── Header ("ИНВЕНТАРЬ" + кнопка ✕ + Сорт.)
    │   ├── ContentArea
    │   │   ├── BodyDollPanel (200px)
    │   │   │   ├── TitleText "КУКЛА"
    │   │   │   ├── BodySilhouette (placeholder Image)
    │   │   │   ├── HeadSlot (DollSlotUI, EquipmentSlot.Head)
    │   │   │   ├── TorsoSlot (DollSlotUI, EquipmentSlot.Torso)
    │   │   │   ├── BeltSlot (DollSlotUI, EquipmentSlot.Belt)
    │   │   │   ├── LegsSlot (DollSlotUI, EquipmentSlot.Legs)
    │   │   │   ├── FeetSlot (DollSlotUI, EquipmentSlot.Feet)
    │   │   │   ├── WeaponMainSlot (DollSlotUI, EquipmentSlot.WeaponMain)
    │   │   │   ├── WeaponOffSlot (DollSlotUI, EquipmentSlot.WeaponOff)
    │   │   │   ├── RingLeft1Slot (скрыт)
    │   │   │   ├── RingLeft2Slot (скрыт)
    │   │   │   ├── RingRight1Slot (скрыт)
    │   │   │   ├── RingRight2Slot (скрыт)
    │   │   │   ├── StatsPanel
    │   │   │   │   ├── DamageText
    │   │   │   │   ├── DefenseText
    │   │   │   │   └── StatsSummaryText
    │   │   │   └── RingStorageIndicator (скрыт)
    │   │   │       └── RingVolumeText
    │   │   └── BackpackPanel (450px)
    │   │       ├── BackpackNameText "Тканевая сумка"
    │   │       ├── WeightText "0.0 / 10.0 кг"
    │   │       ├── WeightBar (Slider)
    │   │       ├── SlotsText "0/12"
    │   │       ├── GridBackground
    │   │       │   └── GridContainer
    │   │       └── SlotUIPrefab (неактивен, шаблон)
    │   ├── SpiritStoragePanel (скрыт)
    │   ├── BeltPanel
    │   └── TabBar
    │       ├── BackpackTab "Рюкзак"
    │       ├── SpiritStorageTab "Дух. хранилище"
    │       └── StorageRingTab "Кольцо"
    ├── TooltipPanel (скрыт, 250×300, 24 поля wired)
    ├── DragDropLayer
    │   ├── DragIcon (скрыт)
    │   ├── ContextMenuPrefab (скрыт)
    │   └── ContextMenuContainer
    └── ContextMenu (скрыт)
```

**Все SerializeField ссылки подключены автоматически.**
Ручное подключение НЕ требуется (в отличие от предыдущей версии).

---

## Шаг 2: Добавление контроллеров на Player (Phase 18)

**Меню:** `Tools → Full Scene Builder → Phase 18: Inventory Components`

Или вручную:
1. Выберите Player в Hierarchy
2. Add Component → `SpiritStorageController`
3. Add Component → `StorageRingController`

---

## Шаг 3: Проверка работоспособности

1. Запустите Play Mode
2. Нажмите **I** — инвентарь должен открыться
3. Левая сторона — кукла с 7 пустыми слотами
4. Правая сторона — сетка 3×4 (пустая, если нет предметов)
5. Нажмите **ESC** или **I** — инвентарь закрывается

### Как добавить тестовые предметы

Для проверки отображения предметов в сетке можно добавить их через код:
1. Найдите `InventoryController` на Player
2. В Play Mode вызовите через Inspector или скрипт:
```csharp
inventoryController.AddItem(itemData, 1); // ItemData из Assets/Data/
```

---

## Шаг 4: Добавление иконок (ВРУЧНУЮ, опционально)

Для каждого слота экипировки назначьте иконку пустого слота:
- Head: шлем
- Torso: нагрудник
- Belt: ремень
- Legs: поножи
- Feet: сапоги
- WeaponMain: меч
- WeaponOff: щит

Для BodySilhouette — назначьте спрайт силуэта персонажа.

---

## Клавиши управления

| Клавиша | Действие |
|---------|----------|
| I | Открыть/закрыть инвентарь |
| Левый клик | Выбрать/перетащить |
| Правый клик | Контекстное меню |
| Shift+клик | Переместить в хранилище |
| Ctrl+клик | Разделить стек |
| ESC | Закрыть инвентарь |

---

## Цветовая схема

| Элемент | Цвет |
|---------|------|
| Фон MainPanel | rgba(26, 26, 46, 240) |
| Фон BodyDollPanel | rgba(34, 34, 51, 255) |
| Фон слота куклы | rgba(51, 51, 68, 255) |
| Рамка слота куклы | rgba(68, 68, 68, 255) → цвет по редкости |
| Фон BackpackPanel | rgba(34, 34, 51, 255) + коричневый |
| Пустой слот сетки | rgba(26, 26, 46, 255) + рамка #444 |
| Common рамка | #6b7280 |
| Uncommon рамка | #22c55e |
| Rare рамка | #3b82f6 |
| Epic рамка | #a855f7 |
| Legendary рамка | #fbbf24 |
| Mythic рамка | #ef4444 |
| Блокировка (двуручное) | rgba(0.3, 0.3, 0.3, 0.6) |

---

## Wiring-отчёт (что подключает Phase17)

| Компонент | SerializeField полей | Статус |
|-----------|---------------------|--------|
| InventoryScreen | 10 (панели, кнопки, вкладки) | ✅ Автоматически |
| BackpackPanel | 7 (prefab, container, bar, texts) | ✅ Автоматически |
| InventorySlotUI prefab | 6 (icon, bg, border, count, dur, blocked) | ✅ Автоматически |
| DragDropHandler | 5 (dragIcon, transform, menu, container, tooltip) | ✅ Автоматически |
| TooltipPanel | 24 (все разделы) | ✅ Автоматически |
| BodyDollPanel | 17 (11 слотов + indicator + volume + stats + silhouette) | ✅ Автоматически |
| DollSlotUI × 11 | 8 × 11 = 88 (icon, border, label, name, dur, blocked, empty, slotType) | ✅ Автоматически |
| UIManager | 2 (inventoryPanel, inventoryScreen) | ✅ Автоматически |
| **ИТОГО** | **~150 wiring операций** | ✅ Все автоматически |

---

*Документ создано: 2026-04-19 06:25:00 UTC*
*Редактировано: 2026-04-25 16:10:00 MSK — Обновлено: все wiring теперь автоматически, DollSlotUI иерархия, полный wiring-отчёт*

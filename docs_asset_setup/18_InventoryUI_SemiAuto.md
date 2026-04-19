# Настройка UI инвентаря (Полуавтомат)

**Инструмент:** `Tools → Full Scene Builder → Phase 17: Inventory UI`
**Зависимости:** Phase07UI (Canvas), Phase16InventoryData (ассеты)

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание InventoryScreen в GameUI Canvas | ✅ Автоматически |
| Создание BackgroundOverlay | ✅ Автоматически |
| Создание MainPanel + Header | ✅ Автоматически |
| Создание BodyDollPanel с 7 слотами | ✅ Автоматически |
| Создание BackpackPanel с сеткой 3×4 | ✅ Автоматически |
| Создание WeightBar | ✅ Автоматически |
| Создание BeltPanel | ✅ Автоматически |
| Создание TabBar (3 вкладки) | ✅ Автоматически |
| Создание TooltipPanel | ✅ Автоматически |
| Создание DragDropLayer | ✅ Автоматически |
| Создание ContextMenu | ✅ Автоматически |
| Создание SpiritStoragePanel | ✅ Автоматически |
| Создание StorageRingPanel | ✅ Автоматически |
| Назначение скриптов на объекты | ✅ Автоматически |
| Подключение ссылок между компонентами | ❌ Частично (см. Шаг 2) |
| Добавление SpiritStorageController/StorageRingController на Player | ❌ Phase 18 |
| Добавление спрайтов иконок | ❌ Руками |

---

## Шаг 1: Запуск генератора (АВТОМАТИЧЕСКИ)

**Меню:** `Tools → Full Scene Builder → Phase 17: Inventory UI`

**Предусловие:** Фаза 07 (UI) уже выполнена — Canvas `GameUI` существует.

**Результат:** Создаётся иерархия объектов:

```
GameUI/
└── InventoryScreen (скрыт, CanvasGroup)
    ├── BackgroundOverlay
    ├── MainPanel
    │   ├── Header ("ИНВЕНТАРЬ" + кнопка ✕)
    │   ├── ContentArea
    │   │   ├── BodyDollPanel (7 EquipmentSlotUI)
    │   │   └── BackpackPanel (3×4 GridContainer + WeightBar)
    │   ├── BeltPanel
    │   └── TabBar (3 кнопки-вкладки)
    ├── TooltipPanel (скрыт)
    ├── DragDropLayer (скрыт)
    ├── ContextMenu (скрыт)
    ├── SpiritStoragePanel (скрыт)
    └── StorageRingPanel (скрыт)
```

---

## Шаг 2: Подключение ссылок (ВРУЧНУЮ)

### 2.1 InventoryScreen Inspector

В компоненте `InventoryScreen`:
- `inventoryController` → перетащите Player/InventoryController
- `equipmentController` → перетащите Player/EquipmentController
- `spiritStorageController` → перетащите Player/SpiritStorageController
- `storageRingController` → перетащите Player/StorageRingController
- `bodyDollPanel` → перетащите BodyDollPanel
- `backpackPanel` → перетащите BackpackPanel
- `tooltipPanel` → перетащите TooltipPanel
- `dragDropHandler` → перетащите DragDropLayer

### 2.2 BodyDollPanel Inspector

В компоненте `BodyDollPanel`:
- `equipmentController` → Player/EquipmentController
- `equipmentSlots` → добавьте 7 элементов:
  1. HeadSlot (slotType = WeaponMain → Head, если enum не совпадает — выбрать ближайший)
  2. TorsoSlot → Armor
  3. BeltSlot → Clothing
  4. LegsSlot → Armor
  5. FeetSlot → Armor
  6. WeaponMainSlot → WeaponMain
  7. WeaponOffSlot → WeaponOff

### 2.3 BackpackPanel Inspector

В компоненте `BackpackPanel`:
- `inventoryController` → Player/InventoryController

### 2.4 UIManager Inspector

В компоненте `UIManager`:
- `inventoryPanel` → InventoryScreen

---

## Шаг 3: Добавление контроллеров на Player (Phase 18)

**Меню:** `Tools → Full Scene Builder → Phase 18: Inventory Components`

Или вручную:
1. Выберите Player в Hierarchy
2. Add Component → `SpiritStorageController`
3. Add Component → `StorageRingController`

---

## Шаг 4: Добавление иконок (ВРУЧНУЮ)

Для каждого слота экипировки назначьте иконку пустого слота (опционально):
- Head: шлем
- Torso: нагрудник
- Belt: ремень
- Legs: поножи
- Feet: сапоги
- WeaponMain: меч
- WeaponOff: щит

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
| Фон BackpackPanel | rgba(34, 34, 51, 255) + коричневый |
| Пустой слот | rgba(26, 26, 46, 255) + рамка #444 |
| Common рамка | #6b7280 |
| Uncommon рамка | #22c55e |
| Rare рамка | #3b82f6 |
| Epic рамка | #a855f7 |
| Legendary рамка | #fbbf24 |
| Mythic рамка | #ef4444 |

---

*Документ создано: 2026-04-19 06:25:00 UTC*

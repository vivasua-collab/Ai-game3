# Чекпоинт: План внедрения куклы + сетки инвентаря
# Дата: 2026-04-25 12:52:20 UTC
# Статус: in_progress — реализация начата

---

## Контекст

Текущий этап: спрайты корректны, чёрные тайлы исправлены. Инвентарь открывается по клавише I,
но панели пустые — Phase17 создаёт компоненты без внутренней иерархии.

**Цель:** Реализовать классический инвентарь с куклой (левая сторона) и сеткой (правая сторона)
с отображением предметов.

---

## АРХИТЕКТУРА: что существует

### Рабочий код (НЕ трогать)

| Файл | Статус | Что делает |
|------|--------|------------|
| `InventoryScreen.cs` | ✅ Рабочий | Оркестратор, откр/закр, вкладки |
| `BackpackPanel.cs` | ✅ Рабочий | Сетка рюкзака, RebuildGrid, PlaceItemInGrid |
| `BodyDollPanel.cs` | ✅ Рабочий | Кукла, RefreshAllSlots, Equip/Unequip |
| `DollSlotUI` (в BodyDollPanel.cs) | ✅ Рабочий | Слот экипировки, SetEquipment, Clear |
| `InventorySlotUI.cs` | ✅ Рабочий | Ячейка сетки, SetSlot, drag, hover |
| `DragDropHandler.cs` | ✅ Рабочий | Перетаскивание, контекстное меню |
| `TooltipPanel.cs` | ✅ Рабочий | Карточка предмета |
| `StorageRingPanel.cs` | ✅ Рабочий | Кольцо хранения |

### Проблемный код (ИСПРАВИТЬ)

| Файл | Проблема |
|------|----------|
| `Phase17InventoryUI.cs` | Создаёт панели как пустые оболочки — BodyDollPanel без слотов, без силуета |

### Уже сделано в Phase17 (предыдущий фикс)

- ✅ BackpackPanel internals: GridBackground, GridContainer, BackpackNameText, WeightText, WeightBar, SlotsText, SlotUIPrefab — 7 SerializeField подключены
- ✅ InventorySlotUI prefab: 6 дочерних + wiring 6 SerializeField
- ✅ DragDropHandler internals: dragIcon, dragTransform, contextMenuPrefab, contextMenuContainer, tooltipPanel — 5 SerializeField подключены
- ✅ TooltipPanel internals: 24 SerializeField подключены
- ✅ InventoryScreen wiring: все панели + кнопки + вкладки подключены

---

## ПЛАН: Левая сторона — КУКЛА (BodyDollPanel)

### Что нужно создать в Phase17

Phase17 создаёт BodyDollPanel как пустой GameObject с компонентом.
Нужно добавить внутреннюю иерархию:

```
BodyDollPanel (RectTransform 0..0.35 x 0..1)
├── TitleText "КУКЛА" (TMP_Text, anchor: top-center)
├── BodySilhouette (Image, anchor: center) → bodySilhouette
├── HeadSlot (DollSlotUI) → headSlot
├── TorsoSlot (DollSlotUI) → torsoSlot
├── BeltSlot (DollSlotUI) → beltSlot
├── LegsSlot (DollSlotUI) → legsSlot
├── FeetSlot (DollSlotUI) → feetSlot
├── WeaponMainSlot (DollSlotUI) → weaponMainSlot
├── WeaponOffSlot (DollSlotUI) → weaponOffSlot
├── RingLeft1Slot (DollSlotUI, скрыт) → ringLeft1Slot
├── RingLeft2Slot (DollSlotUI, скрыт) → ringLeft2Slot
├── RingRight1Slot (DollSlotUI, скрыт) → ringRight1Slot
├── RingRight2Slot (DollSlotUI, скрыт) → ringRight2Slot
├── StatsPanel (GameObject)
│   ├── DamageText (TMP_Text) → damageText
│   ├── DefenseText (TMP_Text) → defenseText
│   └── StatsSummaryText (TMP_Text) → statsSummaryText
└── RingStorageIndicator (GameObject, скрыт)
    └── RingVolumeText (TMP_Text) → ringVolumeText
```

### DollSlotUI — внутренняя иерархия (7+1 дочерних на каждый слот)

Каждый DollSlotUI требует 8 SerializeField:

| Поле | Тип | Что создавать |
|------|-----|---------------|
| `slotType` | EquipmentSlot (enum) | Установить через SerializedProperty |
| `iconImage` | Image | Иконка экипированного предмета |
| `slotBorder` | Image | Рамка слота (цвет по редкости) |
| `slotLabel` | TMP_Text | Название слота («Голова», «Торс»...) |
| `itemNameText` | TMP_Text | Название экипированного предмета |
| `durabilityBar` | Image | Полоска прочности |
| `blockedOverlay` | GameObject | Затемнение при блокировке (двуручное) |
| `emptyIcon` | GameObject | Иконка пустого слота |

**Итого на 11 слотов:** 11 × 8 = 88 wiring операций

### Размеры куклы (из документации)

| Элемент | Размер | Источник |
|---------|--------|----------|
| BodyDollPanel | preferred width: 200px | 18_InventoryUI.md §4 |
| Слот экипировки | 180×40px | 18_InventoryUI.md §4.2 |
| Отступ между слотами | 8px vertical | 18_InventoryUI.md §4 |
| Padding | 10px | 18_InventoryUI.md §4 |

### Layout куклы (сверху вниз)

```
[TitleText "КУКЛА"] — h=22px
[BodySilhouette] — h=80px, center
[HeadSlot]       — h=40px, y=0
[TorsoSlot]      — h=40px, y=-48
[BeltSlot]       — h=40px, y=-96
[LegsSlot]       — h=40px, y=-144
[FeetSlot]       — h=40px, y=-192
[WeaponMainSlot] — h=40px, y=-248 (отступ больше — разделитель)
[WeaponOffSlot]  — h=40px, y=-296
[StatsPanel]     — гибкая высота
  [DamageText]
  [DefenseText]
  [StatsSummaryText]
```

### Цвета куклы (из 18_InventoryUI.md)

| Элемент | Цвет |
|---------|------|
| Фон куклы | rgba(34, 34, 51, 255) — тёмно-синий |
| Фон слота | rgba(51, 51, 68, 255) |
| Рамка слота | серая #444 → цвет по редкости при экипировке |
| Текст названия слота | белый/серый |
| Блокировка | rgba(0.3, 0.3, 0.3, 0.6) |

---

## ПЛАН: Правая сторона — СЕТКА (BackpackPanel)

### Текущее состояние

BackpackPanel уже имеет внутренние элементы и wiring:
- ✅ GridBackground, GridContainer — созданы
- ✅ SlotUIPrefab — создан с InventorySlotUI + 6 дочерних
- ✅ BackpackNameText, WeightText, WeightBar, SlotsText — созданы
- ✅ 7 SerializeField подключены

### Что НЕ работает

Сетка отображается, но предметы не видны потому что:
1. **Нет тестовых предметов** — InventoryController пуст при старте
2. **PlaceItems()** работает корректно, но `inventoryController.Slots` пуст

### Решение: Добавить тестовые предметы

Необходимо в Phase16 (или Phase17) добавить стартовые предметы в инвентарь
для демонстрации работы сетки:

| Предмет | Размер | Категория | Откуда |
|---------|--------|-----------|--------|
| Железный меч | 1×2 | Weapon | equipment.json |
| Кожаная броня | 1×2 | Armor | equipment.json |
| Деревянный щит | 2×2 | Armor | equipment.json |
| Пилюля здоровья | 1×1 | Consumable | items.json |
| Пилюля здоровья ×3 | 1×1 | Consumable (стак) | items.json |
| Железная руда ×5 | 1×1 | Material | items.json |
| Дерево ×3 | 1×1 | Material | items.json |

**Как:** Добавить метод `AddTestItems()` в Phase16InventoryData или Phase17.
Вызвать через InventoryController.AddItem() после инициализации.

### Размеры сетки (из документации)

| Элемент | Размер | Источник |
|---------|--------|----------|
| BackpackPanel | preferred width: 450px | 18_InventoryUI.md §5 |
| Ячейка (cellSize) | 50×50px | 18_InventoryUI.md §5.2 / BackpackPanel.cs |
| Расстояние (cellSpacing) | 2px | 18_InventoryUI.md §5.2 / BackpackPanel.cs |
| Padding | 8px | BackpackPanel.cs |
| Стартовый рюкзак | 3×4 (12 ячеек) | INVENTORY_UI_DRAFT.md §3.3 |

### Layout сетки (сверху вниз)

```
[BackpackNameText "Тканевая сумка"] — h=20px
[WeightBar 0.0/10.0 кг] — h=8px
[SlotsText 0/12] — h=14px (справа)
[GridBackground]
  └── [GridContainer]
       ├── [Cell 0,0] (50×50)
       ├── [Cell 1,0]
       ├── [Cell 2,0]
       ├── [Cell 0,1]
       ├── ...
       └── [Cell 2,3]
```

---

## ПЛАН: Wiring — BodyDollPanel SerializeField

### BodyDollPanel: 17 полей для wiring

```csharp
private void WireBodyDollPanelReferences(
    BodyDollPanel panel,
    DollSlotUI headSlot, DollSlotUI torsoSlot, DollSlotUI beltSlot,
    DollSlotUI legsSlot, DollSlotUI feetSlot,
    DollSlotUI weaponMainSlot, DollSlotUI weaponOffSlot,
    DollSlotUI ringLeft1Slot, DollSlotUI ringLeft2Slot,
    DollSlotUI ringRight1Slot, DollSlotUI ringRight2Slot,
    GameObject ringStorageIndicator, TMP_Text ringVolumeText,
    TMP_Text damageText, TMP_Text defenseText, TMP_Text statsSummaryText,
    Image bodySilhouette)
{
    SerializedObject so = new SerializedObject(panel);
    so.FindProperty("headSlot").objectReferenceValue = headSlot;
    so.FindProperty("torsoSlot").objectReferenceValue = torsoSlot;
    // ... 14 полей
    so.ApplyModifiedProperties();
}
```

### Каждый DollSlotUI: 8 полей для wiring

```csharp
private void WireDollSlotReferences(DollSlotUI slotUI,
    Image iconImage, Image slotBorder, TMP_Text slotLabel,
    TMP_Text itemNameText, Image durabilityBar,
    GameObject blockedOverlay, GameObject emptyIcon,
    EquipmentSlot slotType)
{
    SerializedObject so = new SerializedObject(slotUI);
    so.FindProperty("iconImage").objectReferenceValue = iconImage;
    so.FindProperty("slotBorder").objectReferenceValue = slotBorder;
    so.FindProperty("slotLabel").objectReferenceValue = slotLabel;
    so.FindProperty("itemNameText").objectReferenceValue = itemNameText;
    so.FindProperty("durabilityBar").objectReferenceValue = durabilityBar;
    so.FindProperty("blockedOverlay").objectReferenceValue = blockedOverlay;
    so.FindProperty("emptyIcon").objectReferenceValue = emptyIcon;
    so.FindProperty("slotType").intValue = (int)slotType;
    so.ApplyModifiedProperties();
}
```

**Итого wiring операций:** 17 (BodyDollPanel, включая bodySilhouette) + 11×8 (DollSlotUI) = 105

---

## ПОРЯДОК ВНЕДРЕНИЯ

### Шаг 1: Phase17 — Создать DollSlotUI (метод CreateDollSlot)

**Что:** Новый метод `CreateDollSlot()` — создаёт GameObject с DollSlotUI + 7 дочерних
**Файл:** `Phase17InventoryUI.cs`
**Риск:** Низкий — новый метод, не трогает существующий код
**Проверка:** DollSlotUI.Initialize() вызывает GetSlotDisplayName() → slotLabel.text

### Шаг 2: Phase17 — Создать внутреннюю иерархию BodyDollPanel

**Что:** Заменить пустой BodyDollPanel на полноценную иерархию:
- TitleText, BodySilhouette, 7 видимых DollSlotUI, 4 скрытых DollSlotUI
- StatsPanel (damageText, defenseText, statsSummaryText)
- RingStorageIndicator + RingVolumeText
**Файл:** `Phase17InventoryUI.cs` — метод `CreateContentArea()`
**Риск:** Средний — меняет существующий метод CreateContentArea
**Проверка:** BodyDollPanel.Initialize() → BuildSlotMap() → slotMap.Count == 11

### Шаг 3: Phase17 — Wire BodyDollPanel ссылки

**Что:** Добавить `WireBodyDollPanelReferences()` + вызов
**Файл:** `Phase17InventoryUI.cs`
**Риск:** Низкий — новый wiring метод
**Проверка:** BodyDollPanel.RefreshAllSlots() не выдаёт NullReferenceException

### Шаг 4: Добавить тестовые предметы в инвентарь

**Что:** Создать метод `AddTestItemsToInventory()` — добавляет 6 предметов
**Файл:** `Phase17InventoryUI.cs` или `Phase16InventoryData.cs`
**Риск:** Низкий — только добавление данных
**Проверка:** BackpackPanel.RebuildGrid() создаёт ячейки с предметами

### Шаг 5: Проверка и git push

**Что:** Запустить FullSceneBuilder → проверить инвентарь → git push
**Риск:** Нет

---

## ИЗМЕНЯЕМЫЕ ФАЙЛЫ

| Файл | Что меняется |
|------|-------------|
| `Phase17InventoryUI.cs` | +CreateDollSlot(), +WireBodyDollPanelReferences(), +WireDollSlotReferences(), доработка CreateContentArea(), +AddTestItemsToInventory() |

## НЕ ИЗМЕНЯЕМЫЕ ФАЙЛЫ

| Файл | Почему |
|------|--------|
| `BodyDollPanel.cs` | Код корректен, проблема только в wiring |
| `BackpackPanel.cs` | Код корректен, wiring уже сделан |
| `DollSlotUI` (в BodyDollPanel.cs) | Код корректен, проблема в wiring |
| `InventorySlotUI.cs` | Код корректен |
| `InventoryScreen.cs` | Код корректен |

---

## РИСКИ

| Риск | Вероятность | Митигация |
|------|-------------|-----------|
| DollSlotUI иерархия не совпадает с ожиданием кода | Средняя | Проверить все 8 SerializeField имён |
| EquipmentSlot.slotType не устанавливается через int | Низкая | SerializedProperty.intValue для enum |
| BodySilhouette не имеет спрайта → Image пустой | Высокая | Создать пустой Image с цветом (placeholder) |
| Тестовые предметы не создаются (ItemData = null) | Средняя | Проверить наличие JSON данных в Phase16 |
| GridLayoutGroup vs ручное позиционирование слотов куклы | Низкая | Кукла = VerticalLayout, не Grid |

---

## РАЗМЕРЫ из документации (справочно)

| Параметр | Значение | Источник |
|----------|----------|----------|
| MainPanel | 900×600 | docs_asset_setup/18_InventoryUI.md §3 |
| ContentArea | HorizontalLayout, spacing 15 | docs_asset_setup/18_InventoryUI.md §3.2 |
| BodyDollPanel | 200px width | docs_asset_setup/18_InventoryUI.md §4 |
| BackpackPanel | 450px width | docs_asset_setup/18_InventoryUI.md §5 |
| Слот куклы | 180×40px | docs_asset_setup/18_InventoryUI.md §4.2 |
| Ячейка сетки | 50×50px | docs_asset_setup/18_InventoryUI.md §5.2 |
| Spacing ячеек | 2px | docs_asset_setup/18_InventoryUI.md §5.2 |
| Стартовый рюкзак | 3×4 | docs_temp/INVENTORY_UI_DRAFT.md §3.3 |
| Header | h=40px | docs_asset_setup/18_InventoryUI.md §3.1 |
| BeltPanel | h=50px | docs_asset_setup/18_InventoryUI.md §6 |
| TabBar | h=35px | docs_asset_setup/18_InventoryUI.md §7 |
| TooltipPanel | 250×300 | docs_asset_setup/18_InventoryUI.md §8 |

---

## ПРЕДПОСЫЛКИ

Этапы 0-2 из `docs_temp/INVENTORY_IMPLEMENTATION_PLAN.md` уже выполнены:
- ✅ Этап 0: EquipmentSlot enum обновлён (Enums.cs), WeaponHandType добавлен, BackpackData SO создан
- ✅ Этап 1: EquipmentController переписан (7 видимых слотов, 1H/2H)
- ✅ Этап 2: InventoryController переписан (динамическая сетка от рюкзака)
- Текущий план = Этап 3 UI (wiring Phase17)

---

## ИСТОРИЯ

*Чекпоинт создан: 2026-04-25 12:52:20 UTC — план без кода*
*Редактировано: 2026-04-25 15:57 MSK — 4 правки по аудиту: wiring-счёт 16→17, полный путь docs_asset_setup/, 2×2 предмет, предпосылки этапов 0-2*

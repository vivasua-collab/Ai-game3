# Чекпоинт: План внедрения куклы + сетки инвентаря
# Дата: 2026-04-25 12:52:20 UTC
# Статус: in_progress — redesign куклы (v2: двухколоночный layout)

---

## Контекст

Текущий этап: спрайты корректны, чёрные тайлы исправлены. Инвентарь открывается по клавише I,
но текущая реализация куклы (вертикальный список 180×40 слотов) неприемлема.

**Цель:** Реализовать инвентарь с двухколоночной куклой: силуэт тела по центру,
квадратные слоты по бокам. Слева — одежда, справа — оружие/кольца.

---

## РЕДИЗАЙН КУКЛЫ (v2)

### Проблема текущей реализации

Текущий layout куклы — вертикальный список прямоугольных слотов (180×40).
Это не похоже на классическую RPG-куклу. Пользователь требует:

1. **Сгенерировать картинку тела** — силуэт персонажа по центру
2. **Квадратные слоты** — вместо прямоугольных 180×40
3. **Две колонки** — слоты размещены по бокам тела
4. **Левая колонка** — экипировка одежды (Голова, Торс, Пояс, Ноги, Ступни)
5. **Правая колонка** — экипировка рук (Осн. рука, Доп. рука), кольца (скрыты)

### Новый layout куклы

```
BodyDollPanel (width: 300px, anchor: 0..0.40 × 0..1)
├── TitleText "КУКЛА" (h=20px, top-center)
├── DollArea (fills remaining space)
│   ├── LeftColumn (x: 2..54)
│   │   ├── HeadSlot     (50×50, y=top+25)
│   │   ├── TorsoSlot    (50×50, y=top+80)
│   │   ├── BeltSlot     (50×50, y=top+135)
│   │   ├── LegsSlot     (50×50, y=top+190)
│   │   └── FeetSlot     (50×50, y=top+245)
│   ├── BodySilhouette   (centered, 80×220, procedural sprite)
│   ├── RightColumn (x: right-54..right-2)
│   │   ├── WeaponMainSlot (50×50, y=top+80)
│   │   ├── WeaponOffSlot  (50×50, y=top+135)
│   │   ├── RingLeft1Slot  (50×50, hidden)
│   │   ├── RingLeft2Slot  (50×50, hidden)
│   │   ├── RingRight1Slot (50×50, hidden)
│   │   └── RingRight2Slot (50×50, hidden)
├── StatsPanel (bottom)
│   ├── DamageText
│   ├── DefenseText
│   └── StatsSummaryText
└── RingStorageIndicator (hidden)
    └── RingVolumeText
```

### DollSlotUI — квадратный слот (50×50)

Каждый DollSlotUI сохраняет те же 8 SerializeField, но меняется визуальный layout:

| Поле | Тип | Визуальное расположение |
|------|-----|------------------------|
| `slotType` | EquipmentSlot | SerializedProperty.intValue |
| `iconImage` | Image | Центр слота, 42×42 (4px отступ) |
| `slotBorder` | Image | Рамка по периметру 50×50 |
| `slotLabel` | TMP_Text | Нижняя часть, мелкий шрифт 8pt |
| `itemNameText` | TMP_Text | Скрыт (показывается в tooltip) |
| `durabilityBar` | Image | Тонкая полоска внизу (2px) |
| `blockedOverlay` | GameObject | Полное перекрытие 50×50 |
| `emptyIcon` | GameObject | Центр слота, 24×24 |

### Силуэт тела — процедурная генерация

Метод `GenerateBodySilhouetteSprite()`:
- Создаёт Texture2D 80×220px
- Рисует схематичную фигуру человека (голова, торс, руки, ноги)
- Сохраняет как PNG в `Assets/Sprites/UI/BodySilhouette.png`
- Импортирует как Sprite через AssetDatabase
- Цвет: rgba(40, 40, 60, 255) — тёмно-синий, как фон куклы, но чуть светлее

### Изменение размеров

| Параметр | Было | Стало | Причина |
|----------|------|-------|---------|
| BodyDollPanel width | 200px (0.35) | 300px (0.40) | Две колонки + тело по центру |
| BackpackPanel anchor | 0.37..1.0 | 0.42..1.0 | Сдвиг из-за расширения куклы |
| Слот куклы | 180×40px | 50×50px | Квадратные слоты |
| Силуэт тела | 60×80 placeholder | 80×220 procedural | Реальная фигура |

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

### Уже сделано в Phase17 (предыдущий фикс)

- ✅ BackpackPanel internals: 7 SerializeField подключены
- ✅ InventorySlotUI prefab: 6 дочерних + wiring
- ✅ DragDropHandler internals: 5 SerializeField подключены
- ✅ TooltipPanel internals: 24 SerializeField подключены
- ✅ InventoryScreen wiring: все панели + кнопки + вкладки подключены
- ✅ BodyDollPanel wiring: 17 SerializeField подключены (старый layout)

---

## ПОРЯДОК ВНЕДРЕНИЯ

### Шаг 1: Modify CreateDollSlot — квадратные слоты

**Что:** Переписать `CreateDollSlot()` — 50×50 вместо 180×40
**Файл:** `Phase17InventoryUI.cs`
**Риск:** Низкий — визуальное изменение, SerializeField те же

### Шаг 2: Добавить GenerateBodySilhouetteSprite

**Что:** Новый метод — процедурная генерация силуэта тела
**Файл:** `Phase17InventoryUI.cs`
**Риск:** Низкий — новый метод

### Шаг 3: Переписать CreateContentArea — двухколоночный layout

**Что:** Заменить вертикальный список слотов на двухколоночный layout:
- BodyDollPanel width: 0.40
- BodySilhouette по центру
- Левая колонка: 5 слотов одежды
- Правая колонка: 2 видимых + 4 скрытых слота
- StatsPanel внизу
**Файл:** `Phase17InventoryUI.cs`
**Риск:** Средний — меняет CreateContentArea

### Шаг 4: Обновить BackpackPanel anchor

**Что:** Сдвинуть BackpackPanel anchor с 0.37 на 0.42
**Файл:** `Phase17InventoryUI.cs`
**Риск:** Низкий

---

## ИЗМЕНЯЕМЫЕ ФАЙЛЫ

| Файл | Что меняется |
|------|-------------|
| `Phase17InventoryUI.cs` | Редизайн куклы: CreateDollSlot → 50×50, +GenerateBodySilhouetteSprite, CreateContentArea двухколоночный, якоря панелей |

## НЕ ИЗМЕНЯЕМЫЕ ФАЙЛЫ

| Файл | Почему |
|------|--------|
| `BodyDollPanel.cs` | SerializeField те же, меняется только визуальный layout |
| `BackpackPanel.cs` | Код корректен |
| `DollSlotUI` (в BodyDollPanel.cs) | Код корректен, wiring через те же 8 полей |
| `InventorySlotUI.cs` | Код корректен |
| `InventoryScreen.cs` | Код корректен |

---

## РИСКИ

| Риск | Вероятность | Митигация |
|------|-------------|-----------|
| Процедурный силуэт выглядит грубо | Средняя | MVP — замена на арт позже |
| Квадратные слоты слишком мелкие для текста | Низкая | itemNameText скрыт, tooltip показывает детали |
| BackpackPanel сдвиг влияет на сетку | Низкая | Anchor 0.42 даёт достаточно места |
| 50×50 слот не совпадает с ожиданием DollSlotUI кода | Низкая | Код не зависит от размера, только от SerializeField |

---

## ПРЕДПОСЫЛКИ

Этапы 0-2 из `docs_temp/INVENTORY_IMPLEMENTATION_PLAN.md` уже выполнены:
- ✅ Этап 0: EquipmentSlot enum обновлён
- ✅ Этап 1: EquipmentController переписан
- ✅ Этап 2: InventoryController переписан
- Текущий план = Этап 3 UI (редизайн куклы)

---

## ИСТОРИЯ

*Чекпоинт создан: 2026-04-25 12:52:20 UTC — план без кода*
*Редактировано: 2026-04-25 15:57 MSK — 4 правки по аудиту*
*Редактировано: 2026-04-25 18:30 MSK — РЕДИЗАЙН: двухколоночный layout, квадратные 50×50 слоты, процедурный силуэт тела, BodyDollPanel 300px*

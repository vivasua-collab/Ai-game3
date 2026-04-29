# 🎨 Аудит спрайтов экипировки — Перечень и план генерации

**Дата:** 2026-04-28 15:40 UTC
**Проект:** Cultivation World Simulator (Unity 6.3 URP 2D)
**Статус:** ✅ Этап 1 завершён — Icon-спрайты сгенерированы
**Редактировано:** 2026-04-29 06:05:00 UTC

---

## 📎 Перекрёстные ссылки

| Документ | Описание |
|----------|----------|
| **`docs/SPRITE_INDEX.md`** §5 | Полный перечень существующих спрайтов экипировки (icon, 21 файл) |
| **`docs_temp/EQUIPPED_SPRITES_DRAFT.md`** | Черновик системы equipped-спрайтов (архитектура Overlay, код) |

---

## ⚠️ Задача

Отображение экипировки в двух состояниях:
1. **На земле (Item Drop)** — иконка предмета в инвентаре и на карте ✅ *(реализовано, перечень см. `docs/SPRITE_INDEX.md` §5)*
2. **Надетая на персонажа (Equipped)** — визуальное изменение спрайта персонажа ❌ *(НЕ реализовано, см. `EQUIPPED_SPRITES_DRAFT.md`)*

---

## 📐 Спецификация спрайтов

| Параметр | Значение |
|----------|----------|
| Размер исходный | 1024×1024 px |
| Размер рабочий (иконка) | 128×128 px (уменьшенная копия) |
| Формат | PNG, RGB (без альфа-канала — фон заливается) |
| Стиль | Fantasy icon, xianxia aesthetic, тёмный фон |
| PPU | 100 (для Unity Sprite) |

### Два типа спрайтов на каждый предмет:

| Тип | Описание | Размер | Назначение |
|-----|----------|--------|------------|
| **Icon** | Иконка предмета (вид сверху/сбоку) | 128×128 px | Инвентарь, слот экипировки, дроп на карте |
| **Equipped** | Визуальный слой на персонаже | 256×256 px | Накладывается на спрайт персонажа |

> **Упрощённая модель (решение 2026-04-29):** 1 слот = 1 предмет = 1 equipped-спрайт. Система слоёв экипировки (Матрёшка v1) упразднена — нельзя надеть несколько предметов в один слот. Equipped-спрайт накладывается как отдельный SpriteRenderer (sorting order) поверх базового спрайта персонажа.

---

## 📊 СУЩЕСТВУЮЩИЕ СПРАЙТЫ

**21 icon-спрайт** экипировки существует. Полный перечень с описаниями → **`docs/SPRITE_INDEX.md`** §5 «Equipment (Снаряжение)».

**Статус equipped-спрайтов:** ❌ Ни для одного предмета нет equipped-версии. См. **`EQUIPPED_SPRITES_DRAFT.md`**.

---

## 🔴 НЕХВАТАЮЩИЕ ICON-СПРАЙТЫ

### Оружие — отсутствует (4 подтипа)

| # | Имя файла | Подтип | Описание |
|---|-----------|--------|----------|
| W1 | weapon_hammer_iron.png | Hammer | Боевой молот, железо |
| W2 | weapon_mace_iron.png | Mace | Булава, железо |
| W3 | weapon_wand_wood.png | Wand | Жезл, дерево |
| W4 | weapon_fists.png | Unarmed | Кулаки (вариант без когтей) |

### Броня — отсутствует (15 комбинаций)

| # | Имя файла | Подтип | Вес.класс | Описание |
|---|-----------|--------|-----------|----------|
| A1 | armor_helmet_medium.png | Head | Medium | Шлем кольчужный |
| A2 | armor_bracers_cloth.png | Arms | Light | Наручи тканевые |
| A3 | armor_bracers_chain.png | Arms | Medium | Наручи кольчужные |
| A4 | armor_bracers_plate.png | Arms | Heavy | Наручи латные |
| A5 | armor_gloves_chain.png | Hands | Medium | Перчатки кольчужные |
| A6 | armor_gauntlets_plate.png | Hands | Heavy | Рукавицы латные |
| A7 | armor_pants_cloth.png | Legs | Light | Штаны тканевые |
| A8 | armor_pants_leather.png | Legs | Medium | Штаны кожаные |
| A9 | armor_boots_cloth.png | Feet | Light | Тапки тканевые |
| A10 | armor_boots_chain.png | Feet | Medium | Сапоги кольчужные |
| A11 | armor_sabatons_plate.png | Feet | Heavy | Сабатоны латные |
| A12 | armor_full_robe.png | Full | Light | Роба полная |
| A13 | armor_full_chain.png | Full | Medium | Кольчуга полная |
| A14 | armor_full_plate.png | Full | Heavy | Латы полные |
| A15 | armor_vest_spirit.png | Torso | Light (T3) | Жилет духовный |

### Аксессуары — отсутствует полностью (5 слотов)

| # | Имя файла | Слот | Описание |
|---|-----------|------|----------|
| AC1 | accessory_ring_bronze.png | Ring | Кольцо бронзовое |
| AC2 | accessory_ring_silver.png | Ring | Кольцо серебряное |
| AC3 | accessory_ring_jade.png | Ring | Кольцо нефритовое |
| AC4 | accessory_amulet_jade.png | Amulet | Амулет нефритовый |
| AC5 | accessory_belt_leather.png | Belt | Пояс кожаный |

---

## 🔴 НЕХВАТАЮЩИЕ EQUIPPED-СПРАЙТЫ (для всех!)

**Критическая проблема:** Ни для одного предмета экипировки нет Equipped-спрайта (наложение на персонажа). Всего существует 21 icon-спрайт + 24 планируется = **45 предметов нуждаются в Equipped-версии**.

### Приоритет Equipped-спрайтов (по слотам персонажа):

| Приоритет | Слот | Кол-во вариантов | Описание |
|-----------|------|-------------------|----------|
| 🔴 1 | Torso | 6 | Роба/латы/кольчуга — самое заметное |
| 🔴 2 | Head | 3 | Шлем/капюшон — второе по заметности |
| 🟡 3 | WeaponMain | 6 | Меч/топор/посох — в руке |
| 🟡 4 | Legs | 4 | Поножи/штаны |
| 🟡 5 | Feet | 4 | Сапоги/сабатоны |
| 🟢 6 | Hands | 4 | Перчатки/рукавицы |
| 🟢 7 | Arms | 3 | Наручи |
| 🟢 8 | Full body | 3 | Полный доспех |

---

## 📋 ПЛАН ГЕНЕРАЦИИ — 3 ЭТАПА

### Этап 1: Icon-спрайты — отсутствующие (24 шт.)

Генерация иконок для недостающих подтипов оружия, брони и аксессуаров.

**Размер:** 1024×1024 px (исходный) → 128×128 px (уменьшенный)

| Группа | Кол-во | Файлы |
|--------|--------|-------|
| Оружие | 4 | W1-W4 |
| Броня | 15 | A1-A15 |
| Аксессуары | 5 | AC1-AC5 |

### Этап 2: Equipped-спрайты — приоритетные (16 шт.)

Генерация спрайтов надетой экипировки для самых заметных слотов.

| Группа | Кол-во | Слоты |
|--------|--------|-------|
| Torso | 6 | robe_cloth, robe_spirit, vest_leather, chainmail, torso_iron, full_plate |
| Head | 3 | hood_cloth, helmet_medium, helmet_iron |
| WeaponMain | 4 | sword_iron, axe_iron, staff_wood, bow_wood |
| Legs | 3 | pants_cloth, pants_leather, greaves_iron |

### Этап 3: Equipped-спрайты — остальные (29 шт.)

Остальные Equipped-спрайты для Hands, Feet, Arms, Full body, аксессуаров.

---

## 🎨 ПРОМПТЫ ДЛЯ AI-ГЕНЕРАЦИИ

### Стиль (общий для всех):
```
Fantasy RPG item icon, xianxia cultivation game style, dark background,
detailed pixel art style, vibrant colors, Chinese martial arts aesthetic,
game asset, 2D sprite, no text, centered composition
```

### Пример промпта для оружия:
```
Fantasy RPG weapon icon, iron battle hammer, xianxia cultivation game style,
dark background, detailed pixel art style, heavy war hammer with ornate handle,
Chinese martial arts aesthetic, game asset, 2D sprite, no text, centered
```

### Пример промпта для Equipped-спрайта:
```
Fantasy RPG character equipment overlay, iron plate torso armor worn on character,
xianxia cultivation game style, full body character overlay, transparent background,
armor layer sprite, 2D game asset, no text
```

---

## 📁 СТРУКТУРА ПАПОК

```
Sprites/
├── Equipment/
│   ├── Icons/           ← 128×128 иконки (инвентарь/дроп)
│   │   ├── Weapons/
│   │   ├── Armor/
│   │   └── Accessories/
│   ├── Sources/         ← 1024×1024 исходники
│   │   ├── Weapons/
│   │   ├── Armor/
│   │   └── Accessories/
│   └── Equipped/        ← Спрайты надетой экипировки
│       ├── Torso/
│       ├── Head/
│       ├── Legs/
│       ├── Feet/
│       ├── Hands/
│       ├── Arms/
│       ├── Full/
│       ├── WeaponMain/
│       └── Accessories/
└── ...
```

---

## 📊 СВОДКА

| Категория | Существует | Не хватает Icon | Не хватает Equipped | Всего нужно |
|-----------|-----------|-----------------|---------------------|-------------|
| Оружие | 11 | 4 | 11 | 15 |
| Броня | 10 | 15 | 10 | 25 |
| Аксессуары | 0 | 5 | 0 | 5 |
| **Итого** | **21** | **24** | **21** | **45** |

**Этап 1 (Icon):** 24 спрайта × 1024×1024 + уменьшенные 128×128
**Этап 2 (Equipped приоритет):** 16 спрайтов
**Этап 3 (Equipped остальные):** 29 спрайтов

---

*Создано: 2026-04-28 15:40 UTC*

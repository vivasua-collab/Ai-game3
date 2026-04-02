# Настройка MaterialData

**Путь:** `Assets/Data/Materials/`

**Создание:** `Create → Cultivation → Material`

---

## Структура полей (из MaterialData.cs)

### Basic Info (наследуется от ItemData)
| Поле | Тип | Описание |
|------|-----|----------|
| itemId | string | Уникальный ID |
| nameRu | string | Название на русском |
| nameEn | string | Название на английском |
| description | text | Описание |
| icon | Sprite | Иконка |
| category | ItemCategory | Material |
| rarity | ItemRarity | Редкость |
| stackable | bool | true |
| maxStack | int | Максимум в стаке |
| weight | float | Вес (кг) |
| value | int | Стоимость |

### Material
| Поле | Тип | Описание |
|------|-----|----------|
| tier | int (1-5) | Тир материала |
| materialCategory | MaterialCategory | Категория материала |

### Properties
| Поле | Тип | Описание |
|------|-----|----------|
| hardness | int (1-100) | Твёрдость |
| durability | int (1-100) | Прочность |
| conductivity | float (0.1-10) | Проводимость Ци |

### Bonuses
| Поле | Тип | Описание |
|------|-----|----------|
| damageBonus | float | Бонус к урону |
| defenseBonus | float | Бонус к защите |
| qiConductivityBonus | float | Бонус к проводимости Ци |

### Source
| Поле | Тип | Описание |
|------|-----|----------|
| source | text | Где добывается |
| dropChance | float (%) | Шанс выпадения |
| requiredLevel | int (1-10) | Мин. уровень культивации |

---

## Enums

### MaterialCategory
| Категория | Описание |
|-----------|----------|
| Metal | Металл |
| Leather | Кожа |
| Cloth | Ткань |
| Wood | Дерево |
| Bone | Кость |
| Crystal | Кристалл |
| Gem | Драгоценный камень |
| Organic | Органический |
| Spirit | Духовный |
| Void | Пустотный |

### ItemRarity для материалов
| Тир | Rarity | Описание |
|-----|--------|----------|
| 1 | Common | Обычные материалы |
| 2 | Uncommon | Качественные материалы |
| 3 | Rare | Духовные материалы |
| 4 | Epic | Небесные материалы |
| 5 | Legendary | Першородные материалы |

---

## Тир 1: Обычные материалы (5 предметов)

### Материал 1: Железо

**Имя файла:** `Mat_Iron`

```
=== Basic Info ===
itemId: iron
nameRu: Железо
nameEn: Iron
description: Базовый металл. Широко используется в кузнечном деле.
category: Material
rarity: Common
stackable: true
maxStack: 100
weight: 1.0
value: 10

=== Material ===
tier: 1
materialCategory: Metal

=== Properties ===
hardness: 40
durability: 30
conductivity: 0.3

=== Bonuses ===
damageBonus: 0
defenseBonus: 0
qiConductivityBonus: 0

=== Source ===
source: Шахты, Торговцы, Разрушенные здания
dropChance: 50
requiredLevel: 1
```

---

### Материал 2: Кожа

**Имя файла:** `Mat_Leather`

```
=== Basic Info ===
itemId: leather
nameRu: Кожа
nameEn: Leather
description: Выделанная кожа животных. Используется для брони.
category: Material
rarity: Common
stackable: true
maxStack: 50
weight: 0.5
value: 20

=== Material ===
tier: 1
materialCategory: Organic

=== Properties ===
hardness: 10
durability: 25
conductivity: 0.5

=== Bonuses ===
damageBonus: 0
defenseBonus: 0
qiConductivityBonus: 0

=== Source ===
source: Охота на животных, Торговцы
dropChance: 40
requiredLevel: 1
```

---

### Материал 3: Ткань

**Имя файла:** `Mat_Cloth`

```
=== Basic Info ===
itemId: cloth
nameRu: Ткань
nameEn: Cloth
description: Обычная ткань. Не мешает циркуляции Ци.
category: Material
rarity: Common
stackable: true
maxStack: 100
weight: 0.2
value: 5

=== Material ===
tier: 1
materialCategory: Organic

=== Properties ===
hardness: 2
durability: 20
conductivity: 0.8

=== Bonuses ===
damageBonus: 0
defenseBonus: 0
qiConductivityBonus: 0

=== Source ===
source: Торговцы, Сбор растений
dropChance: 60
requiredLevel: 1
```

---

### Материал 4: Дерево

**Имя файла:** `Mat_Wood`

```
=== Basic Info ===
itemId: wood
nameRu: Дерево
nameEn: Wood
description: Обычное дерево. Используется для изготовления.
category: Material
rarity: Common
stackable: true
maxStack: 100
weight: 0.5
value: 3

=== Material ===
tier: 1
materialCategory: Wood

=== Properties ===
hardness: 15
durability: 25
conductivity: 0.4

=== Bonuses ===
damageBonus: 0
defenseBonus: 0
qiConductivityBonus: 0

=== Source ===
source: Леса, Торговцы
dropChance: 70
requiredLevel: 1
```

---

### Материал 5: Кость

**Имя файла:** `Mat_Bone`

```
=== Basic Info ===
itemId: bone
nameRu: Кость
nameEn: Bone
description: Кость животных. Используется для изделий.
category: Material
rarity: Common
stackable: true
maxStack: 50
weight: 0.3
value: 15

=== Material ===
tier: 1
materialCategory: Bone

=== Properties ===
hardness: 25
durability: 30
conductivity: 0.6

=== Bonuses ===
damageBonus: 0
defenseBonus: 0
qiConductivityBonus: 0

=== Source ===
source: Охота, Пещеры
dropChance: 30
requiredLevel: 1
```

---

## Тир 2: Качественные материалы (3 предмета)

### Материал 6: Сталь

**Имя файла:** `Mat_Steel`

```
=== Basic Info ===
itemId: steel
nameRu: Сталь
nameEn: Steel
description: Качественная сталь. Лучше железа для оружия и брони.
category: Material
rarity: Uncommon
stackable: true
maxStack: 50
weight: 1.2
value: 50

=== Material ===
tier: 2
materialCategory: Metal

=== Properties ===
hardness: 60
durability: 55
conductivity: 0.5

=== Bonuses ===
damageBonus: 0.1
defenseBonus: 0.1
qiConductivityBonus: 0

=== Source ===
source: Кузницы, Торговцы
dropChance: 20
requiredLevel: 2
```

---

### Материал 7: Шёлк

**Имя файла:** `Mat_Silk`

```
=== Basic Info ===
itemId: silk
nameRu: Шёлк
nameEn: Silk
description: Качественный шёлк. Хорошо проводит Ци.
category: Material
rarity: Uncommon
stackable: true
maxStack: 50
weight: 0.1
value: 40

=== Material ===
tier: 2
materialCategory: Organic

=== Properties ===
hardness: 5
durability: 40
conductivity: 1.0

=== Bonuses ===
damageBonus: 0
defenseBonus: 0.05
qiConductivityBonus: 0.05

=== Source ===
source: Торговцы, Шёлковые черви
dropChance: 15
requiredLevel: 2
```

---

### Материал 8: Серебро

**Имя файла:** `Mat_Silver`

```
=== Basic Info ===
itemId: silver
nameRu: Серебро
nameEn: Silver
description: Драгоценный металл. Хорошо проводит Ци.
category: Material
rarity: Uncommon
stackable: true
maxStack: 30
value: 80

=== Material ===
tier: 2
materialCategory: Metal

=== Properties ===
hardness: 30
durability: 50
conductivity: 1.2

=== Bonuses ===
damageBonus: 0.05
defenseBonus: 0
qiConductivityBonus: 0.1

=== Source ===
source: Шахты, Торговцы
dropChance: 10
requiredLevel: 2
```

---

## Тир 3: Духовные материалы (3 предмета)

### Материал 9: Духовное железо

**Имя файла:** `Mat_SpiritIron`

```
=== Basic Info ===
itemId: spirit_iron
nameRu: Духовное железо
nameEn: Spirit Iron
description: Железо, пропитанное Ци. Самовосстанавливается со временем.
category: Material
rarity: Rare
stackable: true
maxStack: 30
weight: 1.0
value: 300

=== Material ===
tier: 3
materialCategory: Spirit

=== Properties ===
hardness: 80
durability: 100
conductivity: 1.5

=== Bonuses ===
damageBonus: 0.2
defenseBonus: 0.15
qiConductivityBonus: 0.05

=== Source ===
source: Духовные шахты, Секты
dropChance: 5
requiredLevel: 3
```

---

### Материал 10: Нефрит

**Имя файла:** `Mat_Jade`

```
=== Basic Info ===
itemId: jade
nameRu: Нефрит
nameEn: Jade
description: Драгоценный камень. Отлично проводит Ци.
category: Material
rarity: Rare
stackable: true
maxStack: 20
weight: 0.3
value: 200

=== Material ===
tier: 3
materialCategory: Crystal

=== Properties ===
hardness: 70
durability: 90
conductivity: 2.0

=== Bonuses ===
damageBonus: 0.1
defenseBonus: 0.2
qiConductivityBonus: 0.1

=== Source ===
source: Горы, Секты
dropChance: 3
requiredLevel: 3
```

---

### Материал 11: Холодное железо

**Имя файла:** `Mat_ColdIron`

```
=== Basic Info ===
itemId: cold_iron
nameRu: Холодное железо
nameEn: Cold Iron
description: Железо из северных земель. Холодное и прочное.
category: Material
rarity: Rare
stackable: true
maxStack: 30
weight: 1.2
value: 250

=== Material ===
tier: 3
materialCategory: Metal

=== Properties ===
hardness: 75
durability: 85
conductivity: 1.0

=== Bonuses ===
damageBonus: 0.25
defenseBonus: 0.1
qiConductivityBonus: 0

=== Source ===
source: Северные земли, Подземелья
dropChance: 4
requiredLevel: 4
```

---

## Тир 4: Небесные материалы (3 предмета)

### Материал 12: Звёздный металл

**Имя файла:** `Mat_StarMetal`

```
=== Basic Info ===
itemId: star_metal
nameRu: Звёздный металл
nameEn: Star Metal
description: Металл упавшего метеорита. Невероятно прочен.
category: Material
rarity: Epic
stackable: true
maxStack: 10
weight: 1.5
value: 1000

=== Material ===
tier: 4
materialCategory: Metal

=== Properties ===
hardness: 150
durability: 200
conductivity: 3.0

=== Bonuses ===
damageBonus: 0.4
defenseBonus: 0.3
qiConductivityBonus: 0.1

=== Source ===
source: Падающие звёзды, Древние руины
dropChance: 1
requiredLevel: 6
```

---

### Материал 13: Кость дракона

**Имя файла:** `Mat_DragonBone`

```
=== Basic Info ===
itemId: dragon_bone
nameRu: Кость дракона
nameEn: Dragon Bone
description: Кость древнего дракона. Содержит мощную энергию.
category: Material
rarity: Epic
stackable: true
maxStack: 10
weight: 0.8
value: 1500

=== Material ===
tier: 4
materialCategory: Bone

=== Properties ===
hardness: 180
durability: 250
conductivity: 3.5

=== Bonuses ===
damageBonus: 0.5
defenseBonus: 0.25
qiConductivityBonus: 0.15

=== Source ===
source: Логова драконов, Древние поля битв
dropChance: 0.5
requiredLevel: 7
```

---

### Материал 14: Элементальное ядро

**Имя файла:** `Mat_ElementalCore`

```
=== Basic Info ===
itemId: elemental_core
nameRu: Элементальное ядро
nameEn: Elemental Core
description: Кристаллизованная энергия элементаля.
category: Material
rarity: Epic
stackable: true
maxStack: 10
weight: 0.5
value: 800

=== Material ===
tier: 4
materialCategory: Crystal

=== Properties ===
hardness: 120
durability: 150
conductivity: 3.0

=== Bonuses ===
damageBonus: 0.35
defenseBonus: 0.35
qiConductivityBonus: 0.2

=== Source ===
source: Элементали, Духовные области
dropChance: 0.8
requiredLevel: 5
```

---

## Тир 5: Первородные материалы (3 предмета)

### Материал 15: Материя пустоты

**Имя файла:** `Mat_VoidMatter`

```
=== Basic Info ===
itemId: void_matter
nameRu: Материя пустоты
nameEn: Void Matter
description: Материал из самой пустоты. Искажает реальность.
category: Material
rarity: Legendary
stackable: true
maxStack: 5
weight: 0.1
value: 5000

=== Material ===
tier: 5
materialCategory: Void

=== Properties ===
hardness: 300
durability: 400
conductivity: 4.5

=== Bonuses ===
damageBonus: 0.6
defenseBonus: 0.5
qiConductivityBonus: 0.25

=== Source ===
source: Разломы пустоты, Измерение пустоты
dropChance: 0.1
requiredLevel: 8
```

---

### Материал 16: Материя хаоса

**Имя файла:** `Mat_ChaosMatter`

```
=== Basic Info ===
itemId: chaos_matter
nameRu: Материя хаоса
nameEn: Chaos Matter
description: Первозданный хаос. Невероятно нестабилен.
category: Material
rarity: Legendary
stackable: true
maxStack: 5
weight: 0.2
value: 8000

=== Material ===
tier: 5
materialCategory: Void

=== Properties ===
hardness: 250
durability: 350
conductivity: 5.0

=== Bonuses ===
damageBonus: 0.7
defenseBonus: 0.4
qiConductivityBonus: 0.3

=== Source ===
source: Области хаоса, Древние катаклизмы
dropChance: 0.05
requiredLevel: 9
```

---

### Материал 17: Первородная эссенция

**Имя файла:** `Mat_PrimordialEssence`

```
=== Basic Info ===
itemId: primordial_essence
nameRu: Первородная эссенция
nameEn: Primordial Essence
description: Эссенция времён сотворения мира. Легендарный материал.
category: Material
rarity: Legendary
stackable: true
maxStack: 3
weight: 0.05
value: 10000

=== Material ===
tier: 5
materialCategory: Spirit

=== Properties ===
hardness: 200
durability: 500
conductivity: 5.0

=== Bonuses ===
damageBonus: 0.5
defenseBonus: 0.6
qiConductivityBonus: 0.35

=== Source ===
source: Ядро мира, Первородные существа
dropChance: 0.01
requiredLevel: 10
```

---

## Сводная таблица материалов (17 предметов)

### Тир 1: Обычные
| ID | Название | Категория | Твёрдость | Прочность | Проводимость | Дроп |
|----|----------|-----------|-----------|-----------|--------------|------|
| iron | Железо | Metal | 40 | 30 | 0.3 | 50% |
| leather | Кожа | Organic | 10 | 25 | 0.5 | 40% |
| cloth | Ткань | Organic | 2 | 20 | 0.8 | 60% |
| wood | Дерево | Wood | 15 | 25 | 0.4 | 70% |
| bone | Кость | Bone | 25 | 30 | 0.6 | 30% |

### Тир 2: Качественные
| ID | Название | Категория | Твёрдость | Прочность | Проводимость | Дроп |
|----|----------|-----------|-----------|-----------|--------------|------|
| steel | Сталь | Metal | 60 | 55 | 0.5 | 20% |
| silk | Шёлк | Organic | 5 | 40 | 1.0 | 15% |
| silver | Серебро | Metal | 30 | 50 | 1.2 | 10% |

### Тир 3: Духовные
| ID | Название | Категория | Твёрдость | Прочность | Проводимость | Дроп |
|----|----------|-----------|-----------|-----------|--------------|------|
| spirit_iron | Духовное железо | Spirit | 80 | 100 | 1.5 | 5% |
| jade | Нефрит | Crystal | 70 | 90 | 2.0 | 3% |
| cold_iron | Холодное железо | Metal | 75 | 85 | 1.0 | 4% |

### Тир 4: Небесные
| ID | Название | Категория | Твёрдость | Прочность | Проводимость | Дроп |
|----|----------|-----------|-----------|-----------|--------------|------|
| star_metal | Звёздный металл | Metal | 150 | 200 | 3.0 | 1% |
| dragon_bone | Кость дракона | Bone | 180 | 250 | 3.5 | 0.5% |
| elemental_core | Элементальное ядро | Crystal | 120 | 150 | 3.0 | 0.8% |

### Тир 5: Первородные
| ID | Название | Категория | Твёрдость | Прочность | Проводимость | Дроп |
|----|----------|-----------|-----------|-----------|--------------|------|
| void_matter | Материя пустоты | Void | 300 | 400 | 4.5 | 0.1% |
| chaos_matter | Материя хаоса | Void | 250 | 350 | 5.0 | 0.05% |
| primordial_essence | Первородная эссенция | Spirit | 200 | 500 | 5.0 | 0.01% |

---

## Формулы материала

### Бонусы к оружию
```
finalDamage = baseDamage × (1 + material.damageBonus)
```

### Бонусы к броне
```
finalDefense = baseDefense × (1 + material.defenseBonus)
```

### Проводимость Ци
```
qiEfficiency = baseEfficiency + material.qiConductivityBonus
```

---

## Важные правила

1. **Тир материала** определяет редкость и требования
2. **hardness** влияет на урон оружия
3. **durability** влияет на прочность изделия
4. **conductivity** влияет на проводимость Ци
5. **qiConductivityBonus** может быть отрицательным (штраф)
6. **requiredLevel** — минимальный уровень культивации для добычи
7. **dropChance** — шанс выпадения с врагов/локаций

---

*Документ создан: 2026-04-01*
*Источник данных: UnityProject/Assets/Data/JSON/materials.json*

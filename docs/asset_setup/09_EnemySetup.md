# Настройка Enemy Data

**Путь:** `Assets/Data/Enemies/`

**Примечание:** Враги не используют ScriptableObject, а генерируются из JSON данных через NPCGenerator.

---

## Структура JSON данных (из enemies.json)

### Basic Info
| Поле | Тип | Описание |
|------|-----|----------|
| enemyId | string | Уникальный ID (enemy_type_name) |
| nameRu | string | Название на русском |
| nameEn | string | Название на английском |
| description | text | Описание врага |

### Classification
| Поле | Тип | Описание |
|------|-----|----------|
| soulType | enum | creature, spirit, construct |
| morphology | enum | humanoid, quadruped, bird, serpentine, arthropod, amorphous |
| species | string | Вид существа |
| bodyMaterial | enum | organic, scaled, chitin, ethereal, mineral |

### Cultivation
| Поле | Тип | Описание |
|------|-----|----------|
| cultivationLevel | int (1-10) | Уровень культивации |
| cultivationSubLevel | int (0-9) | Под-уровень |
| coreCapacity | long | Ёмкость ядра |
| qiPercentage | float (%) | Текущее Ци (%) |

### Stats
| Поле | Тип | Описание |
|------|-----|----------|
| strength | int | Сила |
| agility | int | Ловкость |
| intelligence | int | Интеллект |
| vitality | int | Жизнеспособность |
| conductivity | float | Проводимость |

### Behavior
| Поле | Тип | Описание |
|------|-----|----------|
| behaviorType | enum | Passive, Defensive, Neutral, Aggressive, Hostile |
| aggressiveness | float (0-100) | Агрессивность |
| courage | float (0-100) | Смелость |
| aggroRadius | float | Радиус агрессии |
| attackRadius | float | Радиус атаки |

### Loot
| Поле | Тип | Описание |
|------|-----|----------|
| lootTable | List | Таблица лута (itemId, dropChance %, minQty, maxQty) |
| isBoss | bool | Является боссом |

### Techniques
| Поле | Тип | Описание |
|------|-----|----------|
| techniques | List | Список ID техник |

---

## Enums

### SoulType
| Тип | Описание |
|-----|----------|
| creature | Животное/существо |
| spirit | Дух/призрак |
| construct | Конструкт/голем |

### Morphology
| Тип | Описание |
|-----|----------|
| humanoid | Гуманоид |
| quadruped | Четвероногое |
| bird | Птица |
| serpentine | Змеевидное |
| arthropod | Членистоногое |
| amorphous | Бесформенное |

### BodyMaterial
| Материал | Описание |
|----------|----------|
| organic | Органический |
| scaled | Чешуйчатый |
| chitin | Хитиновый |
| ethereal | Эфирный |
| mineral | Минеральный |

---

## Враг 1: Лесной волк

```
=== Basic Info ===
enemyId: enemy_forest_wolf
nameRu: Лесной волк
nameEn: Forest Wolf
description: Обычный лесной волк. Охотится стаями, нападает на слабых путников.

=== Classification ===
soulType: creature
morphology: quadruped
species: wolf
bodyMaterial: organic

=== Cultivation ===
cultivationLevel: 1
cultivationSubLevel: 0
coreCapacity: 50
qiPercentage: 100

=== Stats ===
strength: 12
agility: 14
intelligence: 4
vitality: 10
conductivity: 0.3

=== Behavior ===
behaviorType: Aggressive
aggressiveness: 70
courage: 60
aggroRadius: 15
attackRadius: 2

=== Loot ===
lootTable:
  - itemId: mat_leather, dropChance: 40, minQuantity: 1, maxQuantity: 2
  - itemId: item_food_meat, dropChance: 30, minQuantity: 1, maxQuantity: 1

=== Techniques ===
techniques: (пусто)
```

---

## Враг 2: Горный тигр

```
=== Basic Info ===
enemyId: enemy_mountain_tiger
nameRu: Горный тигр
nameEn: Mountain Tiger
description: Полосатый хищник, мастер засад. Быстр и смертоносен.

=== Classification ===
soulType: creature
morphology: quadruped
species: tiger
bodyMaterial: organic

=== Cultivation ===
cultivationLevel: 3
cultivationSubLevel: 0
coreCapacity: 180
qiPercentage: 100

=== Stats ===
strength: 20
agility: 18
intelligence: 7
vitality: 16
conductivity: 0.5

=== Behavior ===
behaviorType: Aggressive
aggressiveness: 65
courage: 75
aggroRadius: 18
attackRadius: 2

=== Loot ===
lootTable:
  - itemId: mat_leather, dropChance: 50, minQuantity: 2, maxQuantity: 3
  - itemId: item_food_meat, dropChance: 50, minQuantity: 2, maxQuantity: 3
  - itemId: mat_spirit_stone, dropChance: 5, minQuantity: 1, maxQuantity: 1

=== Techniques ===
techniques: ["tech_wind_blade_01"]
```

---

## Враг 3: Блуждающий призрак

```
=== Basic Info ===
enemyId: enemy_spirit_ghost
nameRu: Блуждающий призрак
nameEn: Wandering Ghost
description: Душа умершего, не нашедшая покоя. Бесплотна и неуязвима для обычного оружия.

=== Classification ===
soulType: spirit
morphology: amorphous
species: ghost
bodyMaterial: ethereal

=== Cultivation ===
cultivationLevel: 3
cultivationSubLevel: 0
coreCapacity: 250
qiPercentage: 100

=== Stats ===
strength: 5
agility: 18
intelligence: 12
vitality: 8
conductivity: 2.0

=== Behavior ===
behaviorType: Aggressive
aggressiveness: 80
courage: 40
aggroRadius: 20
attackRadius: 3

=== Loot ===
lootTable:
  - itemId: mat_spirit_stone, dropChance: 20, minQuantity: 1, maxQuantity: 2

=== Techniques ===
techniques: ["tech_void_pierce_01"]
```

---

## Враг 4: Каменный голем

```
=== Basic Info ===
enemyId: enemy_dungeon_golem_stone
nameRu: Каменный голем
nameEn: Stone Golem
description: Анимированная каменная статуя. Медленный, но невероятно прочный.

=== Classification ===
soulType: construct
morphology: humanoid
species: golem
bodyMaterial: mineral

=== Cultivation ===
cultivationLevel: 4
cultivationSubLevel: 3
coreCapacity: 400
qiPercentage: 100

=== Stats ===
strength: 30
agility: 6
intelligence: 4
vitality: 35
conductivity: 0.8

=== Behavior ===
behaviorType: Defensive
aggressiveness: 20
courage: 100
aggroRadius: 10
attackRadius: 2

=== Loot ===
lootTable:
  - itemId: mat_iron_ore, dropChance: 40, minQuantity: 2, maxQuantity: 4
  - itemId: mat_spirit_stone, dropChance: 20, minQuantity: 1, maxQuantity: 2

=== Techniques ===
techniques: ["tech_earth_palm_01"]
```

---

## Босс 1: Духовный тигр

```
=== Basic Info ===
enemyId: enemy_boss_spirit_tiger
nameRu: Духовный тигр
nameEn: Spirit Tiger
description: Тигр, достигший высокой стадии культивации. Владеет Ци ветра и молнии.

=== Classification ===
soulType: creature
morphology: quadruped
species: tiger
bodyMaterial: scaled

=== Cultivation ===
cultivationLevel: 6
cultivationSubLevel: 5
coreCapacity: 2000
qiPercentage: 100

=== Stats ===
strength: 45
agility: 40
intelligence: 25
vitality: 50
conductivity: 2.5

=== Behavior ===
behaviorType: Aggressive
aggressiveness: 70
courage: 85
aggroRadius: 30
attackRadius: 4
isBoss: true

=== Loot ===
lootTable:
  - itemId: mat_leather, dropChance: 100, minQuantity: 5, maxQuantity: 8
  - itemId: mat_spirit_stone, dropChance: 80, minQuantity: 3, maxQuantity: 5
  - itemId: mat_jade, dropChance: 40, minQuantity: 1, maxQuantity: 2
  - itemId: scroll_technique_common, dropChance: 30, minQuantity: 1, maxQuantity: 1

=== Techniques ===
techniques: ["tech_wind_blade_01", "tech_lightning_step_01", "tech_thunder_strike_03"]
```

---

## Босс 2: Ледяной дракон

```
=== Basic Info ===
enemyId: enemy_boss_ice_dragon
nameRu: Ледяной дракон
nameEn: Ice Dragon
description: Древний дракон севера. Его дыхание замораживает всё живое.

=== Classification ===
soulType: creature
morphology: quadruped
species: dragon
bodyMaterial: scaled

=== Cultivation ===
cultivationLevel: 8
cultivationSubLevel: 0
coreCapacity: 8000
qiPercentage: 100

=== Stats ===
strength: 60
agility: 35
intelligence: 50
vitality: 70
conductivity: 5.0

=== Behavior ===
behaviorType: Aggressive
aggressiveness: 75
courage: 95
aggroRadius: 50
attackRadius: 8
isBoss: true

=== Loot ===
lootTable:
  - itemId: mat_spirit_stone, dropChance: 100, minQuantity: 10, maxQuantity: 20
  - itemId: mat_jade, dropChance: 80, minQuantity: 3, maxQuantity: 5
  - itemId: mat_star_metal, dropChance: 40, minQuantity: 1, maxQuantity: 2
  - itemId: scroll_technique_common, dropChance: 70, minQuantity: 1, maxQuantity: 2

=== Techniques ===
techniques: ["tech_water_shield_01", "tech_void_pierce_01", "tech_healing_light_01"]
```

---

## Сводная таблица врагов (27 штук)

### Лесные существа (L1-2)
| ID | Название | Уровень | Тип | Поведение |
|----|----------|---------|-----|-----------|
| enemy_forest_wolf | Лесной волк | L1.0 | creature | Aggressive |
| enemy_forest_boar | Лесной кабан | L1.3 | creature | Neutral |
| enemy_forest_snake | Лесная змея | L1.5 | creature | Defensive |
| enemy_forest_spider | Лесной паук | L2.0 | creature | Aggressive |
| enemy_forest_deer | Лесной олень | L1.0 | creature | Passive |

### Горные существа (L2-4)
| ID | Название | Уровень | Тип | Поведение |
|----|----------|---------|-----|-----------|
| enemy_mountain_bear | Горный медведь | L2.5 | creature | Neutral |
| enemy_mountain_eagle | Горный орёл | L2.2 | creature | Neutral |
| enemy_mountain_tiger | Горный тигр | L3.0 | creature | Aggressive |
| enemy_cave_bear | Пещерный медведь | L4.0 | creature | Aggressive |

### Духи (L3-6)
| ID | Название | Уровень | Тип | Поведение |
|----|----------|---------|-----|-----------|
| enemy_spirit_ghost | Блуждающий призрак | L3.0 | spirit | Aggressive |
| enemy_spirit_wraith | Мстительный дух | L4.5 | spirit | Aggressive |
| enemy_spirit_wisp | Духовный огонёк | L2.5 | spirit | Passive |
| enemy_elemental_fire | Огненный элементаль | L3.5 | spirit | Aggressive |
| enemy_elemental_ice | Ледяной элементаль | L4.0 | spirit | Aggressive |
| enemy_elemental_lightning | Грозовой элементаль | L5.0 | spirit | Aggressive |

### Подземелья (L4-7)
| ID | Название | Уровень | Тип | Поведение |
|----|----------|---------|-----|-----------|
| enemy_dungeon_golem_stone | Каменный голем | L4.3 | construct | Defensive |
| enemy_dungeon_golem_iron | Железный голем | L5.5 | construct | Defensive |
| enemy_dungeon_construct_blade | Клинок-страж | L5.0 | construct | Aggressive |
| enemy_dungeon_corrupted_wolf | Осквернённый волк | L4.0 | creature | Aggressive |
| enemy_dungeon_corrupted_bear | Осквернённый медведь | L5.3 | creature | Aggressive |

### Боссы (L5-9)
| ID | Название | Уровень | Тип |
|----|----------|---------|-----|
| enemy_boss_spirit_tiger | Духовный тигр | L6.5 | creature |
| enemy_boss_fire_phoenix | Огненный феникс | L7.0 | creature |
| enemy_boss_ice_dragon | Ледяной дракон | L8.0 | creature |
| enemy_boss_demon_general | Генерал демонов | L7.5 | creature |
| enemy_boss_ancient_golem | Древний голем | L6.0 | construct |
| enemy_boss_spectral_king | Призрачный король | L8.5 | spirit |

---

## Важные правила

1. **ethereal существа** — иммунны к физическому урону
2. **construct** — не имеют кровотечения, но уязвимы к электричеству
3. **Boss** — всегда dropChance 100% для основных материалов
4. **aggroRadius** — 0 для Passive существ
5. **coreCapacity** должен соответствовать уровню культивации

---

*Документ создан: 2026-04-01*
*Источник данных: UnityProject/Assets/Data/JSON/enemies.json*

# Настройка TechniqueData

**Путь:** `Assets/Data/Techniques/`

**Создание:** Правый клик → Create → Cultivation → Technique

---

## Структура полей (из TechniqueData.cs)

### Basic Info
| Поле | Тип | Описание |
|------|-----|----------|
| techniqueId | string | Уникальный ID (tech_element_name_XX) |
| nameRu | string | Название на русском |
| nameEn | string | Название на английском |
| description | text | Описание техники |
| icon | Sprite | Иконка (опционально) |

### Classification
| Поле | Тип | Описание |
|------|-----|----------|
| techniqueType | enum | Combat, Defense, Curse, Cultivation, Formation, Healing, Movement, Poison, Sensory, Support |
| combatSubtype | enum | None, MeleeStrike, MeleeWeapon, RangedProjectile, RangedBeam, RangedAoe |
| element | enum | Neutral, Fire, Water, Earth, Air, Lightning, Void, Poison |
| grade | enum | Damaged, Common, Refined, Perfect, Transcendent |
| techniqueLevel | int (1-9) | Уровень техники |

### Leveling
| Поле | Тип | Описание |
|------|-----|----------|
| minLevel | int (1-9) | Минимальный уровень развития |
| maxLevel | int (1-9) | Максимальный уровень развития |
| canEvolve | bool | Можно развивать |

### Costs
| Поле | Тип | Описание |
|------|-----|----------|
| baseQiCost | int | Базовая стоимость Ци |
| physicalFatigueCost | float (%) | Физическая усталость |
| mentalFatigueCost | float (%) | Ментальная усталость |
| cooldown | int | Кулдаун (тики) |

### Capacity
| Поле | Тип | Описание |
|------|-----|----------|
| baseCapacity | int | Базовая ёмкость техники |
| isUltimate | bool | Ultimate-техника |

### Scaling
| Поле | Тип | Описание |
|------|-----|----------|
| strengthScaling | float (0-1) | Масштабирование от силы |
| agilityScaling | float (0-1) | Масштабирование от ловкости |
| intelligenceScaling | float (0-1) | Масштабирование от интеллекта |
| conductivityScaling | float (0-1) | Масштабирование от проводимости |

### Requirements
| Поле | Тип | Описание |
|------|-----|----------|
| minCultivationLevel | int (1-10) | Мин. уровень культивации |
| minStrength | int (1-100) | Мин. сила |
| minAgility | int (1-100) | Мин. ловкость |
| minIntelligence | int (1-100) | Мин. интеллект |
| minConductivity | float | Мин. проводимость |

### Effects
| Поле | Тип | Описание |
|------|-----|----------|
| effectType | enum | Damage, Heal, Buff, Debuff, Shield, Movement, StatBoost, StatReduction, Elemental, Special, Block, Dodge, Reflect |
| value | float | Значение эффекта |
| duration | int | Длительность (0 = мгновенный) |
| chance | float (%) | Шанс срабатывания |

### Acquisition
| Поле | Тип | Описание |
|------|-----|----------|
| sources | List<string> | scroll, npc, insight |
| learnableFromScroll | bool | Можно выучить из свитка |
| learnableFromNPC | bool | Можно получить от NPC |

---

## Техника 1: Огненный кулак

**Имя файла:** `Tech_FireFist_01`

```
=== Basic Info ===
techniqueId: tech_fire_fist_01
nameRu: Огненный кулак
nameEn: Fire Fist
description: Базовая боевая техника огненной стихии. Концентрирует Ци в кулаке, создавая пламенное покрытие.

=== Classification ===
techniqueType: Combat
combatSubtype: MeleeStrike
element: Fire
grade: Common
techniqueLevel: 1

=== Leveling ===
minLevel: 1
maxLevel: 5
canEvolve: true

=== Costs ===
baseQiCost: 10
physicalFatigueCost: 2
mentalFatigueCost: 0
cooldown: 3

=== Capacity ===
baseCapacity: 64
isUltimate: false

=== Scaling ===
strengthScaling: 0.05
agilityScaling: 0.025
intelligenceScaling: 0
conductivityScaling: 0.5

=== Requirements ===
minCultivationLevel: 1
minStrength: 5
minAgility: 5
minIntelligence: 3
minConductivity: 0.5

=== Effects ===
[0] effectType: Damage, value: 64, duration: 0, chance: 100
[1] effectType: Elemental, value: 10, duration: 60, chance: 20

=== Acquisition ===
sources: ["scroll", "npc", "insight"]
learnableFromScroll: true
learnableFromNPC: true
```

---

## Техника 2: Водный щит

**Имя файла:** `Tech_WaterShield_01`

```
=== Basic Info ===
techniqueId: tech_water_shield_01
nameRu: Водный щит
nameEn: Water Shield
description: Создаёт защитный барьер из водяного Ци, поглощающий входящий урон.

=== Classification ===
techniqueType: Defense
combatSubtype: None
element: Water
grade: Common
techniqueLevel: 1

=== Leveling ===
minLevel: 1
maxLevel: 5
canEvolve: true

=== Costs ===
baseQiCost: 15
physicalFatigueCost: 0
mentalFatigueCost: 3
cooldown: 10

=== Capacity ===
baseCapacity: 72
isUltimate: false

=== Scaling ===
strengthScaling: 0
agilityScaling: 0
intelligenceScaling: 0.05
conductivityScaling: 0.6

=== Requirements ===
minCultivationLevel: 1
minStrength: 3
minAgility: 3
minIntelligence: 5
minConductivity: 0.5

=== Effects ===
[0] effectType: Shield, value: 72, duration: 120, chance: 100

=== Acquisition ===
sources: ["scroll", "npc"]
learnableFromScroll: true
learnableFromNPC: true
```

---

## Техника 3: Молниеносный шаг

**Имя файла:** `Tech_LightningStep_01`

```
=== Basic Info ===
techniqueId: tech_lightning_step_01
nameRu: Молниеносный шаг
nameEn: Lightning Step
description: Мгновенное перемещение на короткое расстояние с помощью молниевого Ци.

=== Classification ===
techniqueType: Movement
combatSubtype: None
element: Lightning
grade: Common
techniqueLevel: 1

=== Leveling ===
minLevel: 1
maxLevel: 5
canEvolve: true

=== Costs ===
baseQiCost: 8
physicalFatigueCost: 1
mentalFatigueCost: 1
cooldown: 5

=== Capacity ===
baseCapacity: 40
isUltimate: false

=== Scaling ===
strengthScaling: 0
agilityScaling: 0.05
intelligenceScaling: 0.03
conductivityScaling: 0.4

=== Requirements ===
minCultivationLevel: 1
minStrength: 3
minAgility: 5
minIntelligence: 3
minConductivity: 0.5

=== Effects ===
[0] effectType: Movement, value: 5, duration: 0, chance: 100

=== Acquisition ===
sources: ["scroll", "npc"]
learnableFromScroll: true
learnableFromNPC: true
```

---

## Техника 4: Исцеляющий свет

**Имя файла:** `Tech_HealingLight_01`

```
=== Basic Info ===
techniqueId: tech_healing_light_01
nameRu: Исцеляющий свет
nameEn: Healing Light
description: Базовая техника исцеления нейтральным Ци. Восстанавливает здоровье.

=== Classification ===
techniqueType: Healing
combatSubtype: None
element: Neutral
grade: Common
techniqueLevel: 1

=== Leveling ===
minLevel: 1
maxLevel: 5
canEvolve: true

=== Costs ===
baseQiCost: 20
physicalFatigueCost: 0
mentalFatigueCost: 5
cooldown: 15

=== Capacity ===
baseCapacity: 56
isUltimate: false

=== Scaling ===
strengthScaling: 0
agilityScaling: 0
intelligenceScaling: 0.05
conductivityScaling: 0.6

=== Requirements ===
minCultivationLevel: 1
minStrength: 3
minAgility: 3
minIntelligence: 5
minConductivity: 0.5

=== Effects ===
[0] effectType: Heal, value: 56, duration: 0, chance: 100

=== Acquisition ===
sources: ["scroll", "npc"]
learnableFromScroll: true
learnableFromNPC: true
```

---

## Техника 5: Пронзание пустоты (Refined)

**Имя файла:** `Tech_VoidPierce_02`

```
=== Basic Info ===
techniqueId: tech_void_pierce_01
nameRu: Пронзание пустоты
nameEn: Void Pierce
description: Концентрированный луч пустотного Ци, пробивающий защиту противника.

=== Classification ===
techniqueType: Combat
combatSubtype: RangedBeam
element: Void
grade: Refined
techniqueLevel: 2

=== Leveling ===
minLevel: 1
maxLevel: 7
canEvolve: true

=== Costs ===
baseQiCost: 25
physicalFatigueCost: 0
mentalFatigueCost: 4
cooldown: 8

=== Capacity ===
baseCapacity: 32
isUltimate: false

=== Scaling ===
strengthScaling: 0
agilityScaling: 0.025
intelligenceScaling: 0.05
conductivityScaling: 0.5

=== Requirements ===
minCultivationLevel: 2
minStrength: 3
minAgility: 5
minIntelligence: 7
minConductivity: 0.8

=== Effects ===
[0] effectType: Damage, value: 77, duration: 0, chance: 100
[1] effectType: Special, value: 20, duration: 0, chance: 100

=== Acquisition ===
sources: ["scroll", "insight"]
learnableFromScroll: true
learnableFromNPC: false
```

---

## Техника 6: Громовой удар (Ultimate)

**Имя файла:** `Tech_ThunderStrike_03`

```
=== Basic Info ===
techniqueId: tech_thunder_strike_03
nameRu: Громовой удар
nameEn: Thunder Strike
description: Ultimate техника молнии. Мощнейший разряд, поражающий всех врагов в области.

=== Classification ===
techniqueType: Combat
combatSubtype: RangedAoe
element: Lightning
grade: Transcendent
techniqueLevel: 3

=== Leveling ===
minLevel: 2
maxLevel: 7
canEvolve: false

=== Costs ===
baseQiCost: 78
physicalFatigueCost: 15
mentalFatigueCost: 10
cooldown: 60

=== Capacity ===
baseCapacity: 32
isUltimate: true

=== Scaling ===
strengthScaling: 0.02
agilityScaling: 0.04
intelligenceScaling: 0.08
conductivityScaling: 0.5

=== Requirements ===
minCultivationLevel: 3
minStrength: 12
minAgility: 18
minIntelligence: 20
minConductivity: 1.5

=== Effects ===
[0] effectType: Damage, value: 166, duration: 0, chance: 100
[1] effectType: Debuff, value: 50, duration: 60, chance: 80
[2] effectType: Special, value: 50, duration: 0, chance: 30

=== Acquisition ===
sources: ["insight"]
learnableFromScroll: false
learnableFromNPC: false
```

---

## Сводная таблица всех техник (34 штуки)

| ID | Название | Тип | Элемент | Грейд | Уровень |
|----|----------|-----|---------|-------|---------|
| tech_fire_fist_01 | Огненный кулак | Combat | Fire | Common | 1 |
| tech_water_shield_01 | Водный щит | Defense | Water | Common | 1 |
| tech_lightning_step_01 | Молниеносный шаг | Movement | Lightning | Common | 1 |
| tech_earth_palm_01 | Ладонь земли | Combat | Earth | Common | 1 |
| tech_void_pierce_01 | Пронзание пустоты | Combat | Void | Refined | 2 |
| tech_healing_light_01 | Исцеляющий свет | Healing | Neutral | Common | 1 |
| tech_wind_blade_01 | Клинок ветра | Combat | Air | Common | 1 |
| tech_poison_mist_01 | Ядовитый туман | Poison | Poison | Common | 1 |
| tech_fire_inferno_05 | Пламенный ад | Combat | Fire | Perfect | 5 |
| tech_spirit_sense_01 | Духовное чутьё | Sensory | Neutral | Common | 1 |
| tech_cultivation_breath_01 | Дыхание духа | Cultivation | Neutral | Common | 1 |
| tech_thunder_strike_03 | Громовой удар | Combat | Lightning | Transcendent | 3 |
| tech_ice_armor_02 | Ледяная броня | Defense | Water | Refined | 2 |
| tech_stone_block_01 | Каменный блок | Defense | Earth | Common | 1 |
| tech_wind_dodge_01 | Ветряное уклонение | Defense | Air | Common | 1 |
| tech_mirror_reflect_02 | Зеркальное отражение | Defense | Void | Refined | 2 |
| tech_fire_sword_01 | Огненный меч | Combat | Fire | Common | 1 |
| tech_ice_spear_01 | Ледяное копьё | Combat | Water | Common | 1 |
| tech_void_curse_02 | Проклятие пустоты | Curse | Void | Refined | 2 |
| tech_poison_claw_01 | Ядовитый коготь | Combat | Poison | Common | 1 |
| tech_qi_infusion_01 | Насыщение Ци | Support | Neutral | Common | 1 |
| tech_earthquake_stomp_02 | Землетрясение | Combat | Earth | Refined | 2 |
| tech_chain_lightning_02 | Цепная молния | Combat | Lightning | Refined | 2 |
| tech_soul_curse_04 | Проклятие души | Curse | Void | Perfect | 4 |
| tech_divine_restoration_02 | Божественное восстановление | Healing | Neutral | Refined | 2 |
| tech_water_strike_01 | Удар потока | Combat | Water | Common | 1 |
| tech_air_barrier_01 | Воздушный барьер | Defense | Air | Common | 1 |
| tech_poison_dart_01 | Ядовитый дротик | Combat | Poison | Common | 1 |
| tech_void_walk_02 | Шаг пустоты | Movement | Void | Refined | 2 |
| tech_meditation_depth_02 | Глубокая медитация | Cultivation | Neutral | Refined | 2 |
| tech_fire_breath_01 | Огненное дыхание | Combat | Fire | Common | 1 |
| tech_earth_spear_01 | Копьё земли | Combat | Earth | Common | 1 |
| tech_lightning_whip_02 | Молниевая плеть | Combat | Lightning | Refined | 2 |
| tech_void_shield_03 | Щит пустоты | Defense | Void | Perfect | 3 |

---

## Распределение по элементам

| Элемент | Количество |
|---------|------------|
| Neutral | 6 |
| Void | 6 |
| Fire | 4 |
| Earth | 4 |
| Lightning | 4 |
| Water | 4 |
| Air | 3 |
| Poison | 3 |

---

## Важные правила

1. **Healing техники** — только Neutral элемент
2. **Cultivation техники** — только Neutral элемент
3. **Poison техники** — только Poison элемент
4. **Ultimate техники** — нельзя выучить из свитка или от NPC (только insight)
5. **Refined+ техники** — требуют minCultivationLevel >= 2

---

*Документ создан: 2026-04-01*
*Источник данных: UnityProject/Assets/Data/JSON/techniques.json*

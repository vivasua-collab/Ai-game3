# Настройка NPCPresetData

**Путь:** `Assets/Data/NPCPresets/`

**Создание:** Правый клик → Create → Cultivation → NPC Preset

---

## Структура полей (из NPCPresetData.cs)

### Basic Info
| Поле | Тип | Описание |
|------|-----|----------|
| presetId | string | Уникальный ID (npc_type_XX) |
| nameTemplate | string | Имя (или шаблон имени) |
| title | string | Титул |
| backstory | text | Предыстория |

### Category
| Поле | Тип | Описание |
|------|-----|----------|
| category | enum | Temp, Plot, Unique |
| species | SpeciesData | Вид существа |

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
| strength | int (1-100) | Сила |
| agility | int (1-100) | Ловкость |
| intelligence | int (1-100) | Интеллект |
| vitality | int (1-100) | Жизнеспособность |
| conductivity | float | Проводимость |

### Personality
| Поле | Тип | Описание |
|------|-----|----------|
| personalityTraits | List | Черты характера (traitName, intensity -10..10) |
| motivation | text | Мотивация |
| alignment | enum | LawfulGood, NeutralGood, ChaoticGood, LawfulNeutral, TrueNeutral, ChaoticNeutral, LawfulEvil, NeutralEvil, ChaoticEvil |

### Relations
| Поле | Тип | Описание |
|------|-----|----------|
| baseDisposition | int (-100..100) | Базовое отношение к игроку (⚠️ устарело, используйте Attitude) |
| factionId | string | ID фракции |
| factionRole | string | Роль во фракции |

> **⚠️ Важно (CORE-M01):** Поле `baseDisposition` устарело. В NPCState используется `Attitude` (enum) + `PersonalityTrait` (Flags). При инициализации NPC `baseDisposition` (-100..100) автоматически конвертируется через `NPCState.ValueToAttitude()`.

### Attitude (enum — заменяет Disposition)
| Значение | Диапазон | Описание |
|----------|----------|----------|
| Hatred | -100..-51 | Атака без предупреждения |
| Hostile | -50..-21 | Атака если спровоцирован |
| Unfriendly | -20..-10 | Избегание |
| Neutral | -9..9 | Безразличие |
| Friendly | 10..49 | Помощь, торговля |
| Allied | 50..79 | Лояльность |
| SwornAlly | 80..100 | Самопожертвование |

### PersonalityTrait (Flags — заменяет характер Disposition)
| Флаг | Значение | Описание |
|------|----------|----------|
| Aggressive | 1 | Склонен к атаке |
| Cautious | 2 | Избегает рисков |
| Treacherous | 4 | Может предать |
| Ambitious | 8 | Ищет власть |
| Loyal | 16 | Не предаёт никогда |
| Pacifist | 32 | Избегает боя |
| Curious | 64 | Исследует |
| Vengeful | 128 | Помнит обиды |

### Techniques
| Поле | Тип | Описание |
|------|-----|----------|
| knownTechniques | List | Известные техники (techniqueId, mastery 0-100, quickSlot -1..9) |

### Equipment
| Поле | Тип | Описание |
|------|-----|----------|
| equipment | List | Экипировка (slot, itemId, grade, durabilityPercent) |
| inventory | List | Инвентарь (itemId, quantity) |

### AI
| Поле | Тип | Описание |
|------|-----|----------|
| behaviorType | enum | Passive, Defensive, Neutral, Aggressive, Hostile, Friendly |
| aggressiveness | float (0-100) | Агрессивность |
| courage | float (0-100) | Смелость |

---

## Enums

### NPCCategory
| Значение | Описание |
|----------|----------|
| Temp | Временный (генерируется случайно) |
| Plot | Сюжетный (важен для истории) |
| Unique | Уникальный (именованный персонаж) |

### BehaviorType
| Значение | Описание |
|----------|----------|
| Passive | Пассивный (не атакует) |
| Defensive | Оборонительный (атакует при угрозе) |
| Neutral | Нейтральный (реагирует на действия) |
| Aggressive | Агрессивный (атакует врагов) |
| Hostile | Враждебный (атакует игрока) |
| Friendly | Дружелюбный (помогает) |

### Alignment
| Значение | Описание |
|----------|----------|
| LawfulGood | Добропорядочный добрый |
| NeutralGood | Нейтральный добрый |
| ChaoticGood | Хаотичный добрый |
| LawfulNeutral | Добропорядочный нейтральный |
| TrueNeutral | Истинно нейтральный |
| ChaoticNeutral | Хаотичный нейтральный |
| LawfulEvil | Добропорядочный злой |
| NeutralEvil | Нейтральный злой |
| ChaoticEvil | Хаотичный злой |

---

## Пресет 1: Селянин

**Имя файла:** `NPC_Villager_01`

```
=== Basic Info ===
presetId: npc_villager_01
nameTemplate: Селянин
title: Житель деревни
backstory: Обычный житель деревни. Занимается сельским хозяйством и простой работой.

=== Category ===
category: Temp
species: human

=== Cultivation ===
cultivationLevel: 1
cultivationSubLevel: 0
coreCapacity: 500
qiPercentage: 50

=== Stats ===
strength: 8
agility: 7
intelligence: 6
vitality: 8
conductivity: 0.5

=== Personality ===
personalityTraits:
  - traitName: friendly, intensity: 3
  - traitName: honest, intensity: 4
motivation: Жить спокойной жизнью, растить семью
alignment: NeutralGood

=== Relations ===
baseDisposition: 10
factionId: null
factionRole: null

=== Techniques ===
knownTechniques: (пусто)

=== Equipment ===
equipment: (пусто)
inventory: (пусто)

=== AI ===
behaviorType: Passive
aggressiveness: 10
courage: 30
```

---

## Пресет 2: Стражник

**Имя файла:** `NPC_Guard_01`

```
=== Basic Info ===
presetId: npc_guard_01
nameTemplate: Стражник
title: Охранник
backstory: Воин, охраняющий поселение. Прошёл базовую военную подготовку.

=== Category ===
category: Plot
species: human

=== Cultivation ===
cultivationLevel: 2
cultivationSubLevel: 3
coreCapacity: 1500
qiPercentage: 80

=== Stats ===
strength: 12
agility: 10
intelligence: 7
vitality: 12
conductivity: 0.8

=== Personality ===
personalityTraits:
  - traitName: disciplined, intensity: 5
  - traitName: suspicious, intensity: 2
motivation: Защищать порядок и закон
alignment: LawfulNeutral

=== Relations ===
baseDisposition: 0
factionId: faction_guard
factionRole: guard

=== Techniques ===
knownTechniques:
  - techniqueId: tech_earth_palm_01, mastery: 25, quickSlot: 0

=== Equipment ===
equipment:
  - slot: RightHand, itemId: weapon_iron_sword_01, grade: Common, durabilityPercent: 90
  - slot: Torso, itemId: armor_leather_vest_02, grade: Common, durabilityPercent: 85
inventory:
  - itemId: item_bread, quantity: 2

=== AI ===
behaviorType: Defensive
aggressiveness: 40
courage: 70
```

---

## Пресет 3: Старейшина секты (Unique)

**Имя файла:** `NPC_SectElder_01`

```
=== Basic Info ===
presetId: npc_sect_elder_01
nameTemplate: Старейшина
title: Старейшина секты
backstory: Опытный культиватор, достигший высокого уровня. Преподаёт знания младшим ученикам.

=== Category ===
category: Unique
species: human

=== Cultivation ===
cultivationLevel: 5
cultivationSubLevel: 7
coreCapacity: 50000
qiPercentage: 95

=== Stats ===
strength: 25
agility: 22
intelligence: 35
vitality: 28
conductivity: 3.5

=== Personality ===
personalityTraits:
  - traitName: wise, intensity: 8
  - traitName: patient, intensity: 7
  - traitName: secretive, intensity: 5
motivation: Развитие секты, передача знаний
alignment: LawfulGood

=== Relations ===
baseDisposition: 50
factionId: faction_sect
factionRole: elder

=== Techniques ===
knownTechniques:
  - techniqueId: tech_cultivation_breath_01, mastery: 100, quickSlot: -1
  - techniqueId: tech_fire_inferno_05, mastery: 75, quickSlot: 2
  - techniqueId: tech_healing_light_01, mastery: 100, quickSlot: 3

=== Equipment ===
equipment:
  - slot: Torso, itemId: armor_spirit_robe_05, grade: Perfect, durabilityPercent: 100
  - slot: RingLeft1, itemId: acc_jade_pendant_03, grade: Refined, durabilityPercent: 100
inventory:
  - itemId: item_spirit_stone, quantity: 10

=== AI ===
behaviorType: Friendly
aggressiveness: 10
courage: 85
```

---

## Сводная таблица всех пресетов (15 штук)

| ID | Название | Категория | Уровень | Вид | Поведение |
|----|----------|-----------|---------|-----|-----------|
| npc_villager_01 | Селянин | Temp | L1.0 | human | Passive |
| npc_guard_01 | Стражник | Plot | L2.3 | human | Defensive |
| npc_cultivator_01 | Практик | Plot | L2.5 | human | Neutral |
| npc_bandit_01 | Разбойник | Temp | L1.7 | human | Hostile |
| npc_wolf_01 | Волк | Temp | L1.0 | wolf | Aggressive |
| npc_spirit_tiger_01 | Духовный тигр | Temp | L3.2 | tiger | Aggressive |
| npc_merchant_01 | Торговец | Unique | L1.5 | human | Passive |
| npc_sect_elder_01 | Старейшина | Unique | L5.7 | human | Friendly |
| npc_disciple_01 | Ученик | Plot | L2.0 | human | Neutral |
| npc_innkeeper_01 | Трактирщик | Unique | L1.8 | human | Friendly |
| npc_blacksmith_01 | Кузнец | Unique | L2.4 | human | Neutral |
| npc_alchemist_01 | Алхимик | Unique | L3.1 | human | Passive |
| npc_hermit_01 | Отшельник | Unique | L4.5 | human | Neutral |
| npc_traveling_monk_01 | Странствующий монах | Temp | L2.8 | human | Friendly |
| npc_beast_tamer_01 | Укротитель | Plot | L3.4 | human | Defensive |

---

## Распределение по категориям

| Категория | Количество |
|-----------|------------|
| Temp | 5 |
| Plot | 4 |
| Unique | 6 |

---

## Распределение по поведению

| Поведение | Количество |
|-----------|------------|
| Passive | 4 |
| Defensive | 2 |
| Neutral | 4 |
| Aggressive | 2 |
| Hostile | 1 |
| Friendly | 2 |

---

## Важные правила

1. **Temp NPC** — генерируются случайно, имеют nameTemplate
2. **Unique NPC** — именованные персонажи с уникальной историей
3. **coreCapacity** — должна соответствовать уровню культивации
4. **beast/tiger/wolf** — не имеют экипировки (equipment: пусто)
5. **Hostile** — автоматически враждебны к игроку
6. **baseDisposition** — устарело (CORE-M01). Используйте Attitude + PersonalityTrait. Значение -100..100 автоматически конвертируется через `NPCState.ValueToAttitude()`

---

*Документ создан: 2026-04-01*
*Обновлено: 2026-04-11 15:43:14 UTC — добавлены Attitude+PersonalityTrait (CORE-M01), baseDisposition отмечен как устаревший*
*Источник данных: UnityProject/Assets/Data/JSON/npc_presets.json*

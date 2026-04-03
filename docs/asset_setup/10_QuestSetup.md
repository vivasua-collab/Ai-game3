# Настройка Quest Data

**Путь:** `Assets/Data/Quests/`

**Примечание:** Квесты не используют ScriptableObject, а загружаются из JSON данных.

---

## Структура JSON данных (из quests.json)

### Basic Info
| Поле | Тип | Описание |
|------|-----|----------|
| questId | string | Уникальный ID (main_XXX, side_XXX, daily_XXX, cultivation_XXX) |
| nameRu | string | Название на русском |
| nameEn | string | Название на английском |
| description | text | Описание квеста |

### Classification
| Поле | Тип | Описание |
|------|-----|----------|
| category | enum | Main, Side, Daily, Cultivation |
| questType | enum | kill, collect, deliver, escort, explore, defeat, cultivation |
| difficulty | enum | Easy, Medium, Hard, Legendary |

### Requirements
| Поле | Тип | Описание |
|------|-----|----------|
| requiredLevel | int | Минимальный уровень культивации |
| prerequisites | List | ID предшествующих квестов |
| timeLimit | int | Лимит времени (0 = без лимита) |

### Objectives
| Поле | Тип | Описание |
|------|-----|----------|
| type | enum | kill, collect, deliver, escort, explore, defeat, cultivation, social |
| targetId | string | ID цели |
| description | string | Описание цели |
| required | int | Требуемое количество |
| current | int | Текущий прогресс |

### Rewards
| Поле | Тип | Описание |
|------|-----|----------|
| experience | int | Опыт |
| spiritStones | int | Духовные камни |
| items | List | Предметы (itemId, quantity) |
| techniques | List | Техники (techniqueId, level) |
| factionRep | List | Репутация (factionId, value) |

### NPCs
| Поле | Тип | Описание |
|------|-----|----------|
| giverNpcId | string | ID выдающего NPC |
| turnInNpcId | string | ID принимающего NPC |
| isRepeatable | bool | Повторяемый квест |

---

## Enums

### QuestCategory
| Категория | Описание |
|-----------|----------|
| Main | Основной сюжет |
| Side | Побочные квесты |
| Daily | Ежедневные задания |
| Cultivation | Квесты культивации |

### QuestType
| Тип | Описание |
|-----|----------|
| kill | Убийство монстров |
| collect | Сбор предметов |
| deliver | Доставка |
| escort | Сопровождение |
| explore | Исследование |
| defeat | Победа над боссом |
| cultivation | Достижение уровня |

### Difficulty
| Сложность | Описание |
|-----------|----------|
| Easy | Для начинающих |
| Medium | Средняя сложность |
| Hard | Требует подготовки |
| Legendary | Эндгейм контент |

---

## Квест 1: Первые шаги (Main)

```
=== Basic Info ===
questId: main_001
nameRu: Первые шаги
nameEn: First Steps
description: Старейшина секты поручил вам доказать свою ценность. Охотьтесь на волков, тревожащих внешних учеников у горной тропы.

=== Classification ===
category: Main
questType: kill
difficulty: Easy

=== Requirements ===
requiredLevel: 1
prerequisites: (пусто)
timeLimit: 0

=== Objectives ===
[0] type: kill, targetId: enemy_wolf, description: Убейте волков у горной тропы, required: 5, current: 0

=== Rewards ===
experience: 100
spiritStones: 50
items:
  - itemId: item_basic_sword, quantity: 1
  - itemId: item_healing_pill, quantity: 3
techniques: (пусто)
factionRep:
  - factionId: faction_cloud_sect, value: 10

=== NPCs ===
giverNpcId: npc_elder_chen
turnInNpcId: npc_elder_chen
isRepeatable: false
```

---

## Квест 2: Посвящение в секту (Main)

```
=== Basic Info ===
questId: main_002
nameRu: Посвящение в секту
nameEn: Sect Initiation
description: Чтобы стать истинным учеником Облачной секты, вы должны достичь второго уровня культивации и предстать перед Мастером секты.

=== Classification ===
category: Main
questType: cultivation
difficulty: Medium

=== Requirements ===
requiredLevel: 1
prerequisites: ["main_001"]
timeLimit: 0

=== Objectives ===
[0] type: cultivation, targetId: cultivation_level, description: Достигните 2 уровня культивации, required: 2, current: 1
[1] type: social, targetId: npc_sect_master, description: Поговорите с Мастером секты, required: 1, current: 0

=== Rewards ===
experience: 500
spiritStones: 200
items:
  - itemId: item_disciple_robe, quantity: 1
  - itemId: item_spirit_gathering_pill, quantity: 5
techniques:
  - techniqueId: tech_basic_breathing, level: 1
factionRep:
  - factionId: faction_cloud_sect, value: 50

=== NPCs ===
giverNpcId: npc_elder_chen
turnInNpcId: npc_sect_master
isRepeatable: false
```

---

## Квест 3: Проклятые земли (Main - Hard)

```
=== Basic Info ===
questId: main_003
nameRu: Проклятые земли
nameEn: Cursed Lands
description: Древнее зло пробуждается в Проклятой долине. Расследуйте источник порчи и положите конец угрозе.

=== Classification ===
category: Main
questType: explore
difficulty: Hard

=== Requirements ===
requiredLevel: 3
prerequisites: ["main_002"]
timeLimit: 0

=== Objectives ===
[0] type: explore, targetId: location_cursed_valley_entrance, description: Найдите вход в Проклятую долину, required: 1, current: 0
[1] type: collect, targetId: item_corrupted_core, description: Соберите порченные ядра с нежити, required: 3, current: 0
[2] type: defeat, targetId: boss_corrupted_guardian, description: Победите Порченного стража, required: 1, current: 0

=== Rewards ===
experience: 1500
spiritStones: 500
items:
  - itemId: item_cleansing_talisman, quantity: 3
  - itemId: item_mid_grade_spirit_stone, quantity: 2
techniques:
  - techniqueId: tech_purification_arts, level: 1
factionRep:
  - factionId: faction_cloud_sect, value: 100
  - factionId: faction_righteous_path, value: 25

=== NPCs ===
giverNpcId: npc_sect_master
turnInNpcId: npc_sect_master
isRepeatable: false
```

---

## Квест 4: Сердце секты (Main - Legendary)

```
=== Basic Info ===
questId: main_004
nameRu: Сердце секты
nameEn: Heart of the Sect
description: Мастер секты раскрыл страшную угрозу. Камень сердца, защищающий Облачную секту, угасает. Добудьте духовную эссенцию в Древней пещере духов.

=== Classification ===
category: Main
questType: defeat
difficulty: Legendary

=== Requirements ===
requiredLevel: 5
prerequisites: ["main_003"]
timeLimit: 0

=== Objectives ===
[0] type: explore, targetId: location_ancient_spirit_cave, description: Войдите в Древнюю пещеру духов, required: 1, current: 0
[1] type: collect, targetId: item_spirit_essence, description: Добудьте духовную эссенцию, required: 1, current: 0
[2] type: defeat, targetId: boss_spirit_guardian, description: Победите Древнего стража духов, required: 1, current: 0

=== Rewards ===
experience: 5000
spiritStones: 2000
items:
  - itemId: item_elder_token, quantity: 1
  - itemId: item_high_grade_spirit_stone, quantity: 3
  - itemId: item_breakthrough_pill, quantity: 1
techniques:
  - techniqueId: tech_cloud_step, level: 1
  - techniqueId: tech_spirit_sensing, level: 1
factionRep:
  - factionId: faction_cloud_sect, value: 500

=== NPCs ===
giverNpcId: npc_sect_master
turnInNpcId: npc_sect_master
isRepeatable: false
```

---

## Квест 5: Сбор трав (Side - Repeatable)

```
=== Basic Info ===
questId: side_001
nameRu: Сбор трав
nameEn: Herb Gathering
description: В зале лекарств заканчивается духовная трава. Старейшина Лин просит собрать свежие травы в Туманном лесу.

=== Classification ===
category: Side
questType: collect
difficulty: Easy

=== Requirements ===
requiredLevel: 1
prerequisites: (пусто)
timeLimit: 0

=== Objectives ===
[0] type: collect, targetId: item_spirit_grass, description: Соберите духовную траву, required: 10, current: 0

=== Rewards ===
experience: 80
spiritStones: 30
items:
  - itemId: item_healing_pill, quantity: 5
techniques: (пусто)
factionRep:
  - factionId: faction_cloud_sect, value: 5

=== NPCs ===
giverNpcId: npc_elder_lin
turnInNpcId: npc_elder_lin
isRepeatable: true
```

---

## Квест 6: Ежедневная медитация (Daily)

```
=== Basic Info ===
questId: daily_001
nameRu: Ежедневная медитация
nameEn: Daily Meditation
description: Культиватор должен поддерживать дисциплину. Проведите время в медитации у Духовного источника секты.

=== Classification ===
category: Daily
questType: cultivation
difficulty: Easy

=== Requirements ===
requiredLevel: 1
prerequisites: (пусто)
timeLimit: 0

=== Objectives ===
[0] type: cultivation, targetId: action_meditate, description: Медитируйте у Духовного источника, required: 60, current: 0

=== Rewards ===
experience: 50
spiritStones: 20
items:
  - itemId: item_spirit_gathering_pill, quantity: 1
techniques: (пусто)
factionRep:
  - factionId: faction_cloud_sect, value: 5

=== NPCs ===
giverNpcId: npc_disciple_hall
turnInNpcId: npc_disciple_hall
isRepeatable: true
```

---

## Квест 7: Испытание прорыва (Cultivation)

```
=== Basic Info ===
questId: cultivation_001
nameRu: Испытание прорыва
nameEn: Breakthrough Trial
description: Путь культивации требует преодоления внутренних демонов. Успешно прорвитесь на третий уровень культивации.

=== Classification ===
category: Cultivation
questType: cultivation
difficulty: Medium

=== Requirements ===
requiredLevel: 2
prerequisites: ["main_002"]
timeLimit: 0

=== Objectives ===
[0] type: cultivation, targetId: cultivation_level, description: Достигните 3 уровня культивации, required: 3, current: 2
[1] type: cultivation, targetId: action_breakthrough_success, description: Успешно завершите прорыв, required: 1, current: 0

=== Rewards ===
experience: 1000
spiritStones: 300
items:
  - itemId: item_meridian_cleansing_pill, quantity: 1
techniques:
  - techniqueId: tech_inner_vision, level: 1
factionRep:
  - factionId: faction_cloud_sect, value: 75

=== NPCs ===
giverNpcId: npc_cultivation_master
turnInNpcId: npc_cultivation_master
isRepeatable: false
```

---

## Сводная таблица квестов (15 штук)

### Main Quests (4)
| ID | Название | Сложность | Уровень | Предшественник |
|----|----------|-----------|---------|----------------|
| main_001 | Первые шаги | Easy | 1 | - |
| main_002 | Посвящение в секту | Medium | 1 | main_001 |
| main_003 | Проклятые земли | Hard | 3 | main_002 |
| main_004 | Сердце секты | Legendary | 5 | main_003 |

### Side Quests (5)
| ID | Название | Сложность | Уровень | Повторяемый |
|----|----------|-----------|---------|-------------|
| side_001 | Сбор трав | Easy | 1 | ✓ |
| side_002 | Проблема с бандитами | Hard | 3 | ✗ |
| side_003 | Угроза духовного зверя | Hard | 4 | ✗ |
| side_004 | Доставка лекарств | Medium | 2 | ✗ |
| side_005 | Сопровождение купца | Medium | 2 | ✓ |

### Daily Quests (3)
| ID | Название | Сложность | Уровень |
|----|----------|-----------|---------|
| daily_001 | Ежедневная медитация | Easy | 1 |
| daily_002 | Патруль секты | Easy | 2 |
| daily_003 | Сбор ресурсов | Easy | 1 |

### Cultivation Quests (3)
| ID | Название | Сложность | Уровень | Предшественник |
|----|----------|-----------|---------|----------------|
| cultivation_001 | Испытание прорыва | Medium | 2 | main_002 |
| cultivation_002 | Древо души | Hard | 4 | cultivation_001 |
| cultivation_003 | Истинное пробуждение | Legendary | 5 | cultivation_002 |

---

## Важные правила

1. **prerequisites** — все ID в списке должны быть завершены
2. **timeLimit** — 0 = без лимита, иначе время в минутах
3. **isRepeatable** — Daily квесты всегда повторяемые
4. **difficulty** определяет награды и сложность врагов
5. **factionRep** — может быть несколько фракций

---

*Документ создан: 2026-04-01*
*Источник данных: UnityProject/Assets/Data/JSON/quests.json*

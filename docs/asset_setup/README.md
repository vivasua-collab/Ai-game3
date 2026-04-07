# 📁 Asset Setup — Инструкции по настройке

Эта папка содержит детальные инструкции по заполнению ScriptableObject assets в Unity Editor.

---

## 📋 Содержание

| Файл | Описание | Количество assets |
|------|----------|-------------------|
| [01_CultivationLevelData.md](./01_CultivationLevelData.md) | Уровни культивации (1-10) | 10 файлов |
| [02_MortalStageData.md](./02_MortalStageData.md) | Этапы развития смертного (0) | 6 файлов |
| [03_ElementData.md](./03_ElementData.md) | Элементы (стихии) | 7 файлов |
| [04_BasicScene.md](./04_BasicScene.md) | Базовая сцена (ручная) | 1 сцена |
| [04_BasicScene_SemiAuto.md](./04_BasicScene_SemiAuto.md) | Базовая сцена (полуавтомат) | 1 сцена |
| [05_PlayerSetup.md](./05_PlayerSetup.md) | Настройка Player (ручная) | 1 префаб |
| [05_PlayerSetup_SemiAuto.md](./05_PlayerSetup_SemiAuto.md) | Настройка Player (полуавтомат) | 1 префаб |
| [06_TechniqueData.md](./06_TechniqueData.md) | Техники культивации | 34 файла |
| [06_TechniqueData_SemiAuto.md](./06_TechniqueData_SemiAuto.md) | Техники (полуавтомат) | 34 файла |
| [07_NPCPresetData.md](./07_NPCPresetData.md) | Пресеты NPC | 15 файлов |
| [07_NPCPresetData_SemiAuto.md](./07_NPCPresetData_SemiAuto.md) | NPC пресеты (полуавтомат) | 15 файлов |
| [08_EquipmentData.md](./08_EquipmentData.md) | Экипировка (оружие/броня) | 39 файлов |
| [08_EquipmentData_SemiAuto.md](./08_EquipmentData_SemiAuto.md) | Экипировка (полуавтомат) | 39 файлов |
| [09_EnemySetup.md](./09_EnemySetup.md) | Настройка врагов | 27 типов |
| [09_EnemySetup_SemiAuto.md](./09_EnemySetup_SemiAuto.md) | Враги (полуавтомат) | 27 типов |
| [10_QuestSetup.md](./10_QuestSetup.md) | Настройка квестов | 15 квестов |
| [10_QuestSetup_SemiAuto.md](./10_QuestSetup_SemiAuto.md) | Квесты (полуавтомат) | 15 квестов |
| [11_ItemData.md](./11_ItemData.md) | Расходники и свитки | 8 предметов |
| [11_ItemData_SemiAuto.md](./11_ItemData_SemiAuto.md) | Расходники (полуавтомат) | 8 предметов |
| [12_MaterialData.md](./12_MaterialData.md) | Материалы для крафта | 17 материалов |
| [12_MaterialData_SemiAuto.md](./12_MaterialData_SemiAuto.md) | Материалы (полуавтомат) | 17 материалов |
| [13_SpriteSetup.md](./13_SpriteSetup.md) | **Настройка спрайтов (полная)** | 57 спрайтов |
| [13_SpriteSetup_QuickStart.md](./13_SpriteSetup_QuickStart.md) | **Настройка спрайтов (быстрая)** | 57 спрайтов |
| [14_FormationData.md](./14_FormationData.md) | **Формации (ручная)** | 24+ формаций |
| [15_FormationCoreData.md](./15_FormationCoreData.md) | **Ядра формаций (ручная)** | 30+ ядер |
| [16_TileSystem_SemiAuto.md](./16_TileSystem_SemiAuto.md) | **Тайловая система (полуавтомат)** | 13 тайлов + сцена |

---

## 🤖 Инструменты автоматизации

### 1. Asset Generator (базовый)

**Меню:** `Tools → Generate Assets → ...`

**Скрипт:** `Assets/Scripts/Editor/AssetGenerator.cs`

**Функции:**

| Кнопка | Создаёт | Количество |
|--------|---------|------------|
| Generate Cultivation Levels | CultivationLevelData | 10 файлов |
| Generate Mortal Stages | MortalStageData | 6 файлов |
| Generate Elements | ElementData | 7 файлов |
| Clear All Generated | Очистка | — |

---

### 2. Asset Generator Extended

**Меню:** `Tools → Generate Assets → ...`

**Скрипт:** `Assets/Scripts/Editor/AssetGeneratorExtended.cs`

**Функции:**

| Кнопка | Создаёт | Количество |
|--------|---------|------------|
| All Extended Assets | Все расширенные | 122 файла |
| Techniques | TechniqueData | 34 файла |
| NPC Presets | NPCPresetData | 15 файлов |
| Equipment | EquipmentData | 39 файлов |
| Items | ItemData | 8 файлов |
| Materials | MaterialData | 17 файлов |
| Clear Extended Assets | Очистка | — |

**Результат:**
```
Assets/Data/
├── CultivationLevels/
│   ├── L1_AwakenedCore.asset
│   ├── L2_LifeFlow.asset
│   └── ... (10 файлов)
├── MortalStages/
│   ├── MS1_Newborn.asset
│   ├── MS2_Child.asset
│   └── ... (6 файлов)
└── Elements/
    ├── E_Neutral.asset
    ├── E_Fire.asset
    └── ... (7 файлов)
```

**Использование:**
1. Открой `Window → Asset Generator`
2. Нажми нужную кнопку
3. Assets создадутся автоматически в правильные папки

---

### 3. Formation Asset Generator

**Меню:** `Tools → Generate Assets → Formation Assets`

**Скрипт:** `Assets/Scripts/Editor/FormationAssetGenerator.cs`

**Функции:**

| Кнопка | Создаёт | Количество |
|--------|---------|------------|
| Formation Assets (All) | Все формации и ядра | 54 файла |
| Formation Data (24) | FormationData | 24 файла |
| Formation Core Data (30) | FormationCoreData | 30 файлов |
| Validate Formation Assets | Валидация | — |
| Clear Formation Assets | Очистка | — |

**UI Prefabs:** `Tools → Formation UI → Generate UI Prefabs`

| Префаб | Описание |
|--------|----------|
| FormationListItem | Элемент списка формаций |
| ActiveFormationItem | Элемент активной формации |
| PlacementPreview | Превью размещения |

---

### 4. Scene Setup Tools

**Меню:** `Window → Scene Setup Tools`

**Скрипт:** `Assets/Scripts/Editor/SceneSetupTools.cs`

**Функции:**

| Кнопка | Действие |
|--------|----------|
| Create GameManager & Systems | Создаёт GameManager с 7 контроллерами |
| Setup TimeController Settings | Настраивает время |
| Setup SaveManager Settings | Настраивает сохранения |
| Create Player GameObject | Создаёт Player с 9 компонентами |
| Create GameUI Canvas | Создаёт Canvas с EventSystem |
| Create HUD Panel | Создаёт HUD с текстами |
| **SETUP ALL (Full Scene)** | Всё сразу одной кнопкой |

---

### 5. Tile System Tools

**Меню:** `Tools → ...`

**Скрипты:** `Assets/Scripts/Tile/Editor/`

**Функции:**

| Меню | Действие | Создаёт |
|------|----------|---------|
| Generate Tile Sprites | Генерация спрайтов | 13 PNG файлов |
| Setup Test Location Scene | Создание сцены | 1 сцена + Grid + Tilemaps |

**Результат:**
```
Assets/Sprites/Tiles/
├── terrain_grass.png
├── terrain_dirt.png
├── terrain_stone.png
├── terrain_water_shallow.png
├── terrain_water_deep.png
├── terrain_sand.png
├── terrain_snow.png
├── terrain_void.png
├── obj_tree.png
├── obj_rock_small.png
├── obj_rock_medium.png
├── obj_bush.png
└── obj_chest.png

Assets/Scenes/
└── TestLocation.unity
    ├── Grid (cellSize: 2×2×1)
    │   ├── Terrain (Tilemap)
    │   └── Objects (Tilemap)
    └── TileMapController
```

**Что автоматизируется:**

| Компонент | Автоматически | Вручную |
|-----------|---------------|---------|
| GameManager + Systems | ✅ Создание объектов | — |
| Все контроллеры | ✅ Добавление компонентов | — |
| Настройка полей | ✅ Заполнение Inspector | — |
| Player + 9 компонентов | ✅ Создание и настройка | — |
| Canvas + HUD | ✅ Создание | — |
| Папки | — | ✋ Project → Create → Folder |
| Файл сцены (.unity) | — | ✋ Project → Create → Scene |
| Project Settings | — | ✋ Теги, слои, Input |
| Префаб Player | — | ✋ Drag to Prefabs/ |
| Sprite / Animator | — | ✋ Добавить визуал |

---

## 🗂️ Структура папок

```
Assets/
├── Data/
│   ├── CultivationLevels/     ← CultivationLevelData (10 файлов)
│   ├── MortalStages/          ← MortalStageData (6 файлов)
│   ├── Elements/              ← ElementData (7 файлов)
│   ├── Techniques/            ← TechniqueData (34 файла)
│   ├── NPCPresets/            ← NPCPresetData (15 файлов)
│   ├── Materials/             ← MaterialData (17 материалов)
│   ├── Items/                 ← ItemData (8 предметов)
│   ├── Species/               ← SpeciesData — требуется создать
│   └── Equipment/             ← EquipmentData — требуется создать
├── Prefabs/
│   └── Player/
│       └── Player.prefab      ← Создать из Hierarchy
└── Scenes/
    └── Main.unity             ← Создать вручную
```

---

## 🚀 Быстрый старт (рекомендуемый порядок)

### Шаг 1: Создать папки (вручную)

В Project окне:
```
Assets → Create → Folder → "Scenes"
Assets → Create → Folder → "Prefabs"
Assets → Create → Folder → "Data"
```

### Шаг 2: Создать assets данных (автоматически)

1. `Window → Asset Generator`
2. Нажать по очереди:
   - Generate Cultivation Levels
   - Generate Mortal Stages
   - Generate Elements

### Шаг 3: Создать сцену (вручную)

1. `Assets/Scenes → Create → Scene`
2. Назови `Main`
3. Открой сцену

### Шаг 4: Настроить сцену (автоматически)

1. `Window → Scene Setup Tools`
2. Нажми **SETUP ALL (Full Scene)**
3. Дождись сообщений в Console

### Шаг 5: Настроить Project Settings (вручную)

**Edit → Project Settings → Tags and Layers**

Теги:
```
Player, NPC, Interactable, Item, Enemy
```

Слои (User Layer 6+):
```
Player, NPC, Interactable, Item, Enemy, UI, Background
```

### Шаг 6: Создать префаб (вручную)

1. Создай папку `Assets/Prefabs/Player/`
2. Перетащи `Player` из Hierarchy в папку
3. Выбери "Original Prefab"

### Шаг 7: Добавить визуал (вручную)

1. Выдели Player в Hierarchy
2. Add Component → Sprite Renderer
3. Назначь спрайт

---

## 📝 Ручной режим (альтернатива)

Если не используешь автоматизацию:

1. Открой соответствующий .md файл
2. В Unity создай новый asset (Create → Cultivation → ...)
3. Скопируй данные из таблицы в поля Inspector
4. Сохрани asset

Для сцены и игрока используй:
- [04_BasicScene.md](./04_BasicScene.md) — полная ручная настройка
- [05_PlayerSetup.md](./05_PlayerSetup.md) — полная ручная настройка

---

## ⚠️ Важно

- Все числовые значения указаны точно
- Checkbox (✓/✗) = true/false
- Длинные числа (1,000,000) вводи без запятых (1000000)
- Инструкции *_SemiAuto.md описывают ручные шаги после автоматизации

---

## 🔧 Компоненты GameManager

GameManager содержит следующие контроллеры:

| Компонент | Поля конфигурации |
|-----------|-------------------|
| WorldController | worldName, worldSeed |
| TimeController | currentTimeSpeed, autoAdvance, normalSpeedRatio, fastSpeedRatio, veryFastSpeedRatio, daysPerMonth, monthsPerYear |
| LocationController | baseTravelSpeed, travelDangerBaseChance |
| EventController | eventCheckInterval, baseEventChance, maxActiveEvents |
| FactionController | relationDecayRate, warThreshold, allianceThreshold |
| SaveManager | saveFolder, autoSave, autoSaveInterval, maxSlots |

---

## 🔧 Компоненты Player

Player содержит следующие контроллеры:

| Компонент | Поля конфигурации |
|-----------|-------------------|
| PlayerController | playerId, playerName, moveSpeed, runSpeedMultiplier |
| BodyController | bodyMaterial, vitality, cultivationLevel |
| QiController | cultivationLevel, cultivationSubLevel, coreQuality, currentQi |
| InventoryController | gridWidth, gridHeight, maxWeight |
| EquipmentController | useLayerSystem, maxLayersPerSlot |
| TechniqueController | maxQuickSlots, maxUltimates |
| SleepSystem | minSleepHours, maxSleepHours, optimalSleepHours |

---

## 📚 Ключевые формулы

### Ёмкость ядра
```
coreCapacity = 1000 × qualityMultiplier × 1.1^totalSubLevels
```
Где `totalSubLevels = (level-1) × 10 + subLevel`

### Плотность Ци
```
qiDensity = 2^(level - 1)
```

### Динамический расчёт прорыва
```
qiForSubLevelBreakthrough = coreCapacity × 10
qiForLevelBreakthrough = coreCapacity × 100
```

---

## 🎮 Enums проекта

### CultivationLevel (1-10)
| Level | Name |
|-------|------|
| 1 | AwakenedCore |
| 2 | LifeFlow |
| 3 | InternalFire |
| 4 | BodySpiritUnion |
| 5 | HeartOfHeaven |
| 6 | VeilBreaker |
| 7 | EternalRing |
| 8 | VoiceOfHeaven |
| 9 | ImmortalCore |
| 10 | Ascension |

### Element (0-7)
| Index | Element |
|-------|---------|
| 0 | Neutral |
| 1 | Fire |
| 2 | Water |
| 3 | Earth |
| 4 | Air |
| 5 | Lightning |
| 6 | Void |
| 7 | Poison |

### CoreQuality (1-7)
| Quality | Multiplier |
|---------|------------|
| Fragmented | ×0.5 |
| Cracked | ×0.7 |
| Flawed | ×0.85 |
| Normal | ×1.0 |
| Refined | ×1.2 |
| Perfect | ×1.5 |
| Transcendent | ×2.0 |

---

*Папка создана: 2026-03-30*
*Обновлено: 2026-04-07 — добавлена инструкция по тайловой системе (16_TileSystem_SemiAuto.md), полный комплект: 28 файлов документации*

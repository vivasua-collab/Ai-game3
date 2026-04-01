# 📋 План внедрения: Unity Assets и интеграция

**Версия:** 1.0  
**Дата:** 2026-03-31  
**Цель:** Создать .asset файлы и базовую сцену для тестирования

---

## 📊 Текущее состояние

### ✅ Создано

| Компонент | Статус |
|-----------|--------|
| ScriptableObject классы (7 файлов) | ✅ |
| JSON конфигурации (5 файлов) | ✅ |
| Контроллеры (43 файла) | ✅ |
| Документация (50+ файлов) | ✅ |

### ❌ Отсутствует

| Компонент | Статус |
|-----------|--------|
| .asset файлы (CultivationLevelData) | ❌ 0/10 |
| .asset файлы (ElementData) | ❌ 0/7 |
| .asset файлы (MortalStageData) | ❌ 0/6 |
| .asset файлы (SpeciesData) | ❌ 0/5 |
| .asset файлы (TechniqueData) | ❌ 0/~20 |
| Сцена Main.unity | ❌ |
| Префаб Player.prefab | ❌ |

---

## 🎯 Этап 1: Создание .asset файлов

### 1.1 CultivationLevelData (10 уровней)

**Путь:** `Assets/Data/CultivationLevels/`

**Файлы:**
1. `Level1_AwakenedCore.asset`
2. `Level2_LifeFlow.asset`
3. `Level3_InternalFire.asset`
4. `Level4_BodySpiritUnion.asset`
5. `Level5_HeartOfHeaven.asset`
6. `Level6_VeilBreaker.asset`
7. `Level7_EternalRing.asset`
8. `Level8_VoiceOfHeaven.asset`
9. `Level9_ImmortalCore.asset`
10. `Level10_Ascension.asset`

**Данные из:** `docs/asset_setup/01_CultivationLevelData.md`

### 1.2 MortalStageData (6 этапов)

**Путь:** `Assets/Data/MortalStages/`

**Файлы:**
1. `Stage0_Newborn.asset`
2. `Stage1_Child.asset`
3. `Stage2_Adult.asset`
4. `Stage3_Mature.asset`
5. `Stage4_Elder.asset`
6. `Stage9_Awakening.asset`

**Данные из:** `docs/asset_setup/02_MortalStageData.md`

### 1.3 ElementData (7 элементов)

**Путь:** `Assets/Data/Elements/`

**Файлы:**
1. `Element_Neutral.asset`
2. `Element_Fire.asset`
3. `Element_Water.asset`
4. `Element_Earth.asset`
5. `Element_Air.asset`
6. `Element_Lightning.asset`
7. `Element_Void.asset`

**Данные из:** `docs/asset_setup/03_ElementData.md`

### 1.4 SpeciesData (5 базовых видов)

**Путь:** `Assets/Data/Species/`

**Файлы:**
1. `Species_Human.asset` — базовый гуманоид
2. `Species_Elf.asset` — гуманоид с бонусами
3. `Species_Wolf.asset` — четвероногое
4. `Species_Ghost.asset` — дух (аморфное)
5. `Species_Golem.asset` — конструкт (минерал)

---

## 🎯 Этап 2: Создание сцены Main

### 2.1 Структура сцены

```
Main (сцена)
├── Main Camera
├── Directional Light
├── GameManager
│   ├── WorldController
│   ├── TimeController
│   ├── LocationController
│   ├── EventController
│   ├── FactionController
│   └── SaveManager
├── Player
│   ├── PlayerController
│   ├── BodyController
│   ├── QiController
│   ├── InventoryController
│   ├── EquipmentController
│   └── TechniqueController
├── EventSystem
└── GameUI (Canvas)
    ├── HUD
    │   ├── TimeText
    │   ├── HPText
    │   └── QiText
    └── PauseMenu (позже)
```

### 2.2 Компоненты GameManager

**WorldController:**
- worldName: "Cultivation World"
- worldSeed: 12345

**TimeController:**
- currentTimeSpeed: Normal
- autoAdvance: true

**SaveManager:**
- autoSaveEnabled: true
- autoSaveInterval: 300

---

## 🎯 Этап 3: Создание префаба Player

### 3.1 Структура Player

```
Player (GameObject)
├── Transform
├── Rigidbody2D (Gravity Scale: 0, Freeze Rotation Z)
├── PlayerController
│   ├── playerId: "player"
│   ├── playerName: "Игрок"
│   └── moveSpeed: 5
├── BodyController
│   ├── bodyMaterial: Organic
│   └── vitality: 10
├── QiController
│   ├── cultivationLevel: 1
│   ├── coreQuality: Normal
│   └── currentQi: 100
├── InventoryController
│   ├── gridWidth: 8
│   └── gridHeight: 6
├── EquipmentController
│   └── useLayerSystem: true
└── TechniqueController
    └── maxTechniqueSlots: 3
```

### 3.2 Настройка для смертного

```
QiController:
- cultivationLevel: 0 (или 1)
- currentQi: 50
- enablePassiveRegen: false
```

---

## 🎯 Этап 4: Тестирование

### 4.1 Проверка компиляции

1. Открыть проект в Unity 6000.3
2. Проверить Console на ошибки
3. Устранить все ошибки перед продолжением

### 4.2 Проверка .asset файлов

1. Выбрать .asset файл в Project
2. Проверить данные в Inspector
3. Сравнить с документацией

### 4.3 Проверка сцены

1. Запустить Play Mode
2. Проверить Console: "World initialized"
3. Проверить HUD: отображение времени, HP, Ци

---

## 📊 Время выполнения

| Этап | Оценка |
|------|--------|
| Этап 1: .asset файлы | 30-60 мин |
| Этап 2: Сцена Main | 15-30 мин |
| Этап 3: Префаб Player | 10-20 мин |
| Этап 4: Тестирование | 15-30 мин |
| **Итого** | **70-140 мин** |

---

## 📁 Файловая структура после выполнения

```
UnityProject/Assets/
├── Data/
│   ├── CultivationLevels/
│   │   ├── Level1_AwakenedCore.asset
│   │   ├── Level2_LifeFlow.asset
│   │   └── ... (10 файлов)
│   ├── MortalStages/
│   │   ├── Stage0_Newborn.asset
│   │   └── ... (6 файлов)
│   ├── Elements/
│   │   ├── Element_Fire.asset
│   │   └── ... (7 файлов)
│   ├── Species/
│   │   ├── Species_Human.asset
│   │   └── ... (5 файлов)
│   └── JSON/
│       └── ... (существующие)
├── Prefabs/
│   └── Player/
│       └── Player.prefab
├── Scenes/
│   ├── SampleScene.unity
│   └── Main.unity
└── Scripts/
    └── ... (существующие 43 файла)
```

---

## ✅ Чек-лист

### Этап 1: .asset файлы
- [ ] Level1_AwakenedCore.asset
- [ ] Level2_LifeFlow.asset
- [ ] Level3_InternalFire.asset
- [ ] Level4_BodySpiritUnion.asset
- [ ] Level5_HeartOfHeaven.asset
- [ ] Level6_VeilBreaker.asset
- [ ] Level7_EternalRing.asset
- [ ] Level8_VoiceOfHeaven.asset
- [ ] Level9_ImmortalCore.asset
- [ ] Level10_Ascension.asset
- [ ] Stage0_Newborn.asset
- [ ] Stage1_Child.asset
- [ ] Stage2_Adult.asset
- [ ] Stage3_Mature.asset
- [ ] Stage4_Elder.asset
- [ ] Stage9_Awakening.asset
- [ ] Element_Neutral.asset
- [ ] Element_Fire.asset
- [ ] Element_Water.asset
- [ ] Element_Earth.asset
- [ ] Element_Air.asset
- [ ] Element_Lightning.asset
- [ ] Element_Void.asset
- [ ] Species_Human.asset

### Этап 2: Сцена
- [ ] Создать Main.unity
- [ ] Настроить GameManager
- [ ] Добавить WorldController
- [ ] Добавить TimeController
- [ ] Добавить SaveManager

### Этап 3: Player
- [ ] Создать Player GameObject
- [ ] Добавить PlayerController
- [ ] Добавить BodyController
- [ ] Добавить QiController
- [ ] Добавить InventoryController
- [ ] Создать Player.prefab

### Этап 4: UI
- [ ] Создать Canvas
- [ ] Создать HUD Panel
- [ ] Добавить TimeText
- [ ] Добавить HPText
- [ ] Добавить QiText

---

*План создан: 2026-03-31*

# Настройка базовой сцены

**Путь:** `Assets/Scenes/`

**Имя файла:** `Main.unity`

---

## Шаг 1: Создание папок

Перед созданием сцены убедись, что папки существуют:

```
Assets/
├── Scenes/
├── Prefabs/
│   └── Player/
└── Data/
    ├── JSON/
    ├── CultivationLevels/
    ├── Elements/
    └── MortalStages/
```

**Создание через Unity:**
1. В окне **Project** правый клик на `Assets`
2. **Create → Folder**
3. Назови папку

---

## Шаг 2: Создание сцены

1. В окне **Project** найди папку `Assets/Scenes/`
2. **Правый клик** в папке Scenes
3. **Create → Scene**
4. Назови файл `Main`
5. Двойной клик чтобы открыть сцену

---

## Шаг 3: Структура сцены

В окне **Hierarchy** (слева) ты увидишь:
```
Main
├── Main Camera
└── Directional Light
```

Это базовые объекты. Оставь их.

---

## Шаг 4: Создание GameManager

GameManager — главный объект, управляющий игрой.

### 4.1 Создай пустой объект:

1. В окне **Hierarchy** правый клик
2. **Create Empty**
3. Назови его `GameManager`

### 4.2 Добавь компонент:

Выдели `GameManager` в Hierarchy, затем в **Inspector** (справа):

1. Нажми кнопку **Add Component**
2. Найди `GameManager` (из Scripts/Managers)
3. Добавь его

### 4.3 Настрой GameManager:

```
GameManager:
├── === Config ===
├── initializeOnStart: ☑ true
├── debugMode: ☐ false
├──
├── === References ===
├── worldController: None        ← автоматически найдёт
├── timeController: None         ← автоматически найдёт
├── playerController: None       ← автоматически найдёт
└── uiManager: None              ← автоматически найдёт
```

**Важно:** References найдутся автоматически через `FindFirstObjectByType<>()`

---

## Шаг 5: Создание системных контроллеров

Создай пустой объект `Systems` как дочерний к `GameManager`:

1. Правый клик на `GameManager` в Hierarchy
2. **Create Empty**
3. Назови `Systems`

### 5.1 Добавь контроллеры на `Systems`:

Выдели `Systems`, затем добавь компоненты:

| Компонент | Назначение |
|-----------|------------|
| `WorldController` | Управление миром, NPC, событиями |
| `TimeController` | Игровое время, дата, скорость |
| `LocationController` | Локации и переходы |
| `EventController` | Мировые события |
| `FactionController` | Фракции и отношения |
| `GeneratorRegistry` | Генераторы NPC, техник, предметов |
| `SaveManager` | Сохранение/загрузка |

---

## Шаг 6: Настройка WorldController

```
WorldController:
├── === World Settings ===
├── worldName: "Cultivation World"
├── worldSeed: 12345
├──
├── === References ===
├── timeController: None        ← автоматически найдёт
├── locationController: None    ← автоматически найдёт
├── factionController: None     ← автоматически найдёт
├── eventController: None       ← автоматически найдёт
└── generatorRegistry: None     ← автоматически найдёт
```

---

## Шаг 7: Настройка TimeController

```
TimeController:
├── === Time Settings ===
├── currentTimeSpeed: Normal    ← выбрать из списка
├── autoAdvance: ☑ true
├──
├── === Time Ratios ===
├── normalSpeedRatio: 60        (1 сек = 1 минута)
├── fastSpeedRatio: 300         (1 сек = 5 минут)
├── veryFastSpeedRatio: 900     (1 сек = 15 минут)
├──
├── === Calendar ===
├── daysPerMonth: 30
├── monthsPerYear: 12
├── hoursPerDay: 24
└── minutesPerHour: 60
```

### TimeSpeed (выбрать один):

| Значение | Описание |
|----------|----------|
| Paused | Пауза |
| Normal | 1 сек = 1 минута |
| Fast | 1 сек = 5 минут |
| VeryFast | 1 сек = 15 минут |

---

## Шаг 8: Настройка других контроллеров

### LocationController:
```
LocationController:
├── baseTravelSpeed: 1
└── travelDangerBaseChance: 0.1
```

### EventController:
```
EventController:
├── eventCheckInterval: 60
├── baseEventChance: 0.05
├── maxActiveEvents: 5
└── eventHistorySize: 50
```

### FactionController:
```
FactionController:
├── relationDecayRate: 0.1
├── warThreshold: -50
└── allianceThreshold: 50
```

### GeneratorRegistry:
```
GeneratorRegistry:
└── (без полей в Inspector)
```

### SaveManager:
```
SaveManager:
├── saveFolder: "Saves"
├── fileExtension: ".sav"
├── useEncryption: false
├── autoSave: ☑ true
├── autoSaveInterval: 300
└── maxSlots: 5
```

---

## Шаг 9: Создание UI Canvas

### 9.1 Создай Canvas:

1. В Hierarchy правый клик
2. **UI → Canvas**
3. Назови его `GameUI`

### 9.2 Настрой Canvas:

Выдели `GameUI`, в Inspector:

```
Canvas:
├── Render Mode: Screen Space - Overlay

Canvas Scaler:
├── UI Scale Mode: Scale With Screen Size
├── Reference Resolution: X=1920, Y=1080
├── Screen Match Mode: Match Width Or Height
└── Match: 0.5
```

### 9.3 EventSystem:

Если в Hierarchy появился `EventSystem` — отлично. Если нет:
1. Правый клик в Hierarchy
2. **UI → Event System**

---

## Шаг 10: Создание HUD

### 10.1 Создай панель HUD:

1. Правый клик на `GameUI` в Hierarchy
2. **UI → Panel**
3. Назови её `HUD`

### 10.2 Настрой HUD Panel:

```
Rect Transform:
├── Anchor: Top Left
├── Pos X: 10
├── Pos Y: -10
├── Width: 300
└── Height: 150

Image (прозрачный фон):
└── Color: R=0, G=0, B=0, A=100
```

### 10.3 Добавь тексты:

**Текст времени:**
1. Правый клик на `HUD`
2. **UI → Text - TextMeshPro**
3. Назови `TimeText`
4. Настрой:
```
Text: "День 1 - 06:00"
Font Size: 24
Color: Белый
Alignment: Top Left
Pos Y: 0
```

**Текст HP:**
1. Правый клик на `HUD`
2. **UI → Text - TextMeshPro**
3. Назови `HPText`
```
Text: "HP: 100%"
Font Size: 20
Pos Y: -30
```

**Текст Ци:**
1. Правый клик на `HUD`
2. **UI → Text - TextMeshPro**
3. Назови `QiText`
```
Text: "Ци: 0/100"
Font Size: 20
Pos Y: -55
```

---

## Итоговая структура Hierarchy

```
Main (сцена)
├── Main Camera
├── Directional Light
├── GameManager
│   └── Systems
│       ├── WorldController
│       ├── TimeController
│       ├── LocationController
│       ├── EventController
│       ├── FactionController
│       ├── GeneratorRegistry
│       └── SaveManager
├── Player              ← создать по документу 05_PlayerSetup.md
├── EventSystem
└── GameUI (Canvas)
    ├── HUD
    │   ├── TimeText
    │   ├── HPText
    │   └── QiText
    └── (PauseMenu - добавить позже)
```

---

## Сохранение сцены

1. **Ctrl + S** или **File → Save**
2. Сцена сохранится в `Assets/Scenes/Main.unity`

---

## Проверка

### В Play Mode:

1. Нажми кнопку **Play**
2. Проверь Console:
   - Должно появиться: `[GameManager] Initializing game...`
   - Должно появиться: `World initialized: Cultivation World (Seed: 12345)`
   - Должно появиться: `Player initialized: Игрок`
3. Проверь HUD — тексты должны отображаться

### Типичные ошибки:

| Ошибка | Причина | Решение |
|--------|---------|---------|
| "Can't add component" | Компонент не скомпилировался | Проверь Console на ошибки C# |
| Canvas не виден | Нет EventSystem | Добавь UI → Event System |
| Текст не отображается | Нет TextMeshPro | Import TextMeshPro package |
| "NullReferenceException" | Пустая ссылка | References найдутся автоматически |

---

## Горячие клавиши (в игре)

| Клавиша | Действие |
|---------|----------|
| Escape | Пауза |
| F5 | Медитация (Player) |
| F9 | Quick Load |

---

*Документ создан: 2026-03-30*
*Обновлено: 2026-04-11 15:43:14 UTC — исправлена дата (2025→2026)*

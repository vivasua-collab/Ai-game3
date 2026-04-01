# Настройка Player

**Путь:** `Assets/Prefabs/Player/` (для префаба) или в сцене

---

## Шаг 1: Создание Player GameObject

### 1.1 Создай пустой объект:

1. В окне **Hierarchy** (в сцене Main)
2. **Правый клик → Create Empty**
3. Назови `Player`

### 1.2 Добавь Rigidbody2D (для движения):

1. **Add Component → Rigidbody 2D**
2. Настрой:
```
Rigidbody2D:
├── Body Type: Dynamic
├── Mass: 1
├── Linear Drag: 0
├── Angular Drag: 0.05
├── Gravity Scale: 0      (для top-down игры)
└── Constraints:
    ├── Freeze Rotation Z: ☑
    └── Freeze Position: ☐
```

### 1.3 Добавь Collider (для столкновений):

1. **Add Component → Circle Collider 2D**
2. Настрой:
```
Circle Collider 2D:
├── Is Trigger: ☐
├── Radius: 0.5
└── Offset: (0, 0)
```

### 1.4 Позиция:

```
Transform:
├── Position: X=0, Y=0, Z=0
├── Rotation: X=0, Y=0, Z=0
└── Scale: X=1, Y=1, Z=1
```

---

## Шаг 2: Добавление компонентов

Выдели `Player` в Hierarchy, затем добавь компоненты через **Add Component**:

### 2.1 Обязательные компоненты:

| Компонент | Назначение |
|-----------|------------|
| `PlayerController` | Главный контроллер игрока |
| `BodyController` | Система тела, HP, части тела |
| `QiController` | Система Ци, ядро, регенерация |
| `InventoryController` | Инвентарь |
| `EquipmentController` | Экипировка |
| `TechniqueController` | Техники культивации |
| `StatDevelopment` | Развитие характеристик |
| `SleepSystem` | Сон и восстановление |

### 2.2 Опциональные компоненты:

| Компонент | Назначение |
|-----------|------------|
| `InteractionController` | Взаимодействие с миром |
| `PlayerVisual` | Визуальное отображение |

---

## Шаг 3: Настройка PlayerController

```
PlayerController:
├── === Identity ===
├── playerId: "player"
├── playerName: "Игрок"
├──
├── === Systems ===
├── bodyController: None        ← автоматически найдёт
├── qiController: None          ← автоматически найдёт
├── techniqueController: None   ← автоматически найдёт
├── interactionController: None ← автоматически найдёт
├── statDevelopment: None       ← автоматически найдёт
├── sleepSystem: None           ← автоматически найдёт
├──
├── === Movement ===
├── moveSpeed: 5
├── runSpeedMultiplier: 1.5
└── staminaCostPerSecond: 1
├──
├── === References ===
├── worldController: None       ← автоматически найдёт
└── timeController: None        ← автоматически найдёт
```

**Важно:** Поля Systems и References найдутся автоматически через `GetComponent<>()` и `FindFirstObjectByType<>()`

---

## Шаг 4: Настройка BodyController

```
BodyController:
├── === Species ===
├── speciesData: None           ← можно оставить пустым (создаст гуманоида)
├──
├── === Body Material ===
├── bodyMaterial: Organic       ← выбрать из списка
├──
├── === Stats ===
├── vitality: 10
├── cultivationLevel: 1
├──
├── === Regeneration ===
├── enableRegeneration: ☑ true
└── regenRate: 1
```

### BodyMaterial (выбрать один):

| Значение | Описание | Снижение урона |
|----------|----------|----------------|
| Organic | Органика (по умолчанию) | 0% |
| Scaled | Чешуя | 30% |
| Chitin | Хитин | 20% |
| Mineral | Минерал | 50% |
| Ethereal | Эфир | 70% физики |
| Construct | Конструкт | 30-50% |
| Chaos | Хаос | переменное |

---

## Шаг 5: Настройка QiController

```
QiController:
├── === Cultivation ===
├── cultivationLevel: 1         ← 1-10
├── cultivationSubLevel: 0      ← 0-9
├── coreQuality: Normal         ← выбрать из списка
├──
├── === Qi Stats ===
├── coreCapacity: 1000          ← базовая ёмкость (пересчитывается автоматически)
├── currentQi: 100              ← текущее Ци
├── conductivity: 1.0           ← пересчитывается автоматически
├──
├── === Regeneration ===
├── enablePassiveRegen: ☑ true
└── regenMultiplier: 1
├──
├── === Combat ===
└── hasShieldTechnique: ☐ false
```

### CoreQuality (выбрать один):

| Значение | Множитель | Описание |
|----------|-----------|----------|
| Fragmented | ×0.5 | Осколочное |
| Cracked | ×0.7 | Треснутое |
| Flawed | ×0.85 | С изъяном |
| Normal | ×1.0 | Нормальное |
| Refined | ×1.2 | Очищенное |
| Perfect | ×1.5 | Совершенное |
| Transcendent | ×2.0 | Трансцендентное |

### Для смертного (до пробуждения):

```
cultivationLevel: 1
cultivationSubLevel: 0
currentQi: 50
enablePassiveRegen: false      ← смертные не регенерируют Ци
```

### Для практика 1 уровня:

```
cultivationLevel: 1
cultivationSubLevel: 0
coreQuality: Normal
currentQi: 100
enablePassiveRegen: true
```

---

## Шаг 6: Настройка InventoryController

```
InventoryController:
├── === Inventory Settings ===
├── gridWidth: 8
├── gridHeight: 6
├── maxWeight: 100
├── useWeightLimit: ☑ true
├──
├── === References ===
└── owner: None                ← можно оставить пустым
```

---

## Шаг 7: Настройка EquipmentController

```
EquipmentController:
├── === Equipment Settings ===
├── useLayerSystem: ☑ true
└── maxLayersPerSlot: 2
```

---

## Шаг 8: Настройка TechniqueController

```
TechniqueController:
├── === References ===
├── qiController: None         ← автоматически найдёт
├──
├── === Config ===
├── maxQuickSlots: 10
└── maxUltimates: 1
```

---

## Шаг 9: Настройка StatDevelopment

```
StatDevelopment:
├── === Config ===
├── enableSleepConsolidation: ☑ true
└── sleepDuration: 8
```

---

## Шаг 10: Настройка SleepSystem

```
SleepSystem:
├── === Config ===
├── minSleepHours: 1
├── maxSleepHours: 12
└── defaultSleepHours: 8
```

---

## Шаг 11: Создание префаба Player

### 11.1 Создай папку префабов:

1. В Project найди папку `Assets/Prefabs/`
2. Если папки нет — создай: **Create → Folder** → назови `Player`

### 11.2 Создай префаб:

1. Перетащи `Player` из Hierarchy в папку `Assets/Prefabs/Player/`
2. Player станет синим (это префаб)

### 11.3 Структура папки:

```
Assets/Prefabs/Player/
└── Player.prefab
```

---

## Итоговая структура Player

```
Player (GameObject)
├── Transform
│   ├── Position: (0, 0, 0)
│   ├── Rotation: (0, 0, 0)
│   └── Scale: (1, 1, 1)
├── Rigidbody2D
│   ├── Body Type: Dynamic
│   ├── Gravity Scale: 0
│   └── Freeze Rotation Z: ☑
├── Circle Collider 2D
│   └── Radius: 0.5
├── PlayerController
│   ├── playerId: "player"
│   ├── playerName: "Игрок"
│   ├── moveSpeed: 5
│   └── runSpeedMultiplier: 1.5
├── BodyController
│   ├── bodyMaterial: Organic
│   ├── vitality: 10
│   └── cultivationLevel: 1
├── QiController
│   ├── cultivationLevel: 1
│   ├── coreQuality: Normal
│   ├── currentQi: 100
│   └── enablePassiveRegen: true
├── InventoryController
│   ├── gridWidth: 8
│   └── gridHeight: 6
├── EquipmentController
│   └── useLayerSystem: true
├── TechniqueController
│   ├── maxQuickSlots: 10
│   └── maxUltimates: 1
├── StatDevelopment
│   └── enableSleepConsolidation: true
└── SleepSystem
    └── defaultSleepHours: 8
```

---

## Проверка

### В Play Mode:

1. Нажми кнопку **Play**
2. Проверь Console на ошибки
3. Должны появиться сообщения:
   - `[GameManager] Initializing game...`
   - `World initialized: Cultivation World (Seed: 12345)`
   - `Player initialized: Игрок`
4. Используй WASD для движения
5. F5 — медитация

### Типичные ошибки:

| Ошибка | Причина | Решение |
|--------|---------|---------|
| "NullReferenceException" | Пустая ссылка | References найдутся автоматически |
| "Missing component" | Нет компонента | Добавь компонент |
| Rigidbody2D not found | Нет Rigidbody2D | Добавь Rigidbody2D |
| Player не двигается | Нет Collider или RigidBody | Добавь компоненты |

---

## Горячие клавиши (игрок)

| Клавиша | Действие |
|---------|----------|
| WASD | Движение |
| Shift | Бег |
| F5 | Медитация |
| Escape | Пауза |

---

## Краткая шпаргалка

| Компонент | Ключевые поля |
|-----------|---------------|
| PlayerController | playerId, playerName, moveSpeed |
| BodyController | bodyMaterial, vitality |
| QiController | cultivationLevel, coreQuality, currentQi |
| InventoryController | gridWidth, gridHeight |
| EquipmentController | useLayerSystem |
| TechniqueController | maxQuickSlots, maxUltimates |
| StatDevelopment | enableSleepConsolidation |
| SleepSystem | defaultSleepHours |

---

## Enums (из кода)

### CultivationLevel
```csharp
public enum CultivationLevel
{
    None = 0,               // Смертный (без ядра)
    AwakenedCore = 1,       // Пробуждённое Ядро
    LifeFlow = 2,           // Течение Жизни
    InternalFire = 3,       // Пламя Внутреннего Огня
    BodySpiritUnion = 4,    // Объединение Тела и Духа
    HeartOfHeaven = 5,      // Сердце Небес
    VeilBreaker = 6,        // Разрыв Пелены
    EternalRing = 7,        // Вечное Кольцо
    VoiceOfHeaven = 8,      // Глас Небес
    ImmortalCore = 9,       // Бессмертное Ядро
    Ascension = 10          // Вознесение
}
```

### CoreQuality
```csharp
public enum CoreQuality
{
    Fragmented = 1,     // ×0.5
    Cracked = 2,        // ×0.7
    Flawed = 3,         // ×0.85
    Normal = 4,         // ×1.0
    Refined = 5,        // ×1.2
    Perfect = 6,        // ×1.5
    Transcendent = 7    // ×2.0
}
```

### BodyMaterial
```csharp
public enum BodyMaterial
{
    Organic,        // 0% снижения
    Scaled,         // 30% снижения
    Chitin,         // 20% снижения
    Mineral,        // 50% снижения
    Ethereal,       // 70% физики
    Construct,      // 30-50%
    Chaos           // переменное
}
```

---

*Документ создан: 2026-03-30*
*Обновлено: 2025-04-01 — добавлены новые компоненты, обновлены поля*

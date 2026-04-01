# Настройка Player (Полуавтомат)

**Инструмент:** `Window → Scene Setup Tools`

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание GameObject Player | ✅ Автоматически |
| Добавление Rigidbody2D | ✅ Автоматически |
| Настройка Rigidbody2D | ✅ Автоматически |
| Добавление CircleCollider2D | ✅ Автоматически |
| Добавление PlayerController | ✅ Автоматически |
| Добавление BodyController | ✅ Автоматически |
| Добавление QiController | ✅ Автоматически |
| Добавление InventoryController | ✅ Автоматически |
| Добавление EquipmentController | ✅ Автоматически |
| Добавление TechniqueController | ✅ Автоматически |
| Добавление StatDevelopment | ✅ Автоматически |
| Добавление SleepSystem | ✅ Автоматически |
| Настройка всех полей | ✅ Автоматически |
| Создание префаба | ❌ Руками |
| Визуальное отображение | ❌ Руками |
| Sprite/Animator | ❌ Руками |

---

## Шаг 1: Запуск Scene Setup Tools (АВТОМАТИЧЕСКИ)

**Действия:**
1. Открой меню: **Window → Scene Setup Tools**
2. Разверни секцию **"2. Player Setup"**
3. Настрой параметры:

### Основные настройки:
```
Player Name:     Игрок        (или своё имя)
Move Speed:      5
Run Multiplier:  1.5
```

### Body Settings:
```
Body Material:   Organic      (выбрать из списка)
Vitality:        10
```

### Qi Settings:
```
Cultivation Level:  1         (1-10)
Core Quality:       Normal    (выбрать из списка)
Current Qi:         100
Passive Regen:      ☑ true
```

4. Нажми **"Create Player GameObject"**

---

## Шаг 2: Проверка созданного Player

**Hierarchy должен содержать:**

```
Player (GameObject)
├── Transform
│   ├── Position: (0, 0, 0)
│   ├── Rotation: (0, 0, 0)
│   └── Scale: (1, 1, 1)
├── Rigidbody2D           ✅ настроен
│   ├── Body Type: Dynamic
│   ├── Gravity Scale: 0
│   └── Freeze Rotation Z: ☑
├── Circle Collider 2D    ✅ настроен
│   └── Radius: 0.5
├── PlayerController      ✅ настроен
├── BodyController        ✅ настроен
├── QiController          ✅ настроен
├── InventoryController   ✅ настроен
├── EquipmentController   ✅ настроен
├── TechniqueController   ✅ настроен
├── StatDevelopment       ✅ настроен
└── SleepSystem           ✅ настроен
```

---

## Шаг 3: Создание префаба (ВРУКАМИ)

**Prefab нельзя создать скриптом!**

**Действия:**
1. Убедись что папка `Assets/Prefabs/Player/` существует
2. Перетащи `Player` из Hierarchy в папку `Assets/Prefabs/Player/`
3. Player станет синим (это префаб)
4. При появлении диалога выбери **"Original Prefab"**

**Структура папки:**
```
Assets/Prefabs/Player/
└── Player.prefab
```

---

## Шаг 4: Визуальное отображение (ВРУКАМИ)

### 4.1 Добавь Sprite Renderer:

Выдели `Player` в Hierarchy или открой префаб:

1. **Add Component → Sprite Renderer**
2. Настрой:
```
Sprite Renderer:
├── Sprite: (выбери спрайт персонажа)
├── Color: Белый
├── Flip: ☐ X, ☐ Y
└── Sorting Layer: Default (или создай "Characters")
```

### 4.2 Если нет спрайта (временно):

1. Создай простой спрайт: **Assets → Create → 2D → Sprites → Square**
2. Назови его `TempPlayerSprite`
3. Перетащи его в поле Sprite компонента Sprite Renderer

---

## Шаг 5: Дочерние объекты (ВРУКАМИ, опционально)

### 5.1 Визуальный контейнер (рекомендуется):

Создай дочерний объект для визуала:

1. Правый клик на `Player` → **Create Empty**
2. Назови `Visual`
3. Добавь Sprite Renderer на `Visual`
4. Анимации будут на `Visual`, не на `Player`

**Структура:**
```
Player
├── Visual              ← Sprite Renderer, Animator
├── InteractionRange    ← Circle Collider 2D (Trigger)
└── (другие дочерние объекты)
```

### 5.2 Зона взаимодействия:

1. Правый клик на `Player` → **Create Empty**
2. Назови `InteractionRange`
3. **Add Component → Circle Collider 2D**
4. Настрой:
```
Circle Collider 2D:
├── Is Trigger: ☑
├── Radius: 1.5
└── Offset: (0, 0)
```

---

## Шаг 6: Тег и слой (ВРУКАМИ)

Выдели `Player`:

### Тег:
1. В Inspector найди **Tag** вверху
2. Выбери `Player` (или создай: Add Tag → Player)

### Слой:
1. В Inspector найди **Layer** вверху
2. Выбери `Player` (или создай: Add Layer → Layer 6: Player)

---

## Шаг 7: Animator (ВРУКАМИ, опционально)

### 7.1 Создай Animator Controller:

1. Правый клик в Project: **Create → Animator Controller**
2. Назови `PlayerAnimator`
3. Двойной клик — открой Animator окно

### 7.2 Добавь Animator:

Выдели `Player` (или `Visual`):

1. **Add Component → Animator**
2. Перетащи `PlayerAnimator` в поле Controller

### 7.3 Базовые состояния (создай в Animator):

```
Entry → Idle
Idle ↔ Walk
Idle → Attack → Idle
Any State → Hurt → Idle
Any State → Death
```

---

## Шаг 8: Проверка

### Нажми Play:

**Console должна показать:**
```
[GameManager] Initializing game...
Player initialized: Игрок
```

**Проверь:**
- [ ] Player виден на сцене (если добавлен Sprite)
- [ ] Player двигается (WASD)
- [ ] Player поворачивается (Shift + движение = бег)
- [ ] F5 — медитация работает
- [ ] Escape — пауза работает

### Тест движения:

| Клавиша | Действие |
|---------|----------|
| W | Вверх |
| S | Вниз |
| A | Влево |
| D | Вправо |
| Shift | Бег (×1.5) |
| F5 | Медитация |

---

## Настройки для разных типов персонажей

### Смертный (не практик):
```
Cultivation Level:  1
Core Quality:       Fragmented (×0.5)
Current Qi:         0-50
Passive Regen:      ☐ false
```

### Начинающий практик:
```
Cultivation Level:  1
Core Quality:       Normal (×1.0)
Current Qi:         100
Passive Regen:      ☑ true
```

### Опытный практик:
```
Cultivation Level:  3-4
Core Quality:       Refined (×1.2)
Current Qi:         500+
Passive Regen:      ☑ true
```

### Мастер:
```
Cultivation Level:  7-8
Core Quality:       Perfect (×1.5)
Current Qi:         2000+
Passive Regen:      ☑ true
```

---

## Шпаргалка: Что руками, что скриптом

| Задача | Способ |
|--------|--------|
| Создать Player GameObject | 🤖 Скрипт |
| Добавить компоненты | 🤖 Скрипт |
| Настроить поля | 🤖 Скрипт |
| Создать префаб | ✋ Руками |
| Добавить Sprite | ✋ Руками |
| Настроить Animator | ✋ Руками |
| Создать дочерние объекты | ✋ Руками |
| Настроить тег/слой | ✋ Руками |

---

## Быстрая проверка всех компонентов

Выдели `Player` и проверь что все компоненты есть:

| Компонент | Проверка |
|-----------|----------|
| Rigidbody2D | Body Type: Dynamic, Gravity: 0 |
| CircleCollider2D | Radius: 0.5 |
| PlayerController | playerName, moveSpeed |
| BodyController | bodyMaterial, vitality |
| QiController | cultivationLevel, coreQuality |
| InventoryController | gridWidth: 8, gridHeight: 6 |
| EquipmentController | useLayerSystem: true |
| TechniqueController | maxQuickSlots: 10 |
| StatDevelopment | enableSleepConsolidation: true |
| SleepSystem | defaultSleepHours: 8 |

---

*Документ создан: 2025-04-01*

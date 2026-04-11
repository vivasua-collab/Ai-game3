# Настройка ElementData

**Путь:** `Assets/Data/Elements/`

**Создание:** Правый клик → Create → Cultivation → Element

---

## Структура полей (из ElementData.cs)

### Basic Info
| Поле | Тип | Описание |
|------|-----|----------|
| elementType | enum (Element) | Тип элемента |
| nameRu | string | Название на русском |
| nameEn | string | Название на английском |
| icon | Sprite | Иконка (пока null) |
| color | Color | Цвет элемента |
| description | text | Описание элемента |

### Relationships
| Поле | Тип | Описание |
|------|-----|----------|
| oppositeElement | enum (Element) | Противоположный элемент |
| affinityElements | List\<Element\> | Элементы сродства (урон ↓) |
| weakToElements | List\<Element\> | Элементы уязвимости (урон ↑) |

### Damage Multipliers
| Поле | Тип | По умолчанию | Описание |
|------|-----|--------------|----------|
| oppositeMultiplier | float | 1.5 | Урон по противоположному |
| affinityMultiplier | float | 0.8 | Урон по сродству |
| voidMultiplier | float | 1.2 | Урон от Void |

### Effects
| Поле | Тип | Описание |
|------|-----|----------|
| possibleEffects | List\<ElementEffect\> | Возможные эффекты элемента |

### ElementEffect (класс)
| Поле | Тип | Описание |
|------|-----|----------|
| effectName | string | Название эффекта |
| description | string | Описание |
| baseDuration | int | Длительность (тики) |
| damageMultiplier | float | Множитель урона от эффекта |
| applyChance | float (%) | Шанс наложения |

### Environment
| Поле | Тип | Описание |
|------|-----|----------|
| affectsEnvironment | bool | Влияет на окружение |
| environmentEffects | List\<EnvironmentEffect\> | Эффекты на местности |

### EnvironmentEffect (класс)
| Поле | Тип | Описание |
|------|-----|----------|
| effectName | string | Название |
| description | string | Описание |
| radius | int | Радиус |
| duration | int | Длительность |

---

## Enum Element (из Enums.cs)

```csharp
public enum Element
{
    Neutral,    // 0 — Нейтральный
    Fire,       // 1 — Огонь
    Water,      // 2 — Вода
    Earth,      // 3 — Земля
    Air,        // 4 — Воздух
    Lightning,  // 5 — Молния
    Void,       // 6 — Пустота
    Poison      // 7 — Яд (особая стихия)
    // FIX CORE-H05: Count убран. Используйте Enum.GetValues(typeof(Element)).Length
}
```

---

## Элемент 0: Neutral (Нейтральный)

**Имя файла:** `Element_Neutral`

```
=== Basic Info ===
elementType: Neutral
nameRu: Нейтральный
nameEn: Neutral
icon: None
color: White (#FFFFFF)
description: Нейтральный элемент, не имеет преимуществ или слабостей.

=== Relationships ===
oppositeElement: None
affinityElements: (пустой список)
weakToElements: (пустой список)

=== Damage Multipliers ===
oppositeMultiplier: 1.5
affinityMultiplier: 0.8
voidMultiplier: 1.2

=== Effects ===
possibleEffects: (пустой список)

=== Environment ===
affectsEnvironment: false
environmentEffects: (пустой список)
```

---

## Элемент 1: Fire (Огонь)

**Имя файла:** `Element_Fire`

```
=== Basic Info ===
elementType: Fire
nameRu: Огонь
nameEn: Fire
icon: None
color: Orange Red (#FF4500) — R:255 G:69 B:0
description: Стихия огня — разрушительная сила, сильна против воздуха.

=== Relationships ===
oppositeElement: Water
affinityElements: Air
weakToElements: Water

=== Damage Multipliers ===
oppositeMultiplier: 1.5
affinityMultiplier: 0.8
voidMultiplier: 1.2

=== Effects ===
possibleEffects: (2 эффекта)

Эффект 1:
  effectName: burning
  description: Постоянный урон от огня
  baseDuration: 10
  damageMultiplier: 0.1
  applyChance: 50

Эффект 2:
  effectName: heat
  description: Повышение температуры, ослабление
  baseDuration: 5
  damageMultiplier: 0
  applyChance: 30

=== Environment ===
affectsEnvironment: true
environmentEffects: (пустой список)
```

---

## Элемент 2: Water (Вода)

**Имя файла:** `Element_Water`

```
=== Basic Info ===
elementType: Water
nameRu: Вода
nameEn: Water
icon: None
color: Dodger Blue (#1E90FF) — R:30 G:144 B:255
description: Стихия воды — гибкая и изменчивая, сильна против огня.

=== Relationships ===
oppositeElement: Fire
affinityElements: Lightning
weakToElements: Fire

=== Damage Multipliers ===
oppositeMultiplier: 1.5
affinityMultiplier: 0.8
voidMultiplier: 1.2

=== Effects ===
possibleEffects: (2 эффекта)

Эффект 1:
  effectName: freezing
  description: Замедление движения
  baseDuration: 8
  damageMultiplier: 0.05
  applyChance: 40

Эффект 2:
  effectName: slow
  description: Снижение скорости
  baseDuration: 10
  damageMultiplier: 0
  applyChance: 60

=== Environment ===
affectsEnvironment: true
environmentEffects: (пустой список)
```

---

## Элемент 3: Earth (Земля)

**Имя файла:** `Element_Earth`

```
=== Basic Info ===
elementType: Earth
nameRu: Земля
nameEn: Earth
icon: None
color: Saddle Brown (#8B4513) — R:139 G:69 B:19
description: Стихия земли — устойчивая и защитная.

=== Relationships ===
oppositeElement: Air
affinityElements: Fire
weakToElements: Air

=== Damage Multipliers ===
oppositeMultiplier: 1.5
affinityMultiplier: 0.8
voidMultiplier: 1.2

=== Effects ===
possibleEffects: (2 эффекта)

Эффект 1:
  effectName: shield
  description: Временная защита
  baseDuration: 15
  damageMultiplier: 0
  applyChance: 70

Эффект 2:
  effectName: knockback
  description: Отталкивание врага
  baseDuration: 1
  damageMultiplier: 0
  applyChance: 80

=== Environment ===
affectsEnvironment: true
environmentEffects: (пустой список)
```

---

## Элемент 4: Air (Воздух)

**Имя файла:** `Element_Air`

```
=== Basic Info ===
elementType: Air
nameRu: Воздух
nameEn: Air
icon: None
color: Light Sky Blue (#87CEEB) — R:135 G:206 B:235
description: Стихия воздуха — быстрая и неуловимая.

=== Relationships ===
oppositeElement: Earth
affinityElements: Fire, Lightning
weakToElements: Earth

=== Damage Multipliers ===
oppositeMultiplier: 1.5
affinityMultiplier: 0.8
voidMultiplier: 1.2

=== Effects ===
possibleEffects: (2 эффекта)

Эффект 1:
  effectName: knockback
  description: Отталкивание врага
  baseDuration: 1
  damageMultiplier: 0
  applyChance: 80

Эффект 2:
  effectName: speed
  description: Повышение скорости
  baseDuration: 10
  damageMultiplier: 0
  applyChance: 50

=== Environment ===
affectsEnvironment: false
environmentEffects: (пустой список)
```

---

## Элемент 5: Lightning (Молния)

**Имя файла:** `Element_Lightning`

```
=== Basic Info ===
elementType: Lightning
nameRu: Молния
nameEn: Lightning
icon: None
color: Gold (#FFD700) — R:255 G:215 B:0
description: Стихия молнии — быстрая и пробивающая.

=== Relationships ===
oppositeElement: None
affinityElements: Water, Air
weakToElements: Earth

=== Damage Multipliers ===
oppositeMultiplier: 1.5
affinityMultiplier: 0.8
voidMultiplier: 1.2

=== Effects ===
possibleEffects: (3 эффекта)

Эффект 1:
  effectName: stun
  description: Временная парализация
  baseDuration: 3
  damageMultiplier: 0
  applyChance: 30

Эффект 2:
  effectName: pierce
  description: Игнорирование части защиты
  baseDuration: 1
  damageMultiplier: 0
  applyChance: 60

Эффект 3:
  effectName: chain
  description: Переход на соседние цели
  baseDuration: 1
  damageMultiplier: 0.5
  applyChance: 40

=== Environment ===
affectsEnvironment: true
environmentEffects: (пустой список)
```

---

## Элемент 6: Void (Пустота)

**Имя файла:** `Element_Void`

```
=== Basic Info ===
elementType: Void
nameRu: Пустота
nameEn: Void
icon: None
color: Indigo (#4B0082) — R:75 G:0 B:130
description: Стихия пустоты — загадочная и всепоглощающая.

=== Relationships ===
oppositeElement: None
affinityElements: (пустой список)
weakToElements: (пустой список)

=== Damage Multipliers ===
oppositeMultiplier: 1.5
affinityMultiplier: 0.8
voidMultiplier: 1.2

=== Effects ===
possibleEffects: (3 эффекта)

Эффект 1:
  effectName: pierce
  description: Игнорирование защиты
  baseDuration: 1
  damageMultiplier: 0
  applyChance: 80

Эффект 2:
  effectName: leech
  description: Кража Ци или HP
  baseDuration: 5
  damageMultiplier: 0
  applyChance: 40

Эффект 3:
  effectName: drain
  description: Ослабление врага
  baseDuration: 10
  damageMultiplier: 0
  applyChance: 50

=== Environment ===
affectsEnvironment: true
environmentEffects: (пустой список)
```

---

## Элемент 7: Poison (Яд)

**Имя файла:** `Element_Poison`

```
=== Basic Info ===
elementType: Poison
nameRu: Яд
nameEn: Poison
icon: None
color: Dark Green (#006400) — R:0 G:100 B:0
description: Особая стихия яда — отравление, дебаффы. Техники Poison доступны только с element=poison.

=== Relationships ===
oppositeElement: None
affinityElements: (пустой список)
weakToElements: Fire

=== Damage Multipliers ===
oppositeMultiplier: 1.5
affinityMultiplier: 0.8
voidMultiplier: 1.2

=== Effects ===
possibleEffects: (3 эффекта)

Эффект 1:
  effectName: poison_dot
  description: Постоянный урон от отравления (DoT)
  baseDuration: 15
  damageMultiplier: 0.08
  applyChance: 60

Эффект 2:
  effectName: weaken
  description: Снижение характеристик врага
  baseDuration: 10
  damageMultiplier: 0
  applyChance: 50

Эффект 3:
  effectName: corrosion
  description: Разрушение экипировки (снижение прочности)
  baseDuration: 8
  damageMultiplier: 0
  applyChance: 30

=== Environment ===
affectsEnvironment: true
environmentEffects: (пустой список)
```

---

## Сводная таблица

| Element | nameRu | Color | Противоположный | Сродство | Уязвим |
|---------|--------|-------|-----------------|----------|--------|
| Neutral (0) | Нейтральный | Белый | — | — | — |
| Fire (1) | Огонь | Оранжевый | Water | Air | Water |
| Water (2) | Вода | Синий | Fire | Lightning | Fire |
| Earth (3) | Земля | Коричневый | Air | Fire | Air |
| Air (4) | Воздух | Голубой | Earth | Fire, Lightning | Earth |
| Lightning (5) | Молния | Золотой | — | Water, Air | Earth |
| Void (6) | Пустота | Фиолетовый | — | — | — |
| Poison (7) | Яд | Тёмно-зелёный | — | — | Fire |

---

## Диаграмма отношений

```
           [Void]              [Poison]
         (особый элемент)     (особая стихия, только Poison-техники)
        

     Fire ←——✕——→ Water
       ↑           ↑
       |           |
       ↓           ↓
     Air  ←——✕——→ Earth
       
     
    Lightning (самостоятельный)
           ↓
      уязвим к Earth

    Poison (самостоятельный)
           ↓
      уязвим к Fire
```

---

## Цвета для Unity Color Picker

| Element | R | G | B | Hex |
|---------|---|---|---|-----|
| Neutral | 255 | 255 | 255 | #FFFFFF |
| Fire | 255 | 69 | 0 | #FF4500 |
| Water | 30 | 144 | 255 | #1E90FF |
| Earth | 139 | 69 | 19 | #8B4513 |
| Air | 135 | 206 | 235 | #87CEEB |
| Lightning | 255 | 215 | 0 | #FFD700 |
| Void | 75 | 0 | 130 | #4B0082 |
| Poison | 0 | 100 | 0 | #006400 |

---

## Примеры расчёта урона

### Fire vs Water (противоположные)
```
finalDamage = baseDamage × oppositeMultiplier = baseDamage × 1.5
```

### Fire vs Air (сродство)
```
finalDamage = baseDamage × affinityMultiplier = baseDamage × 0.8
```

### Void vs любой элемент
```
finalDamage = baseDamage × voidMultiplier = baseDamage × 1.2
```

### Neutral vs любой
```
finalDamage = baseDamage × 1.0
```

---

*Документ создан: 2026-03-30*
*Обновлено: 2026-04-11 15:43:14 UTC — добавлен Poison (8-й элемент), убран Count (CORE-H05)*

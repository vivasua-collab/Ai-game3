# Настройка CultivationLevelData

**Путь:** `Assets/Data/CultivationLevels/`

**Создание:** Правый клик → Create → Cultivation → Cultivation Level

---

## Структура полей (из CultivationLevelData.cs)

### Basic Info
| Поле | Тип | Описание |
|------|-----|----------|
| level | int (1-10) | Уровень культивации |
| nameRu | string | Название на русском |
| nameEn | string | Название на английском |
| description | text | Описание уровня |

### Qi Parameters
| Поле | Тип | Описание |
|------|-----|----------|
| qiDensity | int | Плотность Ци = 2^(level-1) |
| coreCapacityMultiplier | float | Множитель роста ёмкости ядра |
| baseCoreCapacity | long | Базовая ёмкость ядра на этом уровне |

### Body Effects
| Поле | Тип | Описание |
|------|-----|----------|
| agingMultiplier | float (0-1) | Множитель старения (1.0 = норма, 0.0 = остановлено) |
| regenerationMultiplier | float | Множитель регенерации |
| conductivityMultiplier | float | Множитель проводимости |

### Abilities
| Поле | Тип | Описание |
|------|-----|----------|
| abilitiesDescription | text | Описание способностей |
| noFoodRequired | bool | Может жить без еды |
| noWaterRequired | bool | Может жить без воды |
| canFly | bool | Может летать |
| canRegenerateLimbs | bool | Регенерация конечностей |

### Breakthrough
| Поле | Тип | По умолчанию | Описание |
|------|-----|--------------|----------|
| useDynamicBreakthroughCalculation | bool | true | Использовать динамический расчёт |
| subLevelMultiplier | int | 10 | Множитель Ци для под-уровня |
| levelMultiplier | int | 100 | Множитель Ци для уровня |
| qiForSubLevelBreakthrough | long | 10000 | Ци для прорыва под-уровня (если dynamic = false) |
| qiForLevelBreakthrough | long | 100000 | Ци для прорыва уровня (если dynamic = false) |
| breakthroughFailureChance | float (%) | 10 | Шанс неудачи прорыва |
| breakthroughFailureDamage | float (%) | 20 | Урон при неудаче |

---

## Формулы (из кода)

### Плотность Ци
```
qiDensity = 2^(level - 1)
```

### Ёмкость ядра (для L.X.0)
```
baseCoreCapacity = 1000 × 1.1^((level-1) × 10)
```

### Полная формула ёмкости ядра
```
coreCapacity = 1000 × qualityMultiplier × 1.1^((level-1) × 10 + subLevel)
```

### Качество ядра (CoreQuality)
| Качество | Множитель |
|----------|-----------|
| Fragmented | 0.5 |
| Cracked | 0.7 |
| Flawed | 0.85 |
| Normal | 1.0 |
| Refined | 1.2 |
| Perfect | 1.5 |
| Transcendent | 2.0 |

### Динамический расчёт прорыва (из GetQiForSubLevelBreakthrough / GetQiForLevelBreakthrough)
```
qiForSubLevelBreakthrough = coreCapacity × subLevelMultiplier
qiForLevelBreakthrough = coreCapacity × levelMultiplier
```

---

## Уровень 1: Пробуждённое Ядро

**Имя файла:** `Level1_AwakenedCore`

```
=== Basic Info ===
level: 1
nameRu: Пробуждённое Ядро
nameEn: Awakened Core
description: Первый уровень культивации. Ядро Ци пробуждено и начинает активно работать.

=== Qi Parameters ===
qiDensity: 1
coreCapacityMultiplier: 1.0
baseCoreCapacity: 1000

=== Body Effects ===
agingMultiplier: 1.0
regenerationMultiplier: 1.1
conductivityMultiplier: 1.0

=== Abilities ===
abilitiesDescription: Базовое управление Ци. Простейшие техники.
noFoodRequired: false
noWaterRequired: false
canFly: false
canRegenerateLimbs: false

=== Breakthrough ===
useDynamicBreakthroughCalculation: true
subLevelMultiplier: 10
levelMultiplier: 100
qiForSubLevelBreakthrough: 10000       (игнорируется если dynamic = true)
qiForLevelBreakthrough: 100000         (игнорируется если dynamic = true)
breakthroughFailureChance: 5
breakthroughFailureDamage: 10
```

---

## Уровень 2: Течение Жизни

**Имя файла:** `Level2_LifeFlow`

```
=== Basic Info ===
level: 2
nameRu: Течение Жизни
nameEn: Life Flow
description: Ци течёт по телу, укрепляя меридианы. Улучшается здоровье и выносливость.

=== Qi Parameters ===
qiDensity: 2
coreCapacityMultiplier: 2.594
baseCoreCapacity: 2594

=== Body Effects ===
agingMultiplier: 1.0
regenerationMultiplier: 2.0
conductivityMultiplier: 1.2

=== Abilities ===
abilitiesDescription: Укрепление тела. Техники исцеления лёгких ран.
noFoodRequired: false
noWaterRequired: false
canFly: false
canRegenerateLimbs: false

=== Breakthrough ===
useDynamicBreakthroughCalculation: true
subLevelMultiplier: 10
levelMultiplier: 100
breakthroughFailureChance: 8
breakthroughFailureDamage: 15
```

---

## Уровень 3: Пламя Внутреннего Огня

**Имя файла:** `Level3_InternalFire`

```
=== Basic Info ===
level: 3
nameRu: Пламя Внутреннего Огня
nameEn: Internal Fire
description: Внутренний огонь очищает тело. Можно пробуждать других.

=== Qi Parameters ===
qiDensity: 4
coreCapacityMultiplier: 6.727
baseCoreCapacity: 6727

=== Body Effects ===
agingMultiplier: 0.9
regenerationMultiplier: 3.0
conductivityMultiplier: 1.5

=== Abilities ===
abilitiesDescription: Пробуждение смертных. Техники огня. Очистка тела от токсинов.
noFoodRequired: false
noWaterRequired: false
canFly: false
canRegenerateLimbs: false

=== Breakthrough ===
useDynamicBreakthroughCalculation: true
subLevelMultiplier: 10
levelMultiplier: 100
breakthroughFailureChance: 10
breakthroughFailureDamage: 20
```

---

## Уровень 4: Объединение Тела и Духа

**Имя файла:** `Level4_BodySpiritUnion`

```
=== Basic Info ===
level: 4
nameRu: Объединение Тела и Духа
nameEn: Body Spirit Union
description: Тело и дух едины. Практик может обходиться без пищи.

=== Qi Parameters ===
qiDensity: 8
coreCapacityMultiplier: 17.45
baseCoreCapacity: 17450

=== Body Effects ===
agingMultiplier: 0.4
regenerationMultiplier: 5.0
conductivityMultiplier: 2.0

=== Abilities ===
abilitiesDescription: Питание Ци вместо еды. Техники поддержки. Укрепление духа.
noFoodRequired: true
noWaterRequired: false
canFly: false
canRegenerateLimbs: false

=== Breakthrough ===
useDynamicBreakthroughCalculation: true
subLevelMultiplier: 10
levelMultiplier: 100
breakthroughFailureChance: 15
breakthroughFailureDamage: 25
```

---

## Уровень 5: Сердце Небес

**Имя файла:** `Level5_HeartOfHeaven`

```
=== Basic Info ===
level: 5
nameRu: Сердце Небес
nameEn: Heart of Heaven
description: Практик слышит голос Небес. Может обходиться без воды.

=== Qi Parameters ===
qiDensity: 16
coreCapacityMultiplier: 45.26
baseCoreCapacity: 45260

=== Body Effects ===
agingMultiplier: 0.3
regenerationMultiplier: 8.0
conductivityMultiplier: 3.0

=== Abilities ===
abilitiesDescription: Предвидение. Питание только Ци. Техники молнии.
noFoodRequired: true
noWaterRequired: true
canFly: false
canRegenerateLimbs: false

=== Breakthrough ===
useDynamicBreakthroughCalculation: true
subLevelMultiplier: 10
levelMultiplier: 100
breakthroughFailureChance: 20
breakthroughFailureDamage: 30
```

---

## Уровень 6: Разрыв Пелены

**Имя файла:** `Level6_VeilBreaker`

```
=== Basic Info ===
level: 6
nameRu: Разрыв Пелены
nameEn: Veil Breaker
description: Практик разрывает пелену между мирами. Способность к полёту.

=== Qi Parameters ===
qiDensity: 32
coreCapacityMultiplier: 117.39
baseCoreCapacity: 117390

=== Body Effects ===
agingMultiplier: 0.1
regenerationMultiplier: 15.0
conductivityMultiplier: 5.0

=== Abilities ===
abilitiesDescription: Полёт/парение. Техники пространства. Видение сквозь иллюзии.
noFoodRequired: true
noWaterRequired: true
canFly: true
canRegenerateLimbs: false

=== Breakthrough ===
useDynamicBreakthroughCalculation: true
subLevelMultiplier: 10
levelMultiplier: 100
breakthroughFailureChance: 25
breakthroughFailureDamage: 35
```

---

## Уровень 7: Вечное Кольцо

**Имя файла:** `Level7_EternalRing`

```
=== Basic Info ===
level: 7
nameRu: Вечное Кольцо
nameEn: Eternal Ring
description: Тело практически бессмертно. Регенерация конечностей.

=== Qi Parameters ===
qiDensity: 64
coreCapacityMultiplier: 304.48
baseCoreCapacity: 304480

=== Body Effects ===
agingMultiplier: 0.0
regenerationMultiplier: 30.0
conductivityMultiplier: 8.0

=== Abilities ===
abilitiesDescription: Регенерация конечностей. Техники времени. Почти бессмертие.
noFoodRequired: true
noWaterRequired: true
canFly: true
canRegenerateLimbs: true

=== Breakthrough ===
useDynamicBreakthroughCalculation: true
subLevelMultiplier: 10
levelMultiplier: 100
breakthroughFailureChance: 30
breakthroughFailureDamage: 40
```

---

## Уровень 8: Глас Небес

**Имя файла:** `Level8_VoiceOfHeaven`

```
=== Basic Info ===
level: 8
nameRu: Глас Небес
nameEn: Voice of Heaven
description: Практик говорит с Небесами. Слова имеют силу закона.

=== Qi Parameters ===
qiDensity: 128
coreCapacityMultiplier: 789.75
baseCoreCapacity: 789750

=== Body Effects ===
agingMultiplier: 0.0
regenerationMultiplier: 100.0
conductivityMultiplier: 15.0

=== Abilities ===
abilitiesDescription: Слово закона. Техники души. Контроль погоды на уровне региона.
noFoodRequired: true
noWaterRequired: true
canFly: true
canRegenerateLimbs: true

=== Breakthrough ===
useDynamicBreakthroughCalculation: true
subLevelMultiplier: 10
levelMultiplier: 100
breakthroughFailureChance: 35
breakthroughFailureDamage: 45
```

---

## Уровень 9: Бессмертное Ядро

**Имя файла:** `Level9_ImmortalCore`

```
=== Basic Info ===
level: 9
nameRu: Бессмертное Ядро
nameEn: Immortal Core
description: Ядро бессмертно. Практик не стареет.

=== Qi Parameters ===
qiDensity: 256
coreCapacityMultiplier: 2048.4
baseCoreCapacity: 2048400

=== Body Effects ===
agingMultiplier: 0.0
regenerationMultiplier: 1000.0
conductivityMultiplier: 50.0

=== Abilities ===
abilitiesDescription: Истинное бессмертие. Техники реальности. Создание измерений.
noFoodRequired: true
noWaterRequired: true
canFly: true
canRegenerateLimbs: true

=== Breakthrough ===
useDynamicBreakthroughCalculation: true
subLevelMultiplier: 10
levelMultiplier: 100
breakthroughFailureChance: 40
breakthroughFailureDamage: 50
```

---

## Уровень 10: Вознесение

**Имя файла:** `Level10_Ascension`

```
=== Basic Info ===
level: 10
nameRu: Вознесение
nameEn: Ascension
description: Последний уровень. Практик готов покинуть этот мир.

=== Qi Parameters ===
qiDensity: 512
coreCapacityMultiplier: 5313.0
baseCoreCapacity: 5313000

=== Body Effects ===
agingMultiplier: 0.0
regenerationMultiplier: 999999     (бесконечность в коде)
conductivityMultiplier: 100.0

=== Abilities ===
abilitiesDescription: Вознесение. Трансцендентность. Слияние с Дао.
noFoodRequired: true
noWaterRequired: true
canFly: true
canRegenerateLimbs: true

=== Breakthrough ===
useDynamicBreakthroughCalculation: true
subLevelMultiplier: 10
levelMultiplier: 100
breakthroughFailureChance: 50
breakthroughFailureDamage: 60
```

---

## Сводная таблица

| L | nameRu | baseCore | qiDensity | aging | regen | conduct | food | water | fly | limbs |
|---|--------|----------|-----------|-------|-------|---------|------|-------|-----|-------|
| 1 | Пробуждённое Ядро | 1,000 | 1 | 1.0 | 1.1 | 1.0 | ✗ | ✗ | ✗ | ✗ |
| 2 | Течение Жизни | 2,594 | 2 | 1.0 | 2.0 | 1.2 | ✗ | ✗ | ✗ | ✗ |
| 3 | Пламя Внутреннего Огня | 6,727 | 4 | 0.9 | 3.0 | 1.5 | ✗ | ✗ | ✗ | ✗ |
| 4 | Объединение Тела и Духа | 17,450 | 8 | 0.4 | 5.0 | 2.0 | ✓ | ✗ | ✗ | ✗ |
| 5 | Сердце Небес | 45,260 | 16 | 0.3 | 8.0 | 3.0 | ✓ | ✓ | ✗ | ✗ |
| 6 | Разрыв Пелены | 117,390 | 32 | 0.1 | 15.0 | 5.0 | ✓ | ✓ | ✓ | ✗ |
| 7 | Вечное Кольцо | 304,480 | 64 | 0.0 | 30.0 | 8.0 | ✓ | ✓ | ✓ | ✓ |
| 8 | Глас Небес | 789,750 | 128 | 0.0 | 100.0 | 15.0 | ✓ | ✓ | ✓ | ✓ |
| 9 | Бессмертное Ядро | 2,048,400 | 256 | 0.0 | 1000.0 | 50.0 | ✓ | ✓ | ✓ | ✓ |
| 10 | Вознесение | 5,313,000 | 512 | 0.0 | ∞ | 100.0 | ✓ | ✓ | ✓ | ✓ |

---

## Runtime Methods (из CultivationLevelData.cs)

### GetQiForSubLevelBreakthrough(long currentCoreCapacity)
```csharp
public long GetQiForSubLevelBreakthrough(long currentCoreCapacity)
{
    if (useDynamicBreakthroughCalculation)
        return currentCoreCapacity * subLevelMultiplier;  // coreCapacity × 10
    return qiForSubLevelBreakthrough;  // статическое значение
}
```

### GetQiForLevelBreakthrough(long currentCoreCapacity)
```csharp
public long GetQiForLevelBreakthrough(long currentCoreCapacity)
{
    if (useDynamicBreakthroughCalculation)
        return currentCoreCapacity * levelMultiplier;  // coreCapacity × 100
    return qiForLevelBreakthrough;  // статическое значение
}
```

---

## Примеры расчёта Ци для прорыва

### Уровень 4, Normal качество, subLevel = 0
```
coreCapacity = 1000 × 1.0 × 1.1^((4-1) × 10 + 0) = 1000 × 17.45 = 17450
qiForSubLevelBreakthrough = 17450 × 10 = 174500
qiForLevelBreakthrough = 17450 × 100 = 1745000
```

### Уровень 4, Refined качество, subLevel = 5
```
coreCapacity = 1000 × 1.2 × 1.1^(30 + 5) = 1200 × 28.1 = 33720
qiForSubLevelBreakthrough = 33720 × 10 = 337200
qiForLevelBreakthrough = 33720 × 100 = 3372000
```

---

*Документ создан: 2026-03-30*
*Обновлено: 2026-04-01 — данные синхронизированы с CultivationLevelData.cs*

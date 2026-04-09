# Настройка MortalStageData

**Путь:** `Assets/Data/MortalStages/`

**Создание:** Правый клик → Create → Cultivation → Mortal Stage

---

## Структура полей (из MortalStageData.cs)

### Basic Info
| Поле | Тип | Описание |
|------|-----|----------|
| stage | enum (MortalStage) | Этап развития |
| nameRu | string | Название на русском |
| nameEn | string | Название на английском |
| description | text | Описание этапа |

### Age Range
| Поле | Тип | Описание |
|------|-----|----------|
| minAge | int (0-100) | Минимальный возраст (лет) |
| maxAge | int (0-150) | Максимальный возраст (лет) |

### Dormant Core Formation
| Поле | Тип | Описание |
|------|-----|----------|
| minCoreFormation | float (%) | Мин. сформированность ядра |
| maxCoreFormation | float (%) | Макс. сформированность ядра |
| coreFormationRate | float | Скорость формирования (% в год) |

### Qi Capacity
| Поле | Тип | Описание |
|------|-----|----------|
| minQiCapacity | int (1-500) | Мин. ёмкость Ци |
| maxQiCapacity | int (1-500) | Макс. ёмкость Ци |
| qiAbsorptionRate | float (0-1) | Поглощение Ци из среды |
| canRegenerateQi | bool | Может регенерировать Ци |

### Base Stats Range
| Поле | Тип | Описание |
|------|-----|----------|
| minStrength | float (0-20) | Мин. сила |
| maxStrength | float (0-20) | Макс. сила |
| minAgility | float (0-20) | Мин. ловкость |
| maxAgility | float (0-20) | Макс. ловкость |
| minConstitution | float (0-20) | Мин. выносливость |
| maxConstitution | float (0-20) | Макс. выносливость |

### Awakening
| Поле | Тип | Описание |
|------|-----|----------|
| baseAwakeningChance | float (%) | Базовый шанс пробуждения |
| highDensityAwakeningChance | float (%) | Шанс в зоне высокой плотности Ци |
| criticalAwakeningChance | float (%) | Шанс при критическом состоянии |
| canAwaken | bool | Можно пробудиться |

### Abilities & Limitations
| Поле | Тип | Описание |
|------|-----|----------|
| requiresFood | bool | Требуется еда |
| requiresWater | bool | Требуется вода |
| requiresSleep | bool | Требуется сон |
| canLearnMartialArts | bool | Может учить боевые искусства |
| canMeditate | bool | Может медитировать |
| abilitiesDescription | text | Описание способностей |

### Transition
| Поле | Тип | Описание |
|------|-----|----------|
| nextStage | enum (MortalStage) | Следующий этап (None = последний) |
| isAwakeningPoint | bool | Точка пробуждения |

---

## Enum MortalStage (из Enums.cs)

```csharp
public enum MortalStage
{
    None = 0,           // Не применимо (практик)
    Newborn = 1,        // Новорождённый (0-7 лет)
    Child = 2,          // Ребёнок (7-16 лет)
    Adult = 3,          // Взрослый (16-30 лет)
    Mature = 4,         // Зрелый (30-50 лет)
    Elder = 5,          // Старец (50+ лет)
    Awakening = 9       // Точка пробуждения
}
```

---

## Этап 1: Newborn (Новорождённый)

**Имя файла:** `Stage1_Newborn`

```
=== Basic Info ===
stage: Newborn
nameRu: Новорождённый
nameEn: Newborn
description: Первый этап жизни. Дремлющее ядро только начинает формироваться.

=== Age Range ===
minAge: 0
maxAge: 7

=== Dormant Core Formation ===
minCoreFormation: 0
maxCoreFormation: 30
coreFormationRate: 4

=== Qi Capacity ===
minQiCapacity: 1
maxQiCapacity: 30
qiAbsorptionRate: 0.01
canRegenerateQi: false

=== Base Stats Range ===
minStrength: 0.1
maxStrength: 5
minAgility: 0.1
maxAgility: 5
minConstitution: 0.1
maxConstitution: 5

=== Awakening ===
baseAwakeningChance: 0
highDensityAwakeningChance: 0
criticalAwakeningChance: 0
canAwaken: false

=== Abilities & Limitations ===
requiresFood: true
requiresWater: true
requiresSleep: true
canLearnMartialArts: false
canMeditate: false
abilitiesDescription: Полная зависимость от родителей. Естественное поглощение Ци через питание.

=== Transition ===
nextStage: Child
isAwakeningPoint: false
```

---

## Этап 2: Child (Ребёнок)

**Имя файла:** `Stage2_Child`

```
=== Basic Info ===
stage: Child
nameRu: Ребёнок
nameEn: Child
description: Период активного роста. Ядро формируется быстрее.

=== Age Range ===
minAge: 7
maxAge: 16

=== Dormant Core Formation ===
minCoreFormation: 30
maxCoreFormation: 60
coreFormationRate: 3.5

=== Qi Capacity ===
minQiCapacity: 30
maxQiCapacity: 100
qiAbsorptionRate: 0.02
canRegenerateQi: false

=== Base Stats Range ===
minStrength: 2
maxStrength: 8
minAgility: 2
maxAgility: 10
minConstitution: 2
maxConstitution: 8

=== Awakening ===
baseAwakeningChance: 0.001
highDensityAwakeningChance: 0.01
criticalAwakeningChance: 0.1
canAwaken: false

=== Abilities & Limitations ===
requiresFood: true
requiresWater: true
requiresSleep: true
canLearnMartialArts: true
canMeditate: true
abilitiesDescription: Начало обучения. Пик пластичности тела. Можно начать боевые искусства.

=== Transition ===
nextStage: Adult
isAwakeningPoint: false
```

---

## Этап 3: Adult (Взрослый)

**Имя файла:** `Stage3_Adult`

```
=== Basic Info ===
stage: Adult
nameRu: Взрослый
nameEn: Adult
description: Полноценный взрослый. Ядро почти сформировано. Оптимальное время для пробуждения.

=== Age Range ===
minAge: 16
maxAge: 30

=== Dormant Core Formation ===
minCoreFormation: 60
maxCoreFormation: 90
coreFormationRate: 2

=== Qi Capacity ===
minQiCapacity: 100
maxQiCapacity: 200
qiAbsorptionRate: 0.03
canRegenerateQi: false

=== Base Stats Range ===
minStrength: 8
maxStrength: 15
minAgility: 8
maxAgility: 15
minConstitution: 8
maxConstitution: 15

=== Awakening ===
baseAwakeningChance: 0.01
highDensityAwakeningChance: 0.1
criticalAwakeningChance: 1
canAwaken: true

=== Abilities & Limitations ===
requiresFood: true
requiresWater: true
requiresSleep: true
canLearnMartialArts: true
canMeditate: true
abilitiesDescription: Пик физических возможностей. Лучшее время для пробуждения ядра.

=== Transition ===
nextStage: Mature
isAwakeningPoint: false
```

---

## Этап 4: Mature (Зрелый)

**Имя файла:** `Stage4_Mature`

```
=== Basic Info ===
stage: Mature
nameRu: Зрелый
nameEn: Mature
description: Зрелый возраст. Ядро полностью сформировано. Ещё можно пробудиться.

=== Age Range ===
minAge: 30
maxAge: 50

=== Dormant Core Formation ===
minCoreFormation: 90
maxCoreFormation: 100
coreFormationRate: 0.5

=== Qi Capacity ===
minQiCapacity: 80
maxQiCapacity: 150
qiAbsorptionRate: 0.02
canRegenerateQi: false

=== Base Stats Range ===
minStrength: 8
maxStrength: 15
minAgility: 6
maxAgility: 12
minConstitution: 10
maxConstitution: 18

=== Awakening ===
baseAwakeningChance: 0.1
highDensityAwakeningChance: 1
criticalAwakeningChance: 5
canAwaken: true

=== Abilities & Limitations ===
requiresFood: true
requiresWater: true
requiresSleep: true
canLearnMartialArts: true
canMeditate: true
abilitiesDescription: Мудрость и опыт. Тело начинает медленно деградировать.

=== Transition ===
nextStage: Elder
isAwakeningPoint: false
```

---

## Этап 5: Elder (Старец)

**Имя файла:** `Stage5_Elder`

```
=== Basic Info ===
stage: Elder
nameRu: Старец
nameEn: Elder
description: Пожилой возраст. Ядро начинает угасать. Окно пробуждения закрывается.

=== Age Range ===
minAge: 50
maxAge: 100

=== Dormant Core Formation ===
minCoreFormation: 50
maxCoreFormation: 100
coreFormationRate: -1

=== Qi Capacity ===
minQiCapacity: 30
maxQiCapacity: 80
qiAbsorptionRate: 0.01
canRegenerateQi: false

=== Base Stats Range ===
minStrength: 4
maxStrength: 10
minAgility: 3
maxAgility: 8
minConstitution: 5
maxConstitution: 12

=== Awakening ===
baseAwakeningChance: 0.005
highDensityAwakeningChance: 0.05
criticalAwakeningChance: 0.5
canAwaken: true

=== Abilities & Limitations ===
requiresFood: true
requiresWater: true
requiresSleep: true
canLearnMartialArts: false
canMeditate: true
abilitiesDescription: Угасание тела. Духовная мудрость. Сложно пробудиться.

=== Transition ===
nextStage: None
isAwakeningPoint: false
```

---

## Этап 6: Awakening (Пробуждение)

**Имя файла:** `Stage9_Awakening`

```
=== Basic Info ===
stage: Awakening
nameRu: Пробуждение
nameEn: Awakening
description: Точка перехода. Готовность к пробуждению ядра Ци.

=== Age Range ===
minAge: 16
maxAge: 50

=== Dormant Core Formation ===
minCoreFormation: 80
maxCoreFormation: 100
coreFormationRate: 0

=== Qi Capacity ===
minQiCapacity: 100
maxQiCapacity: 200
qiAbsorptionRate: 0.05
canRegenerateQi: false

=== Base Stats Range ===
minStrength: 8
maxStrength: 15
minAgility: 8
maxAgility: 15
minConstitution: 8
maxConstitution: 15

=== Awakening ===
baseAwakeningChance: 1
highDensityAwakeningChance: 5
criticalAwakeningChance: 10
canAwaken: true

=== Abilities & Limitations ===
requiresFood: true
requiresWater: true
requiresSleep: true
canLearnMartialArts: true
canMeditate: true
abilitiesDescription: Ядро готово к пробуждению. Высокий шанс успеха при правильных условиях.

=== Transition ===
nextStage: None
isAwakeningPoint: true
```

---

## Сводная таблица

| stage | nameRu | age | core% | qi | canAwaken | food | water |
|-------|--------|-----|-------|----|-----------|------|-------|
| Newborn (1) | Новорождённый | 0-7 | 0-30 | 1-30 | ✗ | ✓ | ✓ |
| Child (2) | Ребёнок | 7-16 | 30-60 | 30-100 | ✗ | ✓ | ✓ |
| Adult (3) | Взрослый | 16-30 | 60-90 | 100-200 | ✓ | ✓ | ✓ |
| Mature (4) | Зрелый | 30-50 | 90-100 | 80-150 | ✓ | ✓ | ✓ |
| Elder (5) | Старец | 50+ | 50-100 | 30-80 | ✓ | ✓ | ✓ |
| Awakening (9) | Пробуждение | 16-50 | 80-100 | 100-200 | ✓ | ✓ | ✓ |

---

## Runtime Methods (из MortalStageData.cs)

### GetRandomCoreFormation()
```csharp
public float GetRandomCoreFormation()
{
    return UnityEngine.Random.Range(minCoreFormation, maxCoreFormation);
}
```

### GetRandomQiCapacity()
```csharp
public int GetRandomQiCapacity()
{
    return UnityEngine.Random.Range(minQiCapacity, maxQiCapacity + 1);
}
```

### CalculateAwakeningChance(float coreFormation, bool highDensity, bool critical)
```csharp
public float CalculateAwakeningChance(float coreFormation, bool highDensity, bool critical)
{
    if (!canAwaken || coreFormation < 80f)
        return 0f;

    float chance = baseAwakeningChance;

    if (highDensity)
        chance = Mathf.Max(chance, highDensityAwakeningChance);

    if (critical)
        chance = Mathf.Max(chance, criticalAwakeningChance);

    // Бонус за высокую сформированность ядра
    if (coreFormation >= 90f)
        chance *= 1.5f;

    return chance;
}
```

### IsAgeInRange(int age)
```csharp
public bool IsAgeInRange(int age)
{
    return age >= minAge && age <= maxAge;
}
```

### GetCoreFormationForAge(int age)
```csharp
public float GetCoreFormationForAge(int age)
{
    if (age < minAge) return minCoreFormation;
    if (age > maxAge) return maxCoreFormation;

    float progress = (float)(age - minAge) / (maxAge - minAge);
    return Mathf.Lerp(minCoreFormation, maxCoreFormation, progress);
}
```

---

*Документ создан: 2026-03-30*
*Обновлено: 2026-04-01 — данные синхронизированы с MortalStageData.cs и Enums.cs*

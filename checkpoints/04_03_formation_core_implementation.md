# Чекпоинт: Реализация ядер формаций

**Дата:** 2026-04-03
**Статус:** ✅ COMPLETE
**Связанные документы:**
- docs/FORMATION_SYSTEM.md
- docs_old/formation_unified.md
- docs_old/formation_analysis.md

---

## 📋 Выполненные задачи

### Создание FormationCoreData.cs

Реализованы физические носители формаций согласно старой документации (formation_unified.md).

**Файл:** `Scripts/Data/ScriptableObjects/FormationCoreData.cs` (~290 строк)

---

## 📐 Реализованные enums

### FormationCoreType

```csharp
public enum FormationCoreType
{
    Disk,           // Диск (портативный) — L1-L6
    Altar,          // Алтарь (стационарный) — L5-L9
    Array,          // Массив (напольный)
    Totem,          // Тотем
    Seal            // Печать
}
```

### FormationCoreVariant (Материал)

```csharp
public enum FormationCoreVariant
{
    Stone,          // Камень — базовый
    Jade,           // Нефрит — L2-L4
    Iron,           // Железо — L3-L5
    SpiritIron,     // Духовное железо — L4-L6
    Crystal,        // Кристалл — L6-L7
    StarMetal,      // Звёздный металл — L7-L8
    VoidMatter      // Пустотная материя — L8-L9
}
```

### FormationType

```csharp
public enum FormationType
{
    Barrier,        // Барьер (защита)
    Trap,           // Ловушка (атака)
    Amplification,  // Усиление (баффы)
    Suppression,    // Подавление (дебаффы)
    Gathering,      // Сбор (ресурсы)
    Detection,      // Обнаружение (сенсор)
    Teleportation,  // Телепортация
    Summoning       // Призыв
}
```

### FormationSize

```csharp
public enum FormationSize
{
    Small,          // 3x3 метра
    Medium,         // 10x10 метров
    Large,          // 30x30 метров
    Great,          // 100x100 метров
    Heavy           // 300x300 метров (города)
}
```

---

## 🔮 Дисковые ядра (L1-L6)

| Тип | Материал | Уровни | Слоты | Проводимость |
|-----|----------|--------|-------|--------------|
| Каменный диск | Stone | L1-L2 | 1 | 5 |
| Нефритовый диск | Jade | L2-L4 | 1 | 10 |
| Железный диск | Iron | L3-L5 | 2 | 15 |
| Духовно-железный диск | SpiritIron | L4-L6 | 3 | 25 |

**Характеристики:**
- Переносные
- Малая ёмкость
- 1-3 слота для камней Ци
- Проводимость: 5-25 ед/сек

---

## 🏛️ Алтарные ядра (L5-L9)

| Тип | Материал | Уровни | Слоты | Проводимость |
|-----|----------|--------|-------|--------------|
| Нефритовый алтарь | Jade | L5-L6 | 3 | 40 |
| Кристаллический алтарь | Crystal | L6-L7 | 5 | 55 |
| Духовно-кристаллический алтарь | SpiritCrystal | L7-L8 | 8 | 75 |
| Алтарь из кости дракона | DragonBone | L8-L9 | 10 | 100 |

**Характеристики:**
- Стационарные (требуют монтаж)
- Большая ёмкость
- 3-10 слотов для камней Ци
- Проводимость: 40-100 ед/сек
- L8+ можно добавить контур сбора Ци

---

## 📊 FormationCoreData поля

### Основные параметры
- `coreId` — уникальный ID
- `nameRu/nameEn` — названия
- `description` — описание
- `icon` — иконка

### Классификация
- `coreType` — тип ядра (Disk/Altar/...)
- `variant` — материал
- `rarity` — редкость

### Ёмкость
- `levelMin/levelMax` — диапазон уровней формаций
- `maxSlots` — слоты для камней Ци
- `baseConductivity` — проводимость (ед/сек)
- `maxCapacity` — максимальная ёмкость

### Совместимость
- `supportedTypes` — поддерживаемые типы формаций
- `recommendedType` — рекомендуемый тип
- `supportedSizes` — поддерживаемые размеры
- `maxSize` — максимальный размер

### Прочность
- `maxDurability` — максимальная прочность
- `durabilityPerActivation` — расход при активации
- `durabilityPerHour` — расход при поддержании

---

## 🔧 Runtime методы

```csharp
// Проверка совместимости
bool SupportsType(FormationType type)
bool SupportsSize(FormationSize size)

// Расчёты
float GetEffectMultiplier(int level)
long CalculateQiCost(FormationType type, FormationSize size, int level)
```

---

## 📁 Структура файла

```
Scripts/Data/ScriptableObjects/
└── FormationCoreData.cs
    ├── FormationCoreType enum
    ├── FormationCoreVariant enum
    ├── FormationType enum
    ├── FormationSize enum
    ├── FormationCoreData class
    ├── QiStoneSlot class
    └── QiStoneType enum
```

---

## 🎯 Соответствие старой документации

| Старый документ | Реализация |
|-----------------|------------|
| formation_unified.md §ЯДРА | FormationCoreData.cs |
| Дисковые ядра L1-L6 | FormationCoreType.Disk |
| Алтарные ядра L5-L9 | FormationCoreType.Altar |
| Проводимость 5-100 | baseConductivity field |
| Слоты 1-10 | maxSlots field |
| Материалы 8 тиров | FormationCoreVariant enum |

---

## ✅ Следующие шаги

1. Создать .asset файлы ядер в Unity Editor
2. Создать FormationController.cs для управления формациями
3. Интегрировать с Charger System для подпитки
4. Добавить UI для работы с формациями

---

*Чекпоинт создан: 2026-04-03*

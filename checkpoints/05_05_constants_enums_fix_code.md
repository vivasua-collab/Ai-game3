# 📦 Кодовая база: Исправление констант и Enum

👉 Основной план: [05_05_constants_enums_fix.md](05_05_constants_enums_fix.md)
**Дата:** 2026-05-05 09:45:07 UTC
**Верификация:** 2026-05-05 14:06:23 MSK
**Статус:** complete ✅

---

## К-01: TechniqueGradeMultipliers

**Файл:** `Scripts/Core/Constants.cs` строки 332–338

**Сейчас:**
```csharp
{ TechniqueGrade.Common, 1.0f },
{ TechniqueGrade.Refined, 1.2f },
{ TechniqueGrade.Perfect, 1.4f },
{ TechniqueGrade.Transcendent, 1.6f }
```

**Должно быть (TECHNIQUE_SYSTEM.md):**
```csharp
{ TechniqueGrade.Common, 1.0f },
{ TechniqueGrade.Refined, 1.3f },
{ TechniqueGrade.Perfect, 1.6f },
{ TechniqueGrade.Transcendent, 2.0f }
```

**Зависимость:** Обновить комментарии в `Scripts/Combat/TechniqueCapacity.cs` строки 30–37

---

## К-02: ULTIMATE_DAMAGE_MULTIPLIER

**Файл:** `Scripts/Core/Constants.cs` строка 358

**Сейчас:**
```csharp
public const float ULTIMATE_DAMAGE_MULTIPLIER = 1.3f;
```

**Должно быть (COMBAT_SYSTEM.md, ALGORITHMS.md):**
```csharp
public const float ULTIMATE_DAMAGE_MULTIPLIER = 2.0f;
```

**Зависимость:** Обновить комментарий в TechniqueCapacity.cs строка 37

---

## К-03: GetEffectivenessMultiplier

**Файл:** `Scripts/Inventory/EquipmentController.cs` строки 779–786

**Сейчас:**
```csharp
EquipmentGrade.Damaged => 0.5f,
EquipmentGrade.Common => 1.0f,
EquipmentGrade.Refined => 1.4f,       // (1.3 + 1.5) / 2
EquipmentGrade.Perfect => 2.1f,       // (1.7 + 2.5) / 2
EquipmentGrade.Transcendent => 3.25f, // (2.5 + 4.0) / 2
```

**Должно быть (EQUIPMENT_SYSTEM.md §2.1):**
```csharp
EquipmentGrade.Damaged => 0.5f,
EquipmentGrade.Common => 1.0f,
EquipmentGrade.Refined => 1.3f,
EquipmentGrade.Perfect => 1.6f,
EquipmentGrade.Transcendent => 2.0f,
```

---

## К-04: Добавить Light в Element enum

**Файл:** `Scripts/Core/Enums.cs` строки 92–103

**Сейчас:**
```csharp
public enum Element
{
    Neutral, Fire, Water, Earth, Air, Lightning, Void, Poison
}
```

**Должно быть:**
```csharp
public enum Element
{
    Neutral, Fire, Water, Earth, Air, Lightning, Void, Light, Poison
}
```

**Дополнительные изменения:**

1. `Scripts/Core/Constants.cs` OppositeElements — добавить:
```csharp
{ Element.Light, Element.Void },
// И обратную пару
{ Element.Void, Element.Light }, // Void уже имеет Lightning, нужен список
```

2. `Scripts/Combat/DamageCalculator.cs` CalculateElementalInteraction — добавить:
```csharp
// Light → Poison ×1.2 (очищение)
if (attackerElement == Element.Light && defenderElement == Element.Poison)
    return 1.2f;
```

3. Создать `ElementData` SO для Light

---

## К-05: Защитный код для Poison

**Файл:** `Scripts/Combat/DamageCalculator.cs` CalculateElementalInteraction

**Добавить в начало метода:**
```csharp
// Poison — НЕ стихия атаки, не имеет стихийных взаимодействий
if (attackerElement == Element.Poison) return 1.0f;
```

---

## В-16: QiStoneQuality — выровнять с EquipmentGrade

**Файл:** `Scripts/Charger/ChargerSlot.cs` строки 19–25

**Сейчас:**
```csharp
public enum QiStoneQuality { Raw, Refined, Perfect, Transcendent }
```

**Должно быть:**
```csharp
public enum QiStoneQuality { Damaged, Common, Refined, Perfect, Transcendent }
```

**Обновить QiStone.InitializeStats():** Добавить множитель для Damaged (0.5f), переименовать Raw→Common (1.0f)

---

## С-01: DurabilityCondition — убрать Excellent

**Файл:** `Scripts/Core/Enums.cs` строки 402–410

**Сейчас (6 состояний):**
```csharp
Pristine, Excellent, Good, Worn, Damaged, Broken
```

**Должно быть (5 состояний, EQUIPMENT_SYSTEM.md §4.1):**
```csharp
Pristine, Good, Worn, Damaged, Broken
```

**Пороги и эффективность:**

| Состояние | Прочность | Эффективность |
|-----------|-----------|---------------|
| Pristine | 100% | 100% |
| Good | 80-99% | 95% |
| Worn | 60-79% | 85% |
| Damaged | 20-59% | 60% |
| Broken | <20% | 20% |

**Затронутые файлы:**
- `Scripts/Core/Enums.cs` — убрать Excellent из DurabilityCondition
- `Scripts/Core/Constants.cs` — обновить DurabilityRanges и DurabilityEfficiency
- `Scripts/Inventory/InventoryController.cs` — обновить GetCondition()
- `Scripts/Inventory/EquipmentController.cs` — обновить если использует DurabilityCondition

---

## С-03: Физические константы QiBuffer → Constants.cs

**Файл:** `Scripts/Combat/QiBuffer.cs` строки 77–86

**Добавить в `Scripts/Core/Constants.cs`:**
```csharp
// Физический Qi Buffer (проверено верификацией: QiBuffer.cs строки 77-86)
public const float PHYSICAL_RAW_QI_ABSORPTION = 0.8f;
public const float PHYSICAL_RAW_QI_PIERCING = 0.2f;
public const float PHYSICAL_RAW_QI_RATIO = 5.0f;
public const float PHYSICAL_SHIELD_QI_RATIO = 2.0f;
```

**Заменить в QiBuffer.cs** локальные константы на ссылки:
```csharp
PHYSICAL_RAW_ABSORPTION → GameConstants.PHYSICAL_RAW_QI_ABSORPTION
// и т.д.
```

**Также удалить дублирующиеся Qi-техник константы из QiBuffer.cs** (уже в Constants.cs):
```csharp
// Удалить из QiBuffer.cs:
QI_TECHNIQUE_RAW_ABSORPTION = 0.9f;  // → GameConstants.RAW_QI_ABSORPTION
QI_TECHNIQUE_RAW_PIERCING = 0.1f;    // → GameConstants.RAW_QI_PIERCING
QI_TECHNIQUE_RAW_RATIO = 3.0f;       // → GameConstants.RAW_QI_RATIO
QI_TECHNIQUE_SHIELD_RATIO = 1.0f;    // → GameConstants.SHIELD_QI_RATIO
```

---

## Н-11: basePlayerQi = 100 → 1000

**Файл:** `Scripts/Core/GameSettings.cs` строка 52

**Сейчас:**
```csharp
public long basePlayerQi = 100;
```

**Должно быть:**
```csharp
public long basePlayerQi = 1000;
```

(Соответствует Constants.BASE_CORE_CAPACITY = 1000 и GLOSSARY.md)

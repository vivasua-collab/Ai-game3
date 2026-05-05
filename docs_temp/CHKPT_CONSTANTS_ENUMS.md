# 🔴 Чекпоинт: Константы и Enum — план исправления

**Дата:** 2026-05-08 | **Статус:** ПЛАН | **Приоритет:** КРИТИЧЕСКИЙ
**Связанный аудит:** AUDIT_COMBAT_EQUIPMENT_v3.md (К-01, К-02, К-03, К-04, К-05, В-16, С-01, Н-11, С-03)

---

## Проблемы (все верифицированы ✅)

### К-01: TechniqueGradeMultipliers
- **Файл:** `Core/Constants.cs` строки 332–338
- **Сейчас:** `{Common:1.0, Refined:1.2, Perfect:1.4, Transcendent:1.6}`
- **Должно быть (TECHNIQUE_SYSTEM.md):** `{Common:1.0, Refined:1.3, Perfect:1.6, Transcendent:2.0}`
- **Исправление:**
  ```csharp
  { TechniqueGrade.Refined, 1.3f },    // было 1.2f
  { TechniqueGrade.Perfect, 1.6f },    // было 1.4f
  { TechniqueGrade.Transcendent, 2.0f }, // было 1.6f
  ```
- **Зависимости:** Обновить комментарии в TechniqueCapacity.cs строки 30–37

### К-02: ULTIMATE_DAMAGE_MULTIPLIER
- **Файл:** `Core/Constants.cs` строка 358
- **Сейчас:** `1.3f`
- **Должно быть (COMBAT_SYSTEM.md, ALGORITHMS.md):** `2.0f`
- **Исправление:**
  ```csharp
  public const float ULTIMATE_DAMAGE_MULTIPLIER = 2.0f; // было 1.3f
  ```

### К-03: GetEffectivenessMultiplier
- **Файл:** `Inventory/EquipmentController.cs` строки 779–786
- **Сейчас:** `{Damaged:0.5, Common:1.0, Refined:1.4, Perfect:2.1, Transcendent:3.25}`
- **Должно быть (EQUIPMENT_SYSTEM.md §2.1):** `{Damaged:0.5, Common:1.0, Refined:1.3, Perfect:1.6, Transcendent:2.0}`
- **Исправление:**
  ```csharp
  EquipmentGrade.Refined => 1.3f,       // было 1.4f (среднее диапазона)
  EquipmentGrade.Perfect => 1.6f,       // было 2.1f
  EquipmentGrade.Transcendent => 2.0f,  // было 3.25f
  ```
- **Примечание:** Код использовал средние значения диапазонов. Документация даёт конкретные значения — они приоритетнее.

### К-04: Element enum — добавить Light
- **Файл:** `Core/Enums.cs` строки 92–103
- **Сейчас:** `Neutral, Fire, Water, Earth, Air, Lightning, Void, Poison`
- **Должно быть (ALGORITHMS.md §10.1):** + `Light`
- **Исправление:**
  1. Добавить `Light` в enum Element (перед Poison)
  2. Добавить `Light ↔ Void` в `Constants.cs OppositeElements`
  3. Добавить `Light → Poison ×1.2` в `DamageCalculator.CalculateElementalInteraction`
  4. Добавить `ElementData` ScriptableObject для Light
  5. Обновить `ElementData` — заменить `oppositeElement` на `List<Element> oppositeElements` (см. В-12)

### К-05: Poison в Element enum
- **Файл:** `Core/Enums.cs` строка 101
- **Проблема:** ALGORITHMS.md: «Poison — НЕ стихия, а состояние Ци»
- **Варианты исправления:**
  - **Вариант А (рекомендуемый):** Оставить Poison в enum, но добавить защитный код в DamageCalculator:
    ```csharp
    if (attackerElement == Element.Poison) return 1.0f; // Poison не атакует как стихия
    ```
  - **Вариант Б:** Убрать Poison из Element, создать отдельный PoisonType enum
  - **Риск Варианта Б:** Много рефакторинга (все switch на Element, сериализаторы, SO)
- **Рекомендация:** Вариант А — минимальный риск

### В-16: QiStoneQuality не совпадает с EquipmentGrade
- **Файл:** `Charger/ChargerSlot.cs` строки 19–25
- **Сейчас:** `{Raw, Refined, Perfect, Transcendent}` (4 значения)
- **Должно быть:** `{Damaged, Common, Refined, Perfect, Transcendent}` (5 значений, как EquipmentGrade)
- **Исправление:**
  1. Заменить `Raw` на `Common`
  2. Добавить `Damaged` (перед Common)
  3. Обновить QiStone.InitializeStats() — добавить множитель для Damaged (0.5f)

### С-01: DurabilityCondition — 6 состояний вместо 5
- **Файлы:** `Core/Enums.cs` строки 402–410, `Core/Constants.cs` DurabilityRanges/DurabilityEfficiency, `Inventory/InventoryController.cs`
- **Сейчас:** Pristine, Excellent, Good, Worn, Damaged, Broken
- **Должно быть (EQUIPMENT_SYSTEM.md §4.1):** Pristine, Good, Worn, Damaged, Broken
- **Исправление:**
  1. Удалить `Excellent` из enum DurabilityCondition
  2. Сместить пороги: Good=80-99%, Worn=60-79%, Damaged=20-59%, Broken=<20%
  3. Обновить эффективность: Good=95%, Worn=85%, Damaged=60%
  4. Обновить Constants.cs, InventoryController.GetCondition(), EquipmentController

### С-03: QiBuffer физические константы не в Constants.cs
- **Файл:** `Combat/QiBuffer.cs` строки 77–86
- **Сейчас:** 4 локальных константы для физ. урона (0.8f, 0.2f, 5.0f, 2.0f)
- **Исправление:** Перенести в Constants.cs:
  ```csharp
  public const float PHYSICAL_RAW_QI_ABSORPTION = 0.8f;
  public const float PHYSICAL_RAW_QI_PIERCING = 0.2f;
  public const float PHYSICAL_RAW_QI_RATIO = 5.0f;
  public const float PHYSICAL_SHIELD_QI_RATIO = 2.0f;
  ```
- **Также:** Удалить дублирующиеся Qi-техник константы из QiBuffer.cs (уже есть в Constants.cs)

### Н-11: GameSettings basePlayerQi = 100 вместо 1000
- **Файл:** `Core/GameSettings.cs` строка 52
- **Сейчас:** `public long basePlayerQi = 100;`
- **Должно быть (GLOSSARY.md, Constants.cs BASE_CORE_CAPACITY):** `1000`
- **Исправление:** `public long basePlayerQi = 1000;`

---

## Порядок исправления

1. **К-01 + К-02** — простая замена чисел, ~5 мин
2. **К-03** — замена чисел + удалить комментарии о «средних значениях»
3. **К-04 + К-05** — добавить Light + защитный код Poison, ~30 мин
4. **В-16** — расширить enum QiStoneQuality, ~15 мин
5. **С-01** — убрать Excellent, сместить пороги, обновить 3 файла, ~30 мин
6. **С-03** — перенести константы в Constants.cs, ~10 мин
7. **Н-11** — заменить 100 на 1000, ~1 мин

**Итого:** ~90 мин работы

---

## Риски

- **К-04 (Light):** Добавление нового значения в enum может нарушить сериализацию существующих SO. Проверить все ElementData SO.
- **С-01 (Excellent):** Удаление значения из enum — breaking change для сохранений. Нужна миграция.
- **К-03 (Effectiveness):** Снижение множителей с 3.25→2.0 для Transcendent — значительный нерф. Проверить баланс.

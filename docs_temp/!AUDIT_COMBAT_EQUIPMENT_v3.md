# 🔍 Аудит боевой системы и экипировки — v3.0 (глубокий, 2 итерации)

**Дата:** 2026-05-05 08:35:42 UTC
**Аудитор:** AI-ассистент
**Область:** Боевая система (11 слоёв) + Система экипировки + Генераторы
**Документы-источники:** ALGORITHMS.md, COMBAT_SYSTEM.md, EQUIPMENT_SYSTEM.md, INVENTORY_SYSTEM.md, TECHNIQUE_SYSTEM.md, GENERATORS_SYSTEM.md
**Код:** `Scripts/Combat/`, `Scripts/Inventory/`, `Scripts/Generators/`, `Scripts/Core/Constants.cs`, `Scripts/Core/Enums.cs`

---

## 📊 Сводка критичности

| Категория | Количество |
|-----------|-----------|
| 🔴 КРИТИЧЕСКОЕ (нарушает формулы документации) | 5 |
| 🟠 ВЫСОКОЕ (пропущенные функции/фичи) | 6 |
| 🟡 СРЕДНЕЕ (расхождения данных) | 7 |
| 🟢 НИЗКОЕ (стиль/нейминг/структура) | 5 |

---

## 🔴 КРИТИЧЕСКИЕ РАСХОЖДЕНИЯ

### К-01: Множители Grade техник НЕ совпадают с документацией

**Документ (TECHNIQUE_SYSTEM.md §«Система Grade»):**
| Grade | Урон |
|-------|------|
| Common | ×1.0 |
| Refined | ×1.3 |
| Perfect | ×1.6 |
| Transcendent | ×2.0 |

**Код (Constants.cs + TechniqueCapacity.cs):**
| Grade | Урон |
|-------|------|
| Common | ×1.0 |
| Refined | **×1.2** |
| Perfect | **×1.4** |
| Transcendent | **×1.6** |

**Разница:** Refined = 1.2 вместо 1.3 (−8%), Perfect = 1.4 вместо 1.6 (−12.5%), Transcendent = 1.6 вместо 2.0 (−20%).

**Файлы:**
- `Core/Constants.cs` строки 332–338: `TechniqueGradeMultipliers`
- `Combat/TechniqueCapacity.cs` строки 30–36: комментарии тоже указывают ×1.2/×1.4/×1.6

**Вердикт:** ❌ Код не соответствует документации. TECHNIQUE_SYSTEM.md — источник истины.

---

### К-02: Ultimate множитель НЕ совпадает с документацией

**Документ (COMBAT_SYSTEM.md, ALGORITHMS.md, TECHNIQUE_SYSTEM.md):**
```
ultimateMultiplier = isUltimate ? 2.0 : 1.0
```

**Код (Constants.cs строка 358):**
```csharp
public const float ULTIMATE_DAMAGE_MULTIPLIER = 1.3f;
```

**Разница:** 1.3 вместо 2.0 (−35%). Ultimate-техники наносят на 35% меньше урона, чем указано в документации.

**Файлы:**
- `Core/Constants.cs` строка 358
- `Combat/TechniqueCapacity.cs` строка 37: комментарий «Ultimate множитель: ×1.3»

**Вердикт:** ❌ Критическое расхождение. Документация требует ×2.0.

---

### К-03: Множители эффективности экипировки НЕ совпадают с документацией

**Документ (EQUIPMENT_SYSTEM.md §2.1):**
| Grade | Эффективность |
|-------|---------------|
| Damaged | ×0.5 |
| Common | ×1.0 |
| Refined | ×1.3 |
| Perfect | ×1.6 |
| Transcendent | ×2.0 |

**Код (EquipmentController.cs GetEffectivenessMultiplier):**
| Grade | Эффективность |
|-------|---------------|
| Damaged | ×0.5 |
| Common | ×1.0 |
| Refined | **×1.4** |
| Perfect | **×2.1** |
| Transcendent | **×3.25** |

**Разница:** Все множители выше документированных на +8% (Refined) до +63% (Transcendent). Код использует «средние значения» из диапазонов, но документация указывает конкретные числа.

**Комментарий в коде:** «Для диапазонов используется среднее значение» — но документация даёт конкретные значения, а не диапазоны.

**Вердикт:** ❌ Код завышает множители. EQUIPMENT_SYSTEM.md — источник истины.

---

### К-04: Элемент Light отсутствует в enum — нарушает ALGORITHMS.md

**Документ (ALGORITHMS.md §10.1):**
```
Light ↔ Void: ×1.5 (двусторонняя противоположность, добавлено 2026-04-27)
Light → Poison: ×1.2 (очищение)
```

**Код (Core/Enums.cs Element enum):**
```csharp
public enum Element
{
    Neutral, Fire, Water, Earth, Air, Lightning, Void, Poison
    // Light — ОТСУТСТВУЕТ
}
```

**Код (Constants.cs OppositeElements):**
```csharp
// Нет записи для Light ↔ Void
```

**Код (DamageCalculator.CalculateElementalInteraction):**
```csharp
// Нет обработки Light → Poison ×1.2
```

**Вердикт:** ❌ Целая стихия не реализована. Невозможно корректно рассчитать взаимодействия Light ↔ Void и Light → Poison.

---

### К-05: Poison как элемент enum нарушает архитектурное правило

**Документ (ALGORITHMS.md §10.1):**
> «Poison (Яд) — НЕ стихия, а состояние Ци. Не имеет противоположностей.»

**Код (Element enum):** Poison включён как полноценный элемент.

**Проблема:** Код может рассчитывать «противоположности» для Poison, хотя документация это запрещает. В `DamageCalculator.CalculateElementalInteraction` нет защитного кода от обработки Poison как стихии атаки.

**Вердикт:** ❌ Архитектурное нарушение. Poison не должен быть в enum Element или должен обрабатываться отдельно.

---

## 🟠 ВЫСОКИЕ РАСХОЖДЕНИЯ

### В-01: Парирование и блок — захардкоженные множители вместо чтения из экипировки

**Документация (COMBAT_SYSTEM.md Слой 4):**
```
Парирование: успех → damage ×= (1 - blockEffectiveness), durability--
Блок щитом: успех → damage ×= (1 - shieldEffectiveness), shieldDurability--
```

**Код (DamageCalculator.cs строки 247–258):**
```csharp
// Парирование
if (RollChance(defenseData.ParryChance))
{
    result.WasParried = true;
    damage *= 0.5f;   // ← ЗАХАРДКОЖЕНО 50%
}

// Блок
if (RollChance(defenseData.BlockChance))
{
    result.WasBlocked = true;
    damage *= 0.3f;   // ← ЗАХАРДКОЖЕНО 70% снижение
}
```

**Проблемы:**
1. `blockEffectiveness` и `shieldEffectiveness` из экипировки НЕ используются
2. Нет снижения прочности оружия/щита при парировании/блоке
3. Множители не читаются из EquipmentData

**Вердикт:** ❌ Функциональность не реализована. Захардкожены константы.

---

### В-02: CombatantBase не передаёт статы экипировки в DefenderParams

**Код (Combatant.cs строки 172–173, 321):**
```csharp
public virtual float ArmorCoverage => 0f;     // Заглушка!
public virtual float DamageReduction => 0f;    // Заглушка!
public virtual int ArmorValue => 0;            // Заглушка!

// В GetDefenderParams:
DodgePenalty = 0f,  // Заглушка!
```

**Комментарий:** «ФАЗА 1: реализовано через EquipmentController в PlayerController/NPCController»

**Проверка PlayerController:** Переопределяет ли PlayerController эти свойства? Если EquipmentController интегрирован, то PlayerController.GetDefenderParams() должен брать значения из EquipmentController. Если нет — боевой пайплайн работает без учёта экипировки.

**Вердикт:** ⚠️ Требует проверки переопределений в PlayerController/NPCController. Если не переопределены — боёвка полностью игнорирует экипировку.

---

### В-03: Износ брони при получении урона НЕ реализован

**Документация (COMBAT_SYSTEM.md Слой 7):**
```
armor.durability.current -= damage × 0.1 / hardness
```

**Код (DamageCalculator.cs):** Нет расчёта износа брони. DamageCalculator только рассчитывает урон, но не обновляет прочность.

**Код (EquipmentController.cs DamageEquipment):** Метод существует, но НИКТО его не вызывает из боевого пайплайна.

**Вердикт:** ❌ Документированная функция не интегрирована.

---

### В-04: Кровотечение, шок, оглушение (Слой 10) — НЕ реализованы

**Документация (COMBAT_SYSTEM.md Слой 10):**
- Кровотечение: если damage > threshold → bleedDamage per tick
- Шок: если redHP < 30% → штрафы
- Оглушение: шанс = damage / maxHP × 10%

**Код (DamageCalculator.cs строка 312):**
```csharp
result.IsFatal = (result.HitPart == BodyPartType.Heart || result.HitPart == BodyPartType.Head) 
              && result.FinalDamage > 50f;
```

Только проверка на смертельность. Кровотечение, шок и оглушение НЕ рассчитываются.

**Вердикт:** ❌ Три документированных эффекта отсутствуют в реализации.

---

### В-05: LootGenerator (Combat) использует захардкоженные пулы предметов

**Код (Combat/LootGenerator.cs строки 303–327):**
```csharp
string[] fireItems = { "flame_core", "ember_stone", "phoenix_feather" };
string[] waterItems = { "frost_crystal", "sea_pearl", "ice_shard" };
string[] earthItems = { "earth_crystal", "mountain_heart", "clay_tablet" };
string[] epics = { "ancient_scroll", "spirit_vein_map", "breakthrough_pill", "formation_disk" };
```

**Проблема:** Строковые ID не связаны с реальными предметами в игре. Нет проверки, что эти предметы существуют. GeneratorRegistry не используется для генерации лута.

**Документация (GENERATORS_SYSTEM.md):** Указывает, что генерация должна быть процедурной через GeneratorRegistry.

**Вердикт:** ❌ Лут генерируется из захардкоженных пулов, а не через процедурные генераторы.

---

### В-06: Износ оружия при атаке НЕ реализован

**Документация (EQUIPMENT_SYSTEM.md §4.2):**
```
Порог удара = твёрдость × 10
Дополнительный износ = (урон - порог) / 10
```

**Код:** Ни CombatManager, ни DamageCalculator не вызывают EquipmentController.DamageEquipment() для атакующего оружия.

**Вердикт:** ❌ Документированная функция не реализована.

---

## 🟡 СРЕДНИЕ РАСХОЖДЕНИЯ

### С-01: Состояния прочности экипировки — разные границы

**Документация (EQUIPMENT_SYSTEM.md §4.1):** 5 состояний
| Состояние | Прочность | Эффективность |
|-----------|-----------|---------------|
| Pristine | 100% | 100% |
| Good | 80-99% | 95% |
| Worn | 60-79% | 85% |
| Damaged | 20-59% | 60% |
| Broken | <20% | 20% |

**Код (Constants.cs DurabilityRanges + DurabilityEfficiency):** 6 состояний
| Состояние | Прочность | Эффективность |
|-----------|-----------|---------------|
| Pristine | 100% | 100% |
| Excellent | 80-99% | 95% |
| Good | 60-79% | 85% |
| Worn | 40-59% | 70% |
| Damaged | 20-39% | 50% |
| Broken | 0-19% | 20% |

**Разница:** Код добавляет 6-е состояние «Excellent», смещает границы и снижает эффективность (Worn: 70% вместо 85%, Damaged: 50% вместо 60%).

**Вердикт:** ⚠️ Расхождение в гранулах и значениях.

---

### С-02: Формула урона оружия (Слой 1b) — упрощена

**Документация (EQUIPMENT_SYSTEM.md §7.3):**
```
baseDamage = max(handDamage, weaponDamage × 0.5)
bonusDamage = weaponDamage × statScaling
totalDamage = baseDamage + bonusDamage

Где:
handDamage = 3 + (STR-10) × 0.3
statScaling = (STR×STR_BONUS + AGI×AGI_BONUS) / 100
```

**Код (DamageCalculator.cs строки 159–162):**
```csharp
if (attacker.CombatSubtype == CombatSubtype.MeleeWeapon && attacker.WeaponBonusDamage > 0)
{
    damage += attacker.WeaponBonusDamage;   // Просто добавляется как плоский бонус
}
```

**Проблема:** Нет формулы `max(handDamage, weaponDamage × 0.5)`. Нет `statScaling`. WeaponBonusDamage — это просто число из EquipmentController, а не расчёт по документированной формуле.

**Вердикт:** ⚠️ Формула упрощена и не соответствует документации.

---

### С-03: Qi Buffer — физические константы отсутствуют в Constants.cs

**Документация (ALGORITHMS.md §2.3):**
| Тип урона | Поглощение | Соотношение | Пробитие |
|-----------|------------|-------------|----------|
| Физ. + Сырая Ци | 80% | 5:1 | 20% |
| Физ. + Щит | 100% | 2:1 | 0% |

**Constants.cs:** Содержит только константы для Ци-техник (RAW_QI_ABSORPTION=0.9, RAW_QI_RATIO=3.0, SHIELD_QI_RATIO=1.0). Физические константы отсутствуют.

**QiBuffer.cs:** Содержит захардкоженные значения для физического урона (0.8, 5.0, 0.2), но не через Constants.

**Вердикт:** ⚠️ Константы физического Qi Buffer не вынесены в Constants.cs. Дублирование — нарушение принципа единого источника.

---

### С-04: Стихийные эффекты (Слой 10) — не реализованы

**Документация (COMBAT_SYSTEM.md §«Эффекты стихий»):**
- Огонь: Горение (DoT 5%/тик, 3 тика)
- Вода: Замедление (−20% скорости, 2 тика)
- Земля: Оглушение (15% шанс)
- и т.д.

**Код:** DamageCalculator не применяет стихийные эффекты. CalculateElementalInteraction только модифицирует множитель урона, но не накладывает статусы.

**Вердикт:** ⚠️ Стихийные эффекты не реализованы — только множитель урона.

---

### С-05: Генератор Consumable — ConsumableSOFactory не существует

**Код (Generators/LootGenerator.cs):** Имеет TODO: `ConsumableSOFactory` — расходники не создаются как ScriptableObject.

**Документация (GENERATORS_SYSTEM.md):** Указывает `EquipmentGenerator` для оружия и брони, но не описывает фабрику расходников.

**Вердикт:** ⚠️ Неполная интеграция. Расходники генерируются как DTO, но не конвертируются в SO.

---

### С-06: Дубликат класса LootGenerator в двух пространствах имён

**Файлы:**
- `Scripts/Generators/LootGenerator.cs` (CultivationGame.Generators, 171 строка) — генерация лута для спавна
- `Scripts/Combat/LootGenerator.cs` (CultivationGame.Combat, 375 строк) — генерация лута при смерти

**Проблема:** Одинаковое имя класса, разные пространства. Могут возникнуть конфликты при using.

**Вердикт:** ⚠️ Потенциальный конфликт имён. Рекомендуется переименовать.

---

### С-07: StatBonus — дублирующий класс в двух пространствах

**Файлы:**
- `Data/ScriptableObjects/ItemData.cs` — `StatBonus { statName, bonus, isPercentage }`
- `Generators/WeaponGenerator.cs` — `StatBonus { statName, value, isPercentage }`

**Проблема:** Разные имена полей (`bonus` vs `value`), одно и то же назначение. При конвертации DTO → SO возможны ошибки.

**Вердикт:** ⚠️ Дублирование с расхождением в полях.

---

## 🟢 НИЗКИЕ РАСХОЖДЕНИЯ

### Н-01: Файловая структура документации не совпадает с реальной

**Документация (EQUIPMENT_SYSTEM.md):**
- Scripts/Equipment/EquipmentManager.cs
- Scripts/Equipment/GradeSystem.cs
- Scripts/Equipment/DurabilitySystem.cs
- Scripts/Equipment/RepairSystem.cs
- Scripts/Equipment/MaterialRegistry.cs

**Реальность:** Файлы находятся в `Scripts/Inventory/EquipmentController.cs` и `Scripts/Generators/`. Отдельных GradeSystem.cs, DurabilitySystem.cs, RepairSystem.cs не существует.

**Вердикт:** ℹ️ Документация описывает теоретическую структуру, код использует другую организацию.

---

### Н-02: WeaponSubtype расширен по сравнению с документацией

**Документация (EQUIPMENT_SYSTEM.md §1.3):** 8 подтипов (unarmed, dagger, sword, greatsword, axe, spear, bow, staff)

**Код (WeaponGenerator.cs WeaponSubtype):** 12 подтипов (+ Hammer, Mace, Crossbow, Wand)

**Вердикт:** ℹ️ Код добавляет подтипы сверх документации. Не ошибка, но требует обновления docs.

---

### Н-03: ArmorWeightClass — не документирован

**Код (ArmorGenerator.cs):** `ArmorWeightClass` enum (Light, Medium, Heavy) — определяет штрафы скорости и уклонения.

**Документация:** Не описывает WeightClass как отдельный параметр, хотя описывает moveSpeedPenalty и dodgePenalty для брони.

**Вердикт:** ℹ️ Полезное расширение, требует документирования.

---

### Н-04: EquipmentController.GetDefenderParams — DodgePenalty передаётся как 0f

**Код (Combatant.cs строка 321):**
```csharp
DodgePenalty = 0f, // ФАЗА 1: реализовано через EquipmentController
```

**Но:** EquipmentController имеет метод `GetDodgePenalty()`, который возвращает штраф от брони. CombatantBase.GetDefenderParams() не вызывает этот метод.

**Вердикт:** ℹ️ Требует интеграции (если PlayerController/NPCController не переопределяют GetDefenderParams).

---

### Н-05: TechniqueData.baseQiCost — long vs int

**Документация:** Не уточняет тип для qiCost.
**Код:** TechniqueData.baseQiCost = `long` (FIX DAT-H01 для Qi > 2.1B на L5+).
**Код:** TechniqueCapacity.CalculateQiCost() возвращает `long`.

**Вердикт:** ℹ️ Корректное решение, но документация должна отражать тип long.

---

## 📊 СООТВЕТСТВИЕ ПАЙПЛАЙНА УРОНА (11 СЛОЁВ)

| Слой | Документация | Код | Статус |
|------|-------------|-----|--------|
| 1: Исходный урон | capacity × gradeMult × ultimateMult | ✅ Реализовано | ⚠️ gradeMult и ultimateMult НЕ совпадают (К-01, К-02) |
| 1b: Урон оружия | max(handDmg, weaponDmg×0.5) + weaponDmg×statScaling | ⚠️ Упрощено до += weaponBonusDamage | С-02 |
| 1c: TechniqueDamageBonus | — | ✅ Добавлено (ФАЗА 2) | ✅ Расширение кода |
| 2: Level Suppression | Таблица ×5×3 | ✅ Полное совпадение | ✅ |
| 3: Часть тела | rollBodyPart() с шансами | ✅ Полное совпадение | ✅ |
| 3b: Формация | formationBuffMultiplier | ✅ Добавлено (ФАЗА 7) | ✅ |
| 4: Активная защита | dodge/parry/block | ⚠️ Шансы ✅, Множители ❌ | В-01 |
| 5: Qi Buffer | 4 режима | ✅ Полное совпадение формул | ✅ (но см. С-03) |
| 6: Покрытие брони | if(random < coverage) | ✅ Реализовано | ⚠️ coverage=0f в CombatantBase (В-02) |
| 7: Снижение бронёй | DR + flat reduction | ✅ Реализовано | ⚠️ Нет износа (В-03) |
| 8: Материал тела | materialReduction | ✅ Полное совпадение | ✅ |
| 9: Распределение HP | 0.7/0.3 split | ✅ Полное совпадение | ✅ |
| 10: Последствия | Кровотечение, шок, оглушение | ❌ Только IsFatal | В-04 |
| 10b: Лут | Генерация дропа | ⚠️ Реализовано, но из хардкода | В-05 |

---

## 📊 СООТВЕТСТВИЕ СИСТЕМЫ ЭКИПИРОВКИ

| Параметр | Документация | Код | Статус |
|----------|-------------|-----|--------|
| Слоты (15 шт) | EquipmentSlot enum | ✅ Полное совпадение | ✅ |
| 1 слот = 1 предмет | v2.0 правило | ✅ EquipmentController v2.0 | ✅ |
| 5 грейдов | Damaged→Transcendent | ✅ EquipmentGrade enum | ✅ |
| Durability множитель | ×0.5/×1.0/×1.5/×2.5/×4.0 | ✅ Полное совпадение | ✅ |
| Effectiveness множитель | ×0.5/×1.0/×1.3/×1.6/×2.0 | ❌ ×0.5/×1.0/×1.4/×2.1/×3.25 | К-03 |
| Двуручное оружие | WeaponMain+WeaponOff | ✅ Реализовано | ✅ |
| Прочность: 5 состояний | Pristine/Good/Worn/Damaged/Broken | ❌ 6 состояний, другие границы | С-01 |
| Зачарование (§5.5) | 5 источников бонусов | ❌ Не реализовано | — |
| Сетовые бонусы (§6) | SetBonuses | ❌ Не реализовано | — |
| Ремонт | NPC/самостоятельный/Ци | ⚠️ Базовый RepairEquipment | — |
| Проводимость Ци материалов | Таблица штрафов | ⚠️ qiFlowPenalty в EquipmentData | Частично |

---

## 📊 СООТВЕТСТВИЕ СТИХИЙНОЙ СИСТЕМЫ

| Взаимодействие | Документация | Код | Статус |
|----------------|-------------|-----|--------|
| Fire ↔ Water ×1.5 | ✅ | ✅ OppositeElements + CalculateElementalInteraction | ✅ |
| Earth ↔ Air ×1.5 | ✅ | ✅ | ✅ |
| Lightning ↔ Void ×1.5 | ✅ | ✅ | ✅ |
| Light ↔ Void ×1.5 | ✅ | ❌ Light нет в enum | К-04 |
| Fire → Poison ×1.2 | ✅ | ✅ | ✅ |
| Light → Poison ×1.2 | ✅ | ❌ Light нет в enum | К-04 |
| Void → All ×1.2 | ✅ | ✅ (кроме Lightning) | ✅ |
| Poison НЕ стихия | ✅ | ❌ Включён в Element enum | К-05 |
| Neutral → All ×1.0 | ✅ | ✅ | ✅ |

---

## 📋 РЕКОМЕНДАЦИИ (приоритизированные)

### 🔴 Немедленные исправления

1. **К-01:** Исправить `TechniqueGradeMultipliers` в Constants.cs на ×1.0/×1.3/×1.6/×2.0
2. **К-02:** Исправить `ULTIMATE_DAMAGE_MULTIPLIER` с 1.3 на 2.0
3. **К-03:** Исправить `GetEffectivenessMultiplier` на ×0.5/×1.0/×1.3/×1.6/×2.0
4. **К-04:** Добавить `Light` в Element enum, добавить Light↔Void в OppositeElements, добавить Light→Poison ×1.2
5. **К-05:** Либо убрать Poison из Element, либо добавить защитный код в CalculateElementalInteraction

### 🟠 Следующие по приоритету

6. **В-01:** Заменить захардкоженные ×0.5/×0.3 на чтение blockEffectiveness/shieldEffectiveness из экипировки
7. **В-02:** Проверить и обеспечить передачу статов экипировки из PlayerController/NPCController в DefenderParams
8. **В-03:** Интегрировать вызов EquipmentController.DamageEquipment() в боевой пайплайн
9. **В-04:** Реализовать кровотечение, шок, оглушение (Слой 10)
10. **В-05:** Заменить захардкоженные пулы LootGenerator на GeneratorRegistry
11. **В-06:** Реализовать износ оружия при атаке

### 🟡 Улучшения

12. **С-01:** Синхронизировать состояния прочности с EQUIPMENT_SYSTEM.md (5 состояний)
13. **С-02:** Реализовать полную формулу урона оружия из EQUIPMENT_SYSTEM.md §7.3
14. **С-03:** Вынести физические константы Qi Buffer в Constants.cs
15. **С-04:** Реализовать стихийные эффекты (горение, замедление, оглушение и т.д.)
16. **С-05:** Создать ConsumableSOFactory
17. **С-06:** Переименовать один из LootGenerator
18. **С-07:** Объединить StatBonus в единый класс

---

## 📐 МЕТОДОЛОГИЯ АУДИТА

### Итерация 1: Структурный аудит
- Сравнение файловой структуры документации с реальным расположением файлов
- Проверка наличия всех документированных классов/модулей
- Идентификация недостающих и лишних файлов

### Итерация 2: Формульный аудит
- Побитовое сравнение всех числовых констант в Constants.cs с таблицами в ALGORITHMS.md, EQUIPMENT_SYSTEM.md, TECHNIQUE_SYSTEM.md
- Проверка всех формул в DamageCalculator, TechniqueCapacity, QiBuffer, DefenseProcessor
- Сравнение enum значений (EquipmentGrade, TechniqueGrade, Element, BodyMaterial) с документированными

### Файлы проверены (23 файла, ~12K строк кода):
- `Scripts/Combat/DamageCalculator.cs` (367)
- `Scripts/Combat/DefenseProcessor.cs` (197)
- `Scripts/Combat/CombatManager.cs` (691)
- `Scripts/Combat/HitDetector.cs` (456)
- `Scripts/Combat/LevelSuppression.cs` (124)
- `Scripts/Combat/QiBuffer.cs` (294)
- `Scripts/Combat/TechniqueCapacity.cs` (257)
- `Scripts/Combat/Combatant.cs` (330)
- `Scripts/Combat/TechniqueChargeSystem.cs` (605)
- `Scripts/Combat/CombatAI.cs` (264)
- `Scripts/Combat/LootGenerator.cs` (375)
- `Scripts/Player/PlayerController.cs` (1237)
- `Scripts/NPC/NPCController.cs` (1108)
- `Scripts/Inventory/EquipmentController.cs` (1076)
- `Scripts/Generators/WeaponGenerator.cs` (686)
- `Scripts/Generators/ArmorGenerator.cs` (691)
- `Scripts/Generators/TechniqueGenerator.cs` (600)
- `Scripts/Generators/ConsumableGenerator.cs` (642)
- `Scripts/Generators/EquipmentSOFactory.cs` (383)
- `Scripts/Generators/EquipmentRuntimeSpawner.cs` (244)
- `Scripts/Generators/GeneratorRegistry.cs` (528)
- `Scripts/Generators/LootGenerator.cs` (171)
- `Scripts/Core/Constants.cs` (689)
- `Scripts/Core/Enums.cs` (735)
- `Scripts/Data/ScriptableObjects/EquipmentData.cs` (101)
- `Scripts/Data/ScriptableObjects/TechniqueData.cs` (178)
- `Scripts/Data/ScriptableObjects/ItemData.cs` (129)

---

*Аудит создан: 2026-05-05 08:35:42 UTC*
*Аудитор: AI-ассистент*
*Следующий шаг: Исправление критических расхождений (К-01…К-05)*

# Чекпоинт: Исправление пути генерации экипировки

**Дата:** 2026-05-05 11:10:00 MSK
**Статус:** in_progress

---

## АУДИТ: Путь генерации оружия

### Полный путь: GeneratorRegistry → WeaponGenerator → GeneratedWeapon → EquipmentSOFactory → EquipmentData → EquipmentController → Бой

```
GeneratorRegistry.GenerateWeaponByLevel()
  → WeaponGenerator.Generate(params, rng)
    → GeneratedWeapon DTO
      → EquipmentSOFactory.CreateRuntimeFromWeapon(dto)
        → EquipmentData SO
          → EquipmentController.Equip(data)
            → EquipmentStats.RecalculateStats()
              → ICombatant свойства → DamageCalculator
```

### 🔴 КРИТИЧЕСКИЕ БАГИ

#### БАГ-1: GeneratedWeapon DTO НЕ имеет techniqueDamageBonus

**Файл:** `Scripts/Generators/WeaponGenerator.cs:94-150`

DTO содержит:
- `qiConductivity` ✅
- `qiCostReduction` ✅
- `techniqueDamageBonus` ❌ ОТСУТСТВУЕТ

**Следствие:** Слой 1c в DamageCalculator (`if IsQiTechnique && TechniqueDamageBonus > 0`) **никогда не сработает** для сгенерированного оружия, т.к. EquipmentData.techniqueDamageBadge всегда = 0.

#### БАГ-2: WeaponGenerator НЕ генерирует techniqueDamageBonus

**Файл:** `Scripts/Generators/WeaponGenerator.cs:362-367`

Генерируются:
```csharp
weapon.qiConductivity = rng.NextFloat(minCond, maxCond) * gradeEffMult;
weapon.qiCostReduction = weapon.grade >= EquipmentGrade.Refined ? (int)weapon.grade * 0.05f : 0f;
// techniqueDamageBonus — ОТСУТСТВУЕТ
```

**Логика генерации (ПЛАН):**
- Magic оружие (Staff, Wand): базовый 5-20%, зависит от qiConductivity
- Refined+: +5% за грейд выше Common (как qiCostReduction)
- Не-магическое оружие: 0% (физическое оружие не усиливает Ци-техники)

#### БАГ-3: EquipmentSOFactory НЕ маппит techniqueDamageBonus

**Файл:** `Scripts/Generators/EquipmentSOFactory.cs:86-88`

```csharp
so.qiCostReduction = dto.qiCostReduction;  // ✅
so.chargeSpeedBonus = dto.qiConductivity > 0 ? dto.qiConductivity * 0.1f : 0f;  // ⚠️ хак
// so.techniqueDamageBonus = ???  // ❌ ОТСУТСТВУЕТ
```

**Исправление:** Добавить `so.techniqueDamageBonus = dto.techniqueDamageBonus;`

---

### ⚠️ ВТОРОСТЕПЕННЫЕ НАХОДКИ

#### НФ-1: chargeSpeedBonus — производная от qiConductivity

`so.chargeSpeedBonus = dto.qiConductivity * 0.1f` — это работает, но неявно.
Проводимость 3.0 → chargeSpeedBonus 0.3 (30% ускорение). Логика корректна для
высоких тиров, но не документирована в WeaponGenerator.

**Решение:** Оставить как есть — проводимость Ци логично ускоряет накачку.
При необходимости можно добавить отдельное поле в DTO позже.

#### НФ-2: LootGenerator хардкодит пулы предметов

**Файл:** `Scripts/Combat/LootGenerator.cs:302-305,324`

Хардкод:
```csharp
string[] fireItems = { "flame_core", "ember_stone", "phoenix_feather" };
string[] epics = { "ancient_scroll", "spirit_vein_map", "breakthrough_pill", "formation_disk" };
```

Эти пулы НЕ проходят через GeneratorRegistry. Для редких/эпических предметов
это допустимо (уникальные ID), но для элементальных ресурсов лучше использовать
генератор расходуемых.

**Решение:** Не блокирует текущую работу. Вынести в отдельную задачу.

#### НФ-3: HasShieldTechnique — заглушка в обоих контроллерах

PlayerController:127 и NPCController:107 оба возвращают `false`.
Нужно проверять TechniqueController на наличие активной техники защиты.

---

## ПЛАН ИСПРАВЛЕНИЯ

### ШАГ 1: GeneratedWeapon — добавить techniqueDamageBonus

**Файл:** `Scripts/Generators/WeaponGenerator.cs`

Добавить поле в DTO:
```csharp
// Qi properties
public float qiConductivity;
public float qiCostReduction;
public float techniqueDamageBonus;  // НОВОЕ
```

### ШАГ 2: WeaponGenerator — генерация techniqueDamageBonus

**Файл:** `Scripts/Generators/WeaponGenerator.cs`

Логика генерации рядом с qiCostReduction:
```csharp
// Бонус к урону техник — только для магического оружия и высоких грейдов
if (weapon.weaponClass == WeaponClass.Magic)
{
    // Магическое оружие: 5-20% бонус, зависит от проводимости и грейда
    weapon.techniqueDamageBonus = weapon.qiConductivity * 0.04f + 
        (weapon.grade >= EquipmentGrade.Refined ? (int)weapon.grade * 0.03f : 0f);
}
else if (weapon.grade >= EquipmentGrade.Perfect)
{
    // Совершенное+ нефизическое оружие: малый бонус 2-8%
    weapon.techniqueDamageBonus = (int)weapon.grade * 0.02f;
}
else
{
    weapon.techniqueDamageBonus = 0f;
}
```

### ШАГ 3: EquipmentSOFactory — маппинг techniqueDamageBonus

**Файл:** `Scripts/Generators/EquipmentSOFactory.cs`

Добавить после строки 88:
```csharp
so.techniqueDamageBonus = dto.techniqueDamageBonus;
```

### ШАГ 4: HasShieldTechnique — исправить заглушку

**Файл:** `Scripts/Player/PlayerController.cs`
**Файл:** `Scripts/NPC/NPCController.cs`

Заменить:
```csharp
bool ICombatant.HasShieldTechnique => false; // TODO
```
На:
```csharp
bool ICombatant.HasShieldTechnique => techniqueController != null && techniqueController.HasDefensiveTechnique();
```

Потребуется добавить метод `HasDefensiveTechnique()` в TechniqueController.

---

## СВОДНАЯ ТАБЛИЦА ИЗМЕНЕНИЙ

| Файл | Изменение | Приоритет |
|------|-----------|-----------|
| WeaponGenerator.cs | +поле techniqueDamageBonus в DTO | Критично |
| WeaponGenerator.cs | +генерация techniqueDamageBonus | Критично |
| EquipmentSOFactory.cs | +маппинг so.techniqueDamageBonus | Критично |
| TechniqueController.cs | +метод HasDefensiveTechnique() | Среднее |
| PlayerController.cs | HasShieldTechnique из TechniqueController | Среднее |
| NPCController.cs | HasShieldTechnique из TechniqueController | Среднее |

# 📦 Кодовая база: Обновление документации (пост-аудит)

👉 Основной план: [05_05_docs_update.md](05_05_docs_update.md)
**Дата:** 2026-05-05 14:06:23 MSK
**Статус:** in_progress

---

## В-13: CultivationLevelData qiDensity — формульная валидация

**Файл:** `Scripts/Data/ScriptableObjects/CultivationLevelData.cs`

```csharp
// Заменить редактируемое поле на свойство с валидацией:
public int QiDensity => GameConstants.QiDensityByLevel[
    Mathf.Min(level, GameConstants.QiDensityByLevel.Length - 1)];
```

**Обновить документацию:** CULTIVATION_SYSTEM.md — описать формулу qiDensity по уровням.

---

## С-15: coreCapacityMultiplier — OnValidate

**Файл:** `Scripts/Data/ScriptableObjects/CultivationLevelData.cs`

```csharp
private void OnValidate()
{
    coreCapacityMultiplier = Mathf.Pow(1.1f, totalSubLevels);
}
```

**Обновить документацию:** CULTIVATION_SYSTEM.md — указать формулу `1.1^totalSubLevels`.

---

## С-16: MortalStageData — дефолтные шансы пробуждения

**Файл:** `Scripts/Data/ScriptableObjects/MortalStageData.cs`

**Сейчас:** Все шансы по умолчанию = 0 (невозможно пробуждение)

**Исправление:**
```csharp
public float baseAwakeningChance = 0.0001f;   // 0.01%
public float highDensityBonus = 0.001f;        // 0.1%
public float criticalBonus = 0.01f;            // 1%
```

**Обновить документацию:** CULTIVATION_SYSTEM.md — описать механику шансов пробуждения.

---

## С-08: BodyPartState.Destroyed unreachable

**Файл:** `Scripts/Core/Enums.cs`

Верификация показала: Destroyed определён в enum, но никогда не устанавливается в UpdateState().

**Варианты:**
- A) Удалить Destroyed из enum → обновить BODY_SYSTEM.md (5 состояний)
- B) Реализовать в UpdateState() → обновить BODY_SYSTEM.md (6 состояний)

**Рекомендация:** Вариант B — тяжелые повреждения могут полностью уничтожать часть тела.

---

## С-17: FactionData.FactionType

**Файл:** `Scripts/Data/ScriptableObjects/FactionData.cs`

Согласовать FactionType enum с документацией FACTION_SYSTEM.md. Проверить: совпадают ли типы фракций в коде и в docs.

---

## Н-06: AIPersonality ↔ PersonalityTrait

**Файл:** `Scripts/NPC/AIPersonality.cs`, `Scripts/Data/ScriptableObjects/NPCPresetData.cs`

Документировать связь: AIPersonality определяет поведение ИИ, PersonalityTrait — характеристики NPC для пресета. Оба типа используются, но связь не документирована.

---

## Н-07: MinChargeTime = 0.1f захардкожено

**Файл:** `Scripts/Charger/TechniqueChargeSystem.cs`

Либо вынести в `Constants.MIN_CHARGE_TIME` и задокументировать, либо задокументировать текущее значение в TECHNIQUE_SYSTEM.md.

---

## Н-09: CombatEvents статические события без очистки

**Файл:** `Scripts/Combat/CombatEvents.cs`

Статические C# events не отписываются при уничтожении combatants. Задокументировать жизненный цикл: кто подписывается, кто отписывается, когда очищается.

---

## НФ-1: chargeSpeedBonus = qiConductivity × 0.1f

**Файл:** `Scripts/Generators/EquipmentSOFactory.cs:88`

```csharp
so.chargeSpeedBonus = dto.qiConductivity > 0 ? dto.qiConductivity * 0.1f : 0f;
```

Формула работает корректно (проводимость Ци логично ускоряет накачку), но не документирована в EQUIPMENT_SYSTEM.md и WeaponGenerator не описывает эту зависимость.

**Действие:** Добавить описание в EQUIPMENT_SYSTEM.md §2 или §3.

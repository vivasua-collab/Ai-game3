# 📦 Кодовая база: Обновление документации (пост-аудит)

👉 Основной план: [05_05_docs_update.md](05_05_docs_update.md)
**Дата:** 2026-05-05 14:06:23 MSK
**Статус:** in_progress

---

## В-13: CultivationLevelData qiDensity — ИСПОЛНЕНО ✅

**Файл:** `Scripts/Data/ScriptableObjects/CultivationLevelData.cs`

**Реализация:** Добавлен OnValidate(), который автоматически вычисляет qiDensity из GameConstants.QiDensityByLevel[level-1]:
```csharp
private void OnValidate()
{
    int idx = Mathf.Clamp(level - 1, 0, GameConstants.QiDensityByLevel.Length - 1);
    qiDensity = GameConstants.QiDensityByLevel[idx];
}
```

**Обновить документацию:** CULTIVATION_SYSTEM.md — описать формулу qiDensity = 2^(level-1).

---

## С-15: coreCapacityMultiplier — ИСПОЛНЕНО ✅

**Файл:** `Scripts/Data/ScriptableObjects/CultivationLevelData.cs`

**Реализация:** В том же OnValidate():
```csharp
int totalSubLevels = (level - 1) * GameConstants.MAX_SUB_LEVEL;
coreCapacityMultiplier = Mathf.Pow(GameConstants.CORE_CAPACITY_GROWTH, totalSubLevels);
```

**Обновить документацию:** CULTIVATION_SYSTEM.md — указать формулу `1.1^((level-1)×9)`.

---

## С-16: MortalStageData — ИСПОЛНЕНО ✅

**Файл:** `Scripts/Data/ScriptableObjects/MortalStageData.cs`

**Реализация:**
```csharp
public float baseAwakeningChance = 0.01f;       // 0.01% (было 0)
public float highDensityAwakeningChance = 0.1f;  // 0.1%  (было 0)
public float criticalAwakeningChance = 1.0f;     // 1.0%  (было 0)
```

**Обновить документацию:** CULTIVATION_SYSTEM.md — описать механику шансов пробуждения.

---

## С-08: BodyPartState.Destroyed — ИСПОЛНЕНО ✅

**Файл:** `Scripts/Core/Enums.cs`

**Реализация:** Убран Destroyed из enum. Теперь 5 состояний (Healthy → Severed).
Обновлён CharacterPanelUI.cs — убрана ветка BodyPartState.Destroyed.

**Обновить документацию:** BODY_SYSTEM.md — 5 состояний вместо 6.

---

## С-17: FactionData.FactionType — ИСПОЛНЕНО ✅

**Файл:** `Scripts/Data/ScriptableObjects/FactionData.cs`

**Реализация:** Добавлен комментарий с перечислением всех значений и ссылкой на FACTION_SYSTEM.md §2.
Код не изменён — значения совпадают с документацией.

---

## Н-06: AIPersonality ↔ PersonalityTrait — ИСПОЛНЕНО ✅

**Файл:** `Scripts/Combat/AIPersonality.cs`

**Реализация:** Добавлена развёрнутая документация в XML-комментарии класса:
- Таблица маппинга PersonalityTrait → параметры AIPersonality
- Разделение ответственности: AIPersonality = бой, PersonalityTrait = характер
- Путь инициализации: NPCPresetData.personalityFlags → CombatAI → AIPersonality

---

## Н-07: MinChargeTime → Constants — ИСПОЛНЕНО ✅

**Файл:** `Scripts/Core/Constants.cs` + `Scripts/Combat/TechniqueChargeSystem.cs`

**Реализация:**
1. Добавлено `GameConstants.MIN_CHARGE_TIME = 0.1f` (новый region Combat - Charge System)
2. TechniqueChargeSystem.GetMinChargeTime() → fallback: `GameConstants.MIN_CHARGE_TIME` вместо хардкода `0.1f`

---

## Н-09: CombatEvents жизненный цикл — ИСПОЛНЕНО ✅

**Файл:** `Scripts/Combat/CombatEvents.cs`

**Реализация:** Добавлена развёрнутая документация в XML-комментарии CombatEvents:
- 6 пунктов жизненного цикла подписок
- Предупреждение о рисках утечек
- Явное указание: подписчики ДОЛЖНЫ отписываться в OnDisable/OnDestroy

---

## НФ-1: chargeSpeedBonus = qiConductivity × 0.1f — ⚠️ ПЕРЕПРОВЕРИТЬ

**Файл:** `Scripts/Generators/EquipmentSOFactory.cs:88`

```csharp
so.chargeSpeedBonus = dto.qiConductivity > 0 ? dto.qiConductivity * 0.1f : 0f;
```

Формула работает корректно (проводимость Ци логично ускоряет накачку), но не документирована в EQUIPMENT_SYSTEM.md и WeaponGenerator не описывает эту зависимость.

**Действие:** ⚠️ Перепроверить формулу перед фиксацией в документации. Добавить описание в EQUIPMENT_SYSTEM.md §2 или §3.

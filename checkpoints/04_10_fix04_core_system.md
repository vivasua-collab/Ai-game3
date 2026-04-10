# Чекпоинт: Fix-04 — Core System (Constants + Enums + StatDev + GameEvents)

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** P0-HIGH

---

## Описание

Фундаментальные проблемы Core: StatDevelopment не работает для статов <10, Constants содержит Infinity и пропущенные записи, Enums содержит смешанные концепты и дубликаты, GameEvents утекает.

**Все решения пользователя внесены.**

---

## Файлы (4 файла, ~2620 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `Core/Constants.cs` | 678 | Construct в BodyMaterialReduction, Infinity→MaxValue, OppositeElements (Variant A), MAX_STAT_VALUE=1000 |
| 2 | `Core/Enums.cs` | 650 | Element.Count, Disposition→Attitude+PersonalityTrait, EquipmentSlot, AttackResult collision, Duplicate enums |
| 3 | `Core/StatDevelopment.cs` | 568 | Порог <10, ConsolidateStat, [SerializeField]→[Serializable] |
| 4 | `Core/GameEvents.cs` | 724 | ClearAllEvents при смене сцены |

---

## Задачи

### CRITICAL
- [ ] CORE-C02: StatDevelopment.GetThreshold — `threshold = Math.Max(1f, Mathf.Floor(currentStat / 10f))`
- [ ] CORE-C03: StatDevelopment.ConsolidateStat — использовать availableConsolidation в вычислениях
- [ ] CORE-C04: StatDevelopment — убрать [SerializeField], обернуть в [Serializable] класс, ссылаться из MonoBehaviour
- [ ] CORE-C05: Constants.BodyMaterialReduction — добавить `{ BodyMaterial.Construct, 0.4f }` и в BodyMaterialHardness
- [ ] CORE-C06: Constants.RegenerationMultipliers[9] — заменить float.PositiveInfinity на float.MaxValue с обработкой "мгновенного восстановления"
- [ ] CORE-C07: MAX_STAT_VALUE = **1000** — единая константа в GameConstants. Убрать дублирующее значение из StatDevelopment

### HIGH
- [ ] CORE-H01: Constants.OppositeElements — реализовать Вариант А:
  ```
  Fire ↔ Water       (×1.5 opposite, ×0.8 affinity)
  Earth ↔ Air        (×1.5 opposite, ×0.8 affinity)
  Lightning ↔ Void   (×1.5 opposite, ×0.8 affinity)
  ```
  Добавить в CalculateElementalInteraction:
  - Fire → Poison: ×1.2 (выжигание токсинов, одностороннее)
  - Void → All: ×1.2 (поглощение)
  - Neutral → All: ×1.0 (без бонусов)
- [ ] CORE-H02: GameEvents — вызвать ClearAllEvents() при смене сцены (в SceneLoader.OnSceneUnloaded)
- [ ] CORE-H05: Enums.Element — убрать Count из enum, использовать `Enum.GetValues(typeof(Element)).Length - 1` или фильтр при итерации

### MEDIUM — Disposition → Attitude + PersonalityTrait
- [ ] CORE-M01: Полная замена Disposition на два enum:

**Attitude (отношение к игроку, числовое -100..+100):**
```csharp
public enum Attitude
{
    Hatred,         // -100..-51 — атака без предупреждения
    Hostile,        // -50..-21  — атака если спровоцирован
    Unfriendly,     // -20..-10  — избегание
    Neutral,        // -9..9     — безразличие
    Friendly,       // 10..49    — помощь, торговля
    Allied,         // 50..79    — лояльность
    SwornAlly       // 80..100   — самопожертвование
}
```

**PersonalityTrait (характер NPC, [Flags] комбинируемый):**
```csharp
[Flags]
public enum PersonalityTrait
{
    None        = 0,
    Aggressive  = 1 << 0,   // Склонен к атаке, первый удар
    Cautious    = 1 << 1,   // Избегает рисков, защита
    Treacherous = 1 << 2,   // Может предать при возможности
    Ambitious   = 1 << 3,   // Ищет власть, лидерство
    Loyal       = 1 << 4,   // Не предаёт никогда
    Pacifist    = 1 << 5,   // Избегает боя
    Curious     = 1 << 6,   // Исследует, задаёт вопросы
    Vengeful    = 1 << 7    // Помнит обиды, мстит
}
```

**Затронутые системы (каскад):**
- HitDetector.IsValidTarget() — проверять Attitude, не Disposition → Fix-02
- NPCController — поле disposition → attitude + personality → Fix-07
- NPCAI.DecideNormalBehavior — PersonalityTrait flags для весов → Fix-07
- RelationshipController — CalculateAttitude() → Fix-07
- FormationEffects.IsAlly() — через Attitude → Fix-06
- SaveDataTypes — NPCSaveData: 2 поля → Fix-08

- [ ] CORE-M02: Enums.EquipmentSlot — привести в соответствие с INVENTORY_SYSTEM.md:
  ```
  WeaponMain, WeaponOff, Armor, Clothing, Charger,
  RingLeft, RingRight, Accessory, Backpack
  ```
- [ ] CORE-M03: AttackResult — переименовать struct в CombatAttackResult или enum в CoreAttackResult для устранения коллизии

---

## Порядок выполнения

1. StatDevelopment.cs — пороги + консолидация + SerializeField + MAX_STAT_VALUE=1000
2. Constants.cs — все фиксы (Construct, Infinity, OppositeElements Variant A, MAX_STAT_VALUE)
3. Enums.cs — Element.Count + Disposition→Attitude+PersonalityTrait + EquipmentSlot + AttackResult
4. GameEvents.cs — ClearAllEvents при смене сцены

---

## Зависимости

- **Предшествующие:** Fix-01 (Qi types), Fix-02 (Combat pipeline — elemental interaction может дублироваться)
- **Последующие:** Fix-05 (Body — зависит от CORE-C01 Heart blackHP), Fix-06 (FormationEffects.IsAlly через Attitude), Fix-07 (NPC все изменения Disposition), Fix-08 (SaveData)

---

## ⚠️ Важно: Каскадные эффекты Disposition

Замена Disposition → Attitude + PersonalityTrait затрагивает 6+ файлов за пределами этого чекпоинта. В этом чекпоинте:
1. Создать новые enum в Enums.cs
2. Пометить Disposition как [Obsolete]
3. НЕ удалять Disposition сразу — каскадные изменения в Fix-06, Fix-07, Fix-08

---

*Чекпоинт обновлён: 2026-04-10 13:37:00 UTC*

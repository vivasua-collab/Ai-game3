# Чекпоинт: Fix-04 — Core System (Constants + Enums + StatDev + GameEvents)

**Дата:** 2026-04-10 12:55:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** P0-HIGH

---

## Описание

Фундаментальные проблемы Core: StatDevelopment не работает для статов <10, Constants содержит Infinity и пропущенные записи, Enums содержит смешанные концепты и дубликаты, GameEvents утекает.

---

## Файлы (4 файла, ~2620 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `Core/Constants.cs` | 678 | Construct в BodyMaterialReduction, Infinity→MaxValue, OppositeElements, MAX_STAT_VALUE |
| 2 | `Core/Enums.cs` | 650 | Element.Count, Disposition split, EquipmentSlot, AttackResult collision, Duplicate enums |
| 3 | `Core/StatDevelopment.cs` | 568 | Порог <10, ConsolidateStat, [SerializeField]→[Serializable] |
| 4 | `Core/GameEvents.cs` | 724 | ClearAllEvents при смене сцены |

---

## Задачи

### CRITICAL
- [ ] CORE-C02: StatDevelopment.GetThreshold — `threshold = Math.Max(1f, Mathf.Floor(currentStat / 10f))`
- [ ] CORE-C03: StatDevelopment.ConsolidateStat — использовать availableConsolidation
- [ ] CORE-C04: StatDevelopment — убрать [SerializeField], обернуть в [Serializable] класс, ссылаться из MonoBehaviour
- [ ] CORE-C05: Constants.BodyMaterialReduction — добавить `{ BodyMaterial.Construct, 0.4f }`
- [ ] CORE-C06: Constants.RegenerationMultipliers[9] — заменить float.PositiveInfinity на float.MaxValue с обработкой "мгновенного восстановления"
- [ ] CORE-C07: Синхронизировать MAX_STAT_VALUE (100 vs 1000) — принять решение, обновить оба файла

### HIGH
- [ ] CORE-H01: Constants.OppositeElements — добавить Lightning/Void/Poison/Neutral (определить пары по лору)
- [ ] CORE-H02: GameEvents — вызвать ClearAllEvents() при смене сцены (в SceneLoader или GameInitializer)
- [ ] CORE-H05: Enums.Element — убрать Count из enum или добавить фильтр при итерации

### MEDIUM
- [ ] CORE-M01: Enums.Disposition — разделить на Attitude (Hostile..Allied) и PersonalityTrait (Aggressive..Ambitious) [flags]
- [ ] CORE-M02: Enums.EquipmentSlot — привести в соответствие с INVENTORY_SYSTEM.md
- [ ] CORE-M03: AttackResult — переименовать struct в CombatAttackResult или enum в CoreAttackResult для устранения коллизии

---

## Порядок выполнения

1. StatDevelopment.cs — пороги + консолидация + SerializeField
2. Constants.cs — все фиксы
3. Enums.cs — Element.Count + Disposition + EquipmentSlot + AttackResult
4. GameEvents.cs — ClearAllEvents при смене сцены

---

## Зависимости

- **Предшествующие:** Fix-01 (Qi types), Fix-02 (Combat pipeline)
- **Последующие:** Fix-05 (Body — зависит от CORE-C01 Heart blackHP)

---

## Решения пользователя (требуются)

- CORE-C07: MAX_STAT_VALUE = 100 или 1000?
- CORE-H01: Какие противоположности для Lightning/Void/Poison?
- CORE-M01: Подтвердить разделение Disposition → Attitude + PersonalityTrait
- CORE-M02: EquipmentSlot — уточнить маппинг по документации

---

*Чекпоинт создан: 2026-04-10 12:55:00 UTC*

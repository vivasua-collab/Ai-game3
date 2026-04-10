# Чекпоинт: Fix-01 — Qi int→long миграция + Core типы

**Дата:** 2026-04-10 12:55:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** P0 (блокирует gameplay)

---

## Описание

Кросс-системная проблема: Qi использует `long` в QiController, но `int` в интерфейсе ICombatant, DefenderParams, PlayerState, SaveData, SO-данных, генераторах и тестах. На уровнях культивации L5+ Qi > 2.1 млрд → truncation → сломанный бой.

**Стратегия:** Миграция `int→long` для всех Qi-полей. `int→long` обратно совместим на уровне вызовов (int неявно приводится к long).

---

## Файлы (8 файлов, ~3200 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `Qi/QiController.cs` | 494 | Fix long→int casts (:357,364,365,371), SpendQi(long), negative check |
| 2 | `Combat/Combatant.cs` | 289 | ICombatant.SpendQi(int)→(long), DefenderParams.CurrentQi int→long |
| 3 | `Player/PlayerController.cs` | 525 | PlayerState Qi int→long, rb.velocity→linearVelocity |
| 4 | `Charger/ChargerController.cs` | 594 | UseQiForTechnique int→long, [SerializeField] fix |
| 5 | `NPC/NPCController.cs` | 483 | (int)generated.maxQi fix, NPCSaveData MaxQi/MaxHealth |
| 6 | `Formation/FormationController.cs` | 868 | (int)playerQi.CurrentQi fix, _instance=null в OnDestroy |
| 7 | `Formation/FormationQiPool.cs` | ~250 | AcceptQi int→long |
| 8 | `Data/ScriptableObjects/TechniqueData.cs` | ~150 | baseQiCost int→long |

## Связанные файлы (не в этом чекпоинте, но требуют миграции позже)

- `SpeciesData.cs` (coreCapacityBase float→long) → Fix-06
- `ConsumableGenerator.cs` (Qi float→long) → Fix-06
- `IntegrationTestScenarios.cs`, `IntegrationTests.cs` → Fix-10
- `MortalStageData.cs` (Qi int→long) → Fix-06

---

## Задачи

### Qi типы (CRITICAL)
- [ ] QI-C01: QiController — убрать все long→int casts, использовать long
- [ ] CMB-C04: ICombatant.SpendQi(int) → SpendQi(long) в интерфейсе и CombatantBase
- [ ] CMB-C05: DefenderParams.CurrentQi int → long
- [ ] PLR-H04: PlayerState Qi int → long
- [ ] CHR-C02: ChargerController.UseQiForTechnique int → long
- [ ] NPC-H04: NPCController (int)generated.maxQi → long
- [ ] FRM-M05: FormationController (int)playerQi.CurrentQi → убрать cast
- [ ] FRM-M03: FormationQiPool.AcceptQi int → long
- [ ] DAT-H01: TechniqueData.baseQiCost int → long

### Qi эксплойты (HIGH)
- [ ] QI-H03: SpendQi — добавить `if (amount <= 0) return false;`
- [ ] QI-L01: SpendQi(0) — проверить `amount > 0` вместо `>=`

### Unity 6 (CRITICAL)
- [ ] PLR-C01: PlayerController `rb.velocity` → `rb.linearVelocity`

### Formation singleton (CRITICAL)
- [ ] FRM-C01: FormationController.OnDestroy — `_instance = null;`

### NPC SaveData (CRITICAL)
- [ ] NPC-C01: NPCSaveData — добавить MaxQi, MaxHealth, MaxStamina, MaxLifespan

### Charger SerializeField (CRITICAL)
- [ ] CHR-C01: ChargerHeat, ChargerBuffer, ChargerSlot — убрать [SerializeField] или сделать MonoBehaviour/Serializable

---

## Порядок выполнения

1. QiController.cs — SpendQi(long) + negative check + убрать casts
2. Combatant.cs — ICombatant.SpendQi(long) + DefenderParams.CurrentQi long
3. PlayerController.cs — PlayerState long + linearVelocity
4. ChargerController.cs — UseQiForTechnique(long) + [SerializeField] fix
5. NPCController.cs — long maxQi + NPCSaveData дополнить
6. FormationController.cs — long cast fix + _instance=null
7. FormationQiPool.cs — AcceptQi long
8. TechniqueData.cs — baseQiCost long

---

## Зависимости

- **Предшествующие:** нет (первый чекпоинт)
- **Последующие:** Fix-02 (Combat Pipeline) зависит от SpendQi(long)

---

## Риски

- ICombatant.SpendQi(int→long) — все реализации должны быть обновлены одновременно
- Если есть другие файлы с SpendQi(int), которые не в этом чекпоинте — компиляция упадёт до Fix-06/10
- Рекомендация: после этого чекпоинта сразу выполнить Fix-06 и Fix-10

---

*Чекпоинт создан: 2026-04-10 12:55:00 UTC*

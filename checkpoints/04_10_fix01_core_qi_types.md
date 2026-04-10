# Чекпоинт: Fix-01 — Qi int→long миграция + Core типы

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** complete
**Приоритет:** P0 (блокирует gameplay)
**Завершено:** 2026-04-10 14:40:00 UTC

---

## Описание

Кросс-системная проблема: Qi использует `long` в QiController, но `int` в интерфейсе ICombatant, DefenderParams, PlayerState, SaveData, SO-данных, генераторах и тестах. На уровнях культивации L5+ Qi > 2.1 млрд → truncation → сломанный бой.

**Стратегия:** Миграция `int→long` для всех Qi-полей. `int→long` обратно совместим на уровне вызовов (int неявно приводится к long).

---

## Изменённые файлы (18 файлов)

| # | Файл | Изменение |
|---|------|-----------|
| 1 | `Qi/QiController.cs` | SpendQi(long), negative check, EffectiveQi, EstimateCapacityAtLevel, long casts removed |
| 2 | `Combat/QiBuffer.cs` | QiBufferResult.QiConsumed/Remaining → long, все методы currentQi long, division → double precision |
| 3 | `Combat/Combatant.cs` | ICombatant.SpendQi(long), DefenderParams.CurrentQi long, lambda→named method (CMB-C08) |
| 4 | `Combat/DamageCalculator.cs` | DefenderParams.CurrentQi long |
| 5 | `Player/PlayerController.cs` | PlayerState Qi long, rb.linearVelocity (Unity 6) |
| 6 | `Charger/ChargerBuffer.cs` | UseQiForTechnique/CanUseTechnique/GetEffectiveQiAvailable → long params, ChargerBufferResult.QiFromCore long, [Header]→[SerializeField] |
| 7 | `Charger/ChargerController.cs` | UseQiForTechnique(long), ChargeFormation long, practitionerCurrentQi long |
| 8 | `Charger/ChargerHeat.cs` | AddHeatFromQi(long), [Header]→[SerializeField] |
| 9 | `Charger/ChargerSlot.cs` | [SerializeField] восстановлен |
| 10 | `NPC/NPCController.cs` | (int)generated.maxQi→long, NPCSaveData load/save MaxQi/MaxHealth/MaxStamina/MaxLifespan |
| 11 | `NPC/NPCData.cs` | NPCState/NPCSaveData CurrentQi/MaxQi long, MaxQi/MaxHealth/MaxStamina/MaxLifespan добавлены |
| 12 | `Formation/FormationController.cs` | ContributeQi/ChargeFormationFromCharger long, _instance=null в OnDestroy |
| 13 | `Formation/FormationCore.cs` | ContributeQi → long params/return |
| 14 | `Formation/FormationQiPool.cs` | AcceptQi → long |
| 15 | `Formation/FormationUI.cs` | ContributeQi call → long |
| 16 | `Data/ScriptableObjects/TechniqueData.cs` | baseQiCost int→long |
| 17 | `Editor/AssetGeneratorExtended.cs` | TechniqueJsonData.baseQiCost int→long (каскад) |
| 18 | `Tests/IntegrationTests.cs` | SpendQi(long) (каскад ICombatant) |

## Выполненные задачи

### Qi типы (CRITICAL)
- [x] QI-C01: QiController — убрать все long→int casts, QiBufferResult.QiRemaining long
- [x] CMB-C04: ICombatant.SpendQi(long) в интерфейсе и CombatantBase
- [x] CMB-C05: DefenderParams.CurrentQi long
- [x] PLR-H04: PlayerState Qi long
- [x] CHR-C02: ChargerController.UseQiForTechnique long
- [x] NPC-H04: NPCController (int)generated.maxQi → long
- [x] FRM-M05: FormationController (int)playerQi.CurrentQi → long
- [x] FRM-M03: FormationQiPool.AcceptQi long
- [x] DAT-H01: TechniqueData.baseQiCost long

### Qi эксплойты (HIGH)
- [x] QI-H03: SpendQi — добавлен `if (amount <= 0) return false;`
- [x] QI-L01: SpendQi(0) — проверка amount > 0 вместо >=

### Qi Model В — подготовка (HIGH)
- [x] QI-MDL-B: EffectiveQi property `(long)(currentQi * qiDensity)`
- [x] QI-MDL-B: EstimateCapacityAtLevel/EstimateCapacityAtSubLevel helpers

### Unity 6 (CRITICAL)
- [x] PLR-C01: PlayerController `rb.velocity` → `rb.linearVelocity`

### Formation singleton (CRITICAL)
- [x] FRM-C01: FormationController.OnDestroy — `_instance = null;`

### NPC SaveData (CRITICAL)
- [x] NPC-C01: NPCSaveData — MaxQi (long), MaxHealth, MaxStamina, MaxLifespan

### Charger SerializeField (CRITICAL)
- [x] CHR-C01: ChargerHeat/Buffer/Slot — [Header] убраны (не работают в non-MonoBehaviour), [SerializeField] оставлены для сериализации

---

## Зависимости

- **Предшествующие:** нет (первый чекпоинт)
- **Последующие:** Fix-02 (Combat Pipeline) зависит от SpendQi(long)

---

*Чекпоинт обновлён: 2026-04-10 14:40:00 UTC*

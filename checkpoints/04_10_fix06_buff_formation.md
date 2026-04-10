# Чекпоинт: Fix-06 — Buff Manager + Formation Effects

**Дата:** 2026-04-10 12:55:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** P0-HIGH

---

## Описание

BuffManager: float.Parse crash, Independent stacking, ConductivityModifier мёртвый код, SO утечки. FormationEffects: перманентный контроль, заглушка BuffManager, IsAlly сломан. FormationArrayEffect обходит BuffManager.

---

## Файлы (5 файлов, ~3250 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `Buff/BuffManager.cs` | 1278 | float.Parse→TryParse, Independent, ConductivityModifier, SO leak, Slow |
| 2 | `Formation/FormationEffects.cs` | 495 | Control восстановление, IsAlly, заглушка BuffManager, ApplyHeal |
| 3 | `Combat/Effects/FormationArrayEffect.cs` | ~200 | ApplyBuff/Debuff через BuffManager |
| 4 | `Combat/Effects/ExpandingEffect.cs` | ~150 | ICombatTarget квалификация |
| 5 | `Formation/FormationCore.cs` | 685 | OverlapCircleAll layer mask, ContributeQi cap, practitionerId |

---

## Задачи

### CRITICAL
- [ ] BUF-C01: BuffManager — заменить 7 float.Parse → float.TryParse с fallback
- [ ] BUF-C02: BuffManager.HandleExistingBuff — Independent: реально добавить бафф, не просто вернуть Applied
- [ ] BUF-C03: BuffManager — применить ConductivityModifier к QiController (подключить мёртвый код)
- [ ] BUF-C04: FormationEffects.ApplyControlFallback — сохранять и восстанавливать оригинальные Rigidbody2D значения

### HIGH
- [ ] BUF-H01: BuffManager — убрать ScriptableObject.CreateInstance утечки, использовать кэш
- [ ] BUF-H02: BuffManager — Slow effect использовать buff.data вместо hardcoded 50%
- [ ] BUF-H03: BuffManager.RemoveControl — проверять другие активные контроль-баффы перед сбросом флага
- [ ] FRM-H01: FormationCore.ApplyEffects — добавить layer mask в OverlapCircleAll
- [ ] FRM-H02: FormationEffects.IsAlly — исправить faction detection через FactionController
- [ ] FRM-H03: FormationArrayEffect.ApplyBuff/ApplyDebuff — маршрутизировать через BuffManager

### MEDIUM
- [ ] BUF-M01: RemoveBuff(FormationBuffType) — сузить критерий удаления
- [ ] BUF-M02: CreateTempBuffData — учесть tickInterval при секунды→тики
- [ ] BUF-M03: ApplySpecialEffect — triggerChance проверять при тике, не при применении
- [ ] BUF-M04: CurrentControl — хранить стек контроль-эффектов
- [ ] BUF-M05: Удалить заглушку BuffManager из FormationEffects.cs
- [ ] FRM-M01: FormationCore.ContributeQi — добавить проверку maxCapacity
- [ ] FRM-M02: FormationCore.practitionerId — использовать persistend ID вместо GetInstanceID()
- [ ] FRM-M04: FormationEffects.ApplyHeal — раздельный выбор: лечить Qi, Body, или оба

---

## Порядок выполнения

1. BuffManager.cs — все Buff фиксы (крупный файл, много изменений)
2. FormationEffects.cs — Control + IsAlly + заглушка + ApplyHeal
3. FormationCore.cs — layer mask + ContributeQi + practitionerId
4. FormationArrayEffect.cs — BuffManager интеграция
5. ExpandingEffect.cs — ICombatTarget

---

## Зависимости

- **Предшествующие:** Fix-01 (Qi types), Fix-02 (Combat pipeline)
- **Последующие:** Fix-09 (Save — BuffSaveData)

---

*Чекпоинт создан: 2026-04-10 12:55:00 UTC*

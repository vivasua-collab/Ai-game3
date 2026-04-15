# Чекпоинт: Fix-05 — Body System + Heart blackHP

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** complete
**Приоритет:** P0
**Завершено:** 2026-04-11 18:45:00 UTC

---

## Выполненные задачи

### CRITICAL
- [x] CORE-C01: BodyPart.cs — если partType == Heart: maxBlackHP = 0, currentBlackHP = 0
- [x] BOD-C01: BodyDamage.CreateHumanoidBody — HP значения по BODY_SYSTEM.md
  - Head=50, Torso=100, Heart=80, Arm=40, Hand=20, Leg=50, Foot=25 (при VIT=10)
  - CreateQuadrupedBody аналогично обновлены HP

### MEDIUM
- [x] BOD-M01: BodyDamage.ApplyDamage — убран двойной 70/30 split. Теперь только BodyPart.ApplyDamage делает split
- [x] BOD-M02: BodyController.FullRestore — Heart blackHP=0 корректно (CORE-C01)
- [x] BOD-M03: BodyController.BodyParts — IReadOnlyList вместо List

### LOW
- [x] BOD-L01: BodyController.InitializeBody — guard `if (_isInitialized) return;`
- [x] BOD-L02: BodyController.Heal — проверка `bodyParts.Count == 0` перед делением
- [x] BOD-L03: BodyDamage Quadruped Torso — isVital=false по документации

## Изменённые файлы (3 файла)

| # | Файл | Изменение |
|---|------|-----------|
| 1 | `Body/BodyPart.cs` | CORE-C01: Heart blackHP=0 |
| 2 | `Body/BodyController.cs` | BOD-M02/03, BOD-L01/02 |
| 3 | `Body/BodyDamage.cs` | BOD-C01 (HP по доке), BOD-M01 (double split), BOD-L03 (Quadruped Torso) |

---

*Чекпоинт обновлён: 2026-04-11 18:45:00 UTC*

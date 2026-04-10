# Чекпоинт: Fix-03 — Qi System + Technique + Модель В

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** complete
**Приоритет:** P0-HIGH
**Завершено:** 2026-04-10 16:25:00 UTC

---

## Выполненные задачи

### Qi Модель В (CRITICAL)
- [x] QI-MDL-B01: CanBreakthrough — Модель В: требование = capacity(next) × density(next). Добавлен CalculateBreakthroughRequirement()
- [x] QI-MDL-B02: PerformBreakthrough — coreCapacity обновляется ПОСЛЕ RecalculateStats(): level++ → RecalculateStats() → coreCapacity = maxQiCapacity
- [x] QI-MDL-B03: EffectiveQi — уже добавлен в Fix-01

### Qi HIGH
- [x] QI-H01: Meditate — добавлено продвижение игрового времени через FindFirstObjectByType<TimeController>().AdvanceHours()
- [x] QI-H02: PerformBreakthrough — порядок level++→RecalculateStats→coreCapacity=maxQiCapacity гарантирован

### Qi MEDIUM
- [x] QI-M01: Meditate — overflow check (double + MAX_SAFE_GAIN)

### Technique HIGH
- [x] CMB-H01: IncreaseMastery — формула max(0.1, baseGain × (1 - mastery/100))
- [x] CMB-H02: Cooldown — убран ×60 множитель. technique.Data.cooldown уже в секундах

## Изменённые файлы (2 файла)

| # | Файл | Изменение |
|---|------|-----------|
| 1 | `Qi/QiController.cs` | QI-MDL-B01/02, QI-H01/02, QI-M01 |
| 2 | `Combat/TechniqueController.cs` | CMB-H01, CMB-H02 |

---

*Чекпоинт обновлён: 2026-04-10 16:25:00 UTC*

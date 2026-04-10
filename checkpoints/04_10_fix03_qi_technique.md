# Чекпоинт: Fix-03 — Qi System + TechniqueController

**Дата:** 2026-04-10 12:55:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** P0-HIGH

---

## Описание

Qi система: медитация не тратит время, coreCapacity stale после прорыва, формулы мастерства техник не соответствуют документации, кулдаун ×60 без документирования единиц.

---

## Файлы (2 файла, ~941 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `Qi/QiController.cs` | 494 | Медитация + время, coreCapacity, conductivity, overflow |
| 2 | `Combat/TechniqueController.cs` | 447 | Формула мастерства, cooldown |

---

## Задачи

### Qi CRITICAL (остатки после Fix-01)
- [ ] QI-C01: Убедиться что все long→int casts удалены (проверить после Fix-01)

### Qi HIGH
- [ ] QI-H01: QiController.Meditate — добавить TimeController.AdvanceHours для продвижения игрового времени
- [ ] QI-H02: QiController.PerformBreakthrough — coreCapacity должен обновляться ПОСЛЕ установки нового maxQiCapacity

### Qi MEDIUM
- [ ] QI-M01: QiController.Meditate — добавить проверку overflow для durationTicks * conductivity
- [ ] QI-M02: QiController.baseConductivity — рассмотреть double для precision при больших capacities

### Technique HIGH
- [ ] CMB-H01: TechniqueController.IncreaseMastery — реализовать формулу из документации: `max(0.1, baseGain × (1 - currentMastery / 100))`
- [ ] CMB-H02: TechniqueController:262 — уточнить единицу cooldown (тики или секунды), добавить комментарий, убрать ×60 если секунды

---

## Порядок выполнения

1. QiController.cs — все Qi-specific фиксы
2. TechniqueController.cs — формулы

---

## Зависимости

- **Предшествующие:** Fix-01 (SpendQi long)
- **Последующие:** нет прямых

---

*Чекпоинт создан: 2026-04-10 12:55:00 UTC*

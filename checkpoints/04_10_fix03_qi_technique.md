# Чекпоинт: Fix-03 — Qi System + Technique + Модель В

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** P0-HIGH

---

## Описание

Qi система: медитация не тратит время, CanBreakthrough не учитывает плотность (Модель В), формулы мастерства техник не соответствуют документации, кулдаун ×60 без документирования единиц.

**Модель В принята:** Растущий резервуар + Сжатие Ци. Ёмкость растёт, плотность растёт, прорыв требует capacity(next) × density(next).

---

## Файлы (2 файла, ~941 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `Qi/QiController.cs` | 494 | CanBreakthrough Модель В, Meditate + время, coreCapacity stale, overflow, EffectiveQi |
| 2 | `Combat/TechniqueController.cs` | 447 | Формула мастерства, cooldown |

---

## Задачи

### Qi Модель В (CRITICAL)
- [ ] QI-MDL-B01: QiController.CanBreakthrough — учитывать плотность Ци:
  ```
  Требование для прорыва = EstimateCapacityAtLevel(nextLevel) × density(nextLevel)
  // БОЛЬШОЙ прорыв: capacity(nextMajorLevel) × density(nextMajorLevel)
  // МАЛЫЙ прорыв: capacity(nextSubLevel) × currentDensity
  ```
- [ ] QI-MDL-B02: QiController.PerformBreakthrough — убедиться что coreCapacity обновляется через RecalculateStats() корректно. Проверить что `coreCapacity = maxQiCapacity` после `cultivationLevel++` даёт правильное значение
- [ ] QI-MDL-B03: QiController.EffectiveQi property — `(long)(currentQi * qiDensity)` — для UI и боевых расчётов

### Qi CRITICAL (остатки после Fix-01)
- [ ] QI-C01: Убедиться что все long→int casts удалены (проверить после Fix-01)

### Qi HIGH
- [ ] QI-H01: QiController.Meditate — добавить TimeController.AdvanceHours для продвижения игрового времени. Медитация 8ч → `timeController.AdvanceHours(8)`
- [ ] QI-H02: QiController.PerformBreakthrough — coreCapacity обновляется ПОСЛЕ установки нового maxQiCapacity. Проверить порядок: level++ → RecalculateStats() → coreCapacity = maxQiCapacity

### Qi MEDIUM
- [ ] QI-M01: QiController.Meditate — добавить проверку overflow для durationTicks * conductivity
- [ ] QI-M02: QiController.baseConductivity — рассмотреть double для precision при больших capacities

### Technique HIGH
- [ ] CMB-H01: TechniqueController.IncreaseMastery — реализовать формулу из документации:
  ```
  masteryGained = max(0.1, baseGain × (1 - currentMastery / 100))
  technique.Mastery = min(100f, technique.Mastery + masteryGained)
  ```
- [ ] CMB-H02: TechniqueController:262 — уточнить единицу cooldown (тики или секунды), добавить комментарий, убрать ×60 если секунды. Если cooldown в TechniqueData уже в секундах → `CooldownRemaining = technique.Data.cooldown;` без множителя

---

## Порядок выполнения

1. QiController.cs — Модель В (CanBreakthrough + EffectiveQi) + Meditate время + coreCapacity + overflow
2. TechniqueController.cs — формула мастерства + cooldown

---

## Зависимости

- **Предшествующие:** Fix-01 (SpendQi long, EffectiveQi property добавлен)
- **Последующие:** нет прямых

---

## Примечания по Модели В

### Текущий код vs Модель В

| Аспект | Модель В | Текущий код | Статус |
|--------|----------|-------------|--------|
| coreCapacity = 1000 × 1.1^totalSubLevels | ✅ | CalculateMaxCapacity() — совпадает | ✅ OK |
| qiDensity = 2^(level-1) | ✅ | qiDensity = Mathf.Pow(2, level-1) | ✅ OK |
| effectiveQi = capacity × density | ✅ | Нигде не используется | ❌ FIX |
| conductivity = capacity / 360 | ✅ | baseConductivity = maxQiCapacity / 360f | ✅ OK |
| После прорыва Ци = 0 | ✅ | currentQi = 0 | ✅ OK |
| Требование = capacity(next) × density(next) | ✅ | capacity × BIG/SMALL_MULTIPLIER | ❌ FIX |

### CanBreakthrough — логика изменения

```csharp
// ТЕКУЩИЙ (неправильно):
long required = isMajorLevel 
    ? (long)(coreCapacity * BIG_BREAKTHROUGH_MULTIPLIER)
    : (long)(coreCapacity * SMALL_BREAKTHROUGH_MULTIPLIER);

// МОДЕЛЬ В:
if (isMajorLevel)
{
    int nextLevel = cultivationLevel + 1;
    float nextDensity = Mathf.Pow(2, nextLevel - 1);
    long nextCapacity = EstimateCapacityAtLevel(nextLevel);
    required = (long)(nextCapacity * nextDensity);
}
else
{
    // Малый прорыв: текущая плотность, следующая подёмкость
    float currentDensity = qiDensity;
    long nextSubCapacity = EstimateCapacityAtSubLevel(cultivationLevel, cultivationSubLevel + 1);
    required = (long)(nextSubCapacity * currentDensity);
}
```

---

*Чекпоинт обновлён: 2026-04-10 13:37:00 UTC*

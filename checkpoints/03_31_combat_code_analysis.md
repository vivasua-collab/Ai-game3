# 📋 Анализ кода: UnityProject/Assets/Scripts/Combat

**Дата:** 2026-03-31 09:24:43 UTC
**Папка:** `UnityProject/Assets/Scripts/Combat`
**Файлов:** 6

---

## 📁 Обработанные файлы

| Файл | Статус | Изменения |
|------|--------|-----------|
| `QiBuffer.cs` | ✅ Обновлён | Добавлены timestamps, методы для физ. урона |
| `LevelSuppression.cs` | ✅ Обновлён | Добавлены timestamps, исправлено использование GameConstants |
| `TechniqueCapacity.cs` | ✅ Обновлён | Добавлены timestamps, документация расхождений |
| `DamageCalculator.cs` | ✅ Обновлён | Добавлены timestamps, исправлен IsQiTechnique |
| `DefenseProcessor.cs` | ✅ Обновлён | Добавлены timestamps, документация формул |
| `TechniqueController.cs` | ✅ Обновлён | Добавлены timestamps, документация |

---

## ✅ Исправленные расхождения

### 1. QiBuffer — Физический урон (КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ)

**Проблема:** QiBuffer не различал техники Ци и физический урон.

**Документация (ALGORITHMS.md §2):**

| Тип атаки | Сырая Ци | Щит |
|-----------|----------|-----|
| **Техники Ци** | 90%/3:1/10% | 100%/1:1/0% |
| **Физический** | 80%/5:1/20% | 100%/2:1/0% |

**Решение:** Добавлены:
- `ProcessPhysicalDamage()` — обработка физического урона
- `ProcessQiTechniqueDamage()` — обработка техник Ци
- `DamageSourceType` enum для различения типов

---

### 2. LevelSuppression — Использование GameConstants

**Проблема:** Код использовал локальную копию таблицы вместо `GameConstants.LevelSuppressionTable`.

**Решение:** Теперь использует `GameConstants.LevelSuppressionTable[tableIndex][attackIndex]`.

---

### 3. DamageCalculator — IsQiTechnique флаг

**Проблема:** DamageCalculator не передавал тип урона (Ци vs физический) в QiBuffer.

**Решение:** Добавлен флаг `IsQiTechnique` в `AttackerParams`, который передаётся в QiBuffer.

---

## ⚠️ Оставшиеся расхождения (требуют решения в документации)

### 1. Множители Grade техник

**Код (GameConstants.cs):**
| Grade | Код |
|-------|-----|
| Common | 1.0 |
| Refined | 1.3 |
| Perfect | 1.6 |
| Transcendent | 2.0 |

**Документация (TECHNIQUE_SYSTEM.md):**
| Grade | Документация |
|-------|--------------|
| Common | 1.0 |
| Refined | 1.2 |
| Perfect | 1.4 |
| Transcendent | 1.6 |

**Статус:** ⚠️ Требует решения: обновить код или документацию

---

### 2. Ultimate множитель

**Код:** `ULTIMATE_DAMAGE_MULTIPLIER = 2.0`
**Документация (TECHNIQUE_SYSTEM.md):** "Множитель урона: ×1.3"

**Статус:** ⚠️ Требует решения: обновить код или документацию

---

## ✅ Проверенные соответствия

### 1. Таблица подавления уровнем (ALGORITHMS.md §1)

| Разница | Normal | Technique | Ultimate | Статус |
|---------|--------|-----------|----------|--------|
| 0 | ×1.0 | ×1.0 | ×1.0 | ✅ |
| 1 | ×0.5 | ×0.75 | ×1.0 | ✅ |
| 2 | ×0.1 | ×0.25 | ×0.5 | ✅ |
| 3 | ×0.0 | ×0.05 | ×0.25 | ✅ |
| 4 | ×0.0 | ×0.0 | ×0.1 | ✅ |
| 5+ | ×0.0 | ×0.0 | ×0.0 | ✅ |

### 2. Формула ёмкости техник (ALGORITHMS.md §3)

```
capacity = baseCapacity × 2^(level-1) × (1 + mastery × 0.5%)
```
✅ Совпадает с кодом

### 3. Распределение урона по HP (ALGORITHMS.md §9)

```
redHP -= damage × 0.7  (функциональная)
blackHP -= damage × 0.3 (структурная)
```
✅ Совпадает с константами `RED_HP_RATIO` и `BLACK_HP_RATIO`

### 4. Формулы активной защиты (ALGORITHMS.md §5.2)

```
dodgeChance = 5% + (AGI-10) × 0.5% - armorDodgePenalty
parryChance = weaponParryBonus + (AGI-10) × 0.3%
blockChance = shieldBlock + (STR-10) × 0.2%
```
✅ Совпадает с DefenseProcessor

### 5. Шансы попадания по частям тела (ALGORITHMS.md §8)

| Часть | Документация | Код | Статус |
|-------|--------------|-----|--------|
| head | 5% | 0.05f | ✅ |
| torso | 40% | 0.40f | ✅ |
| heart | 2% | 0.02f | ✅ |
| arms | 10% | 0.10f | ✅ |
| legs | 12% | 0.12f | ✅ |
| hands | 4% | 0.04f | ✅ |
| feet | 0.5% | 0.005f | ✅ |

---

## 📊 10-слойный пайплайн урона

**Истинный порядок (ALGORITHMS.md §5):**

| Слой | Название | Реализация |
|------|----------|------------|
| 1 | Исходный урон | ✅ DamageCalculator |
| 2 | Level Suppression | ✅ DamageCalculator |
| 3 | Определение части тела | ✅ DefenseProcessor.RollBodyPart() |
| 4 | Активная защита | ✅ DefenseProcessor |
| 5 | Qi Buffer | ✅ QiBuffer |
| 6 | Покрытие брони | ✅ DefenseProcessor |
| 7 | Снижение бронёй | ✅ DefenseProcessor |
| 8 | Материал тела | ✅ DefenseProcessor |
| 9 | Распределение по HP | ✅ DamageCalculator |
| 10 | Последствия | ✅ DamageCalculator |

---

## 📝 Следующие шаги

1. **Приоритет HIGH:** Решить расхождение множителей Grade (код vs документация)
2. **Приоритет HIGH:** Решить расхождение Ultimate множителя (2.0 vs 1.3)
3. **Приоритет MEDIUM:** Добавить单元 тесты для QiBuffer с разными типами урона

---

## 📚 Связанные документы

- [ALGORITHMS.md](../docs/ALGORITHMS.md)
- [TECHNIQUE_SYSTEM.md](../docs/TECHNIQUE_SYSTEM.md)
- [COMBAT_SYSTEM.md](../docs/COMBAT_SYSTEM.md)
- [ENTITY_TYPES.md](../docs/ENTITY_TYPES.md)

---

*Отчёт создан: 2026-03-31 09:24:43 UTC*

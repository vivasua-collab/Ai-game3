# План исправлений по результатам аудита

**Дата:** 2026-04-10 13:37:00 UTC
**Основа:** CONSOLIDATED_AUDIT.md (210 проблем: 36 CRITICAL, 59 HIGH, 70 MEDIUM, 45 LOW)
**Контекст:** ~200k токенов на сессию
**Обновлено:** Решения пользователя внесены

---

## ✅ Решения пользователя (ПРИНЯТЫ)

| Вопрос | Решение | Влияние |
|--------|---------|---------|
| MAX_STAT_VALUE | **1000** — единая константа в GameConstants | CORE-C07 |
| Противоположности стихий | **Вариант А:** Lightning ↔ Void; Poison особая с Fire ×1.2; Neutral ×1.0 | CORE-H01 |
| Disposition | **Attitude + PersonalityTrait[Flags]** — полное разделение | CORE-M01 |
| Qi модель прорыва | **Модель В** — Растущий резервуар + Сжатие Ци | QI-C01, Fix-03 |
| Weather System | Отложить (отметить в документации как запланированный) | ARC-C01 |

---

## 📊 Сводка чекпоинтов

| # | Чекпоинт | Файлов | Строк | Проблем | Приоритет |
|---|----------|--------|-------|---------|-----------|
| 01 | [Qi int→long + Core типы](./04_10_fix01_core_qi_types.md) | 8 | ~3200 | 13 | P0 🔴 |
| 02 | [Combat Damage Pipeline](./04_10_fix02_combat_pipeline.md) | 4 | ~1241 | 9 | P0 🔴 |
| 03 | [Qi System + Technique + Модель В](./04_10_fix03_qi_technique.md) | 2 | ~941 | 9 | P0-HIGH |
| 04 | [Core System](./04_10_fix04_core_system.md) | 4 | ~2620 | 12 | P0-HIGH |
| 05 | [Body System](./04_10_fix05_body_system.md) | 3 | ~931 | 7 | P0 |
| 06 | [Buff + Formation](./04_10_fix06_buff_formation.md) | 5 | ~3250 | 18 | P0-HIGH |
| 07 | [NPC + Quest + Dialogue](./04_10_fix07_npc_quest_dialogue.md) | 7 | ~2620 | 15 | P0-HIGH |
| 08 | [Save + Inventory](./04_10_fix08_save_inventory.md) | 8 | ~4200 | 12 | P0-HIGH |
| 09 | [World System](./04_10_fix09_world_system.md) | 5 | ~2694 | 10 | P0-HIGH |
| 10 | [Managers + Player](./04_10_fix10_managers_player.md) | 6 | ~2448 | 18 | HIGH |
| 11 | [Data + Generators](./04_10_fix11_data_generators.md) | 10 | ~4400 | 15 | HIGH |
| 12 | [UI + Tile + Tests](./04_10_fix12_ui_tile_tests.md) | 12 | ~5800 | 14 | MED-HIGH |
| 13 | [Documentation](./04_10_fix13_documentation.md) | 3 | — | 5 | MEDIUM |
| | **ИТОГО** | **77** | | **155** | |

> Примечание: Некоторые проблемы покрываются в нескольких чекпоинтах (каскадные эффекты). Оставшиеся 55 проблем (LOW + часть MEDIUM) не включены в чекпоинты — фиксить по мере необходимости.

---

## 🔄 Порядок выполнения и зависимости

```
Fix-01 (Qi types) ─────┬──→ Fix-02 (Combat) ──→ Fix-03 (Qi+Technique+ModelB)
                        │
                        ├──→ Fix-04 (Core) ──→ Fix-05 (Body)
                        │
                        ├──→ Fix-06 (Buff+Formation)
                        │
                        ├──→ Fix-07 (NPC+Quest+Dialogue) ←── Fix-04 (Attitude/PersonalityTrait)
                        │
                        ├──→ Fix-08 (Save+Inventory) ←── Fix-06, Fix-07
                        │
                        ├──→ Fix-09 (World) ←── Fix-04
                        │
                        ├──→ Fix-10 (Managers+Player) ←── Fix-04
                        │
                        ├──→ Fix-11 (Data+Generators) ←── Fix-04
                        │
                        └──→ Fix-12 (UI+Tile+Tests) ←── Fix-08
                        
Fix-13 (Documentation) ←── Все кодовые фиксы
```

### Строгий порядок (обязательный)

1. **Fix-01** — FIRST. Qi int→long миграция. Без этого Combat pipeline и все Qi-зависимые системы не смогут корректно работать.
2. **Fix-02** — после Fix-01. Combat pipeline зависит от SpendQi(long).
3. **Fix-03** — после Fix-01. Qi system + Technique + Модель В прорыва.
4. **Fix-04** — после Fix-01. Core types (Constants, Enums, StatDev, GameEvents). Включает все пользовательские решения.
5. **Fix-05** — после Fix-04. Body зависит от CORE-C01.
6. **Fix-06 → Fix-12** — можно выполнять в любом порядке после Fix-01 и Fix-04.
7. **Fix-08** — желательно после Fix-06 и Fix-07 (SaveData классы зависят от Buff/NPC/Quest).
8. **Fix-13** — LAST. После всех кодовых фиксов.

---

## 🧮 Контекстный бюджет на чекпоинт

| Чекпоинт | Исходный код (чтение) | Изменения (запись) | Оверхед | Итого оценка |
|----------|----------------------|-------------------|---------|-------------|
| Fix-01 | ~10k токенов | ~8k | ~5k | ~23k |
| Fix-02 | ~5k | ~5k | ~5k | ~15k |
| Fix-03 | ~3k | ~4k | ~5k | ~12k |
| Fix-04 | ~8k | ~8k | ~5k | ~21k |
| Fix-05 | ~3k | ~3k | ~5k | ~11k |
| Fix-06 | ~10k | ~8k | ~5k | ~23k |
| Fix-07 | ~8k | ~7k | ~5k | ~20k |
| Fix-08 | ~13k | ~10k | ~5k | ~28k |
| Fix-09 | ~8k | ~6k | ~5k | ~19k |
| Fix-10 | ~8k | ~6k | ~5k | ~19k |
| Fix-11 | ~14k | ~8k | ~5k | ~27k |
| Fix-12 | ~18k | ~8k | ~5k | ~31k |
| Fix-13 | ~5k | ~3k | ~5k | ~13k |

> Все чекпоинты в пределах 31k токенов — безопасный запас для 200k контекста.

---

## 📈 Покрытие аудита чекпоинтами

| Категория | Всего | В чекпоинтах | Без чекпоинта |
|-----------|-------|--------------|----------------|
| 🔴 CRITICAL | 36 | 36 (100%) | 0 |
| 🟠 HIGH | 59 | 45 (76%) | 14 (LOW→HIGH повышенные, нишевые) |
| 🟡 MEDIUM | 70 | 25 (36%) | 45 (косметика, оптимизация) |
| 🟢 LOW | 45 | 5 (11%) | 40 (стиль, Debug.Log, регионы) |

---

## 📋 Ключевые формулы (для справки при имплементации)

### OppositeElements (Вариант А)

```
Fire ↔ Water       ×1.5 противоположный, ×0.8 сродство
Earth ↔ Air        ×1.5 противоположный, ×0.8 сродство
Lightning ↔ Void   ×1.5 противоположный, ×0.8 сродство
Fire → Poison      ×1.2 (выжигание токсинов, одностороннее)
Void → All         ×1.2 (поглощение, существующее)
Neutral → All      ×1.0 (без бонусов)
```

### Модель В прорыва

```
coreCapacity = 1000 × 1.1^totalSubLevels × qualityMultiplier
qiDensity = 2^(level-1)
effectiveQi = coreCapacity × qiDensity
conductivity = coreCapacity / 360

Требование прорыва = capacity(nextLevel) × density(nextLevel)
После прорыва: currentQi = 0
```

### Attitude пороги

```
Hatred:      -100..-51  → атака без предупреждения
Hostile:     -50..-21   → атака если спровоцирован
Unfriendly:  -20..-10   → избегание
Neutral:     -9..9      → безразличие
Friendly:    10..49     → помощь, торговля
Allied:      50..79     → лояльность
SwornAlly:   80..100    → самопожертвование
```

### PersonalityTrait [Flags]

```
None=0, Aggressive=1, Cautious=2, Treacherous=4, Ambitious=8,
Loyal=16, Pacifist=32, Curious=64, Vengeful=128
```

---

*План обновлён: 2026-04-10 13:37:00 UTC*
*Решения пользователя: MAX_STAT_VALUE=1000, Lightning↔Void, Attitude+PersonalityTrait, Модель В*

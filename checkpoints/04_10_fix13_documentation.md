# Чекпоинт: Fix-13 — Documentation Updates

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** MEDIUM

---

## Описание

Обновление документации после исправления кода. Несоответствия код↔документация выявлены аудитом. Все архитектурные решения приняты.

---

## Файлы (3 файла)

| # | Файл | Изменение |
|---|------|-----------|
| 1 | `docs/ARCHITECTURE.md` | Weather System: отметить как "не реализован"; обновить схему элементов (Lightning↔Void); обновить Disposition→Attitude+PersonalityTrait |
| 2 | `docs/SAVE_SYSTEM.md` | Обновить: Formation/Buff/Tile/Charger теперь сохраняются (после Fix-08); Qi long в SaveData |
| 3 | `docs/COMBAT_SYSTEM.md` | 10-й слой (Loot Generation): отметить как "запланирован"; обновить стихийную таблицу (Variant A) |

---

## Задачи

### MEDIUM (Docs-Code mismatches)
- [ ] DOC-M01: ARCHITECTURE.md — Weather System статус → "Запланирован" или убрать из списка реализованных систем
- [ ] DOC-M02: SAVE_SYSTEM.md — обновить описание после Fix-08:
  - Добавить FormationSaveData, BuffSaveData, TileSaveData, ChargerSaveData
  - Qi типы: int→long
  - NPC SaveData: дополненные поля
- [ ] DOC-M03: COMBAT_SYSTEM.md — Loot Generation: отметить как future/план

### Архитектурные решения (обновить документацию)
- [ ] ARC-DOC-01: ARCHITECTURE.md — обновить схему стихийных взаимодействий:
  - Fire ↔ Water (×1.5/×0.8)
  - Earth ↔ Air (×1.5/×0.8)
  - Lightning ↔ Void (×1.5/×0.8)
  - Fire → Poison (×1.2 одностороннее)
  - Void → All (×1.2)
  - Neutral → All (×1.0)
- [ ] ARC-DOC-02: ENTITY_TYPES.md / NPC_AI_SYSTEM.md — обновить Disposition → Attitude + PersonalityTrait
- [ ] ARC-DOC-03: QI_SYSTEM.md — обновить формулы прорыва (Модель В):
  - Требование прорыва = capacity(nextLevel) × density(nextLevel)
  - EffectiveQi = coreCapacity × qiDensity
  - Ссылка на BREAKTHROUGH_MODELS_COMPARISON.md
- [ ] ARC-DOC-04: ALGORITHMS.md §10 — обновить таблицу противоположностей

### Решения (ПРИНЯТЫ — зафиксировать в документации)
- [ ] MAX_STAT_VALUE = 1000
- [ ] Стихии: Вариант А (Lightning↔Void)
- [ ] Disposition → Attitude + PersonalityTrait[Flags]
- [ ] Qi Модель В (Растущий резервуар + Сжатие Ци)
- [ ] Weather System: отложен

---

## Зависимости

- **Предшествующие:** ВСЕ кодовые фиксы (Fix-01 — Fix-12)
- **Последующие:** нет

---

*Чекпоинт обновлён: 2026-04-10 13:37:00 UTC*

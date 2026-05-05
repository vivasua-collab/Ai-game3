# Чекпоинт: Обновление документации (пост-аудит)

**Дата:** 2026-05-05 14:06:23 MSK
**Фаза:** Пост-аудит — документация
**Статус:** in_progress

👉 Кодовая база: [05_05_docs_update_code.md](05_05_docs_update_code.md)

---

## Источник

Задачи перенесены из чекпоинтов Волны 1–3 и аудита экипировки:
- `05_05_audit_verification.md` — раздел «Не исправлено (низкий приоритет / документация)»
- `05_05_data_inventory_fix.md` — раздел «Не затронуты (низкий приоритет / документация)»
- `05_05_equipment_generation_fix.md` — НФ-1

---

## Задачи — обновление документации

### Чисто документация (обновить docs ↔ код)

- [ ] Н-01: Файловая структура документации не совпадает с реальной
- [ ] Н-02: WeaponSubtype расширен — обновить docs (не ошибка, расширение)
- [ ] Н-03: ArmorWeightClass не документирован
- [ ] С-14: FormationCoreType — обновить GLOSSARY.md
- [ ] НФ-1: chargeSpeedBonus = qiConductivity × 0.1f — не документировано в WeaponGenerator / EQUIPMENT_SYSTEM.md

### Документация + минимальный код

- [ ] В-13: CultivationLevelData qiDensity — формульная валидация (+ OnValidate)
- [ ] С-08: BodyPartState.Destroyed unreachable — убрать из enum ИЛИ задокументировать
- [ ] С-15: coreCapacityMultiplier — OnValidate (+ обновить docs)
- [ ] С-16: MortalStageData — дефолтные шансы пробуждения (+ обновить docs)
- [ ] С-17: FactionData.FactionType — согласовать с документацией
- [ ] Н-06: AIPersonality не связан с PersonalityTrait — задокументировать связь
- [ ] Н-07: MinChargeTime = 0.1f захардкожено — задокументировать или вынести в Constants
- [ ] Н-09: CombatEvents статические события без очистки — задокументировать жизненный цикл

### Информационные (не требуют действий)

- Н-04: DodgePenalty = 0f — устранено через В-01 ( EquipmentController )
- Н-05: TechniqueData.baseQiCost — long vs int — корректно, без изменений
- Н-08: FormationCore Drain через ProcessTimeTick — корректно, без изменений

---

## Изменённые файлы (план)

**Документация:**
- `docs/STRUCTURE.md` — Н-01
- `docs/EQUIPMENT_SYSTEM.md` — Н-02, Н-03, НФ-1
- `docs/GLOSSARY.md` — С-14
- `docs/BODY_SYSTEM.md` — С-08
- `docs/CULTIVATION_SYSTEM.md` — В-13, С-16
- `docs/FACTION_SYSTEM.md` — С-17
- `docs/COMBAT_SYSTEM.md` — Н-06, Н-07, Н-09

**Код:**
- `Scripts/Data/ScriptableObjects/CultivationLevelData.cs` — В-13, С-15
- `Scripts/Data/ScriptableObjects/MortalStageData.cs` — С-16
- `Scripts/Core/Enums.cs` — С-08 (если убираем Destroyed)

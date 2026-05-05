# Чекпоинт: Обновление документации (пост-аудит)

**Дата:** 2026-05-05 14:06:23 MSK
**Редактировано:** 2026-05-05 14:06:23 MSK — реструктуризация по указанию пользователя
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

- ~~Н-01: Файловая структура документации не совпадает с реальной~~ — **не требуется**, внутренние папки отличаются, копия делается вручную
- [ ] Н-02: WeaponSubtype расширен — обновить docs (не ошибка, расширение)
- [ ] Н-03: ArmorWeightClass не документирован
- [ ] НФ-1: chargeSpeedBonus = qiConductivity × 0.1f — внести в EQUIPMENT_SYSTEM.md, ⚠️ **перепроверить** формулу перед фиксацией
- С-14: FormationCoreType — перенесён в 👉 [05_05_glossary_update.md](05_05_glossary_update.md)

### Документация + минимальный код — ИСПОЛНЕНО ✅

- [x] В-13: CultivationLevelData qiDensity — формульная валидация ✅
- [x] С-08: BodyPartState.Destroyed unreachable — убран из enum ✅
- [x] С-15: coreCapacityMultiplier — OnValidate ✅
- [x] С-16: MortalStageData — дефолтные шансы пробуждения ✅
- [x] С-17: FactionData.FactionType — согласовано с документацией ✅
- [x] Н-06: AIPersonality ↔ PersonalityTrait — задокументирована связь ✅
- [x] Н-07: MinChargeTime → Constants + документация ✅
- [x] Н-09: CombatEvents — задокументирован жизненный цикл ✅

### Информационные (не требуют действий)

- Н-04: DodgePenalty = 0f — устранено через В-01 ( EquipmentController )
- Н-05: TechniqueData.baseQiCost — long vs int — корректно, без изменений
- Н-08: FormationCore Drain через ProcessTimeTick — корректно, без изменений

---

## Изменённые файлы

**Код (исполнено):**
- `Scripts/Data/ScriptableObjects/CultivationLevelData.cs` — В-13, С-15
- `Scripts/Data/ScriptableObjects/MortalStageData.cs` — С-16
- `Scripts/Core/Enums.cs` — С-08 (убран Destroyed)
- `Scripts/Core/Constants.cs` — Н-07 (MIN_CHARGE_TIME)
- `Scripts/Combat/CombatEvents.cs` — Н-09 (комментарии жизненного цикла)

**Документация (к исполнению):**
- `docs/EQUIPMENT_SYSTEM.md` — Н-02, Н-03, НФ-1
- `docs/BODY_SYSTEM.md` — С-08
- `docs/CULTIVATION_SYSTEM.md` — В-13, С-16
- `docs/FACTION_SYSTEM.md` — С-17
- `docs/COMBAT_SYSTEM.md` — Н-06, Н-07, Н-09

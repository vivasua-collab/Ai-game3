# Чекпоинт: Исправление констант и Enum

**Дата:** 2026-05-05 09:45:07 UTC
**Фаза:** Пост-аудит исправления — Волна 1
**Статус:** in_progress

👉 Кодовая база: [05_05_constants_enums_fix_code.md](05_05_constants_enums_fix_code.md)

---

## Выполненные задачи

- [x] Верификация проблем (все 9 подтверждены)

## Текущие задачи

- [ ] К-01: TechniqueGradeMultipliers → 1.3/1.6/2.0
- [ ] К-02: ULTIMATE_DAMAGE_MULTIPLIER → 2.0f
- [ ] К-03: GetEffectivenessMultiplier → 1.3/1.6/2.0
- [ ] К-04: Добавить Light в Element enum
- [ ] К-05: Защитный код для Poison в DamageCalculator
- [ ] В-16: QiStoneQuality → Damaged/Common/Refined/Perfect/Transcendent
- [ ] С-01: DurabilityCondition — 5 состояний вместо 6
- [ ] С-03: Физические константы QiBuffer → Constants.cs
- [ ] Н-11: basePlayerQi → 1000

## Проблемы

- К-04 (Light): добавление в enum может нарушить сериализацию существующих SO — проверить ElementData
- С-01 (Excellent): удаление из enum — breaking change для сохранений, нужна миграция
- К-03 (Effectiveness): снижение Transcendent с 3.25→2.0 — значительный нерф, проверить баланс

## Следующие шаги

1. Исправить К-01 + К-02 (простая замена чисел)
2. Исправить К-03 (заменить множители + убрать комментарии о средних)
3. Исправить К-04 + К-05 (Light + Poison guard)
4. Исправить В-16 (QiStoneQuality enum)
5. Исправить С-01 (убрать Excellent, сместить пороги)
6. Исправить С-03 (перенести константы)
7. Исправить Н-11 (100→1000)
8. Проверить компиляцию

## Изменённые файлы

- `Scripts/Core/Constants.cs` — К-01, К-02, С-03, Н-11
- `Scripts/Core/Enums.cs` — К-04, С-01, В-16
- `Scripts/Inventory/EquipmentController.cs` — К-03
- `Scripts/Charger/ChargerSlot.cs` — В-16
- `Scripts/Combat/DamageCalculator.cs` — К-05
- `Scripts/Combat/TechniqueCapacity.cs` — К-01 (комментарии)
- `Scripts/Combat/QiBuffer.cs` — С-03
- `Scripts/Inventory/InventoryController.cs` — С-01

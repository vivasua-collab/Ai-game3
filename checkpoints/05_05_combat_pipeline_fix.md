# Чекпоинт: Исправление боевого пайплайна

**Дата:** 2026-05-05 09:45:07 UTC
**Фаза:** Пост-аудит исправления — Волна 3 (самая сложная)
**Статус:** in_progress

👉 Кодовая база: [05_05_combat_pipeline_fix_code.md](05_05_combat_pipeline_fix_code.md)

---

## Выполненные задачи

- [x] Верификация проблем (все 11 подтверждены)

## Текущие задачи

- [ ] В-01: Парирование/блок — заменить хардкод на чтение из экипировки
- [ ] В-02: CombatantBase заглушки — пометить Obsolete (мёртвый код)
- [ ] В-03 + В-06: Износ брони и оружия — интегрировать DamageEquipment()
- [ ] В-04 + К-06 + К-07: Последствия урона (кровотечение, шок, оглушение)
- [ ] В-09: HasActiveShield() — проверить DefenseSubtype
- [ ] К-08 + С-10: Регенерация → OnWorldTick (BodyController + QiController)
- [ ] С-02: Формула урона оружия — полная реализация
- [ ] С-04: Стихийные эффекты — горение/замедление/оглушение

## Проблемы

- В-04 (Последствия): Новый функционал — нужны поля в DamageResult + обработчики
- К-08 (OnWorldTick): Нужно убедиться что TimeController корректно вызывает подписчиков
- С-02 (Формла урона): Изменение формулы может нарушить баланс
- В-09 (DefenseSubtype): Нужен новый enum + обновление TechniqueData — cascade change

## Следующие шаги

1. В-01 — Добавить BlockEffectiveness/ShieldEffectiveness в DefenderParams (~30 мин)
2. В-09 — Добавить DefenseSubtype enum, исправить HasActiveShield() (~20 мин)
3. К-08 + С-10 — Перенести регенерацию в OnWorldTick (~45 мин)
4. В-03 + В-06 — Интегрировать DamageEquipment() в боевой пайплайн (~45 мин)
5. В-04 + К-06 + К-07 — Реализовать Слой 10 (кровотечение/шок/оглушение) (~90 мин)
6. С-02 — Полная формула урона оружия (~45 мин)
7. С-04 — Стихийные эффекты (~60 мин)

## Изменённые файлы

- `Scripts/Combat/DamageCalculator.cs` — В-01, В-04, С-02, С-04
- `Scripts/Combat/Combatant.cs` — В-02
- `Scripts/Combat/TechniqueController.cs` — В-09
- `Scripts/Body/BodyController.cs` — К-08
- `Scripts/Qi/QiController.cs` — С-10, В-07, В-08
- `Scripts/Inventory/EquipmentController.cs` — В-03, В-06
- `Scripts/Core/Enums.cs` — В-09 (DefenseSubtype)
- `Scripts/Data/ScriptableObjects/TechniqueData.cs` — В-09

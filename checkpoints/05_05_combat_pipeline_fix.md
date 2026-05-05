# Чекпоинт: Исправление боевого пайплайна

**Дата:** 2026-05-05 09:45:07 UTC
**Фаза:** Пост-аудит исправления — Волна 3
**Статус:** complete ✅

👉 Кодовая база: [05_05_combat_pipeline_fix_code.md](05_05_combat_pipeline_fix_code.md)

---

## Выполненные задачи

- [x] Верификация проблем (все 11 подтверждены)

### Высокие
- [x] В-01: Парирование/блок — заменить хардкод на чтение из экипировки ✅
- [x] В-03 + В-06: Износ брони и оружия — интегрировать DamageEquipment() ✅
- [x] В-04 + К-06 + К-07: Последствия урона (кровотечение, шок, оглушение) ✅
- [x] К-08 + С-10: Регенерация → OnWorldTick (BodyController + QiController) ✅

### Средние
- [x] В-02: CombatantBase заглушки — пометить Obsolete ✅
- [x] В-07: QiController — проводимость от coreCapacity ✅
- [x] В-08: QiController — Meditate() по тикам ✅
- [x] В-09: HasActiveShield() — проверить DefenseSubtype ✅
- [x] С-02: Формула урона оружия — полная реализация ✅
- [x] С-04: Стихийные эффекты — горение/замедление/оглушение ✅

## Изменённые файлы

- `Scripts/Combat/DamageCalculator.cs` — В-01, В-04, В-06, С-02, С-04
- `Scripts/Combat/DefenseProcessor.cs` — В-01
- `Scripts/Combat/Combatant.cs` — В-01, В-02
- `Scripts/Combat/TechniqueController.cs` — В-09
- `Scripts/Combat/CombatManager.cs` — В-03, В-06
- `Scripts/Body/BodyController.cs` — К-08
- `Scripts/Body/BodyDamage.cs` — К-06, К-07, В-04
- `Scripts/Qi/QiController.cs` — С-10, В-07, В-08, В-10
- `Scripts/Inventory/EquipmentController.cs` — В-01, С-02
- `Scripts/Player/PlayerController.cs` — В-01, С-02
- `Scripts/NPC/NPCController.cs` — В-01, С-02
- `Scripts/Core/Enums.cs` — В-09, С-04
- `Scripts/Data/ScriptableObjects/TechniqueData.cs` — В-09

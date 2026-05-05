# Чекпоинт: Исправление данных и инвентаря

**Дата:** 2026-05-05 09:45:07 UTC
**Верификация:** 2026-05-05 14:06:23 MSK
**Фаза:** Пост-аудит исправления — Волна 2
**Статус:** complete ✅

👉 Кодовая база: [05_05_data_inventory_fix_code.md](05_05_data_inventory_fix_code.md)

---

## Выполненные задачи

- [x] Верификация проблем (все 25 подтверждены)

### Критические
- [x] К-09: SpiritStorage requiredCultivationLevel → 3 ✅
- [x] К-10: SpiritStorage — добавить лимит 20 слотов ✅
- [x] К-11: ChargerData — удалить бонусы к первичным статам ✅
- [x] К-12: NPCPresetData — добавить baseAttitude ✅

### Высокие
- [x] В-10: FormationEffects ApplyShield() — реальная реализация ✅
- [x] В-11: NPCPresetData PersonalityTrait [Flags] ✅
- [x] В-12: ElementData — список противоположностей ✅
- [x] В-14: BackpackData — ограничение «только расходники» для пояса ✅
- [x] В-15: MaterialData — добавить flexibility и qiRetention ✅

### Средние
- [x] С-05: Создать ConsumableSOFactory ✅
- [x] С-06: Переименовать Combat/LootGenerator → DeathLootGenerator ✅
- [x] С-07: Объединить StatBonus в единый класс ✅
- [x] С-09: Quadruped Torso HP → 100 ✅
- [x] С-11: IncreaseMastery baseGain → 1.0f/2.0f ✅
- [x] С-12: CombatTrigger fallback ✅
- [x] С-13: NPCPresetData Alignment → PersonalityTrait (Obsolete) ✅

## Документация — перенесено

Задачи по документации перенесены:
👉 [05_05_docs_update.md](05_05_docs_update.md) (В-13, С-08, С-14..С-17)

## Изменённые файлы

- `Scripts/Inventory/SpiritStorageController.cs` — К-09, К-10
- `Scripts/Charger/ChargerData.cs` — К-11
- `Scripts/Data/ScriptableObjects/NPCPresetData.cs` — К-12, В-11, С-13
- `Scripts/NPC/NPCController.cs` — К-12
- `Scripts/Formation/FormationEffects.cs` — В-10
- `Scripts/Qi/QiController.cs` — В-10 (AddTemporaryShield)
- `Scripts/Data/ScriptableObjects/ElementData.cs` — В-12
- `Scripts/Data/ScriptableObjects/BackpackData.cs` — В-14
- `Scripts/Data/ScriptableObjects/MaterialData.cs` — В-15
- `Scripts/Generators/ConsumableSOFactory.cs` — С-05 (новый)
- `Scripts/Combat/LootGenerator.cs` → `DeathLootGenerator` — С-06
- `Scripts/Combat/CombatManager.cs` — С-06 (using)
- `Scripts/Body/BodyDamage.cs` — С-09
- `Scripts/Combat/TechniqueController.cs` — С-11
- `Scripts/Combat/CombatTrigger.cs` — С-12
- `Scripts/Data/StatBonus.cs` — С-07 (новый)
- `Scripts/Data/ScriptableObjects/ItemData.cs` — С-07
- `Scripts/Generators/WeaponGenerator.cs` — С-07
- `Scripts/Generators/EquipmentSOFactory.cs` — С-07
- `Scripts/Data/ScriptableObjects/EquipmentData.cs` — С-07 (using)

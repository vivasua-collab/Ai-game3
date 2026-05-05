# Чекпоинт: Исправление данных и инвентаря

**Дата:** 2026-05-05 09:45:07 UTC
**Фаза:** Пост-аудит исправления — Волна 2
**Статус:** in_progress

👉 Кодовая база: [05_05_data_inventory_fix_code.md](05_05_data_inventory_fix_code.md)

---

## Выполненные задачи

- [x] Верификация проблем (все 25 подтверждены)

## Текущие задачи

### Критические
- [ ] К-09: SpiritStorage requiredCultivationLevel → 3
- [ ] К-10: SpiritStorage — добавить лимит 20 слотов
- [ ] К-11: ChargerData — удалить бонусы к первичным статам
- [ ] К-12: NPCPresetData — добавить baseAttitude

### Высокие
- [ ] В-10: FormationEffects ApplyShield() — реальная реализация
- [ ] В-11: NPCPresetData PersonalityTrait [Flags]
- [ ] В-12: ElementData — список противоположностей
- [ ] В-13: CultivationLevelData qiDensity — формульная валидация
- [ ] В-14: BackpackData — ограничение «только расходники» для пояса
- [ ] В-15: MaterialData — добавить flexibility и qiRetention

### Средние
- [ ] С-05: Создать ConsumableSOFactory
- [ ] С-06: Переименовать Combat/LootGenerator → DeathLootGenerator
- [ ] С-07: Объединить StatBonus в единый класс
- [ ] С-08: BodyPartState — убрать unreachable Destroyed
- [ ] С-09: Quadruped Torso HP → 100 (уточнить в документации)
- [ ] С-11: IncreaseMastery baseGain → 1.0f/2.0f
- [ ] С-12: CombatTrigger fallback — исправить обход attitude
- [ ] С-13: NPCPresetData Alignment → PersonalityTrait
- [ ] С-14: FormationCoreType — обновить GLOSSARY.md
- [ ] С-15: coreCapacityMultiplier — OnValidate
- [ ] С-16: MortalStageData — дефолтные шансы пробуждения
- [ ] С-17: FactionData.FactionType — согласовать с документацией

## Проблемы

- К-12 + В-11 + С-13 связаны: NPCPresetData нужно комплексное обновление (Attitude + PersonalityTrait + убрать Alignment + убрать baseDisposition)
- С-07 (StatBonus): объединение затронет WeaponGenerator.cs и ItemData.cs — cascade
- С-06 (LootGenerator rename): нужно обновить все using-ссылки

## Следующие шаги

1. К-09 + К-10 — SpiritStorage исправления (~15 мин)
2. К-11 — Удалить первичные бонусы из ChargerData (~10 мин)
3. К-12 + В-11 + С-13 — NPCPresetData комплексное обновление (~30 мин)
4. В-10 — ApplyShield реализация (~20 мин)
5. В-12 + В-13 + В-14 + В-15 — Data SO поля (~45 мин)
6. С-05 + С-06 + С-07 — Рефакторинг генераторов (~45 мин)
7. С-08..С-17 — Средние исправления (~60 мин)

## Изменённые файлы

- `Scripts/Inventory/SpiritStorageController.cs` — К-09, К-10
- `Scripts/Charger/ChargerData.cs` — К-11
- `Scripts/Data/ScriptableObjects/NPCPresetData.cs` — К-12, В-11, С-13
- `Scripts/Formation/FormationEffects.cs` — В-10
- `Scripts/Data/ScriptableObjects/ElementData.cs` — В-12
- `Scripts/Data/ScriptableObjects/CultivationLevelData.cs` — В-13, С-15
- `Scripts/Data/ScriptableObjects/BackpackData.cs` — В-14
- `Scripts/Data/ScriptableObjects/MaterialData.cs` — В-15
- `Scripts/Generators/ConsumableSOFactory.cs` — С-05 (новый)
- `Scripts/Combat/LootGenerator.cs` — С-06 (переименовать)
- `Scripts/Data/ScriptableObjects/ItemData.cs` — С-07
- `Scripts/Generators/WeaponGenerator.cs` — С-07
- `Scripts/Core/Enums.cs` — С-08
- `Scripts/Body/BodyDamage.cs` — С-09
- `Scripts/Combat/TechniqueController.cs` — С-11
- `Scripts/Combat/CombatTrigger.cs` — С-12

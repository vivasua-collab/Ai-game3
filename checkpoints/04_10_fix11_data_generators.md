# Чекпоинт: Fix-11 — Data/ScriptableObjects + Generators/Editor

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** HIGH

---

## Описание

Дубликаты enum (FactionRelationType, LocationType), деление на ноль в MortalStageData, TerrainConfig обрезает температуру, SceneSetupTools без #if UNITY_EDITOR ломает билд, генераторы с Qi float и кросс-зависимостями.

---

## Файлы (10 файлов, ~4400 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `Data/ScriptableObjects/FactionData.cs` | 329 | Duplicate FactionRelationType, GetRankByReputation |
| 2 | `Data/ScriptableObjects/LocationData.cs` | 262 | Duplicate LocationType, class name collision |
| 3 | `Data/ScriptableObjects/MortalStageData.cs` | ~230 | Деление на ноль, Qi int→long |
| 4 | `Data/ScriptableObjects/TerrainConfig.cs` | ~150 | Lava Range обрезает, singleton reload |
| 5 | `Data/ScriptableObjects/SpeciesData.cs` | ~200 | coreCapacityBase float→long, weaknesses/resistances |
| 6 | `Editor/SceneSetupTools.cs` | 628 | #if UNITY_EDITOR, reflection namespace |
| 7 | `Generators/GeneratorRegistry.cs` | 412 | Seed truncation, unbounded cache |
| 8 | `Generators/ConsumableGenerator.cs` | 619 | Qi float→long |
| 9 | `Generators/WeaponGenerator.cs` | 624 | Duplicate bonus statNames, nameEn=nameRu |
| 10 | `Generators/ArmorGenerator.cs` | 603 | Cross-dependency WeaponGenerator, nameEn=nameRu |

---

## Задачи

### CRITICAL
- [ ] DAT-C01: FactionRelationType — удалить дубликат из FactionData.cs, использовать Enums.cs. Если FactionData имеет больше значений (Vassal, etc.) — расширить Enums.cs
- [ ] DAT-C02: LocationType — удалить дубликат из LocationData.cs, расширить Enums.cs если нужно
- [ ] DAT-C03: MortalStageData.GetCoreFormationForAge — guard: `if (minAge >= maxAge) return 0;`
- [ ] DAT-C04: TerrainConfig — расширить Range для Lava (-200,200) или передавать temperatureModifier напрямую без Range clamp
- [ ] GEN-C01: SceneSetupTools — обернуть в `#if UNITY_EDITOR` / `#endif`

### HIGH
- [ ] DAT-H01: TechniqueData.baseQiCost int→long (если не в Fix-01)
- [ ] DAT-H02: SpeciesData.coreCapacityBase float→long (для Модели В)
- [ ] DAT-H03: LocationData class name collision — переименовать SO версию в LocationDataSO или LocationAsset
- [ ] GEN-H01: GeneratorRegistry seed — использовать long, не truncating to int
- [ ] GEN-H02: ArmorGenerator — убрать cross-dependency на WeaponGenerator
- [ ] GEN-H04: ConsumableGenerator — Qi float→long

### MEDIUM
- [ ] DAT-M01: FactionData.GetRankByReputation — отсортировать ranks перед поиском
- [ ] DAT-M03: TerrainConfig singleton — сброс в [RuntimeInitializeOnLoadMethod(SubsystemReload)]
- [ ] DAT-M04: SpeciesData — проверить пересечение weaknesses/resistances (элемент не может быть одновременно)
- [ ] DAT-M05: MortalStageData Qi int→long
- [ ] GEN-M01: AdjectiveForms — автодеривация неверна для русских прилагательных
- [ ] GEN-M02: TechniqueGenerator Combat names — дубли "Удар" ×3
- [ ] GEN-M06: GeneratorRegistry — bounded cache (LRU или maxSize)
- [ ] GEN-M08: SeededRandom NextGaussian — guard log(0) → -Infinity

---

## Порядок выполнения

1. FactionData.cs — убрать дубликат enum + GetRankByReputation
2. LocationData.cs — убрать дубликат enum + переименовать класс
3. MortalStageData.cs — деление на ноль + Qi long
4. TerrainConfig.cs — Range + singleton
5. SpeciesData.cs — coreCapacityBase long + weaknesses
6. SceneSetupTools.cs — #if UNITY_EDITOR
7. GeneratorRegistry.cs — seed long + cache
8. ConsumableGenerator.cs — Qi long
9. WeaponGenerator.cs + ArmorGenerator.cs — фиксы

---

## Зависимости

- **Предшествующие:** Fix-01 (Qi types), Fix-04 (Enums cleanup — удалить дубликаты)
- **Последующие:** нет прямых

---

*Чекпоинт обновлён: 2026-04-10 13:37:00 UTC*

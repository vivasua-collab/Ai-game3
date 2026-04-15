# Чекпоинт: Fix-11 — Data/ScriptableObjects + Generators/Editor

**Дата:** 2026-04-11 (updated)
**Фаза:** Phase 7 — Integration
**Статус:** completed
**Приоритет:** HIGH

---

## Описание

Дубликаты enum (FactionRelationType, LocationType), деление на ноль в MortalStageData, TerrainConfig обрезает температуру, SceneSetupTools без #if UNITY_EDITOR ломает билд, генераторы с Qi float и кросс-зависимостями.

---

## Файлы (11 файлов)

| # | Файл | Изменение |
|---|------|-----------|
| 1 | `Core/Enums.cs` | +Overlord/Rival to FactionRelationType, +Dungeon/Secret to LocationType |
| 2 | `Data/ScriptableObjects/FactionData.cs` | Removed duplicate FactionRelationType, GetRankByReputation sort, LocationData→LocationAsset ref |
| 3 | `Data/ScriptableObjects/LocationData.cs` | Removed duplicate LocationType, renamed class to LocationAsset |
| 4 | `Data/ScriptableObjects/MortalStageData.cs` | Division by zero guard, Qi int→long |
| 5 | `Data/TerrainConfig.cs` | Range -200..200 for Lava, singleton RuntimeInitializeOnLoadMethod reset |
| 6 | `Data/ScriptableObjects/SpeciesData.cs` | coreCapacityBase→LongMinMaxRange, OnValidate weaknesses/resistances check |
| 7 | `Editor/SceneSetupTools.cs` | #if UNITY_EDITOR guard |
| 8 | `Generators/SeededRandom.cs` | seed int→long, NextGaussian guard log(0) |
| 9 | `Generators/GeneratorRegistry.cs` | seed long, bounded cache (maxSize=100) |
| 10 | `Generators/ConsumableGenerator.cs` | ConsumableEffect Qi valueLong+isLongValue |
| 11 | `Generators/ArmorGenerator.cs` | Local GenerateGrade (removed WeaponGenerator dep) |
| 12 | `Generators/TechniqueGenerator.cs` | Combat names disambiguated |
| 13 | `Generators/Naming/AdjectiveForms.cs` | Warning comment about incorrect Russian auto-derivation |

---

## Задачи — все выполнены

### CRITICAL (5/5)
- [x] DAT-C01: FactionRelationType — добавлены Overlord/Rival в Enums.cs, дубликат удалён из FactionData.cs
- [x] DAT-C02: LocationType — добавлены Dungeon/Secret в Enums.cs, дубликат удалён из LocationData.cs
- [x] DAT-C03: MortalStageData.GetCoreFormationForAge — guard: `if (maxAge <= minAge) return minCoreFormation;`
- [x] DAT-C04: TerrainConfig — Range расширен с -50..50 до -200..200
- [x] GEN-C01: SceneSetupTools — обёрнут в `#if UNITY_EDITOR` / `#endif`

### HIGH (5/6 — DAT-H01 was already done)
- [x] DAT-H01: TechniqueData.baseQiCost — already long (verified, done in Fix-01)
- [x] DAT-H02: SpeciesData.coreCapacityBase — MinMaxRange→LongMinMaxRange (new class)
- [x] DAT-H03: LocationData→LocationAsset (SO class renamed, all references updated)
- [x] GEN-H01: SeededRandom seed int→long, GeneratorRegistry uses long seed
- [x] GEN-H02: ArmorGenerator — local GenerateGrade extracted, WeaponGenerator dependency removed
- [x] GEN-H04: ConsumableGenerator — ConsumableEffect.valueLong+isLongValue for Qi effects

### MEDIUM (8/8)
- [x] DAT-M01: FactionData.GetRankByReputation — sorted descending before search
- [x] DAT-M03: TerrainConfig singleton — RuntimeInitializeOnLoadMethod(SubsystemRegistration) reset
- [x] DAT-M04: SpeciesData — OnValidate checks weaknesses/resistances overlap
- [x] DAT-M05: MortalStageData Qi — minQiCapacity/maxQiCapacity int→long, GetRandomQiCapacity returns long
- [x] GEN-M01: AdjectiveForms — extensive warning comment about Russian adjective auto-derivation
- [x] GEN-M02: TechniqueGenerator Combat names — "Удар"×3 → "Удар кулаком"/"Удар ладонью"/"Рубящий удар"/"Толчковый удар"
- [x] GEN-M06: GeneratorRegistry — bounded cache with MaxCacheSize=100 and LRU eviction
- [x] GEN-M08: SeededRandom NextGaussian — `Math.Max(double.Epsilon, u1)` guard

---

## Зависимости

- **Предшествующие:** Fix-01 (Qi types), Fix-04 (Enums cleanup)
- **Последующие:** нет прямых

---

*Чекпоинт обновлён: 2026-04-11*

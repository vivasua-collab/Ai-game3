# Аудит Batch 3 — Data/ScriptableObjects + Generators/Editor + UI/Character/Tile/Tests

**Дата:** 2026-04-10  
**Агенты:** 3 параллельных агента  
**Файлов:** 52 файла (13 Data/SO + 17 Gen/Editor + 22 UI/Char/Tile/Tests)

Этот файл дополняет основной `AUDIT_2026-04-10.md` результатами третьей волны аудита.

---

## 📊 Итоговая статистика (все 7 агентов, 115 файлов)

| Категория | Исходный | Batch 1-2 (4 агента) | Batch 3 (3 агента) | Итого |
|-----------|---------|---------------------|-------------------|-------|
| 🔴 CRITICAL | 11 | +10 | +11 | **32** |
| 🟠 HIGH | 15 | +20 | +14 | **49** |
| 🟡 MEDIUM | 17 | +28 | +18 | **63** |
| 🟢 LOW | 15 | +18 | +14 | **47** |
| **Total** | **58** | **+76** | **+57** | **191** |

---

## 🔴 Новые CRITICAL от Batch 3 (3 агента)

### DA-C-01: Duplicate FactionRelationType enum
**Файлы:** Core/Enums.cs:466 vs Data/ScriptableObjects/FactionData.cs:37  
Core: 4 значения (Ally, Enemy, Neutral, Vassal). FactionData: 6 значений (+Overlord, Rival).  
**Fix:** Удалить дубликат из FactionData.cs, добавить Overlord/Rival в Core/Enums.cs.

### DA-C-02: Duplicate LocationType enum
**Файлы:** Core/Enums.cs:481 vs Data/ScriptableObjects/LocationData.cs:24  
Core: 4 значения. LocationData: 6 (+Dungeon, Secret). LocationController не может создавать Dungeon/Secret.  
**Fix:** Удалить дубликат, добавить Dungeon/Secret в Core/Enums.cs.

### GE-C-01: SceneSetupTools без #if UNITY_EDITOR → BUILD BREAK
**Файл:** Editor/SceneSetupTools.cs (весь файл)  
Использует UnityEditor, но БЕЗ `#if UNITY_EDITOR` / `#endif`. Ошибка компиляции в player build.  
**Fix:** Обернуть файл в `#if UNITY_EDITOR` / `#endif`.

### GE-C-02: FormationUIPrefabsGenerator — Texture2D/Sprite не сохранены
**Файл:** Editor/FormationUIPrefabsGenerator.cs:381-405  
Texture2D и Sprite создаются рантайм но НЕ сохраняются как ассеты. Сломанные ссылки в префабах.  
**Fix:** Использовать AssetDatabase.CreateAsset/AddObjectToAsset.

### UT-C-01: TileData temperature accumulation bug
**Файл:** Tile/TileData.cs:81 — `temperature += config.temperatureModifier;`  
При многократных вызовах температура накапливается.  
**Fix:** `temperature = baseTemperature + config.temperatureModifier;`

### UT-C-02: TileMapData DateTime не сериализуется JsonUtility
**Файл:** Tile/TileMapData.cs:35 — `public DateTime generatedAt;`  
Ранее исправлено в SaveManager, но TileMapData пропущен.  
**Fix:** Заменить на `long generatedAtTicks;`.

### UT-C-03: IntegrationTestScenarios PlayerSaveData Qi — float вместо long
**Файл:** Tests/IntegrationTestScenarios.cs:655-656  
`public float CurrentQi; public float MaxQi;` — потеря точности Qi.

### UT-C-04: IntegrationTestScenarios Dictionary не сериализуем
**Файл:** Tests/IntegrationTestScenarios.cs:551  
`Dictionary<string, EquipmentSaveData>` молча теряется при JsonUtility сериализации.

### UT-C-05: DialogUI.HideDialog деактивирует панель до завершения анимации
**Файл:** UI/DialogUI.cs:196-204  
SetTrigger(hideTrigger) + SetActive(false) — анимация не воспроизводится.

### DA-H-05→CRITICAL: MortalStageData.GetCoreFormationForAge деление на ноль
**Файл:** Data/ScriptableObjects/MortalStageData.cs:230  
`float progress = (float)(age - minAge) / (maxAge - minAge);` при minAge==maxAge → Infinity/NaN.

### DA-H-04→CRITICAL: TerrainConfig Lava Range(-50,50) против 1000f
**Файл:** Data/TerrainConfig.cs:47,126  
`[Range(-50f, 50f)]` молча обрезает temperatureModifier=1000f для Lava до 50f.

---

## 🟠 Новые HIGH от Batch 3

### DA-H-01: TechniqueData.baseQiCost int вместо long
TechniqueData.cs:70 — `public int baseQiCost = 10;` — нарушает конвенцию long Qi.

### DA-H-02: SpeciesData.coreCapacityBase float вместо long
SpeciesData.cs:74 — float теряет точность выше 16.7M.

### DA-H-03: Duplicate LocationData class name (SO vs plain)
LocationData.cs (SO) vs LocationController.cs:20 (plain class) — разные классы, одно имя.

### GE-H-01: GeneratorRegistry seed truncation long→int с sign issues
GeneratorRegistry.cs:100 — `(int)(worldSeed % int.MaxValue)` — отрицательный seed.

### GE-H-02: ArmorGenerator cross-dependency на WeaponGenerator
ArmorGenerator.cs:372 — `WeaponGenerator.GenerateGrade(rng, level)` — нарушение SRP.

### GE-H-03: NamingDatabase/NameBuilder мёртвый код
Полная система грамматического согласования построена, но генераторы используют хардкод.

### GE-H-04: ConsumableGenerator Qi float вместо long
ConsumableGenerator.cs:394-399 — Qi restoration как float.

### UT-H-01: 9 UI файлов FindFirstObjectByType вместо ServiceLocator
InventoryUI, CultivationProgressBar, HUDController, MenuUI, CharacterPanelUI, DialogUI, DestructibleObjectController, и др.

### UT-H-02: Qi long→float потеря точности на слайдерах (L7+)
CultivationProgressBar.cs:228, HUDController.cs:157, CharacterPanelUI.cs:256  
**Fix:** Нормализовать к 0-1: `qiSlider.value = (float)(currentQi / (double)maxQi);`

### UT-H-03: CharacterPanelUI GetComponentsInChildren каждый кадр
CharacterPanelUI.cs:507 — аллокация памяти каждый Update.

### UT-H-04: 4 UI файла старый Input System
InventoryUI, CultivationProgressBar, DialogUI — Input.GetKeyDown/Input.mousePosition.

### UT-H-05: DestructibleObjectController утечка Texture2D/Sprite
DestructibleObjectController.cs:316-342 — Runtime Texture2D/Sprite не уничтожаются.

### UT-H-06: WeaponDirectionIndicator неправильный namespace
WeaponDirectionIndicator.cs:8 — `CultivationWorld.UI` вместо `CultivationGame.UI`.

### UT-H-07: IndependentScale + CharacterSpriteController неправильный namespace
Character/IndependentScale.cs:6, CharacterSpriteController.cs:6 — `CultivationWorld.Character`.

### UT-H-08: IntegrationTests MockCombatant.SpendQi int вместо long
IntegrationTests.cs:859 — `public bool SpendQi(int amount)`.

### UT-H-09: IntegrationTests ConductivityPayback dead loop
IntegrationTests.cs:339-347 — Loop вычисляет expectedModifier но никогда не проверяет.

---

## 🟡 Новые MEDIUM от Batch 3 (ключевые)

- DA-M-01: FactionData.GetRankByReputation без сортировки
- DA-M-02: LocationData/BuffData циклические SO ссылки
- DA-M-03: Element.Count в enum — итерация даёт фейковый элемент
- DA-M-04: TerrainConfig singleton не сбрасывается при domain reload
- DA-M-05: SpeciesData weaknesses/resistances пересечение
- DA-M-06: MortalStageData Qi int вместо long
- GE-M-01: AdjectiveForms авто-деривация неверна для русских прилагательных
- GE-M-02: TechniqueGenerator Combat names дубли ("Удар" ×3)
- GE-M-03: TechniqueGenerator Healing генерирует damage потом очищает
- GE-M-04: WeaponGenerator duplicate bonus statNames
- GE-M-05: AssetGeneratorExtended Gloves → LeftHand slot
- GE-M-06: GeneratorRegistry unbounded cache
- GE-M-07: Weapon/Armor nameEn = nameRu
- GE-M-08: SeededRandom NextGaussian log(0) → -Infinity
- GE-M-09: SceneSetupTools reflection wrong namespace
- UT-M-01: ResourcePickup Time.time вместо unscaledTime
- UT-M-02: DestructibleSystem weak attacks → 0 damage
- UT-M-03: HUDController divide by zero в cooldown
- UT-M-04: IntegrationTests FormationStage duplicate enum
- UT-M-05: IntegrationTests test name contradicts assertion
- UT-M-06: CultivationProgressBar оба бара одинаковое значение
- UT-M-07: NPCAssemblyExample qiDensity underflow при level=0
- UT-M-08: BalanceVerification non-deterministic random
- UT-M-09: CharacterPanelUI CultivationLevel как int вместо имени

---

## 🟢 Новые LOW от Batch 3 (ключевые)

- DA-L-01: MinMaxRange.GetRandom() без min<=max
- DA-L-02: NPCPresetData.PersonalityTrait.intensity без Range
- DA-L-03: ItemData.ItemEffect.effectType string вместо enum
- DA-L-04: SpeciesData.innateTechniques List<string> без валидации
- DA-L-05: FactionData.JoinRequirement Quest/Item всегда true
- DA-L-06: MortalStageData Qi int вместо long
- GE-L-01..11: SeededRandom dead code, ID collision risk (5 генераторов), GeneratorRegistry без DontDestroyOnLoad, date typo
- UT-L-01..05: ResourcePickup TryAddToInventory always true, Camera.main без null check, Dictionary не сериализуем, float comparison без tolerance, duplicate helpers в 4+ UI файлов

---

## Файлы проверенные Batch 3

### Agent 3-a: Data/ScriptableObjects (13 файлов)
| Файл | Статус |
|------|--------|
| SpeciesData.cs | ⚠️ DA-C-01 (триггер), DA-H-02, DA-M-05, DA-L-04 |
| ElementData.cs | ⚠️ DA-M-03 (Element.Count) |
| TechniqueData.cs | ⚠️ DA-H-01 (int Qi) |
| MortalStageData.cs | ⚠️ DA-H-05 (div-by-zero), DA-L-06 |
| ItemData.cs | ⚠️ DA-L-03 |
| NPCPresetData.cs | ⚠️ DA-L-02 |
| BuffData.cs | ⚠️ DA-M-03 (циклические ссылки) |
| CultivationLevelData.cs | ✅ |
| FactionData.cs | ⚠️ DA-C-01 (duplicate enum), DA-M-01, DA-L-05 |
| LocationData.cs | ⚠️ DA-C-02 (duplicate enum), DA-H-03, DA-M-02 |
| FormationCoreData.cs | ✅ |
| TerrainConfig.cs | ⚠️ DA-H-04, DA-M-04 |

### Agent 3-b: Generators + Editor (17 файлов)
| Файл | Статус |
|------|--------|
| GeneratorRegistry.cs | ⚠️ GE-H-01, GE-M-06, GE-L-09..11 |
| SeededRandom.cs | ⚠️ GE-M-08, GE-L-01,02 |
| ArmorGenerator.cs | ⚠️ GE-H-02, GE-L-04,07 |
| NPCGenerator.cs | ⚠️ GE-L-09 |
| TechniqueGenerator.cs | ⚠️ GE-M-02,03, GE-L-09 |
| ConsumableGenerator.cs | ⚠️ GE-H-04, GE-L-07,09 |
| WeaponGenerator.cs | ⚠️ GE-M-04, GE-L-05,07,09 |
| Naming/GrammaticalGender.cs | ✅ |
| Naming/NounWithGender.cs | ✅ |
| Naming/AdjectiveForms.cs | ⚠️ GE-M-01 |
| Naming/NameBuilder.cs | ⚠️ GE-H-03 |
| Naming/NamingDatabase.cs | ✅ |
| Editor/AssetGeneratorExtended.cs | ⚠️ GE-M-05 |
| Editor/FormationAssetGenerator.cs | ✅ |
| Editor/SceneSetupTools.cs | ⚠️ GE-C-01, GE-M-09 |
| Editor/AssetGenerator.cs | ✅ |
| Editor/FormationUIPrefabsGenerator.cs | ⚠️ GE-C-02 |

### Agent 3-c: UI + Character + Tile + Tests (22 файла)
| Файл | Статус |
|------|--------|
| UI/InventoryUI.cs | ⚠️ UT-H-01, UT-H-04 |
| UI/CultivationProgressBar.cs | ⚠️ UT-H-01, UT-H-02, UT-M-06 |
| UI/WeaponDirectionIndicator.cs | ⚠️ UT-H-06 |
| UI/HUDController.cs | ⚠️ UT-H-01, UT-M-03 |
| UI/MenuUI.cs | ⚠️ UT-H-01 |
| UI/CharacterPanelUI.cs | ⚠️ UT-H-01, UT-H-02, UT-H-03, UT-M-09 |
| UI/DialogUI.cs | ⚠️ UT-C-05, UT-H-01, UT-H-04 |
| Character/IndependentScale.cs | ⚠️ UT-H-07 |
| Character/CharacterSpriteController.cs | ⚠️ UT-H-07, UT-L-02 |
| Tile/TileMapController.cs | ⚠️ Minor |
| Tile/GameTile.cs | ⚠️ Minor |
| Tile/ResourcePickup.cs | ⚠️ UT-M-01, UT-L-01 |
| Tile/TileData.cs | ⚠️ UT-C-01 |
| Tile/TileMapData.cs | ⚠️ UT-C-02 |
| Tile/DestructibleObjectController.cs | ⚠️ UT-H-01, UT-H-05 |
| Tile/DestructibleSystem.cs | ⚠️ UT-M-02 |
| Tile/TileEnums.cs | ✅ |
| Examples/NPCAssemblyExample.cs | ⚠️ UT-M-07 |
| Tests/IntegrationTests.cs | ⚠️ UT-C-03, UT-H-08, UT-H-09, UT-M-04,05 |
| Tests/IntegrationTestScenarios.cs | ⚠️ UT-C-03, UT-C-04 |
| Tests/BalanceVerification.cs | ⚠️ UT-M-08 |
| Tests/CombatTests.cs | ⚠️ Minor |

---

*Batch 3 завершён: 2026-04-10 — 3 параллельных агента, 52 файла, 57 новых проблем*
*Совместно с Batch 1-2: 191 уникальных проблем в 115 файлах*

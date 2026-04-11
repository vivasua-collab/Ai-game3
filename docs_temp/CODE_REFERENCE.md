# CODE_REFERENCE — Cultivation World Simulator
> Генерировано: 2026-04-11 08:29:17 UTC  
> Проект: CultivationGame (namespace `CultivationGame.*`)  
> Путь: `Assets/Scripts/`

---

## 1. Namespace Map

| Namespace | Папка | Файлы | Основные типы |
|-----------|-------|-------|---------------|
| `CultivationGame.Core` | Core/ | Enums.cs, VFXPool.cs, GameEvents.cs, Camera2DSetup.cs, ServiceLocator.cs, Constants.cs, GameSettings.cs, StatDevelopment.cs | GameConstants, GameEvents, ServiceLocator, VFXPool, TemporaryVFX, Camera2DSetup, GameSettings, AudioSettings, GraphicsSettings, StatDevelopment, RegisteredBehaviour\<T\> + **30 enum**, 3 struct |
| `CultivationGame.Body` | Body/ | BodyPart.cs, BodyController.cs, BodyDamage.cs | BodyPart, BodyController, BodyDamage, BodyDamageResult |
| `CultivationGame.Buff` | Buff/ | BuffManager.cs | BuffManager, ActiveBuff, BuffResult |
| `CultivationGame.Character` | Character/ | CharacterSpriteController.cs, IndependentScale.cs | CharacterSpriteController, IndependentScale |
| `CultivationGame.Charger` | Charger/ | ChargerSlot.cs, ChargerData.cs, ChargerController.cs, ChargerBuffer.cs, ChargerHeat.cs | ChargerSlot, QiStone, ChargerData, ChargerController, ChargerBuffer, ChargerHeat + enums |
| `CultivationGame.Combat` | Combat/ | CombatManager.cs, Combatant.cs, DamageCalculator.cs, CombatEvents.cs, DefenseProcessor.cs, HitDetector.cs, LevelSuppression.cs, QiBuffer.cs, TechniqueController.cs, TechniqueCapacity.cs | CombatManager, CombatantBase, ICombatant, ITechniqueUser, DamageCalculator, DefenseProcessor, HitDetector, LevelSuppression, QiBuffer, TechniqueController, TechniqueCapacity, CombatEvents, CombatLog + structs/params |
| `CultivationGame.Combat.Effects` | Combat/Effects/ | TechniqueEffect.cs, TechniqueEffectFactory.cs, ExpandingEffect.cs, DirectionalEffect.cs, FormationArrayEffect.cs | TechniqueEffect, TechniqueEffectFactory, ExpandingEffect, DirectionalEffect, FormationArrayEffect |
| `CultivationGame.Combat.OrbitalSystem` | Combat/OrbitalSystem/ | OrbitalWeapon.cs, OrbitalWeaponController.cs | OrbitalWeapon, OrbitalWeaponController, ICombatTarget, IHealth, DamageInfo |
| `CultivationGame.Data.ScriptableObjects` | Data/ScriptableObjects/ | ElementData.cs, NPCPresetData.cs, MortalStageData.cs, LocationData.cs, TechniqueData.cs, FactionData.cs, ItemData.cs, CultivationLevelData.cs, FormationCoreData.cs, SpeciesData.cs, BuffData.cs | ElementData, NPCPresetData, MortalStageData, LocationAsset, TechniqueData, FactionData, ItemData, EquipmentData, MaterialData, CultivationLevelData, FormationCoreData, SpeciesData, BuffData + enums |
| `CultivationGame.Editor` | Editor/ | AssetGenerator.cs, AssetGeneratorExtended.cs, FormationAssetGenerator.cs, FormationUIPrefabsGenerator.cs, SceneSetupTools.cs | AssetGenerator, AssetGeneratorExtended, FormationAssetGenerator, FormationUIPrefabsGenerator, SceneSetupTools |
| `CultivationGame.Examples` | Examples/ | NPCAssemblyExample.cs | NPCAssemblyExample |
| `CultivationGame.Formation` | Formation/ | FormationCore.cs, FormationQiPool.cs, FormationController.cs, FormationUI.cs, FormationEffects.cs, FormationData.cs | FormationCore, FormationQiPool, FormationController, FormationUI, FormationEffects, FormationData, IControlReceiver, IStunnable + enums |
| `CultivationGame.Generators` | Generators/, Generators/Naming/ | NPCGenerator.cs, ArmorGenerator.cs, TechniqueGenerator.cs, ConsumableGenerator.cs, SeededRandom.cs, GeneratorRegistry.cs, WeaponGenerator.cs, NameBuilder.cs, NamingDatabase.cs, AdjectiveForms.cs, NounWithGender.cs, GrammaticalGender.cs | NPCGenerator, ArmorGenerator, TechniqueGenerator, ConsumableGenerator, SeededRandom, GeneratorRegistry, WeaponGenerator, NameBuilder, NameGenerator, NamingDatabase + enums/structs |
| `CultivationGame.Interaction` | Interaction/ | DialogueSystem.cs, InteractionController.cs | DialogueSystem, InteractionController, Interactable + enums |
| `CultivationGame.Inventory` | Inventory/ | EquipmentController.cs, MaterialSystem.cs, CraftingController.cs, InventoryController.cs | InventoryController, EquipmentController, MaterialSystem, CraftingController + inner classes |
| `CultivationGame.Managers` | Managers/ | GameInitializer.cs, SceneLoader.cs, GameManager.cs | GameInitializer, SceneLoader, GameManager |
| `CultivationGame.NPC` | NPC/ | NPCController.cs, NPCData.cs, RelationshipController.cs, NPCAI.cs | NPCController, NPCData, NPCAI, RelationshipController + enums |
| `CultivationGame.Player` | Player/ | SleepSystem.cs, PlayerController.cs, PlayerVisual.cs | PlayerController, PlayerState, SleepSystem, PlayerVisual + enums |
| `CultivationGame.Qi` | Qi/ | QiController.cs | QiController |
| `CultivationGame.Quest` | Quest/ | QuestController.cs, QuestObjective.cs, QuestData.cs | QuestController, QuestObjective, QuestData + enums |
| `CultivationGame.Save` | Save/ | SaveFileHandler.cs, SaveDataTypes.cs, SaveManager.cs | SaveManager, SaveFileHandler, GameSaveData, PlayerSaveData + save structs |
| `CultivationGame.Tests` | Tests/ | CombatTests.cs, BalanceVerification.cs, IntegrationTestScenarios.cs, IntegrationTests.cs | CombatTests, BalanceVerification, IntegrationTestScenarios, IntegrationTests + mocks |
| `CultivationGame.TileSystem` | Tile/ | TileEnums.cs, GameTile.cs, TileData.cs, TileMapData.cs, TileMapController.cs, DestructibleObjectController.cs, DestructibleSystem.cs, ResourcePickup.cs, TerrainConfig.cs | GameTile, TerrainTile, ObjectTile, TileData, TileObjectData, TileMapData, TileMapController, DestructibleObjectController, DestructibleSystem, IDestructible, ResourcePickup, TerrainConfig + enums |
| `CultivationGame.TileSystem.Editor` | Tile/Editor/ | TestLocationSetup.cs, TileSpriteGenerator.cs | TestLocationSetup, TileSpriteGenerator |
| `CultivationGame.UI` | UI/ | CharacterPanelUI.cs, CombatUI.cs, WeaponDirectionIndicator.cs, MenuUI.cs, DialogUI.cs, InventoryUI.cs, CultivationProgressBar.cs, UIManager.cs, HUDController.cs | UIManager, HUDController, CombatUI, InventoryUI, CharacterPanelUI, DialogUI, MenuUI, CultivationProgressBar, WeaponDirectionIndicator + inner UI classes |
| `CultivationGame.World` | World/ | TimeController.cs, WorldController.cs, FactionController.cs, LocationController.cs, EventController.cs, TestLocationGameController.cs | TimeController, WorldController, FactionController, LocationController, EventController, TestLocationGameController + enums |

---

## 2. Class Reference

### CultivationGame.Core

#### `GameConstants` (static class)
- **Файл:** Core/Constants.cs
- **Наследует:** —
- **Ключевые поля:** VERSION (string), SAVE_VERSION (int), BASE_STAT_VALUE, MAX_STAT_VALUE, BASE_CORE_CAPACITY, CORE_CAPACITY_GROWTH, LevelSuppressionTable (float[][]), QiDensityByLevel, RegenerationMultipliers, BodyPartHitChances, OppositeElements, TechniqueGradeMultipliers, EquipmentGradeMultipliers, RarityDropChances, DormantCoreFormation, MaxMortalQi, AwakeningChance, TimeSpeedMultipliers, DurabilityRanges, DurabilityEfficiency, BodyMaterialReduction, BodyMaterialHardness
- **Вложенный класс:** `SoftCaps` — константы мягких капов (SPEED_CAP, DAMAGE_CAP, CRIT_CHANCE_CAP, etc.)
- **Cross-refs:** Используется почти всеми системами боёв, Ци, тела

#### `GameEvents` (static class)
- **Файл:** Core/GameEvents.cs
- **Свойства:** DebugLogging (bool)
- **События:** OnGameStart, OnGamePause, OnGameResume, OnGameQuit, OnGameStateChanged, OnSceneLoaded, OnSceneUnloading, OnPlayerHealthChanged, OnPlayerQiChanged, OnPlayerCultivationLevelChanged, OnPlayerDeath, OnPlayerRevive, OnPlayerLocationChanged, OnPlayerMeditationStart/End, OnPlayerSleepStart/End, OnPlayerBreakthrough, OnCombatStart, OnCombatEnd, OnDamageDealt, OnDamageTaken, OnTechniqueUsed/Learned/Mastered, OnEnemyKilled, OnNPCInteract, OnDialogueStart/End, OnRelationChanged, OnNPCJoined/Left, OnTimeHourChanged, OnDayChanged, OnMonthChanged, OnYearChanged, OnTimeSpeedChanged, OnWorldEventTriggered/Ended, OnItemAdded/Removed/Equipped/Unequipped/Crafted, OnQuestStarted/ObjectiveUpdated/Completed/Failed, OnGameSaving/Saved/Loading/Loaded
- **Методы-триггеры:** Trigger* для каждого события (30+ методов)
- **Метод:** ClearAllEvents()
- **Cross-refs:** GameState, TimeSpeed

#### `ServiceLocator` (static class)
- **Файл:** Core/ServiceLocator.cs
- **Методы:** Register\<T\>, Unregister\<T\>, Get\<T\>, GetOrFind\<T\>, TryGet\<T\>, Request\<T\>, IsRegistered\<T\>, Clear(), GetRegisteredServices()
- **Свойства:** DebugLogging, ServiceCount
- **Cross-refs:** Используется PlayerController, DestructibleObjectController, ResourcePickup и др.

#### `RegisteredBehaviour<T>` (abstract class, MonoBehaviour)
- **Файл:** Core/ServiceLocator.cs
- **Наследует:** MonoBehaviour
- **Авто-регистрация в ServiceLocator при Awake/OnDestroy**

#### `VFXPool` (MonoBehaviour, Singleton)
- **Файл:** Core/VFXPool.cs
- **Поля:** initialPoolSize (int), maxPoolSize (int), prewarmOnStart (bool)
- **Свойства:** Instance (static), TotalPooledObjects, ActiveObjectsCount
- **Методы:** Get(prefab, position, rotation), Return(instance, delay), GetAndReturn(prefab, pos, rot, returnDelay), ClearAllPools()
- **Статические:** Spawn(prefab, pos, rot, lifetime), SpawnDefault(prefab, pos)
- **Cross-refs:** TemporaryVFX

#### `TemporaryVFX` (MonoBehaviour)
- **Файл:** Core/VFXPool.cs
- **Метод:** Initialize(VFXPool pool, float delay)

#### `Camera2DSetup` (MonoBehaviour)
- **Файл:** Core/Camera2DSetup.cs
- **Поля:** cameraZ (float), orthographicSize (float), backgroundColor (Color), setupOnStart (bool)
- **Метод:** SetupCamera()

#### `GameSettings` (ScriptableObject)
- **Файл:** Core/GameSettings.cs
- **Поля:** gameName, version, startYear/Month/Day/Hour, defaultTimeSpeed (TimeSpeed), basePlayerHealth, basePlayerQi, startCultivationLevel/SubLevel, useLevelSuppression, maxLevelDifferenceForAttack, useQiBuffer, useKenshiDamage, maxNPCsInScene, npcDeactivationDistance, aiUpdateInterval, autoSave, autoSaveIntervalMinutes, maxSaveSlots, showHUD, showMinimap, minimapSize, debugMode, showGizmos, logCombat, godMode

#### `AudioSettings` (ScriptableObject)
- **Файл:** Core/GameSettings.cs
- **Поля:** masterVolume, musicVolume, sfxVolume, ambientVolume, loopMusic, musicFadeTime

#### `GraphicsSettings` (ScriptableObject)
- **Файл:** Core/GameSettings.cs
- **Поля:** defaultWidth/Height, fullscreen, vsync, qualityLevel, targetFrameRate, postProcessing, particles, maxParticleCount

#### `StatDevelopment` (class, [Serializable])
- **Файл:** Core/StatDevelopment.cs
- **Свойства:** Strength, Agility, Intelligence, Vitality (float), StrengthDelta/AgilityDelta/IntelligenceDelta/VitalityDelta (float), TrainingModifier, AgeModifier
- **Методы:** AddDelta(StatType, float, bool), AddCombatDelta(CombatActionType), AddTrainingDelta(StatType, float, TrainingType), GetThreshold(StatType), CanAdvance(StatType), GetProgress(StatType), ConsolidateSleep(float), SetStat(StatType, float), ModifyStat(StatType, float), ResetDeltas(), GetProgressInfo(), GetAllStatsAsDictionary(), GetStat(StatType), GetDelta(StatType), GetMaxDelta(StatType)
- **События:** OnStatChanged, OnDeltaAdded, OnStatIncreased, OnSleepConsolidated
- **Cross-refs:** GameConstants, StatType, CombatActionType, TrainingType, StatDevelopmentResult, SleepConsolidationResult

---

### CultivationGame.Combat

#### `ICombatant` (interface)
- **Файл:** Combat/Combatant.cs
- **Свойства:** Name, GameObject, CultivationLevel, CultivationSubLevel, Strength, Agility, Intelligence, Vitality, CurrentQi, MaxQi, QiDensity, QiDefense, HasShieldTechnique, BodyMaterial, HealthPercent, IsAlive, Penetration, DodgeChance, ParryChance, BlockChance, ArmorCoverage, DamageReduction, ArmorValue
- **Методы:** TakeDamage(BodyPartType, float), TakeDamageRandom(float), SpendQi(long), AddQi(long), GetAttackerParams(Element), GetDefenderParams()
- **События:** OnDeath, OnDamageTaken, OnQiChanged

#### `ITechniqueUser` (interface, extends ICombatant)
- **Файл:** Combat/Combatant.cs
- **Доп. свойства:** TechniqueController
- **Доп. методы:** CanUseTechnique(LearnedTechnique), UseTechnique(LearnedTechnique)

#### `CombatantBase` (abstract, MonoBehaviour, ICombatant)
- **Файл:** Combat/Combatant.cs
- **Поля:** combatantName, strength, agility, intelligence, vitality
- **Кэширует:** qiController (Qi.QiController), bodyController (Body.BodyController), techniqueController
- **Cross-refs:** QiController, BodyController, TechniqueController, DefenseProcessor

#### `CombatManager` (MonoBehaviour, Singleton)
- **Файл:** Combat/CombatManager.cs
- **Свойства:** Instance, State, IsInCombat, Combatants, CurrentAttacker, CurrentDefender
- **Методы:** InitiateCombat(ICombatant, ICombatant), ExecuteAttack(...), ExecuteTechniqueAttack(...), ExecuteBasicAttack(ICombatant, ICombatant), EndCombat(ICombatant, ICombatant), ForceEndCombat(), IsCombatantInCombat(ICombatant), GetOpponent(ICombatant)
- **События:** OnCombatStart, OnCombatEnd, OnAttackExecuted, OnStateChanged
- **Cross-refs:** ICombatant, DamageCalculator, CombatEvents, TechniqueCapacity, LevelSuppression

#### `DamageCalculator` (static class)
- **Файл:** Combat/DamageCalculator.cs
- **Методы:** CalculateDamage(int techniqueCapacity, AttackerParams, DefenderParams) → DamageResult, CalculateElementalInteraction(Element attacker, Element defender) → float
- **Cross-refs:** GameConstants, LevelSuppression, QiBuffer, DefenseProcessor, TechniqueCapacity

#### `DefenseProcessor` (static class)
- **Файл:** Combat/DefenseProcessor.cs
- **Методы:** ProcessDefense(float rawDamage, DefenseData) → DefenseResult, CalculateDodgeChance(int agility, float armorDodgePenalty), CalculateParryChance(int agility, float weaponParryBonus), CalculateBlockChance(int strength, float shieldBlockBonus), RollBodyPart() → BodyPartType, ApplySoftCap(float bonus, float cap, float decayRate)

#### `LevelSuppression` (static class)
- **Файл:** Combat/LevelSuppression.cs
- **Методы:** CalculateSuppression(int attackerLevel, int defenderLevel, AttackType, int techniqueLevel), CanDealDamage(...), GetSuppressionDescription(float), GetSuppressionByDiff(int levelDiff, AttackType)

#### `QiBuffer` (static class)
- **Файл:** Combat/QiBuffer.cs
- **Методы:** ProcessDamage(float, long, QiDefenseType), ProcessQiTechniqueDamage(float, long, QiDefenseType), ProcessPhysicalDamage(float, long, QiDefenseType) → QiBufferResult, CalculateRequiredQi(float, QiDefenseType, DamageSourceType), CanAbsorbDamage(float, long, QiDefenseType, DamageSourceType), GetDefenseEfficiency(QiDefenseType, DamageSourceType)

#### `TechniqueCapacity` (static class)
- **Файл:** Combat/TechniqueCapacity.cs
- **Методы:** CalculateCapacity(TechniqueType, CombatSubtype, int level, float mastery), GetBaseCapacity(TechniqueType, CombatSubtype), CalculateQiCost(long baseCapacity, int level), CalculateDamage(int capacity, TechniqueGrade, bool isUltimate), CalculateDestabilization(long, long, bool), CalculateCastTime(long qiCost, float conductivity, int cultivationLevel, float mastery), CanUseTechnique(long currentQi, long qiCost, int cultivationLevel, int requiredLevel)

#### `TechniqueController` (MonoBehaviour)
- **Файл:** Combat/TechniqueController.cs
- **Поля:** qiController, maxQuickSlots, maxUltimates
- **Свойства:** Techniques, TechniqueCount
- **Методы:** LearnTechnique(TechniqueData, float), HasTechnique(string), GetTechnique(string), AssignToQuickSlot(LearnedTechnique, int), GetQuickSlotTechnique(int), CanUseTechnique(LearnedTechnique), UseTechnique(LearnedTechnique), UseQuickSlot(int), ResetAllCooldowns(), GetSaveData(), LoadSaveData(...)
- **События:** OnTechniqueLearned, OnTechniqueUsed, OnTechniqueMastered, OnCooldownUpdated
- **Cross-refs:** QiController, TechniqueCapacity, LearnedTechnique

#### `CombatEvents` (static class)
- **Файл:** Combat/CombatEvents.cs
- **События:** OnCombatEvent, OnCombatStart, OnCombatEnd, OnDamageDealt, OnDamageTaken, OnCombatantDeath, OnTechniqueUsed, OnQiAbsorbedDamage
- **Методы:** Dispatch(CombatEventData), Dispatch(CombatEventType, ...), DispatchTechniqueUsed(...), DispatchDamage(...), DispatchDeath(...)

#### `CombatLog` (class)
- **Файл:** Combat/CombatEvents.cs
- **Методы:** Initialize(), Cleanup(), AddEntry(CombatEventArgs), GetEntries(int), Clear(), GetFormattedEntry(CombatEventData)
- **Событие:** OnEntryAdded

#### `HitDetector` (static class)
- **Файл:** Combat/HitDetector.cs
- **Методы:** FindNearestTarget(ICombatant, float, TargetType), FindTargetsInRange(ICombatant, float, TargetType), HasLineOfSight(ICombatant, ICombatant), HasLineOfSightToPoint(ICombatant, Vector3), CheckAttackFeasibility(ICombatant, ICombatant, CombatSubtype), GetAttackRange(CombatSubtype), CalculateHitChance(ICombatant, ICombatant, CombatSubtype), RollForHit(float), IsRangedAttack(CombatSubtype), FindTargetsInArea(Vector3, float, ICombatant), FindTargetsInCone(...)

---

### CultivationGame.Body

#### `BodyPart` (class, [Serializable])
- **Файл:** Body/BodyPart.cs
- **Свойства (read-only):** PartType, CustomName, IsVital, MaxRedHP, CurrentRedHP, MaxBlackHP, CurrentBlackHP, State, BaseHitChance
- **Методы:** TakeDamage(float red, float black) → bool, ApplyDamage(float total) → bool, Heal(float redHeal, float blackHeal) → bool, UpdateState(), IsFunctional(), IsSevered(), IsDisabled(), GetRedHPPercent(), GetBlackHPPercent(), Clone()
- **Internal:** SetCustomName(string), AddHitChanceModifier(float), SetHP(float red, float black)
- **Cross-refs:** BodyPartType, BodyPartState, GameConstants

#### `BodyController` (MonoBehaviour)
- **Файл:** Body/BodyController.cs
- **Поля:** speciesData, bodyMaterial, vitality, cultivationLevel, enableRegeneration, regenRate
- **Свойства:** BodyParts, BodyMaterial, Morphology, SoulType, IsAlive, HealthPercent, DamagePenalty, Vitality
- **Методы:** InitializeBody(), InitializeFromSpecies(SpeciesData), InitializeDefaultHumanoid(), TakeDamage(BodyPartType, float), TakeDamage(BodyPart, float), TakeDamageRandom(float), HealPart(BodyPartType, float, float), HealAll(float, float), FullRestore(), ApplyDamage(int), Heal(int), SetCultivationLevel(int), GetPart(BodyPartType), GetVitalParts(), GetDisabledParts(), GetSeveredParts()
- **События:** OnDamageTaken, OnPartSevered, OnDeath
- **Cross-refs:** BodyPart, BodyDamage, SpeciesData, GameConstants

#### `BodyDamage` (static class)
- **Файл:** Body/BodyDamage.cs
- **Методы:** ApplyDamage(BodyPart, float totalDamage) → BodyDamageResult, CreateHumanoidBody(int vitality), CreateQuadrupedBody(int vitality), CalculateDamagePenalty(List\<BodyPart\>), IsAlive(List\<BodyPart\>), GetOverallHealthPercent(List\<BodyPart\>)

---

### CultivationGame.Qi

#### `QiController` (MonoBehaviour)
- **Файл:** Qi/QiController.cs
- **Поля:** cultivationLevel, cultivationSubLevel, coreQuality, coreCapacity, currentQi, conductivity, conductivityBonus, enablePassiveRegen, regenMultiplier, hasShieldTechnique
- **Свойства:** CurrentQi, MaxQi, CoreCapacity, Conductivity, BaseConductivity, ConductivityBonus, CultivationLevel, QiPercent, QiDensity, IsFull, IsEmpty, EffectiveQi, QiDefense
- **Методы:** RecalculateStats(), SpendQi(long), AddQi(long), SetQi(long), RestoreFull(), Meditate(int durationTicks), SetCultivationLevel(int, int), CanBreakthrough(bool), CalculateBreakthroughRequirement(bool), PerformBreakthrough(bool), AbsorbDamage(float, bool), CanAbsorbDamage(float, bool), CalculateRequiredQiForDamage(float, bool), SetConductivityBonus(float), AddConductivityBonus(float), TransferToFormation(FormationCore, long), GetTransferRate(), EstimateCapacityAtLevel(int), EstimateCapacityAtSubLevel(int, int), GetQiInfo(), GetCultivationInfo()
- **События:** OnQiChanged, OnQiDepleted, OnQiFull, OnCultivationLevelChanged
- **Cross-refs:** GameConstants, QiBuffer, FormationCore, World.TimeController

---

### CultivationGame.Player

#### `PlayerController` (MonoBehaviour, ICombatant)
- **Файл:** Player/PlayerController.cs
- **Поля:** playerId, playerName, bodyController, qiController, techniqueController, interactionController, statDevelopment, sleepSystem, moveSpeed, runSpeedMultiplier, staminaCostPerSecond, worldController, timeController
- **Свойства:** PlayerId, PlayerName, State, StatDevelopment, CultivationLevel, IsAlive, CurrentLocation
- **ICombatant реализация:** Явные реализации через делегирование к QiController/BodyController
- **Методы:** StartMeditation(), AttemptBreakthrough(bool), TakeDamage(int, string), Heal(int), UseQuickSlot(int), SetLocation(string), Die(), Revive(float), AddStatExperience(StatType, float), AddCombatExperience(CombatActionType), StartSleep(float), EndSleep(), GetStat(StatType), GetStatProgress(StatType), LearnTechnique(TechniqueData, float), AssignTechniqueToSlot(int, LearnedTechnique), GetSaveData(), LoadSaveData(PlayerSaveData)
- **События:** OnHealthChanged, OnQiChanged, OnCultivationLevelChanged, OnLocationChanged, OnPlayerDeath, OnPlayerRevive
- **Cross-refs:** BodyController, QiController, TechniqueController, InteractionController, StatDevelopment, SleepSystem, ServiceLocator, ICombatant

#### `PlayerState` (class, [Serializable])
- **Файл:** Player/PlayerController.cs
- **Поля:** PlayerId, Name, CultivationLevel, CurrentQi, MaxQi, HealthPercent, CurrentStamina, MaxStamina, CurrentLocation, DeathCount, IsAlive, IsInCombat, IsMeditating, IsSleeping, IsTraveling

#### `SleepSystem` (MonoBehaviour)
- **Файл:** Player/SleepSystem.cs
- **Поля:** minSleepHours, maxSleepHours, optimalSleepHours, hpRecoveryRate, staminaRecoveryRate, statDevelopment, qiController, bodyController, timeController
- **Свойства:** CurrentState, IsAwake, IsSleeping, SleepProgress, HoursSlept
- **Методы:** StartSleep(float hours), EndSleep(), InterruptSleep(string), UpdateSleep(float gameHours), QuickSleep(float hours), GetStatusText()
- **События:** OnSleepStateChanged, OnSleepStarted, OnSleepEnded, OnSleepProgress
- **Cross-refs:** StatDevelopment, QiController, BodyController, TimeController

---

### CultivationGame.TileSystem

#### `GameTile` (TileBase)
- **Файл:** Tile/GameTile.cs
- **Поля:** sprite, color, terrainType (TerrainType), objectCategory (TileObjectCategory), objectType (TileObjectType), moveCost, isPassable, flags (GameTileFlags)
- **Метод:** GetTileData(Vector3Int, Tilemap, ref TileData)

#### `TerrainTile` (GameTile)
- **Файл:** Tile/GameTile.cs

#### `ObjectTile` (GameTile)
- **Файл:** Tile/GameTile.cs
- **Доп. поля:** width, height, durability, blocksVision, providesCover, isInteractable, isHarvestable

#### `TileData` (class, [Serializable])
- **Файл:** Tile/TileData.cs
- **Поля:** x, y, z, terrain (TerrainType), moveCost, objects (List\<TileObjectData\>), baseQiDensity, currentQiDensity, temperature, hasWater, waterDepth, flags, entityIds
- **Методы:** UpdateTerrainProperties(), IsPassable(bool canSwim, bool canFly), BlocksVision(), AddObject(TileObjectData), RemoveObject(TileObjectData), GetWorldPosition(), WorldToTile(Vector2)
- **Cross-refs:** TerrainConfig, GameTileFlags, TileObjectData

#### `TileObjectData` (class, [Serializable])
- **Файл:** Tile/TileData.cs
- **Поля:** objectId, objectType (TileObjectType), category (TileObjectCategory), width, height, isPassable, blocksVision, providesCover, isInteractable, isHarvestable, maxDurability, currentDurability, resourceId, resourceCount

#### `TileMapData` (class, [Serializable])
- **Файл:** Tile/TileMapData.cs
- **Поля:** mapId, mapName, width, height, tileSize, tiles (TileData[]), seed, biome, generatedAtTicks
- **Свойство:** GeneratedAt (DateTime helper)
- **Методы:** InBounds(int x, int y), GetTile(int x, int y), SetTile(int x, int y, TileData), CreateTile(int x, int y, TerrainType), FindTiles(TerrainType), FindObjects(TileObjectType), FindPassableNearby(int, int, int), WorldToTile(Vector2), TileToWorld(int, int), GetWorldSize(), ToJson(), FromJson(string)

#### `TileMapController` (MonoBehaviour)
- **Файл:** Tile/TileMapController.cs
- **Поля:** terrainTilemap, objectTilemap, overlayTilemap, tile references, defaultWidth/Height, generateOnStart
- **Свойства:** MapData, Width, Height
- **Методы:** GenerateTestMap(), GenerateMap(int, int, string), RenderMap(), GetTileAtWorld(Vector2), GetTile(int, int), SetTerrain(int, int, TerrainType), AddObject(int, int, TileObjectType), RemoveObject(int, int, TileObjectData), GetNeighbors(int, int, bool)
- **События:** OnTileChanged, OnMapGenerated

#### `DestructibleObjectController` (MonoBehaviour)
- **Файл:** Tile/DestructibleObjectController.cs
- **Методы:** DamageObjectAtTile(int, int, int, TileDamageType), DamageObjectAtWorld(Vector2, int, TileDamageType), GetObjectAtTile(int, int), GetObjectDurability(int, int), IsDestructible(int, int)
- **События:** OnObjectDamaged, OnObjectDestroyed, OnResourceDropped
- **Cross-refs:** TileMapController, ServiceLocator, ResourcePickup, TileObjectDestructibleExtensions

#### `IDestructible` (interface)
- **Файл:** Tile/DestructibleSystem.cs
- **Свойства:** CurrentDurability, MaxDurability, IsDestroyed
- **Методы:** TakeDamage(int, TileDamageType), Repair(int)
- **События:** OnDamageTaken, OnDestroyed

#### `ResourcePickup` (MonoBehaviour)
- **Файл:** Tile/ResourcePickup.cs
- **Поля:** resourceId, amount, pickupRadius, autoPickup, lifetime
- **Свойства:** ResourceId, Amount
- **Методы:** Initialize(string, int), Pickup(GameObject)
- **Событие:** OnPickedUp
- **Cross-refs:** ServiceLocator, InventoryController

---

### CultivationGame.Formation

#### `FormationCore` (MonoBehaviour)
- **Файл:** Formation/FormationCore.cs
- **Методы:** ContributeQi(GameObject, long, float), TransferQi(...)
- **Cross-refs:** FormationParticipant, FormationCoreData

#### `FormationQiPool` (class)
- **Файл:** Formation/FormationQiPool.cs
- **Константы:** FormationDrainConstants (static class)
- **Методы:** Управление пулом Ци формации

#### `FormationController` (MonoBehaviour)
- **Файл:** Formation/FormationController.cs
- **Внутренние классы:** KnownFormation, ImbuedCore, FormationSystemSaveData, KnownFormationSaveData, ImbuedCoreSaveData, FormationSaveData
- **Cross-refs:** FormationCore, ChargerController, FormationCoreData

#### `FormationEffects` (static class)
- **Файл:** Formation/FormationEffects.cs
- **Интерфейсы:** IControlReceiver, IStunnable
- **Cross-refs:** BuffManager, OrbitalWeaponController

#### `FormationData` (ScriptableObject)
- **Файл:** Formation/FormationData.cs
- **Enums:** FormationEffectType, ControlType, BuffType, FormationStage
- **Классы:** FormationEffect, FormationRequirement

#### `FormationUI` (MonoBehaviour)
- **Файл:** Formation/FormationUI.cs
- **Structs:** FormationUIState, FormationInfo

---

### CultivationGame.Save

#### `SaveManager` (MonoBehaviour)
- **Файл:** Save/SaveManager.cs
- **Классы:** SaveSlotInfo, GameSaveData, PlayerSaveData
- **Cross-refs:** GameEvents, NPCController, WorldController, CombatManager, FormationController, BuffManager, TileMapController, ChargerController, QuestController, PlayerController

#### `SaveFileHandler` (static class)
- **Файл:** Save/SaveFileHandler.cs

---

### CultivationGame.Buff

#### `BuffManager` (MonoBehaviour)
- **Файл:** Buff/BuffManager.cs
- **Классы:** ActiveBuff, BuffResult
- **Cross-refs:** BuffData, FormationController

---

### CultivationGame.Inventory

#### `InventoryController` (MonoBehaviour)
- **Файл:** Inventory/InventoryController.cs
- **Внутренние классы:** InventorySlot, ItemStack, InventorySlotSaveData

#### `EquipmentController` (MonoBehaviour)
- **Файл:** Inventory/EquipmentController.cs
- **Внутренние классы:** EquipmentInstance, EquipmentSlotsUI, EquipmentStats, EquipmentSaveData, EquipmentLayerSaveData
- **Cross-refs:** QiController, PlayerController

#### `CraftingController` (MonoBehaviour)
- **Файл:** Inventory/CraftingController.cs
- **Внутренние классы:** CraftingRecipe, CraftingIngredient, CraftingResult, RecipeValidation, MaterialRequirement, CraftingSaveData

#### `MaterialSystem` (MonoBehaviour)
- **Файл:** Inventory/MaterialSystem.cs
- **Внутренние классы:** MaterialProperties, MaterialCategoryConfig, MaterialInstance

---

### CultivationGame.NPC

#### `NPCController` (MonoBehaviour)
- **Файл:** NPC/NPCController.cs
- **Cross-refs:** ICombatant, QiController, BodyController, WorldController

#### `NPCData` (class)
- **Файл:** NPC/NPCData.cs
- **Enums:** NPCAIState
- **Классы:** SkillLevelEntry, SkillLevelData, NPCState, NPCInteractionResult, DialogueOption, NPCSaveData

#### `RelationshipController` (MonoBehaviour)
- **Файл:** NPC/RelationshipController.cs
- **Enums:** RelationshipType
- **Классы:** RelationshipRecord, RelationshipEvent, RelationshipSaveData
- **Cross-refs:** FactionController

#### `NPCAI` (MonoBehaviour)
- **Файл:** NPC/NPCAI.cs
- **Cross-refs:** QiController

---

### CultivationGame.World

#### `TimeController` (MonoBehaviour)
- **Файл:** World/TimeController.cs
- **Enum:** Season
- **Класс:** TimeSaveData
- **Методы:** AdvanceHours(int) и др.

#### `WorldController` (MonoBehaviour)
- **Файл:** World/WorldController.cs
- **Enums:** WorldEventType
- **Классы:** WorldData, WorldEvent, WorldEventDataEntry, WorldEventData, WorldStatistics, WorldSaveData
- **Cross-refs:** NPCController, NPCGenerator

#### `FactionController` (MonoBehaviour)
- **Файл:** World/FactionController.cs
- **Enums:** FactionType, FactionRank (дублирует Data.ScriptableObjects.FactionData.FactionRank!)
- **Классы:** FactionData, FactionMembership, FactionSystemSaveData, FactionMembershipSaveData

#### `LocationController` (MonoBehaviour)
- **Файл:** World/LocationController.cs
- **Классы:** LocationData, LocationTransition, LocationSaveData

#### `EventController` (MonoBehaviour)
- **Файл:** World/EventController.cs
- **Enums:** EventEffectType
- **Классы:** EventTemplate, EventEffect, ActiveEvent, EventSaveData

---

### CultivationGame.Charger

#### `ChargerController` (MonoBehaviour)
- **Файл:** Charger/ChargerController.cs
- **Struct:** ChargerUIState
- **Cross-refs:** QiController, ChargerData, ChargerSlot, ChargerBuffer, ChargerHeat

#### `ChargerData` (ScriptableObject)
- **Файл:** Charger/ChargerData.cs
- **Enums:** ChargerFormFactor, ChargerPurpose, ChargerMaterial, ChargerMode
- **Structs:** ChargerSlotData, ChargerBufferData

#### `ChargerSlot` (class)
- **Файл:** Charger/ChargerSlot.cs
- **Enums:** QiStoneQuality, QiStoneSize
- **Класс:** QiStone

#### `ChargerBuffer` (class)
- **Файл:** Charger/ChargerBuffer.cs
- **Struct:** ChargerBufferResult

#### `ChargerHeat` (class)
- **Файл:** Charger/ChargerHeat.cs
- **Enums:** HeatState
- **Structs:** HeatResult, HeatSaveData

---

### CultivationGame.Data.ScriptableObjects

#### `TechniqueData` (ScriptableObject)
- **Файл:** Data/ScriptableObjects/TechniqueData.cs
- **Поля:** techniqueId, techniqueName, techniqueType, combatSubtype, element, grade, baseQiCost, cooldown, minCultivationLevel, techniqueLevel, isUltimate, range, effects
- **Внутренний enum:** EffectType
- **Внутренний класс:** TechniqueEffect

#### `SpeciesData` (ScriptableObject)
- **Файл:** Data/ScriptableObjects/SpeciesData.cs
- **Enums:** SizeClass
- **Классы:** MinMaxRange, LongMinMaxRange, BodyPartConfig
- **Поля:** soulType, morphology, bodyMaterial, vitality, bodyParts

#### `ItemData` (ScriptableObject)
- **Файл:** Data/ScriptableObjects/ItemData.cs
- **Подклассы:** EquipmentData, MaterialData
- **Внутренние классы:** ItemEffect, StatRequirement, EquipmentLayer, StatBonus, SpecialEffect

#### `FactionData` (ScriptableObject)
- **Файл:** Data/ScriptableObjects/FactionData.cs
- **Enums:** FactionType, RequirementType, BenefitType
- **Классы:** FactionRelation, FactionResources, JoinRequirement, FactionBenefit, FactionRank

#### `FormationCoreData` (ScriptableObject)
- **Файл:** Data/ScriptableObjects/FormationCoreData.cs
- **Enums:** FormationCoreType, FormationCoreVariant, FormationType, FormationSize, QiStoneType
- **Класс:** QiStoneSlot

#### `BuffData` (ScriptableObject)
- **Файл:** Data/ScriptableObjects/BuffData.cs
- **Enums:** BuffType, BuffRemovalType, BuffCategory, StackType, PeriodicType, SpecialEffectType
- **Классы:** StatModifier, PeriodicEffect, SpecialBuffEffect

#### `ElementData` (ScriptableObject)
- **Файл:** Data/ScriptableObjects/ElementData.cs
- **Классы:** ElementEffect, EnvironmentEffect

#### `LocationAsset` (ScriptableObject)
- **Файл:** Data/ScriptableObjects/LocationData.cs
- **Enums:** ResourceType
- **Классы:** LocationResource, LocationConnection

#### `NPCPresetData` (ScriptableObject)
- **Файл:** Data/ScriptableObjects/NPCPresetData.cs
- **Enums:** Alignment, BehaviorType
- **Классы:** PersonalityTraitEntry, KnownTechnique, EquippedItem, InventoryItem

#### `CultivationLevelData` (ScriptableObject), `MortalStageData` (ScriptableObject)
- Данные для уровней культивации и стадий смертного

---

### CultivationGame.Generators

#### `NPCGenerator` (static class)
- **Файл:** Generators/NPCGenerator.cs
- **Enums:** NPCRole
- **Классы:** NPCGenerationParams, GeneratedNPC, GeneratedBodyPart
- **Cross-refs:** NPCPresetData, SpeciesData

#### `WeaponGenerator` (static class)
- **Файл:** Generators/WeaponGenerator.cs
- **Enums:** WeaponSubtype, WeaponClass, WeaponDamageType
- **Классы:** WeaponGenerationParams, GeneratedWeapon, StatBonus

#### `ArmorGenerator` (static class)
- **Файл:** Generators/ArmorGenerator.cs
- **Enums:** ArmorSubtype, ArmorWeightClass
- **Классы:** ArmorGenerationParams, GeneratedArmor, ArmorBonus

#### `TechniqueGenerator` (static class)
- **Файл:** Generators/TechniqueGenerator.cs
- **Классы:** TechniqueGenerationParams, GeneratedTechnique, TechniqueEffect

#### `ConsumableGenerator` (static class)
- **Файл:** Generators/ConsumableGenerator.cs
- **Enums:** ConsumableType, ConsumableEffectCategory
- **Классы:** ConsumableGenerationParams, GeneratedConsumable, ConsumableEffect

#### `GeneratorRegistry` (MonoBehaviour)
- **Файл:** Generators/GeneratorRegistry.cs
- **Struct:** GeneratorStatistics

#### `SeededRandom` (class)
- **Файл:** Generators/SeededRandom.cs

#### `NameBuilder` (class), `NameGenerator` (static class)
- **Файл:** Generators/Naming/NameBuilder.cs

#### `NamingDatabase` (static class)
- **Файл:** Generators/Naming/NamingDatabase.cs

#### `GrammaticalGender` (enum), `AdjectiveForms` (struct), `NounWithGender` (struct)
- **Файл:** Generators/Naming/

---

### CultivationGame.Interaction

#### `DialogueSystem` (MonoBehaviour)
- **Файл:** Interaction/DialogueSystem.cs
- **Enums:** DialogueActionType, DialogueConditionType, CompareOperator
- **Классы:** DialogueNode, DialogueNodeArrayWrapper, DialogueChoice, DialogueAction, DialogueCondition

#### `InteractionController` (MonoBehaviour)
- **Файл:** Interaction/InteractionController.cs
- **Enums:** InteractionType
- **Классы:** InteractionResult, Interactable (abstract)
- **Cross-refs:** NPCController, QiController, BodyController

---

### CultivationGame.Quest

#### `QuestController` (MonoBehaviour)
- **Файл:** Quest/QuestController.cs
- **Классы:** ActiveQuest, QuestSystemSaveData, ActiveQuestSaveData
- **Cross-refs:** WorldController

#### `QuestData` (ScriptableObject)
- **Файл:** Quest/QuestData.cs
- **Enums:** QuestType, QuestState
- **Классы:** QuestReward, QuestRequirements

#### `QuestObjective` (class)
- **Файл:** Quest/QuestObjective.cs
- **Enums:** QuestObjectiveType, ObjectiveState
- **Struct:** ObjectiveSaveData

---

### CultivationGame.UI

#### `UIManager` (MonoBehaviour)
- **Файл:** UI/UIManager.cs

#### `HUDController` (MonoBehaviour)
- **Файл:** UI/HUDController.cs
- **Enum:** NotificationType
- **Cross-refs:** World.TimeController

#### `CombatUI` (MonoBehaviour)
- **Файл:** UI/CombatUI.cs
- **Внутренние:** CombatantData, TechniqueUIData, TurnOrderEntry, LogEntry, LogType, ProgressBar, EnemyUIEntry, TechniqueButtonEntry

#### `InventoryUI` (MonoBehaviour)
- **Файл:** UI/InventoryUI.cs
- **Внутренние:** ItemSlotUI, ItemTooltipUI, ContextMenuUI, ContextMenuOption

#### `CharacterPanelUI` (MonoBehaviour)
- **Файл:** UI/CharacterPanelUI.cs
- **Внутренние:** BodyPartUI, EquipmentSlotUI, StatRowUI
- **Cross-refs:** BodyController, PlayerController, InventoryController, QiController

#### `DialogUI` (MonoBehaviour)
- **Файл:** UI/DialogUI.cs
- **Cross-refs:** NPCController, InteractionController

#### `MenuUI` (MonoBehaviour)
- **Файл:** UI/MenuUI.cs
- **Cross-refs:** SaveManager

#### `CultivationProgressBar` (MonoBehaviour)
- **Файл:** UI/CultivationProgressBar.cs
- **Внутренние:** QuickSlotPanel, QuickSlotUI, MinimapUI
- **Cross-refs:** QiController

#### `WeaponDirectionIndicator` (MonoBehaviour)
- **Файл:** UI/WeaponDirectionIndicator.cs
- **Cross-refs:** OrbitalWeaponController

---

## 3. Enum Reference

### CultivationGame.Core

| Enum | Значения | Атрибуты | Используется в |
|------|----------|----------|----------------|
| `MortalStage` | None=0, Newborn=1, Child=2, Adult=3, Mature=4, Elder=5, Awakening=9 | — | GameConstants (DormantCoreFormation, MaxMortalQi, AwakeningChance, AgeRanges) |
| `CultivationLevel` | None=0, AwakenedCore=1, LifeFlow=2, InternalFire=3, BodySpiritUnion=4, HeartOfHeaven=5, VeilBreaker=6, EternalRing=7, VoiceOfHeaven=8, ImmortalCore=9, Ascension=10 | — | PlayerController, QiController, GameConstants |
| `AwakeningType` | None, Natural, Guided, Artifact, Forced | — | GameConstants (AwakeningTypeMultipliers) |
| `CoreQuality` | Fragmented=1, Cracked=2, Flawed=3, Normal=4, Refined=5, Perfect=6, Transcendent=7 | — | QiController |
| `Element` | Neutral, Fire, Water, Earth, Air, Lightning, Void, Poison | — | DamageCalculator, GameConstants (OppositeElements), TechniqueData |
| `DamageType` | Physical, Qi, Elemental, Pure, Void | — | CombatUI (единственный потребитель в Core) |
| `TechniqueType` | Combat, Cultivation, Defense, Support, Healing, Movement, Sensory, Curse, Poison, Formation | — | TechniqueCapacity, TechniqueData, GameConstants |
| `CombatSubtype` | None, MeleeStrike, MeleeWeapon, RangedProjectile, RangedBeam, RangedAoe, DefenseBlock, DefenseShield, DefenseDodge | — | DamageCalculator, TechniqueCapacity, HitDetector |
| `TechniqueGrade` | Common, Refined, Perfect, Transcendent | — | TechniqueCapacity, GameConstants |
| `SoulType` | Character, Creature, Spirit, Artifact, Construct | — | BodyController, SpeciesData |
| `Morphology` | Humanoid, Quadruped, Bird, Serpentine, Arthropod, Amorphous, HybridCentaur, HybridMermaid, HybridHarpy, HybridLamia | — | BodyController, SpeciesData |
| `BodyMaterial` | Organic, Scaled, Chitin, Mineral, Ethereal, Construct, Chaos | — | BodyController, DefenseProcessor, GameConstants |
| `BodyPartType` | Head, Torso, Heart, LeftArm, RightArm, LeftLeg, RightLeg, LeftHand, RightHand, LeftFoot, RightFoot | — | BodyPart, BodyController, DefenseProcessor, ICombatant |
| `BodyPartState` | Healthy, Bruised, Wounded, Disabled, Severed, Destroyed | — | BodyPart |
| `EquipmentSlot` | None, WeaponMain, WeaponOff, Armor, Clothing, Charger, RingLeft, RingRight, Accessory, Backpack | — | EquipmentController, GameEvents |
| `ItemCategory` | Weapon, Armor, Accessory, Consumable, Material, Technique, Quest, Misc | — | ItemData |
| `ItemRarity` | Common, Uncommon, Rare, Epic, Legendary, Mythic | — | GameConstants (RarityDropChances) |
| `EquipmentGrade` | Damaged, Common, Refined, Perfect, Transcendent | — | TechniqueCapacity, GameConstants |
| `DurabilityCondition` | Pristine, Excellent, Good, Worn, Damaged, Broken | — | GameConstants |
| `MaterialTier` | Tier1=1, Tier2=2, Tier3=3, Tier4=4, Tier5=5 | — | MaterialSystem |
| `MaterialCategory` | Metal, Leather, Cloth, Wood, Bone, Crystal, Gem, Organic, Spirit, Void | — | MaterialSystem |
| `NPCCategory` | Temp, Plot, Unique | — | NPCData |
| `Disposition` ⚠️ | Hostile, Unfriendly, Neutral, Friendly, Allied, Aggressive, Cautious, Treacherous, Ambitious | **[Obsolete]** | Устарел, заменён на Attitude + PersonalityTrait |
| `Attitude` | Hatred, Hostile, Unfriendly, Neutral, Friendly, Allied, SwornAlly | — | NPCData, RelationshipController |
| `PersonalityTrait` | None=0, Aggressive=1, Cautious=2, Treacherous=4, Ambitious=8, Loyal=16, Pacifist=32, Curious=64, Vengeful=128 | **[Flags]** | NPCData, NPCAI, InteractionController |
| `FactionRelationType` | Ally, Enemy, Neutral, Vassal, Overlord, Rival | — | FactionData |
| `LocationType` | Region, Area, Building, Room, Dungeon, Secret | — | LocationData |
| `BiomeType` | Mountains, Plains, Forest, Sea, Desert, Swamp, Tundra, Jungle, Volcanic, Spiritual | — | LocationAsset, LocationController (переименован из TerrainType, FIX CORE-H01 2026-04-11) |
| `BuildingType` | House, Shop, Temple, Cave, Tower, SectHQ, Dojo, Forge, AlchemyLab, Library | — | LocationData |
| `TimeSpeed` | Paused, Normal, Fast, VeryFast | — | TimeController, GameConstants, GameEvents |
| `TimeOfDay` | Dawn, Morning, Noon, Afternoon, Evening, Night, Midnight | — | TimeController |
| `AttackType` | Normal, Technique, Ultimate | — | LevelSuppression, DamageCalculator |
| `CombatAttackResult` | Miss, Dodge, Parry, Block, Hit, CriticalHit, Kill | — | (переименован из AttackResult для избежания конфликта) |
| `CombatStage` | None, Initiative, PlayerTurn, EnemyTurn, Resolution, Victory, Defeat | — | (UI) |
| `SaveSlot` | Slot1, Slot2, Slot3, AutoSave, QuickSave | — | SaveManager |
| `SaveType` | Manual, Auto, Quick, Checkpoint | — | SaveManager |
| `GameState` | None, MainMenu, Loading, Playing, Paused, Inventory, Combat, Dialog, Cutscene, Settings | — | GameEvents, UIManager |

### CultivationGame.Core (StatDevelopment.cs)

| Enum | Значения | Используется в |
|------|----------|----------------|
| `StatType` | Strength, Agility, Intelligence, Vitality | StatDevelopment, PlayerController |
| `CombatActionType` | Strike, Dodge, Block, TakeDamage, UseTechnique | StatDevelopment, PlayerController |
| `TrainingType` | General, Physical, Sparring, Meditation, BodyHardening | StatDevelopment |

### CultivationGame.Combat

| Enum | Значения | Используется в |
|------|----------|----------------|
| `CombatState` | None, Initiating, Active, Paused, Ending | CombatManager |
| `CombatEventType` | CombatStart, CombatEnd, TurnStart, TurnEnd, AttackStart, AttackHit, AttackMiss, AttackDodged, AttackParried, AttackBlocked, DamageDealt, DamageTaken, QiAbsorbed, QiDepleted, BodyPartHit, BodyPartSevered, Death, TechniqueUsed, TechniqueLearned, CooldownReady | CombatEvents, CombatLog |
| `QiDefenseType` | None, RawQi, Shield | QiBuffer, QiController, ICombatant |
| `DamageSourceType` | QiTechnique, Physical | QiBuffer |
| `TargetType` | None, Self, Ally, Enemy, Neutral, Any | HitDetector |

### CultivationGame.TileSystem

| Enum | Значения | Атрибуты | Используется в |
|------|----------|----------|----------------|
| `TerrainType` | None=0, Grass=1, Dirt=2, Stone=3, Water_Shallow=4, Water_Deep=5, Sand=6, Snow=7, Ice=8, Lava=9, Void=10 | — | TileData, TileMapData, TileMapController, GameTile, TerrainConfig |
| `TileObjectCategory` | None=0, Vegetation=1, Rock=2, Water=3, Building=4, Furniture=5, Interactive=6, Decoration=7 | — | TileObjectData, GameTile |
| `TileObjectType` | None=0, Tree_Oak=100, Tree_Pine=101, Tree_Birch=102, Bush=110, Bush_Berry=111, Grass_Tall=120, Flower=121, Rock_Small=200, Rock_Medium=201, Rock_Large=202, Boulder=210, Pond=300, Well=310, Wall_Wood=400, Wall_Stone=401, Door=410, Window=411, Chest=500, Shrine=510, Altar=511, OreVein=520, Herb=530 | — | TileObjectData, DestructibleSystem |
| `GameTileFlags` | None=0, Passable=1, Swimable=2, Flyable=4, BlocksVision=8, ProvidesCover=16, Interactable=32, Harvestable=64, Dangerous=128 | [System.Flags] | TileData, GameTile |
| `TileDamageType` | Physical, Slashing, Piercing, Blunt, Energy, Fire, Explosive | — | IDestructible, DestructibleObjectController, DamageTypeMultipliers (переименован из DamageType, FIX TIL-H02 2026-04-11) |

### CultivationGame.Formation

| Enum | Значения | Используется в |
|------|----------|----------------|
| `FormationEffectType` | Buff, Debuff, Control, Damage, Healing, Shield | FormationData, FormationEffects |
| `ControlType` | Stun, Slow, Immobilize, Fear, Charm | FormationData, FormationEffects |
| `BuffType` | Attack, Defense, Speed, QiRegen, QiCapacity, Resistance, Special | FormationData, FormationEffects, BuffManager |
| `FormationStage` | Inactive, Charging, Active, Overloaded, Exhausted | FormationData, FormationController |

### CultivationGame.Data.ScriptableObjects

| Enum | Значения | Используется в |
|------|----------|----------------|
| `BuffType` (BuffData.cs) | Attack, Defense, Speed, QiRegen, QiCapacity, Resistance, Special | BuffData, BuffManager |
| `BuffRemovalType` | Timer, CombatEnd, Manual, Death | BuffData |
| `BuffCategory` | Positive, Negative, Neutral | BuffData |
| `StackType` | None, Refresh, AddDuration, AddIntensity | BuffData |
| `PeriodicType` | Damage, Heal, QiDrain, QiRestore, StatChange | BuffData |
| `SpecialEffectType` | Stun, Root, Silence, Blind, Fear, Charm | BuffData |
| `FactionType` (FactionData.cs) | Sect, Clan, Guild, Organization, Empire | FactionData |
| `RequirementType` | CultivationLevel, QuestComplete, ItemOwned, FactionRank, Attribute | FactionData |
| `BenefitType` | QiRegen, Discount, TechniqueDiscount, Access, Stats, Special | FactionData |
| `FormationCoreType` | Basic, Advanced, Legendary | FormationCoreData |
| `FormationCoreVariant` | Standard, Reinforced, Resonant, Void | FormationCoreData |
| `FormationType` (FormationCoreData.cs) | Attack, Defense, Support, Control | FormationCoreData |
| `FormationSize` | Solo, Duo, Trio, Small, Medium, Large | FormationCoreData |
| `QiStoneType` | Basic, Spirit, Heavenly, Void | FormationCoreData |
| `ResourceType` | QiDensity, Material, Herb, Ore, Food, Water | LocationAsset |
| `Alignment` (NPCPresetData.cs) | LawfulGood, NeutralGood, ChaoticGood, LawfulNeutral, TrueNeutral, ChaoticNeutral, LawfulEvil, NeutralEvil, ChaoticEvil | NPCPresetData |
| `BehaviorType` | Aggressive, Defensive, Neutral, Friendly, Cowardly | NPCPresetData |
| `SizeClass` (SpeciesData.cs) | Tiny, Small, Medium, Large, Huge, Colossal | SpeciesData |
| `EffectType` (TechniqueData.cs) | Damage, Heal, Buff, Debuff, Knockback, Pull, Stun | TechniqueData |
| `EffectType` (TechniqueEffectFactory.cs) | Directional, Expanding, FormationArray | TechniqueEffectFactory |

### CultivationGame.World

| Enum | Значения | Используется в |
|------|----------|----------------|
| `FactionType` (FactionController.cs) ⚠️ | Sect, Clan, Guild, Organization, Empire | FactionController (ДУБЛИРОВАН в Data.ScriptableObjects.FactionData!) |
| `FactionRank` (FactionController.cs) ⚠️ | OuterDisciple, InnerDisciple, CoreDisciple, Elder, Patriarch | FactionController (ДУБЛИРОВАН в Data.ScriptableObjects.FactionData!) |
| `Season` | Spring, Summer, Autumn, Winter | TimeController |
| `WorldEventType` | NaturalDisaster, CultivationSurge, FactionWar, QiStorm, BeastTide, ResourceDiscovery | WorldController |
| `EventEffectType` | Damage, QiChange, FactionRelationChange, ItemGain, StatChange | EventController |

### CultivationGame.NPC

| Enum | Значения | Используется в |
|------|----------|----------------|
| `NPCAIState` | Idle, Patrolling, Following, Attacking, Fleeing, Meditating, Dead | NPCData |
| `RelationshipType` | Neutral, Friend, Rival, Ally, Enemy, Mentor, Student, Family | RelationshipController |

### CultivationGame.Interaction

| Enum | Значения | Используется в |
|------|----------|----------------|
| `InteractionType` | Talk, Trade, Attack, Use, Examine | InteractionController |
| `DialogueActionType` | StartQuest, GiveItem, ChangeRelation, Teleport, SetFlag, ModifyStat | DialogueSystem |
| `DialogueConditionType` | HasItem, QuestState, RelationLevel, CultivationLevel, StatCheck, Flag | DialogueSystem |
| `CompareOperator` | Equal, NotEqual, Greater, Less, GreaterOrEqual, LessOrEqual | DialogueSystem |

### CultivationGame.Player

| Enum | Значения | Используется в |
|------|----------|----------------|
| `SleepState` | Awake, FallingAsleep, Sleeping, WakingUp, Interrupted | SleepSystem |

### CultivationGame.Quest

| Enum | Значения | Используется в |
|------|----------|----------------|
| `QuestType` | Main, Side, Daily, Repeatable | QuestData |
| `QuestState` | NotStarted, Active, Completed, Failed, Abandoned | QuestData |
| `QuestObjectiveType` | Kill, Collect, Talk, Reach, Cultivate, Defend | QuestObjective |
| `ObjectiveState` | Inactive, Active, Completed, Failed | QuestObjective |

### CultivationGame.Charger

| Enum | Значения | Используется в |
|------|----------|----------------|
| `ChargerFormFactor` | Handheld, Wearable, Standing, FormationCore | ChargerData |
| `ChargerPurpose` | Combat, Cultivation, Utility, Mixed | ChargerData |
| `ChargerMaterial` | Wood, Bronze, Iron, Jade, SpiritJade, Crystal | ChargerData |
| `ChargerMode` | Passive, Active, Burst | ChargerData |
| `QiStoneQuality` | Fragmented, Cracked, Flawed, Normal, Refined, Perfect, Transcendent | ChargerSlot |
| `QiStoneSize` | Small, Medium, Large | ChargerSlot |
| `HeatState` | Cool, Warm, Hot, Overheating, Critical | ChargerHeat |

### CultivationGame.Generators

| Enum | Значения | Используется в |
|------|----------|----------------|
| `NPCRole` | Warrior, Cultiator, Merchant, Artisan, Leader | NPCGenerator |
| `WeaponSubtype` | Sword, Spear, Axe, Bow, Staff, Dagger | WeaponGenerator |
| `WeaponClass` | OneHanded, TwoHanded | WeaponGenerator |
| `WeaponDamageType` | Slashing, Piercing, Blunt, Elemental | WeaponGenerator |
| `ArmorSubtype` | Light, Medium, Heavy | ArmorGenerator |
| `ArmorWeightClass` | Cloth, Leather, Chain, Plate | ArmorGenerator |
| `ConsumableType` | Pill, Elixir, Food, Scroll, Talisman | ConsumableGenerator |
| `ConsumableEffectCategory` | Healing, QiRestore, Buff, Cure, Damage | ConsumableGenerator |
| `GrammaticalGender` | Masculine, Feminine, Neuter | NameBuilder, NamingDatabase |

### CultivationGame.Combat.Effects

| Enum | Значения | Используется в |
|------|----------|----------------|
| `EffectType` (TechniqueEffectFactory) | Directional, Expanding, FormationArray | TechniqueEffectFactory |

### CultivationGame.UI

| Enum | Значения | Используется в |
|------|----------|----------------|
| `NotificationType` | Info, Warning, Error, Success | HUDController |
| `LogType` (CombatUI) | System, Damage, Heal, Buff, Technique | CombatUI |

---

## 4. DUPLICATE NAMES WARNING ⚠️

### Критические дублирования (могут вызвать CS0104)

| Имя типа | Namespace 1 | Namespace 2 | Описание |
|----------|-------------|-------------|----------|
| **`DamageType`** → РАЗРЕШЕНО | `CultivationGame.Core` (DamageType) | `CultivationGame.TileSystem` (TileDamageType) | Core: Physical, Qi, Elemental, Pure, Void. TileSystem→TileDamageType: Physical, Slashing, Piercing, Blunt, Energy, Fire, Explosive. **FIX TIL-H02 2026-04-11: TileSystem.DamageType→TileDamageType** |
| **`TerrainType`** → РАЗРЕШЕНО | `CultivationGame.Core` (BiomeType) | `CultivationGame.TileSystem` (TerrainType) | Core→BiomeType: Mountains, Plains, Forest, Sea, Desert, Swamp, Tundra, Jungle, Volcanic, Spiritual. TileSystem: None=0, Grass=1, Dirt=2, Stone=3, Water_Shallow=4, Water_Deep=5, Sand=6, Snow=7, Ice=8, Lava=9, Void=10. **FIX CORE-H01 2026-04-11: Core.TerrainType→BiomeType** |
| **`FactionType`** | `CultivationGame.Data.ScriptableObjects` | `CultivationGame.World` | Одинаковые значения: Sect, Clan, Guild, Organization, Empire. FactionData.cs — SO-данные, FactionController.cs — runtime-логика |
| **`FactionRank`** | `CultivationGame.Data.ScriptableObjects` | `CultivationGame.World` | FactionData.FactionRank (inner class) vs FactionController.FactionRank (enum). **Разные типы!** |
| **`FormationType`** | `CultivationGame.Data.ScriptableObjects.FormationCoreData` | `CultivationGame.Formation.FormationData` | FormationCoreData: Attack, Defense, Support, Control. FormationData: (те же). |
| **`BuffType`** | `CultivationGame.Data.ScriptableObjects.BuffData` | `CultivationGame.Formation.FormationData` | Оба: Attack, Defense, Speed, QiRegen, QiCapacity, Resistance, Special |
| **`EffectType`** | `CultivationGame.Data.ScriptableObjects.TechniqueData` | `CultivationGame.Combat.Effects.TechniqueEffectFactory` | TechniqueData: Damage, Heal, Buff, Debuff, Knockback, Pull, Stun. TechniqueEffectFactory: Directional, Expanding, FormationArray |
| **`PlayerSaveData`** | `CultivationGame.Save` | `CultivationGame.Tests.IntegrationTestScenarios` | Save — основная, Tests — тестовая заглушка |
| **`GameSaveData`** | `CultivationGame.Save` | `CultivationGame.Tests.IntegrationTestScenarios` | Аналогично |
| **`FormationStage`** | `CultivationGame.Formation.FormationData` | `CultivationGame.Tests.IntegrationTests` | Основная vs тестовая |
| **`FormationSaveData`** | `CultivationGame.Formation.FormationController` | `CultivationGame.Tests.IntegrationTests` | Основная vs тестовая |

### Рекомендации по разрешению

1. ~~**`DamageType`**~~: ✅ РАЗРЕШЕНО 2026-04-11 — TileSystem.DamageType → `TileDamageType` (FIX TIL-H02)
2. ~~**`TerrainType`**~~: ✅ РАЗРЕШЕНО 2026-04-11 — Core.TerrainType → `BiomeType` (FIX CORE-H01)
3. **`FactionType`/`FactionRank`**: Удалить из World.FactionController, использовать Data.ScriptableObjects
4. **`FormationType`/`BuffType`**: Удалить из FormationData, использовать FormationCoreData
5. **`EffectType`**: TechniqueData.EffectType → `TechniqueEffectType`; TechniqueEffectFactory.EffectType → `VisualEffectType`

---

## 5. Cross-Namespace Dependencies

Ниже для каждого файла указаны `using` на другие пространства имён `CultivationGame.*` (внутренние зависимости).

### Core
| Файл | Зависимости (using CultivationGame.*) |
|------|--------------------------------------|
| Enums.cs | — (нет) |
| VFXPool.cs | — (нет) |
| GameEvents.cs | CultivationGame.Core |
| Camera2DSetup.cs | — (нет) |
| ServiceLocator.cs | — (нет) |
| Constants.cs | — (нет) |
| GameSettings.cs | — (нет) |
| StatDevelopment.cs | CultivationGame.Core |

### Body
| Файл | Зависимости |
|------|-------------|
| BodyPart.cs | CultivationGame.Core |
| BodyController.cs | CultivationGame.Core |
| BodyDamage.cs | CultivationGame.Core |

### Buff
| Файл | Зависимости |
|------|-------------|
| BuffManager.cs | CultivationGame.Core, CultivationGame.Data.ScriptableObjects, CultivationGame.Formation, CultivationGame.Combat |

### Character
| Файл | Зависимости |
|------|-------------|
| CharacterSpriteController.cs | — |
| IndependentScale.cs | — |

### Charger
| Файл | Зависимости |
|------|-------------|
| ChargerSlot.cs | CultivationGame.Core |
| ChargerData.cs | CultivationGame.Core |
| ChargerController.cs | CultivationGame.Core, CultivationGame.Qi |
| ChargerBuffer.cs | CultivationGame.Core |
| ChargerHeat.cs | — |

### Combat
| Файл | Зависимости |
|------|-------------|
| CombatManager.cs | CultivationGame.Core |
| Combatant.cs | CultivationGame.Core |
| DamageCalculator.cs | CultivationGame.Core |
| CombatEvents.cs | CultivationGame.Core |
| DefenseProcessor.cs | CultivationGame.Core |
| HitDetector.cs | CultivationGame.Core |
| LevelSuppression.cs | CultivationGame.Core |
| QiBuffer.cs | CultivationGame.Core |
| TechniqueCapacity.cs | CultivationGame.Core |
| TechniqueController.cs | CultivationGame.Core, CultivationGame.Qi, CultivationGame.Combat |

### Combat/Effects
| Файл | Зависимости |
|------|-------------|
| TechniqueEffect.cs | CultivationGame.Core |
| TechniqueEffectFactory.cs | CultivationGame.Core |
| ExpandingEffect.cs | CultivationGame.Core, CultivationGame.Combat.OrbitalSystem |
| DirectionalEffect.cs | CultivationGame.Core, CultivationGame.Combat.OrbitalSystem |
| FormationArrayEffect.cs | CultivationGame.Core, CultivationGame.Buff, CultivationGame.Combat.OrbitalSystem, CultivationGame.Formation |

### Combat/OrbitalSystem
| Файл | Зависимости |
|------|-------------|
| OrbitalWeapon.cs | CultivationGame.Core |
| OrbitalWeaponController.cs | — |

### Data/ScriptableObjects
| Файл | Зависимости |
|------|-------------|
| Все SO | CultivationGame.Core (+ System.Collections.Generic для некоторых) |

### Data
| Файл | Зависимости |
|------|-------------|
| TerrainConfig.cs | — (CultivationGame.TileSystem, нет внешних) |

### Editor
| Файл | Зависимости |
|------|-------------|
| AssetGenerator.cs | CultivationGame.Core, CultivationGame.Data.ScriptableObjects |
| AssetGeneratorExtended.cs | CultivationGame.Core, CultivationGame.Data.ScriptableObjects |
| FormationAssetGenerator.cs | CultivationGame.Core, CultivationGame.Formation, CultivationGame.Data.ScriptableObjects |
| FormationUIPrefabsGenerator.cs | — |
| SceneSetupTools.cs | CultivationGame.Core, CultivationGame.Managers, CultivationGame.World, CultivationGame.Player, CultivationGame.Qi, CultivationGame.Body, CultivationGame.Inventory, CultivationGame.Combat, CultivationGame.Save, CultivationGame.Interaction |

### Examples
| Файл | Зависимости |
|------|-------------|
| NPCAssemblyExample.cs | CultivationGame.Core, CultivationGame.Generators |

### Formation
| Файл | Зависимости |
|------|-------------|
| FormationCore.cs | CultivationGame.Core, CultivationGame.Data.ScriptableObjects |
| FormationQiPool.cs | CultivationGame.Data.ScriptableObjects |
| FormationController.cs | CultivationGame.Core, CultivationGame.Data.ScriptableObjects, CultivationGame.Qi, CultivationGame.Charger |
| FormationUI.cs | CultivationGame.Core, CultivationGame.Data.ScriptableObjects |
| FormationEffects.cs | CultivationGame.Core, CultivationGame.Combat, CultivationGame.Buff, CultivationGame.Combat.OrbitalSystem |
| FormationData.cs | CultivationGame.Core, CultivationGame.Data.ScriptableObjects |

### Generators
| Файл | Зависимости |
|------|-------------|
| NPCGenerator.cs | CultivationGame.Core, CultivationGame.Data.ScriptableObjects |
| WeaponGenerator.cs | CultivationGame.Core |
| ArmorGenerator.cs | CultivationGame.Core |
| TechniqueGenerator.cs | CultivationGame.Core, CultivationGame.Data.ScriptableObjects |
| ConsumableGenerator.cs | CultivationGame.Core |
| GeneratorRegistry.cs | CultivationGame.Core |
| SeededRandom.cs | — |
| NameBuilder.cs | CultivationGame.Core |
| NamingDatabase.cs | CultivationGame.Core |
| AdjectiveForms.cs | — |
| NounWithGender.cs | — |
| GrammaticalGender.cs | — |

### Interaction
| Файл | Зависимости |
|------|-------------|
| DialogueSystem.cs | CultivationGame.Core, CultivationGame.NPC |
| InteractionController.cs | CultivationGame.Core, CultivationGame.NPC, CultivationGame.Combat, CultivationGame.Qi, CultivationGame.Body |

### Inventory
| Файл | Зависимости |
|------|-------------|
| InventoryController.cs | CultivationGame.Core |
| EquipmentController.cs | CultivationGame.Core, CultivationGame.Data.ScriptableObjects, CultivationGame.Save, CultivationGame.Qi, CultivationGame.Player |
| CraftingController.cs | CultivationGame.Core, CultivationGame.Save |
| MaterialSystem.cs | CultivationGame.Core |

### Managers
| Файл | Зависимости |
|------|-------------|
| GameInitializer.cs | CultivationGame.Core, CultivationGame.Player, CultivationGame.Qi, CultivationGame.Body, CultivationGame.World, CultivationGame.Save |
| GameManager.cs | CultivationGame.Core, CultivationGame.World, CultivationGame.Player, CultivationGame.UI |
| SceneLoader.cs | CultivationGame.Core |

### NPC
| Файл | Зависимости |
|------|-------------|
| NPCController.cs | CultivationGame.Core, CultivationGame.Combat, CultivationGame.Qi, CultivationGame.Body, CultivationGame.World |
| NPCData.cs | CultivationGame.Core, CultivationGame.Combat, CultivationGame.Qi |
| RelationshipController.cs | CultivationGame.Core, CultivationGame.World |
| NPCAI.cs | CultivationGame.Core, CultivationGame.Qi |

### Player
| Файл | Зависимости |
|------|-------------|
| PlayerController.cs | CultivationGame.Core, CultivationGame.Combat, CultivationGame.Qi, CultivationGame.Body, CultivationGame.NPC, CultivationGame.Interaction, CultivationGame.World, CultivationGame.Save |
| SleepSystem.cs | CultivationGame.Core, CultivationGame.Qi, CultivationGame.Body |
| PlayerVisual.cs | — |

### Qi
| Файл | Зависимости |
|------|-------------|
| QiController.cs | CultivationGame.Core, CultivationGame.Combat |

### Quest
| Файл | Зависимости |
|------|-------------|
| QuestController.cs | CultivationGame.Core, CultivationGame.World |
| QuestData.cs | CultivationGame.Core |
| QuestObjective.cs | CultivationGame.Core |

### Save
| Файл | Зависимости |
|------|-------------|
| SaveFileHandler.cs | — |
| SaveDataTypes.cs | — |
| SaveManager.cs | CultivationGame.Core, CultivationGame.NPC, CultivationGame.World, CultivationGame.Combat, CultivationGame.Formation, CultivationGame.Buff, CultivationGame.TileSystem, CultivationGame.Charger, CultivationGame.Quest, CultivationGame.Player |

### Tests
| Файл | Зависимости |
|------|-------------|
| CombatTests.cs | CultivationGame.Core, CultivationGame.Combat |
| BalanceVerification.cs | CultivationGame.Core, CultivationGame.Combat |
| IntegrationTestScenarios.cs | CultivationGame.Core, CultivationGame.Combat, CultivationGame.Generators, CultivationGame.Inventory |
| IntegrationTests.cs | CultivationGame.Core, CultivationGame.Combat, CultivationGame.Qi, CultivationGame.Buff, CultivationGame.Formation |

### Tile
| Файл | Зависимости |
|------|-------------|
| TileEnums.cs | — |
| GameTile.cs | — |
| TileData.cs | — |
| TileMapData.cs | — |
| TileMapController.cs | — |
| DestructibleObjectController.cs | CultivationGame.Core |
| DestructibleSystem.cs | — |
| ResourcePickup.cs | CultivationGame.Core, CultivationGame.Inventory |
| TerrainConfig.cs | — |

### Tile/Editor
| Файл | Зависимости |
|------|-------------|
| TestLocationSetup.cs | — |
| TileSpriteGenerator.cs | — |

### UI
| Файл | Зависимости |
|------|-------------|
| UIManager.cs | CultivationGame.Core |
| HUDController.cs | CultivationGame.Core, CultivationGame.World |
| CombatUI.cs | CultivationGame.Core |
| InventoryUI.cs | CultivationGame.Core, CultivationGame.Inventory |
| CharacterPanelUI.cs | CultivationGame.Core, CultivationGame.Body, CultivationGame.Player, CultivationGame.Inventory, CultivationGame.Qi |
| DialogUI.cs | CultivationGame.Core, CultivationGame.NPC, CultivationGame.Interaction |
| MenuUI.cs | CultivationGame.Core, CultivationGame.Save |
| CultivationProgressBar.cs | CultivationGame.Core, CultivationGame.Qi |
| WeaponDirectionIndicator.cs | CultivationGame.Core, CultivationGame.Combat.OrbitalSystem |

### World
| Файл | Зависимости |
|------|-------------|
| TimeController.cs | CultivationGame.Core |
| WorldController.cs | CultivationGame.Core, CultivationGame.NPC, CultivationGame.Generators |
| FactionController.cs | CultivationGame.Core |
| LocationController.cs | CultivationGame.Core |
| EventController.cs | CultivationGame.Core |
| TestLocationGameController.cs | CultivationGame.TileSystem, CultivationGame.Player, CultivationGame.Core, CultivationGame.Qi, CultivationGame.Body |

---

### Граф связности (сильно связанные модули)

```
Core ←── ALL (фундамент, используется везде)
Combat ←── Qi, Body, Core, Data.SO
Player ←── Combat, Qi, Body, NPC, Interaction, World, Save, Core
NPC ←── Combat, Qi, Body, World, Core
Formation ←── Core, Data.SO, Qi, Charger, Combat, Buff, Combat.OrbitalSystem
Save ←── Core, NPC, World, Combat, Formation, Buff, TileSystem, Charger, Quest, Player
UI ←── Core, World, Inventory, Body, Player, Qi, NPC, Interaction, Combat.OrbitalSystem, Save
Inventory ←── Core, Data.SO, Save, Qi, Player
Interaction ←── Core, NPC, Combat, Qi, Body
Editor ←── Core, Data.SO, Formation, Managers, World, Player, Qi, Body, Inventory, Combat, Save, Interaction
```

---

*Конец документа CODE_REFERENCE.md*  
*Генерировано: 2026-04-11 08:00:00 UTC*

# Audit: Core + Combat Systems
**Date:** 2026-04-10
**Files analyzed:** 25 C# files across Core (8) and Combat (17) directories

---

## Critical Issues

### C-NEW-01: BodyMaterialReduction / BodyMaterialHardness missing `Construct` key — KeyNotFoundException at runtime
**File:** `Core/Constants.cs:381-403`  
**Code:**
```csharp
public static readonly Dictionary<BodyMaterial, float> BodyMaterialReduction = new Dictionary<BodyMaterial, float>
{
    { BodyMaterial.Organic, 0.0f },
    { BodyMaterial.Scaled, 0.3f },
    { BodyMaterial.Chitin, 0.2f },
    { BodyMaterial.Mineral, 0.5f },
    { BodyMaterial.Ethereal, 0.7f },
    { BodyMaterial.Chaos, 0.4f }
    // MISSING: BodyMaterial.Construct
};
```
**Explanation:** The `BodyMaterial` enum includes `Construct` (line 269 in Enums.cs), but both `BodyMaterialReduction` and `BodyMaterialHardness` dictionaries omit it. When any entity with `BodyMaterial.Construct` takes damage, `DefenseProcessor.ProcessDefense()` calls `GameConstants.BodyMaterialReduction.TryGetValue(defense.BodyMaterial, out float materialReduction)` — which returns `false`, and no reduction is applied. This silently fails instead of throwing, but the `CombatantBase.BodyMaterial` default is `Organic`, so Construct entities would need explicit assignment. If a future system sets `BodyMaterial = Construct`, the TryGetValue silently skips material reduction. For `BodyMaterialHardness`, there is no `TryGetValue` usage — any direct access would throw `KeyNotFoundException`.  
**Fix:** Add `Construct` entries to both dictionaries:
```csharp
{ BodyMaterial.Construct, 0.4f }  // in BodyMaterialReduction
{ BodyMaterial.Construct, 6 }     // in BodyMaterialHardness
```

---

### C-NEW-02: `StatDevelopment.ConsolidateStat` — `availableConsolidation` computed but never used; consolidation cannot produce any stat increase when `maxConsolidation < threshold` (all stats ≥ 10)
**File:** `Core/StatDevelopment.cs:415`  
**Code:**
```csharp
float availableConsolidation = Mathf.Min(delta, maxConsolidation); // line 415 — NEVER USED

// Line 418-422: The real gating logic
if (delta >= threshold && threshold > 0f)
{
    int levelsPossible = Mathf.FloorToInt(delta / threshold);
    levelsPossible = Mathf.Min(levelsPossible, Mathf.FloorToInt(maxConsolidation / threshold));
    // For stat=10, threshold=1.0, maxConsolidation=0.20:
    // levelsPossible = Min(N, FloorToInt(0.20/1.0)) = Min(N, 0) = 0
    // → NO stat increase ever occurs
```
**Explanation:** `maxConsolidation` caps at `MAX_CONSOLIDATION_PER_SLEEP = 0.20f`. For any stat ≥ 10, the threshold is ≥ 1.0. `FloorToInt(0.20 / 1.0) = 0`, so `levelsPossible` is always 0 and no stat increase can ever occur from sleep. The variable `availableConsolidation` on line 415 was apparently meant to handle partial consolidation (gradual delta consumption without a full level gain), but it is dead code. This means the entire sleep consolidation system is non-functional for stats ≥ 10. Note: C-08 covers "stats below 10 can't grow" (threshold=0, `threshold > 0f` guard), but this extends to ALL stats.  
**Fix:** Implement partial consolidation: consume `availableConsolidation` delta without requiring a full level gain, or increase `MAX_CONSOLIDATION_PER_SLEEP` to be ≥ 1.0.

---

### C-NEW-03: `DamageCalculator.CalculateElementMultiplier` ignores defender's element — always returns 1.0 for non-Void/Neutral
**File:** `Combat/DamageCalculator.cs:307-314`  
**Code:**
```csharp
private static float CalculateElementMultiplier(Element element)
{
    if (element == Element.Void)
        return GameConstants.VOID_ELEMENT_MULTIPLIER;
    if (element == Element.Neutral)
        return 1.0f;
    return 1.0f; // Fire, Water, Earth, Air, Lightning, Poison all return 1.0!
}
```
**Explanation:** The method `CalculateElementMultiplier` takes only the attacker's element and always returns 1.0 for non-Void/Neutral elements. It completely ignores the defender's element, so elemental interactions (opposite elements ×1.5, affinity ×0.8) are never applied in the actual damage pipeline. The correct method `CalculateElementalInteraction(attacker, defender)` exists on line 325 but is never called from `CalculateDamage`. This is related to C-11 but goes further — not only are elemental interactions ignored, the method that IS called hardcodes 1.0 for most elements.  
**Fix:** Replace the `CalculateElementMultiplier` call on line 171 with `CalculateElementalInteraction(attacker.AttackElement, defenderElement)`. This requires adding a `defenderElement` field to `DefenderParams`.

---

### C-NEW-04: `DamageCalculator` sets `AttackType` to `Technique` for ALL non-Ultimate attacks, including basic attacks
**File:** `Combat/DamageCalculator.cs:128`  
**Code:**
```csharp
AttackType = attacker.IsUltimate ? AttackType.Ultimate : AttackType.Technique,
```
**Explanation:** When `IsUltimate` is false, AttackType is always set to `Technique`. There is no path for `AttackType.Normal`. This means level suppression always uses the "Technique" column of the suppression table, making basic attacks significantly stronger than intended (Technique column has higher multipliers: e.g., level diff 2 → 0.25 vs 0.1 for Normal). The `AttackerParams` struct has no field to indicate a basic attack vs technique attack separately from `IsUltimate`. This is related to H-14 but is the root cause.  
**Fix:** Add a field `AttackType AttackType` to `AttackerParams`, or derive it from whether a technique is being used. For `ExecuteBasicAttack`, set it to `AttackType.Normal`.

---

### C-NEW-05: `DefenderParams.CurrentQi` is `int` but `ICombatant.CurrentQi` is `long` — silent truncation in `CombatantBase.GetDefenderParams()`
**File:** `Combat/Combatant.cs:275`  
**Code:**
```csharp
CurrentQi = (int)Math.Min(CurrentQi, int.MaxValue),
```
**Explanation:** `ICombatant.CurrentQi` returns `long` (line 36), but `DefenderParams.CurrentQi` is `int` (line 71). The cast silently truncates values above `int.MaxValue` (~2.1 billion). At high cultivation levels (L8+), Qi can exceed this. This is C-05 but the truncation in `GetDefenderParams` is the specific runtime manifestation — any Qi above 2.1B is silently lost in the defense calculation, making the Qi Buffer calculation incorrect for high-level characters.  
**Fix:** Change `DefenderParams.CurrentQi` from `int` to `long`, and update `QiBuffer` methods to accept `long`.

---

### C-NEW-06: `CombatantBase.OnDestroy` does not unsubscribe from `qiController.OnQiChanged` — lambda leak
**File:** `Combat/Combatant.cs:195,199-205`  
**Code:**
```csharp
// In CacheComponents():
qiController.OnQiChanged += (current, max) => OnQiChanged?.Invoke(current, max); // line 195

// In OnDestroy():
if (bodyController != null)
{
    bodyController.OnDeath -= HandleDeath; // Unsubscribed correctly
}
// qiController.OnQiChanged is NEVER unsubscribed!
```
**Explanation:** The lambda subscription on line 195 captures `this` (the CombatantBase instance) and can never be unsubscribed because the lambda is not stored. This is M-09 (already known) but the specific consequence: if the `QiController` outlives the `CombatantBase` (e.g., component removal order), the lambda will invoke `OnQiChanged` on a destroyed MonoBehaviour, causing a `MissingReferenceException`. Additionally, the lambda prevents GC of the CombatantBase.  
**Fix:** Store the lambda in a field and unsubscribe in `OnDestroy`:
```csharp
private Action<long, long> _qiChangedHandler;
// In CacheComponents: _qiChangedHandler = (c, m) => OnQiChanged?.Invoke(c, m);
// qiController.OnQiChanged += _qiChangedHandler;
// In OnDestroy: qiController.OnQiChanged -= _qiChangedHandler;
```

---

## High Issues

### H-NEW-01: `OppositeElements` dictionary missing entries for Lightning, Void, Poison, Neutral — KeyNotFoundException
**File:** `Core/Constants.cs:591-597`  
**Code:**
```csharp
public static readonly Dictionary<Element, Element> OppositeElements = new Dictionary<Element, Element>
{
    { Element.Fire, Element.Water },
    { Element.Water, Element.Fire },
    { Element.Earth, Element.Air },
    { Element.Air, Element.Earth }
    // Missing: Lightning, Void, Poison, Neutral
};
```
**Explanation:** `DamageCalculator.CalculateElementalInteraction` (line 328) calls `GameConstants.OppositeElements.TryGetValue(attacker, ...)`. For Lightning/Void/Poison/Neutral, this returns `false` and no opposite element is found. While this doesn't crash (TryGetValue returns false), it means these elements can NEVER benefit from the ×1.5 opposite element bonus, and the design intent for their interactions is unclear. If any code uses direct dictionary access (`OppositeElements[element]`) instead of TryGetValue, it would throw `KeyNotFoundException`.  
**Fix:** Add entries for remaining elements, or document that only 4 elements have opposites.

---

### H-NEW-02: `GameConstants.MAX_STAT_VALUE` = 100 (int) conflicts with `StatDevelopment.MAX_STAT_VALUE` = 1000.0f
**File:** `Core/Constants.cs:48` vs `Core/StatDevelopment.cs:119`  
**Explanation:** Two different constants define the max stat value with different values. `GameConstants.MAX_STAT_VALUE = 100` and `StatDevelopment.MAX_STAT_VALUE = 1000.0f`. Code using `GameConstants.MAX_STAT_VALUE` will cap stats at 100, while `StatDevelopment` allows up to 1000. This creates inconsistent behavior depending on which constant is referenced.  
**Fix:** Unify to a single source of truth. Update `GameConstants.MAX_STAT_VALUE` to 1000 or remove the duplicate.

---

### H-NEW-03: `CombatantBase.GetDefenderParams` passes `ParryChance` as `ParryBonus` and `BlockChance` as `BlockBonus` — double calculation
**File:** `Combat/Combatant.cs:283-284`  
**Code:**
```csharp
ParryBonus = ParryChance,   // Already calculated: weaponParry + (AGI-10)×0.3%
BlockBonus = BlockChance,   // Already calculated: shieldBlock + (STR-10)×0.2%
```
**Explanation:** `ParryChance` and `BlockChance` are already full calculated chances (from `DefenseProcessor.CalculateParryChance/CalculateBlockChance`). But `DamageCalculator` passes them as `ParryBonus`/`BlockBonus` to `DefenseProcessor.CalculateParryChance/CalculateBlockChance` again, which adds `(AGI-10)*0.003` / `(STR-10)*0.002` a second time. A character with AGI=20 and weapon bonus 5% would get: first calculation = 5% + 3% = 8%, then second calculation = 8% + 3% = 11% — almost double the intended value. This is C-09 (already known) but documenting the specific double-calculation path.  
**Fix:** Either: (a) Store raw bonuses and recalculate, or (b) Pass pre-calculated chances directly to the roll check without re-calculating.

---

### H-NEW-04: `TechniqueEffectFactory.DetermineEffectType` returns wrong type for DirectionalEffect with Poison element
**File:** `Combat/Effects/TechniqueEffectFactory.cs:254-263`  
**Code:**
```csharp
if (effect is DirectionalEffect)
{
    return effect.Element switch
    {
        Element.Fire => EffectType.FireSlash,
        Element.Water => EffectType.WaterWave,
        Element.Air => EffectType.AirBlade,
        Element.Lightning => EffectType.LightningBolt,
        Element.Earth => EffectType.EarthSpike,
        Element.Void => EffectType.VoidRift,
        _ => EffectType.FireSlash  // Poison falls through to FireSlash!
    };
}
```
**Explanation:** When a `DirectionalEffect` has `Element.Poison`, the switch falls through to the default case and returns `EffectType.FireSlash`. This means returning a Poison directional effect to the pool will put it in the wrong pool slot, and the next time `FireSlash` is requested, a Poison-typed effect will be returned. Pool contamination.  
**Fix:** Add `Element.Poison => EffectType.PoisonCloud` (or create a dedicated PoisonSlash type) and handle the default case properly.

---

### H-NEW-05: `FormationArrayEffect` uses `ICombatTarget` directly but `ExpandingEffect` references it via `OrbitalSystem.ICombatTarget`
**File:** `Combat/Effects/FormationArrayEffect.cs:112` vs `Combat/Effects/ExpandingEffect.cs:126`  
**Code:**
```csharp
// FormationArrayEffect.cs line 112:
var target = hit.GetComponent<ICombatTarget>(); // Uses CultivationWorld.Combat.OrbitalSystem.ICombatTarget

// ExpandingEffect.cs line 126:
var combatTarget = target.GetComponent<OrbitalSystem.ICombatTarget>(); // Explicitly qualified
```
**Explanation:** `FormationArrayEffect` uses `ICombatTarget` without namespace qualification. Since the file imports `CultivationWorld.Combat.OrbitalSystem`, it resolves correctly. However, the two effects files use different qualification styles, and if a different `ICombatTarget` interface is ever introduced in a closer namespace, `FormationArrayEffect` could silently resolve to the wrong one. Inconsistent pattern.  
**Fix:** Use consistent namespace qualification across all effect files.

---

### H-NEW-06: `VFXPool` global max size applies across ALL prefab types — single prefab can monopolize pool
**File:** `Core/VFXPool.cs:113`  
**Code:**
```csharp
if (instanceToPrefab.Count < maxPoolSize) // maxPoolSize = 50 across ALL prefabs
```
**Explanation:** H-03 is already known, but to clarify: `instanceToPrefab.Count` tracks total objects across ALL prefab types. With 50 max and 10 different VFX types, each type gets only ~5 instances on average. One heavy-use VFX type can exhaust the pool for all others. Additionally, `prewarmOnStart = true` is declared but never implemented — no prewarming logic exists.  
**Fix:** Implement per-prefab pool sizes, or at minimum implement prewarming as declared.

---

### H-NEW-07: `DefenseProcessor.RollChance` uses `System.Random` (non-deterministic) while `DamageCalculator.RollChance` uses `UnityEngine.Random`
**File:** `Combat/DefenseProcessor.cs:77,271` vs `Combat/DamageCalculator.cs:343`  
**Code:**
```csharp
// DefenseProcessor.cs:
private static readonly Random random = new Random(); // System.Random
private static bool RollChance(float chance) => random.NextDouble() < chance;

// DamageCalculator.cs:
private static bool RollChance(float chance) => UnityEngine.Random.value < chance;
```
**Explanation:** C-02 is already known about System.Random usage. However, the additional issue is the inconsistency: `DefenseProcessor` uses `System.Random` while `DamageCalculator` uses `UnityEngine.Random`. This means defense rolls and other combat rolls use different RNG sources, making reproduction of bugs extremely difficult and causing desyncs in multiplayer/replay scenarios.  
**Fix:** Unify all RNG to a single source (preferably `UnityEngine.Random` or a custom deterministic RNG).

---

### H-NEW-08: `AttackResult` type name collision between `CombatManager` and `Core/Enums`
**File:** `Combat/CombatManager.cs:52` vs `Core/Enums.cs:576`  
**Code:**
```csharp
// CombatManager.cs line 52:
public struct AttackResult { ... }

// Enums.cs line 576:
public enum AttackResult { Miss, Dodge, Parry, Block, Hit, CriticalHit, Kill }
```
**Explanation:** Both a struct `AttackResult` (in `CultivationGame.Combat`) and an enum `AttackResult` (in `CultivationGame.Core`) exist. Since `CultivationGame.Combat` uses `using CultivationGame.Core`, any file that imports both namespaces will have an ambiguity. The struct is used in `CombatManager`, but the enum in `Enums.cs` is never used anywhere in the audited code. This will cause compilation errors in files that reference both.  
**Fix:** Rename the enum to `AttackOutcome` or remove it if unused, or rename the struct.

---

## Medium Issues

### M-NEW-01: `VFXPool.Awake` doesn't call `DontDestroyOnLoad` — pool destroyed on scene load
**File:** `Core/VFXPool.cs:64-72`  
**Explanation:** The singleton `Awake` method doesn't call `DontDestroyOnLoad`, so the VFXPool will be destroyed when a new scene loads. The static `Spawn` method (line 247) DOES add `DontDestroyOnLoad`, creating an inconsistency: if the pool is created via the scene, it dies on scene transition; if created via `Spawn`, it persists.  
**Fix:** Add `DontDestroyOnLoad(gameObject)` in `Awake` if the pool should persist.

---

### M-NEW-02: `VFXPool.prewarmOnStart` declared but never implemented
**File:** `Core/VFXPool.cs:38`  
**Explanation:** `[SerializeField] private bool prewarmOnStart = true;` is declared but there's no `Start()` method or any code that reads this field. Prewarming would pre-create objects to avoid runtime allocation spikes.  
**Fix:** Implement prewarming in `Start()` or remove the field.

---

### M-NEW-03: `DirectionalEffect` creates hit particles and sound via `Instantiate`/`Destroy` — not pooled
**File:** `Combat/Effects/DirectionalEffect.cs:193-194`  
**Code:**
```csharp
var particles = Instantiate(hitParticles, position, Quaternion.identity);
particles.Play();
Destroy(particles.gameObject, particles.main.duration);
```
**Explanation:** While the main effect objects use `TechniqueEffectFactory` pooling, the hit particle effects are created/destroyed via `Instantiate`/`Destroy`, which is the exact problem the VFXPool was designed to solve. Same issue in `OrbitalWeapon.cs:218-220`.  
**Fix:** Use `VFXPool.Spawn` or `TechniqueEffectFactory` for hit particles.

---

### M-NEW-04: `Element.Count` enum member can cause issues in iterations
**File:** `Core/Enums.cs:103`  
**Code:**
```csharp
public enum Element
{
    Neutral, Fire, Water, Earth, Air, Lightning, Void, Poison,
    Count  // This is a value that can be iterated over
}
```
**Explanation:** `Count` is a common C++ pattern but problematic in C#. `Enum.GetValues(typeof(Element))` includes `Count`. Any code iterating over all elements will process `Count` as an element, leading to bugs (e.g., looking up `Count` in `OppositeElements` dictionary, using it as an attack element).  
**Fix:** Remove `Count` from the enum. Use `Enum.GetNames(typeof(Element)).Length` or a constant instead.

---

### M-NEW-05: `Disposition` enum conflates relationship state and personality traits
**File:** `Core/Enums.cs:450-461`  
**Code:**
```csharp
public enum Disposition
{
    Hostile, Unfriendly, Neutral, Friendly, Allied,  // Relationship states
    Aggressive, Cautious, Treacherous, Ambitious     // Personality traits
}
```
**Explanation:** Mixing relationship disposition (Hostile→Allied) and personality traits (Aggressive, Cautious) in one enum means a character can't be both "Friendly" AND "Cautious" — they're mutually exclusive. This is a design/architecture issue.  
**Fix:** Split into `RelationshipDisposition` and `PersonalityTrait` enums.

---

### M-NEW-06: `CombatLog` static constructor subscribes to `CombatEvents.OnCombatEvent` — never unsubscribed
**File:** `Combat/CombatEvents.cs:274-277`  
**Code:**
```csharp
static CombatLog()
{
    CombatEvents.OnCombatEvent += AddEntry;
}
```
**Explanation:** C-10 is already known. The additional note: since `CombatLog` has no way to unsubscribe, and `CombatEvents` is static, the `AddEntry` handler persists for the entire application lifetime. The `entries` list grows unbounded (capped at 100, but removal creates GC pressure from `RemoveAt(0)` which is O(n)).  
**Fix:** Use a `Queue<CombatEventData>` instead of `List` for O(1) dequeue, or implement `Clear` properly with scene transitions.

---

### M-NEW-07: `CombatManager.totalDamageDealt == totalDamageTaken` — both track the same value
**File:** `Combat/CombatManager.cs:269-270`  
**Code:**
```csharp
totalDamageDealt += (int)damageResult.FinalDamage;
totalDamageTaken += (int)damageResult.FinalDamage;
```
**Explanation:** H-15 is already known. Both counters are incremented by the same amount for every hit. In a 1v1 fight, "dealt" should track the player's damage output and "taken" should track damage received. Currently both track damage dealt TO the defender.  
**Fix:** Differentiate: `totalDamageDealt` increments when the player attacks; `totalDamageTaken` increments when the player is attacked.

---

### M-NEW-08: `TechniqueController.IncreaseMastery` ignores the documented formula
**File:** `Combat/TechniqueController.cs:344-355`  
**Code:**
```csharp
// Documented formula (line 52): masteryGained = max(0.1, baseGain × (1 - currentMastery / 100))
// Actual implementation:
technique.Mastery = Mathf.Min(100f, technique.Mastery + amount); // Just adds flat amount
```
**Explanation:** H-12 is already known. The documented formula includes diminishing returns (`1 - currentMastery / 100`) and a minimum gain of 0.1, but the implementation simply adds the flat `amount` parameter (0.01 per use). At 99% mastery, the formula should give `max(0.1, 0.01 × 0.01) = 0.1`, but the code gives `0.01`.  
**Fix:** Implement the documented formula.

---

### M-NEW-09: `TechniqueController` cooldown converts `cooldown * 60f` — unclear unit
**File:** `Combat/TechniqueController.cs:262`  
**Code:**
```csharp
technique.CooldownRemaining = technique.Data.cooldown * 60f;
```
**Explanation:** H-13 is already known. The `cooldown` field is multiplied by 60, suggesting `cooldown` is in minutes and `CooldownRemaining` in seconds. But there's no documentation or validation. If `cooldown` is already in seconds, this creates a 60× multiplier bug. The `ProcessCooldowns` method decrements by `Time.deltaTime` (seconds), so `CooldownRemaining` must be in seconds.  
**Fix:** Add documentation specifying the unit of `TechniqueData.cooldown`, or use a named constant `SECONDS_PER_MINUTE = 60`.

---

### M-NEW-10: `DefenderParams` missing `defenderElement` — elemental defense is impossible
**File:** `Combat/DamageCalculator.cs:68-85`  
**Explanation:** `DefenderParams` has no element field. The elemental interaction system (`CalculateElementalInteraction`) requires both attacker and defender elements, but the defender's element is never passed. This means fire attacks against water-aligned defenders (or vice versa) can never get the ×1.5 bonus, making elemental strategy one-sided. This compounds C-NEW-03.  
**Fix:** Add `Element DefenderElement` to `DefenderParams`.

---

### M-NEW-11: `HitDetector.GetComponent<ICombatant>()` — interface GetComponent is expensive and unreliable in Unity
**File:** `Combat/HitDetector.cs:109`  
**Code:**
```csharp
ICombatant target = collider.GetComponent<ICombatant>();
```
**Explanation:** `GetComponent<T>()` with an interface type is slower than with a concrete type in Unity, and may not work with some Unity versions (though Unity 6000.3 supports it). More importantly, this is called in a `Physics.OverlapSphere` loop which could hit many colliders per frame. Should use a cached component lookup or a manager-based approach.  
**Fix:** Use a combatant registry or add a concrete `CombatantBase` component reference.

---

### M-NEW-12: `AwakeningChance` values may be percentages treated as probabilities
**File:** `Core/Constants.cs:105-114`  
**Code:**
```csharp
public static readonly Dictionary<MortalStage, float> AwakeningChance = new Dictionary<MortalStage, float>
{
    { MortalStage.Child, 0.001f },    // 0.001% per comment
    { MortalStage.Mature, 1f },       // 1% per comment
    { MortalStage.Awakening, 5f }     // 5% per comment
};
```
**Explanation:** The comments say these are percentages (0.001%, 1%, 5%), but the values (0.001f, 1f, 5f) don't match if used as direct probabilities (0-1 range). If `0.001f` is meant as 0.1% probability, the comment saying "0.001%" is wrong. If `1f` is meant as 100% probability, the comment saying "1%" is wrong. If these are percentages and need to be divided by 100 before use as probabilities, the consuming code must do that division — but there's no indication of this.  
**Fix:** Clarify in documentation, rename to `AwakeningChancePercent`, or convert to probability range (0-1).

---

### M-NEW-13: `TechniqueEffectFactory.CreateEffect` double-dequeues from pool on creation
**File:** `Combat/Effects/TechniqueEffectFactory.cs:147-151`  
**Code:**
```csharp
else if (pool.activeCount < pool.maxPoolSize)
{
    obj = CreatePooledObject(pool);    // Creates AND enqueues
    pool.pool.Dequeue();               // Then dequeues — but what if pool was empty?
}
```
**Explanation:** `CreatePooledObject` creates a new object AND enqueues it to the pool. Then `Dequeue()` removes it. If the pool was empty (which is the case here — we're in the `else` branch because `pool.pool.Count == 0`), this works correctly but is confusing. However, if `CreatePooledObject` returns null (when `pool.prefab == null`), then `Dequeue()` would still try to dequeue from a potentially empty queue.  
**Fix:** Refactor to create the object without enqueueing, or add a null check before dequeue.

---

### M-NEW-14: `OrbitalWeaponController.AttackCoroutine` doesn't disable normal orbit update during attack
**File:** `Combat/OrbitalSystem/OrbitalWeaponController.cs:98-121,152-184`  
**Explanation:** During the attack coroutine, `UpdateWeaponPositions()` still runs every frame and modifies `_currentAngle`. The coroutine also modifies `_currentAngle` via `Mathf.LerpAngle`. This creates a race condition where both the Update loop and the coroutine write to `_currentAngle` simultaneously. The attack animation would be jittery as both systems fight over the angle.  
**Fix:** Skip normal orbit update when `_isAttacking` is true, or use a separate attack angle variable.

---

## Low Issues

### L-NEW-01: `StatDevelopment` uses `[SerializeField]` without inheriting from `MonoBehaviour`
**File:** `Core/StatDevelopment.cs:86-87`  
**Code:**
```csharp
[Serializable]
public class StatDevelopment
{
    [SerializeField] private float strength = BASE_STAT_VALUE;
```
**Explanation:** C-07 is already known. Additional note: `[SerializeField]` works with Unity's serialization system but only when the containing object is serialized by Unity (i.e., it's a MonoBehaviour field, ScriptableObject field, or marked with `[Serializable]` and nested inside one). If `StatDevelopment` is instantiated as a standalone object, `[SerializeField]` does nothing useful.

---

### L-NEW-02: `GameEvents` static events are never automatically cleared — memory leak risk
**File:** `Core/GameEvents.cs`  
**Explanation:** Static events hold references to subscribers. If a MonoBehaviour subscribes to `GameEvents.OnPlayerDeath` but doesn't unsubscribe in `OnDestroy`, the static event keeps a reference to the destroyed object, preventing GC. The `ClearAllEvents()` method exists but must be called manually (typically on scene load).  
**Fix:** Consider using weak references or automatic cleanup on scene load.

---

### L-NEW-03: `ServiceLocator.pendingRequests` can accumulate indefinitely if services are never registered
**File:** `Core/ServiceLocator.cs:44`  
**Explanation:** If `Request<T>` is called for a service that is never registered, the callback stays in `pendingRequests` forever. There's no timeout or cleanup mechanism.  
**Fix:** Add a timeout or cleanup method.

---

### L-NEW-04: `Camera2DSetup.OnValidate` calls `SetupCamera()` in editor — can overwrite manual changes
**File:** `Core/Camera2DSetup.cs:77-83`  
**Explanation:** Any change to any field in the inspector triggers `OnValidate`, which calls `SetupCamera()`, which overwrites ALL camera settings (orthographic, size, position, etc.). This makes it impossible to manually tweak the camera in the editor without this component reverting changes.  
**Fix:** Only apply specific changed settings, or make individual field validation.

---

### L-NEW-05: `ExpandingEffect._affectedBuffer` allocated but never used (dead code after Unity 6 update)
**File:** `Combat/Effects/ExpandingEffect.cs:45`  
**Code:**
```csharp
private Collider2D[] _affectedBuffer = new Collider2D[30]; // Never used
```
**Explanation:** The buffer was likely used with the old `OverlapCircleNonAlloc` API. After updating to `OverlapCircleAll` (line 109), the buffer is dead code. Same issue in `DirectionalEffect._hitBuffer` (line 47) and `FormationArrayEffect._affectedBuffer` (line 49).  
**Fix:** Remove unused buffer fields.

---

### L-NEW-06: `OrbitalWeapon._hitResults` allocated but never used
**File:** `Combat/OrbitalSystem/OrbitalWeapon.cs:43`  
**Code:**
```csharp
private Collider2D[] _hitResults = new Collider2D[10]; // Never used
```
**Explanation:** Same as L-NEW-05 — leftover from `OverlapCircleNonAlloc` API.  
**Fix:** Remove the unused field.

---

### L-NEW-07: `TechniqueEffect` sets `gameObject.SetActive(true)` in `Play()` but `Pause()` doesn't deactivate
**File:** `Combat/Effects/TechniqueEffect.cs:136,160`  
**Code:**
```csharp
public virtual void Play(...) { gameObject.SetActive(true); ... }
public virtual void Pause() { _isPlaying = false; } // Object still active
```
**Explanation:** `Play` activates the game object, but `Pause` only stops the animation logic — the object remains active and visible. `Stop` deactivates it. This asymmetry might confuse callers expecting `Pause` to hide the effect.  
**Fix:** Document the difference clearly or make behavior consistent.

---

### L-NEW-08: `QiBuffer.ProcessDamage` method is a misleading alias
**File:** `Combat/QiBuffer.cs:96-102`  
**Code:**
```csharp
public static QiBufferResult ProcessDamage(float incomingDamage, int currentQi, QiDefenseType defenseType)
{
    return ProcessQiTechniqueDamage(incomingDamage, currentQi, defenseType);
}
```
**Explanation:** `ProcessDamage` always routes to `ProcessQiTechniqueDamage`, but the method name doesn't indicate this. Callers might assume it handles both physical and Qi damage. The comment at line 74-76 warns about this, but it's still a footgun.  
**Fix:** Mark `[Obsolete("Use ProcessQiTechniqueDamage or ProcessPhysicalDamage")]` or rename to `ProcessQiDamage`.

---

### L-NEW-09: `DamageResult.IsFatal` uses arbitrary threshold of 50 damage
**File:** `Combat/DamageCalculator.cs:297-298`  
**Code:**
```csharp
result.IsFatal = (result.HitPart == BodyPartType.Heart || result.HitPart == BodyPartType.Head) 
              && result.FinalDamage > 50f;
```
**Explanation:** The threshold of 50 for a "fatal" hit is hardcoded and arbitrary. There's no reference to the body part's actual HP, and the value 50 is not documented in any design doc. A 50-damage hit to the head at early game might be overkill, while at late game it might be trivial.  
**Fix:** Compare against actual body part HP instead of a fixed threshold.

---

### L-NEW-10: `LevelSuppression.attackIndex` cast assumes enum values match array indices
**File:** `Combat/LevelSuppression.cs:84`  
**Code:**
```csharp
int attackIndex = (int)attackType;
```
**Explanation:** This relies on `AttackType.Normal=0, AttackType.Technique=1, AttackType.Ultimate=2` matching the suppression table column order. While currently correct, if the enum is reordered or new values inserted, this would silently access wrong table columns.  
**Fix:** Add a debug assert or use a named constant mapping.

---

## Files Reviewed

| File | Status | Notes |
|------|--------|-------|
| Core/StatDevelopment.cs | ⚠️ | C-07, C-08, C-NEW-02 (consolidation broken) |
| Core/Constants.cs | ⚠️ | C-NEW-01 (missing Construct), H-NEW-01 (OppositeElements), H-NEW-02 (MAX_STAT_VALUE conflict), M-NEW-12 (AwakeningChance units) |
| Core/VFXPool.cs | ⚠️ | H-03 (global max), M-NEW-01 (no DontDestroyOnLoad), M-NEW-02 (prewarm not implemented) |
| Core/Camera2DSetup.cs | ⚠️ | L-NEW-04 (OnValidate overwrites) |
| Core/GameEvents.cs | ✅ | L-NEW-02 (static event leak risk - low) |
| Core/ServiceLocator.cs | ✅ | L-NEW-03 (pending requests accumulate) |
| Core/Enums.cs | ⚠️ | M-NEW-04 (Element.Count), M-NEW-05 (Disposition mixed), H-NEW-08 (AttackResult collision) |
| Core/GameSettings.cs | ✅ | Clean |
| Combat/DamageCalculator.cs | ⚠️ | C-NEW-03 (elements ignored), C-NEW-04 (AttackType always Technique), H-NEW-07 (inconsistent RNG), M-NEW-10 (no defender element), L-NEW-09 (IsFatal threshold), L-NEW-10 (attackIndex cast) |
| Combat/DefenseProcessor.cs | ⚠️ | C-02 (System.Random), C-NEW-01 trigger (Construct lookup), C-09 (double calc path), H-NEW-07 (inconsistent RNG) |
| Combat/CombatManager.cs | ⚠️ | M-NEW-07 (dealt==taken), H-NEW-08 (AttackResult name collision) |
| Combat/Combatant.cs | ⚠️ | C-04 (SpendQi int), C-05 (CurrentQi int), C-NEW-05 (Qi truncation), C-NEW-06 (lambda leak), H-NEW-03 (double parry/block calc) |
| Combat/CombatEvents.cs | ⚠️ | C-10 (static constructor), M-NEW-06 (CombatLog queue inefficiency) |
| Combat/LevelSuppression.cs | ✅ | Clean, well-documented |
| Combat/TechniqueCapacity.cs | ✅ | Clean |
| Combat/QiBuffer.cs | ⚠️ | L-NEW-08 (ProcessDamage misleading name) |
| Combat/HitDetector.cs | ⚠️ | M-NEW-11 (interface GetComponent expensive) |
| Combat/TechniqueController.cs | ⚠️ | H-12 (mastery formula), H-13 (cooldown ×60), M-NEW-08, M-NEW-09 |
| Combat/Effects/TechniqueEffect.cs | ✅ | L-NEW-07 (Pause/Stop asymmetry) |
| Combat/Effects/ExpandingEffect.cs | ✅ | L-NEW-05 (dead buffer) |
| Combat/Effects/DirectionalEffect.cs | ⚠️ | M-NEW-03 (Instantiate not pooled), L-NEW-05 (dead buffer) |
| Combat/Effects/TechniqueEffectFactory.cs | ⚠️ | H-NEW-04 (wrong pool for Poison), M-NEW-13 (double dequeue) |
| Combat/Effects/FormationArrayEffect.cs | ✅ | H-NEW-05 (ICombatTarget namespace), L-NEW-05 (dead buffer) |
| Combat/OrbitalSystem/OrbitalWeaponController.cs | ⚠️ | M-NEW-14 (race condition on angle) |
| Combat/OrbitalSystem/OrbitalWeapon.cs | ⚠️ | M-NEW-03 (Instantiate not pooled), L-NEW-06 (dead buffer) |

---

## Summary Statistics

- **Critical NEW issues:** 6
- **High NEW issues:** 8
- **Medium NEW issues:** 14
- **Low NEW issues:** 10
- **Previously known issues verified:** 14 (C-01 through M-09)
- **Total files reviewed:** 25

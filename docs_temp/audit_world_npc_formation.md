# Audit: World + NPC + Formation + Save + Inventory + Quest + Interaction Systems
**Date:** 2026-04-10
**Auditor:** World+NPC+Formation+Save+Inv Audit Agent
**Files analyzed:** 28 files across 7 subsystems

## Critical Issues

1. **NPCController.cs — NPCSaveData missing MaxQi/MaxHealth/MaxStamina/MaxLifespan**
   - `NPCSaveData` does not contain `MaxQi`, `MaxHealth`, `MaxStamina`, or `MaxLifespan` fields
   - `LoadSaveData()` does not restore these values either
   - After loading, `state.MaxQi`, `state.MaxHealth`, etc. will be default (0 for int, 0f for float)
   - This means any loaded NPC will have 0 max health/qi/stamina, effectively killing them or making them unusable
   - **Fix:** Add `MaxQi`, `MaxHealth`, `MaxStamina`, `MaxLifespan` to `NPCSaveData` and restore them in `LoadSaveData()`

2. **LocationController.cs:330-335 — CompleteTravel nullifies travelDestinationId before ShouldTriggerTravelEvent uses it**
   - In `CompleteTravel()`, lines 330-332 set `travelDestinationId = null` and `travelProgress = 0f`
   - Then `ShouldTriggerTravelEvent()` on line 335 calls `GetLocation(travelDestinationId)` where it's now null
   - `GetLocation(null)` returns null, so the destination's danger level is never factored into travel event calculation
   - **Fix:** Save `travelDestinationId` to a local variable before clearing it, and use the local in `ShouldTriggerTravelEvent()`

3. **RelationshipController.cs:327-358 — ProcessDecay mixes real time (Time.time) with game time**
   - `record.LastInteractionTime` is set using `Time.time` (real wall-clock time) on line 196
   - `ProcessDecay(float currentGameTime)` expects game time, but the comparison `currentGameTime - record.LastInteractionTime` is mixing scales
   - With time acceleration (60x–900x), this will cause relationships to decay at wildly wrong rates
   - **Fix:** Use consistent time source — either both real time or both game time

4. **QuestData.cs + QuestObjective.cs — Quest objectives stored on shared ScriptableObject cause state corruption**
   - `QuestData` (ScriptableObject) contains `List<QuestObjective> objectives` with mutable state (currentAmount, state)
   - ScriptableObjects are shared assets; modifying objective state on one quest instance mutates ALL references
   - If player accepts the same quest type twice, both share the same objective instances
   - After completing a quest, the ScriptableObject's objectives remain in "Completed" state — re-accepting the same quest type is impossible
   - **Fix:** Clone objectives into ActiveQuest at acceptance time, don't mutate the ScriptableObject's list

5. **QuestController.cs:641-650 — LoadSaveData doesn't restore active quests**
   - `LoadSaveData` has `// TODO: Восстановить активные квесты` — active quests are simply dropped on load
   - All quest progress is lost when loading a save
   - **Fix:** Reconstruct active quests from save data, re-subscribe to objective events, restore objective progress

6. **InventorySlot.cs:674 — GetCondition returns Pristine for broken items (durability=0)**
   - `if (durability <= 0) return DurabilityCondition.Pristine;` — items with 0 durability (broken) return "Pristine"
   - Should be: items with durability < 0 (no durability system) → Pristine; durability == 0 → Broken
   - Same bug exists in EquipmentInstance.GetCondition() line 536
   - **Fix:** Change to `if (durability < 0) return DurabilityCondition.Pristine;` in both classes

7. **FormationEffects.cs:340-368 — ApplyControlFallback permanently modifies Rigidbody2D**
   - Freeze sets `rb.simulated = false` with no timer to restore
   - Slow modifies `rb.linearVelocity *= 0.5f` once (no ongoing slowdown, no restore)
   - Root sets `rb.linearVelocity = Vector2.zero` once (no ongoing root, no restore)
   - None of these effects are ever reversed — permanent movement disable
   - **Fix:** Implement a coroutine or timed system to restore original state after `controlDuration`

8. **DialogueSystem.cs:474 — LoadDialogueFromJson uses JsonUtility with array root, which always fails**
   - `JsonUtility.FromJson<DialogueNode[]>(json)` — JsonUtility cannot deserialize JSON arrays as root objects
   - This method will always throw or return empty, making JSON dialogue loading non-functional
   - **Fix:** Use a wrapper class: `[Serializable] class DialogueNodeList { public DialogueNode[] nodes; }` then `JsonUtility.FromJson<DialogueNodeList>(json)`

## High Issues

9. **TimeController.cs:195-207 — AdvanceHour fires OnHourPassed with post-increment hour value**
   - `AdvanceHour()` increments `currentHour++` then fires `OnHourPassed(currentHour)`
   - When hour rolls from 23→0, the event fires with hour=0 instead of the old value 23
   - Similarly `AdvanceDay()` fires `OnDayPassed(currentDay)` with day=31 before rolling to month advance
   - **Fix:** Fire events with the old value or a more meaningful value before mutating

10. **NPCAI.cs:146-175 — DecideNormalBehavior has illogical probability distribution**
    - `socialness * 0.3f` determines Wandering (exploration), but socialness should determine social behavior
    - `ambition * 0.4f` determines Cultivating, but the ranges overlap with socialness ranges
    - The `if/else if` chain means earlier conditions take priority, producing unexpected distributions
    - **Fix:** Redesign probability weights; use independent weights or a proper weighted selection

11. **NPCController.cs:201-205 — UpdateLifespan() is empty, age-related death never occurs**
    - `CheckDeathFromAge()` checks `state.Lifespan <= 0` but nothing decrements lifespan
    - `UpdateLifespan()` is called every frame but is an empty stub
    - **Fix:** Implement lifespan decrement based on game time passage

12. **NPCController.cs:241-253 — TakeDamage doesn't notify AI of attacker**
    - When an NPC takes damage, the AI's threat system is never notified
    - NPC will not fight back or flee from attacker
    - **Fix:** Call `aiController.AddThreat(attackerId, ...)` in `TakeDamage()`

13. **EventController.cs:201-207 — Cooldown uses Time.time (real seconds) not game time**
    - `CanTriggerEvent()` calculates cooldown in real seconds
    - The formula `CooldownDays * 86400f / 60f` is wrong for the time acceleration system
    - At different game speeds, cooldowns will feel different durations
    - **Fix:** Use game time from TimeController for cooldown tracking

14. **WorldController.cs — WorldData.EventData uses Dictionary<string, object> which is not Unity-serializable**
    - `WorldEvent.EventData` is `Dictionary<string, object>` — Unity's serializer cannot handle this
    - Will be lost on domain reload and cannot be saved properly
    - **Fix:** Replace with serializable structure (e.g., List of key-value string pairs)

15. **FactionController.cs:467-477 — LoadSaveData doesn't restore playerMemberships**
    - `LoadSaveData()` clears `playerMemberships` but never repopulates it from `data.Memberships`
    - After loading, no NPC will have faction memberships
    - **Fix:** Iterate `data.Memberships` and reconstruct `playerMemberships` dictionary

16. **NPCData.cs:60 — NPCState.SkillLevels is Dictionary<string, float>, not Unity-serializable**
    - `SkillLevels` in both `NPCState` and `NPCSaveData` uses `Dictionary<string, float>`
    - Unity's default serializer cannot serialize this; it will be empty after domain reload
    - **Fix:** Convert to two parallel Lists (keys + values) or use a serializable wrapper

17. **RelationshipController.cs — CalculateRelationshipType never returns Stranger or SwornSibling**
    - `Stranger` (supposed to be <-50) is never returned; `Hostile` is returned for <= -50 instead
    - `SwornSibling` (supposed to be 90+) is never returned; `Family` is returned for >= 75
    - These enum values are unreachable in normal flow
    - **Fix:** Adjust thresholds to match the enum documentation, or remove unused values

18. **FormationEffects.cs:69-101 — IsAlly() faction detection never works**
    - `target.GetComponent<World.FactionController>()` looks for FactionController as a component on individual GameObjects
    - But FactionController is a global manager, not attached to individual characters
    - `targetFaction` will always be null, and faction-based ally detection is dead code
    - **Fix:** Use a global FactionController reference and look up membership by NPC ID

19. **FormationCore.cs:454 — ApplyEffects uses OverlapCircleAll without layer mask**
    - `Physics2D.OverlapCircleAll(center, currentRadius)` detects ALL colliders, not just characters
    - Will attempt to apply effects to terrain, items, projectiles, etc.
    - **Fix:** Pass the `targetLayerMask` from FormationController configuration

20. **CraftingController.cs:194-197 — Craft() creates items but never applies calculated grade**
    - Equipment grade is calculated (`DetermineGrade(baseQuality)`) but never applied to created items
    - `playerInventory.AddItem(recipe.resultItem, 1)` always creates Common-grade items
    - TODO comment on line 197 confirms this is known but unimplemented
    - **Fix:** Create the item with the determined grade applied

21. **CraftingController.cs:426-438 — AddSkillExperience uses random level-up instead of XP accumulation**
    - Skill level up is determined by `UnityEngine.Random.value < 0.2f` — 20% random chance per craft
    - No actual experience tracking; the `experience` parameter is ignored
    - **Fix:** Track experience points per skill and level up at thresholds

22. **SaveManager.cs:143 — TotalPlayTimeHours uses game-time not real play time**
    - `saveData.TotalPlayTimeHours = (float)(timeController.TotalGameSeconds / 3600.0)` stores game hours
    - At 60x speed, 1 real hour = 60 game hours — displayed "Play Time" is misleading
    - **Fix:** Use `Time.realtimeSinceStartup` or accumulate real wall-clock time separately

23. **SaveDataTypes.cs:130,209 — Dictionary fields in save data types not JsonUtility-compatible**
    - `SettingsSaveData.KeyBindings` is `Dictionary<string, string>` — not serializable by JsonUtility
    - `QuestSaveData.Objectives` is `Dictionary<string, int>` — not serializable by JsonUtility
    - `QuestSaveData.CompletionTimeUnix` is `long?` — nullable not supported by JsonUtility
    - These will silently deserialize as empty/null
    - **Fix:** Replace Dictionaries with serializable List-of-pairs; replace nullable with sentinel value

24. **SaveDataTypes.cs + CraftingSaveData + EquipmentStats — Multiple Dictionary serialization failures**
    - `EquipmentStats.customBonuses` is `Dictionary<string, float>` — not JsonUtility-serializable
    - `CraftingSaveData.skills` is `Dictionary<CraftingType, int>` — enum-keyed dict not serializable
    - `EquipmentController.GetSaveData()` returns `Dictionary<string, EquipmentSaveData>` — not serializable
    - **Fix:** Replace all Dictionary fields in save data with serializable List structures

## Medium Issues

25. **TimeController.cs:299-303 — SetTime() doesn't fire any transition events**
    - Calling `SetTime()` changes hour/minute but doesn't fire `OnHourPassed`, `OnTimeOfDayChanged`, etc.
    - Listeners won't be notified of time changes
    - **Fix:** Fire appropriate events after setting time, or document this as intentional

26. **TimeController.cs:318-324 — AdvanceHours()/AdvanceDays() can cause cascading event storms**
    - `AdvanceHours(100)` calls `AdvanceHour()` 100 times, each firing events
    - Can cause performance issues and duplicate event processing
    - **Fix:** Batch the advancement and fire consolidated events

27. **LocationController.cs:112 — Uses FindFirstObjectByType<TimeController> (known H-02)**
    - Also EventController.cs:129-131, SaveManager.cs:104-108, FormationController.cs:215
    - Performance concern and architectural issue across multiple systems

28. **NPCAI.cs:308-323 — Threat levels never decay**
    - `AddThreat()` accumulates threat, but there's no passive decay mechanism
    - An NPC that was attacked once will remember the threat forever
    - **Fix:** Add threat decay in `Update()` or on a timer

29. **NPCAI.cs:265-273 — ExecuteCultivating restores only 10 Qi then returns to Idle**
    - After 30 seconds of cultivating, only 10 Qi is restored
    - No continuous Qi regeneration during cultivation state
    - **Fix:** Implement gradual Qi restoration over time during cultivation

30. **RelationshipController.cs:107-109 — ownerId not validated before use**
    - If `Initialize()` is never called, `ownerId` is null
    - `GetKey()` returns `"_targetId"` which produces invalid keys
    - **Fix:** Add null check or log warning in `GetKey()`

31. **RelationshipController.cs:269-282 — GetTargetsByType uses stale record.Type**
    - `GetTargetsByType()` compares `record.Type` but `Type` is only updated in `ModifyRelationship` and `ProcessDecay`
    - If a flag like "master" is added via `AddFlag()`, `record.Type` won't change
    - Meanwhile `GetRelationshipType()` correctly checks flags first
    - **Fix:** Update `record.Type` in `AddFlag()`/`RemoveFlag()`, or use `GetRelationshipType()` in query

32. **NPCController.cs:332 — InitializeFromGenerated uses random age instead of generated data**
    - `state.Age = 18 + UnityEngine.Random.Range(0, 50)` ignores any age from `GeneratedNPC`
    - **Fix:** Use `generated.age` if available, or document that age is randomized

33. **EventController.cs:134-146 — eventCheckInterval is in real seconds, not game time**
    - `eventCheckInterval = 60f` means events are checked every 60 real seconds
    - At VeryFast speed (900x), that's 54000 game minutes between checks
    - **Fix:** Scale check interval with game speed or use game time ticks

34. **LocationController.cs:70 — LocationData.BuildingType? nullable enum won't serialize in Unity**
    - `BuildingType?` is a nullable enum — Unity's serializer doesn't support nullable value types
    - **Fix:** Use a separate bool flag + non-nullable enum

35. **FactionData — FactionRelations Dictionary<string,int> won't serialize in Unity**
    - `Dictionary<string, int>` is not supported by Unity's default serializer
    - Will be empty after domain reload
    - **Fix:** Use serializable key-value list structure

36. **NPCController.cs:360-361 — Casting generated.maxQi (likely long) to int**
    - `state.MaxQi = (int)generated.maxQi` — if maxQi exceeds int.MaxValue, data loss occurs
    - In a cultivation game, Qi values can grow very large
    - **Fix:** Change NPCState.MaxQi/CurrentQi to long, or validate range before casting

37. **FormationController.cs:507 — ContributeQi casts long CurrentQi to int**
    - `int amount = Mathf.Min(maxAmount, (int)playerQi.CurrentQi)` — if CurrentQi > int.MaxValue, overflow
    - **Fix:** Use long arithmetic throughout or validate range

38. **FormationCore.cs:34 — practitionerId uses GetInstanceID() which isn't persistent**
    - `practitionerId = practitioner.GetInstanceID().ToString()` — InstanceID changes between sessions
    - After save/load, participant IDs won't match original practitioners
    - **Fix:** Use a persistent ID from NPCController or similar

39. **FormationQiPool.cs:326-330 — AcceptQi uses int for accepted amount, can overflow from long space**
    - `long space = capacity - currentQi` is long, but `accepted = (int)Mathf.Min(accepted, space)` truncates to int
    - If space > int.MaxValue, this produces wrong results
    - **Fix:** Use long arithmetic throughout AcceptQi

40. **FormationEffects.cs:291-310 — ApplyHeal always heals both Qi AND body**
    - No way to configure "heal Qi only" vs "heal body only"
    - Both `qi.AddQi()` and `body.Heal()` are called unconditionally
    - **Fix:** Add a heal target field to FormationEffect

41. **InteractionController.cs:108-138 — ScanForInteractables runs every frame with OverlapCircleAll**
    - `Physics2D.OverlapCircleAll` allocates a new array and scans physics every frame
    - Very expensive; should use interval-based scanning or trigger-based detection
    - **Fix:** Scan every 0.2-0.5s instead of every frame, or use OnTriggerEnter/Exit

42. **InteractionController.cs:58 — InteractionResult.CustomData is Dictionary<string, object>**
    - Not serializable by Unity or JsonUtility
    - **Fix:** Replace with serializable structure

43. **DialogueSystem.cs — No save/load support for dialogue state**
    - `dialogueFlags` are never persisted; all dialogue progress is lost on save/load
    - `dialogueNodes` cache is never saved; loaded dialogues are lost
    - **Fix:** Add GetSaveData/LoadSaveData for dialogue flags and current state

44. **DialogueSystem.cs:460-465 — LoadDialogueNode always returns null**
    - The entire dialogue loading pipeline is unimplemented
    - `LoadDialogueNode` is the only way to load dialogue data, and it's a stub
    - **Fix:** Implement dialogue data loading from ScriptableObjects or resources

45. **SaveManager.cs:232-234 — CollectSaveData doesn't save Player or NPC data**
    - TODO comment: "Добавить игрока и NPC при интеграции"
    - Player data and NPC data are completely missing from saves
    - Formation, Quest, Inventory, Equipment systems are also not saved
    - **Fix:** Integrate all subsystem save data into CollectSaveData/ApplySaveData

46. **SaveManager.cs:434-442 — XOR encryption is trivially breakable**
    - Simple XOR with 0x5A then Base64 — not real encryption
    - Meanwhile SaveFileHandler.cs has proper AES encryption but is unused (known M-06)
    - **Fix:** Use SaveFileHandler's AES encryption or integrate a proper encryption scheme

47. **SaveFileHandler.cs:336 — AES encryption uses zero IV**
    - `aes.IV = new byte[16]` — all-zero IV means identical data always produces identical ciphertext
    - Combined with no salt in key derivation (line 373), this is vulnerable to known-plaintext attacks
    - **Fix:** Generate random IV and store it prepended to ciphertext; use proper key derivation (PBKDF2)

## Low Issues

48. **TimeController.cs:341 — CalculateTimeOfDay: hour 0 = Midnight but hours 21-23 = Night**
    - No explicit "Late Night" distinction; Midnight is a separate value, which is fine but could be more nuanced

49. **WorldController.cs:259-272 — GetNPCsInLocation creates new list every call**
    - Allocates a new List each call — frequent calls could cause GC pressure

50. **FactionController.cs:93-98 — FactionData constructor initializes Lists but not all fields**
    - `MaxMembers` defaults to 0, which means "unlimited" (confusing)

51. **NPCData.cs:119-135 — NPCInteractionResult and DialogueOption are defined but unused**
    - These classes exist in NPCData.cs but no code uses them

52. **TestLocationGameController.cs:258-293 — CreateTempSprite creates Texture2D that's never destroyed**
    - Memory leak — Texture2D should be destroyed when no longer needed (known L-07)

53. **FormationUI.cs:526-559 — CreateDefaultPreview creates Texture2D that's never destroyed**
    - Same memory leak pattern

54. **FormationUI.cs:131 — Gets FormationController.Instance in Awake, may be null**
    - If FormationController hasn't run its Awake yet, Instance is null
    - **Fix:** Use lazy initialization or subscribe in Start()

55. **FormationUI.cs:484 — Camera.main called every frame during placement mode**
    - Performance concern; cache the camera reference

56. **EquipmentController.cs:239-248 — CanEquip always returns true**
    - No requirement checks implemented despite TODO comments
    - All equipment can be equipped regardless of level or stats

57. **CraftingController.cs:218-269 — CraftCustom always succeeds**
    - Line 260: `result.success = true` — custom crafting has no failure chance
    - Unlike regular Craft(), there's no success/fail roll

58. **QuestController.cs:283 — GrantRewards is a TODO stub**
    - Quest rewards are never actually granted to the player
    - Only logs to console

59. **DialogueSystem.cs:351-358 — Typing effect may skip characters at low FPS**
    - `if (Time.deltaTime >= typingSpeed)` may miss frames; characters could appear in bursts

60. **SaveManager.cs:199 — Random seed saved as hash, not restorable**
    - `Seed = UnityEngine.Random.state.GetHashCode()` saves a hash, not the actual state
    - Cannot restore the random number generator from this value

## Files Reviewed

### World System
- TimeController.cs ✅ (3 issues found)
- WorldController.cs ✅ (2 issues found)
- LocationController.cs ✅ (4 issues found)
- FactionController.cs ✅ (4 issues found)
- EventController.cs ✅ (3 issues found)
- TestLocationGameController.cs ⚠️ (2 issues, known L-07)

### NPC System
- NPCAI.cs ✅ (3 issues found)
- NPCController.cs ✅ (5 issues found)
- NPCData.cs ✅ (2 issues found)
- RelationshipController.cs ✅ (4 issues found)

### Formation System
- FormationController.cs ✅ (2 issues found)
- FormationData.cs ✅ (no new issues)
- FormationEffects.cs ✅ (4 issues found)
- FormationQiPool.cs ✅ (1 issue found)
- FormationCore.cs ✅ (3 issues found)
- FormationUI.cs ✅ (4 issues found)

### Save System
- SaveManager.cs ✅ (4 issues found)
- SaveDataTypes.cs ✅ (3 issues found)
- SaveFileHandler.cs ⚠️ (2 issues, known M-06 unused)

### Inventory System
- InventoryController.cs ✅ (1 issue found)
- EquipmentController.cs ✅ (3 issues found)
- CraftingController.cs ✅ (3 issues found)
- MaterialSystem.cs ✅ (no new issues)

### Quest System
- QuestController.cs ✅ (3 issues found)
- QuestData.cs ✅ (1 critical issue found)
- QuestObjective.cs ✅ (no new standalone issues)

### Interaction System
- DialogueSystem.cs ✅ (4 issues found)
- InteractionController.cs ✅ (2 issues found)

## Summary Statistics
- **Critical Issues:** 8
- **High Issues:** 16
- **Medium Issues:** 23
- **Low Issues:** 13
- **Total Issues:** 60 (including known issues verified)
- **Files Analyzed:** 28

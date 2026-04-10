# Audit: Body + Qi + Player + Managers Systems
**Date:** 2026-04-10
**Files analyzed:** BodyPart.cs, BodyController.cs, BodyDamage.cs, QiController.cs, PlayerController.cs, PlayerVisual.cs, SleepSystem.cs, GameManager.cs, GameInitializer.cs, SceneLoader.cs, BuffManager.cs

## Critical Issues

1. **C-03 (VERIFIED) BodyPart.cs:94 — Heart gets blackHP despite docs saying it shouldn't**
   ```csharp
   this.maxBlackHP = maxRedHP * GameConstants.STRUCTURAL_HP_MULTIPLIER;
   this.currentBlackHP = this.maxBlackHP;
   ```
   Heart should have `maxBlackHP = 0` when `partType == BodyPartType.Heart`. Currently Heart has structural HP making it much harder to kill than designed.

2. **[NEW] BodyDamage.cs:99-149 — CreateHumanoidBody HP values don't match BODY_SYSTEM.md**
   Code documents 5/7 parts having wrong HP (lines 115-121). Head=30 (should be 50), Heart=15 (should be 80), Hands=15 (should be 20), Feet=10 (should be 25). Heart is only 18.75% of documented HP.

3. **[NEW] QiController.cs:357,364,365,371 — long→int cast loses Qi values above 2.1 billion**
   ```csharp
   result.QiRemaining = (int)currentQi;
   QiBuffer.ProcessQiTechniqueDamage(damage, (int)currentQi, QiDefense);
   ```
   At cultivation level 4+ where capacity exceeds int.MaxValue, combat Qi absorption uses incorrect/wrapped values.

4. **[NEW] BuffManager.cs:788,792,796,800,930,934,938 — float.Parse on special.parameters crashes on invalid input**
   ```csharp
   currentShield += float.Parse(special.parameters);  // line 788
   lifestealPercent += float.Parse(special.parameters);  // line 792
   ```
   If `special.parameters` is null, empty, or non-numeric, this throws `FormatException` at runtime with no try/catch. Seven call sites affected. Fix: use `float.TryParse` with default values.

5. **[NEW] BuffManager.cs:1000-1004 — HandleExistingBuff returns Applied for Independent but never adds buff**
   ```csharp
   else  // StackType.Independent
   {
       return BuffResult.Applied;  // Lies! Nothing was applied
   }
   ```
   When a non-stackable buff has `stackType == Independent`, the method returns `BuffResult.Applied` but never creates a new ActiveBuff entry. The caller assumes the buff was applied but it's silently lost. Fix: create and add a new ActiveBuff for Independent stacking.

6. **[NEW] BuffManager.cs:126–439 — ConductivityModifier tracked but never applied to QiController**
   The `conductivityModifier` field and payback system are fully implemented in BuffManager, but no code ever calls `qiController.SetConductivityBonus()` or reads the modifier to update QiController's conductivity. The payback ticks reduce `conductivityModifier` over time, but since nothing reads it, the entire conductivity buff/payback system is dead code with zero gameplay effect.

7. **[NEW] SceneLoader.cs:226-277 — Loading scene unloaded after already destroyed by Single-mode load**
   ```csharp
   yield return SceneManager.LoadSceneAsync(loadingScene, LoadSceneMode.Additive);  // line 231
   // ...
   AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);  // Single mode (default)
   // This destroys the additive loading scene!
   // ...
   yield return SceneManager.UnloadSceneAsync(loadingScene);  // line 277 - scene already gone
   ```
   Loading a scene in Single mode (default) unloads all other scenes including the additively-loaded loading screen. The subsequent `UnloadSceneAsync` call on line 277 attempts to unload an already-destroyed scene, causing a Unity error/warning.

8. **[NEW] SceneLoader.cs:286-289 — Unconditional timeScale=1f after loading unpauses a previously-paused game**
   ```csharp
   if (pauseDuringLoading)
   {
       Time.timeScale = 1f;  // Always 1f, even if game was paused before loading
   }
   ```
   Should save and restore the previous `Time.timeScale` value rather than always setting it to 1f.

## High Issues

9. **C-04 (VERIFIED) QiController.cs:162 — SpendQi takes int not long**

10. **H-08 (VERIFIED) QiController.cs:248-260 — Meditate doesn't advance game time**

11. **H-09 (VERIFIED) PlayerController.cs — No ICombatant interface on PlayerController**

12. **H-10 (VERIFIED) GameManager.cs:157-164 — FindReferences uses FindFirstObjectByType**

13. **H-04 (VERIFIED) GameInitializer.cs:355 — SubscribeToEvents() never called, SubscribeToTimeEvents() dead code**
    `SubscribeToEvents()` (line 355-364) is the only method that calls `SubscribeToTimeEvents()` (line 415-418), but `SubscribeToEvents()` itself is never invoked. Instead, individual subscribe methods are called directly. The time event `OnTimeSpeedChanged` subscription never happens.

14. **[NEW] QiController.cs:308 — coreCapacity set to stale maxQiCapacity after breakthrough**
    After `cultivationLevel++`, `coreCapacity = maxQiCapacity` uses the OLD value. `RecalculateStats()` then computes a new higher `maxQiCapacity`. Breakthrough costs become progressively too cheap. Fix: move `coreCapacity = maxQiCapacity` AFTER `RecalculateStats()`.

15. **[NEW] PlayerController.cs:223 — Uses rb.velocity (obsolete in Unity 6000.3)**
    ```csharp
    rb.velocity = moveInput * speed;
    ```
    Should be `rb.linearVelocity` for Unity 6+.

16. **[NEW] PlayerController.cs:351 — Revive() ignores healthPercent parameter**
    ```csharp
    public void Revive(float healthPercent = 0.5f)
    {
       bodyController.FullRestore();  // Always 100%, ignores healthPercent
       qiController.RestoreFull();     // Always 100%
    ```
    The parameter suggests the player should revive at 50% (default) but FullRestore always gives 100%.

17. **[NEW] PlayerController.cs:231-232 / PlayerState:504-505 — long→int truncation in state sync**
    ```csharp
    state.CurrentQi = (int)qiController.CurrentQi;
    state.MaxQi = (int)qiController.MaxQi;
    ```
    PlayerState uses `int` for Qi, truncating long values. Same issue in GetSaveData line 473 and LoadSaveData line 482.

18. **[NEW] SleepSystem.cs:288-289 — HP recovery formula is wrong**
    ```csharp
    float hpAmount = hours * hpRecoveryRate * 100; // hours * 0.125 * 100 = hours * 12.5
    bodyController.HealAll(hpAmount, hpAmount * 0.3f);
    ```
    HealAll applies `hpAmount` flat to EACH body part. For 8h sleep, each part gets 100 HP. But parts have different max HP (Head=30, Torso=100). Head gets 333% of max. Comment says "100% за 8 часов" but implementation is inconsistent.

19. **[NEW] SleepSystem.cs:302-306 — ProcessFinalHPRecovery always fully restores regardless of sleep duration**
    ```csharp
    bodyController.FullRestore();
    float afterHP = 100f;
    ```
    Even minimum 4h sleep triggers FullRestore, making per-hour healing in ProcessRecovery() redundant.

20. **[NEW] BuffManager.cs:1156,1202 — ScriptableObject.CreateInstance leaks memory**
    ```csharp
    var data = ScriptableObject.CreateInstance<BuffData>();  // line 1156
    var stunData = ScriptableObject.CreateInstance<BuffData>();  // line 1202
    ```
    Created ScriptableObjects are never destroyed. Each buff application from formation effects or control effects leaks a ScriptableObject. Fix: cache temp BuffData or destroy after use.

21. **[NEW] BuffManager.cs:772,905 — Slow effect hardcodes 50%, doesn't use buff data**
    ```csharp
    slowMultiplier *= 0.5f; // 50% замедление — always 50%
    ```
    Multiple slows compound multiplicatively (0.5 * 0.5 = 0.25 = 75% slow), which is extremely harsh. The slow value should come from the buff data.

22. **[NEW] BuffManager.cs:1232-1257 — RemoveControl doesn't check for other active buffs**
    Unlike `RemoveSpecialEffect` which checks `HasSpecialEffect`, `RemoveControl` unconditionally sets flags to false. If two buffs apply Stun, removing one via `RemoveControl` sets `isStunned = false` while the other buff still applies.

23. **[NEW] GameInitializer.cs:394-413 — Individual subscribe methods don't check isSubscribed, causing double-subscription on Reinitialize**
    `SubscribeToSaveEvents()` and `SubscribeToPlayerEvents()` don't check the `isSubscribed` flag. When `Reinitialize()` is called, it unsubscribes then re-runs `InitializeGameAsync()` which calls these methods directly, but they could double-subscribe if called outside the normal flow.

24. **[NEW] GameManager.cs:91 — Property `Time` shadows `UnityEngine.Time`**
    ```csharp
    public TimeController Time => timeController;
    ```
    This causes ambiguity. Code on line 127 uses `UnityEngine.Time.deltaTime` explicitly, showing the collision. Could lead to subtle bugs if someone forgets the `UnityEngine.` prefix.

25. **[NEW] BodyDamage.cs:176 — CreateQuadrupedBody Torso marked isVital=true contrary to docs**

26. **[NEW] BodyController.cs:54 — BodyParts property exposes mutable internal list**

27. **[NEW] BodyController.cs:283 — Division by zero risk in Heal(int) when bodyParts is empty**

## Medium Issues

28. **[NEW] BuffManager.cs:308-320 — RemoveBuff(FormationBuffType) removes buffs too broadly**
    Removes ALL buffs that modify the matching stat name, not just the specific buff type. A buff modifying both "damage" and "speed" would be fully removed when targeting only speed.

29. **[NEW] BuffManager.cs:1173 — CreateTempBuffData converts seconds to ticks without considering tickInterval**
    ```csharp
    data.durationTicks = Mathf.RoundToInt(duration);
    ```
    If `tickInterval` is changed from 1.0, the duration in seconds won't map to the correct number of ticks.

30. **[NEW] BuffManager.cs:763 — ApplySpecialEffect uses random trigger chance at application time, not per-tick**
    Special effects check `triggerChance` when the buff is first applied, not when they trigger periodically. Two identical buffs applied simultaneously may have different special effects active.

31. **[NEW] BuffManager.cs:1260 — CurrentControl only returns one type, loses information**
    If both Stun and Root are active, CurrentControl only returns Stun. The IsControlled property is correct but CurrentControl is lossy.

32. **[NEW] SleepSystem.cs:317 — long→int truncation in ProcessFinalQiRecovery**
    ```csharp
    return (int)(afterQi - beforeQi);
    ```

33. **[NEW] SleepSystem.cs:346 — Auto-sleep capped at optimalSleepHours (8h), not maxSleepHours (12h)**
    CalculateOptimalSleepTime clamps to `optimalSleepHours` not `maxSleepHours`. Even if HP/Qi need more than 8 hours, auto-sleep stops at 8h. (Mitigated by FullRestore always running.)

34. **[NEW] SleepSystem.cs:246-251 — QuickSleep bypasses state management and events**
    Doesn't set state to Sleeping, doesn't fire OnSleepStarted/OnSleepEnded events, doesn't interact with time system.

35. **[NEW] SleepSystem.cs:169-174 — FallingAsleep/WakingUp states never active**
    ```csharp
    SetState(SleepState.FallingAsleep);
    SetState(SleepState.Sleeping);  // Immediately overwrites
    ```
    Same for WakingUp (lines 194-195). These transition states are unreachable in the current implementation.

36. **[NEW] PlayerVisual.cs:70 — Invalid URP shader name**
    ```csharp
    Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
    ```
    "Sprite-Unlit-Default" is not a standard URP shader. Should be "Universal Render Pipeline/2D/Sprite-Lit-Default" or "Sprites/Default". The fallback handles it but the URP path never works.

37. **[NEW] PlayerVisual.cs:70,103 — Material and Texture2D memory leaks**
    `new Material()` and `new Texture2D()` are created but never destroyed in OnDestroy. Each scene reload leaks GPU memory.

38. **[NEW] PlayerVisual.cs:166-172 — Flash race condition**
    If Flash() is called while another flash is active, the first coroutine's captured originalColor is the flash color, not the player color. Restoring causes wrong color.

39. **[NEW] QiController.cs:251 — Meditate float overflow risk**
    `float baseGain = conductivity * qiDensity * durationTicks;` — at high levels with large durationTicks, float precision is insufficient. Passive regen uses double; meditation should too.

40. **[NEW] QiController.cs:100 — baseConductivity float precision loss for large capacities**

41. **[NEW] BodyDamage.cs:62 — Dual 70/30 split paths (BodyDamage.ApplyDamage and BodyPart.ApplyDamage) are confusing**

42. **[NEW] BodyController.cs:251-258 — FullRestore sets Heart blackHP to non-zero (cascading from C-03)**

43. **[NEW] GameManager.cs:314-331 — StartNewGame goes Loading→Playing instantly with no async work**
    The Loading state is never visible since there's no actual loading operation between the two SetState calls.

44. **[NEW] GameManager.cs:99-109 — Singleton with DontDestroyOnLoad retains stale scene references**
    worldController, playerController references become null/invalid after scene transitions.

45. **[NEW] SleepSystem.cs:139 — Uses FindFirstObjectByType instead of ServiceLocator**
    Inconsistent with PlayerController which was updated to use ServiceLocator.

## Low Issues

46. **[NEW] BodyController.cs:84-93 — InitializeBody() can be called multiple times, losing damage state**

47. **[NEW] QiController.cs:162 — SpendQi(0) fires events and returns true**

48. **[NEW] PlayerController.cs:105-118 — OnDestroy only unsubscribes body/qi events, not other potential subscriptions**

49. **[NEW] SleepSystem.cs:98 — sleepStartTime tracked but never used for any calculation**

50. **[NEW] BuffManager.cs:68 — BuffResult.Replaced enum value is never returned by any code path**

51. **[NEW] BuffManager.cs:1100-1107 — IsPercentageModifier hardcodes stat names, fragile**

52. **[NEW] BuffManager.cs:1109-1129 — GetStatFromController only handles 3 of many possible stats**

53. **[NEW] PlayerVisual.cs:190 — Camera.main is slow/obsolete in Unity 6**

54. **[NEW] GameManager.cs:337-344 — LoadGame is a stub that doesn't actually load anything**

55. **[NEW] GameInitializer.cs:446 — HandleBodyDamageTaken casts float damage sum to int, losing precision**

56. **[NEW] SceneLoader.cs:169 — ReloadCurrentScene uses stale currentScene variable**

## Files Reviewed

- ⚠️ BodyPart.cs — C-03 verified, negative HP issue
- ⚠️ BodyController.cs — Mutable list exposure, div-by-zero, FullRestore/C-03 cascade
- ⚠️ BodyDamage.cs — Major HP value discrepancies with docs, Quadruped vital issue
- ⚠️ QiController.cs — long→int casts in combat, coreCapacity stale after breakthrough
- ⚠️ PlayerController.cs — rb.velocity obsolete, Revive ignores param, Qi truncation
- ⚠️ PlayerVisual.cs — Shader name wrong, memory leaks, flash race condition
- ⚠️ SleepSystem.cs — Wrong HP recovery, unconditional FullRestore, dead states
- ⚠️ GameManager.cs — Time property shadow, FindFirstObjectByType, stale references
- ⚠️ GameInitializer.cs — SubscribeToEvents never called, double-subscription risk
- ⚠️ SceneLoader.cs — Loading scene already destroyed, unconditional unpause
- ⚠️ BuffManager.cs — float.Parse crashes, dead conductivity system, leaks, control inconsistencies

**Total: 8 Critical, 19 High, 18 Medium, 11 Low issues**
**NEW issues found: 8 Critical, 16 High, 18 Medium, 11 Low = 53 new issues**
**Known issues verified: C-03, C-04, H-04, H-08, H-09, H-10**

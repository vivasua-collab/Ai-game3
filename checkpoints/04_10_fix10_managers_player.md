# Чекпоинт: Fix-10 — Managers + Player System

**Дата:** 2026-04-11 (updated)
**Фаза:** Phase 7 — Integration
**Статус:** completed
**Приоритет:** HIGH

---

## Описание

Managers: параллельная инициализация, SceneLoader баги, FindFirstObjectByType, заглушки. Player: ICombatant реализован, SleepSystem исправлена, PlayerVisual утечки устранены.

---

## Файлы (7 файлов изменено)

| # | Файл | Изменение |
|---|------|-----------|
| 1 | `Managers/GameInitializer.cs` | isInitializing guard (MGR-C01), SubscribeToEvents unified (MGR-H02), isSubscribed note (MGR-H05) |
| 2 | `Managers/SceneLoader.cs` | Build Settings validation (MGR-C02), timeScale restore (MGR-C03), loading scene unload check (MGR-H03), previousTimeScale save (MGR-H04) |
| 3 | `Managers/GameManager.cs` | FindReferences uses ServiceLocator.GetOrFind (MGR-H01) |
| 4 | `Player/PlayerController.cs` | ICombatant implementation (PLR-H01), Revive healthPercent (PLR-H03) |
| 5 | `Player/SleepSystem.cs` | Recovery formula comment (M01), proportional HP (M02), delayed transition (M03), QuickSleep states (M04), auto-sleep cap (M05), long Qi (M06) |
| 6 | `Player/PlayerVisual.cs` | URP shader fix (M07), OnDestroy cleanup (L01), Flash guard (L02), Camera cache (L03) |
| 7 | `Save/SaveManager.cs` | PlayerSaveData.CurrentQi int→long (PLR-H02) |

---

## Задачи — Статус

### CRITICAL (Managers)
- [x] MGR-C01: GameInitializer — isInitializing flag, блокировка параллельного InitializeGameAsync
- [x] MGR-C02: SceneLoader — IsSceneInBuildSettings() validation перед LoadScene
- [x] MGR-C03: SceneLoader — Time.timeScale восстановление через previousTimeScale

### HIGH (Managers)
- [x] MGR-H01: GameManager.FindReferences — ServiceLocator.GetOrFind<T>
- [x] MGR-H02: GameInitializer — SubscribeToEvents() единый вызов в FinalSetup
- [x] MGR-H03: SceneLoader — проверка IsSceneValid перед UnloadSceneAsync
- [x] MGR-H04: SceneLoader — сохранение previousTimeScale вместо безусловного 1f
- [x] MGR-H05: GameInitializer — NOTE комментарий о isSubscribed guards

### HIGH (Player)
- [x] PLR-H01: PlayerController — ICombatant реализован (explicit interface, delegating to QiController/BodyController)
- [x] PLR-H02: PlayerSaveData — один класс в Save namespace, CurrentQi int→long
- [x] PLR-H03: PlayerController.Revive — healthPercent для восстановления

### MEDIUM (Player)
- [x] PLR-M01: SleepSystem — комментарий к HP recovery формуле
- [x] PLR-M02: SleepSystem.ProcessFinalHPRecovery — пропорциональное восстановление
- [x] PLR-M03: SleepSystem — TransitionToSleeping coroutine для FallingAsleep→Sleeping
- [x] PLR-M04: SleepSystem.QuickSleep — через SetState transitions
- [x] PLR-M05: SleepSystem — auto-sleep cap = maxSleepHours (12h)
- [x] PLR-M06: SleepSystem — long для Qi recovery
- [x] PLR-M07: PlayerVisual — Sprite-Lit-Default + Sprites/Default fallback

### LOW (Player)
- [x] PLR-L01: PlayerVisual — OnDestroy Destroy(Material/Texture2D)
- [x] PLR-L02: PlayerVisual — Flash coroutine guard (StopCoroutine перед новым)
- [x] PLR-L03: PlayerVisual — cachedCamera в Start()

---

*Чекпоинт обновлён: 2026-04-11*

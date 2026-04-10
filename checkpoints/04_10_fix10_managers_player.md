# Чекпоинт: Fix-10 — Managers + Player System

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** HIGH

---

## Описание

Managers: параллельная инициализация, SceneLoader баги, FindFirstObjectByType, заглушки. Player: ICombatant не реализован, SleepSystem сломана, PlayerVisual утечки.

---

## Файлы (6 файлов, ~2448 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `Managers/GameManager.cs` | 470 | FindFirstObjectByType→ServiceLocator, singleton stale refs, Input |
| 2 | `Managers/GameInitializer.cs` | 479 | isInitializing guard, SubscribeToEvents, isSubscribed check |
| 3 | `Managers/SceneLoader.cs` | 334 | GetSceneByName, timeScale restore, loading scene unload |
| 4 | `Player/PlayerController.cs` | 525 | ICombatant реализация, Revive healthPercent, OnDestroy отписки |
| 5 | `Player/SleepSystem.cs` | 377 | Recovery formulas, dead states, QuickSleep, auto-sleep cap |
| 6 | `Player/PlayerVisual.cs` | 263 | URP shader, Material/Texture2D leak, Camera.main |

---

## Задачи

### CRITICAL (Managers)
- [ ] MGR-C01: GameInitializer — добавить isInitializing flag, заблокировать параллельный InitializeGameAsync
- [ ] MGR-C02: SceneLoader — исправить GetSceneByName (проверять Build Settings, не загруженные)
- [ ] MGR-C03: SceneLoader — Time.timeScale восстановление в finally блоке

### HIGH (Managers)
- [ ] MGR-H01: GameManager.FindReferences — ServiceLocator.GetOrFind<T>
- [ ] MGR-H02: GameInitializer — вызывать SubscribeToEvents() вместо отдельных Subscribe методов
- [ ] MGR-H03: SceneLoader — исправить loading scene unload при Single-mode
- [ ] MGR-H04: SceneLoader — сохранить предыдущий timeScale, не безусловно 1f
- [ ] MGR-H05: GameInitializer — isSubscribed checks в unsubscribe methods

### HIGH (Player)
- [ ] PLR-H01: PlayerController — реализовать ICombatant (или наследовать CombatantBase)
- [ ] PLR-H02: Duplicate PlayerSaveData — объединить в SaveDataTypes.cs (один canonical класс)
- [ ] PLR-H03: PlayerController.Revive — использовать healthPercent параметр для восстановления

### MEDIUM (Player)
- [ ] PLR-M01: SleepSystem — корректная HP recovery формула (пропорционально duration)
- [ ] PLR-M02: SleepSystem.ProcessFinalHPRecovery — пропорциональное восстановление, не FullRestore
- [ ] PLR-M03: SleepSystem — активировать FallingAsleep/WakingUp states
- [ ] PLR-M04: SleepSystem.QuickSleep — пройти через state management
- [ ] PLR-M05: SleepSystem — auto-sleep cap = maxSleepHours (12h)
- [ ] PLR-M06: SleepSystem — long для Qi recovery (вместо int)
- [ ] PLR-M07: PlayerVisual — правильный URP shader name

### LOW (Player)
- [ ] PLR-L01: PlayerVisual — Material/Texture2D Dispose/Destroy
- [ ] PLR-L02: PlayerVisual — Flash coroutine guard (cancel previous before starting new)
- [ ] PLR-L03: PlayerVisual — Cache Camera reference (не Camera.main каждый кадр)

---

## Порядок выполнения

1. GameInitializer.cs — isInitializing + SubscribeToEvents
2. SceneLoader.cs — GetSceneByName + timeScale + loading scene
3. GameManager.cs — ServiceLocator + singleton + Input
4. PlayerController.cs — ICombatant + Revive + SaveData
5. SleepSystem.cs — все фиксы recovery
6. PlayerVisual.cs — shader + утечки + camera

---

## Зависимости

- **Предшествующие:** Fix-01 (Qi types), Fix-04 (GameEvents cleanup)
- **Последующие:** Fix-08 (Save — PlayerSaveData объединение)

---

*Чекпоинт обновлён: 2026-04-10 13:37:00 UTC*

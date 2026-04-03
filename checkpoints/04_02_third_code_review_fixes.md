# Checkpoint: Third Code Review - Critical Bug Fixes

**Дата:** 2026-04-02 15:15:00 UTC  
**Статус:** Complete

---

## Обзор

Третье код-ревью от двух агентов. Исправлено **8 критических/высоких проблем**.

---

## Исправленные проблемы

### 1. 🔴 TimeController.totalGameSeconds — неправильный подсчёт времени

**Проблема:** `totalGameSeconds++` — минута считалась как 1 секунда (ошибка 60x)

**Решение:**
```csharp
// ДО:
totalGameSeconds++;

// ПОСЛЕ:
totalGameSeconds += 60;  // FIX: минута = 60 секунд
```

**Файл:** `World/TimeController.cs` (v1.2)

---

### 2. 🔴 TimeController.time-of-day transition — неправильный порядок

**Проблема:** `oldTimeOfDay` вычислялся после инкремента `currentHour++`

**Решение:**
```csharp
// ДО:
currentHour++;
TimeOfDay oldTimeOfDay = CalculateTimeOfDay();  // Уже изменён!

// ПОСЛЕ:
TimeOfDay oldTimeOfDay = CalculateTimeOfDay();  // FIX: До мутации
currentHour++;
```

**Файл:** `World/TimeController.cs` (v1.2)

---

### 3. 🔴 DateTime serialization — JsonUtility не поддерживает DateTime

**Проблема:** Все `DateTime` поля терялись при сериализации

**Решение:** Заменены на `long Unix timestamp`:
- `GameSaveData.SaveTimeUnix`
- `SaveSlotInfo.SaveTimeUnix`
- `SaveMetadata.CreatedAtUnix`, `ModifiedAtUnix`
- `AchievementSaveData.UnlockTimeUnix`
- `QuestSaveData.StartTimeUnix`, `CompletionTimeUnix`
- `SaveChecksum.GeneratedAtUnix`
- `SaveBackup.BackupTimeUnix`

Добавлены helper-методы `GetXXXTime()` для конвертации в DateTime.

**Файлы:** `Save/SaveManager.cs` (v1.1), `Save/SaveDataTypes.cs` (v1.1)

---

### 4. 🔴 SaveManager.PlayTimeSeconds — сброс при перезапуске

**Проблема:** `Time.realtimeSinceStartup` сбрасывается при перезапуске приложения

**Решение:**
```csharp
// ДО:
saveData.PlayTimeSeconds = Time.realtimeSinceStartup;

// ПОСЛЕ:
saveData.TotalPlayTimeHours = (float)(timeController.TotalGameSeconds / 3600.0);
```

Теперь время накапливается через `TimeController.TotalGameSeconds`.

**Файл:** `Save/SaveManager.cs` (v1.1)

---

### 5. 🔴 GameInitializer event subscriptions — cascading reinitialization

**Проблема:**
- Lambdas без отписки → утечка памяти
- `Reinitialize()` в `OnGameLoaded` → бесконечный цикл

**Решение:**
- Все lambdas заменены на named methods
- Добавлен `UnsubscribeFromEvents()` в `OnDestroy`
- Убран `Reinitialize()` из `HandleGameLoaded()`
- Добавлен флаг `isSubscribed` для защиты

**Файл:** `Managers/GameInitializer.cs` (v1.1)

---

### 6. 🔴 GameManager start vs resume — неправильная логика

**Проблема:** `currentState` обновлялся ДО вызова `HandlePlaying()`

**Решение:**
```csharp
// ДО:
currentState = newState;
HandlePlaying();  // currentState уже == Playing

// ПОСЛЕ:
HandlePlaying(oldState);  // FIX: Передаём oldState
currentState = newState;  // Обновляем после
```

**Файл:** `Managers/GameManager.cs` (v1.1)

---

### 7. 🔴 UIManager initial state — hidden UI on first launch

**Проблема:** `currentState = MainMenu`, `SetState(MainMenu)` return early

**Решение:**
```csharp
// ДО:
private GameState currentState = GameState.MainMenu;

// ПОСЛЕ:
private GameState currentState = GameState.None;  // Sentinel
private bool hasInitialized = false;

private void Start() {
    ForceInitialSync();  // Показываем MainMenu
}
```

**Файл:** `UI/UIManager.cs` (v1.1)

---

### 8. 🟡 CombatManager null guards — NullReferenceException risk

**Проблема:** Нет проверки `attacker` и `defender` на null

**Решение:** Добавлены null guards:
```csharp
if (attacker == null)
    return AttackResult.Failed("Attacker is null");
if (defender == null)
    return AttackResult.Failed("Defender is null");
```

**Файл:** `Combat/CombatManager.cs` (v1.2)

---

## Изменённые файлы

| Файл | Версия | Изменения |
|------|--------|-----------|
| `World/TimeController.cs` | 1.1 → 1.2 | totalGameSeconds, time-of-day |
| `Save/SaveManager.cs` | 1.0 → 1.1 | Unix timestamp, PlayTime |
| `Save/SaveDataTypes.cs` | 1.0 → 1.1 | DateTime → Unix |
| `Managers/GameInitializer.cs` | 1.0 → 1.1 | Named events, unsubscribe |
| `Managers/GameManager.cs` | 1.0 → 1.1 | oldState, unsubscribe |
| `UI/UIManager.cs` | 1.0 → 1.1 | Sentinel state |
| `Combat/CombatManager.cs` | 1.1 → 1.2 | Null guards |

---

## Статистика

- **Файлов изменено:** 7
- **Строк добавлено:** ~150
- **Строк изменено:** ~80
- **Критических багов:** 7 🔴
- **Средних багов:** 1 🟡

---

## Следующие шаги

1. Тестирование сохранений/загрузки
2. Проверка time-of-day transitions в игре
3. Тестирование UI initial state

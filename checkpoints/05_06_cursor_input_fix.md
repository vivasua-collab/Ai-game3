# Чекпоинт: Исправление перехвата мыши и ввода в инвентаре

**Дата:** 2026-05-06  
**Автор:** AI Assistant  
**Связанный аудит:** `05_05_inventory_audit.md`

---

## Проблема

Пользователь сообщил: **«ПОЧЕМУ ПРОИСХОДИТ ПОЛНЫЙ ПЕРЕХВАТ МЫШИ, Я НЕ МОГУ НАЖАТЬ КНОПКУ DEBUG»**

## Корневая причина

`InventoryScreen.Close()` устанавливал `Cursor.lockState = Locked` и `Cursor.visible = false`, **полностью блокируя курсор**. При этом:

1. **Нигде в UIManager не было управления курсором** — после закрытия инвентаря курсор оставался заблокированным навсегда
2. **`InventoryScreen.Open()` никогда не вызывался UIManager'ом** — он только делал `SetActive(true)`, который вызывает `OnEnable()`, но не `Open()` с разблокировкой
3. **Для 2D top-down игры курсор НЕ должен блокироваться** — игроку нужно кликать по NPC, предметам, кнопке DEBUG

## Исправления

### ФИКС-1: Централизованное управление курсором в UIManager

**Файл:** `Assets/Scripts/UI/UIManager.cs`

- Добавлен метод `UpdateCursorState(GameState state)` — курсор ВСЕГДА свободен и видим
- Вызывается из `UpdatePanels()` при каждой смене состояния
- Вызывается из `ForceInitialSync()` при старте (разблокировка для MainMenu)
- Убрана привязка к конкретным состояниям — в 2D top-down игре курсор всегда нужен

### ФИКС-2: Убрано управление курсором из InventoryScreen

**Файл:** `Assets/Scripts/UI/Inventory/InventoryScreen.cs`

- Убран `Cursor.lockState = Locked; Cursor.visible = false;` из `Close()`
- Убран `Cursor.lockState = None; Cursor.visible = true;` из `Open()`
- Управление курсором теперь полностью в UIManager

### ФИКС-3: Убрана двойная обработка ESC

**Файл:** `Assets/Scripts/UI/Inventory/InventoryScreen.cs`

- **Критический баг:** и `InventoryScreen.HandleInput()`, и `UIManager.HandleInput()` обрабатывали ESC
- Это вызывало **двойной `ReturnToPrevious()`** — прыжок через 2 состояния вместо 1
- Аналогично ранее исправленному багу с клавишей I (2026-04-25)
- Решение: убрана обработка ESC из InventoryScreen, UIManager — единственный обработчик

### ФИКС-4: Явный обработчик GameState.Inventory в UIManager

**Файл:** `Assets/Scripts/UI/UIManager.cs`

- Добавлен `case GameState.Inventory:` в `HandleEscape()` для ясности
- Раньше попадал в `default:` — работало, но неявно

### ФИКС-5: ContextMenuUI — миграция на Input System

**Файл:** `Assets/Scripts/UI/ContextMenuUI.cs`

- Заменена старая Input система (`Input.GetMouseButtonDown`, `Input.mousePosition`) на Input System (`Mouse.current`)
- Проект использует Input System package — старая система может быть отключена

### ФИКС-6: NullRef guard в InventoryController

**Файл:** `Assets/Scripts/Inventory/InventoryController.cs`

- `RecalculateTotals()` — добавлена проверка `slot.ItemData == null` (БАГ-ИНВ-47)
- `RemoveFromSlot()` — добавлена проверка `slot.ItemData == null` перед доступом к weight/volume
- `AddItem()` — добавлена проверка в цикле заполнения стаков
- `GetSaveData()` — добавлена проверка при сериализации

---

## Подтверждённые исправления из предыдущего аудита

| Баг | Статус | Примечание |
|-----|--------|------------|
| БАГ-ИНВ-01: CreateRowUI без SetActive(true) | ✅ Исправлен ранее | `rowGO.SetActive(true)` на строке 154 |
| БАГ-ИНВ-03: GameEvents.OnItemAdded не вызывается | ✅ Исправлен ранее | `GameEvents.TriggerItemAdded()` на строке 369 |
| БАГ-ИНВ-47: NullRef при null ItemData | ✅ Исправлен сейчас | 4 метода с guard |

---

## Изменённые файлы

1. `Assets/Scripts/UI/UIManager.cs` — курсор, ESC
2. `Assets/Scripts/UI/Inventory/InventoryScreen.cs` — курсор, ESC
3. `Assets/Scripts/UI/ContextMenuUI.cs` — Input System
4. `Assets/Scripts/Inventory/InventoryController.cs` — NullRef guards

# Чекпоинт: Исправление открытия инвентаря по клавише I + GlobalLight2D
# Дата: 2026-04-25 10:18:49 UTC (создан) / 10:40:00 UTC (обновлён)
# Статус: complete

## Проблема
1. Клавиша "I" не открывает инвентарь в runtime
2. GlobalLight2D не создаётся в сцену (спрайты чёрные)

## Диагностика — найдено 6 багов

### БАГ #0: GlobalLight2D не создаётся (reflection fallback)
- Phase04 ищет Light2D через одну сборку Unity.2D.RenderPipeline.Runtime
- В Unity 6.3 тип может быть в другой сборке
- **Статус: ✅ ИСПРАВЛЕН** — Добавлен fallback: 3 варианта сборок + поиск по всем AppDomain

### БАГ #1 (КРИТИЧЕСКИЙ): UIManager НИКОГДА не добавляется в сцену
- **Статус: ✅ ИСПРАВЛЕН** — Phase07UI теперь добавляет UIManager + MenuUI + HUDController

### БАГ #2 (КРИТИЧЕСКИЙ): SerializedField ссылки UIManager не подключены
- **Статус: ✅ ИСПРАВЛЕН** — Phase07 подключает mainCanvas, hudPanel, mainMenu, pauseMenu; Phase17 подключает inventoryPanel, inventoryScreen + все внутренние ссылки InventoryScreen

### БАГ #3 (КРИТИЧЕСКИЙ): GameInitializer не синхронизирует UIManager state
- **Статус: ✅ ИСПРАВЛЕН** — GameInitializer.InitializeGameAsync() теперь вызывает uiManager.StartNewGame() после установки GameManager.Playing

### БАГ #4 (СРЕДНИЙ): Двойная обработка клавиши I
- **Статус: ✅ ИСПРАВЛЕН** — Убрана обработка I из InventoryScreen.HandleInput(), UIManager — единственный обработчик

### БАГ #5 (НИЗКИЙ): FindFirstObjectByType вместо UIManager.Instance
- **Статус: ✅ ИСПРАВЛЕН** — InventoryScreen теперь использует UIManager.Instance

## Изменённые файлы
1. `Editor/SceneBuilder/Phase04CameraLight.cs` — GlobalLight2D reflection fallback
2. `Editor/SceneBuilder/Phase07UI.cs` — UIManager + MenuUI + HUDController + wiring
3. `Editor/SceneBuilder/Phase17InventoryUI.cs` — InventoryScreen → UIManager wiring + внутренние ссылки
4. `Managers/GameInitializer.cs` — UIManager state sync (StartNewGame)
5. `UI/Inventory/InventoryScreen.cs` — Убрана обработка I, UIManager.Instance

## Что нужно проверить на локальном ПК
1. Удалить старую сцену (или пересоздать через Build All)
2. Запустить Build All (Tools > Full Scene Builder > Build All)
3. Нажать Play
4. Нажать I — инвентарь должен открыться
5. Нажать I ещё раз — инвентарь должен закрыться
6. Нажать ESC — пауза, ESC ещё раз — продолжить
7. Проверить: GlobalLight2D создан (в консоли нет [RPL]-L8 ❌)

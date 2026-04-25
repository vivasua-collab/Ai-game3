# Чекпоинт: Исправление открытия инвентаря по клавише I
# Дата: 2026-04-25 10:18:49 UTC
# Статус: in_progress

## Проблема
Клавиша "I" не открывает инвентарь в runtime (после генерации сцены и нажатия Play).

## Диагностика — найдено 5 багов

### БАГ #1 (КРИТИЧЕСКИЙ): UIManager НИКОГДА не добавляется в сцену SceneBuilder-ом
- Phase07 создаёт Canvas "GameUI" + HUD + EventSystem, но НЕ добавляет UIManager
- Без UIManager нет обработки клавиши I, нет управления панелями, нет стейт-машины
- **Статус: не исправлен**

### БАГ #2 (КРИТИЧЕСКИЙ): SerializedField ссылки UIManager не подключены
- UIManager имеет [SerializeField] inventoryPanel, inventoryScreen, hudPanel, pauseMenu, mainMenu, dialogPanel, characterPanel, mapPanel
- Ни одна фаза SceneBuilder не назначает эти ссылки
- Даже если UIManager будет добавлен, все панели == null → ничего не отображается
- **Статус: не исправлен**

### БАГ #3 (КРИТИЧЕСКИЙ): GameInitializer не синхронизирует состояние UIManager
- GameInitializer.InitializeGameAsync() → GameManager.Instance.SetState(GameState.Playing)
- Но UIManager.currentState остаётся MainMenu (из ForceInitialSync)
- ToggleInventory() работает ТОЛЬКО из Playing/Inventory → клавиша I игнорируется
- **Статус: не исправлен**

### БАГ #4 (СРЕДНИЙ): Двойная обработка клавиши I
- UIManager.HandleInput() обрабатывает I → ToggleInventory()
- InventoryScreen.HandleInput() тоже обрабатывает I → Close() + uiManager.ToggleInventory()
- Результат: двойной toggle или конфликт SetActive
- **Статус: не исправлен**

### БАГ #5 (НИЗКИЙ): InventoryScreen ищет UIManager через FindFirstObjectByType вместо Instance
- InventoryScreen.HandleInput() использует FindFirstObjectByType<CultivationGame.UI.UIManager>()
- UIManager — singleton с Instance, нужно использовать UIManager.Instance
- **Статус: не исправлен**

## Изменённые файлы
(пока нет)

## План исправлений
(см. ниже в чате)

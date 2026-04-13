# Чекпоинт: Исправление чёрного экрана и отсутствия движения

**Дата:** 2026-04-13 12:17:09 UTC  
**Статус:** complete

## Проблемы

При нажатии Play после генерации сцены:
1. Чёрный экран
2. Движение игрока не работает
3. InvalidOperationException от legacy Input

## Корневые причины

### 1. GameInitializer deadlock (БЛОКЕР)
`UIManager.Start()` ставит `Time.timeScale = 0` (состояние MainMenu).  
`GameInitializer.InitializeGameAsync()` использует `WaitForSeconds(0.1f)`, который зависит от timeScale.  
Результат: корутина зависает навсегда, игра никогда не переходит в Playing.

**FIX:** `WaitForSeconds` → `WaitForSecondsRealtime` (не зависит от timeScale)

### 2. Input.mousePosition (3 вызова, БЛОКЕР)
Проект использует Input System Package, но 3 файла ещё вызывают legacy `Input.mousePosition`:
- FormationUI.cs:486
- InventoryUI.cs:330
- CharacterSpriteController.cs:204

**FIX:** `Input.mousePosition` → `Mouse.current.position.value`

### 3. Невидимый игрок
SpriteRenderer создан, но sprite = null (без спрайта объект невидим).

**FIX:** Добавлен `CreatePlayerSprite()` — процедурная генерация фигурки персонажа.

### 4. Чёрный фон
Camera.backgroundColor = Color.black, нет тайлов в Tilemap.

**FIX:** Цвет фона изменён на тёмно-синий `new Color(0.05f, 0.07f, 0.12f)`.

## Изменённые файлы

| Файл | Изменение |
|------|-----------|
| `Assets/Scripts/Managers/GameInitializer.cs` | WaitForSeconds → WaitForSecondsRealtime |
| `Assets/Scripts/Formation/FormationUI.cs` | Input.mousePosition → Mouse.current.position |
| `Assets/Scripts/UI/InventoryUI.cs` | Input.mousePosition → Mouse.current.position |
| `Assets/Scripts/Character/CharacterSpriteController.cs` | Input.mousePosition → Mouse.current.position + using InputSystem |
| `Assets/Scripts/Editor/FullSceneBuilder.cs` | Camera bg цвет + CreatePlayerSprite() |

## Что нужно сделать руками в Unity

После компиляции:
1. **Удалить старую сцену Main.unity** (если Player и Camera уже созданы без спрайта/с нужным фоном)
2. **Запустить Tools → Full Scene Builder → Build All** — пересоздаст сцену с исправлениями
3. Нажать Play — теперь:
   - Фон тёмно-синий (не чёрный)
   - Персонаж виден (голубая фигурка)
   - WASD/стрелки двигают персонажа
   - Нет InvalidOperationException

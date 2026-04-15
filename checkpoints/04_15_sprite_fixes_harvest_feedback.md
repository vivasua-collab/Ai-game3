# Чекпоинт: Исправление спрайтов + спрайт игрока + обратная связь добычи

**Дата:** 2026-04-15 08:00:00 UTC
**Фаза:** Визуальные исправления + UX
**Статус:** in_progress

## Анализ проблем

### Проблема 1: Зазоры между тайлами ✅ ЧАСТИЧНО
**Симптом:** Белые/тёмные линии между тайлами на карте
**Причина:** AI-спрайты (64x64) импортируются Unity с дефолтными настройками (PPU=100).
TileSpriteGenerator.cs устанавливает PPU=32 только для спрайтов, которые он сам генерирует.
AI-спрайты созданы через Python resize, Unity создала .meta с PPU=100.
**Что сделано:**
- [x] TileSpriteGenerator.SaveTexture: добавлен `spriteBorder = new Vector4(1,1,1,1)` + `wrapMode = Clamp`
**Что осталось:**
- [ ] FullSceneBuilder Phase 14: добавить метод ReimportTileSprites() — переимпорт ВСЕХ PNG из Assets/Sprites/Tiles/ с PPU=32, spriteBorder(1,1,1,1), filterMode=Point, wrapMode=Clamp
- [ ] Вызвать ReimportTileSprites() в начале ExecuteCreateTileAssets() ДО создания tile assets

### Проблема 2: Нет спрайта у персонажа ✅ СДЕЛАНО
**Симптом:** Персонаж — маленькая точка (PPU=64, половина тайла)
**Что сделано:**
- [x] FullSceneBuilder.CreatePlayerSprite: загрузка из Assets/Sprites/Characters/Player/
- [x] Fallback CreateProceduralPlayerSprite: PPU=32 вместо 64
- [x] При загрузке — реимпорт с PPU=32

### Проблема 3: Сцены удалены с GitHub ✅ СДЕЛАНО
- [x] git pull подтянул удаление 3 файлов сцен (коммит 6e11fa9)

### Проблема 4: Нет визуальной обратной связи при добыче (F) ❌ НЕ НАЧАТО
**Симптом:** Игрок нажимает F для добычи ресурса — нет индикации
**План:**
- [ ] Создать HarvestFeedbackUI.cs — WorldSpace канвас с прогресс-баром + текст
- [ ] Обновить PlayerController.cs — добавить F-key обработку:
  - Определение ближайшего разрушаемого объекта
  - Вызов DamageObjectAtTile через DestructibleObjectController
  - Создание визуальной обратной связи
- [ ] Обновить DestructibleObjectController.cs — добавить событие OnHarvestStarted

## ПЛАН ВЫПОЛНЕНИЯ

### Шаг 1: Добавить ReimportTileSprites() в FullSceneBuilder
- Новый метод, который проходит по всем PNG в Assets/Sprites/Tiles/
- Устанавливает PPU=32, spriteBorder(1,1,1,1), filterMode=Point, wrapMode=Clamp
- Вызвать в начале ExecuteCreateTileAssets()

### Шаг 2: Создать HarvestFeedbackUI.cs
- WorldSpace Canvas с прогресс-баром и текстом "Добываю..."
- Плавающий над объектом добычи
- Цветовая индикация: жёлтый → зелёный
- Автоматическое уничтожение после завершения

### Шаг 3: Обновить PlayerController.cs
- Добавить F-key обработку в ProcessInput()
- Raycast/OverlapCircle для поиска ближайшего разрушаемого объекта
- Вызов DestructibleObjectController.DamageObjectAtWorld()
- Создание HarvestFeedbackUI для визуальной обратной связи

### Шаг 4: Обновить DestructibleObjectController.cs
- Добавить событие OnHarvestStarted
- Вызывать при начале добычи

### Шаг 5: Push в main

## Изменённые файлы
- [x] UnityProject/Assets/Scripts/Tile/Editor/TileSpriteGenerator.cs — spriteBorder + wrapMode
- [x] UnityProject/Assets/Scripts/Editor/FullSceneBuilder.cs — CreatePlayerSprite + PPU=32
- [ ] UnityProject/Assets/Scripts/Editor/FullSceneBuilder.cs — ReimportTileSprites()
- [ ] UnityProject/Assets/Scripts/UI/HarvestFeedbackUI.cs — НОВЫЙ
- [ ] UnityProject/Assets/Scripts/Player/PlayerController.cs — F-key harvest
- [ ] UnityProject/Assets/Scripts/Tile/DestructibleObjectController.cs — OnHarvestStarted

# FullSceneBuilder — Инкрементальный One-Click Builder

**Дата:** 2026-04-13  
**Тип:** Feature

## Что сделано

Создан `UnityProject/Assets/Scripts/Editor/FullSceneBuilder.cs` — единый Builder для автосборки сцены.

### Архитектура

13 идемпотентных фаз. Каждая фаза:
1. Проверяет нужно ли действие (`IsPhaseNeeded`)
2. Выполняет (`ExecutePhase`)
3. Логирует результат

### Фазы

| # | Фаза | Что делает |
|---|------|------------|
| 01 | Folders | Создаёт 33 папки в Assets/ |
| 02 | Tags & Layers | Настраивает 5 тегов + 7 слоёв через TagManager |
| 03 | Create Scene | Создаёт Main.unity через EditorSceneManager |
| 04 | Camera & Light | Orthographic камера + Directional Light |
| 05 | GameManager | GameManager + GameInitializer + Systems (8 контроллеров) |
| 06 | Player | Player с 8 компонентами + настройки через SerializedObject |
| 07 | UI | Canvas + HUD (4 бара + тексты) + EventSystem + InputSystemUIInputModule |
| 08 | Tilemap | Grid + Terrain/Objects Tilemaps + TileMapController + TestLocationGameController |
| 09 | Generate Assets | Вызов AssetGenerator + AssetGeneratorExtended + FormationAssetGenerator + валидация |
| 10 | Tile Sprites | Вызов TileSpriteGenerator (13 тайлов) |
| 11 | UI Prefabs | Вызов FormationUIPrefabsGenerator (3 префаба) |
| 12 | TMP Essentials | Проверка/импорт TextMeshPro |
| 13 | Save Scene | Сохранение сцены |

### Меню Unity

- `Tools → Full Scene Builder → Build All (One Click)` — все фазы разом
- `Tools → Full Scene Builder → Phase XX: Name` — отдельная фаза

### Инкрементальность

- Повторный запуск пропускает выполненные фазы
- Ошибка в фазе не прерывает остальные (спрашивает: продолжить?)
- Можно запускать фазы в любом порядке

### Существующая документация

Не затронута. Документы в `docs_asset_setup/` для ручной/полуручной сборки остаются актуальными.

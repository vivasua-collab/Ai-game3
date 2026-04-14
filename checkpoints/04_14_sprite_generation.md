# Чекпоинт: Генерация PNG-спрайтов и интеграция в автогенератор

**Дата:** 2026-04-14 14:11:00 UTC
**Фаза:** Спрайты и интеграция
**Статус:** in_progress

## Проблема
PNG-спрайты тайлов (terrain_grass, terrain_snow, obj_tree и т.д.) НЕ существуют как файлы в `Assets/Sprites/Tiles/`. Tile .asset файлы также отсутствуют в `Assets/Tiles/`. Спрайты не применяются к объектам.

## ПЛАН (до выполнения)

### Этап 1: Генерация PNG-спрайтов [ ]
- Создать Python-скрипт для генерации всех PNG спрайтов
- Terrain: grass, dirt, stone, water_shallow, water_deep, sand, snow, ice, lava, void
- Objects: tree, rock_small, rock_medium, bush, chest, ore_vein, herb
- Спрайты 64×64px с PerlinNoise вариацией (как в TileSpriteGenerator.cs)
- Сохранить в `UnityProject/Assets/Sprites/Tiles/`

### Этап 2: Доработка FullSceneBuilder [ ]
- Проверить что Phase 10 вызывает TileSpriteGenerator
- Проверить что Phase 14 корректно создаёт .asset и назначает спрайты
- Проверить что AssignTileBasesToController назначает ВСЕ поля (включая snowTile)
- Убедиться что Build All корректно вызывает все фазы последовательно

### Этап 3: Интеграция TileMapController [ ]
- Проверить что все поля TileBase (snowTile, iceTile, lavaTile, oreVeinTile, herbTile) назначаются
- Проверить GetTerrainTile() для всех TerrainType
- Проверить GetObjectTile() для всех TileObjectType

### Этап 4: Коммит и PR [ ]
- Создать feature branch
- Закоммитить PNG-спрайты + правки
- Создать Pull Request

## Изменённые файлы (планируемые)
- UnityProject/Assets/Sprites/Tiles/*.png (новые)
- UnityProject/Assets/Scripts/Editor/FullSceneBuilder.cs (возможно)
- UnityProject/Assets/Scripts/Tile/TileMapController.cs (возможно)

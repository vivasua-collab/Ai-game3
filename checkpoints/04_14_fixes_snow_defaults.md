# Чекпоинт: Snow Tile + Default Map Size

**Дата:** 2026-04-14 13:44:06 UTC
**Фаза:** Пост-инструкционные исправления
**Статус:** in_progress

## Контекст
Все основные изменения из инструкции (баги, расширение карты 100×80, спрайты, FullSceneBuilder) уже применены в коммите 38fa7ba. Однако при сверке обнаружены 6 пропущенных моментов.

## Текущие задачи (ПЛАН)
- [ ] TileMapController.cs: `defaultWidth = 80` → `100`
- [ ] TileMapController.cs: `defaultHeight = 60` → `80`
- [ ] TileMapController.cs: добавить поле `[SerializeField] private TileBase snowTile;`
- [ ] TileMapController.cs: `GetTerrainTile()` — добавить `TerrainType.Snow => snowTile`
- [ ] FullSceneBuilder.cs: Phase 14 — добавить `CreateTerrainTileAsset("Tile_Snow", "terrain_snow", TerrainType.Snow, 1.3f, true, GameTileFlags.Passable)`
- [ ] FullSceneBuilder.cs: `AssignTileBasesToController()` — добавить `AssignTileProperty(so, "snowTile", "Assets/Tiles/Terrain/Tile_Snow.asset")`

## Выполненные задачи
(пока нет — план записан до выполнения)

## Изменённые файлы (планируемые)
- Assets/Scripts/Tile/TileMapController.cs
- Assets/Scripts/Editor/FullSceneBuilder.cs

## GitHub: через Pull Request
Ветка: `fix/snow-tile-defaults` → `main`

## Примечания
- TerrainType.Snow (enum value=7) существует в TileEnums.cs, но TileMapController не имеет соответствующего TileBase-поля
- terrain_snow спрайт уже генерируется в TileSpriteGenerator.cs (строка 39)
- FullSceneBuilder Phase 14 создаёт Tile_Ice и Tile_Lava, но пропущен Tile_Snow

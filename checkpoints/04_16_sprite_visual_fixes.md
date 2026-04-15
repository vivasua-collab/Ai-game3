# Чекпоинт: Исправление визуальных проблем спрайтов

**Дата:** 2026-04-15 12:58:44 UTC
**Статус:** in_progress

## Проблемы (от пользователя)
1. Белая сетка между спрайтами ландшафта (неполное перекрытие тайлов)
2. Спрайты объектов — белый фон (без прозрачности), слишком большие — перекрывают несколько клеток
3. Спрайт игрока не прорисовывается
4. Не у всех элементов сцены есть спрайты — цветные точки

## Корневые причины
1. **Белая сетка**: cellGap=-0.01 не помогал. Terrain спрайты 64px/PPU32 = точно 2.0 юнита = нет перекрытия. Sub-pixel артефакты.
2. **Белый фон объектов**: alphaIsTransparency=true установлен, но `TextureImporter.sprites` удалён в Unity 6.3 → ошибка CS1061 → спрайты не импортировались корректно.
3. **Слишком большие объекты**: PPU=32 → 64px/32 = 2.0 юнита = целый тайл. Объекты должны быть в 5 раз меньше.
4. **Игрок**: SpriteRenderer создаётся в PlayerVisual.cs, но при PPU=32 и scale=0.8 = 1.6 юнита — должен быть виден. Возможно проблема в Z-позиции или sortingOrder.

## Выполненные изменения

### 1. TileSpriteGenerator.cs
- Terrain: 64×64 → **68×68 px** (pixel bleed +2px с каждой стороны)
- Terrain PPU=32 → **68/32 = 2.125 юнита** (перекрытие 0.0625u устраняет белую сетку)
- Objects PPU=32 → **PPU=160** (64/160 = 0.4 юнита, в 5 раз меньше ячейки)
- Все текстуры RGBA32 с filterMode=Point
- textureCompression=Uncompressed (без артефактов сжатия)
- SpriteImportMode.Single (TextureImporter.sprites удалён в Unity 6.3)

### 2. FullSceneBuilder.cs
- Grid cellGap: (-0.01,-0.01,0) → **Vector3.zero** (pixel bleed заменяет)
- TilemapRenderer.mode = **Individual** (устраняет артефакты чанков)
- ReimportTileSprites: сканирует **обе** директории (Tiles/ и Tiles_AI/)
- ReimportTileSprites: PPU terrain=32, objects=160

### 3. TileMapController.cs
- cellGap → Vector3.zero (в Awake и EnsureTileAssets)
- CreateProceduralTileSprite: terrain 68×68 PPU=32, objects 64×64 PPU=160
- Object shapes уменьшены пропорционально

### 4. ResourceSpawner.cs
- spriteScale: 0.8 → **0.16** (в 5 раз меньше)
- spriteSize: 48 → **64**
- CreateResourceSprite PPU: 32 → **160**

### 5. PlayerVisual.cs
- size: 0.8 → **0.4** (персонаж = 0.8 юнита при display)

### 6. TestLocationSetup.cs
- cellGap → Vector3.zero
- TilemapRenderer.mode = Individual

### 7. Удалены старые PNG
- Assets/Sprites/Tiles/*.png — удалены (будут регенерированы)

## ОШИБКА ИСПРАВЛЕНА
- ReimportTileSprites был сломан (дублирование переменных, spritesDir вне scope) — полностью переписан

## Изменённые файлы
- `Assets/Scripts/Tile/Editor/TileSpriteGenerator.cs`
- `Assets/Scripts/Editor/FullSceneBuilder.cs`
- `Assets/Scripts/Tile/TileMapController.cs`
- `Assets/Scripts/Tile/ResourceSpawner.cs`
- `Assets/Scripts/Player/PlayerVisual.cs`
- `Assets/Scripts/Tile/Editor/TestLocationSetup.cs`

## Что осталось сделать
- [ ] Проверить компиляцию (нет ли других ошибок)
- [ ] Git commit + push
- [ ] Проверить результат в Unity (пользователь проверяет)

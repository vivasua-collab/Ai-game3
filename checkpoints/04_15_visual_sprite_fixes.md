# Чекпоинт: Исправление визуальных проблем со спрайтами
# Дата: 2026-04-15 11:52:51 UTC
# Статус: in_progress

## Задачи:
1. Белая сетка между спрайтами локации — неполное перекрытие тайлов
2. Спрайты объектов — белый фон (без прозрачности), слишком большие
3. Спрайт игрока не прорисовывается совсем
4. Не для всех элементов сцены есть спрайты — цветные точки
5. Исправлена ошибка компиляции CS1061 (spriteAlignment)

## Корневые причины:
1. **Белая сетка**: Процедурные terrain-спрайты используют Rect(1,1,64,64) → ровно 2.0 юнита при PPU=32. Sub-pixel рендеринг создаёт зазоры. Нужен полный 66×66 rect для pixel bleed.
2. **Белый фон объектов**: TileSpriteGenerator.SaveTexture() падал на CS1061 (spriteAlignment не существует в Unity 6.3). PNG не реимпортировались с alphaIsTransparency=true.
3. **Игрок без спрайта**: FullSceneBuilder создаёт SpriteRenderer + PlayerVisual добавляет ДРУГОЙ SpriteRenderer на дочерний объект. Конфликт.
4. **Цветные точки**: Некоторые TileObjectType мапятся в null в GetObjectTile() → Tilemap рисует пустую клетку. ResourceSpawner создаёт маленькие круги (24px при scale=0.6).

## Изменённые файлы:
- TileSpriteGenerator.cs — исправлен CS1061, добавлен spriteRect через TextureImporterSettings
- TileMapController.cs — Rect(0,0,66,66) для terrain, исправлена прозрачность объектов
- FullSceneBuilder.cs — PlayerVisual не добавляется, исправлен spriteRect в ReimportTileSprites
- PlayerVisual.cs — совместимость с FullSceneBuilder SpriteRenderer

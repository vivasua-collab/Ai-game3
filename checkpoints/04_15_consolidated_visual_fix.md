# Чекпоинт: Консолидированное исправление визуальных проблем

**Дата:** 2026-04-15 17:51:32 UTC
**Статус:** in_progress
**Основание:** Анализ 8 чекпоинтов за 04_15

---

## КОНСОЛИДИРОВАННЫЙ АНАЛИЗ ВСЕХ ИТЕРАЦИЙ

### Что УЖЕ СДЕЛАНО (работает):
- ✅ Light2D в сцене (FullSceneBuilder Phase 04)
- ✅ Позиция игрока на центре карты (100, 80, 0)
- ✅ Unlit шейдер для PlayerVisual и ResourceSpawner
- ✅ Устранение задвоения спрайта (TestLocationGameController проверяет существующего Player)
- ✅ Tiles_AI/ убран из поиска в TileMapController и ResourceSpawner
- ✅ ResourceLogger.cs создан
- ✅ ItemData fallback в ResourcePickup
- ✅ Спрайт игрока обработан: 128×128 RGBA (было 1024×1024 RGB)
- ✅ AI obj спрайты обработаны: 64×64 RGBA в Tiles/
- ✅ TileSpriteGenerator v3: terrain процедурные 68×68 PPU=32 Bilinear
- ✅ TilemapRenderer.mode = Chunk

### КОРНЕВЫЕ ПРИЧИНЫ ОСТАВШИХСЯ ПРОБЛЕМ:

#### Проблема 1: Белая сетка между terrain-тайлами
**Причина:** `FullSceneBuilder.ReimportTileSprites()` ПЕРЕЗАПИСЫВАЕТ правильные настройки:
- TileSpriteGenerator: terrain PPU=32, Bilinear → ReimportTileSprites: PPU=31, Point
- ReimportTileSprites сканирует Tiles_AI/ (1024×1024 RGB → PPU=31 = 32 юнита!)
- Phase 14 вызывает ReimportTileSprites ПОСЛЕ TileSpriteGenerator → неправильные настройки побеждают

**Исправление:** Синхронизировать ReimportTileSprites с TileSpriteGenerator:
- PPU=32 (не 31) для terrain
- Bilinear (не Point) для terrain
- НЕ сканировать Tiles_AI/

#### Проблема 2: Спрайт игрока — белый фон
**Причина:** Файл 128×128 RGBA (обработан), но Unity import settings могут быть неправильными.
PlayerVisual.EnsurePlayerSpritePPU() должен исправить, но может быть race condition.

**Исправление:** Усилить EnsurePlayerSpritePPU — принудительный реимпорт с ForceUpdate

#### Проблема 3: Ресурсы слишком мелкие
**Причина:** AI obj спрайты PPU=160 → 64/160 = 0.4 юнита при scale=1.0
Fallback процедурные PPU=32 → 64/32 = 2.0 юнита при scale=1.0
Несогласованность: AI = 0.4u, fallback = 2.0u

**Исправление:** Масштаб ресурсов по PPU загруженного спрайта:
- AI спрайт (PPU=160): scale = 5.0 → 0.4 × 5.0 = 2.0 юнита
- Fallback (PPU=32): scale = 1.0 → 2.0 × 1.0 = 2.0 юнита

---

## ПЛАН ИСПРАВЛЕНИЙ

### Fix 1: FullSceneBuilder.ReimportTileSprites()
- PPU terrain: 31 → 32 (совпадает с TileSpriteGenerator)
- Filter terrain: Point → Bilinear (совпадает с TileSpriteGenerator)
- Убрать Tiles_AI/ из сканирования

### Fix 2: PlayerVisual.EnsurePlayerSpritePPU()
- Добавить ForceUpdate + Refresh для гарантии реимпорта

### Fix 3: ResourceSpawner — масштаб ресурсов
- Вычислять scale на основе PPU загруженного спрайта
- Целевой размер = 2.0 юнита (1 тайл)
- scale = targetWorldSize / spriteWorldSize

---

## ИЗМЕНЯЕМЫЕ ФАЙЛЫ

| Файл | Изменение |
|------|-----------|
| FullSceneBuilder.cs | ReimportTileSprites: PPU=32, Bilinear, без Tiles_AI/ |
| PlayerVisual.cs | EnsurePlayerSpritePPU: принудительный реимпорт |
| ResourceSpawner.cs | Динамический scale по PPU спрайта |

---

*Создано: 2026-04-15 17:51:32 UTC*

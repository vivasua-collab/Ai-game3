# Чекпоинт: FIX-SORT — Исправление порядка Sorting Layers

**Дата:** 2026-04-17 12:50 UTC
**Фаза:** Рендеринг
**Статус:** complete

## Проблема

Спрайты terrain-поверхности рендерились **ПОВЕРХ** спрайтов игрока и объектов окружения. Корневые причины:

1. **Sorting Layers в неправильном порядке** — `EnsureSortingLayersExist()` только создавала недостающие слои, но НЕ проверяла их порядок. Если "Player" имел индекс меньше "Terrain", игрок рендерился позади terrain.
2. **TilemapRenderer'ы на "Default" слое** — `FixTilemapSortingLayers()` работала только с 3 конкретными [SerializeField] ссылками. Дополнительные TilemapRenderer'ы, созданные FullSceneBuilder, оставались на "Default" → рендерились поверх Player.
3. **PlayerVisual не проверял порядок слоёв** — `EnsureCorrectSortingLayer()` проверяла только существование слоя "Player", но не его позицию относительно Terrain/Objects.

## Выполненные задачи

- [x] TileMapController.cs: Добавлен `EnsureSortingLayerOrder()` — проверяет и исправляет ПОРЯДОК слоёв через TagManager
- [x] TileMapController.cs: Добавлен `FixAllTilemapRenderers()` — ищет ВСЕ TilemapRenderer на сцене по типу
- [x] TileMapController.cs: Добавлен `DetermineSortingLayerForRenderer()` — определяет слой по имени GameObject
- [x] PlayerVisual.cs: Улучшен `EnsureCorrectSortingLayer()` — проверяет ПОРЯДОК слоёв, fallback на "Objects" с order=100
- [x] RenderPipelineLogger.cs: Добавлен `LogAllRendererState()` (L9) — полная диагностика всех рендереров
- [x] Создан docs/SORTING_LAYERS.md — полная документация порядка слоёв
- [x] Обновлён docs/!LISTING.md — добавлены 4 новых документа

## Изменённые файлы

- `UnityProject/Assets/Scripts/Tile/TileMapController.cs` — +3 метода (EnsureSortingLayerOrder, FixAllTilemapRenderers, DetermineSortingLayerForRenderer)
- `UnityProject/Assets/Scripts/Player/PlayerVisual.cs` — переписан EnsureCorrectSortingLayer
- `UnityProject/Assets/Scripts/Core/RenderPipelineLogger.cs` — +1 метод (LogAllRendererState)
- `docs/SORTING_LAYERS.md` — создан (полная документация Sorting Layers)
- `docs/!LISTING.md` — обновлён (версия 2.5, +4 документа)

## Известные проблемы (на следующий этап)

- HarvestableSpawner сортирует по ТИПУ объекта (Herb=1, Tree=5), а не по Y-позиции → все деревья всегда поверх всех камней
- ResourceSpawner и DestructibleObjectController используют sortingOrder=5 — коллизия с деревьями
- Z=-0.5f в ResourceSpawner не влияет на рендеринг при Sorting Layers (бессмысленный хак)

## Следующие шаги

- Внедрить Y-сортировку для объектов на слое "Objects" (sortingOrder = -Y * 10)
- Удалить Z=-0.5f хак в ResourceSpawner
- Рассмотреть SortingGroup для Y-сортировки между Player и Objects слоями

# Чекпоинт: Критические фиксы + расширение tile системы

**Дата:** 2026-04-14 06:42:00 UTC
**Статус:** complete

## Выполненные задачи

### 1. Очистка мусорных файлов (после перезапуска окружения)
- Удалены src/, prisma/, db/, .zscripts/, mini-services/, examples/, bun.lock, Caddyfile и т.д.
- .gitignore обновлён — секция «Инфраструктура песочницы»

### 2. Перемещение файлов по START_PROMPT.md
- UnityProject/checkpoints/ → checkpoints/
- UnityProject/docs/ → docs/
- UnityProject/docs_temp/ → docs_temp/
- **Причина потери аудита 13.04:** файлы были в UnityProject/docs_temp/ вместо docs_temp/

### 3. Исправлено 8 из 9 критических багов
| # | Баг | Статус |
|---|-----|--------|
| 1 | SleepSystem.GetComponent<StatDevelopment>() ArgumentException | ✅ |
| 2 | WorldController не в ServiceLocator | ✅ |
| 3 | FactionData.FactionRelations Dictionary не сериализуется | ✅ |
| 4 | TestLocationGameController — не хватает компонентов | ✅ |
| 5 | SleepSystem.FindFirstObjectByType → ServiceLocator | ✅ |
| 6 | Синий фон между тайлами (pixelsPerUnit=64→32) | ✅ |
| 7 | ResourcePickup — тихая потеря предметов | ✅ |
| 8 | EventController — activeEvents теряются при загрузке | ✅ |
| 9 | 7 missing script references | ❌ (только ручная чистка в Unity Editor) |

### 4. Расширение tile системы
- Добавлены iceTile, lavaTile, oreVeinTile, herbTile в TileMapController
- FullSceneBuilder Phase 14: создание .asset + назначение для Ice, Lava, OreVein, Herb

## Изменённые файлы
- SleepSystem.cs, WorldController.cs, FactionController.cs, TestLocationGameController.cs
- ResourcePickup.cs, TileSpriteGenerator.cs, EventController.cs, TileMapController.cs
- FullSceneBuilder.cs, .gitignore, worklog.md
- docs_temp/AUDIT_2026-04-14.md

## Git
- Все коммиты запушены в origin/main
- Последний: ae15195

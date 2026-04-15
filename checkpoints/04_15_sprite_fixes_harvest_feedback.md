# Чекпоинт: Исправление спрайтов + спрайт игрока + обратная связь добычи

**Дата:** 2026-04-15 08:30:00 UTC
**Фаза:** Визуальные исправления + UX
**Статус:** ✅ COMPLETE

## Выполненные задачи

### Проблема 1: Зазоры между тайлами ✅
**Решение:**
- [x] TileSpriteGenerator.SaveTexture: spriteBorder(1,1,1,1) + wrapMode=Clamp
- [x] FullSceneBuilder.ReimportTileSprites(): переимпорт ВСЕХ PNG из Assets/Sprites/Tiles/ с PPU=32, spriteBorder, Point filter
- [x] Вызов ReimportTileSprites() в ExecuteCreateTileAssets() до создания tile assets

### Проблема 2: Нет спрайта у персонажа ✅
**Решение:**
- [x] CreatePlayerSprite: загрузка из Assets/Sprites/Characters/Player/ (8 вариантов)
- [x] Реимпорт спрайта с PPU=32 при загрузке
- [x] CreateProceduralPlayerSprite fallback: PPU=32 (было 64)

### Проблема 3: Сцены удалены с GitHub ✅
- [x] git pull подтянул удаление (коммит 6e11fa9)

### Проблема 4: Нет визуальной обратной связи при добыче (F) ✅
**Решение:**
- [x] HarvestFeedbackUI.cs — WorldSpace Canvas с прогресс-баром + текст
  - Цветовая индикация: жёлтый → зелёный → ярко-зелёный
  - Текст: "⏳ Добываю [ресурс]..." / "+N [ресурс]" / "Слишком далеко"
  - Fade-out автоуничтожение
- [x] PlayerController.AttemptHarvest() — F-key обработка:
  - Поиск ближайшего разрушаемого объекта (по направлению движения + соседние тайлы)
  - Дистанционная проверка (harvestRange=2.5)
  - Кулдаун 0.8с
  - Прогресс-бар + текст через HarvestFeedbackUI
  - Опыт Strength за добычу
- [x] DestructibleObjectController.OnHarvestStarted событие

## Изменённые файлы
- UnityProject/Assets/Scripts/Tile/Editor/TileSpriteGenerator.cs — spriteBorder + wrapMode
- UnityProject/Assets/Scripts/Editor/FullSceneBuilder.cs — ReimportTileSprites + CreatePlayerSprite PPU=32
- UnityProject/Assets/Scripts/UI/HarvestFeedbackUI.cs — НОВЫЙ
- UnityProject/Assets/Scripts/Player/PlayerController.cs — F-key harvest + HarvestFeedbackUI
- UnityProject/Assets/Scripts/Tile/DestructibleObjectController.cs — OnHarvestStarted
- checkpoints/04_15_sprite_fixes_harvest_feedback.md — НОВЫЙ

## Коммит
3e3a1d3 — Pushed to origin/main

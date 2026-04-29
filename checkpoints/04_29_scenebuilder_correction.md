# Чекпоинт: Корректировка оркестратора и файлов билдера

**Дата:** 2026-04-29 12:13:35 UTC
**Статус:** complete
**Цель:** Исправить критические и средние проблемы, выявленные аудитом SceneBuilder

---

## Выполненные задачи

### 🔴 Критические (K1-K3)

| # | Фаза | Проблема | Решение | Статус |
|---|------|----------|---------|--------|
| K1 | Phase16 | IsNeeded() проверяет только Backpacks | Добавлены проверки StorageRings, TestSet/Basic, weapon_Sword_T1_Common.asset | ✅ |
| K2 | Phase09 | IsNeeded() проверяет 3/9+ типов | Добавлены проверки Techniques, NPCPresets, Equipment, Items, Materials, Formations | ✅ |
| K3 | Phase17, 18 | IsNeeded() → false при отсутствии зависимости | Вернуть true + LogWarning (фаза требуется, но Execute выдаст ошибку) | ✅ |

### 🟡 Средние (C1-C5)

| # | Фаза | Проблема | Решение | Статус |
|---|------|----------|---------|--------|
| C1 | 04-08,14,15,17,18 | EnsureSceneOpen в IsNeeded (побочный эффект) | Убран из всех IsNeeded(). Если сцена не открыта → объекты не найдены → return true автоматически | ✅ |
| C2 | Phase16 | EnsureSceneOpen в Execute (не нужна) | Убран — фаза не модифицирует сцену | ✅ |
| C5 | Phase13 | Нет EnsureSceneOpen перед сохранением | Добавлен в Execute() | ✅ |

### 📝 Документация

| # | Файл | Что | Статус |
|---|------|-----|--------|
| D1 | SCENE_BUILDER_ARCHITECTURE.md | Добавлен Phase00URPSetup в структуру файлов + таблицу фаз | ✅ |

### 🧹 Инлайн-отметки

Убраны все инлайн-отметки `Редактировано:` из тел файлов Phase00, 04, 07, 08, 17. Консолидированы в шапки.

---

## Изменённые файлы

- `Editor/SceneBuilder/Phase04CameraLight.cs` — IsNeeded: убран EnsureSceneOpen
- `Editor/SceneBuilder/Phase05GameManager.cs` — IsNeeded: убран EnsureSceneOpen
- `Editor/SceneBuilder/Phase06Player.cs` — IsNeeded: убран EnsureSceneOpen
- `Editor/SceneBuilder/Phase07UI.cs` — IsNeeded: убран EnsureSceneOpen; убрана инлайн-отметка
- `Editor/SceneBuilder/Phase08Tilemap.cs` — IsNeeded: убран EnsureSceneOpen; убрана инлайн-отметка
- `Editor/SceneBuilder/Phase09GenerateAssets.cs` — IsNeeded: расширена проверка всех типов ассетов
- `Editor/SceneBuilder/Phase13SaveScene.cs` — Execute: добавлен EnsureSceneOpen
- `Editor/SceneBuilder/Phase14CreateTileAssets.cs` — IsNeeded: убран EnsureSceneOpen
- `Editor/SceneBuilder/Phase15ConfigureTestLocation.cs` — IsNeeded: убран EnsureSceneOpen
- `Editor/SceneBuilder/Phase16InventoryData.cs` — IsNeeded: расширена; Execute: убран EnsureSceneOpen
- `Editor/SceneBuilder/Phase17InventoryUI.cs` — IsNeeded: return true + LogWarning при отсутствии зависимости; убраны инлайн-отметки
- `Editor/SceneBuilder/Phase18InventoryComponents.cs` — IsNeeded: return true + LogWarning при отсутствии зависимости
- `Editor/SceneBuilder/Phase00URPSetup.cs` — убрана инлайн-отметка из тела
- `docs_asset_setup/SCENE_BUILDER_ARCHITECTURE.md` — добавлен Phase00URPSetup

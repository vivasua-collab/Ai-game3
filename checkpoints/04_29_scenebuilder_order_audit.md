# Чекпоинт: Аудит порядка создания SceneBuilder

**Дата:** 2026-04-29 12:05:54 UTC
**Статус:** complete
**Цель:** Проверить корректность порядка фаз в SceneBuilder, зависимости, IsNeeded()

---

## 1. Соответствие Order ↔ позиция в PHASES[]

**Результат: ВСЕ СОВПАДАЮТ ✅**

19 фаз (00-18), Order каждой = индексу в массиве PHASES[].

---

## 2. Граф зависимостей (прямых)

```
Phase00 URP ──────→ Phase04 (Light2D)
Phase01 Folders ──→ Phase09, Phase10, Phase11, Phase16 (папки)
Phase02 Tags ─────→ Phase06 (Player tag/layer/sorting), Phase08 (sorting layers)
Phase03 Scene ────→ Phase04-08, 13-15, 17, 18 (сцена должна существовать)
Phase04 Camera ───→ Phase15 (камера)
Phase05 GameMgr ──→ Phase07 (TimeController для HUD)
Phase06 Player ───→ Phase18 (Player GO)
Phase07 UI ───────→ Phase17 (Canvas + UIManager)
Phase08 Tilemap ──→ Phase14 (TileMapController), Phase15 (Grid)
Phase09 Assets ───→ Phase16 (ItemData для обновления)
Phase10 Sprites ──→ Phase14 (спрайты, есть fallback)
Phase16 InvData ──→ Phase18 (Backpack_ClothSack.asset)
Phase17 InvUI ────→ Phase18 (InventoryScreen)
```

**Обратных зависимостей НЕТ ✅** — все связи идут строго вперёд.

---

## 3. Найденные проблемы

### 🔴 КРИТИЧЕСКИЕ

| # | Фаза | Проблема |
|---|------|----------|
| К1 | Phase16 | `IsNeeded()` проверяет ТОЛЬКО `Assets/Data/Backpacks`. Не проверяет: StorageRings, TestSet, обновление ItemData. Если Backpacks существует, но остальное нет → фаза молча пропускается |
| К2 | Phase09 | `IsNeeded()` проверяет ТОЛЬКО 3 из 9+ типов ассетов (CultivationLevels, Elements, MortalStages). Остальные (Techniques, NPC, Equipment, Items, Materials, Formations) — не проверяются |
| К3 | Phase17, 18 | `IsNeeded()` возвращает `false` если зависимость (Canvas/Player) отсутствует → фаза молча пропускается без ошибки |

### 🟡 СРЕДНИЕ

| # | Фаза | Проблема |
|---|------|----------|
| С1 | 04-08,14,15,17,18 | `IsNeeded()` вызывает `EnsureSceneOpen()` — побочный эффект в методе чтения |
| С2 | Phase16 | `Execute()` вызывает `EnsureSceneOpen()` — но фаза не модифицирует сцену (только .asset файлы) |
| С3 | Phase08, Phase15 | Дублирование кода создания GameController, HarvestableSpawner |
| С4 | Phase12 | Интерактивная фаза (открывает окно TMP) — блокирует автоматическую сборку |
| С5 | Phase13 | Не проверяет правильную сцену перед сохранением (нет EnsureSceneOpen) |

### 🟢 НИЗКИЕ

| # | Фаза | Проблема |
|---|------|----------|
| Н1 | Phase04 vs Phase15 | Camera bounds задаются независимо — хрупкое совпадение (200×160) |

---

## 4. Отсутствующая фаза в документации

**Phase00URPSetup** — существует в коде и в PHASES[], но НЕ указана в `SCENE_BUILDER_ARCHITECTURE.md` (там нумерация начинается с 01).

---

## 5. Исправления в этой сессии

- Убраны инлайн-отметки `Редактировано:` из тел файлов Phase00, Phase04, Phase07, Phase08, Phase17
- Все отметки консолидированы в шапках файлов

---

## Изменённые файлы

- `Editor/SceneBuilder/Phase00URPSetup.cs` — убрана инлайн-отметка, добавлена в шапку
- `Editor/SceneBuilder/Phase04CameraLight.cs` — убрана инлайн-отметка
- `Editor/SceneBuilder/Phase07UI.cs` — убрана инлайн-отметка
- `Editor/SceneBuilder/Phase08Tilemap.cs` — убрана инлайн-отметка
- `Editor/SceneBuilder/Phase17InventoryUI.cs` — убраны 5 инлайн-отметок

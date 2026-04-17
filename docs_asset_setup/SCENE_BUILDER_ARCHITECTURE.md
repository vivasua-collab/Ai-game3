# Архитектура генерации сцены — Двухскриптовая модель

**Создано:** 2026-04-17 13:49:14 UTC
**Версия:** 1.0

---

## Принцип

Генерация сцены разделена на **ДВА скрипта**. Это предотвращает регрессионные баги при модификации базового генератора.

---

## Скрипт 1: FullSceneBuilder.cs — ЗАМОРОЖЕН 🧊

**Путь:** `Assets/Scripts/Editor/FullSceneBuilder.cs`
**Версия:** 1.2 (FROZEN)
**Строк:** 2517
**Статус:** ЗАМОРОЖЕН — редактирование ЗАПРЕЩЕНО

### Что делает

- 15 идемпотентных фаз: папки → теги/слои → сцена → камера/свет → GameManager → Player → UI → Tilemap → ассеты → спрайты → формации → TMP → сохранение → тайлы → тестовая локация
- Создаёт сцену с нуля до полностью рабочего состояния
- Каждая фаза: `IsNeeded()` → `Execute()` → логирование
- Повторный запуск безопасен — пропуск уже выполненных фаз

### Почему заморожен

- Многократные баги от прямого редактирования: Sorting Layer порядок, Missing Prefabs, дубликаты компонентов
- Каждый баг требовал отката и повторной сборки
- Скрипт сложный (2517 строк) — любое изменение может сломать 15 фаз

### Исключение — критические багфиксы

- Только если баг ломает компиляцию или делает сцену неработоспособной
- Перед фиксом — ОБЯЗАТЕЛЬНО запросить подтверждение у пользователя
- После фикса — обновить версию (1.2 → 1.3) и дату в заголовке файла

### Фазы (подробно)

| Фаза | Имя | IsNeeded | Execute | Описание |
|------|-----|----------|---------|----------|
| 01 | Folders | Проверка 33 папок | Создание недостающих | Базовая структура каталогов |
| 02 | Tags & Layers | Проверка 7 тегов + 8 слоёв + Sorting Layers | Создание тегов, Physics Layers, Sorting Layers | FIX-SORT: проверка порядка |
| 03 | Create Scene | Проверка файла сцены | Создание сцены, удаление дефолтной камеры | |
| 04 | Camera & Light | Камера + GlobalLight2D | Camera2DSetup + Directional Light + Light2D (reflection) | |
| 05 | GameManager | GameManager GO | GameInitializer + 8 системных компонентов | |
| 06 | Player | Player GO | Rigidbody2D + CircleCollider2D + 8 компонентов | PlayerVisual, НЕ SpriteRenderer |
| 07 | UI | Canvas + EventSystem | HUD: TMP тексты + слайдеры | |
| 08 | Tilemap | Grid + Terrain + Objects tilemaps | TileMapController + TestLocationGameController + HarvestableSpawner | |
| 09 | Generate Assets | AssetGenerator | JSON → ScriptableObjects | |
| 10 | Tile Sprites | Процедурные спрайты | Sprite.Create для terrain | |
| 11 | Formation UI | Префабы формаций | Canvas + элементы | |
| 12 | TMP Essentials | Импорт TMP | Package requirement | |
| 13 | Save Scene | Сохранение | EditorSceneManager.SaveScene | |
| 14 | Tile Assets | .asset файлы | TerrainTile + ObjectTile для 15+ типов | |
| 15 | Configure Test Location | Камера + коллайдеры | Verify HarvestableSpawner | |

---

## Скрипт 2: ScenePatchBuilder.cs — АКТИВНЫЙ 📝

**Путь:** `Assets/Scripts/Editor/ScenePatchBuilder.cs`
**Версия:** 1.0
**Строк:** 1019
**Статус:** АКТИВЕН — сюда вносятся ВСЕ будущие изменения сцены

### Что делает

- Инкрементальные патчи поверх сцены, созданной FullSceneBuilder
- Каждый патч = атомарное изменение с верификацией
- Патчи идемпотентны — повторный запуск безопасен

### Архитектура патча

```
PATCH-NNN:
  1. IsApplied()  — проверка, применён ли уже (по состоянию сцены)
  2. Apply()      — применение патча
  3. Validate()   — пост-проверка корректности
  4. Описание     — для логирования
```

### Реестр применённых патчей

**EditorPrefs ключ:** `CultivationGame_AppliedPatches`
Формат: строка с разделителями `;` (например `PATCH-001;PATCH-002;PATCH-003`)

### Текущие патчи

| ID | Описание | Критичность |
|----|----------|-------------|
| PATCH-001 | Порядок Sorting Layers (Default < Background < Terrain < Objects < Player < UI) | КРИТИЧЕСКИЙ |
| PATCH-002 | TilemapRenderer на правильных Sorting Layers | КРИТИЧЕСКИЙ |
| PATCH-003 | PlayerVisual SpriteRenderer на слое 'Player' | КРИТИЧЕСКИЙ |
| PATCH-004 | GlobalLight2D существует и настроен | ВЫСОКИЙ |
| PATCH-005 | HarvestableSpawner назначен на GameController | ВЫСОКИЙ |
| PATCH-006 | Grid cellSize=(2,2,1), cellGap=(0,0,0) | СРЕДНИЙ |
| PATCH-007 | Terrain НЕ имеет TilemapCollider2D | ВЫСОКИЙ |
| PATCH-008 | Missing Scripts и Prefabs — очистка | СРЕДНИЙ |

### Меню Unity

- `Tools → Scene Patch Builder → Apply All Pending Patches`
- `Tools → Scene Patch Builder → Validate Current Scene`
- `Tools → Scene Patch Builder → Show Applied Patches`
- `Tools → Scene Patch Builder → Reset Patch History (Dangerous!)`

### Как добавить новый патч

1. Определить следующий ID (PATCH-009, PATCH-010...)
2. Добавить `PatchInfo` в массив `PATCHES` в ScenePatchBuilder.cs
3. Реализовать три метода: `IsPatchNNNApplied()`, `ApplyPatchNNN()`, `ValidatePatchNNN()`
4. Проверить: `Validate Current Scene` должен показывать новый патч
5. Закоммитить с пометкой `PATCH-NNN: описание`

---

## Правило: НЕ РЕДАКТИРОВАТЬ FullSceneBuilder.cs

**Если агент собирается редактировать FullSceneBuilder.cs — ОСТАНОВИТЬСЯ и:**
1. Проверить: это критический багфикс? Если НЕТ → создать патч в ScenePatchBuilder.cs
2. Если ДА → запросить подтверждение у пользователя перед редактированием
3. После любого редактирования FullSceneBuilder → обновить версию и дату в заголовке

**Нарушение этого правила = регрессионный баг и потеря времени.**

---

## Известные проблемы (требуют патчей)

1. **Белые швы между terrain-тайлами** — Sprite.Create с FilterMode.Point может оставлять 1px артефакты. Возможное решение: pad-спрайты или Sprite.DrawMode.Sliced
2. **Sorting Layer порядок при первом запуске** — если FullSceneBuilder создаёт слои, а потом ScenePatchBuilder запускается первым, порядок может быть неправильным. Решение: ScenePatchBuilder PATCH-001 покрывает этот случай

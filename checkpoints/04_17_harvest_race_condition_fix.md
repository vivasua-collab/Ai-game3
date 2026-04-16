# Чекпоинт: Race Condition — HarvestableSpawner пропускает OnMapGenerated

**Дата:** 2026-04-17
**Статус:** complete
**Цель:** Исправить Race Condition: HarvestableSpawner не спавнит объекты

---

## Проблема

После создания новой сцены руда/камни/деревья **не появляются как Harvestable GameObject**.
Вместо этого они либо отсутствуют, либо подбираются через ResourcePickup (автоподбор).

**Симптом:** В консоли только логи ResourcePickup (qi_crystal), нет логов HarvestableSpawner.

## Корневая причина

**Race Condition** в порядке выполнения Unity lifecycle:

1. `TileMapController.Start()` → `GenerateTestMap()` → `OnMapGenerated?.Invoke(mapData)`
2. `HarvestableSpawner.Start()` → подписывается на `OnMapGenerated` ← **Событие уже прошло!**

Unity не гарантирует порядок выполнения `Start()`. Если TileMapController
выполняется первым (что происходит по умолчанию), HarvestableSpawner
подписывается ПОСЛЕ вызова события и никогда его не получает.

## Исправление

**HarvestableSpawner.cs** — двойная стратегия:

1. **Awake()**: подписка на `OnMapGenerated` (выполняется до всех Start)
2. **Start()**: проверка — если `tileMapController.MapData != null` и ещё не заспавнено,
   немедленно вызвать `SpawnHarvestables()` с существующими данными

Это покрывает оба сценария:
- HarvestableSpawner.Awake() раньше TileMapController.Start() → событие получено
- TileMapController.Start() раньше → MapData уже есть, спавн в Start()

## Изменённые файлы

| Файл | Изменение |
|------|-----------|
| `HarvestableSpawner.cs` | Awake: подписка + Start: проверка MapData |

---

*Создано: 2026-04-17*

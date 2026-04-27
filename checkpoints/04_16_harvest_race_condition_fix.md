# Чекпоинт: Race Condition — HarvestableSpawner пропускает OnMapGenerated

**Дата:** 2026-04-16 05:48 UTC
**Фаза:** 2 (Post-Harvest hotfix)
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

## Выполненные задачи

- [x] Добавить подписку на OnMapGenerated в Awake()
- [x] Добавить fallback-проверку MapData в Start()
- [x] Покрыты оба сценария порядка выполнения

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
| `Assets/Scripts/Spawners/HarvestableSpawner.cs` | Awake: подписка + Start: проверка MapData |

## Проблемы

⚠️ **После этого фикса обнаружена визуальная регрессия:** HarvestableSpawner начал спавнить объекты, но `GetSpritePath()` содержал неверные пути (без префикса `obj_`), что привело к fallback на процедурные спрайты — объекты отображаются как «каличья». Это НЕ ошибка данного фикса, а следствие изначально неверных путей в `GetSpritePath()` (созданы в 04_15). Подробнее: `04_16_sprite_regression_audit.md`.

## Следующие шаги

- Исправить маппинг спрайтов в HarvestableSpawner (04_16_sprite_regression_audit.md, Вариант B)

---

*Создано: 2026-04-16 05:48 UTC*
*Редактировано: 2026-04-16 07:35 UTC*

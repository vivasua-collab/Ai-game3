# Чекпоинт: План системы добычи (Harvest System) — v3 ФИНАЛЬНЫЙ

**Дата:** 2026-04-15 18:18:27 UTC  
**Статус:** in_progress  
**Цель:** Реализовать систему добычи для статичных объектов (руды, деревья, растения) через кнопку F

---

## 1. Текущее состояние

### Что работает
- `PlayerController.AttemptHarvest()` — реагирует на F, ищет объекты через TileMapController (тайловые данные)
- `DestructibleObjectController.DamageObjectAtTile()` — наносит урон, при разрушении создаёт дроп
- `TileObjectData` — содержит `isHarvestable`, `maxDurability`, `currentDurability`, `resourceId`, `resourceCount`
- `HarvestFeedbackUI` — визуальная обратная связь (прогресс-бар)
- `ResourcePickup` — автоподбор разбросанных ресурсов (травы, ягоды)

### Проблема
Статичные объекты (деревья, руды, камни) отрисовываются через Tilemap,  
не имеют GameObject с коллайдерами → нельзя обнаружить через Physics2D.  

---

## 2. Решённые архитектурные вопросы

### 2.1 Спавн: Вариант C — полный переход на GameObject ✓

**Принято пользователем.** Все harvestable объекты спавнятся как отдельные GameObject.  
objectTilemap используется только для не-harvestable объектов.  
Количество объектов ограничим в финальном релизе.

### 2.2 Harvestable — независимый MonoBehaviour ✓

**Принято пользователем.** Не наследует Interactable.  
Поиск через Physics2D по слою "Harvestable".

### 2.3 Модель добычи: прогресс-бар заполняется за 1 тик ✓

**Уточнение пользователя:** Постепенный прогресс-бар, который заполняется за 1 тик (одно нажатие F).  
Не мгновенный лут, а визуальная анимация заполнения, завершающаяся за время кулдауна.

**Механика:**
1. Нажал F → прогресс-бар начинает заполняться
2. Заполняется за ~0.5–0.8 сек (один тик добычи)
3. По завершении — ресурс добавляется в инвентарь
4. Кулдаун 0.8 сек до следующего удара
5. Каждый тик уменьшает durability на harvestDamage

### 2.4 Количество лута — настраиваемое + коэффициент инструмента ✓

**Принято пользователем:**  
- Базовые значения из таблицы (см. раздел 6.4)  
- Должна быть возможность менять через Inspector  
- Коэффициент инструмента: `finalYield = baseYield * toolMultiplier`  
- Инструменты — отдельная система, черновик документации в docs_temp  
- Пока что `toolMultiplier = 1.0` (без инструмента)

### 2.5 Physics-слой "Harvestable" ✓

**Принято пользователем.**

### 2.6 Респаун — НЕТ ✓

**Решение пользователя:** При входе на локацию выполняется новая генерация.  
Респаун не нужен — объекты создаются заново.  
Будет ограничение таймаутами (детали позже).

**Следствия для Harvestable:**
- Нет поля `respawnTime`
- Нет метода `Respawn()`
- Нет depleted-состояния с таймером
- При исчерпании объект переходит в состояние `isDepleted=true`
- Визуал меняется (пень / пустая жила / обломки) — но НЕ респавнится
- Объект остаётся в сцене как «мёртвый» до перегенерации локации

### 2.7 Визуальная индикация — нужна ✓

Подсказка «F — Добыть [name]» + подсветка ближайшего объекта.

---

## 3. Анализ системы времени (TimeController)

### 3.1 Реализовано

**TimeController** — полноценная система, НЕ заглушка:

| Компонент | Статус |
|-----------|--------|
| Детерминированное время (FixedUpdate) | ✅ Работает |
| `autoAdvance = true` | ✅ По умолчанию |
| `useDeterministicTime = true` | ✅ Включён |
| `normalSpeedRatio = 60` (1с = 1мин) | ✅ Настроен |
| События OnMinutePassed, OnHourPassed, OnDayPassed, OnTick | ✅ Реализованы |
| Форматирование FormattedTime / FormattedDate | ✅ Работает |
| Регистрация в ServiceLocator | ✅ Через GameInitializer |
| Сохранение/загрузка | ✅ TimeSaveData |

### 3.2 Отображение в HUD

**HUDController.UpdateTimeDisplay()** — вызывает каждый кадр:
```
timeText.text = timeController.FormattedTime;  // "HH:MM"
dateText.text = timeController.FormattedDate;  // "DD.MM.YYYY"
```

**Проблема:** `[SerializeField] private TMP_Text timeText/dateText` — назначаются через Inspector.  
Если объект HUD не имеет этих TMP_Text компонентов — отображения не будет.  
Нужна проверка: создаются ли эти элементы в FullSceneBuilder.

### 3.3 Проблема: нет тикового отображения

Пользователь указал: «в отображении не идёт тиковое время».  

**Анализ:** FormattedTime показывает `HH:MM` — минуты обновляются каждую реальную секунду  
(при normalSpeedRatio=60, 1 реальная секунда = 1 игровая минута).  
Тики (OnTick) отправляются каждую реальную секунду (`tickInterval = 1f`),  
но **нигде не отображаются**. OnTick используется только в FormationController.

**Диагноз:** Время РАБОТАЕТ и обновляется, но:
1. Если TMP_Text поля не назначены в Inspector — ничего не видно
2. Секунды не отображаются (только HH:MM) — поэтому кажется что время стоит
3. Тиковый счётчик (CurrentTick) не выводится в UI

**Рекомендация:**  
- Добавить отображение секунд: `HH:MM:SS`  
- Или добавить отдельный текст для тикового времени  
- Проверить назначение timeText/dateText в сцене

---

## 4. Компонент `Harvestable` (финальная спецификация)

### 4.1 Поля

```csharp
[Header("Identity")]
string resourceId           // "wood", "stone", "ore", "herb", "berries"
string displayName          // "Древесина", "Руда" (для UI)

[Header("Durability")]
int maxDurability           // Максимальная прочность
int currentDurability       // Текущая прочность

[Header("Harvest")]
int harvestDamage = 25      // Урон по durability за один тик
float harvestDuration = 0.6f // Длительность одного тика добычи (заполнение прогресс-бара)
float harvestCooldown = 0.8f // Кулдаун между тиками (секунды)
float harvestRange = 2.5f   // Дальность добычи

[Header("Yield")]
int baseYieldPerTick        // Базовое количество ресурса за тик
float toolMultiplier = 1.0f // Коэффициент инструмента (пока = 1.0)
int totalResourceCount      // Общее количество ресурса в объекте
int harvestedSoFar          // Сколько уже добыто

[Header("Visuals")]
Sprite normalSprite         // Нормальный спрайт
Sprite depletedSprite       // Спрайт после исчерпания (пень, пустая жила)

[Header("State")]
bool isDepleted = false     // Исчерпан?
```

### 4.2 Методы

```csharp
bool CanHarvest()              // !isDepleted && currentDurability > 0
int HarvestHit(int damage)     // Один тик добычи, возвращает количество ресурса
void Deplete()                 // Исчерпание объекта (смена спрайта, isDepleted=true)
void Initialize(TileObjectData) // Инициализация из данных тайла
float GetProgress()            // currentDurability / maxDurability (0..1)
int GetRemainingResource()     // totalResourceCount - harvestedSoFar
```

### 4.3 События

```csharp
event Action<Harvestable, int> OnHarvested     // (объект, количество полученного ресурса)
event Action<Harvestable> OnDepleted           // Объект исчерпан
```

### 4.4 Требования к GameObject

1. `SpriteRenderer` — визуал объекта
2. `BoxCollider2D` — физический коллайдер (блокировка прохода), НЕ trigger
3. `CircleCollider2D` (isTrigger) — зона обнаружения для Physics2D
4. `Rigidbody2D` (Static) — для корректной работы физики
5. `Harvestable` — логика добычи
6. Слой `"Harvestable"` — для LayerMask фильтрации
7. Тег `"Harvestable"` — для идентификации

---

## 5. Поток добычи (финальный)

```
Игрок нажимает F
  │
  ├─ PlayerController.AttemptHarvest()
  │   ├─ Проверка кулдауна
  │   ├─ Поиск ближайшего Harvestable:
  │   │   Physics2D.OverlapCircleAll(playerPos, harvestRange, harvestableLayer)
  │   │   → сортировка по расстоянию → ближайший
  │   │
  │   ├─ Если не найден → ShowHarvestFailed("Рядом нет ресурсов")
  │   │
  │   ├─ harvestable.CanHarvest()?
  │   │   ├─ Нет → ShowHarvestFailed("Нельзя добыть")
  │   │   └─ Да → Начать тик добычи:
  │   │       ├─ Показать прогресс-бар HarvestFeedbackUI
  │   │       ├─ Заполнить за harvestDuration (0.6с)
  │   │       ├─ По завершении:
  │   │       │   ├─ currentDurability -= damage
  │   │       │   ├─ yield = CalculateYield(baseYieldPerTick, toolMultiplier, totalResourceCount, harvestedSoFar)
  │   │       │   ├─ Добавить yield ресурса в InventoryController.AddItem()
  │   │       │   ├─ harvestedSoFar += yield
  │   │       │   ├─ Показать "+N ресурс" через HarvestFeedbackUI
  │   │       │   ├─ lastHarvestTime = Time.time
  │   │       │   ├─ Опыт Strength +0.5
  │   │       │   └─ Если currentDurability ≤ 0 → Deplete()
  │   │       └─ Конец тика
  │   │
  │   └─ Конец
```

### 5.1 Формула расчёта лута

```
baseYield = baseYieldPerTick * toolMultiplier
actualYield = min(baseYield, totalResourceCount - harvestedSoFar)
```

### 5.2 Прогресс-бар за 1 тик

При нажатии F:
1. Создаём/обновляем HarvestFeedbackUI над объектом
2. Прогресс-бар анимируется от 0 до 1 за `harvestDuration` (0.6с)
3. В момент завершения (value=1) — добавляем ресурс в инвентарь
4. Кулдаун `harvestCooldown` (0.8с) до следующего нажатия F

---

## 6. Спавн Harvestable объектов

### 6.1 HarvestableSpawner (новый)

Подписывается на `TileMapController.OnMapGenerated`,  
читает TileMapData, создаёт GameObject для isHarvestable объектов.

### 6.2 Интеграция с TileMapController

В `RenderMap()` пропускать объекты с `isHarvestable=true`.  
Не ставить их в objectTilemap — HarvestableSpawner создаст их как GameObject.

### 6.3 Проходимость

Когда Harvestable не исчерпан — BoxCollider2D блокирует проход.  
При исчерпании — коллайдер уменьшается (пень меньше дерева).

### 6.4 Маппинг TileObjectType → параметры

| TileObjectType | resourceId | displayName | maxDur | baseYield | totalRes | depleted |
|----------------|-----------|-------------|--------|-----------|----------|----------|
| Tree_Oak | wood | Древесина | 200 | 2 | 10 | Пень |
| Tree_Pine | wood | Древесина | 200 | 2 | 10 | Пень |
| Tree_Birch | wood | Древесина | 150 | 2 | 8 | Пень |
| Rock_Small | stone | Камень | 100 | 1 | 3 | Обломки |
| Rock_Medium | stone | Камень | 300 | 1 | 10 | Обломки |
| OreVein | ore | Руда | 400 | 1 | 15 | Пустая жила |
| Bush | berries | Ягоды | 50 | 3 | 5 | Сухой куст |
| Bush_Berry | berries | Ягоды | 50 | 3 | 5 | Сухой куст |
| Herb | herb | Трава | 10 | 1 | 1 | Нет |

**Все значения настраиваемые через Inspector/код.**

### 6.5 Herb — особый случай

maxDurability=10, harvestDamage=25 → один удар = полный сбор.  
Фактически автоподбор, но через F-взаимодействие.  
**Решение:** Herb тоже делаем Harvestable (для единообразия),  
но с harvestDuration=0.2с (мгновенный сбор).

### 6.6 ResourceSpawner — что убрать

Убрать из `SetDefaultResourceTypes()` типы, которые теперь спавнит HarvestableSpawner:  
- stone, ore, iron_ore, wood  
Оставить только разбросанные: herb, berries, mushroom, rare_herb, qi_crystal, spirit_stone, sand_pearl, desert_crystal

**ПРИМЕЧАНИЕ:** Травы (herb) и ягоды (berries) — спорный момент.  
В текущем ResourceSpawner они спавнятся как автоподбираемые ResourcePickup.  
С HarvestableSpawner они будут спавниться как статичные Harvestable.  
Нужно выбрать один способ — не оба одновременно.

**Рекомендация:** Оставить herb и berries в ResourceSpawner (автоподбор),  
а HarvestableSpawner спавнит только крупные объекты: trees, rocks, ore, bush.  
Это ближе к пользовательскому ожиданию: мелкие ресурсы подбираются, крупные — добываются.

---

## 7. UI: Подсказка добычи

### 7.1 Отображение

Когда игрок в радиусе `harvestRange` от Harvestable:
- Над объектом текст: **«F — Добыть [displayName]»**
- Жёлтый цвет, WorldSpace Canvas
- При нажатии F → прогресс-бар + текст «+N ресурс»

### 7.2 Реализация

Расширить `HarvestFeedbackUI`:
- Метод `ShowHarvestPrompt(string resourceName)` — показывает подсказку
- Метод `HideHarvestPrompt()` — скрывает
- В `PlayerController.Update()` — сканирование ближайшего Harvestable

---

## 8. Система инструментов — черновик

### 8.1 Концепция

Инструменты — экипируемые предметы, модифицирующие добычу:
- Топор — ×2 урон и ×1.5 лут для деревьев
- Кирка — ×2 урон и ×1.5 лут для камней/руды
- Серп — ×2 урон и ×1.5 лут для растений
- Кулаки (без инструмента) — ×1.0

### 8.2 Структура данных (будущая)

```csharp
public class ToolData : ItemData
{
    ToolType toolType;        // Axe, Pickaxe, Sickle, Hands
    float damageMultiplier;   // 1.0-3.0
    float yieldMultiplier;    // 1.0-3.0
    TileObjectCategory[] effectiveCategories; // Для каких категорий объектов
}
```

### 8.3 Интеграция с Harvestable

В `PlayerController.AttemptHarvest()`:
```
ToolData currentTool = equipmentController?.GetEquippedTool();
float damageMult = currentTool?.GetDamageMultiplier(harvestable.category) ?? 1.0f;
float yieldMult = currentTool?.GetYieldMultiplier(harvestable.category) ?? 1.0f;
int actualDamage = Mathf.RoundToInt(harvestDamage * damageMult);
harvestable.HarvestHit(actualDamage, yieldMult);
```

### 8.4 Документация

Черновик будет создан в `docs_temp/tool_system_draft.md`  
перед реализацией системы добычи.

---

## 9. План реализации (пошаговый)

### Шаг 0: Документация
- Создать `docs_temp/tool_system_draft.md` — черновик системы инструментов
- Обновить чекпоинт до статуса complete

### Шаг 1: Создать Harvestable.cs
- MonoBehaviour с полями из раздела 4.1
- Методы: CanHarvest(), HarvestHit(), Deplete(), Initialize()
- События: OnHarvested, OnDepleted
- Логика расчёта лута с toolMultiplier

### Шаг 2: Создать HarvestableSpawner.cs
- Подписка на TileMapController.OnMapGenerated
- Обход TileMapData, создание GameObject для isHarvestable объектов
- Маппинг TileObjectType → параметры (таблица из 6.4)
- Загрузка AI-спрайтов
- Создание depleted-спрайтов (программных fallback)

### Шаг 3: Модифицировать PlayerController.AttemptHarvest()
- Поиск Harvestable через Physics2D.OverlapCircleAll
- Сортировка по расстоянию
- Вызов Harvestable.HarvestHit() с учётом toolMultiplier
- Прогресс-бар за 1 тик через HarvestFeedbackUI
- Опыт Strength за удар

### Шаг 4: Модифицировать TileMapController.RenderMap()
- Пропускать объекты с isHarvestable=true

### Шаг 5: Модифицировать ResourceSpawner.SetDefaultResourceTypes()
- Убрать: stone, ore, iron_ore, wood
- Оставить: herb, berries, mushroom, rare_herb, qi_crystal, spirit_stone, sand_pearl, desert_crystal

### Шаг 6: UI подсказки
- Расширить HarvestFeedbackUI: ShowHarvestPrompt()
- В PlayerController.Update(): сканирование ближайшего Harvestable
- Отображение «F — Добыть [name]»

### Шаг 7: Добавить Physics-слой "Harvestable"
- Инструкция для Unity Editor
- В коде спавнера: go.layer

### Шаг 8: Интеграция в TestLocationGameController
- Настройка HarvestableSpawner

---

## 10. Файлы для изменения

| Файл | Действие | Приоритет |
|------|----------|-----------|
| `docs_temp/tool_system_draft.md` | **СОЗДАТЬ** — черновик системы инструментов | P0 |
| `Scripts/Tile/Harvestable.cs` | **СОЗДАТЬ** — компонент добычи | P1 |
| `Scripts/Tile/HarvestableSpawner.cs` | **СОЗДАТЬ** — спавнер | P1 |
| `Scripts/Player/PlayerController.cs` | **ИЗМЕНИТЬ** — AttemptHarvest | P1 |
| `Scripts/Tile/TileMapController.cs` | **ИЗМЕНИТЬ** — RenderMap | P1 |
| `Scripts/Tile/ResourceSpawner.cs` | **ИЗМЕНИТЬ** — убрать harvestable-типы | P2 |
| `Scripts/UI/HarvestFeedbackUI.cs` | **ИЗМЕНИТЬ** — ShowHarvestPrompt | P2 |
| `Scripts/World/TestLocationGameController.cs` | **ИЗМЕНИТЬ** — HarvestableSpawner | P2 |

---

## 11. Анализ системы времени — резюме

**TimeController работает полностью.** Это НЕ заглушка.

**Почему не видно тиковое время:**
1. FormattedTime = `HH:MM` — секунды не отображаются
2. `[SerializeField] TMP_Text timeText` — может быть не назначен в Inspector
3. OnTick событие есть, но нигде не визуализируется

**Рекомендации:**
- Добавить секунды в FormattedTime: `HH:MM:SS`
- Или вывести CurrentTick в UI
- Проверить назначение timeText в FullSceneBuilder / сцене
- Отдельная задача — не входит в текущую итерацию добычи

---

*Создано: 2026-04-15 18:04:04 UTC*  
*Редактировано: 2026-04-15 18:18:27 UTC — v3 с учётом замечаний пользователя*

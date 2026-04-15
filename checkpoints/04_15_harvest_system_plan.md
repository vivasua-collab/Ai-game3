# Чекпоинт: План системы добычи (Harvest System) — v2

**Дата:** 2026-04-15 18:07:23 UTC  
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
Текущая AttemptHarvest() ищет объекты через **тайловые координаты** (TileMapController.GetTile).  
Статичные объекты (деревья, руды, камни) отрисовываются через Tilemap как тайлы,  
**не имеют GameObject с коллайдерами** → нельзя обнаружить через Physics2D.  
ResourceSpawner спавнит ресурсы как отдельные GameObject (без связи с TileData).

---

## 2. Два типа ресурсов

| Тип | Примеры | Взаимодействие | Компонент | Спавн |
|-----|---------|----------------|-----------|-------|
| **Разбросанные** | Травы, ягоды, грибы, кристаллы | Автоподбор при подходе | `ResourcePickup` | ResourceSpawner (существующий) |
| **Статичные** | Деревья, руды, камни, кусты | Добыча через F (harvest) | `Harvestable` (новый) | HarvestableSpawner (новый) |

---

## 3. Архитектурные решения

### 3.1 Спавн: Полный переход на GameObject (Вариант C)

**Решение:** Убрать harvestable-объекты из `objectTilemap` полностью.  
Спавнить все harvestable как GameObject (как уже делает ResourceSpawner).  
`objectTilemap` использовать только для не-harvestable объектов (стены, декорации).

**Обоснование:**
- ~500 простых GameObject с одним спрайтом и одним коллайдером — нормальная нагрузка для Unity 2D
- Единообразие с ResourceSpawner
- Чистая архитектура, нет дублирования логики
- Прямое взаимодействие через Physics2D

**Альтернативы (отклонены):**
- A) Tilemap + отдельный Harvestable GameObject → двойное управление, рассинхрон
- B) Tilemap для визуала + прозрачные overlay-коллайдеры → сложно, хрупко

### 3.2 Harvestable — независимый MonoBehaviour (Вариант B)

**Решение:** `Harvestable` наследует `MonoBehaviour`, НЕ наследует `Interactable`.

**Обоснование:**
- `Interactable` ориентирован на NPC-взаимодействия (разговор, торговля, дуэль)
- Для добычи абстракция Interactable избыточна
- Простой класс с CircleCollider2D и тегом `"Harvestable"` проще и легче
- `PlayerController` ищет через `Physics2D.OverlapCircleAll` по LayerMask `"Harvestable"`

**Альтернатива (отклонена):** Наследование от Interactable — overhead, расширение enum InteractionType ради одного типа.

### 3.3 Модель добычи: мгновенный лут за удар (Модель A)

**Решение:** Каждое нажатие F = один удар + мгновенная порция лута.

**Механика:**
1. Нажал F → кулдаун 0.8с (уже есть в PlayerController)
2. Уменьшение `currentDurability` на `harvestDamage`
3. Мгновенное добавление `harvestYield` единиц ресурса в инвентарь
4. Визуальная обратная связь (прогресс-бар, текст «+N ресурс»)
5. Когда `currentDurability ≤ 0` → объект исчерпан (depleted)

**Обоснование:** Ощущения как в Valheim/Terraria. Проще чем канальная модель (hold F).

### 3.4 Расчёт лута за удар

```
harvestYield = ceil(resourceCount * harvestDamage / maxDurability)
```

Но не больше остатка: `min(harvestYield, remainingResources)`

| Объект | maxDur | damage | resourceCount | ударов | за удар |
|--------|--------|--------|---------------|--------|---------|
| Дерево | 200 | 25 | 10 | 8 | 1–2 wood |
| Руда | 400 | 25 | 15 | 16 | 1 ore |
| Камень | 300 | 25 | 10 | 12 | 1 stone |
| Куст | 50 | 25 | 5 | 2 | 2–3 berries |
| Трава | 10 | 25 | 1 | 1 | 1 herb |

**Примечание:** Если harvestDamage ≥ maxDurability — один удар, полный лут.

---

## 4. Компонент `Harvestable`

### 4.1 Поля

```
[Header("Data")]
string resourceId           // "wood", "stone", "ore", "herb", "berries"
string displayName          // "Древесина", "Руда" (для UI)
int resourceCount           // Общее количество ресурса в объекте
int maxDurability           // Максимальная прочность
int currentDurability       // Текущая прочность

[Header("Harvest Settings")]
int harvestDamage = 25      // Урон за удар (базовый, модифицируется инструментом)
float harvestCooldown = 0.8f // Кулдаун между ударами (секунды)
float harvestRange = 2.5f   // Дальность добычи (проверяется в PlayerController)

[Header("Respawn")]
bool enableRespawn = true
float respawnTime = 120f    // Секунды до респауна (0 = нет респауна)
bool isDepleted = false

[Header("Visuals")]
Sprite normalSprite         // Нормальный спрайт (дерево, руда)
Sprite depletedSprite       // Спрайт после исчерпания (пень, пустая жила)
```

### 4.2 Методы

```
bool CanHarvest()           // !isDepleted && currentDurability > 0
int HarvestHit(int damage)  // Один удар, возвращает количество полученного ресурса
void Deplete()              // Исчерпание объекта
void Respawn()              // Респаун
void Initialize(TileObjectData) // Инициализация из данных тайла
```

### 4.3 События

```
event Action<Harvestable, int> OnHarvested     // (объект, количество полученного ресурса)
event Action<Harvestable> OnDepleted           // Объект исчерпан
event Action<Harvestable> OnRespawned          // Объект респавнился
```

### 4.4 Требования к GameObject

- `SpriteRenderer` — спрайт объекта
- `CircleCollider2D` — isTrigger=true, для обнаружения через Physics2D
- Слой `"Harvestable"` — для LayerMask фильтрации
- Тег `"Harvestable"` — для быстрой идентификации

---

## 5. Поток добычи (детально)

```
Игрок нажимает F
  │
  ├─ PlayerController.ProcessInput() → Keyboard.current.fKey.wasPressedThisFrame
  │
  ├─ PlayerController.AttemptHarvest()
  │   ├─ Проверка кулдауна (Time.time - lastHarvestTime < harvestCooldown)
  │   ├─ Поиск ближайшего Harvestable через Physics2D.OverlapCircleAll
  │   │   ├─ Радиус: harvestRange (2.5 юнита)
  │   │   ├─ LayerMask: "Harvestable"
  │   │   └─ Сортировка по расстоянию, берём ближайший
  │   │
  │   ├─ Если не найден → ShowHarvestFailed("Рядом нет ресурсов")
  │   │
  │   ├─ Если найден → harvestable.CanHarvest()?
  │   │   ├─ Нет → ShowHarvestFailed("Нельзя добыть")
  │   │   └─ Да → harvestable.HarvestHit(harvestDamage)
  │   │       ├─ Уменьшает currentDurability на damage
  │   │       ├─ Рассчитывает harvestYield
  │   │       ├─ Добавляет ресурс в InventoryController.AddItem()
  │   │       ├─ Создаёт ItemData через CreateTemporaryItemData() (из ResourcePickup)
  │   │       ├─ Показывает HarvestFeedbackUI ("+N ресурс")
  │   │       ├─ lastHarvestTime = Time.time
  │   │       └─ Если currentDurability ≤ 0 → Deplete()
  │   │
  │   └─ Добавляет опыт StatType.Strength (+0.5 за удар)
  │
  └─ Конец
```

---

## 6. Спавн Harvestable объектов

### 6.1 Новый подход: HarvestableSpawner

Вместо модификации TileMapController.RenderMap(), создаём отдельный спавнер  
(аналогично ResourceSpawner):

```
HarvestableSpawner
  ├─ Подписывается на TileMapController.OnMapGenerated
  ├─ Читает TileMapData
  ├─ Для каждого тайла с isHarvestable объектом:
  │   ├─ Создаёт GameObject с SpriteRenderer + CircleCollider2D + Harvestable
  │   ├─ Инициализирует Harvestable данными из TileObjectData
  │   └─ НЕ добавляет объект в objectTilemap
  └─ Управляет респауном depleted объектов
```

### 6.2 Интеграция с TileMapController

**Проблема:** TileMapController.RenderMap() ставит harvestable-объекты в objectTilemap.  
Если HarvestableSpawner тоже спавнит GameObject — будет двойной рендер (тайл + спрайт).

**Решение:** В `TileMapController.RenderMap()` **пропускать** объекты с `isHarvestable=true`.  
Не ставить их в objectTilemap. HarvestableSpawner создаст их как GameObject.

Для этого в `RenderMap()` нужна проверка:
```
if (obj.isHarvestable) continue; // Пропустить — спавнится HarvestableSpawner'ом
```

### 6.3 Проходимость (collision)

Когда Harvestable не исчерпан — он непроходим (дерево, камень).  
Нужен **Rigidbody2D (Static)** + **BoxCollider2D (не trigger)** для физической блокировки.  
Плюс **CircleCollider2D (trigger)** для обнаружения через Physics2D.

**Итого на одном GameObject:**
1. `SpriteRenderer` — визуал
2. `BoxCollider2D` — физический коллайдер (блокировка прохода)
3. `CircleCollider2D` (isTrigger) — зона обнаружения для Physics2D
4. `Harvestable` — логика добычи
5. `Rigidbody2D` (Static) — для корректной работы физики с BoxCollider2D

### 6.4 Маппинг TileObjectType → параметры Harvestable

| TileObjectType | resourceId | displayName | maxDur | resourceCount | respawnTime | depleted visual |
|----------------|-----------|-------------|--------|---------------|-------------|-----------------|
| Tree_Oak | wood | Древесина | 200 | 10 | 180с | Пень |
| Tree_Pine | wood | Древесина | 200 | 10 | 180с | Пень |
| Tree_Birch | wood | Древесина | 150 | 8 | 180с | Пень |
| Rock_Small | stone | Камень | 100 | 3 | 120с | Обломки |
| Rock_Medium | stone | Камень | 300 | 10 | 180с | Обломки |
| OreVein | ore | Руда | 400 | 15 | 300с | Пустая жила |
| Bush | berries | Ягоды | 50 | 5 | 90с | Сухой куст |
| Bush_Berry | berries | Ягоды | 50 | 5 | 90с | Сухой куст |
| Herb | herb | Целебная трава | 10 | 1 | 60с | Нет спрайта |

**Примечание:** Herb имеет maxDurability=10 и harvestDamage=25 → один удар = полный сбор.  
Это по сути автоподбор, но через F-взаимодействие. Альтернатива: для Herb  
оставить ResourcePickup (автоподбор), не создавать Harvestable.

---

## 7. UI: Подсказка добычи

### 7.1 Отображение

Когда игрок в радиусе `harvestRange` от Harvestable:
- Над объектом текст: **«F — Добыть [displayName]»**
- Жёлтый цвет, WorldSpace Canvas
- При нажатии F → прогресс-бар + текст «+N ресурс»

### 7.2 Реализация

Расширить `HarvestFeedbackUI`:
- Новый метод `ShowHarvestPrompt(string resourceName)` — показывает подсказку
- Обновлять каждый кадр: проверять ближайший Harvestable, показывать/скрывать подсказку
- Вызывать из `PlayerController.Update()` или отдельного `HarvestPromptController`

---

## 8. Physics-слой "Harvestable"

**Требуется добавить в Unity:**  
Project Settings → Tags and Layers → Layers → добавить слой "Harvestable" (номер выбрать свободный, например 8)

**В коде:** `LayerMask harvestableLayer = LayerMask.GetMask("Harvestable");`

**Назначение:** при спавне Harvestable GameObject устанавливать `go.layer = LayerMask.NameToLayer("Harvestable");`

---

## 9. Совместимость с существующими системами

### 9.1 DestructibleObjectController

**Текущая роль:** обрабатывает урон по TileObjectData, создаёт дроп через ResourcePickup.

**После внедрения Harvestable:**  
DestructibleObjectController больше НЕ используется для harvestable-объектов.  
Он остаётся для не-harvestable разрушаемых объектов (стены, мебель).

### 9.2 ResourcePickup

**Без изменений.** Продолжает работать для разбросанных ресурсов (травы, ягоды от ResourceSpawner).

**Разделение:**
- ResourceSpawner → ResourcePickup → автоподбор (мелкие ресурсы)
- HarvestableSpawner → Harvestable → F-добыча (крупные объекты)

### 9.3 ResourceSpawner vs HarvestableSpawner

**Разделение ответственности:**
- `ResourceSpawner` — мелкие разбросанные ресурсы (herb, berries, mushroom, qi_crystal, spirit_stone, sand_pearl, desert_crystal) с автоподбором
- `HarvestableSpawner` — статичные объекты (деревья, камни, руды, кусты) с F-добычей

**Проблема:** сейчас ResourceSpawner спавнит ALL ресурсы, включая stone и ore.  
Нужно убрать harvestable-типы из ResourceSpawner.SetDefaultResourceTypes():
- Убрать: stone, ore, iron_ore, wood
- Оставить: herb, berries, mushroom, rare_herb, qi_crystal, spirit_stone, sand_pearl, desert_crystal

---

## 10. План реализации (пошаговый)

### Шаг 1: Создать Harvestable.cs
- MonoBehaviour с полями из раздела 4.1
- Методы: CanHarvest(), HarvestHit(), Deplete(), Respawn(), Initialize()
- События: OnHarvested, OnDepleted, OnRespawned
- Логика расчёта лута за удар
- Таймер респауна через coroutine

### Шаг 2: Создать HarvestableSpawner.cs
- Подписка на TileMapController.OnMapGenerated
- Обход TileMapData, создание GameObject для isHarvestable объектов
- Маппинг TileObjectType → параметры (таблица из 6.4)
- Загрузка AI-спрайтов (как в ResourceSpawner.LoadResourceSprite)
- Создание depleted-спрайтов (программных fallback)
- Родительский объект "Harvestables" для иерархии сцены

### Шаг 3: Модифицировать PlayerController.AttemptHarvest()
- Поиск Harvestable через Physics2D.OverlapCircleAll по LayerMask "Harvestable"
- Сортировка по расстоянию, ближайший
- Вызов Harvestable.HarvestHit()
- Обратная связь через HarvestFeedbackUI
- Опыт Strength за удар

### Шаг 4: Модифицировать TileMapController.RenderMap()
- Пропускать объекты с isHarvestable=true (не ставить в objectTilemap)
- Это устраняет двойной рендер

### Шаг 5: Модифицировать ResourceSpawner.SetDefaultResourceTypes()
- Убрать harvestable-типы: stone, ore, iron_ore, wood
- Оставить только разбросанные ресурсы с автоподбором

### Шаг 6: Интеграция в TestLocationGameController
- Создание/настройка HarvestableSpawner
- Подписка на события Harvestable (для UI, логов)

### Шаг 7: UI подсказки
- Расширить HarvestFeedbackUI: метод ShowHarvestPrompt()
- В PlayerController.Update(): сканирование ближайшего Harvestable для подсказки
- Отображение «F — Добыть [name]» над объектом

### Шаг 8: Добавить Physics-слой "Harvestable"
- Инструкция для Unity Editor (Layer 8 = "Harvestable")
- В коде спавнера: go.layer = LayerMask.NameToLayer("Harvestable")

---

## 11. Файлы для изменения

| Файл | Действие | Приоритет |
|------|----------|-----------|
| `Scripts/Tile/Harvestable.cs` | **СОЗДАТЬ** — компонент добычи | P0 |
| `Scripts/Tile/HarvestableSpawner.cs` | **СОЗДАТЬ** — спавнер harvestable объектов | P0 |
| `Scripts/Player/PlayerController.cs` | **ИЗМЕНИТЬ** — AttemptHarvest через Physics2D | P0 |
| `Scripts/Tile/TileMapController.cs` | **ИЗМЕНИТЬ** — RenderMap пропускает isHarvestable | P0 |
| `Scripts/Tile/ResourceSpawner.cs` | **ИЗМЕНИТЬ** — убрать harvestable-типы | P1 |
| `Scripts/UI/HarvestFeedbackUI.cs` | **ИЗМЕНИТЬ** — метод ShowHarvestPrompt | P1 |
| `Scripts/World/TestLocationGameController.cs` | **ИЗМЕНИТЬ** — настройка HarvestableSpawner | P1 |

---

## 12. Риски и открытые вопросы

1. **Производительность:** ~500 GameObject вместо Tilemap-объектов. Unity 2D справится, но при расширении карты (>1000 объектов) может потребоваться пул/оптимизация.

2. **Двойной рендер:** Если забыть пропустить isHarvestable в RenderMap(), объект будет виден дважды (тайл + спрайт). Нужно тщательно протестировать.

3. **Depleted-спрайты:** Нет готовых спрайтов для пня/пустой жилы. Нужны либо AI-генерация, либо программные fallback (простые формы).

4. **Сохранение состояния:** Harvestable состояние (depleted, currentDurability) не сохраняется в SaveFile. Нужно добавить в систему сохранения в будущем.

5. **Инструменты:** Сейчас harvestDamage=25 фиксированный. В будущем — модификатор от экипированного инструмента (топор ×2 для деревьев, кирка ×2 для руды).

---

*Создано: 2026-04-15 18:04:04 UTC*  
*Редактировано: 2026-04-15 18:07:23 UTC*

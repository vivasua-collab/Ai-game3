# Чекпоинт: План системы добычи (Harvest System)

**Дата:** 2026-04-15 18:04:04 UTC  
**Статус:** in_progress  
**Цель:** Реализовать систему добычи для статичных объектов (руды, деревья, растения) через кнопку F

---

## Текущее состояние

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

Кроме того, текущий код предполагает что объекты = данные в `TileData.objects`,  
но ResourceSpawner спавнит ресурсы как отдельные GameObject (без связи с TileData).

---

## Архитектурное решение

### Два типа ресурсов

| Тип | Примеры | Способ взаимодействия | Компонент |
|-----|---------|----------------------|-----------|
| **Разбросанные** | Травы, ягоды, грибы, кристаллы | Автоподбор / подход | `ResourcePickup` (существующий) |
| **Статичные** | Деревья, руды, камни, кусты | Добыча через F (harvest) | `Harvestable` (новый) |

### Ключевой компонент: `Harvestable`

```csharp
// Наследник Interactable — для совместимости с InteractionController
public class Harvestable : Interactable
{
    // Данные
    string resourceId;        // "wood", "stone", "ore", "herb", "berries"
    int harvestAmount;         // Количество за одну добычу
    float harvestTime;         // Время одного удара (секунды)
    int harvestDamage;         // Урон по durability за удар
    
    // Прочность
    int maxDurability;
    int currentDurability;
    
    // Респаун
    float respawnTime;         // Время респауна после исчерпания
    
    // Состояние
    bool isDepleted;           // Исчерпан?
    
    // Визуал
    Sprite normalSprite;
    Sprite depletedSprite;
    
    // Методы
    bool CanHarvest(PlayerController player);
    void OnHarvestHit(PlayerController player);  // Один удар
    void OnDepleted();                           // Исчерпание
    void Respawn();                              // Респаун
}
```

### Поток добычи

1. **Игрок нажимает F**
2. `PlayerController.AttemptHarvest()` ищет ближайший `Harvestable` в радиусе через `Physics2D.OverlapCircleAll`
3. Если найден — вызывает `harvestable.OnHarvestHit(player)`
4. `Harvestable.OnHarvestHit()`:
   - Проверяет кулдаун
   - Уменьшает `currentDurability` на `harvestDamage`
   - Добавляет `resourceId × harvestAmount` в инвентарь через `InventoryController.AddItem()`
   - Показывает визуальную обратную связь через `HarvestFeedbackUI`
   - Если `currentDurability <= 0` → `OnDepleted()`
5. `Harvestable.OnDepleted()`:
   - Меняет спрайт на depleted (пень / пустая жила)
   - Отмечает `isDepleted = true`
   - Запускает таймер респауна
6. `Harvestable.Respawn()`:
   - Восстанавливает durability
   - Возвращает нормальный спрайт

### Спавн Harvestable объектов

Вместо модификации TileMapController (который отрисовывает через Tilemap),  
создаём отдельный `HarvestableObjectSpawner` который:
- Читает `TileMapData` после генерации карты
- Для каждого объекта с `isHarvestable=true` создаёт GameObject с `Harvestable`
- НЕ добавляет объект в objectTilemap (убирает дублирование)
- Или: добавляет ВМЕСТО objectTilemap (префабы вместо тайлов)

**Проще:** Модифицировать `TileMapController.RenderMap()` —  
при отрисовке объектов с `isHarvestable=true` создавать GameObject с Harvestable  
вместо размещения тайла в objectTilemap.

---

## План реализации

### Шаг 1: Создать Harvestable.cs
- Компонент для статичных добываемых объектов
- Синхронизация с TileObjectData (опционально)
- Визуальная обратная связь (прогресс, урон)
- Респаун после исчерпания

### Шаг 2: Модифицировать PlayerController.AttemptHarvest()
- Искать `Harvestable` через Physics2D вместо тайловых координат
- Использовать `Harvestable.CanHarvest()` для проверки
- Вызывать `Harvestable.OnHarvestHit()` вместо DestructibleObjectController

### Шаг 3: Модифицировать TileMapController.RenderMap()
- При отрисовке объектов с `isHarvestable=true`:
  - Создавать GameObject с SpriteRenderer + Collider2D + Harvestable
  - НЕ размещать в objectTilemap (или размещать, но с прозрачным тайлом)
- Для не-добываемых объектов — как раньше через Tilemap

### Шаг 4: UI подсказки
- Показывать "F — Добыть руду" когда рядом есть Harvestable
- Подсветка ближайшего объекта

### Шаг 5: Интеграция и тестирование
- Убедиться что ресурсы добавляются в инвентарь
- Проверить респаун
- Проверить совместимость с ResourcePickup

---

## Связь с TileObjectData

Есть два подхода:

**A) Синхронизация:** Harvestable хранит ссылку на TileObjectData,  
при уроне обновляет currentDurability в обоих местах.  
При разрушении — вызывает tile.RemoveObject().

**B) Независимость:** Harvestable не зависит от TileObjectData.  
Состояние хранится только в Harvestable.  
TileObjectData используется только для генерации карты.

Выбран **подход B** — проще, меньше связей, нет проблем с рассинхронизацией.  
TileObjectData используется при генерации карты для определения позиции и свойств объектов.  
После спавна Harvestable — объект живёт независимо.

---

## Файлы для изменения

| Файл | Действие |
|------|----------|
| `Scripts/Tile/Harvestable.cs` | **СОЗДАТЬ** — компонент добычи |
| `Scripts/Player/PlayerController.cs` | **ИЗМЕНИТЬ** — AttemptHarvest через Physics2D |
| `Scripts/Tile/TileMapController.cs` | **ИЗМЕНИТЬ** — RenderMap спавнит Harvestable |
| `Scripts/UI/HUDController.cs` | **ИЗМЕНИТЬ** — подсказка "F — Добыть" |
| `Scripts/World/TestLocationGameController.cs` | **ИЗМЕНИТЬ** — подписка на события Harvestable |

---

*Создано: 2026-04-15 18:04:04 UTC*

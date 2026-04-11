# Чекпоинт: Устранение дублирования namespace + ResourcePickup + GameTile API

**Дата:** 2026-04-11 08:30:41 UTC
**Фаза:** 7 — Интеграция
**Статус:** complete

## Выполненные задачи
- [x] FIX: GameTile.cs — добавлена #if директива для совместимости Unity 5/6 API (CS0115)
- [x] FIX TIL-H02: TileSystem.DamageType → TileDamageType (устранение CS0104 конфликта с Core.DamageType)
- [x] FIX CORE-H01: Core.TerrainType → BiomeType (устранение CS0104 конфликта с TileSystem.TerrainType)
- [x] FIX TIL-H01: ResourcePickup.TryAddToInventory() — реальный вызов InventoryController.AddItem()
- [x] FIX NPC-M02: NPCAssemblyExample.cs — #pragma warning disable/restore CS0612 для Disposition
- [x] Обновлён CODE_REFERENCE.md с учётом всех переименований

## Изменённые файлы
- Assets/Scripts/Tile/GameTile.cs — #if UNITY_2023_2_OR_NEWER || UNITY_6000_0_OR_NEWER для GetTileData
- Assets/Scripts/Tile/DestructibleSystem.cs — DamageType → TileDamageType (enum + все ссылки)
- Assets/Scripts/Tile/DestructibleObjectController.cs — DamageType → TileDamageType (4 метода + 1 событие)
- Assets/Scripts/Core/Enums.cs — TerrainType → BiomeType
- Assets/Scripts/Data/ScriptableObjects/LocationData.cs — TerrainType → BiomeType
- Assets/Scripts/World/LocationController.cs — TerrainType → BiomeType (2 места)
- Assets/Scripts/Tile/ResourcePickup.cs — Реальная интеграция с InventoryController.AddItem()
- Assets/Scripts/Examples/NPCAssemblyExample.cs — #pragma warning для Disposition (3 места)
- docs_temp/CODE_REFERENCE.md — Обновлена таблица enum и раздел DUPLICATE NAMES

## Технические детали

### GameTile.cs (CS0115)
- Unity 6 (2023.2+) использует `Tilemap` вместо `ITilemap` в `GetTileData()`
- Добавлена #if директива для совместимости с обеими версиями Unity API
- Полная квалификация `UnityEngine.Tilemaps.TileData` для избежания конфликта с `CultivationGame.TileSystem.TileData`

### DamageType → TileDamageType
- `CultivationGame.Core.DamageType` (5 значений: Physical, Qi, Elemental, Pure, Void) — оставлен для боевой системы
- `CultivationGame.TileSystem.TileDamageType` (7 значений: Physical, Slashing, Piercing, Blunt, Energy, Fire, Explosive) — для разрушения тайлов
- Затронуты: DestructibleSystem.cs (определение + все использования), DestructibleObjectController.cs

### TerrainType → BiomeType
- `CultivationGame.Core.BiomeType` (10 биомов: Mountains, Plains, Forest, Sea, Desert, Swamp, Tundra, Jungle, Volcanic, Spiritual) — для мировой карты
- `CultivationGame.TileSystem.TerrainType` (11 поверхностей: None, Grass, Dirt, Stone, etc.) — для тайлов (без изменений)
- Затронуты: Enums.cs, LocationData.cs, LocationController.cs

### ResourcePickup
- Добавлено поле `[SerializeField] private ItemData itemData`
- Добавлена перегрузка `Initialize(string, int, ItemData)`
- TryAddToInventory теперь вызывает `inventory.AddItem(itemData, amount)`
- Fallback: поиск ItemData через `Resources.LoadAll<ItemData>("Items")`
- Если ItemData не найден — предупреждение + предмет всё равно подбирается

## Оставшиеся проблемы (не блокирующие)
- [ ] FactionType/FactionRank дублирование (Data.ScriptableObjects vs World)
- [ ] FormationType/BuffType дублирование (Data.ScriptableObjects vs Formation)
- [ ] EffectType дублирование (TechniqueData vs TechniqueEffectFactory)

# Чекпоинт: После исправлений FullSceneBuilder Build Log

**Дата:** 2026-04-13 14:18:00 UTC  
**Коммит HEAD:** ec96881  
**Статус:** ✅ Исправлено (требует повторной сборки на локальном ПК)

---

## Контекст

После запуска FullSceneBuilder (15 фаз) на локальном ПК были обнаружены критические ошибки.
Коммиты 0c90cd6 и ec96881 исправляют эти ошибки.

---

## Что было исправлено

### 1. "No script asset for TerrainTile/ObjectTile" (CRITICAL)
- **Причина:** TerrainTile и ObjectTile были определены внутри GameTile.cs.
  Unity требует совпадение имени файла и класса для ScriptableObject с `[CreateAssetMenu]`.
- **Исправление:** Вынесены в отдельные файлы:
  - `Assets/Scripts/Tile/TerrainTile.cs`
  - `Assets/Scripts/Tile/ObjectTile.cs`
- **Коммит:** ec96881

### 2. "No script asset for EquipmentData/MaterialData" (CRITICAL)
- **Причина:** EquipmentData и MaterialData были определены внутри ItemData.cs.
- **Исправление:** Вынесены в отдельные файлы:
  - `Assets/Scripts/Data/ScriptableObjects/EquipmentData.cs`
  - `Assets/Scripts/Data/ScriptableObjects/MaterialData.cs`
- **Коммит:** ec96881

### 3. "type is not a supported int value" (CRITICAL)
- **Причина:** SetProperty() в FullSceneBuilder использовал `intValue` для enum-полей.
  Для enum нужно `enumValueIndex`.
- **Исправление:** Добавлен метод `SetEnumProperty()` и использован для:
  - `currentTimeSpeed` (TimeSpeed enum)
  - `bodyMaterial` (BodyMaterial enum)
  - `coreQuality` (CoreQuality enum)
- **Коммит:** ec96881

### 4. Missing Prefab в сцене (HIGH)
- **Причина:** Сцена содержала сломанную ссылку на Player prefab (guid 797f74407a27bd340af9a9a51b31b0bc).
- **Исправление:** Добавлен `CleanMissingPrefabs()` в `EnsureSceneOpen()`.
- **Коммит:** ec96881

### 5. Новые тайл-спрайты (MEDIUM)
- **Добавлены:** Ice, Lava (террейн) + OreVein, Herb (объекты)
- **Причина:** TerrainType enum содержит Ice=8, Lava=9, но спрайтов для них не было.
  TileObjectType содержит OreVein=520, Herb=530.
- **Коммит:** ec96881

---

## Текущая архитектура FullSceneBuilder

### 15 фаз (версия 1.2)

| # | Фаза | Что делает |
|---|------|------------|
| 01 | Folders | Создаёт 33 папки в Assets/ |
| 02 | Tags & Layers | Настраивает 5 тегов + 7 слоёв |
| 03 | Create Scene | Создаёт Main.unity |
| 04 | Camera & Light | Orthographic камера + Directional Light |
| 05 | GameManager | GameManager + GameInitializer + Systems (8 контроллеров) |
| 06 | Player | Player с 8 компонентами + SpriteRenderer |
| 07 | UI | Canvas + HUD (3 бара + тексты) + EventSystem |
| 08 | Tilemap | Grid + Terrain/Objects Tilemaps + TileMapController |
| 09 | Generate Assets | Вызов AssetGenerator + AssetGeneratorExtended + FormationAssetGenerator |
| 10 | Tile Sprites | Вызов TileSpriteGenerator (10 terrain + 7 object = 17 тайлов) |
| 11 | UI Prefabs | Вызов FormationUIPrefabsGenerator (3 префаба) |
| 12 | TMP Essentials | Проверка/импорт TextMeshPro |
| 13 | Save Scene | Сохранение сцены |
| 14 | Create & Assign Tile Assets | Создание TerrainTile/ObjectTile .asset + назначение в TileMapController |
| 15 | Configure Test Location | Настройка камеры + TestLocationGameController |

### Сгенерированные .asset файлы

**Террейны (7):** Grass, Dirt, Stone, WaterShallow, WaterDeep, Sand, Void  
**Объекты (5):** Tree, RockSmall, RockMedium, Bush, Chest

> ⚠️ Спрайты для Ice, Lava, OreVein, Herb уже генерируются (Phase 10),
> но Phase 14 ещё НЕ создаёт для них .asset файлы.
> Требуется обновление Phase 14 для полного покрытия TerrainType enum.

---

## Известные нерешённые проблемы

### Ошибки (требуют повторной сборки для проверки)
1. **TMP Essentials import failed** — NullReferenceException. Требует ручного импорта через Window → TextMeshPro → Import TMP Essentials.
2. **CS0618 Obsolete warnings** — Disposition→Attitude+PersonalityTrait, SkillLevels→Get/SetSkillLevels(), ConsumableEffect.value→valueLong/valueFloat. Предупреждения, не ошибки.
3. **Phase 14 не покрывает Ice/Lava/OreVein/Herb** — .asset файлы не создаются для новых спрайтов.

### Предупреждения (не блокируют)
- CS0067: unused events (несколько файлов)
- CS0414: unused fields (несколько файлов)
- CS0219: unused variables (несколько файлов)

---

## Git Log (последние 5 коммитов)

```
ec96881 FIX: ScriptableObject file naming + enum SetProperty + new tile sprites
0c90cd6 FEAT: Phase 14-15 tile assets + enhanced test location
090baed FIX: чёрный экран + отсутствие движения — 4 критических бага
b26b084 fix: замена UnityEngine.Input на Input System (6 файлов)
e26e751 docs: добавлена инструкция FullSceneBuilder в docs_asset_setup/README.md
```

---

## Следующие шаги

1. Повторная сборка на локальном ПК для проверки исправлений
2. Обновление Phase 14 для покрытия Ice/Lava/OreVein/Herb .asset файлов
3. Индексация всех спрайтов проекта → `docs/SPRITE_INDEX.md`
4. Устранение CS0618 Obsolete warnings при удобном случае

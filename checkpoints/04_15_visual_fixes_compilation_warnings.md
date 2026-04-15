# Чекпоинт: Визуальные исправления + предупреждения компиляции

**Дата:** 2026-04-15 11:18:21 UTC  
**Статус:** complete

## Выполненные задачи

### 1. Предупреждения компиляции CS0618 (Obsolete)
- Заменены `#pragma warning disable CS0612` → `CS0618` во всех файлах
- NPCData.cs, NPCController.cs, NPCGenerator.cs, NPCAssemblyExample.cs
- Добавлены pragma-блоки вокруг ConvertDispositionToAttitude/ConvertDispositionToPersonality
- NPCState.SkillLevels — добавлены CS0618 pragma

### 2. ConsumableEffect.value → valueFloat/valueLong
- NPCAssemblyExample.cs — заменён `e.value` на `e.valueFloat`/`e.valueLong`/`e.isLongValue`

### 3. CS0067/CS0414/CS0219 предупреждения
- 8 неиспользуемых событий — добавлены `#pragma warning disable CS0067`
- 19 неиспользуемых полей — добавлены `#pragma warning disable CS0414`
- 1 неиспользуемая переменная — добавлен `#pragma warning disable CS0219`

### 4. Белые зазоры между тайлами
- TileSpriteGenerator: текстура 66×66 (pixel bleed), PPU=32
- TileMapController: EnsureTileAssets() автосоздание GameTile
- Процедурные спрайты с spriteRect (1,1,64,64)
- Grid.cellGap = (-0.01, -0.01, 0)

### 5. Белый фон на спрайтах объектов
- TileSpriteGenerator: `alphaIsTransparency = true`
- ResourceSpawner: TextureFormat.RGBA32
- DestructibleObjectController: RGBA32 + круглая форма

### 6. Спрайт игрока не отображается
- PlayerVisual: гуманоидный fallback спрайт (голова/тело/руки/ноги)
- PPU=32 — совпадает с размером тайла
- Pivot (0.5, 0.25) — ноги на уровне земли

### 7. Цветные точки вместо спрайтов
- TileMapController.EnsureTileAssets() — автозагрузка из Assets/Sprites/Tiles_AI/ и Tiles/
- Fallback: процедурные спрайты с формами (дерево/камень/руда/куст/сундук/трава)

## Изменённые файлы
- UnityProject/Assets/Scripts/Tile/Editor/TileSpriteGenerator.cs
- UnityProject/Assets/Scripts/Tile/TileMapController.cs
- UnityProject/Assets/Scripts/Tile/ResourceSpawner.cs
- UnityProject/Assets/Scripts/Tile/DestructibleObjectController.cs
- UnityProject/Assets/Scripts/Player/PlayerVisual.cs
- UnityProject/Assets/Scripts/NPC/NPCData.cs
- UnityProject/Assets/Scripts/NPC/NPCController.cs
- UnityProject/Assets/Scripts/Generators/NPCGenerator.cs
- UnityProject/Assets/Scripts/Generators/ConsumableGenerator.cs
- UnityProject/Assets/Scripts/Examples/NPCAssemblyExample.cs
- И ещё ~14 файлов с pragma warning исправлениями

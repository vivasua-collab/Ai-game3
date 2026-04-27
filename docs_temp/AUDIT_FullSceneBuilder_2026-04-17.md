# Аудит FullSceneBuilder.cs v1.3 — Полный отчёт

**Дата:** 2026-04-17 14:20 UTC  
**Версия:** 1.3 (FROZEN)  
**Строк:** 2603  
**Фаз:** 15  
**Статус:** ЗАМОРОЖЕН — исправления только через ScenePatchBuilder.cs

---

## Резюме

| Критичность | Количество | Исправимо через патч? |
|-------------|-----------|----------------------|
| CRITICAL | 4 | 2 да / 2 требуют модификации FullSceneBuilder |
| HIGH | 6 | 4 да / 2 частично |
| MEDIUM | 8 | Все да |
| LOW | 5 | Все да |
| INFO | 4 | Не требуют исправления |

---

## CRITICAL (4)

### C-01: SetProperty enum обработка некорректна для non-zero-starting enum

**Локация:** Строки 1804-1825 (SetProperty)  
**Влияние:** Если кто-то передаст `(int)CoreQuality.Normal = 4` вместо индекса 3, будет установлен Refined вместо Normal  
**Текущее состояние:** РАБОТАЕТ (используется хардкод 3), но API обманчив  

**Подробность:**  
`SetProperty(so, "coreQuality", 3)` работает корректно — 3 это индекс Normal в enumNames.  
Но если вызвать `SetProperty(so, "coreQuality", (int)CoreQuality.Normal)` = `SetProperty(so, "coreQuality", 4)`, метод установит `enumValueIndex = 4` = "Refined".  

Для BodyMaterial (начинается с 0): `(int)BodyMaterial.Organic = 0` → index 0 → корректно.  
Для CoreQuality (начинается с 1): `(int)CoreQuality.Normal = 4` → index 4 → "Refined" → НЕВЕРНО!  

**Решение:** SetProperty должен принимать enum index, а не enum value. Добавить предупреждение в комментарий + перегрузку для enum типов.

### C-02: ScenePatchBuilder PATCH-001 — нет guard на LoadAllAssetsAtPath

**Локация:** ScenePatchBuilder.cs строки 531-532  
**Влияние:** IndexOutOfRangeException если TagManager.asset пуст  

```csharp
// БАГ: нет проверки на пустой массив
var tagManager = new SerializedObject(
    AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
```

**Решение:** Использовать `LoadTagManager()` аналогично FullSceneBuilder (строка 1775-1784).

### C-03: EnsureSceneOpen() вызывает CleanMissingPrefabs() при КАЖДОМ IsNeeded-чеке

**Локация:** Строки 870-888  
**Влияние:** Побочный эффект при проверке — модификация сцены + автосохранение  

`IsCameraLightNeeded()`, `IsGameManagerNeeded()`, `IsPlayerNeeded()`, `IsUINeeded()`, `IsTilemapNeeded()`, `IsConfigureTestLocationNeeded()` — все вызывают `EnsureSceneOpen()`, который вызывает `CleanMissingPrefabs()` и `CleanMissingScripts()`.  

Результат: при каждом IsNeeded-чеке сцена может быть модифицирована (удалены missing scripts) и сохранена. IsNeeded ДОЛЖЕН быть read-only.  

**Решение:** Разделить EnsureSceneOpen на два метода:
- `EnsureSceneOpenRead()` — только открывает сцену, без очистки
- `EnsureSceneOpenWrite()` — открывает + чистит (использовать в Execute-методах)

### C-04: FillCircle() — нет проверки границ массива

**Локация:** Строки 2087-2106  
**Влияние:** IndexOutOfRangeException при выходе за пределы текстуры  

```csharp
pixels[y * size + x] = color;  // Если y >= size или x >= size → CRASH
```

Текущие параметры безопасны (голова: центр y=50, радиус=8, max y=58 < 64), но любое изменение параметров может вызвать краш.

**Решение:** Добавить `if (y < 0 || y >= size || x < 0 || x >= size) continue;`

---

## HIGH (6)

### H-01: IsTagsLayersNeeded() проверяет только 3 из 6 Sorting Layers

**Локация:** Строки 533-548  
**Влияние:** Если Background после Terrain или UI перед Player, чек пройдёт, но рендеринг будет сломан  

Текущая проверка: `terrainIdx < objectsIdx < playerIdx`  
Пропускает: Default, Background, UI  

**Решение:** Проверять полный порядок всех 6 слоёв:
```csharp
int[] expectedOrder = { defaultIdx, backgroundIdx, terrainIdx, objectsIdx, playerIdx, uiIdx };
for (int i = 1; i < expectedOrder.Length; i++)
    if (expectedOrder[i] <= expectedOrder[i-1] && expectedOrder[i] >= 0 && expectedOrder[i-1] >= 0)
        return true; // Порядок нарушен
```

### H-02: Duplicate "UI" physics layer — конфликт с Unity built-in

**Локация:** Строки 144-154 (REQUIRED_LAYERS)  
**Влияние:** Layer 11 "UI" дублирует Unity built-in layer 5 "UI" → LayerMask.NameToLayer("UI") возвращает 5  

REQUIRED_LAYERS определяет "UI" на позиции 11, но Unity уже имеет "UI" на позиции 5. Проверка `IsTagsLayersNeeded()` находит "UI" в списке слоёв (на позиции 5) и считает что слой есть. НО слой 11 остаётся пустым.  

Код, использующий `layer = 11` для UI-объектов, не будет работать — `NameToLayer("UI")` возвращает 5, не 11.

**Решение:** Удалить "UI" из REQUIRED_LAYERS (Unity уже имеет его на позиции 5) или переименовать в "GameUI" на позиции 11.

### H-03: IsConfigureTestLocationNeeded() — слабая проверка идемпотентности

**Локация:** Строки 2428-2449  
**Влияние:** Если камера случайно окажется на (0.5, 0.3, -10), фаза посчитает что не нужна  

Проверка `cam.transform.position.x < 1f && cam.transform.position.y < 1f` — хрупкая. После Phase 04 камера на (0,0,-10), после Phase 15 — на (100,80,-10). Но если пользователь переместит камеру на (0.5, 0.3, -10), фаза будет пропущена.

**Решение:** Использовать EditorPrefs или создать маркерный компонент для отслеживания выполнения фазы.

### H-04: ScenePatchBuilder PATCH-003/005/006/007 IsApplied возвращает true при отсутствии целевого объекта

**Локация:** ScenePatchBuilder.cs строки 705, 815, 857, 893  
**Влияние:** Патч отмечается как "применён" когда целевого объекта нет в сцене  

Пример: `IsPatch003Applied()` — `if (player == null) return true;`  
Если Player ещё не создан, патч считается применённым и не будет перепроверен.

**Решение:** Возвращать `false` если целевой объект не найден (патч нужен, но не может быть применён), либо `true` только если объект существует И в правильном состоянии.

### H-05: Camera2DSetup.boundsMin не устанавливается в Phase 04

**Локация:** Строки 933-939  
**Влияние:** BoundsMin полагается на дефолт Vector2(0,0) — работает, но неявно  

Phase 04 устанавливает `boundsMax = (200, 160)`, но не устанавливает `boundsMin`. Phase 15 устанавливает оба. Если Phase 15 пропущена, boundsMin = дефолт.

**Решение:** Добавить `camSo.FindProperty("boundsMin").vector2Value = Vector2.zero;` в Phase 04.

### H-06: Phase 14 terrain tile назначения перезаписываются при старте

**Локация:** TileMapController.cs → EnsureTileAssets() в Start()  
**Влияние:** Phase 14 тратит время на создание и назначение terrain .asset файлов, но Start() перезаписывает их процедурными  

Это НЕ баг FullSceneBuilder, но важное наблюдение: `TileMapController.EnsureTileAssets()` вызывает `ForceProceduralTerrainTile()` для ВСЕХ 10 terrain-тайлов, перезаписывая .asset ссылки. Object tiles используют `EnsureTile()` (только если null) — сохраняются.

**Решение:** Добавить комментарий в Phase 14 о том, что terrain tile назначения перезапишутся. Или изменить EnsureTileAssets на non-forced для terrain.

---

## MEDIUM (8)

### M-01: EnsureSortingLayers() — uniqueID может коллизировать

**Локация:** Строки 694, 720  
**Влияние:** Если слой был удалён, nextId может совпасть с существующим  

`nextId = sortingLayersProp.arraySize` — после удаления слоёв arraySize уменьшается, но ID удалённых слоёв не переиспользуются. Теоретически возможна коллизия с существующим ID.

**Решение:** Использовать максимальный существующий ID + 1:
```csharp
int nextId = 0;
for (int i = 0; i < sortingLayersProp.arraySize; i++)
{
    var idProp = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID");
    if (idProp != null) nextId = Mathf.Max(nextId, idProp.intValue);
}
nextId++;
```

### M-02: TMP текст создаётся до импорта TMP Essentials (Phase 07 до Phase 12)

**Локация:** Строки 1289-1324 (CreateHUDPanel)  
**Влияние:** TMP компоненты создаются без Font Asset — шрифт будет missing до ручного обновления  

Фазы выполняются последовательно: 07 (UI) → 12 (TMP Essentials). При первой сборке TMP текст создастся с missing font.

**Решение:** Переставить Phase 12 перед Phase 07, или добавить Post-Phase обновление TMP шрифтов.

### M-03: ExecuteCameraLight() — нет Undo для модификации существующей камеры

**Локация:** Строки 909-939  
**Влияние:** Undo откатит создание новых объектов, но не модификацию существующих  

`Undo.RegisterCreatedObjectUndo` вызывается только для новых объектов. Модификация позиции, orthographicSize и т.д. не регистрируется в Undo.

**Решение:** Использовать `Undo.RecordObject(cam, "Configure Camera")` перед модификацией.

### M-04: ReimportTileSprites() использует Directory.GetFiles вместо AssetDatabase

**Локация:** Строки 2155-2156  
**Влияние:** Может найти .meta файлы или временные файлы, не являющиеся спрайтами  

`Directory.GetFiles(dir, "*.png")` — не фильтрует по типу ассета.

**Решение:** Использовать `AssetDatabase.FindAssets("t:Texture2D", new[] { dir })`.

### M-05: SetProperty warning для "property not found" — недостаточно информативен

**Локация:** Строка 1795  
**Влияние:** Не показывает имя типа компонента, в котором свойство не найдено  

```csharp
Debug.LogWarning($"[FullSceneBuilder] Property '{propertyName}' not found");
```

**Решение:** Добавить тип целевого объекта:
```csharp
Debug.LogWarning($"[FullSceneBuilder] Property '{propertyName}' not found in {so.targetObject.GetType().Name}");
```

### M-06: IsSceneNeeded() — использует System.IO.File.Exists с относительным путём

**Локация:** Строки 838-845  
**Влияние:** Может не найти файл если текущая директория ≠ корень проекта  

`System.IO.File.Exists("Assets/Scenes/Main.unity")` зависит от текущей директории процесса.

**Решение:** Использовать `AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) != null` или `System.IO.File.Exists(System.IO.Path.Combine(Directory.GetCurrentDirectory(), SCENE_PATH))`.

### M-07: ExecutePlayer() — CircleCollider2D.radius = 0.5f может не совпадать с визуальным размером

**Локация:** Строка 1130  
**Влияние:** Коллайдер может быть больше или меньше спрайта персонажа  

При PPU=32 и размере текстуры 64×64, спрайт = 2 юнита. Коллайдер diameter = 1.0 юнит. Центр спрайта = pivot (0.5, 0.5), но коллайдер привязан к центру объекта. Если спрайт персонажа занимает не весь 64×64 квадрат, коллайдер будет несоразмерен.

**Решение:** Увеличить radius до 0.4-0.5 и добавить offset для совпадения с визуальным центром.

### M-08: ExecuteGenerateAssets() — нет try-catch вокруг отдельных генераторов

**Локация:** Строки 1454-1498  
**Влияние:** Если один генератор упадёт, вся фаза прервётся  

Нет обработки ошибок для `AssetGenerator.GenerateCultivationLevels()` и др.

**Решение:** Обернуть каждый вызов в try-catch, продолжать генерацию остальных ассетов при ошибке.

---

## LOW (5)

### L-01: BuildAll() — время через EditorApplication.timeSinceStartup теряет точность при long-running билдах

**Локация:** Строки 296, 332  
**Влияние:** Неточное время при билдах > 24 дней (маловероятно)  

`EditorApplication.timeSinceStartup` возвращает float (точность ~1 сек при больших значениях).

### L-02: EnsureDirectory() — Path.GetDirectoryName/GetFileName на Windows может вернуть backslash

**Локация:** Строки 1753-1754  
**Влияние:** AssetDatabase.CreateFolder может не найти родительскую папку  

На Windows `Path.GetDirectoryName("Assets/Sprites/Tiles")` может вернуть `"Assets\Sprites"`. Unity AssetDatabase использует forward slashes.

### L-03: CreateBar() — Slider.interactable = false но нет Navigation = None

**Локация:** Строка 1958  
**Влияние:** Slider может перехватывать клавиатурный ввод  

**Решение:** Добавить `slider.navigation = new Navigation { mode = Navigation.Mode.None };`

### L-04: ScenePatchBuilder PATCH-008 IsPatch008Applied — проверяет только корневые объекты

**Локация:** ScenePatchBuilder.cs строки 937-953  
**Влияние:** Missing Prefab в дочернем объекте корневого GO может быть не обнаружен  

Проверка `rootObj.name.Contains("Missing Prefab")` — только корневые. Дочерние missing prefabs пропускаются.

### L-05: Phase 08 — TileMapController и GameController как отдельные GO, а не дочерние Grid

**Локация:** Строки 1394-1428  
**Влияние:** При Undo Grid, TileMapController и GameController остаются в сцене как сироты  

Undo для Grid не удаляет TileMapController и GameController. Undo для каждого зарегистрирован отдельно, но порядок Undo обратный — сначала GameController, потом Grid. Пользователь должен нажимать Undo 3 раза.

---

## INFO (4)

### I-01: TileMapController.EnsureTileAssets() перезаписывает terrain tiles — это BY DESIGN

Phase 14 создаёт .asset файлы и назначает их в TileMapController. Но `Start()` вызывает `ForceProceduralTerrainTile()` для terrain, перезаписывая назначения. Object tiles используют `EnsureTile()` (только null) — сохраняются. Это задокументированное поведение.

### I-02: IsSaveSceneNeeded() проверяет isDirty — корректно, но может быть удивительно

После Build All с пропуском всех фаз, сцена может быть dirty по другим причинам, и Phase 13 сохранит её.

### I-03: Фазовый реестр (PHASES) использует struct — копирование при доступе

`PhaseInfo` — struct, не class. При `var phase = PHASES[i]` создаётся копия. Но поскольку PhaseInfo содержит только delegates и strings (immutable), это безопасно.

### I-04: PlayerVisual создаёт SpriteRenderer через PlayerVisual, не через FullSceneBuilder

FullSceneBuilder НЕ добавляет SpriteRenderer на Player (комментарий на строке 1142-1146). PlayerVisual создаёт дочерний "Visual" объект со SpriteRenderer в Awake(). Это корректно — дублирование SpriteRenderer конфликтует.

---

## Зависимости — полная совместимость

Все сериализованные поля, которые FullSceneBuilder назначает, существуют в целевых классах:

| Целевой класс | Проверено полей | Несовпадений |
|---------------|----------------|--------------|
| TileMapController | 21 tile + 4 общих = 25 | 0 |
| Camera2DSetup | 6 свойств | 0 |
| HarvestableSpawner | 1 (tileMapController) | 0 |
| TestLocationGameController | 1 (tileMapController) | 0 |
| DestructibleObjectController | 1 (tileMapController) | 0 |
| TimeController | 7 свойств | 0 |
| SaveManager | 7 свойств | 0 |
| PlayerController | 4 свойства | 0 |
| BodyController | 5 свойств | 0 |
| QiController | 4 свойства | 0 |
| InventoryController | 4 свойства | 0 |
| EquipmentController | 2 свойства | 0 |
| TechniqueController | 2 свойства | 0 |
| SleepSystem | 3 свойства | 0 |
| RenderPipelineLogger | 3 метода | 0 |

---

## Рекомендуемые патчи для ScenePatchBuilder

На основе аудита, следующие проблемы можно исправить патчами:

| Патч | Проблема | Критичность |
|------|----------|-------------|
| PATCH-009 | Camera2DSetup.boundsMin = (0,0) | HIGH |
| PATCH-010 | Camera Undo.RecordObject перед модификацией | MEDIUM |
| PATCH-011 | Проверка всех 6 Sorting Layers (не только 3) | HIGH |
| PATCH-012 | UI physics layer 11 "UI" → "GameUI" | HIGH |

Проблемы, требующие изменения FullSceneBuilder (критические багфиксы):

| Проблема | Почему нельзя патчем |
|----------|---------------------|
| C-01: SetProperty enum | Изменение API в FROZEN скрипте |
| C-03: EnsureSceneOpen side-effect | Изменение архитектуры EnsureSceneOpen |
| C-04: FillCircle bounds | Изменение в процедурной генерации (FROZEN) |

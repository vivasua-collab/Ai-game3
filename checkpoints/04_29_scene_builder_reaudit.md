# Чекпоинт: Повторный аудит SceneBuilder

**Дата:** 2026-04-29 12:26:58 UTC
**Статус:** complete
**Цель:** Повторная полная проверка корректности оркестратора и файлов билдера после исправлений первого аудита

---

## 1. Верификация исправлений первого аудита (04_29_scenebuilder_correction.md)

### 🔴 Критические — статус исправления

| # | Проблема | Статус | Проверка |
|---|----------|--------|----------|
| K1 | Phase16 IsNeeded() проверяет только Backpacks | ✅ ИСПРАВЛЕНО | Теперь проверяет: BACKPACKS_FOLDER, STORAGERINGS_FOLDER, BASIC_FOLDER, weapon_Sword_T1_Common.asset |
| K2 | Phase09 IsNeeded() проверяет 3/9+ типов | ⚠️ ЧАСТИЧНО | Добавлены 6 проверок (Techniques, NPCPresets, Equipment, Items, Materials, Formations), но **FormationCores пропущен** |
| K3 | Phase17/18 IsNeeded() → false при отсутствии зависимости | ✅ ИСПРАВЛЕНО | Теперь return true + LogWarning |

### 🟡 Средние — статус исправления

| # | Проблема | Статус | Проверка |
|---|----------|--------|----------|
| C1 | EnsureSceneOpen в IsNeeded (9 фаз) | ✅ ИСПРАВЛЕНО | Ни одна IsNeeded() больше не вызывает EnsureSceneOpen |
| C2 | Phase16 EnsureSceneOpen не нужен | ✅ ИСПРАВЛЕНО | Execute() не вызывает EnsureSceneOpen, есть комментарий |
| C3 | Phase08/15 дублирование GameController | ❌ НЕ ИСПРАВЛЕНО | Код дублируется |
| C4 | Phase12 интерактивная фаза | ❌ НЕ ИСПРАВЛЕНО | Окно TMP по-прежнему блокирует автосборку |
| C5 | Phase13 нет EnsureSceneOpen | ✅ ИСПРАВЛЕНО | Добавлен в Execute() |

### 📝 Документация

| # | Проблема | Статус |
|---|----------|--------|
| D1 | Phase00 отсутствует в архитектурном документе | ✅ ИСПРАВЛЕНО |

---

## 2. Новые проблемы, найденные при повторном аудите

### 🟡 СРЕДНИЕ

| # | Фаза | Проблема |
|---|------|----------|
| Н-С1 | Phase09 | **IsNeeded() не проверяет FormationCores.** Execute() вызывает `FormationAssetGenerator.GenerateFormationCoreData()`, но IsNeeded() не проверяет `Assets/Data/FormationCores`. Если папка Formations существует, но FormationCores пуст → фаза пропускается, FormationCores не генерируются |
| Н-С2 | Phase14 | **IsNeeded() зависит от сцены для проверки ассетов.** `FindFirstObjectByType<TileMapController>()` (строка 30) возвращает null если сцена не открыта → IsNeeded() возвращает true, даже если все .asset файлы существуют. Фаза перезапускается без необходимости |
| Н-С3 | Phase08/15 | **Дублирование кода создания GameController** (из C3). Обе фазы создают: TestLocationGameController + DestructibleObjectController + HarvestableSpawner с идентичной логикой подключения |
| Н-С4 | Phase12 | **Интерактивная фаза** (из C4). Открывает TMP_PackageResourceImporter окно → блокирует Build All. Unity limitation — автоматический импорт TMP невозможен через API |

### 🟢 НИЗКИЕ

| # | Фаза | Проблема |
|---|------|----------|
| Н-Н1 | Phase16 | **IsNeeded() не проверяет UPGRADED_FOLDER.** Если Basic набор существует, но Upgraded удалён → фаза пропускается |
| Н-Н2 | Phase13 | **IsNeeded() проверяет isDirty активной сцены.** Если активна не Main сцена и она грязная → IsNeeded() true → Execute() открывает Main → исходная грязная сцена не сохранена. Краевой случай при ручном запуске одной фазы |
| Н-Н3 | IScenePhase | **Документация Order говорит "1-18", но реально 0-18** (Phase00 имеет Order=0) |
| Н-Н4 | Phase17 | **1729 строк** — крупнейший файл билдера. Трудно поддерживать, но функционально корректен |
| Н-Н5 | Orchestrator | **Нет явного объявления зависимостей.** Порядок PHASES[] правильный, но при случайной перестановке зависимости сломаются молча |

---

## 3. Полный анализ IsNeeded() всех 19 фаз

| Фаза | IsNeeded() проверяет | Побочные эффекты | Чистота | Сцена-зависимость |
|------|---------------------|------------------|---------|-------------------|
| Phase00 | URP Asset + Renderer2D + GraphicsSettings | Нет | ✅ Чистый | Нет |
| Phase01 | Все REQUIRED_FOLDERS | Нет | ✅ Чистый | Нет |
| Phase02 | Теги + Layers + Sorting Layers + PATCH-012 | Нет (TagManager только читается) | ✅ Чистый | Нет |
| Phase03 | File.Exists(SCENE_PATH) | Нет | ✅ Чистый | Нет |
| Phase04 | Camera.main + Find("GlobalLight2D") | Нет | ✅ Чистый | ⚠️ Null если сцена не открыта → true |
| Phase05 | FindFirstObjectByType<GameManager>() | Нет | ✅ Чистый | ⚠️ Null если сцена не открыта → true |
| Phase06 | Find("Player") | Нет | ✅ Чистый | ⚠️ Null если сцена не открыта → true |
| Phase07 | Find("GameUI") | Нет | ✅ Чистый | ⚠️ Null если сцена не открыта → true |
| Phase08 | FindFirstObjectByType<Grid>() | Нет | ✅ Чистый | ⚠️ Null если сцена не открыта → true |
| Phase09 | HasAssetsInFolder × 9 | Нет | ✅ Чистый | Нет |
| Phase10 | HasAssetsInFolder(Sprites/Tiles) | Нет | ✅ Чистый | Нет |
| Phase11 | File.Exists(prefab) | Нет | ✅ Чистый | Нет |
| Phase12 | TMP_Settings.instance | Нет | ✅ Чистый | Нет |
| Phase13 | activeScene.isDirty | Нет | ✅ Чистый | ⚠️ isDirty чужой сцены |
| Phase14 | HasAssetsInFolder + FindFirstObjectByType<TileMapController> | Нет | ⚠️ Смешанный | ⚠️ TileMapController null → true |
| Phase15 | Camera.main + Camera.position + FindFirstObjectByType<Grid> | Нет | ✅ Чистый | ⚠️ Null если сцена не открыта → true |
| Phase16 | BACKPACKS + STORAGERINGS + BASIC + weapon_Sword | Нет | ✅ Чистый | Нет |
| Phase17 | Find("GameUI") + Find("InventoryScreen") | Нет (только LogWarning) | ✅ Чистый | ⚠️ Зависимость от Phase07 |
| Phase18 | Find("Player") + SpiritStorageController | Нет (только LogWarning) | ✅ Чистый | ⚠️ Зависимость от Phase06 |

**Итого:** Все IsNeeded() теперь свободны от побочных эффектов ✅

---

## 4. Анализ Execute() — EnsureSceneOpen вызовы

| Фаза | Вызывает EnsureSceneOpen | Обоснованность |
|------|------------------------|---------------|
| Phase00 | Нет | ✅ Не нужна — не модифицирует сцену |
| Phase01 | Нет | ✅ Не нужна — только папки |
| Phase02 | Нет | ✅ Не нужна — только TagManager |
| Phase03 | Нет | ✅ Не нужна — сама создаёт сцену |
| Phase04 | Да | ✅ Нужна — модифицирует объекты сцены |
| Phase05 | Да | ✅ Нужна — создаёт GameObject в сцене |
| Phase06 | Да | ✅ Нужна — создаёт Player в сцене |
| Phase07 | Да | ✅ Нужна — создаёт UI в сцене |
| Phase08 | Да | ✅ Нужна — создаёт Grid/Tilemap в сцене |
| Phase09 | Нет | ✅ Не нужна — только .asset файлы |
| Phase10 | Нет | ✅ Не нужна — только спрайты |
| Phase11 | Нет | ✅ Не нужна — только префабы |
| Phase12 | Нет | ✅ Не нужна — только TMP импорт |
| Phase13 | Да | ✅ Нужна — сохраняет сцену |
| Phase14 | Да (в AssignTileBasesToController) | ✅ Нужна для назначения TileBase в сцене |
| Phase15 | Да | ✅ Нужна — модифицирует объекты сцены |
| Phase16 | Нет | ✅ Не нужна — только .asset файлы |
| Phase17 | Да | ✅ Нужна — создаёт UI в сцене |
| Phase18 | Да | ✅ Нужна — модифицирует Player в сцене |

**Итого:** Все EnsureSceneOpen вызовы обоснованы ✅

---

## 5. Граф зависимостей — верификация

```
Phase00 URP ──────→ Phase04 (Light2D)
Phase01 Folders ──→ Phase09, 10, 11, 16 (папки)
Phase02 Tags ─────→ Phase06 (tag/layer), Phase08 (sorting layers)
Phase03 Scene ────→ Phase04-08, 13-15, 17, 18 (сцена)
Phase04 Camera ───→ Phase15 (камера)
Phase05 GameMgr ──→ Phase07 (TimeController для HUD)
Phase06 Player ───→ Phase18 (Player GO)
Phase07 UI ───────→ Phase17 (Canvas + UIManager)
Phase08 Tilemap ──→ Phase14 (TileMapController), Phase15 (Grid)
Phase09 Assets ───→ Phase16 (ItemData)
Phase10 Sprites ──→ Phase14 (спрайты, есть fallback)
Phase16 InvData ──→ Phase18 (Backpack_ClothSack.asset)
Phase17 InvUI ────→ Phase18 (InventoryScreen)
```

**Обратных зависимостей НЕТ ✅**
**Порядок в PHASES[] совпадает с графом ✅**

---

## 6. Итоговая таблица проблем

### Требуют исправления (рекомендовано)

| Приоритет | # | Фаза | Проблема | Сложность |
|-----------|---|------|----------|-----------|
| 🟡 | Н-С1 | Phase09 | Добавить `HasAssetsInFolder("Assets/Data/FormationCores")` в IsNeeded() | Простой |
| 🟡 | Н-С2 | Phase14 | Разделить IsNeeded(): ассеты (без сцены) + назначение (со сценой) | Средний |
| 🟡 | Н-С3 | Phase08/15 | Вынести EnsureGameController() в SceneBuilderUtils | Средний |
| 🟡 | Н-С4 | Phase12 | Добавить try-autoimport через reflection + fallback на interactive | Сложный |
| 🟢 | Н-Н1 | Phase16 | Добавить UPGRADED_FOLDER в IsNeeded() | Простой |
| 🟢 | Н-Н3 | IScenePhase | Исправить комментарий Order: "0-18" | Простой |

### Не требуют исправления (документировать как известные ограничения)

| Приоритет | # | Фаза | Причина |
|-----------|---|------|---------|
| 🟢 | Н-Н2 | Phase13 | Краевой случай только при ручном запуске, не влияет на Build All |
| 🟢 | Н-Н4 | Phase17 | Функционально корректен, рефакторинг — отдельная задача |
| 🟢 | Н-Н5 | Orchestrator | Архитектурное ограничение, явные deps = новый интерфейс |

---

## 7. Заключение

**Общее состояние: ХОРОШЕЕ ✅**

Первый аудит выявил 3 критических и 5 средних проблем. Из них:
- 3/3 критических — исправлены ✅
- 3/5 средних — исправлены ✅
- 2/5 средних — НЕ исправлены (C3 дублирование, C4 интерактив)
- Документация — исправлена ✅

Повторный аудит нашёл 4 новых средних и 5 низких проблем.
Критических проблем **НЕТ**.

**Рекомендация:** Исправить Н-С1 (1 строка), Н-Н1 (1 строка), Н-Н3 (1 символ). Н-С2, Н-С3, Н-С4 — по желанию.

---

## 8. Исправления, выполненные по результатам повторного аудита

**Дата правок:** 2026-04-29 12:30:00 UTC

| # | Файл | Правка | Статус |
|---|------|--------|--------|
| Н-С1 | Phase09GenerateAssets.cs | Добавлена проверка `HasAssetsInFolder("Assets/Data/FormationCores")` в IsNeeded() | ✅ |
| Н-Н1 | Phase16InventoryData.cs | Добавлена проверка UPGRADED_FOLDER + weapon_Sword_T3_Refined.asset в IsNeeded() | ✅ |
| Н-Н3 | IScenePhase.cs | Комментарий Order: «1-18» → «0-18», версия 2.0 → 2.0.1 | ✅ |

### Изменённые файлы

- `Editor/SceneBuilder/Phase09GenerateAssets.cs` — IsNeeded(): +FormationCores проверка
- `Editor/SceneBuilder/Phase16InventoryData.cs` — IsNeeded(): +UPGRADED_FOLDER + weapon_Sword_T3_Refined проверка
- `Editor/SceneBuilder/IScenePhase.cs` — Order XML comment: «0-18», версия 2.0.1

### Оставшиеся проблемы (не требуют немедленного исправления)

| # | Приоритет | Описание |
|---|-----------|----------|
| Н-С2 | 🟡 | Phase14 IsNeeded() зависит от сцены — средний, некритичный |
| Н-С3 | 🟡 | Phase08/15 дублирование GameController — средний, не влияет на корректность |
| Н-С4 | 🟡 | Phase12 интерактивная — Unity limitation |
| Н-Н2 | 🟢 | Phase13 isDirty краевой случай |
| Н-Н4 | 🟢 | Phase17 размер файла |
| Н-Н5 | 🟢 | Orchestrator нет явных deps |

# Чекпоинт: Детальный аудит Scene Builder (Phase00-Phase18)
# Дата: 2026-04-25 13:00:00 UTC
# Статус: analysis_complete — план исправлений утверждён

## Контекст
Пользователь удаляет папку Assets локально перед пересозданием сцены. Scene Builder ДОЛЖЕН создавать ВСЁ необходимое с нуля. Предыдущие баги (UIManager не добавлен, ссылки не подключены) были исправлены, но обнаружены новые.

## Симптомы (от пользователя)
1. ❌ Terrain спрайты ЧЁРНЫЕ
2. ✅ Спрайты объектов (ресурсов) корректны
3. ✅ Спрайт персонажа корректен
4. ⚠️ [RPL]-L8: Тип Light2D не найден (сборка Unity.2D.RenderPipeline.Runtime)
5. ✅ Инвентарь работает (клавиша I)
6. ✅ Главное меню появляется

## Диагноз: Root Cause чёрных спрайтов

### Почему объекты/персонаж видны, а terrain — чёрный?
- **PlayerVisual** → `Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")` → рендерит БЕЗ Light2D
- **HarvestableSpawner** → `Shader.Find("Universal Render Pipeline/2d/Sprite-Unlit-Default")` → рендерит БЕЗ Light2D
- **TilemapRenderer (Terrain)** → использует дефолтный материал 2D Renderer → `Sprite-Lit-Default` → требует Light2D → ЧЁРНЫЙ

### Почему Light2D не найден?
- Phase04 пробует 3 имени сборки + AppDomain search
- Unity 6.3: тип Light2D находится в `Unity.RenderPipeline.Universal.Runtime` (4-е имя, НЕ пробуется)
- `FindTypeByName` fallback ищет `UnityEngine.Rendering.Universal.Light2D` — но в Unity 6.x тип может быть `Light2D` без namespace, или в другом namespace

---

## Найденные баги (пофазово)

### Phase00 — ОТСУТСТВУЕТ: URP Asset Creation
**КРИТИЧЕСКИЙ** — При удалении Assets пропадают:
- `Assets/Settings/UniversalRP.asset` (URP Render Pipeline Asset)
- `Assets/Settings/Renderer2D.asset` (2D Renderer Data)
- НЕТ фазы для их создания!
- Без UniversalRP.asset → GraphicsSettings не назначен → Unity fallback на Built-in renderer

**Статус**: 🔴 НОВАЯ ФАЗА НУЖНА (Phase00)

### Phase01 (Folders) — ✅ OK
Создаёт все необходимые папки. Backpacks/StorageRings включены.

### Phase02 (Tags/Layers) — ✅ OK
Теги, Physics Layers, Sorting Layers. PATCH-012 применён.

### Phase03 (Scene Creation) — ✅ OK
Создаёт пустую сцену, удаляет default camera.

### Phase04 (Camera/Light) — ❌ 2 БАГА
**БАГ A**: GlobalLight2D не создаётся — reflection не находит Light2D тип
- Пробуемые сборки: `Unity.2D.RenderPipeline.Runtime`, `Unity.RenderPipeline.Universal.2D.Runtime`, AppDomain search
- НЕ пробуется: `Unity.RenderPipeline.Universal.Runtime` (именно она в Unity 6.3!)
- НУЖНО: добавить 4-ю сборку + fallback на поиск по короткому имени "Light2D"

**БАГ B**: RenderPipelineLogger.LogLightState() пробует только 1 сборку
- Строка 267: `System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.2D.RenderPipeline.Runtime")`
- НУЖНО: тот же multi-assembly поиск что в Phase04

### Phase05 (GameManager) — ✅ OK
GameManager + GameInitializer + все системы. Конфигурация правильная.

### Phase06 (Player) — ✅ OK
Все компоненты Player создаются и настраиваются. Sorting Layer обработан.

### Phase07 (UI) — ❌ 2 БАГА
**БАГ A**: HUDController SerializeField ссылки НЕ подключены
- Phase07 добавляет `hud.AddComponent<HUDController>()` но НЕ подключает:
  - timeController, healthBar, healthText, qiBar, qiText, staminaBar
  - cultivationLevelText, cultivationProgressBar, cultivationProgressText
  - timeText, dateText, seasonIcon, seasonIcons[]
  - locationNameText, locationTypeText
  - quickSlotsContainer, quickSlotPrefab, notificationContainer, notificationPrefab
  - minimapImage, minimapPlayerIcon
- РЕЗУЛЬТАТ: HUD отображает статический текст, не обновляется в runtime

**БАГ B**: MenuUI SerializeField ссылки ЧАСТИЧНО подключены
- Подключены: uiManager, newGameButton, continueButton, loadGameButton, settingsButton, quitButton
- НЕ подключены (~20 ссылок):
  - saveManager, mainMenuPanel, pauseMenuPanel, versionText
  - resumeButton, saveButton, loadButton, mainMenuButton
  - loadMenuPanel, saveSlotsContainer, saveSlotPrefab, loadBackButton
  - settingsPanel, volume sliders (3), qualityDropdown, fullscreenToggle, languageDropdown, settingsBackButton
  - confirmationPanel, confirmationText, confirmYesButton, confirmNoButton
- РЕЗУЛЬТАТ: Настройки/сохранение/загрузка не работают

### Phase08 (Tilemap) — ✅ OK
Grid + Terrain/Objects tilemaps, TileMapController, GameController, HarvestableSpawner.

### Phase09 (Assets) — ✅ OK
ScriptableObject генерация из JSON.

### Phase10 (Sprites) — ✅ OK
Tile sprite generation.

### Phase11 (UI Prefabs) — ✅ OK
Formation UI prefabs.

### Phase12 (TMP) — ✅ OK
TMP Essentials import.

### Phase13 (Save Scene) — ✅ OK
Сохранение сцены.

### Phase14 (Tile Assets) — ✅ OK
TerrainTile/ObjectTile .asset файлы + назначение в TileMapController.

### Phase15 (Test Location) — ✅ OK
Камера, коллайдеры, GameController.

### Phase16 (Inventory Data) — ✅ OK
BackpackData/StorageRingData .asset файлы.

### Phase17 (Inventory UI) — ✅ OK (после исправления)
InventoryScreen + все панели + wiring к UIManager.

### Phase18 (Inventory Components) — ✅ OK
SpiritStorageController/StorageRingController на Player.

---

## ПЛАН ИСПРАВЛЕНИЙ

### Приоритет 1: КРИТИЧЕСКИЙ (чёрные terrain спрайты)

#### ШАГ 1: Phase04 — Исправить GlobalLight2D reflection
- Добавить 4-ю сборку: `Unity.RenderPipeline.Universal.Runtime`
- Добавить fallback: поиск по короткому имени типа во всех сборках
- Если тип всё же не найден → создать empty Light2D через AddComponent если тип доступен напрямую
- Усилить логи: вывести список ВСЕХ загруженных сборок для диагностики

#### ШАГ 2: RenderPipelineLogger — Исправить LogLightState()
- Добавить тот же multi-assembly поиск что в Phase04
- Логировать КАКАЯ сборка содержит Light2D (если найден)
- Логировать список сборок с именем содержащим "Render" или "2D" для диагностики

#### ШАГ 3: Создать Phase00 — URP Asset Creation
- Создать `Assets/Settings/UniversalRP.asset` (UniversalRenderPipelineAsset)
- Создать `Assets/Settings/Renderer2D.asset` (Renderer2DData)
- Назначить UniversalRP как текущий Render Pipeline Asset в GraphicsSettings
- Настроить Renderer2D: m_DefaultMaterialType=0 (Lit)
- НУЖНО reflection: UniversalRenderPipelineAsset и Renderer2DData не доступны напрямую

### Приоритет 2: ВЫСОКИЙ (UI функциональность)

#### ШАГ 4: Phase07 — Wire HUDController ссылки
- Подключить все SerializeField HUDController'а
- Создать недостающие UI элементы (Slider для healthBar/qiBar/staminaBar и т.д.)
- Подключить TimeController, TMP_Text элементы

#### ШАГ 5: Phase07 — Wire MenuUI оставшиеся ссылки
- Создать недостающие панели (settingsPanel, loadMenuPanel, confirmationPanel)
- Подключить кнопки и UI элементы
- Подключить saveManager

### Приоритет 3: СРЕДНИЙ (диагностика)

#### ШАГ 6: Добавить диагностические логи
- Phase04: список загруженных сборок для поиска Light2D
- Phase04: результат каждой попытки reflection
- Runtime: подробный лог состояния Light2D после создания

---

## Изменённые файлы (план)
1. `Editor/SceneBuilder/Phase04CameraLight.cs` — Fix Light2D reflection + diagnostics
2. `Core/RenderPipelineLogger.cs` — Fix LogLightState() multi-assembly + diagnostics
3. `Editor/SceneBuilder/Phase00URPSetup.cs` — NEW: URP Asset creation
4. `Editor/FullSceneBuilder.cs` — Register Phase00
5. `Editor/SceneBuilder/SceneBuilderConstants.cs` — Add URP settings folder
6. `Editor/SceneBuilder/Phase07UI.cs` — Wire HUDController + MenuUI

## Порядок выполнения
1. ШАГ 1 + ШАГ 2 (параллельно) — Fix Light2D reflection → чёрные спрайты
2. ШАГ 3 — URP Asset creation → полная пересборка с нуля
3. ШАГ 4 + ШАГ 5 — Wire UI → HUD + Menu
4. ШАГ 6 — Диагностика
5. Тест на локальном ПК: удалить Assets → Build All → Play

## Чеклист проверки на локальном ПК
1. Удалить папку Assets локально
2. Выполнить Build All (Tools > Full Scene Builder > Build All)
3. Проверить: Phase00 лог — URP assets созданы
4. Проверить: Phase04 лог — Light2D найден в сборке X
5. Нажать Play
6. Проверить: terrain спрайты НЕ чёрные (свет есть)
7. Проверить: HUD обновляется (здоровье, ци, время)
8. Проверить: I → инвентарь открывается
9. Проверить: ESC → пауза, кнопки работают
10. Скопировать логи консоли → отправить для анализа

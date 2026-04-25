# Чекпоинт: Детальный анализ — чёрные тайлы + UnassignedReferenceException в инвентаре
# Дата: 2026-04-26
# Статус: analysis_complete — план исправлений утверждён

## Симптомы (от пользователя)
1. ❌ Тайлы поверхности ЧЁРНЫЕ, спрайтов нет
2. ✅ Спрайты объектов (ресурсов) корректны
3. ✅ Спрайт персонажа корректен
4. ❌ UnassignedReferenceException: BackpackPanel.slotUIPrefab не назначен
5. ✅ Главное меню появляется
6. ✅ Клавиша I открывает инвентарь (но с ошибкой)

---

## АНАЛИЗ #1: Чёрные тайлы поверхности

### Root Cause Chain (3 звена)

```
Звено 1: Phase04.ResolveLight2DType() → NULL
         → GlobalLight2D GameObject УНИЧТОЖАЕТСЯ (строка 141)
         → В сцене НЕТ компонента Light2D

Звено 2: Phase08 создаёт TilemapRenderer БЕЗ явного материала (строки 51-54)
         → TilemapRenderer наследует default от Renderer2DData
         → Phase00 установил Renderer2DData.m_DefaultMaterialType = 0 (Lit)
         → Default material = "Sprite-Lit-Default"

Звено 3: Sprite-Lit-Default shader БЕЗ Light2D → рендерит ЧЁРНЫМ
         → PlayerVisual/HarvestableSpawner используют Sprite-Unlit-Default → видны
```

### Почему Light2D не найден (детально)

Phase04 пробует 5 методов поиска — ВСЕ возвращают NULL:
1. Сборка `Unity.2D.RenderPipeline.Runtime` — НЕТ
2. Сборка `Unity.RenderPipeline.Universal.2D.Runtime` — НЕТ
3. Сборка `Unity.RenderPipeline.Universal.Runtime` — НЕТ
4. AppDomain fullName search (`UnityEngine.Rendering.Universal.Light2D`) — НЕТ
5. AppDomain shortName search (`Light2D`) — НЕТ

**Вывод**: Тип `Light2D` НЕ существует ни в одной загруженной сборке в Unity 6.3.
При этом `Renderer2DData` и `UniversalRenderPipelineAsset` доступны напрямую — значит
`UnityEngine.Rendering.Universal` namespace ЕСТЬ, но Light2D либо удалён, либо в пакете,
который не установлен.

**Package manifest** (`Packages/manifest.json`):
- ✅ `com.unity.render-pipelines.universal: 17.0.3`
- ✅ `com.unity.2d.sprite: 1.0.0`
- ✅ `com.unity.2d.tilemap: 1.0.0`
- ❓ `com.unity.2d.render-pipelines.universal` — ОТСУТСТВУЕТ

### Файлы-виновники

| Файл | Строка | Проблема |
|------|--------|----------|
| `Phase04CameraLight.cs` | 141 | `Object.DestroyImmediate(light2DObj)` — уничтожает GlobalLight2D когда тип не найден |
| `Phase08Tilemap.cs` | 51-54 | TilemapRenderer без материала → наследует Lit default |
| `Phase08Tilemap.cs` | 61-64 | Objects TilemapRenderer — то же самое |
| `Phase00URPSetup.cs` | 90 | `m_DefaultMaterialType = 0` (Lit) → все 2D рендереры без материала получают Sprite-Lit-Default |

### Контраст: почему объекты/персонаж видны

| Компонент | Shader | Требует Light2D | Видимость |
|-----------|--------|-----------------|-----------|
| PlayerVisual | Sprite-Unlit-Default | НЕТ | ✅ Виден |
| HarvestableSpawner | Sprite-Unlit-Default | НЕТ | ✅ Виден |
| TilemapRenderer (Terrain) | Sprite-Lit-Default (default) | ДА | ❌ ЧЁРНЫЙ |
| TilemapRenderer (Objects) | Sprite-Lit-Default (default) | ДА | ❌ ЧЁРНЫЙ |

---

## АНАЛИЗ #2: BackpackPanel.slotUIPrefab UnassignedReferenceException

### Root Cause

Phase17 создаёт BackpackPanel как **пустую оболочку** — компонент добавляется на bare GameObject,
но НИ ОДНА из его `[SerializeField]` ссылок не создаётся и не подключается:

```
InventoryScreen.Awake() → Initialize() → backpackPanel.Initialize()
→ RebuildGrid() → PlaceItems() → PlaceItemInGrid()
→ Instantiate(slotUIPrefab, gridContainer)  ← NULL → EXCEPTION
```

### BackpackPanel: 7 неназначенных ссылок (кроме default-value полей)

| Поле | Тип | Статус в Phase17 |
|------|-----|-----------------|
| `slotUIPrefab` | `GameObject` | ❌ НЕ создан, НЕ назначен |
| `gridContainer` | `Transform` | ❌ НЕ создан, НЕ назначен |
| `gridBackground` | `RectTransform` | ❌ НЕ создан, НЕ назначен |
| `backpackNameText` | `TMP_Text` | ❌ НЕ создан, НЕ назначен |
| `weightText` | `TMP_Text` | ❌ НЕ создан, НЕ назначен |
| `weightBar` | `Slider` | ❌ НЕ создан, НЕ назначен |
| `slotsText` | `TMP_Text` | ❌ НЕ создан, НЕ назначен |

### СИСТЕМНАЯ ПРОБЛЕМА: Все панели инвентаря — пустые оболочки

Phase17 создаёт компоненты но НЕ создаёт их внутреннюю иерархию UI и НЕ подключает
их `[SerializeField]` ссылки. Это влияет на ВСЕ панели:

| Панель | Неназначенных ссылок | Критичность |
|--------|----------------------|-------------|
| **BackpackPanel** | 7 | 🔴 CRASH при открытии инвентаря |
| **DragDropHandler** | 5 (dragIcon, dragTransform, contextMenuPrefab, contextMenuContainer, tooltipPanel) | 🟡 Drag&Drop/контекстное меню не работают |
| **BodyDollPanel** | 16+ (11 DollSlotUI + тексты + silhouette) | 🟡 Экипировка не отображается |
| **DollSlotUI** (×11) | 8 × 11 = 88 (iconImage, slotBorder, slotLabel, itemNameText, durabilityBar, blockedOverlay, emptyIcon, slotType) | 🟡 Слоты экипировки не созданы |
| **StorageRingPanel** | 15+ (ringNameText, volumeText, volumeBar, contentContainer, entryRowPrefab, и т.д.) | 🟡 Кольцо хранилища не работает |
| **TooltipPanel** | 25+ (rarityBorder, nameText, typeText, все секции, и т.д.) | 🟡 Тултипы пустые |
| **InventorySlotUI** | 6 (iconImage, background, border, countText, durabilityBar, blockedOverlay) | 🟡 Ячейки не отображаются |

**ИТОГО**: ~150+ неназначенных ссылок в инвентаре

### InventorySlotUI — ключевой префаб

`slotUIPrefab` — это `GameObject` с компонентом `InventorySlotUI` + дочерние элементы:
- `iconImage` (Image) — иконка предмета
- `background` (Image) — фон ячейки
- `border` (Image) — рамка
- `countText` (TMP_Text) — количество (стак)
- `durabilityBar` (Image) — прочность
- `blockedOverlay` (GameObject) — заблокированная ячейка

Phase17 НИГДЕ не создаёт этот префаб.

---

## ПЛАН ИСПРАВЛЕНИЙ

### Приоритет 1: КРИТИЧЕСКИЙ — чёрные тайлы (Fix A: Sprite-Unlit-Default)

**Стратегия**: Назначить Sprite-Unlit-Default на TilemapRenderer, точно так же как
PlayerVisual и HarvestableSpawner. Это самый безопасный подход, который точно работает.

#### ШАГ 1.1: Phase08Tilemap.cs — Назначить Sprite-Unlit-Default на TilemapRenderer

**Что**: После создания TilemapRenderer назначить материал Sprite-Unlit-Default
**Где**: Строки 51-54 (terrain) и 61-64 (objects)
**Как**:
```csharp
var terrainRenderer = terrainObj.AddComponent<TilemapRenderer>();
terrainRenderer.sortingLayerName = "Terrain";
terrainRenderer.sortingOrder = 0;
terrainRenderer.mode = TilemapRenderer.Mode.Chunk;
// NEW: Sprite-Unlit-Default — не требует Light2D
Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
if (unlitShader != null)
    terrainRenderer.material = new Material(unlitShader);
```

**Что может сломаться**:
- ❌ Тайлы не будут реагировать на 2D освещение — НО системы освещения пока НЕТ
- ❌ Если потом добавить Light2D, тайлы не будут освещаться — нужно будет вернуть Lit
- ✅ Визуально спрайты будут видны, как у персонажа и объектов

#### ШАГ 1.2: Phase00URPSetup.cs — Изменить m_DefaultMaterialType на 1 (Unlit)

**Что**: Изменить `defaultMatTypeProp.intValue = 0` на `1` (строка 90)
**Зачем**: Defence in depth — если какой-то SpriteRenderer не имеет явного материала,
получит Unlit вместо Lit, и не будет чёрным
**Что может сломаться**:
- ⚠️ Влияет на ВСЕ 2D рендереры без явного материала — но мы даём явные материалы в Phase08
- ⚠️ Если кто-то добавит SpriteRenderer с ожиданием Lit default — получит Unlit
- ✅ Более безопасно чем Lit default без Light2D

#### ШАГ 1.3: Phase04CameraLight.cs — Добавить прямой тип Light2D как первую попытку

**Что**: Перед reflection попробовать `AddComponent<UnityEngine.Rendering.Universal.Light2D>()`
**Зачем**: Если пакет `com.unity.2d.render-pipelines.universal` установлен, тип будет доступен напрямую
**Что может сломаться**:
- ⚠️ Если тип НЕ доступен → ошибка компиляции → нужен #if или try-catch
- **РЕШЕНИЕ**: Не делать это сейчас. Сначала Fix A (Unlit), потом добавить Light2D когда пакет будет найден

#### ШАГ 1.4: Packages/manifest.json — Добавить 2D renderer package (ИССЛЕДОВАНИЕ)

**Что**: Добавить `com.unity.2d.render-pipelines.universal` в манифест
**Риск**: Пакет может не существовать для Unity 6.3 или конфликтовать с URP 17.0.3
**Действие**: СНАЧАЛА проверить на локальном ПК через Package Manager

### Приоритет 2: КРИТИЧЕСКИЙ — BackpackPanel.slotUIPrefab (Fix B: Создать префабы + wiring)

#### ШАГ 2.1: Phase17 — Создать InventorySlotUI префаб

**Что**: Создать GameObject с InventorySlotUI компонентом + дочерние UI элементы:
- Root: InventorySlotUI + Image(background) + LayoutElement
  - Icon (Image) — иконка предмета
  - Border (Image) — рамка
  - CountText (TMP_Text) — количество
  - DurabilityBar (Image) — прочность
  - BlockedOverlay (GameObject + Image) — заблокировано

**Как**: Создавать при каждой инициализации, НЕ как Asset (т.к. Assets удаляется)
```csharp
private GameObject CreateSlotUIPrefab(Transform parent)
{
    GameObject slot = new GameObject("SlotUI");
    slot.AddComponent<RectTransform>();
    slot.AddComponent<Image>(); // background
    slot.AddComponent<InventorySlotUI>();
    // + дочерние: Icon, Border, Count, Durability, Blocked
    // + wiring всех SerializeField InventorySlotUI
    return slot;
}
```

**Что может сломаться**:
- ⚠️ Если иерархия не совпадает с ожидаемой InventorySlotUI — визуальные баги
- ✅ Минимальный риск — InventorySlotUI.InitializeEmpty() и SetSlot() работают с полями

#### ШАГ 2.2: Phase17 — Создать внутреннюю иерархию BackpackPanel

**Что**: Создать дочерние объекты внутри BackpackPanel GameObject:
- GridContainer (empty Transform)
- GridBackground (RectTransform + Image)
- BackpackNameText (TMP_Text)
- WeightText (TMP_Text)
- WeightBar (Slider + fill/background)
- SlotsText (TMP_Text)
- SlotUIPrefab (скрытый шаблон для Instantiate)

**Как**: Через SerializedObject wiring
```csharp
private void WireBackpackPanelReferences(BackpackPanel panel, ...)
{
    SerializedObject so = new SerializedObject(panel);
    so.FindProperty("slotUIPrefab").objectReferenceValue = slotUIPrefab;
    so.FindProperty("gridContainer").objectReferenceValue = gridContainer;
    // ... и т.д.
    so.ApplyModifiedProperties();
}
```

#### ШАГ 2.3: Phase17 — Wire DragDropHandler ссылки

**Что**: Создать и подключить:
- dragIcon (Image) — иконка перетаскивания
- dragTransform (RectTransform)
- contextMenuPrefab (GameObject) — шаблон контекстного меню
- contextMenuContainer (Transform)
- tooltipPanel (TooltipPanel) — уже есть, но нужно подключить к DragDropHandler

#### ШАГ 2.4: Phase17 — Wire BodyDollPanel + DollSlotUI

**Что**: Создать 11 DollSlotUI внутри BodyDollPanel + подключить:
- 7 видимых слотов: head, torso, belt, legs, feet, weaponMain, weaponOff
- 4 слота колец: ringLeft1, ringLeft2, ringRight1, ringRight2
- Каждый DollSlotUI: iconImage, slotBorder, slotLabel, itemNameText, durabilityBar, blockedOverlay, emptyIcon
- Тексты: damageText, defenseText, statsSummaryText, ringVolumeText
- bodySilhouette (Image)

#### ШАГ 2.5: Phase17 — Wire StorageRingPanel

**Что**: Создать и подключить все 15+ ссылок StorageRingPanel

#### ШАГ 2.6: Phase17 — Wire TooltipPanel

**Что**: Создать и подключить все 25+ ссылок TooltipPanel (секции, тексты, бордеры)

### Приоритет 3: ЗАЩИТНЫЙ — Null guards в runtime

#### ШАГ 3.1: BackpackPanel.PlaceItemInGrid() — добавить null guard

**Что**: Добавить проверку `if (slotUIPrefab == null || gridContainer == null) return;`
**Где**: BackpackPanel.cs строка 202 (перед Instantiate)
**Зачем**: Предотвратить crash если slotUIPrefab не назначен

---

## ПОРЯДОК ВЫПОЛНЕНИЯ

1. **ШАГ 1.1** — Phase08: Sprite-Unlit-Default на TilemapRenderer → тайлы видимы
2. **ШАГ 1.2** — Phase00: m_DefaultMaterialType = 1 (Unlit) → defense in depth
3. **ШАГ 2.1** — Phase17: CreateSlotUIPrefab → префаб ячейки существует
4. **ШАГ 2.2** — Phase17: WireBackpackPanelReferences → инвентарь без crash
5. **ШАГ 2.3** — Phase17: WireDragDropHandler → drag&drop работает
6. **ШАГ 3.1** — BackpackPanel: null guard → защита от crash
7. **ШАГ 2.4** — Phase17: WireBodyDollPanel → экипировка (следующий приоритет)
8. **ШАГ 2.5** — Phase17: WireStorageRingPanel → кольцо (следующий приоритет)
9. **ШАГ 2.6** — Phase17: WireTooltipPanel → тултипы (следующий приоритет)

## Изменяемые файлы

| Файл | Что меняется |
|------|-------------|
| `Phase08Tilemap.cs` | Добавить Sprite-Unlit-Default на оба TilemapRenderer |
| `Phase00URPSetup.cs` | m_DefaultMaterialType = 1 (Unlit) |
| `Phase17InventoryUI.cs` | Создать префабы + wire все панели (BackpackPanel, DragDropHandler, BodyDollPanel, StorageRingPanel, TooltipPanel) |
| `BackpackPanel.cs` | Null guard в PlaceItemInGrid |

## Риски и что может сломаться

| Изменение | Риск | Митигация |
|-----------|------|-----------|
| Unlit на TilemapRenderer | Тайлы не реагируют на 2D свет | Пока системы освещения нет — не проблема. Вернуть Lit когда Light2D заработает |
| m_DefaultMaterialType=1 | Все новые SpriteRenderer будут Unlit | Явное назначение материала в Phase08 компенсирует |
| Создание префабов в Phase17 | Иерархия может не совпадать с ожиданиями InventorySlotUI | Проверить все SerializeField имена |
| Null guard в PlaceItemInGrid | Предметы просто не отобразятся вместо crash | Лучше чем crash — видно что проблема есть |

---

## ИСТОРИЯ ИСПРАВЛЕНИЙ

(Пока нет — исправления начинаются после утверждения плана)

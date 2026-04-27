# Сводка проекта (автогенерация)
Обновлено: 2026-04-22 13:01 UTC

## Проект
Cultivation World Simulator, Unity 6.3 URP 2D, C#. 174 скрипта, ~48K строк.

---

## ПРОВАЛЫ (ключевые, с решениями)

### Qi формулы — критические баги (03.31)
- qiDensity множил регенерацию → L9 регенерил в 256x быстрее L1 → **фикс: убран множитель**
- Линейная проводимость вместо `coreCapacity/360` → L9 давал 5.0 вместо 5690 → **фикс: формула из доков**
- Захардкоженные прорывы 10K/100K вместо `coreCapacity×10/100` → **фикс: динамический расчёт**

### Qi int→long — каскадная миграция (04.10)
- 18 файлов, Qi > 2.1 млрд на L5+ → truncation → сломанный бой → **фикс: полная миграция int→long**

### Чёрный экран при запуске (04.13)
- UIManager ставил Time.timeScale=0 → WaitForSeconds зависал → **фикс: WaitForSecondsRealtime**
- Legacy Input.mousePosition при Input System → **фикс: замена на InputSystem**

### Unlit шейдер — повторялся 4 раза (04.15-04.16)
- Каждый новый SpriteRenderer в URP 2D без Light2D = чёрный → **фикс: Sprite-Unlit-Default** — НЕТ базового класса, каждый раз забывали

### Race Condition — HarvestableSpawner (04.16)
- OnMapGenerated вызывался ДО подписки → объекты не спавнились → **фикс: подписка в Awake + fallback**

### Sorting Layers — порядок (04.17)
- EnsureSortingLayersExist создавала слои, но НЕ проверяла порядок → Terrain поверх Player → **фикс: детерминированные uniqueID**

### Инвентарь v1 — 7 багов (04.18)
- Рекурсивный AddItem, сломанный RemoveFromSlot, поломка при Resize → **фикс: полная переделка v2.0**

### ProjectSettings краш Unity Editor (04.20)
- Сгенерированные ProjectSettings файлы ломали Unity → **фикс: удалены из репо, код = источник истины**

---

## УСПЕХИ

- **Код ↔ документация**: полное соответствие 5 ключевым докам после аудита 03.31
- **Грамматическое согласование**: Naming/ система — «Улучшенная секира» вместо «Улучшенный секира»
- **FullSceneBuilder**: 18 идемпотентных фаз, one-click rebuild
- **Qi int→long**: миграция 18 файлов без нарушения обратной совместимости
- **Инвентарь v2.0**: полная переделка за одну сессию (5 этапов)
- **ServiceLocator**: O(1) вместо FindFirstObjectByType O(n)
- **110 спрайтов** сгенерировано AI
- **36 тестов** проходят (Combat 90%, Qi 70%)

---

## РЕАЛИЗОВАНО (рабочее)

**Core**: Enums(539 строк), Constants(579), ServiceLocator, GameEvents, StatDevelopment
**Combat**: 10-слойный пайплайн урона, LevelSuppression, QiBuffer
**Body**: Kenshi-style двойная HP (BodyController, BodyPart, BodyDamage)
**Qi**: QiController v1.1 (регенерация, проводимость, прорывы)
**NPC**: NPCController, NPCAI (behaviour tree), RelationshipController
**World**: TimeController (детерминированный), WorldController, LocationController
**Save**: SaveManager (Unix timestamps)
**Player**: WASD + бег + медитация + прорыв + сон
**Inventory v2.0**: EquipmentController(7 слотов+1H/2H), InventoryController(динамическая сетка), SpiritStorage, StorageRing
**Formation**: 11 файлов ~5500 строк, 8 типов, утечка Ци, BuffManager
**Charger**: 5 файлов ~1630 строк
**Quest**: 3 файла ~1050 строк
**Tile**: TileMapController, GameTile, TileSpriteGenerator, TestLocationSetup
**Harvestable**: Harvestable + HarvestableSpawner + HarvestFeedbackUI
**Generators**: NPC, Technique, Weapon, Armor, Consumable + Naming система
**UI**: HUD, Menu, Dialog, Combat, Inventory(6 файлов)
**JSON контент**: 34 техники, 27 врагов, 39 экипировки, 15 NPC, 15 квестов

---

## ЗАПЛАНИРОВАНО (не начато)

- **Система инструментов** (ToolData): топор/кирка/серп для добычи
- **Y-сортировка** объектов на слое Objects (sortingOrder = -Y×10)
- **Inventory Phase 6**: пояс + контекстное меню + анимации
- **Activity Manager** для TimeController (бой→замедление)
- **Расширение скоростей времени**: 6 вместо текущих 4
- **Weather System** — отложена
- **UI для зарядника Ци** — не начато
- **UI для квестов** — не начато
- **CI/CD** для автотестов — упомянуто

---

## ЗАФИКСИРОВАНО НО НЕ СДЕЛАНО

### Критические
- **7 missing script references** в сцене (04.14) — только ручная чистка в Unity Editor

### Средние
- **Тесты**: Body, Tile, Charger, Inventory, NPC AI, TimeController — не созданы
- **InvokeRepeating** для QiController/BodyController вместо Update() — не внедрено
- **.asset файлы** — сотни ScriptableObject нужно создать в Unity Editor

### Низкие
- **DOTS/ECS миграция** — упомянута
- **Zenject/VContainer** для DI — рекомендовано
- **ICombatService** интерфейс — рекомендован
- **FormattedTime HH:MM:SS** — рекомендовано
- **docs/TIME_SYSTEM.md** — устарел
- **Базовый класс для SpriteRenderer** (Unlit шейдер) — проблема повторялась 4 раза, системного решения нет

---

## ЗАМОРОЖЕННЫЕ РЕШЕНИЯ (НЕ нарушать)

- FullSceneBuilder.cs — заморожен, изменения → PhaseNNXxx.cs
- ProjectSettings/ удалён — краш Unity Editor, НЕ создавать
- Sorting layers: код = источник истины, не TagManager
- Код приводится к документации, не наоборот
- MAX_STAT_VALUE = 1000, Lightning↔Void

---

## ПРЕДУПРЕЖДЕНИЯ

- Dev-сервер НЕ запускать (START_PROMPT.md)
- ProjectSettings НЕ создавать (краш Unity Editor)
- Unlit шейдер: каждый новый SpriteRenderer в URP 2D → Sprite-Unlit-Default
- Race conditions: Unity lifecycle порядок (Awake→Start→OnEnable) критичен

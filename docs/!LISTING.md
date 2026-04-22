# 📂 Документация проекта — листинг

**Версия:** 3.0
**Дата:** 2026-04-22
**Проект:** Cultivation World Simulator (Unity 6.3 URP 2D)

---

## ⚠️ Важно

> **Все документы в `docs/` — ТЕОРИЯ и СПЕЦИФИКАЦИИ.**
> **Код реализации:** `UnityProject/Assets/Scripts/` (174 файла, ~48K строк).
> **Инструкции для Unity Editor:** `docs_asset_setup/`.
> **Временная документация, аудиты, черновики:** `docs_temp/`.
> **Архив (Phaser-эра):** `docs_old/`.

---

## 📋 docs/ — Основная документация (41 файл, 1.2 MB)

### Справочники

| Файл | Описание | Размер |
|------|----------|--------|
| [!Ai_Skills.md](./!Ai_Skills.md) | Доступные Skills ИИ-ассистента (ASR, TTS, LLM, Web-Search и др.) | 11 KB |
| [!LISTING.md](./!LISTING.md) | Этот файл — список всей документации | 10 KB |
| [UNITY_DOCS_LINKS.md](./UNITY_DOCS_LINKS.md) | 150+ ссылок на документацию Unity 6.3 | 14 KB |
| [SETUP_GUIDE.md](./SETUP_GUIDE.md) | Руководство по установке и настройке проекта | 10 KB |

### Архитектура и основы

| Файл | Описание | Размер |
|------|----------|--------|
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Общая архитектура, паттерны, модули | 26 KB |
| [DATA_MODELS.md](./DATA_MODELS.md) | Модели данных, 17 сущностей, ScriptableObjects | 18 KB |
| [DEVELOPMENT_PLAN.md](./DEVELOPMENT_PLAN.md) | План развития проекта | 13 KB |

### Системы данных

| Файл | Описание | Размер |
|------|----------|--------|
| [CONFIGURATIONS.md](./CONFIGURATIONS.md) | Конфигурации: уровни культивации, техники, материалы | 11 KB |
| [ALGORITHMS.md](./ALGORITHMS.md) | Алгоритмы и формулы: урон, подавление, Ци буфер | 24 KB |

### Игровые системы

| Файл | Описание | Размер |
|------|----------|--------|
| [COMBAT_SYSTEM.md](./COMBAT_SYSTEM.md) | Боевая система, пайплайн урона 10 слоёв | 27 KB |
| [TECHNIQUE_SYSTEM.md](./TECHNIQUE_SYSTEM.md) | Система техник культивации | 11 KB |
| [QI_SYSTEM.md](./QI_SYSTEM.md) | Система Ци: накопление, проводимость, плотность, прорывы | 18 KB |
| [BODY_SYSTEM.md](./BODY_SYSTEM.md) | Kenshi-style система тела, двойная HP | 23 KB |
| [INVENTORY_SYSTEM.md](./INVENTORY_SYSTEM.md) | Инвентарь v2.0, экипировка, слоты, рюкзак, кольца | 16 KB |
| [EQUIPMENT_SYSTEM.md](./EQUIPMENT_SYSTEM.md) | Экипировка: материалы, грейды, прочность | 22 KB |
| [BUFF_SYSTEM.md](./BUFF_SYSTEM.md) | Система баффов/дебаффов | 31 KB |
| [FORMATION_SYSTEM.md](./FORMATION_SYSTEM.md) | Система формаций, физические носители, утечка Ци | 17 KB |
| [CHARGER_SYSTEM.md](./CHARGER_SYSTEM.md) | Зарядные ядра для формаций | 22 KB |
| [ELEMENTS_SYSTEM.md](./ELEMENTS_SYSTEM.md) | Стихии, противоположности, сродство | 12 KB |
| [MODIFIERS_SYSTEM.md](./MODIFIERS_SYSTEM.md) | Модификаторы, баффы, дебаффы | 22 KB |
| [STAT_THRESHOLD_SYSTEM.md](./STAT_THRESHOLD_SYSTEM.md) | Пороги развития характеристик | 12 KB |
| [GENERATORS_SYSTEM.md](./GENERATORS_SYSTEM.md) | Генераторы: NPC, техники, предметы | 9 KB |
| [GENERATORS_NAME_FIX.md](./GENERATORS_NAME_FIX.md) | Исправление грамматики в генераторах | 16 KB |
| [PERK_SYSTEM.md](./PERK_SYSTEM.md) | Система перков | 45 KB |
| [MORTAL_DEVELOPMENT.md](./MORTAL_DEVELOPMENT.md) | Этапы развития смертного до культивации | 19 KB |
| [JOURNAL_SYSTEM.md](./JOURNAL_SYSTEM.md) | Система журнала/летописи | 22 KB |

### NPC и AI

| Файл | Описание | Размер |
|------|----------|--------|
| [NPC_AI_SYSTEM.md](./NPC_AI_SYSTEM.md) | AI NPC, Spinal Controller, рефлексы, behaviour tree | 20 KB |

### Мир и время

| Файл | Описание | Размер |
|------|----------|--------|
| [WORLD_SYSTEM.md](./WORLD_SYSTEM.md) | Мир, локации, 3D координаты | 10 KB |
| [WORLD_MAP_SYSTEM.md](./WORLD_MAP_SYSTEM.md) | Мировая карта: секторы, местность, навигация | 79 KB |
| [LOCATION_MAP_SYSTEM.md](./LOCATION_MAP_SYSTEM.md) | Локации: генерация зданий, препятствий, ресурсов | 54 KB |
| [TILE_SYSTEM.md](./TILE_SYSTEM.md) | Тайловая система: структура, параметры, генерация | 114 KB |
| [TRANSITION_SYSTEM.md](./TRANSITION_SYSTEM.md) | Система переходов между картами | 190 KB |
| [TIME_SYSTEM.md](./TIME_SYSTEM.md) | Система времени, тики, календарь | 18 KB |
| [FACTION_SYSTEM.md](./FACTION_SYSTEM.md) | Фракции, отношения, нации | 8 KB |
| [LORE_SYSTEM.md](./LORE_SYSTEM.md) | Лор, истории, миростроение | 6 KB |
| [ENTITY_TYPES.md](./ENTITY_TYPES.md) | Иерархия типов сущностей | 21 KB |

### Рендеринг и графика

| Файл | Описание | Размер |
|------|----------|--------|
| [SORTING_LAYERS.md](./SORTING_LAYERS.md) | Sorting Layers: порядок, диагностика, код = источник истины | 19 KB |
| [SPRITE_INDEX.md](./SPRITE_INDEX.md) | Индекс спрайтов: размеры, PPU, формат | 16 KB |

### Специальные системы

| Файл | Описание | Размер |
|------|----------|--------|
| [SAVE_SYSTEM.md](./SAVE_SYSTEM.md) | Сохранения, JSON, сериализация, Unix timestamps | 4 KB |
| [WORLD_SAVE_SYSTEM.md](./WORLD_SAVE_SYSTEM.md) | Сохранение мира, chunk-based persistence | 62 KB |

### Тестирование

| Файл | Описание | Размер |
|------|----------|--------|
| [UNIT_TEST_RULES.md](./UNIT_TEST_RULES.md) | Правила написания unit тестов | 9 KB |

---

## 📋 docs_asset_setup/ — Инструкции для Unity Editor (33 файла, 400 KB)

### Оркестратор сцен

| Файл | Описание | Размер |
|------|----------|--------|
| [SCENE_BUILDER_ARCHITECTURE.md](../docs_asset_setup/SCENE_BUILDER_ARCHITECTURE.md) | Архитектура FullSceneBuilder (заморожен) + 18 фаз | 16 KB |
| [README.md](../docs_asset_setup/README.md) | Обзор папки, порядок выполнения инструкций | 11 KB |

### Пошаговые инструкции

| Файл | Описание | Ручной | Полуавто |
|------|----------|--------|----------|
| [01_CultivationLevelData.md](../docs_asset_setup/01_CultivationLevelData.md) | Уровни культивации | ✅ | — |
| [02_MortalStageData.md](../docs_asset_setup/02_MortalStageData.md) | Стадии смертного | ✅ | — |
| [03_ElementData.md](../docs_asset_setup/03_ElementData.md) | Данные стихий | ✅ | — |
| [04_BasicScene.md](../docs_asset_setup/04_BasicScene.md) | Создание сцены | ✅ | ✅ SemiAuto |
| [05_PlayerSetup.md](../docs_asset_setup/05_PlayerSetup.md) | Настройка игрока + анимации | ✅ | ✅ SemiAuto |
| [06_TechniqueData.md](../docs_asset_setup/06_TechniqueData.md) | Данные техник | ✅ | ✅ SemiAuto |
| [07_NPCPresetData.md](../docs_asset_setup/07_NPCPresetData.md) | Пресеты NPC | ✅ | ✅ SemiAuto |
| [08_EquipmentData.md](../docs_asset_setup/08_EquipmentData.md) | Данные экипировки | ✅ | ✅ SemiAuto |
| [09_EnemySetup.md](../docs_asset_setup/09_EnemySetup.md) | Настройка врагов | ✅ | ✅ SemiAuto |
| [10_QuestSetup.md](../docs_asset_setup/10_QuestSetup.md) | Настройка квестов | ✅ | ✅ SemiAuto |
| [11_ItemData.md](../docs_asset_setup/11_ItemData.md) | Данные предметов | ✅ | ✅ SemiAuto |
| [12_MaterialData.md](../docs_asset_setup/12_MaterialData.md) | Данные материалов | ✅ | ✅ SemiAuto |
| [13_SpriteSetup.md](../docs_asset_setup/13_SpriteSetup.md) | Настройка спрайтов + QuickStart | ✅ | ✅ QuickStart |
| [14_FormationData.md](../docs_asset_setup/14_FormationData.md) | Данные формаций | ✅ | — |
| [15_FormationCoreData.md](../docs_asset_setup/15_FormationCoreData.md) | Ядра формаций | ✅ | — |
| [16_TileSystem_SemiAuto.md](../docs_asset_setup/16_TileSystem_SemiAuto.md) | Тайловая система | — | ✅ SemiAuto |
| [17_InventoryData.md](../docs_asset_setup/17_InventoryData.md) | Данные инвентаря v2.0 | ✅ | ✅ SemiAuto |
| [18_InventoryUI.md](../docs_asset_setup/18_InventoryUI.md) | UI инвентаря | ✅ | ✅ SemiAuto |

---

## 📋 docs_temp/ — Временная документация (49 файлов, 1.1 MB)

### Актуальные черновики

| Файл | Описание |
|------|----------|
| [LONG_TERM_MEMORY_SCHEME.md](../docs_temp/LONG_TERM_MEMORY_SCHEME.md) | Схема долговременной памяти ИИ-агента (3 варианта) |
| [tool_system_draft.md](../docs_temp/tool_system_draft.md) | Черновик системы инструментов (топор/кирка/серп) |
| [INVENTORY_FLAGS_AUDIT.md](../docs_temp/INVENTORY_FLAGS_AUDIT.md) | Аудит флагов инвентаря |
| [INVENTORY_IMPLEMENTATION_PLAN.md](../docs_temp/INVENTORY_IMPLEMENTATION_PLAN.md) | План реализации инвентаря |
| [INVENTORY_UI_DRAFT.md](../docs_temp/INVENTORY_UI_DRAFT.md) | Черновик UI инвентаря |
| [AUDIT_FullSceneBuilder_2026-04-17.md](../docs_temp/AUDIT_FullSceneBuilder_2026-04-17.md) | Аудит FullSceneBuilder |
| [CODE_REFERENCE.md](../docs_temp/CODE_REFERENCE.md) | Справочник кода |
| [GIT_WORKFLOW_TWO_PC.md](../docs_temp/GIT_WORKFLOW_TWO_PC.md) | Git workflow для двух ПК |
| [WORKFLOW_GITHUB_UNITY.md](../docs_temp/WORKFLOW_GITHUB_UNITY.md) | GitHub + Unity workflow |

### Аудиты и отчёты

| Файл | Описание |
|------|----------|
| [AUDIT_2026-04-10.md](../docs_temp/AUDIT_2026-04-10.md) | Мега-аудит: 210 проблем |
| [AUDIT_2026-04-13.md](../docs_temp/AUDIT_2026-04-13.md) | Аудит чёрного экрана |
| [AUDIT_2026-04-14.md](../docs_temp/AUDIT_2026-04-14.md) | Аудит критических багов |
| [CONSOLIDATED_AUDIT.md](../docs_temp/CONSOLIDATED_AUDIT.md) | Консолидированный аудит |
| [CODE_AUDIT_Unity_6.3.md](../docs_temp/CODE_AUDIT_Unity_6.3.md) | Код-аудит Unity 6.3 адаптации |
| [QWEN_CODE_AUDIT_REPORT.md](../docs_temp/QWEN_CODE_AUDIT_REPORT.md) | Аудит кода (Qwen) |
| [audit_core_combat.md](../docs_temp/audit_core_combat.md) | Аудит: Core + Combat |
| [audit_body_qi_player.md](../docs_temp/audit_body_qi_player.md) | Аудит: Body + Qi + Player |
| [audit_data_tile_ui_gen.md](../docs_temp/audit_data_tile_ui_gen.md) | Аудит: Data + Tile + UI + Gen |
| [audit_world_npc_formation.md](../docs_temp/audit_world_npc_formation.md) | Аудит: World + NPC + Formation |

### Прочие файлы

Остальные ~30 файлов docs_temp/ — исторические отчёты, примеры, исследования. Полный список см. в папке.

---

## 📋 docs_old/ — Архив Phaser-эры (66 файлов, 2.0 MB)

Документация из предыдущей реализации на Phaser. **Только для справки**, не актуально для Unity.

Ключевые файлы: `ARCHITECTURE_cloud.md`, `NPC_AI_NEUROTHEORY.md`, `body_armor.md`, `body_monsters.md`, `weapon-armor-system.md`.

---

## 📊 Статистика

| Папка | Файлов | Объём | Назначение |
|-------|--------|-------|------------|
| `docs/` | 41 | 1.2 MB | Основная документация |
| `docs_asset_setup/` | 33 | 400 KB | Инструкции для Unity Editor |
| `docs_temp/` | 49 | 1.1 MB | Черновики, аудиты, временная |
| `docs_old/` | 66 | 2.0 MB | Архив (Phaser-эра) |
| **Итого** | **189** | **4.7 MB** | |

---

## 🔗 Связи основных документов

```
ARCHITECTURE.md (корневой документ)
    ├── DATA_MODELS.md (структуры данных)
    ├── ALGORITHMS.md (формулы)
    ├── CONFIGURATIONS.md (конфиги)
    │
    ├── COMBAT_SYSTEM.md
    │   ├── TECHNIQUE_SYSTEM.md
    │   ├── QI_SYSTEM.md
    │   ├── BODY_SYSTEM.md
    │   └── BUFF_SYSTEM.md
    │
    ├── INVENTORY_SYSTEM.md
    │   └── EQUIPMENT_SYSTEM.md
    │
    ├── FORMATION_SYSTEM.md
    │   └── CHARGER_SYSTEM.md
    │
    ├── NPC_AI_SYSTEM.md
    │
    ├── WORLD_SYSTEM.md
    │   ├── WORLD_MAP_SYSTEM.md
    │   ├── LOCATION_MAP_SYSTEM.md
    │   ├── TILE_SYSTEM.md
    │   ├── SORTING_LAYERS.md
    │   ├── TRANSITION_SYSTEM.md
    │   ├── TIME_SYSTEM.md
    │   ├── FACTION_SYSTEM.md
    │   └── ENTITY_TYPES.md
    │
    ├── SAVE_SYSTEM.md
    │   └── WORLD_SAVE_SYSTEM.md
    │
    ├── GENERATORS_SYSTEM.md
    │   └── GENERATORS_NAME_FIX.md
    │
    └── PERK_SYSTEM.md
        └── MORTAL_DEVELOPMENT.md
```

---

## 📝 Рекомендации по чтению

### Для старта разработки:
1. `SESSION_SUMMARY.md` — актуальный контекст проекта
2. `ARCHITECTURE.md` — общая картина
3. `SCENE_BUILDER_ARCHITECTURE.md` — генерация сцены (заморожен)

### Для боевой системы:
1. `COMBAT_SYSTEM.md` → `QI_SYSTEM.md` → `BODY_SYSTEM.md` → `BUFF_SYSTEM.md`

### Для инвентаря:
1. `INVENTORY_SYSTEM.md` → `EQUIPMENT_SYSTEM.md`
2. `docs_asset_setup/17_InventoryData.md` + `18_InventoryUI.md`

### Для тайловой системы:
1. `TILE_SYSTEM.md` → `SORTING_LAYERS.md` → `SPRITE_INDEX.md`

---

## 🛠️ Окружение проекта

**Unity 6.3 (6000.3)**, URP 2D, C#
**GitHub:** `vivasua-collab/Ai-game3.git`, branch `main`

**Структура проекта:**
```
/home/z/my-project/
├── UnityProject/          # Unity проект (Assets/Scripts/ — 174 C# файла)
├── checkpoints/           # Чекпоинты работы (116 файлов)
├── docs/                  # Основная документация (41 файл)
├── docs_asset_setup/      # Инструкции для Unity Editor (33 файла)
├── docs_temp/             # Черновики, аудиты (49 файлов)
├── docs_old/              # Архив Phaser-эры (66 файлов)
├── tools/                 # Скрипты-утилиты
├── upload/                # Загруженные изображения
├── SESSION_SUMMARY.md     # Актуальный контекст проекта
├── START_PROMPT.md        # Правила работы ИИ-агента
├── Caveman.md             # Режим коммуникации
├── worklog.md             # Хроника работы всех агентов
└── .gitignore
```

---

*Редактировано: 2026-04-22 13:05:00 UTC*
*Изменения v3.0: Актуализация под текущее состояние проекта — убраны устаревшие ссылки (docs_examples/, docs_implementation_plans/, UnityProject/Local/), добавлены docs_asset_setup/ и docs_temp/, обновлена статистика, добавлен SESSION_SUMMARY.md, обновлены связи документов*

# 📂 Документация проекта — листинг

**Версия:** 4.0
**Дата:** 2026-04-23
**Проект:** Cultivation World Simulator (Unity 6.3 URP 2D)

---

## ⚠️ Важно

> **Все документы в `docs/` — ТЕОРИЯ и СПЕЦИФИКАЦИИ.**
> **Код реализации:** `UnityProject/Assets/Scripts/` (174 файла, ~48K строк).
> **Инструкции для Unity Editor:** `docs_asset_setup/`.
> **Временная документация, аудиты, черновики:** `docs_temp/`.
> **Архив (Phaser-эра):** `docs_old/`.

### 📐 Оценка токенов

> **Метод:** `chars ÷ 3` — приближённая оценка для русскоязычного текста с кодом.
> Реальное потребление зависит от токенизатора (GPT-4: ~2.5 chars/token, Claude: ~3.5).
> **Легенда стоимости:** 🔥 >15K | ⚠️ 5K–15K | ✅ <5K

---

## 📋 docs/ — Основная документация (41 файл, 1.1 MB, ~372K токенов)

### Справочники

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [!Ai_Skills.md](./!Ai_Skills.md) | Доступные Skills ИИ-ассистента (ASR, TTS, LLM, Web-Search и др.) | 11 KB | 3.8K | ✅ |
| [!LISTING.md](./!LISTING.md) | Этот файл — список всей документации | 17 KB | 5.5K | ⚠️ |
| [UNITY_DOCS_LINKS.md](./UNITY_DOCS_LINKS.md) | 150+ ссылок на документацию Unity 6.3 | 14 KB | 4.6K | ✅ |
| [SETUP_GUIDE.md](./SETUP_GUIDE.md) | Руководство по установке и настройке проекта | 10 KB | 3.3K | ✅ |

### Архитектура и основы

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Общая архитектура, паттерны, модули | 26 KB | 8.7K | ⚠️ |
| [DATA_MODELS.md](./DATA_MODELS.md) | Модели данных, 17 сущностей, ScriptableObjects | 18 KB | 6.0K | ⚠️ |
| [DEVELOPMENT_PLAN.md](./DEVELOPMENT_PLAN.md) | План развития проекта | 13 KB | 4.2K | ✅ |

### Системы данных

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [CONFIGURATIONS.md](./CONFIGURATIONS.md) | Конфигурации: уровни культивации, техники, материалы | 11 KB | 3.6K | ✅ |
| [ALGORITHMS.md](./ALGORITHMS.md) | Алгоритмы и формулы: урон, подавление, Ци буфер | 24 KB | 8.0K | ⚠️ |

### Игровые системы

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [COMBAT_SYSTEM.md](./COMBAT_SYSTEM.md) | Боевая система, пайплайн урона 10 слоёв | 27 KB | 9.0K | ⚠️ |
| [TECHNIQUE_SYSTEM.md](./TECHNIQUE_SYSTEM.md) | Система техник культивации | 11 KB | 3.7K | ✅ |
| [QI_SYSTEM.md](./QI_SYSTEM.md) | Система Ци: накопление, проводимость, плотность, прорывы | 18 KB | 6.0K | ⚠️ |
| [BODY_SYSTEM.md](./BODY_SYSTEM.md) | Kenshi-style система тела, двойная HP | 23 KB | 7.6K | ⚠️ |
| [INVENTORY_SYSTEM.md](./INVENTORY_SYSTEM.md) | Инвентарь v2.0, экипировка, слоты, рюкзак, кольца | 16 KB | 5.3K | ⚠️ |
| [EQUIPMENT_SYSTEM.md](./EQUIPMENT_SYSTEM.md) | Экипировка: материалы, грейды, прочность | 22 KB | 7.4K | ⚠️ |
| [BUFF_SYSTEM.md](./BUFF_SYSTEM.md) | Система баффов/дебаффов | 31 KB | 10.4K | ⚠️ |
| [FORMATION_SYSTEM.md](./FORMATION_SYSTEM.md) | Система формаций, физические носители, утечка Ци | 17 KB | 5.8K | ⚠️ |
| [CHARGER_SYSTEM.md](./CHARGER_SYSTEM.md) | Зарядные ядра для формаций | 22 KB | 7.4K | ⚠️ |
| [ELEMENTS_SYSTEM.md](./ELEMENTS_SYSTEM.md) | Стихии, противоположности, сродство | 12 KB | 4.2K | ✅ |
| [MODIFIERS_SYSTEM.md](./MODIFIERS_SYSTEM.md) | Модификаторы, баффы, дебаффы | 22 KB | 7.2K | ⚠️ |
| [STAT_THRESHOLD_SYSTEM.md](./STAT_THRESHOLD_SYSTEM.md) | Пороги развития характеристик | 12 KB | 3.9K | ✅ |
| [GENERATORS_SYSTEM.md](./GENERATORS_SYSTEM.md) | Генераторы: NPC, техники, предметы | 9 KB | 2.9K | ✅ |
| [GENERATORS_NAME_FIX.md](./GENERATORS_NAME_FIX.md) | Исправление грамматики в генераторах | 16 KB | 5.3K | ⚠️ |
| [PERK_SYSTEM.md](./PERK_SYSTEM.md) | Система перков | 45 KB | 15.1K | 🔥 |
| [MORTAL_DEVELOPMENT.md](./MORTAL_DEVELOPMENT.md) | Этапы развития смертного до культивации | 19 KB | 6.2K | ⚠️ |
| [JOURNAL_SYSTEM.md](./JOURNAL_SYSTEM.md) | Система журнала/летописи | 22 KB | 7.4K | ⚠️ |

### NPC и AI

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [NPC_AI_SYSTEM.md](./NPC_AI_SYSTEM.md) | AI NPC, Spinal Controller, рефлексы, behaviour tree | 20 KB | 6.8K | ⚠️ |

### Мир и время

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [WORLD_SYSTEM.md](./WORLD_SYSTEM.md) | Мир, локации, 3D координаты | 10 KB | 3.5K | ✅ |
| [WORLD_MAP_SYSTEM.md](./WORLD_MAP_SYSTEM.md) | Мировая карта: секторы, местность, навигация | 79 KB | 26.5K | 🔥 |
| [LOCATION_MAP_SYSTEM.md](./LOCATION_MAP_SYSTEM.md) | Локации: генерация зданий, препятствий, ресурсов | 54 KB | 18.1K | 🔥 |
| [TILE_SYSTEM.md](./TILE_SYSTEM.md) | Тайловая система: структура, параметры, генерация | 114 KB | 37.9K | 🔥 |
| [TRANSITION_SYSTEM.md](./TRANSITION_SYSTEM.md) | Система переходов между картами | 190 KB | 63.2K | 🔥 |
| [TIME_SYSTEM.md](./TIME_SYSTEM.md) | Система времени, тики, календарь | 18 KB | 5.9K | ⚠️ |
| [FACTION_SYSTEM.md](./FACTION_SYSTEM.md) | Фракции, отношения, нации | 8 KB | 2.7K | ✅ |
| [LORE_SYSTEM.md](./LORE_SYSTEM.md) | Лор, истории, миростроение | 6 KB | 2.1K | ✅ |
| [ENTITY_TYPES.md](./ENTITY_TYPES.md) | Иерархия типов сущностей | 21 KB | 6.9K | ⚠️ |

### Рендеринг и графика

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [SORTING_LAYERS.md](./SORTING_LAYERS.md) | Sorting Layers: порядок, диагностика, код = источник истины | 19 KB | 6.3K | ⚠️ |
| [SPRITE_INDEX.md](./SPRITE_INDEX.md) | Индекс спрайтов: размеры, PPU, формат | 16 KB | 5.2K | ⚠️ |

### Специальные системы

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [SAVE_SYSTEM.md](./SAVE_SYSTEM.md) | Сохранения, JSON, сериализация, Unix timestamps | 4 KB | 1.2K | ✅ |
| [WORLD_SAVE_SYSTEM.md](./WORLD_SAVE_SYSTEM.md) | Сохранение мира, chunk-based persistence | 62 KB | 20.6K | 🔥 |

### Тестирование

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [UNIT_TEST_RULES.md](./UNIT_TEST_RULES.md) | Правила написания unit тестов | 9 KB | 2.9K | ✅ |

### 💰 Топ-5 самых дорогих файлов docs/

| Файл | Токены ≈ | |
|------|----------|-|
| TRANSITION_SYSTEM.md | 63.2K | 🔥 |
| TILE_SYSTEM.md | 37.9K | 🔥 |
| WORLD_MAP_SYSTEM.md | 26.5K | 🔥 |
| WORLD_SAVE_SYSTEM.md | 20.6K | 🔥 |
| LOCATION_MAP_SYSTEM.md | 18.1K | 🔥 |

> ⚡ Эти 5 файлов = **~166K токенов** (45% от всей папки docs/). Читать только по необходимости.

---

## 📋 docs_asset_setup/ — Инструкции для Unity Editor (33 файла, 340 KB, ~114K токенов)

### Оркестратор сцен

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [SCENE_BUILDER_ARCHITECTURE.md](../docs_asset_setup/SCENE_BUILDER_ARCHITECTURE.md) | Архитектура FullSceneBuilder (заморожен) + 18 фаз | 16 KB | 5.3K | ⚠️ |
| [README.md](../docs_asset_setup/README.md) | Обзор папки, порядок выполнения инструкций | 11 KB | 3.7K | ✅ |

### Пошаговые инструкции

| Файл | Описание | Ручной | Полуавто | Размер | Токены ≈ | |
|------|----------|--------|----------|--------|----------|-|
| [01_CultivationLevelData.md](../docs_asset_setup/01_CultivationLevelData.md) | Уровни культивации | ✅ | — | 15 KB | 5.0K | ⚠️ |
| [02_MortalStageData.md](../docs_asset_setup/02_MortalStageData.md) | Стадии смертного | ✅ | — | 13 KB | 4.3K | ✅ |
| [03_ElementData.md](../docs_asset_setup/03_ElementData.md) | Данные стихий | ✅ | — | 13 KB | 4.5K | ✅ |
| [04_BasicScene.md](../docs_asset_setup/04_BasicScene.md) | Создание сцены | ✅ | ✅ SemiAuto | 9 KB | 3.1K | ✅ |
| [04_BasicScene_SemiAuto.md](../docs_asset_setup/04_BasicScene_SemiAuto.md) | Создание сцены (авто) | — | ✅ SemiAuto | 9 KB | 2.9K | ✅ |
| [05_PlayerSetup.md](../docs_asset_setup/05_PlayerSetup.md) | Настройка игрока | ✅ | ✅ SemiAuto | 12 KB | 4.0K | ✅ |
| [05_PlayerSetup_SemiAuto.md](../docs_asset_setup/05_PlayerSetup_SemiAuto.md) | Настройка игрока (авто) | — | ✅ SemiAuto | 14 KB | 4.7K | ✅ |
| [05_PlayerSetup_Animation.md](../docs_asset_setup/05_PlayerSetup_Animation.md) | Анимации игрока | ✅ | — | 19 KB | 6.4K | ⚠️ |
| [06_TechniqueData.md](../docs_asset_setup/06_TechniqueData.md) | Данные техник | ✅ | ✅ SemiAuto | 14 KB | 4.8K | ✅ |
| [06_TechniqueData_SemiAuto.md](../docs_asset_setup/06_TechniqueData_SemiAuto.md) | Данные техник (авто) | — | ✅ SemiAuto | 6 KB | 1.8K | ✅ |
| [07_NPCPresetData.md](../docs_asset_setup/07_NPCPresetData.md) | Пресеты NPC | ✅ | ✅ SemiAuto | 12 KB | 4.1K | ✅ |
| [07_NPCPresetData_SemiAuto.md](../docs_asset_setup/07_NPCPresetData_SemiAuto.md) | Пресеты NPC (авто) | — | ✅ SemiAuto | 4 KB | 1.2K | ✅ |
| [08_EquipmentData.md](../docs_asset_setup/08_EquipmentData.md) | Данные экипировки | ✅ | ✅ SemiAuto | 13 KB | 4.5K | ✅ |
| [08_EquipmentData_SemiAuto.md](../docs_asset_setup/08_EquipmentData_SemiAuto.md) | Данные экипировки (авто) | — | ✅ SemiAuto | 4 KB | 1.2K | ✅ |
| [09_EnemySetup.md](../docs_asset_setup/09_EnemySetup.md) | Настройка врагов | ✅ | ✅ SemiAuto | 13 KB | 4.2K | ✅ |
| [09_EnemySetup_SemiAuto.md](../docs_asset_setup/09_EnemySetup_SemiAuto.md) | Настройка врагов (авто) | — | ✅ SemiAuto | 3 KB | 1.0K | ✅ |
| [10_QuestSetup.md](../docs_asset_setup/10_QuestSetup.md) | Настройка квестов | ✅ | ✅ SemiAuto | 14 KB | 4.6K | ✅ |
| [10_QuestSetup_SemiAuto.md](../docs_asset_setup/10_QuestSetup_SemiAuto.md) | Настройка квестов (авто) | — | ✅ SemiAuto | 4 KB | 1.2K | ✅ |
| [11_ItemData.md](../docs_asset_setup/11_ItemData.md) | Данные предметов | ✅ | ✅ SemiAuto | 11 KB | 3.6K | ✅ |
| [11_ItemData_SemiAuto.md](../docs_asset_setup/11_ItemData_SemiAuto.md) | Данные предметов (авто) | — | ✅ SemiAuto | 3 KB | 1.0K | ✅ |
| [12_MaterialData.md](../docs_asset_setup/12_MaterialData.md) | Данные материалов | ✅ | ✅ SemiAuto | 18 KB | 5.9K | ⚠️ |
| [12_MaterialData_SemiAuto.md](../docs_asset_setup/12_MaterialData_SemiAuto.md) | Данные материалов (авто) | — | ✅ SemiAuto | 4 KB | 1.5K | ✅ |
| [13_SpriteSetup.md](../docs_asset_setup/13_SpriteSetup.md) | Настройка спрайтов | ✅ | ✅ QuickStart | 18 KB | 6.1K | ⚠️ |
| [13_SpriteSetup_QuickStart.md](../docs_asset_setup/13_SpriteSetup_QuickStart.md) | Настройка спрайтов (быстрый старт) | — | ✅ QuickStart | 3 KB | 0.9K | ✅ |
| [14_FormationData.md](../docs_asset_setup/14_FormationData.md) | Данные формаций | ✅ | — | 8 KB | 2.6K | ✅ |
| [15_FormationCoreData.md](../docs_asset_setup/15_FormationCoreData.md) | Ядра формаций | ✅ | — | 7 KB | 2.3K | ✅ |
| [16_TileSystem_SemiAuto.md](../docs_asset_setup/16_TileSystem_SemiAuto.md) | Тайловая система | — | ✅ SemiAuto | 20 KB | 6.7K | ⚠️ |
| [17_InventoryData.md](../docs_asset_setup/17_InventoryData.md) | Данные инвентаря v2.0 | ✅ | ✅ SemiAuto | 8 KB | 2.8K | ✅ |
| [17_InventoryData_SemiAuto.md](../docs_asset_setup/17_InventoryData_SemiAuto.md) | Данные инвентаря (авто) | — | ✅ SemiAuto | 5 KB | 1.8K | ✅ |
| [18_InventoryUI.md](../docs_asset_setup/18_InventoryUI.md) | UI инвентаря | ✅ | ✅ SemiAuto | 13 KB | 4.2K | ✅ |
| [18_InventoryUI_SemiAuto.md](../docs_asset_setup/18_InventoryUI_SemiAuto.md) | UI инвентаря (авто) | — | ✅ SemiAuto | 6 KB | 1.9K | ✅ |

### 💰 Топ-3 самых дорогих файлов docs_asset_setup/

| Файл | Токены ≈ | |
|------|----------|-|
| 16_TileSystem_SemiAuto.md | 6.7K | ⚠️ |
| 05_PlayerSetup_Animation.md | 6.4K | ⚠️ |
| 13_SpriteSetup.md | 6.1K | ⚠️ |

> ⚡ Нет файлов >15K токенов. Папка оптимальна по стоимости.

---

## 📋 docs_temp/ — Временная документация (49 файлов, 900 KB, ~301K токенов)

### Актуальные черновики

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [LONG_TERM_MEMORY_SCHEME.md](../docs_temp/LONG_TERM_MEMORY_SCHEME.md) | Схема долговременной памяти ИИ-агента (3 варианта) | 30 KB | 9.9K | ⚠️ |
| [tool_system_draft.md](../docs_temp/tool_system_draft.md) | Черновик системы инструментов (топор/кирка/серп) | 16 KB | 5.5K | ⚠️ |
| [INVENTORY_FLAGS_AUDIT.md](../docs_temp/INVENTORY_FLAGS_AUDIT.md) | Аудит флагов инвентаря | 20 KB | 6.5K | ⚠️ |
| [INVENTORY_IMPLEMENTATION_PLAN.md](../docs_temp/INVENTORY_IMPLEMENTATION_PLAN.md) | План реализации инвентаря | 18 KB | 5.9K | ⚠️ |
| [INVENTORY_UI_DRAFT.md](../docs_temp/INVENTORY_UI_DRAFT.md) | Черновик UI инвентаря | 38 KB | 12.7K | 🔥 |
| [AUDIT_FullSceneBuilder_2026-04-17.md](../docs_temp/AUDIT_FullSceneBuilder_2026-04-17.md) | Аудит FullSceneBuilder | 21 KB | 6.9K | ⚠️ |
| [CODE_REFERENCE.md](../docs_temp/CODE_REFERENCE.md) | Справочник кода | 71 KB | 23.8K | 🔥 |
| [GIT_WORKFLOW_TWO_PC.md](../docs_temp/GIT_WORKFLOW_TWO_PC.md) | Git workflow для двух ПК | 8 KB | 2.5K | ✅ |
| [WORKFLOW_GITHUB_UNITY.md](../docs_temp/WORKFLOW_GITHUB_UNITY.md) | GitHub + Unity workflow | 9 KB | 2.9K | ✅ |

### Аудиты и отчёты

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [AUDIT_2026-04-10.md](../docs_temp/AUDIT_2026-04-10.md) | Мега-аудит: 210 проблем | 55 KB | 18.4K | 🔥 |
| [AUDIT_2026-04-13.md](../docs_temp/AUDIT_2026-04-13.md) | Аудит чёрного экрана | 33 KB | 10.9K | ⚠️ |
| [AUDIT_2026-04-14.md](../docs_temp/AUDIT_2026-04-14.md) | Аудит критических багов | 11 KB | 3.7K | ✅ |
| [AUDIT_2026-04-14_UPDATED.md](../docs_temp/AUDIT_2026-04-14_UPDATED.md) | Аудит критических багов (обновлённый) | 34 KB | 11.2K | 🔥 |
| [CONSOLIDATED_AUDIT.md](../docs_temp/CONSOLIDATED_AUDIT.md) | Консолидированный аудит | 72 KB | 23.9K | 🔥 |
| [CODE_AUDIT_Unity_6.3.md](../docs_temp/CODE_AUDIT_Unity_6.3.md) | Код-аудит Unity 6.3 адаптации | 15 KB | 4.8K | ✅ |
| [QWEN_CODE_AUDIT_REPORT.md](../docs_temp/QWEN_CODE_AUDIT_REPORT.md) | Аудит кода (Qwen) | 35 KB | 11.6K | 🔥 |
| [audit_core_combat.md](../docs_temp/audit_core_combat.md) | Аудит: Core + Combat | 34 KB | 11.2K | 🔥 |
| [audit_body_qi_player.md](../docs_temp/audit_body_qi_player.md) | Аудит: Body + Qi + Player | 15 KB | 5.1K | ⚠️ |
| [audit_data_tile_ui_gen.md](../docs_temp/audit_data_tile_ui_gen.md) | Аудит: Data + Tile + UI + Gen | 6 KB | 2.0K | ✅ |
| [audit_world_npc_formation.md](../docs_temp/audit_world_npc_formation.md) | Аудит: World + NPC + Formation | 22 KB | 7.4K | ⚠️ |
| [AUDIT_VERIFICATION.md](../docs_temp/AUDIT_VERIFICATION.md) | Верификация аудитов | 9 KB | 3.1K | ✅ |

### Прочие файлы docs_temp/

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [!CONTRADICTIONS_REPORT.md](../docs_temp/!CONTRADICTIONS_REPORT.md) | Отчёт о противоречиях в документации | 9 KB | 2.9K | ✅ |
| [!CONTRADICTIONS_REPORT_v2.md](../docs_temp/!CONTRADICTIONS_REPORT_v2.md) | Отчёт о противоречиях v2 | 14 KB | 4.6K | ✅ |
| [!DUPLICATION_REPORT.md](../docs_temp/!DUPLICATION_REPORT.md) | Отчёт о дублированиях | 6 KB | 2.1K | ✅ |
| [!SINGLE_SOURCE_OF_TRUTH_PROPOSAL.md](../docs_temp/!SINGLE_SOURCE_OF_TRUTH_PROPOSAL.md) | Предложение единого источника истины | 10 KB | 3.3K | ✅ |
| [BREAKTHROUGH_MODELS_COMPARISON.md](../docs_temp/BREAKTHROUGH_MODELS_COMPARISON.md) | Сравнение моделей прорыва | 25 KB | 8.4K | ⚠️ |
| [BuffSystem_Examples.md](../docs_temp/BuffSystem_Examples.md) | Примеры системы баффов | 11 KB | 3.7K | ✅ |
| [COMPUTATIONAL_RESOURCES_CALCULATION.md](../docs_temp/COMPUTATIONAL_RESOURCES_CALCULATION.md) | Расчёт вычислительных ресурсов | 21 KB | 7.0K | ⚠️ |
| [StatThresholdSystem_Examples.md](../docs_temp/StatThresholdSystem_Examples.md) | Примеры порогов характеристик | 11 KB | 3.7K | ✅ |
| [FormationSystem_Examples.md](../docs_temp/FormationSystem_Examples.md) | Примеры системы формаций | 8 KB | 2.8K | ✅ |
| [TechniqueEffectsSystem.md](../docs_temp/TechniqueEffectsSystem.md) | Система эффектов техник | 19 KB | 6.4K | ⚠️ |
| [TECHNIQUE_USAGE_REPORT.md](../docs_temp/TECHNIQUE_USAGE_REPORT.md) | Отчёт по использованию техник | 11 KB | 3.8K | ✅ |
| [TILE_SYSTEM_IMPLEMENTATION.md](../docs_temp/TILE_SYSTEM_IMPLEMENTATION.md) | Реализация тайловой системы | 6 KB | 2.1K | ✅ |
| [CharacterSpriteMirroring.md](../docs_temp/CharacterSpriteMirroring.md) | Зеркалирование спрайтов персонажа | 13 KB | 4.3K | ✅ |
| [NameGenerator_Russian.md](../docs_temp/NameGenerator_Russian.md) | Генератор русских имён | 22 KB | 7.2K | ⚠️ |
| [NPC_L6_ASSEMBLY_EXAMPLE.md](../docs_temp/NPC_L6_ASSEMBLY_EXAMPLE.md) | Пример сборки NPC L6 | 8 KB | 2.5K | ✅ |
| [OrbitalWeaponSystem.md](../docs_temp/OrbitalWeaponSystem.md) | Система орбитального оружия | 16 KB | 5.4K | ⚠️ |
| [QI_ABSORPTION_RADIUS.md](../docs_temp/QI_ABSORPTION_RADIUS.md) | Радиус поглощения Ци | 13 KB | 4.3K | ✅ |
| [ACHIEVEMENT_SYSTEM.md](../docs_temp/ACHIEVEMENT_SYSTEM.md) | Система достижений | 15 KB | 4.9K | ✅ |
| [ANALYSIS_REPORT.md](../docs_temp/ANALYSIS_REPORT.md) | Аналитический отчёт | 9 KB | 2.9K | ✅ |
| [CODE_REVIEW_Local_Folder.md](../docs_temp/CODE_REVIEW_Local_Folder.md) | Ревью папки Local/ | 5 KB | 1.8K | ✅ |
| [LOST_SESSION_ANALYSIS.md](../docs_temp/LOST_SESSION_ANALYSIS.md) | Анализ потерянной сессии | 11 KB | 3.6K | ✅ |
| [MIGRATION_ANALYSIS.md](../docs_temp/MIGRATION_ANALYSIS.md) | Анализ миграции | 10 KB | 3.4K | ✅ |
| [PROJECT_SETUP_PLAN.md](../docs_temp/PROJECT_SETUP_PLAN.md) | План настройки проекта | 9 KB | 2.9K | ✅ |
| [UNITY_63_RESEARCH.md](../docs_temp/UNITY_63_RESEARCH.md) | Исследование Unity 6.3 | 7 KB | 2.4K | ✅ |
| [UNITY_VERSION_COMPARISON.md](../docs_temp/UNITY_VERSION_COMPARISON.md) | Сравнение версий Unity | 12 KB | 3.8K | ✅ |
| [RunningTests.md](../docs_temp/RunningTests.md) | Запуск тестов | 19 KB | 6.5K | ⚠️ |
| [README.md](../docs_temp/README.md) | Описание папки docs_temp | 1 KB | 0.4K | ✅ |
| [audit_batch3_supplement.md](../docs_temp/audit_batch3_supplement.md) | Дополнение к аудиту (batch 3) | 13 KB | 4.2K | ✅ |

### 💰 Топ-5 самых дорогих файлов docs_temp/

| Файл | Токены ≈ | |
|------|----------|-|
| CONSOLIDATED_AUDIT.md | 23.9K | 🔥 |
| CODE_REFERENCE.md | 23.8K | 🔥 |
| AUDIT_2026-04-10.md | 18.4K | 🔥 |
| INVENTORY_UI_DRAFT.md | 12.7K | 🔥 |
| QWEN_CODE_AUDIT_REPORT.md | 11.6K | 🔥 |

> ⚡ Эти 5 файлов = **~90K токенов** (30% от docs_temp/). Аудиты — самые дорогие.

---

## 📋 docs_old/ — Архив Phaser-эры (66 файлов, 1.9 MB, ~648K токенов)

Документация из предыдущей реализации на Phaser. **Только для справки**, не актуально для Unity.

Ключевые файлы: `ARCHITECTURE_cloud.md`, `NPC_AI_NEUROTHEORY.md`, `body_armor.md`, `body_monsters.md`, `weapon-armor-system.md`.

### 💰 Топ-5 самых дорогих файлов docs_old/

| Файл | Токены ≈ | |
|------|----------|-|
| body_monsters.md | 44.2K | 🔥 |
| body_armor.md | 41.4K | 🔥 |
| NPC_AI_NEUROTHEORY.md | 35.1K | 🔥 |
| NPC_AI_THEORY.md | 33.7K | 🔥 |
| weapon-armor-system.md | 26.8K | 🔥 |

> ⚡ docs_old/ = **~648K токенов**. Читать только при миграции лора/механик из Phaser-эры.

---

## 📋 Корневые файлы проекта

| Файл | Описание | Размер | Токены ≈ | |
|------|----------|--------|----------|-|
| [SESSION_SUMMARY.md](../SESSION_SUMMARY.md) | Актуальный контекст проекта (Scheme C) | 7 KB | 2.4K | ✅ |
| [START_PROMPT.md](../START_PROMPT.md) | Правила работы ИИ-агента | 7 KB | 2.4K | ✅ |
| [Caveman.md](../Caveman.md) | Режим коммуникации | 4 KB | 1.4K | ✅ |
| [worklog.md](../worklog.md) | Хроника работы всех агентов | 102 KB | 34.0K | 🔥 |

> ⚡ `worklog.md` — самый дорогой файл проекта. Растёт постоянно. Читать только хвост.

---

## 📊 Статистика

| Папка | Файлов | Объём | Токены ≈ | 🔥 | ⚠️ | ✅ |
|-------|--------|-------|----------|-----|-----|-----|
| `docs/` | 41 | 1.1 MB | 372K | 6 | 20 | 15 |
| `docs_asset_setup/` | 33 | 340 KB | 114K | 0 | 5 | 28 |
| `docs_temp/` | 49 | 900 KB | 301K | 7 | 11 | 31 |
| `docs_old/` | 66 | 1.9 MB | 648K | — | — | — |
| Корневые | 4 | 120 KB | 40K | 1 | 0 | 3 |
| **Итого** | **193** | **4.4 MB** | **~1.5M** | **14** | **36** | **77** |

### Распределение по стоимости

```
✅ Дешёвые (<5K токенов)    77 файлов  ████████████████████████████████████  50%
⚠️ Средние  (5K–15K токенов) 36 файлов  ██████████████████                  23%
🔥 Дорогие  (>15K токенов)   14 файлов  ███████                              9%
                             (docs_old не разбит)66 файлов  ████████████████████████████████  18%
```

> **Вывод:** 14 «дорогих» файлов содержат **~60% всех токенов**. Чтение только по необходимости — главный рычаг экономии.

---

## 🔗 Связи основных документов

```
ARCHITECTURE.md (8.7K токенов, корневой документ)
    ├── DATA_MODELS.md (6.0K)
    ├── ALGORITHMS.md (8.0K)
    ├── CONFIGURATIONS.md (3.6K)
    │
    ├── COMBAT_SYSTEM.md (9.0K)
    │   ├── TECHNIQUE_SYSTEM.md (3.7K)
    │   ├── QI_SYSTEM.md (6.0K)
    │   ├── BODY_SYSTEM.md (7.6K)
    │   └── BUFF_SYSTEM.md (10.4K)
    │
    ├── INVENTORY_SYSTEM.md (5.3K)
    │   └── EQUIPMENT_SYSTEM.md (7.4K)
    │
    ├── FORMATION_SYSTEM.md (5.8K)
    │   └── CHARGER_SYSTEM.md (7.4K)
    │
    ├── NPC_AI_SYSTEM.md (6.8K)
    │
    ├── WORLD_SYSTEM.md (3.5K)
    │   ├── WORLD_MAP_SYSTEM.md (26.5K) 🔥
    │   ├── LOCATION_MAP_SYSTEM.md (18.1K) 🔥
    │   ├── TILE_SYSTEM.md (37.9K) 🔥
    │   ├── SORTING_LAYERS.md (6.3K)
    │   ├── TRANSITION_SYSTEM.md (63.2K) 🔥
    │   ├── TIME_SYSTEM.md (5.9K)
    │   ├── FACTION_SYSTEM.md (2.7K)
    │   └── ENTITY_TYPES.md (6.9K)
    │
    ├── SAVE_SYSTEM.md (1.2K)
    │   └── WORLD_SAVE_SYSTEM.md (20.6K) 🔥
    │
    ├── GENERATORS_SYSTEM.md (2.9K)
    │   └── GENERATORS_NAME_FIX.md (5.3K)
    │
    └── PERK_SYSTEM.md (15.1K) 🔥
        └── MORTAL_DEVELOPMENT.md (6.2K)
```

> 🔥 Все 6 «дорогих» файлов docs/ — в ветке WORLD (5) + PERK_SYSTEM (1).

---

## 📝 Рекомендации по чтению (с учётом стоимости)

### Минимальный старт (~16K токенов):
1. `SESSION_SUMMARY.md` — 2.4K ✅ — актуальный контекст
2. `START_PROMPT.md` — 2.4K ✅ — правила работы
3. `ARCHITECTURE.md` — 8.7K ⚠️ — общая картина
4. `DATA_MODELS.md` — 6.0K ⚠️ — структуры данных

### Для боевой системы (~36K токенов):
1. `COMBAT_SYSTEM.md` (9.0K) → `QI_SYSTEM.md` (6.0K) → `BODY_SYSTEM.md` (7.6K) → `BUFF_SYSTEM.md` (10.4K) → `TECHNIQUE_SYSTEM.md` (3.7K)

### Для инвентаря (~16K токенов):
1. `INVENTORY_SYSTEM.md` (5.3K) → `EQUIPMENT_SYSTEM.md` (7.4K)
2. `docs_asset_setup/17_InventoryData.md` (2.8K) + `18_InventoryUI.md` (4.2K)

### Для тайловой системы (~50K токенов — дорого!):
1. `TILE_SYSTEM.md` (37.9K 🔥) → `SORTING_LAYERS.md` (6.3K) → `SPRITE_INDEX.md` (5.2K)

### ⛔ Читать только по прямой необходимости:
- `TRANSITION_SYSTEM.md` — 63.2K 🔥 (самый дорогой файл)
- `TILE_SYSTEM.md` — 37.9K 🔥
- `WORLD_MAP_SYSTEM.md` — 26.5K 🔥
- `worklog.md` — 34.0K 🔥 (читать только хвост)
- `WORLD_SAVE_SYSTEM.md` — 20.6K 🔥

---

## 🛠️ Окружение проекта

**Unity 6.3 (6000.3)**, URP 2D, C#
**GitHub:** `vivasua-collab/Ai-game3.git`, branch `main`

**Структура проекта:**
```
/home/z/my-project/
├── UnityProject/          # Unity проект (Assets/Scripts/ — 174 C# файла)
├── checkpoints/           # Чекпоинты работы (116 файлов)
├── docs/                  # Основная документация (41 файл, ~372K токенов)
├── docs_asset_setup/      # Инструкции для Unity Editor (33 файла, ~114K токенов)
├── docs_temp/             # Черновики, аудиты (49 файлов, ~301K токенов)
├── docs_old/              # Архив Phaser-эры (66 файлов, ~648K токенов)
├── tools/                 # Скрипты-утилиты
├── upload/                # Загруженные изображения
├── SESSION_SUMMARY.md     # Актуальный контекст проекта (2.4K токенов)
├── START_PROMPT.md        # Правила работы ИИ-агента (2.4K токенов)
├── Caveman.md             # Режим коммуникации (1.4K токенов)
├── worklog.md             # Хроника работы всех агентов (34K токенов 🔥)
└── .gitignore
```

---

*Редактировано: 2026-04-23*
*Изменения v4.0: Добавлена оценка токенов для каждого файла (chars÷3), легенда стоимости (🔥⚠️✅), топ-5 дорогих файлов для каждой папки, рекомендаций по чтению с учётом стоимости, обновлена статистика*

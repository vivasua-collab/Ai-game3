# 📂 docs_temp/ — Временная документация, аудиты, черновики

**Версия:** 1.0
**Дата:** 2026-05-20
**Проект:** Cultivation World Simulator (Unity 6.3 URP 2D)

---

## ⚠️ Назначение папки

> **docs_temp/** — временное хранилище: черновики систем в глубокой разработке,
> результаты аудитов документации и кода, аналитические отчёты, исследования,
> и прочие файлы, не вошедшие в основную документацию `docs/`.
>
> **Статус файлов:** 🟢 Актуальный | 🟡 В разработке | ⚪ Исторический
>
> **Основная документация:** `docs/` (валидированная, утверждённая).
> **Инструкции для Unity Editor:** `docs_asset_setup/`.
> **Архив (Phaser-эра):** `docs_old/`.

### 📐 Оценка токенов

> **Метод:** `chars ÷ 3` — приближённая оценка для русскоязычного текста с кодом.
> **Легенда стоимости:** 🔥 >15K | ⚠️ 5K–15K | ✅ <5K

---

## 📊 Сводка по категориям

| # | Категория | Файлов | Объём | Токены ≈ |
|---|-----------|--------|-------|----------|
| 1 | 🔴 Аудит документации | 7 | 107 KB | 35.6K |
| 2 | 🔍 Аудит кода | 13 | 374 KB | 124.8K |
| 3 | 📐 Черновики систем (в глубокой разработке) | 7 | 149 KB | 49.8K |
| 4 | 🧪 Примеры и спецификации | 6 | 83 KB | 27.6K |
| 5 | 🔧 Технические исследования | 4 | 111 KB | 37.1K |
| 6 | 📊 Аналитические отчёты | 5 | 46 KB | 15.5K |
| 7 | 📖 Справочники и руководства | 8 | 111 KB | 37.1K |
| 8 | 📌 Мета-файлы | 1 | 1 KB | 0.4K |
| | **Итого** | **51** | **983 KB** | **~328K** |

---

## 1. 🔴 Аудит документации (7 файлов)

Результаты аудитов основной документации `docs/`. Выявление противоречий, рекомендации, статус исправлений.

| Файл | Описание | Статус | Размер | Токены ≈ | |
|------|----------|--------|--------|----------|-|
| [AUDIT_DOCS_CONTRADICTIONS_2026-04-27.md](./AUDIT_DOCS_CONTRADICTIONS_2026-04-27.md) | 3-й аудит docs/: 3 итерации (А1/А2/А3), 79 проблем, 73 исправлено | 🟢 Актуальный | 38 KB | 12.5K | 🔥 |
| [CONTRADICTIONS_AUDIT_2026-04-23.md](./CONTRADICTIONS_AUDIT_2026-04-23.md) | 2-й аудит: 83 противоречия (20 крит., 30 major, 33 minor) | ⚪ Исторический | 21 KB | 6.9K | ⚠️ |
| [!CONTRADICTIONS_REPORT.md](!CONTRADICTIONS_REPORT.md) | 1-й отчёт о противоречиях | ⚪ Исторический | 9 KB | 2.9K | ✅ |
| [!CONTRADICTIONS_REPORT_v2.md](!CONTRADICTIONS_REPORT_v2.md) | 2-й отчёт о противоречиях | ⚪ Исторический | 14 KB | 4.6K | ✅ |
| [!DUPLICATION_REPORT.md](!DUPLICATION_REPORT.md) | Отчёт о дублированиях в документации | ⚪ Исторический | 6 KB | 2.1K | ✅ |
| [!SINGLE_SOURCE_OF_TRUTH_PROPOSAL.md](!SINGLE_SOURCE_OF_TRUTH_PROPOSAL.md) | Предложение единого источника истины (SSOT) | ⚪ Исторический | 10 KB | 3.3K | ✅ |
| [AUDIT_VERIFICATION.md](./AUDIT_VERIFICATION.md) | Верификация результатов аудитов | ⚪ Исторический | 9 KB | 3.1K | ✅ |

> 🟢 **Актуальный:** `AUDIT_DOCS_CONTRADICTIONS_2026-04-27.md` — единый трекер всех 3 итераций аудита документации. Остальные — исторические предшественники.

---

## 2. 🔍 Аудит кода (13 файлов)

Результаты аудитов кодовой базы `UnityProject/Assets/Scripts/`. Баги, проблемы, рекомендации.

| Файл | Описание | Статус | Размер | Токены ≈ | |
|------|----------|--------|--------|----------|-|
| [CONSOLIDATED_AUDIT.md](./CONSOLIDATED_AUDIT.md) | Консолидированный аудит кодовой базы | ⚪ Исторический | 72 KB | 23.9K | 🔥 |
| [AUDIT_2026-04-10.md](./AUDIT_2026-04-10.md) | Мега-аудит: 210 проблем | ⚪ Исторический | 55 KB | 18.4K | 🔥 |
| [QWEN_CODE_AUDIT_REPORT.md](./QWEN_CODE_AUDIT_REPORT.md) | Аудит кода (Qwen) | ⚪ Исторический | 35 KB | 11.6K | 🔥 |
| [AUDIT_2026-04-14_UPDATED.md](./AUDIT_2026-04-14_UPDATED.md) | Аудит критических багов (обновлённый) | ⚪ Исторический | 34 KB | 11.2K | 🔥 |
| [audit_core_combat.md](./audit_core_combat.md) | Аудит: Core + Combat модули | ⚪ Исторический | 34 KB | 11.2K | 🔥 |
| [AUDIT_2026-04-13.md](./AUDIT_2026-04-13.md) | Аудит чёрного экрана | ⚪ Исторический | 33 KB | 10.9K | ⚠️ |
| [AUDIT_FullSceneBuilder_2026-04-17.md](./AUDIT_FullSceneBuilder_2026-04-17.md) | Аудит FullSceneBuilder (19 фаз) | ⚪ Исторический | 21 KB | 6.9K | ⚠️ |
| [audit_world_npc_formation.md](./audit_world_npc_formation.md) | Аудит: World + NPC + Formation | ⚪ Исторический | 22 KB | 7.4K | ⚠️ |
| [audit_body_qi_player.md](./audit_body_qi_player.md) | Аудит: Body + Qi + Player | ⚪ Исторический | 15 KB | 5.1K | ⚠️ |
| [AUDIT_2026-04-14.md](./AUDIT_2026-04-14.md) | Аудит критических багов (первичный) | ⚪ Исторический | 11 KB | 3.7K | ✅ |
| [CODE_AUDIT_Unity_6.3.md](./CODE_AUDIT_Unity_6.3.md) | Код-аудит: адаптация к Unity 6.3 | ⚪ Исторический | 15 KB | 4.8K | ✅ |
| [audit_batch3_supplement.md](./audit_batch3_supplement.md) | Дополнение к аудиту (batch 3) | ⚪ Исторический | 13 KB | 4.2K | ✅ |
| [audit_data_tile_ui_gen.md](./audit_data_tile_ui_gen.md) | Аудит: Data + Tile + UI + Gen | ⚪ Исторический | 6 KB | 2.0K | ✅ |

> ⚪ Все аудиты кода — исторические. Исправления внесены. Для актуального состояния кода см. `UnityProject/Assets/Scripts/`.

---

## 3. 📐 Черновики систем — в глубокой разработке (7 файлов)

Системы, находящиеся на этапе проектирования. Не перенесены в `docs/` — требуют доработки, утверждения, или зависят от других решений.

| Файл | Описание | Статус | Размер | Токены ≈ | |
|------|----------|--------|--------|----------|-|
| [INVENTORY_UI_DRAFT.md](./INVENTORY_UI_DRAFT.md) | Черновик UI инвентаря (grid→list переход) | 🟡 В разработке | 38 KB | 12.7K | 🔥 |
| [LONG_TERM_MEMORY_SCHEME.md](./LONG_TERM_MEMORY_SCHEME.md) | Схема долговременной памяти ИИ-агента (3 варианта) | 🟡 В разработке | 30 KB | 9.9K | ⚠️ |
| [INVENTORY_FLAGS_AUDIT.md](./INVENTORY_FLAGS_AUDIT.md) | Аудит флагов инвентаря (внутренний) | 🟡 В разработке | 20 KB | 6.5K | ⚠️ |
| [INVENTORY_IMPLEMENTATION_PLAN.md](./INVENTORY_IMPLEMENTATION_PLAN.md) | План реализации инвентаря v2.0 | 🟡 В разработке | 18 KB | 5.9K | ⚠️ |
| [tool_system_draft.md](./tool_system_draft.md) | Черновик системы инструментов (топор/кирка/серп) | 🟡 В разработке | 16 KB | 5.5K | ⚠️ |
| [ACHIEVEMENT_SYSTEM.md](./ACHIEVEMENT_SYSTEM.md) | Система достижений — теоретические изыскания | 🟡 В разработке | 15 KB | 4.9K | ✅ |
| [QI_ABSORPTION_RADIUS.md](./QI_ABSORPTION_RADIUS.md) | Радиус сферы поглощения Ци (4 модели) | 🟡 В разработке | 13 KB | 4.3K | ✅ |

> 🟡 Все файлы — активные черновики. Не утверждены, не перенесены в `docs/`.
> **Связь с аудитом:** С2-4⏳ (инвентарь list-based) — решение отложено, черновики сохранены.

---

## 4. 🧪 Примеры и спецификации (6 файлов)

Конкретные примеры реализаций, сравнения моделей, спецификации подсистем. Частично — голая теория.

| Файл | Описание | Статус | Размер | Токены ≈ | |
|------|----------|--------|--------|----------|-|
| [BREAKTHROUGH_MODELS_COMPARISON.md](./BREAKTHROUGH_MODELS_COMPARISON.md) | Сравнение 4 моделей прорыва (баланс) | 🟢 Актуальный | 25 KB | 8.4K | ⚠️ |
| [TechniqueEffectsSystem.md](./TechniqueEffectsSystem.md) | Система эффектов техник (спецификация) | 🟡 В разработке | 19 KB | 6.4K | ⚠️ |
| [StatThresholdSystem_Examples.md](./StatThresholdSystem_Examples.md) | Примеры порогов характеристик | ⚪ Справочный | 11 KB | 3.7K | ✅ |
| [BuffSystem_Examples.md](./BuffSystem_Examples.md) | Примеры системы баффов | ⚪ Справочный | 11 KB | 3.7K | ✅ |
| [FormationSystem_Examples.md](./FormationSystem_Examples.md) | Примеры системы формаций | ⚪ Справочный | 8 KB | 2.8K | ✅ |
| [NPC_L6_ASSEMBLY_EXAMPLE.md](./NPC_L6_ASSEMBLY_EXAMPLE.md) | Пример сборки NPC уровня L6 | ⚪ Справочный | 8 KB | 2.5K | ✅ |

> `BREAKTHROUGH_MODELS_COMPARISON.md` — актуальный для баланса. Остальные — справочные примеры.

---

## 5. 🔧 Технические исследования (4 файла)

Исследования технологий, расчёты ресурсов, справочники кода.

| Файл | Описание | Статус | Размер | Токены ≈ | |
|------|----------|--------|--------|----------|-|
| [CODE_REFERENCE.md](./CODE_REFERENCE.md) | Справочник кодовой базы (174 файла) | 🟢 Актуальный | 71 KB | 23.8K | 🔥 |
| [COMPUTATIONAL_RESOURCES_CALCULATION.md](./COMPUTATIONAL_RESOURCES_CALCULATION.md) | Расчёт вычислительных ресурсов | ⚪ Справочный | 21 KB | 7.0K | ⚠️ |
| [UNITY_VERSION_COMPARISON.md](./UNITY_VERSION_COMPARISON.md) | Сравнение версий Unity | ⚪ Исторический | 12 KB | 3.8K | ✅ |
| [UNITY_63_RESEARCH.md](./UNITY_63_RESEARCH.md) | Исследование Unity 6.3 (адаптация) | ⚪ Исторический | 7 KB | 2.4K | ✅ |

> `CODE_REFERENCE.md` — самый дорогой файл папки (23.8K токенов). Читать только по необходимости.

---

## 6. 📊 Аналитические отчёты (5 файлов)

Разовые аналитические отчёты: анализ сессий, миграций, ревью кода.

| Файл | Описание | Статус | Размер | Токены ≈ | |
|------|----------|--------|--------|----------|-|
| [TECHNIQUE_USAGE_REPORT.md](./TECHNIQUE_USAGE_REPORT.md) | Отчёт по использованию техник | 🟢 Актуальный | 11 KB | 3.8K | ✅ |
| [LOST_SESSION_ANALYSIS.md](./LOST_SESSION_ANALYSIS.md) | Анализ потерянной сессии | ⚪ Исторический | 11 KB | 3.6K | ✅ |
| [MIGRATION_ANALYSIS.md](./MIGRATION_ANALYSIS.md) | Анализ миграции Phaser→Unity | ⚪ Исторический | 10 KB | 3.4K | ✅ |
| [ANALYSIS_REPORT.md](./ANALYSIS_REPORT.md) | Общий аналитический отчёт | ⚪ Исторический | 9 KB | 2.9K | ✅ |
| [CODE_REVIEW_Local_Folder.md](./CODE_REVIEW_Local_Folder.md) | Ревью папки Local/ | ⚪ Исторический | 5 KB | 1.8K | ✅ |

---

## 7. 📖 Справочники и руководства (8 файлов)

Руководства по workflow, настройке, генерации, тестированию.

| Файл | Описание | Статус | Размер | Токены ≈ | |
|------|----------|--------|--------|----------|-|
| [NameGenerator_Russian.md](./NameGenerator_Russian.md) | Генератор русских имён (алгоритм + данные) | 🟢 Актуальный | 22 KB | 7.2K | ⚠️ |
| [RunningTests.md](./RunningTests.md) | Запуск тестов в Unity | 🟢 Актуальный | 19 KB | 6.5K | ⚠️ |
| [OrbitalWeaponSystem.md](./OrbitalWeaponSystem.md) | Система орбитального оружия | 🟡 В разработке | 16 KB | 5.4K | ⚠️ |
| [CharacterSpriteMirroring.md](./CharacterSpriteMirroring.md) | Зеркалирование спрайтов персонажа | 🟢 Актуальный | 13 KB | 4.3K | ✅ |
| [WORKFLOW_GITHUB_UNITY.md](./WORKFLOW_GITHUB_UNITY.md) | GitHub + Unity workflow | 🟢 Актуальный | 9 KB | 2.9K | ✅ |
| [PROJECT_SETUP_PLAN.md](./PROJECT_SETUP_PLAN.md) | План настройки проекта | ⚪ Исторический | 9 KB | 2.9K | ✅ |
| [GIT_WORKFLOW_TWO_PC.md](./GIT_WORKFLOW_TWO_PC.md) | Git workflow для двух ПК | 🟢 Актуальный | 8 KB | 2.5K | ✅ |
| [TILE_SYSTEM_IMPLEMENTATION.md](./TILE_SYSTEM_IMPLEMENTATION.md) | Реализация тайловой системы (заметки) | ⚪ Исторический | 6 KB | 2.1K | ✅ |

---

## 8. 📌 Мета-файлы (1 файл)

| Файл | Описание | Статус | Размер | Токены ≈ | |
|------|----------|--------|--------|----------|-|
| [README.md](./README.md) | Описание папки docs_temp (устаревшее) | ⚪ Исторический | 1 KB | 0.4K | ✅ |

> `README.md` содержит устаревшее описание. Данный файл (`!listing.md`) заменяет его как актуальный каталог.

---

## 💰 Топ-5 самых дорогих файлов docs_temp/

| Файл | Токены ≈ | Категория | |
|------|----------|-----------|-|
| CONSOLIDATED_AUDIT.md | 23.9K | Аудит кода | 🔥 |
| CODE_REFERENCE.md | 23.8K | Тех. исследования | 🔥 |
| AUDIT_2026-04-10.md | 18.4K | Аудит кода | 🔥 |
| AUDIT_DOCS_CONTRADICTIONS_2026-04-27.md | 12.5K | Аудит документации | 🔥 |
| INVENTORY_UI_DRAFT.md | 12.7K | Черновики | 🔥 |

> ⚡ Эти 5 файлов = **~91K токенов** (28% от docs_temp/). Аудиты — самые дорогие.

---

## 🔗 Связи с основной документацией

```
docs_temp/ → docs/ (основная документация)
    │
    ├── AUDIT_DOCS_CONTRADICTIONS_2026-04-27.md
    │   └── Трекер аудита → все файлы docs/ (79 проблем, 73 ✅)
    │
    ├── INVENTORY_* (3 файла)
    │   └── → docs/INVENTORY_SYSTEM.md (С2-4⏳: list-based переход)
    │
    ├── QI_ABSORPTION_RADIUS.md
    │   └── → docs/QI_SYSTEM.md, docs/ALGORITHMS.md
    │
    ├── BREAKTHROUGH_MODELS_COMPARISON.md
    │   └── → docs/ALGORITHMS.md, docs/CONFIGURATIONS.md
    │
    ├── TechniqueEffectsSystem.md
    │   └── → docs/TECHNIQUE_SYSTEM.md
    │
    ├── *_Examples.md (3 файла)
    │   └── → docs/BUFF_MODIFIERS_SYSTEM.md, docs/STAT_THRESHOLD_SYSTEM.md, docs/FORMATION_SYSTEM.md
    │
    └── CODE_REFERENCE.md
        └── → UnityProject/Assets/Scripts/ (174 файла)
```

---

## 📝 Рекомендации по работе

### Когда читать docs_temp/:
- **Аудит документации** → только `AUDIT_DOCS_CONTRADICTIONS_2026-04-27.md` (актуальный трекер)
- **Инвентарь** → 3 файла INVENTORY_* (черновики list-based)
- **Баланс прорывов** → `BREAKTHROUGH_MODELS_COMPARISON.md`
- **Радиус Ци** → `QI_ABSORPTION_RADIUS.md`

### Когда НЕ читать docs_temp/:
- Аудиты кода — исторические, исправления уже в коде
- Аналитические отчёты — разовые, не актуальны для текущей работы

### Файлы-кандидаты на перенос в docs/:
- `BREAKTHROUGH_MODELS_COMPARISON.md` → после утверждения модели прорыва
- `TechniqueEffectsSystem.md` → после доработки
- `ACHIEVEMENT_SYSTEM.md` → после утверждения

### Файлы-кандидаты на архивацию в docs_old/:
- Все аудиты кода (13 файлов) — исторические, не актуальны
- Исторические аудиты документации (кроме AUDIT_DOCS_CONTRADICTIONS_2026-04-27.md)

---

## 📊 Статистика по статусам

| Статус | Файлов | Доля |
|--------|--------|------|
| 🟢 Актуальный | 9 | 18% |
| 🟡 В разработке | 10 | 20% |
| ⚪ Исторический/Справочный | 32 | 63% |

> **Вывод:** 63% файлов — исторические/справочные. Активная работа ведётся с ~10 черновиками и ~9 актуальными файлами.

---

*Создано: 2026-05-20*

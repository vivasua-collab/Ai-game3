# 📂 Unity Documentation Listing

**Версия:** 2.1  
**Дата:** 2026-04-04  
**Проект:** Cultivation World Simulator → Unity Migration

---

## ⚠️ Важно

> **Все документы содержат ТОЛЬКО ТЕОРИЮ.**  
> **НЕТ КОДА** — только теоретические описания систем.  
> Код перенесён в `docs/examples/` — см. раздел "Примеры реализации".  
> Реализация в Unity: `UnityProject/Local/Assets/Scripts/`.

---

## 📋 Список документов

### 🤖 Справочники

| Файл | Описание | Токенов |
|------|----------|---------|
| [!Ai_Skills.md](./!Ai_Skills.md) | Доступные Skills ИИ-ассистента (ASR, TTS, LLM, Web-Search и др.) | ~3500 |
| [!LISTING.md](./!LISTING.md) | Этот файл — список всех документов | ~2000 |
| [!DUPLICATION_REPORT.md](./!DUPLICATION_REPORT.md) | Отчёт о дублировании информации | ~3000 |
| [!CONTRADICTIONS_REPORT.md](./!CONTRADICTIONS_REPORT.md) | Отчёт о противоречиях в документации | ~2500 |
| [UNITY_DOCS_LINKS.md](./UNITY_DOCS_LINKS.md) | Листинг 150+ ссылок на документацию Unity 6.3 | ~4500 |

### 🎮 Выбор версии Unity

| Файл | Описание | Токенов |
|------|----------|---------|
| [UNITY_VERSION_COMPARISON.md](./UNITY_VERSION_COMPARISON.md) | Сравнение Unity 2022 LTS vs Unity 6.3, рекомендации | ~4000 |

### Архитектура и основы

| Файл | Описание | Токенов |
|------|----------|---------|
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Общая архитектура Unity проекта, паттерны, модули | ~3500 |
| [DATA_MODELS.md](./DATA_MODELS.md) | Модели данных из Prisma schema, 17 сущностей | ~5000 |
| [MIGRATION_ANALYSIS.md](./MIGRATION_ANALYSIS.md) | Анализ миграции, что нужно перенести | ~2000 |
| [WORKFLOW_GITHUB_UNITY.md](./WORKFLOW_GITHUB_UNITY.md) | Workflow: GitHub ↔ локальный Unity | ~3000 |
| [GIT_WORKFLOW_TWO_PC.md](./GIT_WORKFLOW_TWO_PC.md) | Git workflow для работы на двух ПК | ~2000 |
| [UNITY_63_RESEARCH.md](./UNITY_63_RESEARCH.md) | Исследование API Unity 6.3 (из оф. документации) | ~3500 |
| [PROJECT_SETUP_PLAN.md](./PROJECT_SETUP_PLAN.md) | План подготовки проекта БЕЗ доступа к Unity | ~2500 |

### Системы данных

| Файл | Описание | Токенов |
|------|----------|---------|
| [CONFIGURATIONS.md](./CONFIGURATIONS.md) | Конфигурации: уровни культивации, техники, материалы | ~4500 |
| [ALGORITHMS.md](./ALGORITHMS.md) | Алгоритмы и формулы: урон, подавление, Ци буфер | ~5500 |

### Игровые системы

| Файл | Описание | Токенов |
|------|----------|---------|
| [COMBAT_SYSTEM.md](./COMBAT_SYSTEM.md) | Боевая система, пайплайн урона 10 слоёв | ~4000 |
| [TECHNIQUE_SYSTEM.md](./TECHNIQUE_SYSTEM.md) | Система техник культивации | ~3500 |
| [QI_SYSTEM.md](./QI_SYSTEM.md) | Система Ци: накопление, проводимость, плотность | ~2500 |
| [BODY_SYSTEM.md](./BODY_SYSTEM.md) | Kenshi-style система тела, двойная HP | ~4000 |
| [INVENTORY_SYSTEM.md](./INVENTORY_SYSTEM.md) | Инвентарь, экипировка, слоты | ~3000 |
| [EQUIPMENT_SYSTEM.md](./EQUIPMENT_SYSTEM.md) | Экипировка v2: материалы, грейды, прочность | ~3500 |
| [BUFF_SYSTEM.md](./BUFF_SYSTEM.md) | **🆕** Система баффов/дебаффов | ~2500 |
| [FORMATION_SYSTEM.md](./FORMATION_SYSTEM.md) | **🆕** Система формаций, физические носители | ~4000 |
| [CHARGER_SYSTEM.md](./CHARGER_SYSTEM.md) | Зарядные ядра для формаций | ~2000 |
| [ELEMENTS_SYSTEM.md](./ELEMENTS_SYSTEM.md) | Стихии, противоположности, сродство | ~2500 |
| [MODIFIERS_SYSTEM.md](./MODIFIERS_SYSTEM.md) | Модификаторы, баффы, дебаффы | ~2500 |
| [STAT_THRESHOLD_SYSTEM.md](./STAT_THRESHOLD_SYSTEM.md) | Пороги развития характеристик | ~2500 |
| [GENERATORS_SYSTEM.md](./GENERATORS_SYSTEM.md) | **🆕** Генераторы: NPC, техники, предметы | ~3500 |
| [GENERATORS_NAME_FIX.md](./GENERATORS_NAME_FIX.md) | **🆕** Исправление грамматики в генераторах | ~1500 |

### NPC и AI

| Файл | Описание | Токенов |
|------|----------|---------|
| [NPC_AI_SYSTEM.md](./NPC_AI_SYSTEM.md) | AI NPC, Spinal Controller, рефлексы | ~4000 |

### Мир и время

| Файл | Описание | Токенов |
|------|----------|---------|
| [WORLD_SYSTEM.md](./WORLD_SYSTEM.md) | Мир, локации, 3D координаты | ~3000 |
| [WORLD_MAP_SYSTEM.md](./WORLD_MAP_SYSTEM.md) | **🆕** Мировая карта: секторы, местность, навигация | ~6000 |
| [LOCATION_MAP_SYSTEM.md](./LOCATION_MAP_SYSTEM.md) | **🆕** Локации: генерация зданий, препятствий, ресурсов | ~5500 |
| [TRANSITION_SYSTEM.md](./TRANSITION_SYSTEM.md) | **🆕** Система переходов между картами | ~4500 |
| [TIME_SYSTEM.md](./TIME_SYSTEM.md) | Система времени, тики, календарь | ~2500 |
| [FACTION_SYSTEM.md](./FACTION_SYSTEM.md) | Фракции, отношения, нации | ~2500 |
| [LORE_SYSTEM.md](./LORE_SYSTEM.md) | Лор, истории, миростроение | ~2000 |
| [ENTITY_TYPES.md](./ENTITY_TYPES.md) | Иерархия типов сущностей | ~2000 |

### Специальные системы

| Файл | Описание | Токенов |
|------|----------|---------|
| [SAVE_SYSTEM.md](./SAVE_SYSTEM.md) | Сохранения, JSON, сериализация | ~2500 |

### Развитие персонажа

| Файл | Описание | Токенов |
|------|----------|---------|
| [MORTAL_DEVELOPMENT.md](./MORTAL_DEVELOPMENT.md) | Этапы развития смертного до культивации | ~4000 |
| [DEVELOPMENT_PLAN.md](./DEVELOPMENT_PLAN.md) | Общий план развития проекта | ~3000 |
| [ACHIEVEMENT_SYSTEM.md](./ACHIEVEMENT_SYSTEM.md) | **🆕** Система достижений | ~2500 |
| [JOURNAL_SYSTEM.md](./JOURNAL_SYSTEM.md) | **🆕** Система журнала/летописи | ~2000 |

### Настройка Unity Editor

| Файл | Описание | Токенов |
|------|----------|---------|
| [asset_setup/README.md](./asset_setup/README.md) | Обзор настроек | ~500 |
| [asset_setup/01_CultivationLevelData.md](./asset_setup/01_CultivationLevelData.md) | Настройка уровней культивации | ~3000 |
| [asset_setup/02_MortalStageData.md](./asset_setup/02_MortalStageData.md) | Настройка этапов смертных | ~2500 |
| [asset_setup/03_ElementData.md](./asset_setup/03_ElementData.md) | Настройка элементов | ~2000 |
| [asset_setup/04_BasicScene.md](./asset_setup/04_BasicScene.md) | Создание базовой сцены | ~1500 |
| [asset_setup/04_BasicScene_SemiAuto.md](./asset_setup/04_BasicScene_SemiAuto.md) | Полуавтоматическая настройка сцены | ~1500 |
| [asset_setup/05_PlayerSetup.md](./asset_setup/05_PlayerSetup.md) | Настройка Player | ~1500 |
| [asset_setup/05_PlayerSetup_SemiAuto.md](./asset_setup/05_PlayerSetup_SemiAuto.md) | Полуавтоматическая настройка игрока | ~1500 |
| [asset_setup/06_TechniqueData.md](./asset_setup/06_TechniqueData.md) | Настройка техник | ~2500 |
| [asset_setup/06_TechniqueData_SemiAuto.md](./asset_setup/06_TechniqueData_SemiAuto.md) | Полуавтоматическая генерация техник | ~2000 |
| [asset_setup/07_NPCPresetData.md](./asset_setup/07_NPCPresetData.md) | Настройка пресетов NPC | ~2000 |
| [asset_setup/07_NPCPresetData_SemiAuto.md](./asset_setup/07_NPCPresetData_SemiAuto.md) | Полуавтоматическая генерация NPC | ~1500 |
| [asset_setup/08_EquipmentData.md](./asset_setup/08_EquipmentData.md) | Настройка экипировки | ~2500 |
| [asset_setup/08_EquipmentData_SemiAuto.md](./asset_setup/08_EquipmentData_SemiAuto.md) | Полуавтоматическая генерация экипировки | ~2000 |
| [asset_setup/09_EnemySetup.md](./asset_setup/09_EnemySetup.md) | Настройка врагов | ~2000 |
| [asset_setup/09_EnemySetup_SemiAuto.md](./asset_setup/09_EnemySetup_SemiAuto.md) | Полуавтоматическая генерация врагов | ~1500 |
| [asset_setup/10_QuestSetup.md](./asset_setup/10_QuestSetup.md) | Настройка квестов | ~2000 |
| [asset_setup/10_QuestSetup_SemiAuto.md](./asset_setup/10_QuestSetup_SemiAuto.md) | Полуавтоматическая генерация квестов | ~1500 |
| [asset_setup/11_ItemData.md](./asset_setup/11_ItemData.md) | Настройка предметов | ~2000 |
| [asset_setup/11_ItemData_SemiAuto.md](./asset_setup/11_ItemData_SemiAuto.md) | Полуавтоматическая генерация предметов | ~1500 |
| [asset_setup/12_MaterialData.md](./asset_setup/12_MaterialData.md) | Настройка материалов | ~2000 |
| [asset_setup/12_MaterialData_SemiAuto.md](./asset_setup/12_MaterialData_SemiAuto.md) | Полуавтоматическая генерация материалов | ~1500 |
| [asset_setup/13_SpriteSetup.md](./asset_setup/13_SpriteSetup.md) | Настройка спрайтов | ~1500 |
| [asset_setup/13_SpriteSetup_QuickStart.md](./asset_setup/13_SpriteSetup_QuickStart.md) | Быстрый старт для спрайтов | ~1000 |

### Примеры реализации

| Файл | Описание | Токенов |
|------|----------|---------|
| [examples/NPC_L6_ASSEMBLY_EXAMPLE.md](./examples/NPC_L6_ASSEMBLY_EXAMPLE.md) | Пример сборки NPC L6 | ~2000 |
| [examples/NameGenerator_Russian.md](./examples/NameGenerator_Russian.md) | Русский генератор имён | ~1500 |
| [examples/TechniqueEffectsSystem.md](./examples/TechniqueEffectsSystem.md) | Система эффектов техник | ~2000 |
| [examples/CharacterSpriteMirroring.md](./examples/CharacterSpriteMirroring.md) | Зеркалирование спрайтов персонажа | ~1500 |
| [examples/OrbitalWeaponSystem.md](./examples/OrbitalWeaponSystem.md) | Система орбитального оружия | ~2000 |
| [examples/BuffSystem_Examples.md](./examples/BuffSystem_Examples.md) | **🆕** Примеры кода системы баффов | ~2500 |
| [examples/FormationSystem_Examples.md](./examples/FormationSystem_Examples.md) | **🆕** Примеры кода системы формаций | ~2500 |
| [examples/StatThresholdSystem_Examples.md](./examples/StatThresholdSystem_Examples.md) | **🆕** Примеры кода порогов развития | ~2500 |

### Планы внедрения

| Файл | Описание | Токенов |
|------|----------|---------|
| [implementation_plans/ASSET_CREATION_PLAN.md](./implementation_plans/ASSET_CREATION_PLAN.md) | Создание .asset файлов | ~2000 |
| [implementation_plans/NEXT_ELEMENTS_PLAN.md](./implementation_plans/NEXT_ELEMENTS_PLAN.md) | Следующие элементы разработки | ~2500 |
| [implementation_plans/IMPLEMENTATION_PLAN_NEXT.md](./implementation_plans/IMPLEMENTATION_PLAN_NEXT.md) | План следующих этапов | ~2000 |

### Аналитические отчёты

| Файл | Описание | Токенов |
|------|----------|---------|
| [ANALYSIS_REPORT.md](./ANALYSIS_REPORT.md) | Общий аналитический отчёт | ~3000 |

---

## 📊 Статистика

| Метрика | Значение |
|---------|----------|
| Всего документов игры | 68+ |
| Общий объём | ~176,000 токенов |
| Средний размер | ~2,500 токенов |
| Файлов примеров кода | 8 |

---

## ⚠️ Важно о коде

> **В документации НЕТ кода** — только теоретические описания.
> 
> Весь код перенесён в `docs/examples/` для удобства разработчиков.
> 
> При необходимости посмотреть реализацию — используйте файлы примеров.

---

## 🔗 Связи документов

```
UNITY_VERSION_COMPARISON.md (выбор версии)
    │
    ▼
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
    │   ├── TRANSITION_SYSTEM.md
    │   ├── TIME_SYSTEM.md
    │   ├── FACTION_SYSTEM.md
    │   └── ENTITY_TYPES.md
    │
    ├── SAVE_SYSTEM.md
    │
    └── GENERATORS_SYSTEM.md
        └── GENERATORS_NAME_FIX.md
```

---

## 📝 Рекомендации по чтению

### Для старта разработки:
1. UNITY_VERSION_COMPARISON.md — выбор версии Unity
2. ARCHITECTURE.md — общая картина
3. DATA_MODELS.md — структуры данных
4. ALGORITHMS.md — ключевые формулы

### Для боевой системы:
1. COMBAT_SYSTEM.md — пайплайн урона
2. TECHNIQUE_SYSTEM.md — техники
3. QI_SYSTEM.md — Ци буфер
4. BODY_SYSTEM.md — телесный урон
5. BUFF_SYSTEM.md — баффы/дебаффы

### Для систем мира:
1. WORLD_SYSTEM.md — локации
2. TIME_SYSTEM.md — время
3. NPC_AI_SYSTEM.md — население

### Для формаций:
1. FORMATION_SYSTEM.md — формации и ядра
2. CHARGER_SYSTEM.md — зарядники
3. QI_SYSTEM.md — Ци

---

## 🛠️ Рекомендованная версия Unity

**Unity 6.3 (6000.3)** — рекомендуется для проекта.

**Причины:**
- DOTS 2.0 для NPC AI (10-100x улучшение производительности)
- GPU Resident Drawer для рендеринга
- Долгосрочная перспектива (5+ лет)
- Стабильная версия с актуальной документацией

**Документация:** https://docs.unity3d.com/Manual/index.html

Подробнее: [UNITY_VERSION_COMPARISON.md](./UNITY_VERSION_COMPARISON.md)

---

*Документ обновлён: 2026-04-04*  
*Изменения: Код перенесён из документации в docs/examples/*  
*Проект: Cultivation World Simulator Unity Migration*

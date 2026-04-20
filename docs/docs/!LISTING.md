# 📂 Unity Documentation Listing

**Версия:** 2.4  
**Дата:** 2026-04-09  
**Проект:** Cultivation World Simulator → Unity Migration

---

## ⚠️ Важно

> **Все документы содержат ТОЛЬКО ТЕОРИЮ.**  
> **НЕТ КОДА** — только теоретические описания систем.  
> Код перенесён в `docs_examples/` — см. отдельную папку.  
> Реализация в Unity: `UnityProject/Local/Assets/Scripts/`.

---

## 📋 Список документов (только папка docs/)

### 🤖 Справочники

| Файл | Описание | Токенов |
|------|----------|---------|
| [!Ai_Skills.md](./!Ai_Skills.md) | Доступные Skills ИИ-ассистента (ASR, TTS, LLM, Web-Search и др.) | ~3500 |
| [!LISTING.md](./!LISTING.md) | Этот файл — список всех документов | ~2000 |
| [UNITY_DOCS_LINKS.md](./UNITY_DOCS_LINKS.md) | Листинг 150+ ссылок на документацию Unity 6.3 | ~4500 |

### Архитектура и основы

| Файл | Описание | Токенов |
|------|----------|---------|
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Общая архитектура Unity проекта, паттерны, модули | ~3500 |
| [DATA_MODELS.md](./DATA_MODELS.md) | Модели данных из Prisma schema, 17 сущностей | ~5000 |

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
| [BUFF_SYSTEM.md](./BUFF_SYSTEM.md) | Система баффов/дебаффов | ~2500 |
| [FORMATION_SYSTEM.md](./FORMATION_SYSTEM.md) | Система формаций, физические носители | ~4000 |
| [CHARGER_SYSTEM.md](./CHARGER_SYSTEM.md) | Зарядные ядра для формаций | ~2000 |
| [ELEMENTS_SYSTEM.md](./ELEMENTS_SYSTEM.md) | Стихии, противоположности, сродство | ~2500 |
| [MODIFIERS_SYSTEM.md](./MODIFIERS_SYSTEM.md) | Модификаторы, баффы, дебаффы | ~2500 |
| [STAT_THRESHOLD_SYSTEM.md](./STAT_THRESHOLD_SYSTEM.md) | Пороги развития характеристик | ~2500 |
| [GENERATORS_SYSTEM.md](./GENERATORS_SYSTEM.md) | Генераторы: NPC, техники, предметы | ~3500 |
| [GENERATORS_NAME_FIX.md](./GENERATORS_NAME_FIX.md) | Исправление грамматики в генераторах | ~1500 |

### NPC и AI

| Файл | Описание | Токенов |
|------|----------|---------|
| [NPC_AI_SYSTEM.md](./NPC_AI_SYSTEM.md) | AI NPC, Spinal Controller, рефлексы | ~4000 |

### Мир и время

| Файл | Описание | Токенов |
|------|----------|---------|
| [WORLD_SYSTEM.md](./WORLD_SYSTEM.md) | Мир, локации, 3D координаты | ~3000 |
| [WORLD_MAP_SYSTEM.md](./WORLD_MAP_SYSTEM.md) | Мировая карта: секторы, местность, навигация | ~6000 |
| [LOCATION_MAP_SYSTEM.md](./LOCATION_MAP_SYSTEM.md) | Локации: генерация зданий, препятствий, ресурсов | ~5500 |
| [TILE_SYSTEM.md](./TILE_SYSTEM.md) | Тайловая система: структура, параметры, генерация | ~6000 |
| [TRANSITION_SYSTEM.md](./TRANSITION_SYSTEM.md) | Система переходов между картами | ~4500 |
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
| [PERK_SYSTEM.md](./PERK_SYSTEM.md) | Система перков | ~2000 |
| [JOURNAL_SYSTEM.md](./JOURNAL_SYSTEM.md) | Система журнала/летописи | ~2000 |

### Тестирование

| Файл | Описание | Токенов |
|------|----------|---------|
| [UNIT_TEST_RULES.md](./UNIT_TEST_RULES.md) | Правила написания unit тестов | ~1500 |

---

## 📊 Статистика

| Метрика | Значение |
|---------|----------|
| Основных документов | 40 |
| Общий объём | ~100,000 токенов |

---

## 🔗 Связи документов

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
1. ARCHITECTURE.md — общая картина
2. DATA_MODELS.md — структуры данных
3. ALGORITHMS.md — ключевые формулы

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

---

## 📁 Связанные папки (в корне проекта)

| Папка | Назначение |
|-------|------------|
| `docs_asset_setup/` | Инструкции внедрения через Unity Editor |
| `docs_examples/` | Примеры реализации кода |
| `docs_temp/` | Временная документация, черновики |
| `docs_implementation_plans/` | Планы внедрения |

---

*Документ обновлён: 2026-04-09*  
*Изменения: Реструктуризация — вынос asset_setup, examples, temp_docs, implementation_plans в корень*  
*Проект: Cultivation World Simulator Unity Migration*

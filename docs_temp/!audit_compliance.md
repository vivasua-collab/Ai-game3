# 🔴 Аудит соответствия docs_temp/ ↔ docs/

**Версия:** 1.0
**Дата:** 2026-04-28
**Проект:** Cultivation World Simulator (Unity 6.3 URP 2D)
**Объект:** Временная документация `docs_temp/` — проверка на соответствие основной документации `docs/`, выявление противоречий, оценка новых идей

---

## ⚠️ Методология

> Каждый файл `docs_temp/` категорий **🟢 Актуальный** и **🟡 В разработке** проверен на соответствие своему документу-двойнику в `docs/`. Для файлов без аналога в `docs/` — оценка как новой идеи.
>
> **Критерии оценки:**
> - **Противоречие** — информация в docs_temp/ прямо расходится с утверждённым docs/
> - **Расширение** — информация дополняет docs/ без противоречий
> - **Новая идея** — концепция, отсутствующая в docs/
> - **Устаревшее** — информация, потерявшая актуальность после обновлений docs/
>
> **Шкала соответствия:** ✅ СООТВЕТСТВУЕТ | ⚠️ ЧАСТИЧНО | ❌ ПРОТИВОРЕЧИТ | ➖ Нет аналога в docs/

---

## 📊 Сводка результатов

| Категория | Файлов проверено | ✅ | ⚠️ | ❌ | ➖ |
|-----------|:---:|:---:|:---:|:---:|:---:|
| 📐 Черновики систем | 7 | 0 | 3 | 4 | 0 |
| 🧪 Примеры и спецификации | 6 | 3 | 1 | 1 | 1 |
| 🔧 Тех. исследования | 2 | 1 | 1 | 0 | 0 |
| 📊 Аналитика | 1 | 0 | 0 | 1 | 0 |
| 📖 Справочники | 6 | 0 | 3 | 0 | 3 |
| **Итого** | **22** | **4** | **8** | **6** | **4** |

> **Критических противоречий:** 6 файлов
> **Требуют доработки:** 8 файлов
> **Новых идей:** 4 системы (tools, achievements, orbital, memory)

---

## 🔴 КРИТИЧЕСКИЕ ПРОТИВОРЕЧИЯ (блокируют разработку)

### КП-1. Фундаментальный конфликт моделей поглощения Ци

**Файлы:** `QI_ABSORPTION_RADIUS.md`, `BREAKTHROUGH_MODELS_COMPARISON.md`
**Противоречит:** `QI_SYSTEM.md`, `ALGORITHMS.md`

| Параметр | docs/ (environmentMult) | docs_temp/ (конечный запас) |
|----------|------------------------|---------------------------|
| Модель | Бесконечное Ци, множитель среды | Конечный запас Ци в зоне, истощение |
| Формула | `absorbedQi = time × conductivity × environmentMult` | `qiFromMeditation = min(time × conductivity, qiAvailableInZone)` |
| Баланс L1→L9 | ~6.5 лет (модель В) | ~2.8 года (с истощением) |
| Зона | environmentMult (безразмерный) | ρ_окр × V_zone (конкретные ед/м³) |

**Влияние:** Определяет ВСЁ — баланс прорывов, экономику камней Ци, ценность зарядников, механику медитации.

**Рекомендация:** Принять решение: environmentMult (бесконечная модель) vs конечный запас зоны. Возможна гибридная модель — environmentMult как лимит скорости + QiStock зоны для истощения.

---

### КП-2. Три неразрешённых вопроса инвентаря

**Файлы:** `INVENTORY_UI_DRAFT.md`, `INVENTORY_IMPLEMENTATION_PLAN.md`
**Противоречит:** `INVENTORY_SYSTEM.md`

| Вопрос | docs/ | docs_temp/ |
|--------|-------|-----------|
| Духовное хранилище | 20 слотов | Без ограничений + каталогизатор |
| Пояс быстрого доступа | Фиксированные 4 слота | 0-4 слота от типа пояса |
| Архитектура «Матрёшка» | 4 независимых слоя экипировки | «Один предмет на слот, без слоёв» |

**Дополнительно:** Файловая структура основного документа (InventoryManager.cs и др.) полностью не совпадает с реальным кодом (InventoryController.cs, EquipmentController.cs).

**Рекомендация:** Провести ревизию INVENTORY_SYSTEM.md до v2.0, разрешив все 3 вопроса на основе решений из черновиков.

---

### КП-3. Трёхсторонний конфликт «docs/ ↔ docs_temp/ ↔ код»

**Файл:** `BuffSystem_Examples.md`, `RunningTests.md`
**Противоречит:** `BUFF_MODIFIERS_SYSTEM.md`

| Аспект | docs/ (желаемое) | docs_temp/ | Код (фактическое) |
|--------|-----------------|-----------|-------------------|
| ConductivityBoost | ⛔ УДАЛЁН | Содержится | Реализован в BuffManager.cs |
| BuffType enum | 30+ значений (AttackBoost и т.д.) | 30+ значений | {Buff, Debuff, Neutral} — 3 значения |

**Влияние:** Если код писать по docs_temp/ — будет рассинхрон с docs/. Если по docs/ — нужен рефакторинг кода.

**Рекомендация:** Принять решение: удалить payback из кода (согласно docs/) или обновить docs/ (согласно коду).

---

### КП-4. Критически неверные грейд-множители в TECHNIQUE_USAGE_REPORT

**Файл:** `TECHNIQUE_USAGE_REPORT.md`
**Противоречит:** `TECHNIQUE_SYSTEM.md`, `ALGORITHMS.md`

| Грейд | TECHNIQUE_USAGE_REPORT | TECHNIQUE_SYSTEM + ALGORITHMS | Δ |
|-------|----------------------|------------------------------|---|
| Refined | ×1.2 | ×1.3 | ❌ |
| Perfect | ×1.4 | ×1.6 | ❌ |
| Transcendent | ×1.6 | ×2.0 | ❌ |
| Ultimate | ×1.3 | ×2.0 | ❌ |

**Влияние:** Если реализовать по этому отчёту — боевой баланс сломан. 4 неверных значения из 5.

**Рекомендация:** Исправить множители по ALGORITHMS.md §4.2. Пересчитать все примеры.

---

### КП-5. Element.Poison в TechniqueEffectsSystem

**Файл:** `TechniqueEffectsSystem.md`
**Противоречит:** `TECHNIQUE_SYSTEM.md`, `ELEMENTS_SYSTEM.md`, `ALGORITHMS.md`

Все три документа docs/ утверждают: **«Poison (Яд) — НЕ стихия»**. TechniqueEffectsSystem описывает `Element.Poison` как член перечисления + `EffectType.PoisonCloud` + обработку в DirectionalEffect.

**Дополнительно:** TechniqueType.Offensive не существует — актуальный тип Combat.

**Рекомендация:** Убрать Element.Poison из enum, заменить TechniqueType.Offensive → Combat.

---

### КП-6. Расхождение зарядников: множители vs абсолютные значения

**Файл:** `BREAKTHROUGH_MODELS_COMPARISON.md`
**Подтверждает расхождение внутри docs/:** `QI_SYSTEM.md` (множители ×0.5...×2.0) vs `CHARGER_SYSTEM.md` (абсолютные значения 5...50 ед/сек)

**Рекомендация:** Унифицировать представление зарядников в docs/ — выбрать ОДИН формат.

---

## ⚠️ ЧАСТИЧНЫЕ ПРОТИВОРЕЧИЯ (требуют доработки)

### ЧП-1. INVENTORY_FLAGS_AUDIT.md — «Матрёшка» vs «один предмет на слот»

Аудит предлагает удалить слои (Armor/Clothing) и перейти к «один предмет на слот». Основной документ описывает 4-слойную «Матрёшку». Частичное противоречие: аудит корректно фиксирует текущий код, но предложение по рефакторингу не согласовано с docs/.

**Оценка ценности:** ВЫСОКАЯ — содержит критический технический анализ (безопасность замены enum, QiController API).

---

### ЧП-2. INVENTORY_UI_DRAFT.md — ID слотов snake_case vs PascalCase

Черновик использует `weapon_main`, `ring_left_1`. Основной документ: `WeaponMain`, `RingLeft1`. Нет единого стиля.

---

### ЧП-3. NameGenerator_Russian.md — устаревшая архитектура

| Аспект | docs_temp/ | Реальный код |
|--------|-----------|-------------|
| Namespace | CultivationWorld.Generation | CultivationGame.Generators |
| Класс генератора | NameGenerator : MonoBehaviour | NameBuilder — обычный класс |
| Хранение данных | ScriptableObject базы | static NamingDatabase |
| Структура слова | RussianWord (class) | NounWithGender (struct) |

Грамматическая часть актуальна, архитектура — нет.

---

### ЧП-4. CODE_REFERENCE.md — возможно устарел

Сгенерирован 2026-04-13. Может не отражать последних изменений в коде (проводимость, Qi long migration и др.). Единственный полный справочник кодовой базы (174 файла).

---

### ЧП-5. WORKFLOW_GITHUB_UNITY.md — ошибка про .meta файлы

Рекомендация «Не коммитить .meta файлы» ПРОТИВОРЕЧИТ стандартной практике Unity — .meta файлы ОБЯЗАТЕЛЬНО нужно коммитить.

---

### ЧП-6. GIT_WORKFLOW_TWO_PC.md — расхождение имени ветки

Указана ветка «main», в WORKFLOW_GITHUB_UNITY.md — «main3Uniny».

---

### ЧП-7. TechniqueEffectsSystem.md — Time.deltaTime вместо тиков

Код использует Time.deltaTime (реальное время), что противоречит TECHNIQUE_SYSTEM.md §Принцип 3: «Все эффекты измеряются в тиках».

---

### ЧП-8. TECHNIQUE_USAGE_REPORT.md — устаревший пайплайн урона

Описан 10-слойный пайплайн. ALGORITHMS.md §5 — 11-слойный (отсутствуют слои 1b «Урон оружия» и 3b «Бафф/Дебафф формаций»).

---

## ✅ СООТВЕТСТВУЮТ docs/ (без противоречий)

### StatThresholdSystem_Examples.md
- Формула `floor(currentStat/10)` ✅
- Капы дельт (10.0/15.0) ✅
- Правила сна ✅
- **Ценность:** ВЫСОКАЯ — полезная реализация

### FormationSystem_Examples.md
- CONTOUR_QI_BY_LEVEL таблица ✅
- Множители ёмкости ✅
- Enums совпадают ✅
- **Ценность:** ВЫСОКАЯ — детальные примеры

### NPC_L6_ASSEMBLY_EXAMPLE.md
- Генерация NPC L6 корректна ✅
- Формулы проводимости и Ци ✅
- **Ценность:** СРЕДНЯЯ — одноразовый пример

### COMPUTATIONAL_RESOURCES_CALCULATION.md
- Нет противоречий с WORLD_MAP_SYSTEM.md ✅
- Теоретические расчёты корректны ✅
- **Ценность:** СРЕДНЯЯ — не подтверждено профилированием

---

## ➖ НОВЫЕ ИДЕИ (нет аналога в docs/)

### НИ-1. tool_system_draft.md — Система инструментов добычи 🔴 КРИТИЧНА

**Идея:** Топор/кирка/серп как расширение экипировки. ToolData : EquipmentData, экипируется в WeaponMain, множители по категориям добычи (Wood/Stone/Ore/Plant). Износ, тиры, грейды.

**Связь с docs/:**
- EQUIPMENT_SYSTEM.md — прямо ссылается на tool_system_draft ✅
- INVENTORY_SYSTEM.md — инструменты как предметы
- TILE_SYSTEM.md — Harvestable объекты

**Противоречия с docs/:**
1. EQUIPMENT_SYSTEM §1.2: Tool = 3 грейда (Damaged/Common/Refined) vs черновик: все 5 грейдов
2. Модель износа: понижение грейда vs отдельная шкала durability — не определено
3. Слот WeaponMain блокирует оружие — нет альтернативы (ToolBelt?)

**Проработка:** Частичная — есть формулы и тиры, но нет ToolData.cs, нет UI, 3 открытых вопроса

**Ценность:** 🔴 КРИТИЧНА — без инструментов добыча = плоский геймплей кулаками

**Готовность к переносу:** Нужна доработка (решить 3 конфликта + создать ToolData.cs)

---

### НИ-2. ACHIEVEMENT_SYSTEM.md — Система достижений 🟡 ПОЛЕЗНАЯ

**Идея:** 34 достижения по 7 категориям (культивация, бой, исследование, коллекционирование, социальные, сюжетные, скрытые). Награды в Ци и титулах.

**Связь с docs/:**
- COMBAT_SYSTEM.md — Condition DefeatEnemies/KillBoss
- NPC_AI_SYSTEM.md — Condition TalkToNPC/MakeFriend
- QI_SYSTEM.md — награды «+N Ци» (механизм не описан)
- SAVE_SYSTEM.md — AchievementSaveData

**Противоречия:**
1. Награда «+100 Ци» — qiRegen неизменяем, разовая выдача не описана
2. Составные условия — текущая модель (enum + число) не поддерживает
3. AchievementType.Hidden — атрибут видимости смешан с типом

**Проработка:** Частичная — есть архитектура, JSON-данные, UI-макеты, но нет C#-кода менеджера

**Ценность:** 🟡 ПОЛЕЗНАЯ — повышает вовлечённость, но не критична для MVP

**Готовность к переносу:** Нужна доработка

---

### НИ-3. OrbitalWeaponSystem.md — Орбитальное оружие 🟡 ПОЛЕЗНАЯ

**Идея:** Оружие, вращающееся вокруг персонажа по орбите. При атаке ускоряется, наносит урон. Визуальные эффекты по элементам.

**Связь с docs/:**
- EQUIPMENT_SYSTEM.md — прямо ссылается на OrbitalWeaponSystem ✅
- COMBAT_SYSTEM.md — нанесение урона

**Критические противоречия:**
1. 🔴 Дублирует боевые интерфейсы: собственные ICombatTarget, IHealth, DamageInfo вместо CombatManager
2. Нет слота экипировки — вне «Матрёшки»
3. Нет стоимости Ци за атаку — противоречит лору
4. CharacterController2D вместо PlayerController.cs

**Проработка:** Частичная — есть рабочий код, спрайты, UI-индикатор

**Ценность:** 🟡 ПОЛЕЗНАЯ — визуально эффектная, жанрово уместная для xianxia

**Готовность к переносу:** Нужна доработка (убрать дублирующие интерфейсы, добавить Ци-стоимость)

---

### НИ-4. LONG_TERM_MEMORY_SCHEME.md — Память ИИ-агента 🟢 МЕТА-СИСТЕМА

**Идея:** 3 схемы организации памяти ИИ-ассистента-разработчика: A) чекпоинты+worklog, B) SQLite+JSONL, C) чекпоинты+SESSION_SUMMARY. Рекомендация: схема C.

**Связь с docs/:** Нет — мета-документ процесса разработки

**Противоречия:** Нет

**Проработка:** Полная — детальное сравнение, количественные оценки, SQL-схема для B

**Ценность:** 🟢 ПОЛЕЗНАЯ — экономия 50-70% токенов, но не влияет на игровой код

**Готовность к переносу:** Схема C готова (частично внедрена — SESSION_SUMMARY.md существует). Схема B — рано.

---

## 📖 САМОСТОЯТЕЛЬНЫЕ ДОКУМЕНТЫ (нет аналога, готовы к использованию)

### CharacterSpriteMirroring.md
- Зеркалирование спрайтов: scale.x vs flipX
- Решение проблемы дочерних объектов через IndependentScale
- Структура префаба Player
- **Ценность:** ВЫСОКАЯ — уникальный, нет дублирования
- **Рекомендация:** Перенести в docs/ (как SPRITE_MIRRORING.md или дополнить SPRITE_INDEX.md)

### WORKFLOW_GITHUB_UNITY.md
- Workflow Sandbox → GitHub → Unity
- **Ошибка:** «Не коммитить .meta файлы» — неверно
- **Рекомендация:** Исправить ошибку про .meta

### GIT_WORKFLOW_TWO_PC.md
- Workflow для двух ПК с Unity
- Расхождение имени ветки с WORKFLOW_GITHUB_UNITY.md
- **Рекомендация:** Объединить с WORKFLOW_GITHUB_UNITY.md

---

## 🗑️ УСТАРЕВШИЕ / ИСТОРИЧЕСКИЕ ФАЙЛЫ

> Эти файлы НЕ проверялись на соответствие docs/, так как помечены как ⚪ Исторический в !listing.md.
> Полный список: 32 файла (63% docs_temp/).
> **Рекомендация:** Архивировать в docs_old/ при следующей чистке.

Ключевые устаревшие файлы с комментариями:

| Файл | Причина устаревания |
|------|---------------------|
| Все аудиты кода (13 файлов) | Исправления уже в коде |
| CONTRADICTIONS_AUDIT_2026-04-23.md | Заменён AUDIT_DOCS_CONTRADICTIONS_2026-04-27.md |
| !CONTRADICTIONS_REPORT.md, !_v2.md | Заменены 3-м аудитом |
| PROJECT_SETUP_PLAN.md | Проект уже настроен |
| TILE_SYSTEM_IMPLEMENTATION.md | Исторические заметки |
| ANALYSIS_REPORT.md, MIGRATION_ANALYSIS.md | Разовые отчёты |
| LOST_SESSION_ANALYSIS.md | Разовый анализ |

---

## 📋 МАТРИЦА СООТВЕТСТВИЯ

```
docs_temp/ файл                          → docs/ документ                Статус
─────────────────────────────────────────────────────────────────────────────────
ЧЕРНОВИКИ:
INVENTORY_UI_DRAFT.md                    → INVENTORY_SYSTEM.md          ⚠️ ЧАСТИЧНО
INVENTORY_FLAGS_AUDIT.md                 → INVENTORY_SYSTEM.md          ⚠️ ЧАСТИЧНО
INVENTORY_IMPLEMENTATION_PLAN.md         → INVENTORY_SYSTEM.md          ⚠️ ЧАСТИЧНО
QI_ABSORPTION_RADIUS.md                 → QI_SYSTEM.md, ALGORITHMS.md  ❌ ПРОТИВОРЕЧИТ
BREAKTHROUGH_MODELS_COMPARISON.md        → ALGORITHMS.md, QI_SYSTEM.md ⚠️ ЧАСТИЧНО
TechniqueEffectsSystem.md               → TECHNIQUE_SYSTEM.md          ⚠️ ЧАСТИЧНО
ACHIEVEMENT_SYSTEM.md                   → (нет аналога)                ➖ НОВАЯ ИДЕЯ
LONG_TERM_MEMORY_SCHEME.md              → (нет аналога)                ➖ МЕТА
tool_system_draft.md                    → EQUIPMENT_SYSTEM.md          ➖ НОВАЯ ИДЕЯ
OrbitalWeaponSystem.md                  → EQUIPMENT_SYSTEM.md          ➖ НОВАЯ ИДЕЯ

ПРИМЕРЫ:
BuffSystem_Examples.md                  → BUFF_MODIFIERS_SYSTEM.md     ❌ ПРОТИВОРЕЧИТ
StatThresholdSystem_Examples.md         → STAT_THRESHOLD_SYSTEM.md     ✅ СООТВЕТСТВУЕТ
FormationSystem_Examples.md             → FORMATION_SYSTEM.md          ✅ СООТВЕТСТВУЕТ
NPC_L6_ASSEMBLY_EXAMPLE.md              → GENERATORS_SYSTEM.md         ✅ СООТВЕТСТВУЕТ

АНАЛИТИКА:
TECHNIQUE_USAGE_REPORT.md               → TECHNIQUE_SYSTEM.md          ❌ ПРОТИВОРЕЧИТ

СПРАВОЧНИКИ:
NameGenerator_Russian.md                → GENERATORS_SYSTEM.md         ⚠️ ЧАСТИЧНО
RunningTests.md                         → UNIT_TEST_RULES.md           ⚠️ ЧАСТИЧНО
CharacterSpriteMirroring.md             → (нет аналога)                ➖ ПЕРЕНОС
WORKFLOW_GITHUB_UNITY.md                → (нет аналога)                ➖ САМОСТОЯТЕЛЬНЫЙ
GIT_WORKFLOW_TWO_PC.md                  → (нет аналога)                ➖ САМОСТОЯТЕЛЬНЫЙ
CODE_REFERENCE.md                       → ARCHITECTURE.md, DATA_MODELS ⚠️ ЧАСТИЧНО
COMPUTATIONAL_RESOURCES_CALCULATION.md  → WORLD_MAP_SYSTEM.md          ✅ СООТВЕТСТВУЕТ
```

---

## 🎯 РЕКОМЕНДАЦИИ ПО ПРИОРИТЕТАМ

### 🔴 Критические (блокируют разработку)

| # | Рекомендация | Влияющие файлы |
|---|-------------|----------------|
| Р-1 | Принять решение по модели поглощения Ци: environmentMult vs конечный запас зоны | QI_ABSORPTION_RADIUS, BREAKTHROUGH_MODELS, QI_SYSTEM, ALGORITHMS |
| Р-2 | Провести ревизию INVENTORY_SYSTEM.md до v2.0: решить 3 вопроса (хранилище/пояс/слои) | INVENTORY_UI_DRAFT, INVENTORY_IMPLEMENTATION_PLAN, INVENTORY_SYSTEM |
| Р-3 | Исправить грейд-множители в TECHNIQUE_USAGE_REPORT по ALGORITHMS.md | TECHNIQUE_USAGE_REPORT |
| Р-4 | Разрешить трёхсторонний конфликт ConductivityBoost (docs ↔ temp ↔ код) | BUFF_MODIFIERS_SYSTEM, BuffSystem_Examples, BuffManager.cs |
| Р-5 | Убрать Element.Poison из TechniqueEffectsSystem | TechniqueEffectsSystem |
| Р-6 | Унифицировать представление зарядников в QI_SYSTEM.md и CHARGER_SYSTEM.md | QI_SYSTEM, CHARGER_SYSTEM |

### 🟡 Высокие (требуют доработки до переноса)

| # | Рекомендация | Влияющие файлы |
|---|-------------|----------------|
| Р-7 | Доработать tool_system_draft: решить 3 конфликта с EQUIPMENT_SYSTEM | tool_system_draft |
| Р-8 | Обновить NameGenerator_Russian.md (namespace, архитектура) | NameGenerator_Russian |
| Р-9 | Перегенерировать CODE_REFERENCE.md | CODE_REFERENCE |
| Р-10 | Исправить TechniqueEffectsSystem: тики вместо deltaTime, Offensive→Combat | TechniqueEffectsSystem |

### 🟢 Средние (полезные улучшения)

| # | Рекомендация | Влияющие файлы |
|---|-------------|----------------|
| Р-11 | Перенести CharacterSpriteMirroring.md в docs/ | CharacterSpriteMirroring |
| Р-12 | Объединить WORKFLOW_GITHUB_UNITY + GIT_WORKFLOW_TWO_PC | 2 workflow-файла |
| Р-13 | Исправить ошибку про .meta файлы в WORKFLOW_GITHUB_UNITY | WORKFLOW_GITHUB_UNITY |
| Р-14 | Обновить RunningTests.md (проверить ConductivityPayback тесты) | RunningTests |

### ⚪ Низкие (не срочные)

| # | Рекомендация | Влияющие файлы |
|---|-------------|----------------|
| Р-15 | Переписать BuffSystem_Examples.md на основе актуального кода | BuffSystem_Examples |
| Р-16 | Архивировать 32 исторических файла в docs_old/ | Все ⚪ файлы |
| Р-17 | Доработать ACHIEVEMENT_SYSTEM после стабилизации боевой/NPC систем | ACHIEVEMENT_SYSTEM |
| Р-18 | Рефакторинг OrbitalWeaponSystem (убрать дублирующие интерфейсы) | OrbitalWeaponSystem |

---

## 📊 ОЦЕНКА НОВЫХ ИДЕЙ — ПРИОРИТЕТ РЕАЛИЗАЦИИ

| Приоритет | Система | Ценность | Проработка | Блокирует |
|-----------|---------|----------|------------|-----------|
| 1 🔴 | tool_system_draft | Критичная | Частичная | Harvest System |
| 2 🟡 | OrbitalWeaponSystem | Полезная | Частичная | — |
| 3 🟡 | ACHIEVEMENT_SYSTEM | Полезная | Частичная | — |
| 4 🟢 | LONG_TERM_MEMORY_SCHEME | Мета | Полная | — |

---

## 💰 Оценка объёма работ по разрешению противоречий

| Рекомендация | Трудоёмкость | Токены для реализации |
|-------------|:---:|:---:|
| Р-1 (модель Ци) | Решение + обновление 2 файлов | ~8K |
| Р-2 (инвентарь v2.0) | Переработка основного документа | ~15K |
| Р-3 (грейд-множители) | Исправление 1 файла | ~2K |
| Р-4 (ConductivityBoost) | Решение + обновление docs/ или кода | ~5K |
| Р-5 (Element.Poison) | Исправление 1 файла | ~1K |
| Р-6 (зарядники) | Унификация 2 файлов | ~3K |
| Р-7 (tools) | Доработка драфта | ~8K |
| **Итого критические + высокие** | | **~42K** |

---

*Создано: 2026-04-28*
*Основано на анализе 22 актуальных/в разработке файлов docs_temp/ в сравнении с docs/*

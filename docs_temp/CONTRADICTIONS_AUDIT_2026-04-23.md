# 🔍 Аудит противоречий документации

**Дата:** 2026-04-23
**Авторитетные источники:** ARCHITECTURE.md, DATA_MODELS.md, ALGORITHMS.md
**Аудировано файлов:** 28
**Обнаружено противоречий:** 83 (20 CRITICAL, 30 MAJOR, 33 MINOR)

---

## ⚠️ Конфликты внутри авторитетных источников

Прежде чем фиксировать противоречия в подчинённых документах, необходимо разрешить расхождения **между самими авторитетными источниками**.

### A-01 — Тип Qi: `long` vs `int`

| Источник | Значение |
|----------|----------|
| ARCHITECTURE.md | "Qi type: **long** (Fix-01)" |
| DATA_MODELS.md | currentQi(**int**), accumulatedQi(**int**), coreCapacity(**int**) |
| SAVE_SYSTEM.md | long (правильно) |

**Решение:** Привести DATA_MODELS.md к `long` — ARCHITECTURE и код уже используют `long`.

### A-02 — Morphology: `amorphous` есть/нет

| Источник | Значение |
|----------|----------|
| ARCHITECTURE.md | 6 морфологий: humanoid, quadruped, bird, serpentine, arthropod, **amorphous** |
| DATA_MODELS.md | 5 морфологий: humanoid, quadruped, bird, serpentine, arthropod |

**Решение:** Добавить `amorphous` в DATA_MODELS.md — актуально для Spirit-сущностей.

---

## 🔴 СИСТЕМНЫЕ КОНФЛИКТЫ (затрагивают 3+ документов)

### S-01 — Система грейдов: ни один документ не согласован

Это **критический системный конфликт** — нет единого источника истины.

| Источник | Кол-во | Грейды | Множители |
|----------|--------|--------|-----------|
| **ARCHITECTURE.md** | 7 | Damaged, Normal, Fine, Excellent, Masterwork, Legendary, Transcendent | 0.7, 1.0, 1.3, 1.6, 2.0, 2.5, 3.0 |
| **DATA_MODELS.md** | 5 | damaged, common, refined, perfect, transcendent | (не указаны) |
| **ALGORITHMS.md** (техники) | 4 | common, refined, perfect, transcendent | 1.0, 1.3, 1.6, 2.0 |
| EQUIPMENT_SYSTEM.md | 5 | Damaged, Common, Refined, Perfect, Transcendent | Eff: 0.5/1.0/1.3/1.7/2.5, Dur: 0.5/1.0/1.5/2.5/4.0 |
| CHARGER_SYSTEM.md | 4 | Common, Refined, Perfect, Transcendent | 1.0, 1.5, 2.0, 3.0 |
| TECHNIQUE_SYSTEM.md | 4 | Common, Refined, Perfect, Transcendent | 1.0, **1.2**, **1.4**, **1.6** |
| INVENTORY_SYSTEM.md | 5 | Damaged, Common, Refined, Perfect, Transcendent | (ссылка на EQUIPMENT) |
| GENERATORS_NAME_FIX | 4 | Damaged, Refined, Perfect, Transcendent | (нет) |

**Проблемы:**
- Каждый документ даёт разные множители
- TECHNIQUE_SYSTEM — худший: 3 из 4 множителей неверны
- CHARGER_SYSTEM — свои собственные множители (1.5/2.0/3.0), не совпадающие ни с кем
- EQUIPMENT_SYSTEM — раздельные множители для эффективности и прочности
- Базовый грейд: "Normal" (ARCHITECTURE) vs "Common" (все остальные)

**Предлагаемое решение:** Утвердить DATA_MODELS.md как источник истины для грейдов:
- **5 грейдов**: Damaged, Common, Refined, Perfect, Transcendent
- **Множители для техник** (из ALGORITHMS): 0.5, 1.0, 1.3, 1.6, 2.0
- **Множители для экипировки**: раздельные для Efficiency и Durability (из EQUIPMENT_SYSTEM)
- ARCHITECTURE.md обновить: убрать Fine/Excellent/Masterwork/Legendary

### S-02 — Система слотов экипировки: 5 несовместимых моделей

| Источник | Модель |
|----------|--------|
| **ARCHITECTURE.md** | body-zone dual: head_armor/clothing, torso_armor/clothing, legs_armor/clothing, feet_armor/clothing, hands_armor/clothing + weapon_main/off/twohanded + charger + amulet + ring_left_1/2 + ring_right_1/2 |
| **DATA_MODELS.md** | simple: head, torso, left_hand, right_hand (всего 4!) |
| **INVENTORY_SYSTEM.md** §диаграмма | head, torso, left_hand, right_hand, legs, feet, accessory1, accessory2, backpack (9) |
| **INVENTORY_SYSTEM.md** §слоты | совпадает с ARCHITECTURE (dual armor/clothing + weapons + charger + amulet + 4 rings) |
| **Код (EquipmentSlot enum)** | WeaponMain, WeaponOff, Armor, Clothing, Charger, RingLeft, RingRight, Accessory, Backpack (9) |

**Проблемы:**
- DATA_MODELS — всего 4 слота (нет ног, ступней, аксессуаров, оружия)
- Код объединяет все body-zone armor в один слот "Armor" и все clothing в "Clothing"
- ARCHITECTURE планирует 4 кольца, код реализовал 2
- INVENTORY_SYSTEM противоречит самому себе

**Предлагаемое решение:** Код — источник истины для реализации. Обновить ARCHITECTURE и DATA_MODELS:
- 9 слотов: WeaponMain, WeaponOff, Armor, Clothing, Charger, RingLeft, RingRight, Accessory, Backpack
- Armor/Clothing — без деления по зонам тела (визуальное покрытие — отдельная система)
- 2 кольца (не 4)

### S-03 — Poison как элемент vs как тип техники

| Источник | Poison — это... |
|----------|----------------|
| ARCHITECTURE.md | Не элемент. Fire→Poison ×1.2 (односторонний модификатор) |
| ALGORITHMS.md | 7 элементов (без Poison) |
| ELEMENTS_SYSTEM.md | 8-й элемент со своими эффектами, спрайтами, трансцендентными способностями |
| TECHNIQUE_SYSTEM.md | Перечисляет "Яд (poison)" как элемент |
| SPRITE_INDEX.md | 8 спрайтов стихий (включая element_poison.png) |
| DATA_MODELS.md | Technique.type: combat, cultivation, support, ..., **poison** (ТИП техники) |

**Предлагаемое решение:** Poison = **тип техники**, не стихия. Оставить 7 элементов. Убрать Poison из ELEMENTS_SYSTEM и SPRITE_INDEX, или явно документировать как "специальную модификацию", не входящую в систему противоположностей.

---

## 🔴 CRITICAL (20 противоречий)

### Боевые системы

| # | Файл | Противоречие | Авторитетный источник |
|---|------|-------------|----------------------|
| C-01 | COMBAT_SYSTEM.md | 3 взаимоисключающие формулы урона в одном документе | ALGORITHMS: `finalDamage = capacity × gradeMult × ultimateMult` |
| C-02 | COMBAT_SYSTEM.md | Две модели нанесения урона: 0.7/0.3 одновременный сплит vs последовательное переполнение | ALGORITHMS: одновременный сплит 0.7/0.3 |
| C-03 | TECHNIQUE_SYSTEM.md | Множители грейдов: 1.0/1.2/1.4/1.6 вместо 1.0/1.3/1.6/2.0 | ALGORITHMS: 1.0/1.3/1.6/2.0 |
| C-04 | BODY_SYSTEM.md | Материалы тела: снижение урона (%) интерпретировано как бонус HP (+%) | ARCHITECTURE: (твёрдость, снижение_урона %) |
| C-05 | BUFF_SYSTEM.md ↔ MODIFIERS_SYSTEM.md | qi_regen: "НЕ модифицируется баффами" vs имеет полный фреймворк модификаторов | — |

### Системы мира

| # | Файл | Противоречие | Авторитетный источник |
|---|------|-------------|----------------------|
| C-06 | TIME_SYSTEM.md | Определение тика инвертировано (1 тик = 1 сек реального vs 1 мин игрового) | ARCHITECTURE: 1 тик = 1 минута игрового |
| C-07 | TIME_SYSTEM.md ↔ WORLD_SYSTEM.md | Э.С.М. = "Эра Свободного Мира" vs "Эра Сердца Мира" | — |
| C-08 | ENTITY_TYPES.md | Добавлен SoulType `artifact`, отсутствующий в ARCHITECTURE и DATA_MODELS | DATA_MODELS: 4 типа (character/creature/spirit/construct) |

### Системы предметов

| # | Файл | Противоречие | Авторитетный источник |
|---|------|-------------|----------------------|
| C-09 | INVENTORY_SYSTEM.md | 5 грейдов вместо 7 | ARCHITECTURE: 7 грейдов (см. S-01) |
| C-10 | EQUIPMENT_SYSTEM.md | Множители грейдов не совпадают ни с одним источником | ARCHITECTURE/ALGORITHMS: другие значения |
| C-11 | EQUIPMENT_SYSTEM.md | 5 грейдов вместо 7; Damaged=0.5 вместо 0.7 | ARCHITECTURE: 7 грейдов |
| C-12 | CHARGER_SYSTEM.md | 4 грейда с уникальными множителями (1.0/1.5/2.0/3.0) | ALGORITHMS: 1.0/1.3/1.6/2.0 |
| C-13 | FORMATION_SYSTEM.md | FormationCoreType: 5 типов (Disk/Altar/Array/Totem/Seal) | DATA_MODELS: 2 типа (disk/altar) |
| C-14 | GENERATORS_SYSTEM.md | 3-й слой "Матрёшки" = Bonuses (случайные эффекты) | ARCHITECTURE: 3-й слой = Specialization (тип предмета) |
| C-15 | GENERATORS_SYSTEM.md | Статус "РЕАЛИЗОВАНО" но без спецификации грейдов | — |
| C-16 | GENERATORS_NAME_FIX.md | EquipmentGrade enum: 4 значения, отсутствует Common | DATA_MODELS: 5 значений |

### Прочие системы

| # | Файл | Противоречие | Авторитетный источник |
|---|------|-------------|----------------------|
| C-17 | ELEMENTS_SYSTEM.md | 8 элементов (Poison как элемент) | ALGORITHMS: 7 элементов |
| C-18 | NPC_AI_SYSTEM.md | Disposition → Attitude (Fix-07), но DATA_MODELS не обновлён | DATA_MODELS: disposition(-100..100) |
| C-19 | SAVE_SYSTEM.md | Тоже Attitude вместо Disposition | DATA_MODELS: disposition |
| C-20 | FACTION_SYSTEM ↔ NPC_AI_SYSTEM | Пороги Disposition: "Нейтральный" = 20-49 vs -9..9 | — |

---

## 🟡 MAJOR (30 противоречий)

### Боевые системы

| # | Файл | Противоречие |
|---|------|-------------|
| M-01 | COMBAT_SYSTEM.md | Пайплайн урона: отсутствует слой Level Suppression; добавлен лишний слой "Итоговый урон" |
| M-02 | COMBAT_SYSTEM.md | Диапазон gradeMult = ×1.0~×1.6 (отсутствует transcendent ×2.0) |
| M-03 | COMBAT_SYSTEM.md | Скорость атаки от AGI, но ALGORITHMS: cast time от conductivity + cultivation/mastery |
| M-04 | COMBAT_SYSTEM.md | Сводная таблица Qi Buffer: только Qi-атаки, физический урон не показан |
| M-05 | TECHNIQUE_SYSTEM.md | Формула урона без ultimateMultiplier |
| M-06 | TECHNIQUE_SYSTEM.md | Poison как элемент (см. S-03) |
| M-07 | QI_SYSTEM.md | coreCapacity без qualityMultiplier (внутреннее противоречие: строка 110 vs 315) |
| M-08 | BODY_SYSTEM.md | Отсутствуют Ethereal и Chaos материалы |
| M-09 | BODY_SYSTEM.md | SoulType "Artifact" не в DATA_MODELS (см. C-08) |
| M-10 | BODY_SYSTEM.md | Morphology "Amorphous" и "Hybrid_*" не в DATA_MODELS |
| M-11 | BODY_SYSTEM.md | Последовательная модель HP vs одновременный сплит 0.7/0.3 (см. C-02) |
| M-12 | BUFF_SYSTEM.md | coreCapacity без qualityMultiplier |
| M-13 | MODIFIERS_SYSTEM.md | Пример "+5 к ловкости" нарушает правило "agility — необратимая" |
| M-14 | MODIFIERS_SYSTEM.md | Отсутствует elemental_void в переменных модификаторов |

### Системы мира

| # | Файл | Противоречие |
|---|------|-------------|
| M-15 | ENTITY_TYPES.md | 4 гибридных морфологии не в DATA_MODELS |
| M-16 | WORLD_SYSTEM.md | Типы локаций (region/area/building/room) vs ARCHITECTURE (Town/Village/Wilderness/Dungeon/Sect) |
| M-17 | ENTITY_TYPES.md | PhysicalObject: 5 категорий (resource/structure/container/decoration/item) vs DATA_MODELS: 4 (resource/container/interactable/decoration) |
| M-18 | TRANSITION_SYSTEM.md | 1s:1min скорость, но TIME_SYSTEM "normal" = 1s:5min |
| M-19 | TIME_SYSTEM.md | Внутреннее: шаг = 0.1 game-min vs 1 tick (=5 game-min) за клетку |
| M-20 | TIME_SYSTEM.md | Внутреннее: атака = 1 game-min vs 1 tick (=5 game-min) |
| M-21 | LOCATION_MAP_SYSTEM.md | Типы зданий (7 категорий) vs DATA_MODELS: 6 конкретных типов |

### Системы предметов

| # | Файл | Противоречие |
|---|------|-------------|
| M-22 | INVENTORY_SYSTEM.md | Две системы слотов в одном документе (внутреннее) |
| M-23 | INVENTORY_SYSTEM.md | 11 типов предметов vs ARCHITECTURE: 5 классов |
| M-24 | INVENTORY_SYSTEM.md | Item model: sizeHeight(1-2) vs DATA_MODELS: (1-3); отсутствуют поля Equipment V2 |
| M-25 | EQUIPMENT_SYSTEM.md | Категории материалов: 6 (metal/leather/cloth/crystal/spirit/void) vs DATA_MODELS: 5 (metal/organic/mineral/wood/crystal) |
| M-26 | EQUIPMENT_SYSTEM.md | 6 состояний прочности vs DATA_MODELS: 5 |
| M-27 | EQUIPMENT_SYSTEM.md | armor_arms и armor_full без слотов в ARCHITECTURE |
| M-28 | FORMATION_SYSTEM.md | FormationCoreVariant: 7 vs DATA_MODELS: 5 |
| M-29 | FORMATION_SYSTEM.md | FormationType: 8 vs DATA_MODELS: 4 |
| M-30 | CHARGER_SYSTEM.md | Внутреннее: "только on/off" vs "burst/normal/trickle/off" |

### Прочие системы

| # | Файл | Противоречие |
|---|------|-------------|
| M-31 | CONFIGURATIONS.md | Уровень культивации 10 (не существует в DATA_MODELS: 1-9) |
| M-32 | CONFIGURATIONS.md | qiCost: int вместо long |
| M-33 | STAT_THRESHOLD_SYSTEM.md | Линейная формула порога vs экспоненциальный диминишинг (ALGORITHMS) |
| M-34 | STAT_THRESHOLD_SYSTEM.md | Виртуальная дельта: 0.0005 вместо 0.001 (ARCHITECTURE) |
| M-35 | STAT_THRESHOLD_SYSTEM.md | Нет MAX_STAT_VALUE=1000; заявлен бесконечный рост |
| M-36 | LORE_SYSTEM.md | Стат "Энергия" вместо "Живучесть (VIT)" |
| M-37 | NPC_AI_SYSTEM.md | PersonalityTrait [Flags] vs DATA_MODELS: personality(JSON) |
| M-38 | FACTION_SYSTEM.md | Нет nationId в модели фракции |
| M-39 | FACTION_SYSTEM.md | Нет сущности FactionRelation |
| M-40 | SPRITE_INDEX.md | 8 спрайтов стихий (с Poison); 10 уровней культивации (с L10) |

---

## 🟢 MINOR (33 противоречия)

Основные категории мелких противоречий:

| Категория | Кол-во | Примеры |
|-----------|--------|---------|
| Именование грейдов (Common vs Normal) | 6 | INVENTORY, EQUIPMENT, CHARGER, QI_SYSTEM |
| Стилистика (combined vs per-part hit chances) | 3 | COMBAT_SYSTEM |
| Недостающие поля без критичного влияния | 8 | TECHNIQUE (defense scaling), MODIFIERS (perfect ×1.7) |
| Небольшие численные расхождения | 7 | WORLD_SYSTEM (Qi ranges), SPRITE_INDEX (counts) |
| Терминология (coreVolume vs coreCapacity) | 5 | CONFIGURATIONS, QI_SYSTEM |
| Прочие | 4 | SAVE_SYSTEM (extra trigger), FACTION (Disposition naming) |

---

## 📊 Карта противоречий по файлам

| Файл | 🔴 C | 🟡 M | 🟢 m | Итого |
|------|------|------|------|-------|
| COMBAT_SYSTEM.md | 2 | 4 | 1 | 7 |
| TECHNIQUE_SYSTEM.md | 1 | 2 | 2 | 5 |
| QI_SYSTEM.md | 0 | 1 | 1 | 2 |
| BODY_SYSTEM.md | 1 | 4 | 0 | 5 |
| BUFF_SYSTEM.md | 1 | 1 | 0 | 2 |
| MODIFIERS_SYSTEM.md | 0 | 2 | 1 | 3 |
| TIME_SYSTEM.md | 2 | 2 | 0 | 4 |
| ENTITY_TYPES.md | 1 | 2 | 0 | 3 |
| WORLD_SYSTEM.md | 0 | 1 | 2 | 3 |
| TRANSITION_SYSTEM.md | 0 | 1 | 0 | 1 |
| LOCATION_MAP_SYSTEM.md | 0 | 1 | 0 | 1 |
| INVENTORY_SYSTEM.md | 1 | 3 | 1 | 5 |
| EQUIPMENT_SYSTEM.md | 2 | 3 | 2 | 7 |
| CHARGER_SYSTEM.md | 1 | 1 | 0 | 2 |
| FORMATION_SYSTEM.md | 1 | 3 | 1 | 5 |
| GENERATORS_SYSTEM.md | 2 | 0 | 1 | 3 |
| GENERATORS_NAME_FIX.md | 1 | 0 | 0 | 1 |
| CONFIGURATIONS.md | 0 | 2 | 1 | 3 |
| ELEMENTS_SYSTEM.md | 1 | 0 | 0 | 1 |
| STAT_THRESHOLD_SYSTEM.md | 0 | 3 | 0 | 3 |
| NPC_AI_SYSTEM.md | 1 | 1 | 0 | 2 |
| SAVE_SYSTEM.md | 1 | 0 | 1 | 2 |
| FACTION_SYSTEM.md | 1 | 2 | 1 | 4 |
| LORE_SYSTEM.md | 0 | 1 | 0 | 1 |
| SPRITE_INDEX.md | 0 | 2 | 1 | 3 |
| **Итого** | **20** | **40** | **15** | **75+** |

---

## 🎯 Приоритеты исправления

### Приоритет 1 — Системные конфликты (блокируют всё остальное)

1. **S-01: Утвердить систему грейдов** — обновить ARCHITECTURE.md до 5 грейдов (Damaged/Common/Refined/Perfect/Transcendent), удалить Fine/Excellent/Masterwork/Legendary; зафиксировать множители для техник и экипировки
2. **S-02: Утвердить систему слотов** — обновить ARCHITECTURE и DATA_MODELS до 9-слотовой модели из кода
3. **S-03: Решить статус Poison** — убрать из элементов или документировать как исключение
4. **A-01/A-02: Исправить авторитетные источники** — Qi→long в DATA_MODELS; добавить amorphous в Morphology

### Приоритет 2 — Критические противоречия в боевых системах

5. C-01: Унифицировать формулу урона в COMBAT_SYSTEM
6. C-02: Утвердить модель HP (сплит 0.7/0.3 vs переполнение)
7. C-03: Исправить множители грейдов в TECHNIQUE_SYSTEM
8. C-04: Исправить семантику материалов тела (снижение урона, не бонус HP)
9. C-05: Решить qi_regen: модифицируемый или нет

### Приоритет 3 — Критические противоречия в мире/предметах

10. C-06: Утвердить определение тика (игровое время)
11. C-07: Утвердить название Э.С.М.
12. C-08/C-09: Решить artifact SoulType
13. C-10–C-16: Привести грейды в EQUIPMENT/CHARGER/FORMATION к утверждённой системе
14. C-17–C-20: Исправить Disposition→Attitude / Poison как элемент / пороги фракций

### Приоритет 4 — MAJOR противоречия

Исправить 30 MAJOR-противоречий после разрешения системных конфликтов.

---

## 📝 Примечание о методологии

- **Оценка токенов:** Все файлы прочитаны полностью, противоречия сверены посекционно
- **Авторитетные источники** (ARCHITECTURE/DATA_MODELS/ALGORITHMS) сами содержат 2 конфликта (A-01, A-02), которые нужно разрешить первыми
- **Код** — де-факто источник истины для слотов и типов Qi; документация должна быть приведена к коду, а не наоборот
- **Версионность:** Многие противоречия возникли из-за эволюции дизайна (Fix-01, Fix-07 и т.д.) без обратного обновления всех документов

---

*Отчёт создан: 2026-04-23*
*Метод: кросс-документный аудит 28 файлов против 3 авторитетных источников*

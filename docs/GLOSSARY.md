# 📖 Глоссарий терминологии

**Создано:** 2026-04-27
**Обновлено:** 2026-04-27 (расширен: +60 терминов из аудита СТ-2)
**Статус:** 📋 Черновик для доработки

---

## ⚠️ Важно

> Этот документ — **единый справочник терминологии** проекта.
> При обнаружении расхождений в терминах между документами — считать верным термин из этого глоссария.

---

## 🌊 Система Ци

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `coreCapacity` | Ёмкость ядра | Максимальное количество Ци в ядре практика. Формула: `1000 × 1.1^totalSubLevels × qualityMultiplier` | ARCHITECTURE.md, DATA_MODELS.md |
| ~~coreVolume~~ | ~~Объём ядра~~ | ❌ Устаревший термин. Использовать `coreCapacity` | — |
| `currentQi` | Текущее Ци | Текущее количество Ци в ядре (тип: `long`) | DATA_MODELS.md |
| `qiDensity` | Плотность Ци | Качество Ци = `2^(cultivationLevel - 1)`. Растёт ×2 за уровень | ALGORITHMS.md §3.3 |
| `conductivity` | Проводимость меридиан | Скорость прохождения Ци через меридианы. Формула: `coreCapacity / 360` | QI_SYSTEM.md, ALGORITHMS.md |
| `qiRegen` (базовая) | Базовая регенерация микроядра | 10% ёмкости ядра/сутки — **немодифицируема** баффами | BUFF_MODIFIERS_SYSTEM.md, ALGORITHMS.md §2.3 |
| `qiRestoration` | Восстановление Ци | Общая скорость восстановления Ци (медитация, экипировка, формации) — **модифицируема** | BUFF_MODIFIERS_SYSTEM.md |
| ~~qi_regen_buff~~ | ~~Бафф регенерации Ци~~ | ❌ Устаревшее название эффекта. Использовать `qi_restoration_buff` | — |
| `qi_restoration_buff` | Бафф восстановления Ци | Увеличивает скорость восстановления Ци (медитация, поглощение), но ограничена проводимостью | ELEMENTS_SYSTEM.md |
| `cultivationLevel` | Уровень культивации | Основной уровень 1-9 (10 = Вознесение, конец игры) | ARCHITECTURE.md, DATA_MODELS.md, LORE_SYSTEM.md |
| `cultivationSubLevel` | Подуровень культивации | Под-уровень 0-9. totalSubLevels = level×10 + subLevel | DATA_MODELS.md |
| `coreQuality` / `qualityMultiplier` | Качество ядра | Множитель ёмкости ядра в формуле coreCapacity. Варьируется от рождения | DATA_MODELS.md, ARCHITECTURE.md |
| `effectiveQi` | Эффективное Ци | coreCapacity × qiDensity. Реальная боевая мощь практика | ARCHITECTURE.md §«Вместимость ядра» |
| `DormantCore` | Дремлющее ядро | Зачаток духовного центра у каждого смертного. Формируется 16-30 лет. ≥80% = возможно пробуждение | MORTAL_DEVELOPMENT.md §«Дремлющее ядро» |
| `Awakening` | Пробуждение | Переход Смертный (L0) → Практик (L1). Типы: естественное, направленное, артефактное, насильственное | MORTAL_DEVELOPMENT.md §«Пробуждение ядра» |
| `innateElement` | Врождённый элемент | Один из 8 элементов, к которому практик имеет предрасположение. Ограничивает доступные техники | ELEMENTS_SYSTEM.md, DATA_MODELS.md |

---

## ⚔️ Боевая система

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `qiBuffer` | Буфер Ци | Естественная защита Ци от **ЛЮБОГО урона** (техники Ци и физический) | ALGORITHMS.md §2, COMBAT_SYSTEM.md |
| `levelSuppression` | Подавление уровнем | Множитель, снижающий урон при разнице уровней | ALGORITHMS.md §1 |
| `damageReduction` | Снижение урона | Процентное снижение урона бронёй или материалом. Кап 80% | ALGORITHMS.md §5 |
| `materialReduction` | Снижение от материала | Процентное снижение физ. урона от материала тела | ENTITY_TYPES.md §5 |
| `effectiveBonus` | Эффективный бонус | Бонус после применения мягкого капа | ALGORITHMS.md §6 |
| `decayRate` | Скорость затухания | Параметр мягкого капа, индивидуальный для каждой переменной | ALGORITHMS.md §6.1 |
| `ultimateMultiplier` | Множитель ульты | ×2.0 для Ultimate-техник (5% шанс у Transcendent). Стоимость Ци также ×2.0 | TECHNIQUE_SYSTEM.md §«Ultimate», ALGORITHMS.md §4.2 |
| `mastery` | Мастерство техники | 0-100%. Бонус ёмкости: +0%→+50%. Формула прироста: max(0.1, baseGain × (1 - current/100)) | TECHNIQUE_SYSTEM.md §«Система мастерства» |
| `SpinalAI` | Спинальный AI | Быстрые автоматические реакции NPC (уклонение, щит, бегство). 1-10мс. Приоритеты 30-100 | NPC_AI_SYSTEM.md §«Spinal AI» |
| `NPCCategory` | Категория NPC | Enum: Temp (упрощённое AI), Plot (полное AI), Unique (полное + история) | NPC_AI_SYSTEM.md §«Категории NPC» |

---

## 🦴 Система тела

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `redHP` | Функциональная HP (красная) | Работоспособность части тела. При 0 — паралич | BODY_SYSTEM.md, ALGORITHMS.md §9 |
| `blackHP` | Структурная HP (чёрная) | Целостность части тела = функциональная HP × 2. При 0 — отрубание | BODY_SYSTEM.md |
| `bodyMaterial` | Материал тела | Определяет твёрдость и снижение физ. урона. 6 типов (без construct) | ENTITY_TYPES.md §5 |
| `SoulType` | Тип души | Первичная классификация: character, creature, spirit, artifact, construct | ENTITY_TYPES.md §2 |
| `Morphology` | Морфология | Вторичная классификация: humanoid, quadruped, bird, serpentine, arthropod, amorphous, hybrid_* | ENTITY_TYPES.md §3 |
| `Species` | Вид | Конкретный вид (human, elf, wolf, dragon...) | ENTITY_TYPES.md §4 |

---

## 🎒 Инвентарь и экипировка

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `EquipmentSlot` | Слот экипировки | 15 слотов: 7 активных + 8 заглушек (🔒) | ARCHITECTURE.md, INVENTORY_SYSTEM.md |
| `Grade` (экипировка) | Грейд экипировки | 5 уровней: Damaged, Common, Refined, Perfect, Transcendent | EQUIPMENT_SYSTEM.md §2 |
| `Grade` (техника) | Грейд техники | 4 уровня: Common, Refined, Perfect, Transcendent (без Damaged) | TECHNIQUE_SYSTEM.md |
| `backpack` | Рюкзак | Переменная ёмкость инвентаря (зависит от рюкзака, не фиксирована 49) | INVENTORY_SYSTEM.md |
| `rarity` | Редкость (предмета) | Enum: common, uncommon, rare, epic, legendary, mythic | DATA_MODELS.md §6, INVENTORY_SYSTEM.md |
| `durability` | Прочность | Текущая/макс. прочность экипировки. Состояния: pristine, good, worn, damaged, broken | DATA_MODELS.md §6, EQUIPMENT_SYSTEM.md |

---

## ⚡ Техники

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `TechniqueType` | Тип техники | Enum: combat, defense, support, healing, cultivation, curse, poison, movement, sensory | TECHNIQUE_SYSTEM.md §«Типы техник» |
| `TechniqueSubtype` | Подтип техники | Уточнение типа: melee_strike, melee_weapon, ranged_projectile, ranged_beam, ranged_aoe, shield, block, dodge, reflect, dash, teleport, flight... | TECHNIQUE_SYSTEM.md, CONFIGURATIONS.md |
| `baseCapacity` | Базовая ёмкость техники | Зависит от типа: Formation=80, Defense=72, Combat=64/48/32, Support/Healing=56, Movement=40... | TECHNIQUE_SYSTEM.md §«Структурная ёмкость» |
| `capacity` (техника) | Ёмкость техники | Макс. базовое Ци, которое техника обрабатывает: baseCapacity × 2^(level-1) × (1 + mastery/100 × 0.5) | TECHNIQUE_SYSTEM.md §«Структурная ёмкость» |
| `Matryoshka` | Матрёшка | Архитектура генерации: 3 слоя (База×Грейд×Специализация). Применяется к экипировке, техникам, расходникам | ARCHITECTURE.md §«Принцип Матрёшка» |

---

## 🌍 Мир (World Structure)

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `Chunk` | Чанк | Единица сохранения мира, 100×100 км. Один файл сохранения. Содержит 100 секторов (10×10) | WORLD_MAP_SYSTEM.md §0.1, WORLD_SAVE_SYSTEM.md §2 |
| `Sector` | Сектор | Единица карты мира, 10×10 км. Ячейка на глобальной карте для навигации. Содержит 1-10 локаций | WORLD_MAP_SYSTEM.md §0.1, §2.1 |
| `Tile` | Тайл | Единица навигации, **2×2 м** — единый стандарт проекта. Содержит данные о проходимости, объектах, субъектах | WORLD_MAP_SYSTEM.md §0.2, TILE_SYSTEM.md |
| `Location` | Локация | Единица загрузки, переменный размер (100 м — 10 км). Unity сцена. Содержит до 25M тайлов | WORLD_MAP_SYSTEM.md §0.1, LOCATION_MAP_SYSTEM.md |
| `Region` | Регион | Группа связанных секторов с общими характеристиками (Wilderness, Civilized, Sacred, Cursed, Contested, Restricted) | WORLD_MAP_SYSTEM.md §2.2 |
| `TerrainType` | Тип местности | Enum: plains, mountains, forest, jungle, sea, desert, swamp, tundra, volcanic, holy, cursed | WORLD_SYSTEM.md, WORLD_MAP_SYSTEM.md §3 |
| `Climate` | Климатическая зона | Enum: Tundra, Temperate, Desert, Jungle, Mountain, Volcanic, Swamp, Holy, Cursed | WORLD_MAP_SYSTEM.md §2.3 |
| `FogOfWar` | Фог войны | Система видимости: Hidden → Explored → Visible → Current. Радиус базовый 1 сектор | WORLD_MAP_SYSTEM.md §2.4 |
| `dangerLevel` | Уровень опасности | Число 1-9, определяет уровень врагов и риски в секторе | WORLD_MAP_SYSTEM.md §2.1 |
| `Transition` | Переход | Смена уровня детализации: Мировая карта ↔ Локация ↔ Здание. Включает сохранение позиции и загрузку сцены | TRANSITION_SYSTEM.md §1 |

---

## ⏰ Время

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `tick` | Тик | 1 тик = 1 минута игрового времени. Фундаментальная единица времени | TIME_SYSTEM.md, ARCHITECTURE.md §«Система времени» |
| `Season` | Сезон | Enum: `warm \| cold`. Тёплый = месяцы 1-9, Холодный = месяцы 10-12 | TIME_SYSTEM.md §4, LORE_SYSTEM.md |
| `TimeOfDay` | Время суток | Enum: night, dawn, morning, day, evening, dusk. Определяет освещение и NPC-расписания | TIME_SYSTEM.md §4 |
| `TimeSpeed` | Скорость времени | Enum: pause(0), normal(1 тик/сек), fast(5), quick(15). Синхронизировано с ARCHITECTURE.md | TIME_SYSTEM.md §2 |
| `WorldTime` | Игровое время | Структура: totalMinutes, year, month, day, hour, minute, season. Летосчисление Э.С.М. | TIME_SYSTEM.md §4 |
| `ESM` | Э.С.М. | Эра Сердца Мира — летосчисление мира. Текущий год: 1864 | LORE_SYSTEM.md, TIME_SYSTEM.md, MORTAL_DEVELOPMENT.md |

---

## 🏛️ Фракции

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `Nation` | Государство | Территория с границами и законами. Типы: monarchy, republic, theocracy, federation, warlord | FACTION_SYSTEM.md §«Государство» |
| `Faction` | Фракция | Альянс сект с общей идеологией. Идеологии: righteous, demonic, neutral, pragmatic, isolationist | FACTION_SYSTEM.md §«Фракция» |
| `Sect` | Секта | Организация культиваторов. Типы: orthodox, unorthodox, demonic, neutral, scholarly, martial. Статусы: official, underground, exiled, nomadic, independent | FACTION_SYSTEM.md §«Секта» |
| `FactionRelation` | Отношения фракций | Структура: sourceId, targetId, relationType(ally/enemy/neutral/vassal), strength(-100..+100) | FACTION_SYSTEM.md §«Отношения» |
| `Attitude` | Отношение (NPC) | Числовое значение -100..+100 отношения NPC к ГГ. Формула включает personalAttitude, sectRelation, factionRelation, nationRelation | FACTION_SYSTEM.md §«Расчёт отношений», NPC_AI_SYSTEM.md |
| `PersonalityTrait` | Черты характера | [Flags] enum: Aggressive, Cautious, Treacherous, Ambitious, Loyal, Pacifist, Curious, Vengeful | NPC_AI_SYSTEM.md, SAVE_SYSTEM.md |

---

## ⭐ Перки

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `Perk` | Перк | Постоянная пассивная способность. Отличается от баффа (временный) и навыка (развивается) | PERK_SYSTEM.md §1 |
| `PerkCategory` | Категория перка | Enum: Innate (врождённый, вне слотов), Acquired (приобретённый, основные слоты), Cursed (проклятый, отд. слоты до 3) | PERK_SYSTEM.md §3, §7.1 |
| `conductivityBonus` | Бонус проводимости (перк) | Увеличивает проводимость меридиан от перков: finalConductivity = base × (1 + conductivityBonus) | PERK_SYSTEM.md §4.2 |

---

## 🔄 Баффы / Формации / Зарядники

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `BuffType` | Тип баффа | Enum категорий: AttackBoost, DefenseBoost, SpeedBoost, ConductivityBoost, Shield, Poison, Burn, Stun, Slow, Silence... | BUFF_MODIFIERS_SYSTEM.md ЧАСТЬ A |
| `BuffApplication` | Применение баффа | Enum: Instant, Duration, Permanent, Stacking, Refreshing | BUFF_MODIFIERS_SYSTEM.md ЧАСТЬ A |
| `ConductivityModifier` | Модификатор проводимости | Особая система: бонус X сек → откат X×3 сек, штраф bonusValue×0.5. Меридианы «растягиваются», затем «сжимаются» | BUFF_MODIFIERS_SYSTEM.md ЧАСТЬ A §«Проводимость» |
| `FormationCore` | Ядро формации | Физический носитель: Disk (переносной, L1-L6) или Altar (стационарный, L5-L9). Содержит контур формации | FORMATION_SYSTEM.md §«Физические носители» |
| `FormationType` | Тип формации | Enum: Barrier, Trap, Amplification, Suppression, Gathering, Detection, Teleportation, Summoning | FORMATION_SYSTEM.md §«Типы формаций» |
| `FormationSize` | Размер формации | Enum: Small(3×3м), Medium(10×10м), Large(30×30м), Great(100×100м), Heavy(300×300м, L6+) | FORMATION_SYSTEM.md §«Размеры формаций» |
| `contourQi` | Стоимость контура | Ци на прорисовку контура формации: 80 × 2^(level-1). Тратится создателем | FORMATION_SYSTEM.md §«Формулы» |
| `Charger` | Зарядник | Экипировка для хранения камней Ци и контролируемого поглощения. Слот `charger`. Форм-факторы: belt/bracelet/necklace/ring/backpack | CHARGER_SYSTEM.md §1 |
| `ChargerBuffer` | Буфер зарядника | Мгновенный запас Ци: 50-2000. Пополнение через проводимость 5-50 ед/сек. Перегрев 100% → блок 30 сек | CHARGER_SYSTEM.md §2.2, §4 |

---

## 📊 Развитие

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `virtualDelta` | Виртуальная дельта | Накопленный прогресс развития, не закреплён в реальной характеристике. Кап: STR/AGI/VIT=10, INT=15 | STAT_THRESHOLD_SYSTEM.md §«Виртуальная дельта» |
| `threshold` | Порог развития | Опыт для +1 к характеристике: floor(currentStat / 10). Чем выше стат, тем больше усилий | STAT_THRESHOLD_SYSTEM.md §«Формула порога» |
| `MAX_STAT_VALUE` | Макс. характеристика | Константа 1000. Жёсткий кап развития. GameConstants.MAX_STAT_VALUE | ARCHITECTURE.md, STAT_THRESHOLD_SYSTEM.md §«Баланс» |
| `consolidation` | Закрепление | Конвертация виртуальной дельты в реальную характеристику при сне. Мин. 4 часа, макс +0.20 за 8 часов | STAT_THRESHOLD_SYSTEM.md §«Закрепление при сне» |
| `StatDevelopment` | Развитие характеристик | Структура данных: реальные статы (STR/AGI/INT/VIT) + виртуальные дельты + методы AddDelta/ConsolidateSleep | STAT_THRESHOLD_SYSTEM.md §«Реализация в коде» |

---

## 📦 Журнал / Прочее

| Термин (код) | Русский | Описание | Источник истины |
|-------------|---------|----------|-----------------|
| `JournalEntry` | Запись журнала | Структура: id, title, category, rarity, isDiscovered, completionLevel(0-100%), unlockedFacts | JOURNAL_SYSTEM.md §«JournalEntry» |
| `JournalCategory` | Категория журнала | Enum: Characters, Locations, Techniques, Creatures, Items, Lore, Factions, Notes | JOURNAL_SYSTEM.md §«Категории журнала» |
| `EntryRarity` | Редкость записи | Enum: Common, Uncommon, Rare, Epic, Legendary | JOURNAL_SYSTEM.md §«JournalCategory» |

---

## 🔄 Источники истины (иерархия)

| Приоритет | Документ | Область |
|-----------|----------|---------|
| 1 | ALGORITHMS.md | Формулы, расчёты, таблицы подавления, мягкие капы |
| 2 | ENTITY_TYPES.md | Типы сущностей, морфологии, материалы тела |
| 3 | EQUIPMENT_SYSTEM.md | Грейды, прочность, слоты экипировки |
| 4 | ELEMENTS_SYSTEM.md | Стихии, взаимодействия, ограничения |
| 5 | BUFF_MODIFIERS_SYSTEM.md | Баффы/дебаффы, модификаторы, ограничения |
| 6 | CHARGER_SYSTEM.md | Параметры зарядников |
| 7 | ARCHITECTURE.md | Общая архитектура (ссылки на 1-6) |
| 8 | Остальные документы | Конкретные системы (ссылаются на 1-7) |

**Специализированные источники истины:**

| Область | Документ | Примечание |
|---------|----------|------------|
| Размерность мира | WORLD_MAP_SYSTEM.md | 🔧 В разработке |
| Календарь/время | TIME_SYSTEM.md | |
| Пороги статов | STAT_THRESHOLD_SYSTEM.md | |
| Пороги культивации | TAT_THRESHOLD_SYSTEM | |
| Грейды техник | TECHNIQUE_SYSTEM.md | |
| Лор | LORE_SYSTEM.md | Подчиняется TIME_SYSTEM для календарных фактов |

> **Принцип:** Документ ниже по иерархии НЕ может противоречить документу выше.

---

*Документ создан: 2026-04-27*
*Обновлено: 2026-04-27 — Добавлено 60 терминов (аудит СТ-2), включена WORLD_MAP_SYSTEM.md (СТ-3), обновлены ссылки BUFF_SYSTEM→BUFF_MODIFIERS_SYSTEM (СТ-1)*
*Статус: Черновик для доработки*

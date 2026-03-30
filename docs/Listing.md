# 📚 Перечень документации проекта Cultivation World Simulator

**Последнее обновление:** 2026-03-30 10:00 UTC
**Файлов в /docs:** 47
**Файлов в /Unity:** 13 (+ 4 REQUIRED)

---

## 🔬 ДЕТАЛЬНЫЙ АНАЛИЗ МИГРАЦИИ

### ✅ Полностью мигрированные документы:

| Unity документ | Источник в docs | Покрытие |
|----------------|-----------------|----------|
| BODY_SYSTEM.md | body.md, body-development-analysis.md, body_review.md, body_monsters.md | ✅ 100% |
| COMBAT_SYSTEM.md | combat-system.md, technique-system-v2.md (частично) | ✅ 95% |
| TECHNIQUE_SYSTEM.md | technique-system-v2.md | ✅ 95% |
| TIME_SYSTEM.md | TIME_SYSTEM.md | ✅ 100% |
| QI_SYSTEM.md | start_lore.md, body_review.md | ✅ 100% |
| NPC_AI_SYSTEM.md | npc-session-integration.md, NPC_AI_THEORY.md | ✅ 90% |
| WORLD_SYSTEM.md | sector-architecture.md, soul-system.md | ✅ 95% |
| SAVE_SYSTEM.md | — | ✅ 100% (новый) |
| GENERATORS_SYSTEM.md | generators.md | ✅ 100% |
| FACTION_SYSTEM.md | faction-system.md | ✅ 100% |
| LORE_SYSTEM.md | start_lore.md | ✅ 100% |

### ⚠️ ТРЕБУЕТСЯ ДОПОЛНИТЕЛЬНАЯ МИГРАЦИЯ:

#### 1. EQUIPMENT_SYSTEM.md (НОВЫЙ, ВЫСОКИЙ ПРИОРИТЕТ)

**Источники для консолидации:**
- `equip.md` — унифицированная система экипировки (✅ частично в INVENTORY_SYSTEM.md)
- `equip-v2.md` — экипировка v2 с Grade System
- `materials.md` — система материалов с ID (❌ НЕ мигрировано)
- `weapon-armor-system.md` — оружие, броня, формулы (❌ НЕ мигрировано)

**Что отсутствует в Unity:**
- Таблицы шансов попадания по частям тела
- Прицельные атаки (aimed attacks)
- Детальные формулы урона оружия
- Ремонт и понижение Grade
- Полный реестр материалов (5 тиров)

#### 2. MODIFIERS_SYSTEM.md (НОВЫЙ, ВЫСОКИЙ ПРИОРИТЕТ)

**Источник:** `bonuses.md`

**Что отсутствует в Unity:**
- Единая система бонусов и штрафов
- Мягкие капы (soft caps)
- Диминишинг (diminishing returns)
- Источники модификаторов (material, grade, curse, blessing)
- Примеры расчётов с капами

**КРИТИЧНО:** Эта система используется ВСЕМИ другими системами!

#### 3. ELEMENTS_SYSTEM.md (НОВЫЙ, СРЕДНИЙ ПРИОРИТЕТ)

**Источник:** `elements-system.md`

**Что отсутствует в Unity:**
- Детальные таблицы эффектов стихий по типам техник
- Poison механика (множественные дебаффы по Grade)
- Transcendent-эффекты для каждого элемента
- Ограничения по типам техник

#### 4. CHARGER_SYSTEM.md (НОВЫЙ, СРЕДНИЙ ПРИОРИТЕТ)

**Источник:** `charger.md`

**Что отсутствует в Unity:**
- Типы зарядников (belt, bracelet, necklace, ring, backpack)
- Режимы работы (trickle, normal, burst, combat)
- Буфер Ци зарядника
- Проводимость как ограничитель потока
- Использование в бою и медитации

---

## 🎮 Unity Migration (Новая папка)

Документы для миграции на Unity (только теория, без кода):

### Приоритет 1 — Ядро игры:
| Файл | Статус | Источники |
|------|--------|-----------|
| [Unity/ARCHITECTURE.md](../Unity/ARCHITECTURE.md) | ✅ DRAFT v1.0 | ARCHITECTURE.md |
| [Unity/BODY_SYSTEM.md](../Unity/BODY_SYSTEM.md) | ✅ DRAFT v1.0 | body.md, body-development-analysis.md, body_review.md, body_monsters.md |
| [Unity/TIME_SYSTEM.md](../Unity/TIME_SYSTEM.md) | ✅ DRAFT v1.0 | TIME_SYSTEM.md |
| [Unity/QI_SYSTEM.md](../Unity/QI_SYSTEM.md) | ✅ DRAFT v1.0 | start_lore.md, technique-system-v2.md, body_review.md |
| [Unity/COMBAT_SYSTEM.md](../Unity/COMBAT_SYSTEM.md) | ✅ DRAFT v1.0 | combat-system.md, technique-system-v2.md, body_review.md |
| [Unity/TECHNIQUE_SYSTEM.md](../Unity/TECHNIQUE_SYSTEM.md) | ✅ DRAFT v1.0 | technique-system-v2.md, combat-system.md |

### Приоритет 2 — Игровые системы:
| Файл | Статус | Источники |
|------|--------|-----------|
| [Unity/INVENTORY_SYSTEM.md](../Unity/INVENTORY_SYSTEM.md) | ✅ DRAFT v1.0 | inventory-system.md, equip.md, equip-v2.md |
| [Unity/NPC_AI_SYSTEM.md](../Unity/NPC_AI_SYSTEM.md) | ✅ DRAFT v1.0 | npc-session-integration.md, body_review.md |
| [Unity/WORLD_SYSTEM.md](../Unity/WORLD_SYSTEM.md) | ✅ DRAFT v1.0 | sector-architecture.md, soul-system.md |
| [Unity/SAVE_SYSTEM.md](../Unity/SAVE_SYSTEM.md) | ✅ DRAFT v1.0 | session.service docs |

### Приоритет 3 — Дополнительные системы:
| Файл | Статус | Источники |
|------|--------|-----------|
| [Unity/GENERATORS_SYSTEM.md](../Unity/GENERATORS_SYSTEM.md) | ✅ DRAFT v1.0 | generators.md |
| [Unity/FACTION_SYSTEM.md](../Unity/FACTION_SYSTEM.md) | ✅ DRAFT v1.0 | faction-system.md |
| [Unity/LORE_SYSTEM.md](../Unity/LORE_SYSTEM.md) | ✅ DRAFT v1.0 | start_lore.md |

### Приоритет 4 — Требуется создание (НОВЫЕ):
| Файл | Статус | Источники |
|------|--------|-----------|
| Unity/EQUIPMENT_SYSTEM.md | ❌ REQUIRED | equip.md, equip-v2.md, materials.md, weapon-armor-system.md |
| Unity/MODIFIERS_SYSTEM.md | ❌ REQUIRED | bonuses.md |
| Unity/ELEMENTS_SYSTEM.md | ❌ REQUIRED | elements-system.md |
| Unity/CHARGER_SYSTEM.md | ❌ REQUIRED | charger.md |

---

## 📁 Документы в /docs (оригиналы, НЕ изменять)

### Начало работы (3 файла)
| Файл | Описание |
|------|----------|
| [start_lore.md](./start_lore.md) | Лор мира культивации, система Ци, география |
| [INSTALL.md](./INSTALL.md) | Установка, запуск, скрипты |
| [PHASER_STACK.md](./PHASER_STACK.md) | Стек Phaser 3 |

### Архитектура (7 файлов)
| Файл | Описание |
|------|----------|
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Общая архитектура v19 |
| [TIME_SYSTEM.md](./TIME_SYSTEM.md) | Система времени TickTimer |
| [sector-architecture.md](./sector-architecture.md) | Архитектура мира |
| [FUNCTIONS.md](./FUNCTIONS.md) | Справочник функций |
| [inventory-system.md](./inventory-system.md) | Система инвентаря |
| [event-bus-system.md](./event-bus-system.md) | Шина событий |

### Сущности (5 файлов)
| Файл | Описание | Мигрирован? |
|------|----------|-------------|
| [body.md](./body.md) | Система тела (Kenshi-style) | ✅ в BODY_SYSTEM.md |
| [body_armor.md](./body_armor.md) | **Броня и прохождение урона** | ⚠️ Частично в COMBAT_SYSTEM.md |
| [soul-system.md](./soul-system.md) | SoulEntity + PhysicalObject | ✅ в WORLD_SYSTEM.md |
| [random_npc.md](./random_npc.md) | Генерация NPC | ✅ в NPC_AI_SYSTEM.md |

### Экипировка и материалы (7 файлов)
| Файл | Описание | Мигрирован? |
|------|----------|-------------|
| [equip.md](./equip.md) | **Унифицированная система экипировки** | ⚠️ Частично в INVENTORY_SYSTEM.md |
| [equip-v2.md](./equip-v2.md) | **Экипировка v2 (Grade System)** | ⚠️ Частично в INVENTORY_SYSTEM.md |
| [materials.md](./materials.md) | **Система материалов с ID** | ❌ НЕ мигрировано |
| [weapon-armor-system.md](./weapon-armor-system.md) | **Оружие и броня (теория)** | ❌ НЕ мигрировано |
| [bonuses.md](./bonuses.md) | **Единая система бонусов/штрафов** | ❌ НЕ мигрировано |
| [elements-system.md](./elements-system.md) | **Система стихий (8 элементов)** | ⚠️ Частично в TECHNIQUE_SYSTEM.md |
| [charger.md](./charger.md) | **Зарядник Ци** | ❌ НЕ мигрировано |

### Техники и бой (5 файлов)
| Файл | Описание | Мигрирован? |
|------|----------|-------------|
| [technique-system-v2.md](./technique-system-v2.md) | Система техник v2.1 | ✅ в TECHNIQUE_SYSTEM.md |
| [combat-system.md](./combat-system.md) | Боевая система | ✅ в COMBAT_SYSTEM.md |
| [qi_stone.md](./qi_stone.md) | Камни Ци | ⚠️ Частично |
| [vitality-hp-system.md](./vitality-hp-system.md) | Система HP | ✅ в BODY_SYSTEM.md |
| [condition-system.md](./condition-system.md) | Система состояний | ⚠️ Частично |

### NPC и AI (3 файла)
| Файл | Описание | Мигрирован? |
|------|----------|-------------|
| [npc-session-integration.md](./npc-session-integration.md) | Интеграция NPC | ✅ в NPC_AI_SYSTEM.md |
| [NPC_AI_THEORY.md](./NPC_AI_THEORY.md) | Теория NPC AI | ✅ в NPC_AI_SYSTEM.md |
| [NPC_AI_NEUROTHEORY.md](./NPC_AI_NEUROTHEORY.md) | Нейротеория AI | ⚠️ Частично |

### Прочие системы (10 файлов)
| Файл | Описание | Мигрирован? |
|------|----------|-------------|
| [generators.md](./generators.md) | Генераторы | ✅ в GENERATORS_SYSTEM.md |
| [faction-system.md](./faction-system.md) | Фракции | ✅ в FACTION_SYSTEM.md |
| [relations-system.md](./relations-system.md) | Отношения | ⚠️ Частично |
| [stat-development-system.md](./stat-development-system.md) | Развитие статов | ✅ в BODY_SYSTEM.md |
| [body-development-analysis.md](./body-development-analysis.md) | Анализ развития | ✅ в BODY_SYSTEM.md |
| [body_review.md](./body_review.md) | Обзор тела | ✅ в BODY_SYSTEM.md |
| [body_monsters.md](./body_monsters.md) | Тела монстров | ✅ в BODY_SYSTEM.md |

---

## 📊 Сводка миграции

| Категория | Всего | Мигрировано | Частично | Не мигрировано |
|-----------|-------|-------------|----------|----------------|
| Ядро (архитектура, тело, время) | 6 | 6 | 0 | 0 |
| Боевые системы | 5 | 3 | 2 | 0 |
| Экипировка и материалы | 7 | 0 | 2 | **5** |
| NPC и AI | 3 | 2 | 1 | 0 |
| Прочие | 10 | 7 | 2 | 1 |
| **ИТОГО** | **31** | **18** | **7** | **6** |

### Критические пробелы (НЕОБХОДИМО мигрировать):

1. **bonuses.md** → Unity/MODIFIERS_SYSTEM.md
   - Критично для всех систем модификаторов
   
2. **materials.md** → Unity/EQUIPMENT_SYSTEM.md
   - Необходимо для генерации экипировки
   
3. **weapon-armor-system.md** → Unity/EQUIPMENT_SYSTEM.md
   - Детальные формулы урона и защиты
   
4. **body_armor.md** (дополнения) → Unity/COMBAT_SYSTEM.md
   - Детальные таблицы покрытия брони
   
5. **charger.md** → Unity/CHARGER_SYSTEM.md
   - Уникальная система зарядников Ци
   
6. **elements-system.md** → Unity/ELEMENTS_SYSTEM.md
   - Детальная система стихий с poison

---

## 🔗 Связанные документы

- [Unity/ARCHITECTURE.md](../Unity/ARCHITECTURE.md) — Общая архитектура Unity
- [Unity/BODY_SYSTEM.md](../Unity/BODY_SYSTEM.md) — Система тела
- [Unity/COMBAT_SYSTEM.md](../Unity/COMBAT_SYSTEM.md) — Боевая система

---

*Файл обновлён: 2026-03-30*
*Анализ миграции: ВЫПОЛНЕН*

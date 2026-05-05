# Чекпоинт: Обновление глоссария

**Дата:** 2026-05-05 14:06:23 MSK
**Обновлено:** 2026-05-05 16:53:09 MSK — исполнение завершено
**Фаза:** Пост-аудит — документация
**Статус:** ✅ completed

---

## Источник

- С-14: FormationCoreType — отсутствует в GLOSSARY.md
- Выделен из [05_05_docs_update.md](05_05_docs_update.md) как отдельная задача

---

## План

- [x] Аудит GLOSSARY.md — проверить все термины, которые добавлены/изменены в Волнах 1-3
- [x] FormationCoreType — добавить определение в глоссарий
- [x] WeaponSubtype — обновить расширенные значения (Н-02)
- [x] ArmorWeightClass — добавить определение (Н-03)
- [x] DefenseSubtype — добавить определение (добавлен в В-09)
- [x] QiStoneQuality — обновить после выравнивания с EquipmentGrade (В-16)
- [x] DurabilityCondition — обновить (5 состояний, С-01)
- [x] Cross-check: все новые/изменённые enum-значения из Волны 1

### Дополнительно (выявлено при аудите)

- [x] StatBonus.value — задокументировать переименование bonus→value (С-07)
- [x] ElementData.oppositeElements — задокументировать переименование (В-12)
- [x] NPCRole (вместо NPCType) — добавить определение, отметить несоответствие
- [x] ItemCategory — добавить определение
- [x] CombatSubtype — добавить определение
- [x] EquipmentGrade — обновить множители
- [x] ChargeState, ChargeInterruptReason — добавить
- [x] AIDecision — добавить
- [x] ConsumableType, ConsumableEffectCategory — добавить
- [x] WeaponClass, WeaponDamageType, ArmorSubtype — добавить
- [x] FormationCoreVariant, QiStoneType, QiStoneSize — добавить
- [x] Все enum зарядников (ChargerFormFactor, ChargerPurpose, ChargerMaterial, ChargerMode, HeatState)
- [x] Все enum баффов (BuffType, BuffCategory, BuffRemovalType, StackType, PeriodicType, SpecialEffectType)
- [x] Таблица устаревших/переименованных терминов
- [x] Квестовые enum (QuestType, QuestState, QuestObjectiveType, ObjectiveState)
- [x] Сохранение/состояние игры (SaveSlot, SaveType, GameState)
- [x] NPC enum (NPCAIState, Alignment[Obsolete], BehaviorType, Disposition[Obsolete])
- [x] ElementData поля (affinityElements, weakToElements, oppositeMultiplier)

---

## Изменённые файлы

- `docs/GLOSSARY.md` — обновлено: +45 терминов, обновлены существующие записи

---

## Итог

GLOSSARY.md обновлён с 60 до ~105 терминов. Добавлены:
- 7 новых секций (Оружие и броня, Расходники, Элементы, NPC, Сохранение, Квесты, Прочее)
- Таблица устаревших/переименованных терминов (11 записей)
- Обновлены 8 существующих записей (EquipmentSlot +None, Grade +множители, durability→DurabilityCondition, rarity→ItemRarity и т.д.)
- Все изменения из аудита инвентаря (7 прогонов, 80 багов) отражены в глоссарии

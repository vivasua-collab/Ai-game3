# Чекпоинт: Fix-02 — Combat Damage Pipeline

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** P0 (блокирует gameplay)

---

## Описание

Сердце боевой системы — пайплайн расчёта урона. Стихийные взаимодействия не вызываются, ParryBonus считается дважды, AttackType всегда Technique, DefenseProcessor использует System.Random, шансы не ограничены сверху.

---

## Файлы (4 файла, ~1241 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `Combat/DamageCalculator.cs` | 346 | Elemental interaction, AttackType, defenderElement |
| 2 | `Combat/DefenseProcessor.cs` | 274 | System.Random→UnityEngine.Random, chance caps [0,1] |
| 3 | `Combat/CombatEvents.cs` | 332 | CombatLog надёжная инициализация |
| 4 | `Combat/HitDetector.cs` | 437 | IsValidTarget Ally/Neutral, SizeClass |

---

## Задачи

### CRITICAL
- [ ] CMB-C01: DamageCalculator — вызвать CalculateElementalInteraction вместо CalculateElementMultiplier. Добавить defenderElement в расчёт. Реализовать схему противоположностей:
  - Fire ↔ Water: ×1.5 opposite, ×0.8 affinity
  - Earth ↔ Air: ×1.5 opposite, ×0.8 affinity
  - Lightning ↔ Void: ×1.5 opposite, ×0.8 affinity
  - Fire → Poison: ×1.2 (выжигание токсинов, одностороннее)
  - Void → All: ×1.2 (поглощение)
  - Neutral → All: ×1.0
- [ ] CMB-C02: DamageCalculator:128 — добавить AttackType.Normal для обычных атак (IsQiTechnique ? Technique : Normal)
- [ ] CMB-C03: Combatant.cs:282-284 — ParryBonus/BlockBonus передавать сырые бонусы экипировки, не финальные шансы. DamageCalculator:192-194 — не пересчитывать
- [ ] CMB-C06: DefenseProcessor — заменить `System.Random` на `UnityEngine.Random` (:77,232,271)
- [ ] CMB-C07: CombatEvents.cs:274-277 — CombatLog: убрать подписку из статического конструктора, добавить явную инициализацию и очистку
- [ ] CMB-C10: HitDetector.IsValidTarget() — интегрировать FactionController для Ally/Neutral целей (:177-186). Использовать новый enum Attitude вместо Disposition

### HIGH
- [ ] CMB-H05: DamageCalculator — добавить defenderElement в DefenderParams (:68-85)
- [ ] CMB-H09: Combatant.cs:147-165 — weapon/armor/shield бонусы из EquipmentController вместо 0f (частичная интеграция: добавить поля, заполнить из EquipmentController если доступен)

### MEDIUM
- [ ] CMB-M06: DefenseProcessor — добавить Mathf.Clamp01 для DodgeChance, ParryChance, BlockChance
- [ ] CMB-M07: HitDetector.SizeClass — интегрировать в формулу попадания (:331)

---

## Порядок выполнения

1. DefenseProcessor.cs — System.Random→UnityEngine.Random + Clamp01
2. DamageCalculator.cs — elemental interaction (Variant A схема) + AttackType.Normal + defenderElement в DefenderParams
3. Combatant.cs — ParryBonus/BlockBonus сырые бонусы + equipment бонусы
4. CombatEvents.cs — CombatLog явная инициализация
5. HitDetector.cs — IsValidTarget фракции (через Attitude enum) + SizeClass

---

## Зависимости

- **Предшествующие:** Fix-01 (SpendQi long + DefenderParams.CurrentQi long)
- **Последующие:** Fix-03 (Qi System), Fix-04 (Core — Constants.OppositeElements)

---

## Примечания

- CMB-C01 требует обновления Constants.OppositeElements — делается здесь или в Fix-04 (дублирование допустимо для избежания зависимости)
- CMB-C03 требует понимания архитектуры ParryChance/BlockChance: где считаются базовые шансы и где добавляются бонусы экипировки
- CMB-C10 использует FactionController через ServiceLocator для определения отношения. После Fix-04 использовать новый enum Attitude
- CMB-H09 может потребовать изменений в EquipmentController.cs — если слишком сложно, вынести в Fix-08

---

*Чекпоинт обновлён: 2026-04-10 13:37:00 UTC*

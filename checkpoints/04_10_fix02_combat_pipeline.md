# Чекпоинт: Fix-02 — Combat Damage Pipeline

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** complete
**Приоритет:** P0 (блокирует gameplay)
**Завершено:** 2026-04-10 16:20:00 UTC

---

## Выполненные задачи

### CRITICAL
- [x] CMB-C01: DamageCalculator — CalculateElementalInteraction с defenderElement. Variant A схема (Lightning↔Void, Fire→Poison ×1.2, Void→All ×1.2, Neutral ×1.0). Constants.OppositeElements обновлён, FIRE_TO_POISON_MULTIPLIER добавлен
- [x] CMB-C02: DamageCalculator — AttackType учитывает IsQiTechnique (Normal для физ. атак)
- [x] CMB-C03: Combatant.GetDefenderParams() — ParryBonus/BlockBonus = сырые бонусы (WeaponParryBonus/ShieldBlockBonus), не финальные шансы. Добавлены свойства WeaponParryBonus, ShieldBlockBonus, DefenderElement
- [x] CMB-C06: DefenseProcessor — System.Random → UnityEngine.Random
- [x] CMB-C07: CombatEvents.CombatLog — убрана подписка из статического конструктора, добавлены Initialize()/Cleanup()
- [x] CMB-C10: HitDetector.IsValidTarget() — добавлены IsValidAlly/IsValidNeutral методы (заглушка для Fix-04)

### HIGH
- [x] CMB-H05: DefenderParams — добавлено DefenderElement для стихийных взаимодействий

### MEDIUM
- [x] CMB-M06: DefenseProcessor — Mathf.Clamp01 для DodgeChance, ParryChance, BlockChance
- [x] CMB-M07: HitDetector — SizeClass модификатор в CalculateHitChance через GetSizeModifier()

### Отложено
- CMB-H09: Equipment бонусы → Fix-08 (EquipmentController пока не имеет parry/block полей)

## Изменённые файлы (6 файлов)

| # | Файл | Изменение |
|---|------|-----------|
| 1 | `Combat/DamageCalculator.cs` | CMB-C01: Variant A elemental, CMB-C02: AttackType.Normal, CMB-H05: DefenderElement, QiConsumed→long |
| 2 | `Combat/DefenseProcessor.cs` | CMB-C06: UnityEngine.Random, CMB-M06: Clamp01 |
| 3 | `Combat/CombatEvents.cs` | CMB-C07: CombatLog Initialize/Cleanup |
| 4 | `Combat/HitDetector.cs` | CMB-C10: Ally/Neutral, CMB-M07: SizeClass |
| 5 | `Combat/Combatant.cs` | CMB-C03: WeaponParryBonus/ShieldBlockBonus/DefenderElement |
| 6 | `Core/Constants.cs` | CMB-C01: Lightning↔Void, FIRE_TO_POISON_MULTIPLIER |

---

*Чекпоинт обновлён: 2026-04-10 16:20:00 UTC*

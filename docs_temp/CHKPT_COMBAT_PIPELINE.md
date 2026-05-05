# 🟠 Чекпоинт: Боевой пайплайн — план исправления

**Дата:** 2026-05-08 | **Статус:** ПЛАН | **Приоритет:** ВЫСОКИЙ
**Связанный аудит:** AUDIT_COMBAT_EQUIPMENT_v3.md (В-01, В-02, В-03, В-04, В-06, В-09, К-06, К-07, К-08, С-02, С-04)

---

## Проблемы (все верифицированы ✅)

### В-01: Парирование/блок — захардкоженные множители
- **Файл:** `Combat/DamageCalculator.cs` строки 247–258
- **Сейчас:**
  ```csharp
  damage *= 0.5f;  // Парирование — ЗАХАРДКОЖЕНО
  damage *= 0.3f;  // Блок — ЗАХАРДКОЖЕНО
  ```
- **Должно быть (COMBAT_SYSTEM.md Слой 4):**
  ```
  Парирование: damage *= (1 - blockEffectiveness), durability--
  Блок щитом: damage *= (1 - shieldEffectiveness), shieldDurability--
  ```
- **Исправление:**
  1. Добавить `float BlockEffectiveness` и `float ShieldEffectiveness` в `DefenderParams`
  2. В DamageCalculator заменить хардкод на чтение из DefenderParams:
     ```csharp
     damage *= (1f - defenseData.BlockEffectiveness);  // Парирование
     damage *= (1f - defenseData.ShieldEffectiveness);  // Блок щитом
     ```
  3. PlayerController/NPCController: передавать значения из EquipmentController
  4. Добавить вызов EquipmentController.DamageEquipment() при парировании/блоке

### В-02: CombatantBase заглушки → Player/NPC уже переопределяют
- **Вердикт верификации:** ⚠️ ЧАСТИЧНО — PlayerController и NPCController реализуют ICombatant напрямую через EquipmentController
- **Но:** CombatantBase заглушки = мёртвый код, ловушка для будущих классов
- **Исправление:**
  1. Пометить CombatantBase заглушки `[Obsolete("Используйте EquipmentController")]`
  2. Или удалить CombatantBase целиком, если нет наследников
  3. Убедиться, что монстры/существа тоже получают экипировочные статы

### В-03 + В-06: Износ брони и оружия не интегрирован
- **Файл:** `Inventory/EquipmentController.cs` — DamageEquipment() существует, но 0 вызовов
- **Исправление:**
  1. В CombatManager (или DamageCalculator) после расчёта урона:
     ```csharp
     // Износ брони защитника
     if (result.WasBlocked || result.ArmorAbsorbed)
         defender.EquipmentController?.DamageEquipment(hitSlot, (int)(result.FinalDamage * 0.1f / hardness));
     
     // Износ оружия атакующего
     if (result.AttackerWeaponSlot != EquipmentSlot.None)
         attacker.EquipmentController?.DamageEquipment(result.AttackerWeaponSlot, CalculateWeaponWear(result));
     ```
  2. Формула износа оружия (EQUIPMENT_SYSTEM.md §4.2):
     ```csharp
     int wearThreshold = hardness * 10;
     int additionalWear = Mathf.Max(0, (int)(damage - wearThreshold) / 10);
     return 1 + additionalWear; // Минимум 1 износ за удар
     ```
  3. Нужен интерфейс для передачи информации о слоте оружия атакующего через AttackerParams

### В-04 + К-06 + К-07: Последствия урона (Слой 10)
- **Файл:** `Combat/DamageCalculator.cs` — только IsFatal
- **Требуется реализовать (COMBAT_SYSTEM.md Слой 10):**
  1. **Кровотечение:** если damage > threshold → bleedDamage per tick
     ```csharp
     if (result.FinalDamage > bleedThreshold)
     {
         result.BleedDamage = CalculateBleedDamage(result.FinalDamage, result.HitPart);
         result.BleedDuration = CalculateBleedDuration(result.FinalDamage);
     }
     ```
     Степени из BODY_SYSTEM.md: 1/3/5/10 HP/тик в зависимости от тяжести
  2. **Шок:** если redHP < 30% → штрафы к действиям
     ```csharp
     if (defender.RedHPRatio < 0.3f)
     {
         result.IsInShock = true;
         result.ShockPenalty = CalculateShockPenalty(defender.RedHPRatio);
     }
     ```
  3. **Оглушение:** шанс = damage / maxHP × 10%
     ```csharp
     float stunChance = (result.FinalDamage / defender.MaxHP) * 0.1f;
     result.IsStunned = RollChance(stunChance);
     ```
- **Дополнительные поля в DamageResult:** BleedDamage, BleedDuration, IsInShock, ShockPenalty, IsStunned, StunDuration
- **Обработчики в CombatantBase/PlayerController/NPCController:** применять эффекты при получении результата

### В-09: HasActiveShield() слишком широкий
- **Файл:** `Combat/TechniqueController.cs` строки 114–124
- **Сейчас:** Проверяет `TechniqueType.Defense` — возвращает true для ЛЮБОЙ защитной техники
- **Исправление:**
  ```csharp
  public bool HasActiveShield()
  {
      foreach (var tech in learnedTechniques)
      {
          if (tech.Data != null &&
              tech.Data.techniqueType == TechniqueType.Defense &&
              tech.Data.defenseSubtype == DefenseSubtype.Shield && // NEW
              tech.CooldownRemaining <= 0f)
              return true;
      }
      return false;
  }
  ```
- **Зависимость:** Нужен enum `DefenseSubtype { Block, Parry, Shield, Dodge, Reflect }` или поле в TechniqueData

### К-08 + С-10: Регенерация через Update() вместо OnTick/OnWorldTick
- **Файлы:** `Body/BodyController.cs`, `Qi/QiController.cs`
- **Сейчас:** Используют `Update()` + `Time.deltaTime`
- **Исправление (BodyController):**
  ```csharp
  // Удалить Update()
  // Добавить:
  public void OnWorldTick(float tickInterval)
  {
      if (enableRegeneration && IsAlive)
      {
          ProcessRegeneration(tickInterval); // Использовать tickInterval вместо deltaTime
      }
  }
  ```
- **Аналогично для QiController.ProcessPassiveRegeneration()**
- **Регистрация:** Подписать на TimeController.OnWorldTick

### С-02: Формула урона оружия упрощена
- **Файл:** `Combat/DamageCalculator.cs` строки 159–162
- **Сейчас:** `damage += attacker.WeaponBonusDamage;` (плоский бонус)
- **Должно быть (EQUIPMENT_SYSTEM.md §7.3):**
  ```
  handDamage = 3 + (STR-10) × 0.3
  baseDamage = max(handDamage, weaponDamage × 0.5)
  statScaling = (STR×STR_BONUS + AGI×AGI_BONUS) / 100
  bonusDamage = weaponDamage × statScaling
  totalDamage = baseDamage + bonusDamage
  ```
- **Исправление:** Заменить простую добавку на полную формулу. Потребует передачи STR/AGI через AttackerParams.

### С-04: Стихийные эффекты не реализованы
- **Файл:** `Combat/DamageCalculator.cs`
- **Сейчас:** CalculateElementalInteraction только модифицирует множитель урона
- **Должно быть (COMBAT_SYSTEM.md §«Эффекты стихий»):**
  - Огонь → Горение (DoT 5%/тик, 3 тика)
  - Вода → Замедление (−20% скорости, 2 тика)
  - Земля → Оглушение (15% шанс)
  - Воздух → Откидывание
  - Молния → Паралич (1 тик)
  - Пустота → Истощение Ци
- **Исправление:** После CalculateElementalInteraction добавить ApplyElementalEffect() который создаёт соответствующий статусный эффект

---

## Порядок исправления

1. **В-01** — Добавить поля в DefenderParams, заменить хардкод (~30 мин)
2. **В-09** — Добавить DefenseSubtype, исправить HasActiveShield() (~20 мин)
3. **К-08 + С-10** — Перенести регенерацию в OnWorldTick (~45 мин)
4. **В-03 + В-06** — Интегрировать DamageEquipment() (~45 мин)
5. **В-04 + К-06 + К-07** — Реализовать последствия (Слой 10) (~90 мин)
6. **С-02** — Полная формула урона оружия (~45 мин)
7. **С-04** — Стихийные эффекты (~60 мин)

**Итого:** ~335 мин (~5.5 часов)

---

## Риски

- **В-04 (Последствия):** Новый функционал — нужен дизайн полей в DamageResult и обработчиков в CombatManager
- **К-08 (OnWorldTick):** Нужно убедиться что TimeController корректно вызывает OnWorldTick для всех подписчиков
- **С-02 (Формула урона):** Изменение формулы может значительно изменить баланс — требует тестирования
- **В-09 (DefenseSubtype):** Нужен новый enum + обновление TechniqueData — cascade change

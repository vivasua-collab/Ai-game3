# 📦 Кодовая база: Исправление боевого пайплайна

👉 Основной план: [05_05_combat_pipeline_fix.md](05_05_combat_pipeline_fix.md)
**Дата:** 2026-05-05 09:45:07 UTC
**Статус:** in_progress

---

## В-01: Парирование/блок — заменить хардкод

**Файл:** `Scripts/Combat/DamageCalculator.cs` строки 247–258

**Сейчас:**
```csharp
damage *= 0.5f;  // Парирование — ЗАХАРДКОЖЕНО 50%
damage *= 0.3f;  // Блок — ЗАХАРДКОЖЕНО 70% снижение
```

**Исправление:**

1. Добавить в DefenderParams:
```csharp
public float BlockEffectiveness;   // 0-1, эффективность парирования
public float ShieldEffectiveness;  // 0-1, эффективность блокирования щитом
```

2. В DamageCalculator:
```csharp
// Парирование
if (RollChance(defenseData.ParryChance))
{
    result.WasParried = true;
    damage *= (1f - defenseData.BlockEffectiveness);
}

// Блок щитом
if (RollChance(defenseData.BlockChance))
{
    result.WasBlocked = true;
    damage *= (1f - defenseData.ShieldEffectiveness);
}
```

3. PlayerController/NPCController: передавать значения из EquipmentController

---

## В-02: CombatantBase заглушки — пометить Obsolete

**Файл:** `Scripts/Combat/Combatant.cs`

Верификация показала: PlayerController/NPCController НЕ наследуют CombatantBase,
реализуют ICombatant напрямую. Заглушки = мёртвый код.

**Исправление:**
```csharp
[Obsolete("Используйте EquipmentController через ICombatant")]
public virtual float ArmorCoverage => 0f;
// ... и т.д.
```

---

## В-03 + В-06: Износ брони и оружия

**Файл:** `Scripts/Inventory/EquipmentController.cs` — DamageEquipment() существует, 0 вызовов

**Интеграция в CombatManager:**
```csharp
// После расчёта урона:
// Износ брони защитника (COMBAT_SYSTEM.md Слой 7)
if (result.WasBlocked || result.ArmorAbsorbed)
{
    var eq = defender.GetComponent<EquipmentController>();
    if (eq != null)
    {
        int armorWear = Mathf.Max(1, (int)(result.FinalDamage * 0.1f / armorHardness));
        eq.DamageEquipment(hitSlot, armorWear);
    }
}

// Износ оружия атакующего (EQUIPMENT_SYSTEM.md §4.2)
if (result.AttackerWeaponSlot != EquipmentSlot.None)
{
    var eq = attacker.GetComponent<EquipmentController>();
    if (eq != null)
    {
        int wearThreshold = weaponHardness * 10;
        int additionalWear = Mathf.Max(0, (int)(result.FinalDamage - wearThreshold) / 10);
        eq.DamageEquipment(result.AttackerWeaponSlot, 1 + additionalWear);
    }
}
```

---

## В-04 + К-06 + К-07: Последствия урона (Слой 10)

**Файл:** `Scripts/Combat/DamageCalculator.cs`

**Сейчас:** только IsFatal проверка

**Добавить в DamageResult:**
```csharp
public float BleedDamage;      // Кровотечение: урон за тик
public int BleedDuration;      // Длительность в тиках
public bool IsInShock;         // Шок
public float ShockPenalty;     // Штраф шока
public bool IsStunned;         // Оглушение
public float StunDuration;     // Длительность оглушения в секундах
```

**Логика кровотечения (К-06 → пороговая система):**
```csharp
// Документация: если damage > threshold → bleedDamage per tick
// Степени из BODY_SYSTEM.md: 1/3/5/10 HP/тик
float bleedThreshold = part.MaxRedHP * 0.3f;
if (result.FinalDamage > bleedThreshold)
{
    result.BleedDamage = CalculateBleedSeverity(result.FinalDamage, result.HitPart);
    result.BleedDuration = 5; // 5 тиков
}

float CalculateBleedSeverity(float damage, BodyPartType part)
{
    float ratio = damage / 100f;
    if (ratio > 2.0f) return 10f;  // Тяжёлое
    if (ratio > 1.0f) return 5f;   // Среднее
    if (ratio > 0.5f) return 3f;   // Лёгкое
    return 1f;                       // Микро
}
```

**Логика шока (К-07 → redHP < 30%):**
```csharp
// Документация: если redHP < 30% → штрафы
float redHPRatio = defender.CurrentRedHP / defender.MaxRedHP;
if (redHPRatio < 0.3f)
{
    result.IsInShock = true;
    result.ShockPenalty = (0.3f - redHPRatio) / 0.3f; // 0..1
}
```

**Логика оглушения:**
```csharp
// Документация: шанс = damage / maxHP × 10%
float stunChance = Mathf.Min(0.5f, result.FinalDamage / defender.MaxHP * 0.1f);
result.IsStunned = RollChance(stunChance);
result.StunDuration = result.IsStunned ? 1.5f : 0f;
```

---

## В-09: HasActiveShield() — проверить DefenseSubtype

**Файл:** `Scripts/Combat/TechniqueController.cs` строки 114–124

**Сейчас:** Проверяет TechniqueType.Defense — true для ЛЮБОЙ защитной техники

**Исправление:**

1. Добавить enum DefenseSubtype в Enums.cs:
```csharp
public enum DefenseSubtype
{
    None, Block, Parry, Shield, Dodge, Reflect
}
```

2. Добавить поле в TechniqueData:
```csharp
public DefenseSubtype defenseSubtype = DefenseSubtype.None;
```

3. Исправить HasActiveShield():
```csharp
public bool HasActiveShield()
{
    foreach (var tech in learnedTechniques)
    {
        if (tech.Data != null &&
            tech.Data.techniqueType == TechniqueType.Defense &&
            tech.Data.defenseSubtype == DefenseSubtype.Shield &&
            tech.CooldownRemaining <= 0f)
            return true;
    }
    return false;
}
```

---

## К-08 + С-10: Регенерация → OnWorldTick

**Файл:** `Scripts/Body/BodyController.cs` строки 72–78

**Сейчас:**
```csharp
private void Update()
{
    if (enableRegeneration && IsAlive)
    {
        ProcessRegeneration(); // Time.deltaTime
    }
}
```

**Исправление:**
```csharp
// Удалить Update()
public void OnWorldTick(float tickInterval)
{
    if (enableRegeneration && IsAlive)
    {
        ProcessRegeneration(tickInterval);
    }
}
```

**Аналогично QiController.ProcessPassiveRegeneration()** — заменить Update() на OnWorldTick()

**Регистрация:** Подписать на TimeController.OnWorldTick в OnEnable/OnDisable

---

## В-07: Проводимость QiController — coreCapacity вместо maxQiCapacity

**Файл:** `Scripts/Qi/QiController.cs` строка 107

**Сейчас:**
```csharp
baseConductivity = maxQiCapacity / 360f;
```

**Должно быть:**
```csharp
baseConductivity = coreCapacity / 360f; // По QI_SYSTEM.md
```

---

## В-08: Meditate() обходит тиковую систему

**Файл:** `Scripts/Qi/QiController.cs` строки 260–287

**Сейчас:** AddQi(gained) + AdvanceHours() — всё за один кадр

**Исправление:** Обрабатывать по тикам:
```csharp
public long Meditate(int durationTicks)
{
    long totalGained = 0;
    float qiPerTick = baseConductivity * qiDensity * meditationMult;

    for (int i = 0; i < durationTicks; i++)
    {
        long gained = (long)qiPerTick;
        AddQi(gained);
        totalGained += gained;
        // TimeController обработает один тик — все системы получат обновление
    }
    return totalGained;
}
```

---

## С-02: Формула урона оружия — полная реализация

**Файл:** `Scripts/Combat/DamageCalculator.cs` строки 159–162

**Сейчас:**
```csharp
damage += attacker.WeaponBonusDamage; // Плоский бонус
```

**Должно быть (EQUIPMENT_SYSTEM.md §7.3):**
```csharp
float handDamage = 3f + (attacker.Strength - 10f) * 0.3f;
float weaponDmg = attacker.WeaponDamage;
float baseDamage = Mathf.Max(handDamage, weaponDmg * 0.5f);

float strBonus = attacker.Strength * attacker.STR_BONUS_RATIO;
float agiBonus = attacker.Agility * attacker.AGI_BONUS_RATIO;
float statScaling = (strBonus + agiBonus) / 100f;
float bonusDamage = weaponDmg * statScaling;

float totalWeaponDamage = baseDamage + bonusDamage;
```

**Зависимость:** Добавить Strength, Agility, WeaponDamage, STR_BONUS_RATIO, AGI_BONUS_RATIO в AttackerParams

---

## С-04: Стихийные эффекты

**Файл:** `Scripts/Combat/DamageCalculator.cs`

После CalculateElementalInteraction — добавить ApplyElementalEffect():
```csharp
void ApplyElementalEffect(Element element, DamageResult result, ICombatant defender)
{
    switch (element)
    {
        case Element.Fire:
            // Горение: DoT 5%/тик, 3 тика
            result.ElementalEffect = new ElementalEffect
            {
                type = ElementalEffectType.Burn,
                damagePerTick = defender.MaxHP * 0.05f,
                duration = 3
            };
            break;
        case Element.Water:
            // Замедление: -20% скорости, 2 тика
            result.ElementalEffect = new ElementalEffect
            {
                type = ElementalEffectType.Slow,
                speedPenalty = 0.2f,
                duration = 2
            };
            break;
        case Element.Earth:
            // Оглушение: 15% шанс
            if (RollChance(0.15f))
                result.IsStunned = true;
            break;
        // и т.д.
    }
}
```

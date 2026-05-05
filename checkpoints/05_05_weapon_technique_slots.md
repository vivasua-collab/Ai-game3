# Чекпоинт: Слот оружия и слоты техник

**Дата:** 2026-05-05 06:49:16 UTC
**Редактировано:** 2026-05-06 08:30:00 UTC — расширенный аудит 25 файлов, 16 новых находок, обновлённый план
**Статус:** in_progress — полный аудит завершён, план расширен

---

## АУДИТ КОДА

### Что есть — Слот оружия (EquipmentController)

| Аспект | Статус | Комментарий |
|--------|--------|-------------|
| `EquipmentSlot.WeaponMain` | ✅ Существует | Основная рука |
| `EquipmentSlot.WeaponOff` | ✅ Существует | Вторичная рука |
| `WeaponHandType.OneHand/TwoHand` | ✅ Существует | Тип хвата |
| 1H/2H логика (EquipTwoHand) | ✅ Работает | 2H блокирует WeaponOff |
| `GetMainWeapon()` / `GetOffWeapon()` | ✅ Есть | Возвращает EquipmentInstance |
| `EquipmentInstance` (damage, defense, bonuses) | ✅ Есть | Данные предмета |
| `EquipmentStats` (totalDamage, totalDefense и др.) | ✅ Есть | Кэш с dirty-флагом |
| `customBonuses` Dictionary | ✅ Есть | Можно хранить произвольные бонусы |
| **Связь оружия ↔ бой** | ❌ ОТСУТСТВУЕТ | PlayerController.GetAttackerParams() = hardcoded `WeaponBonusDamage = 0f` |
| **WeaponBonusDamage из EquipmentController** | ❌ ОТСУТСТВУЕТ | Оба контроллера: WeaponBonusDamage = 0f |
| **ParryBonus из оружия** | ❌ ОТСУТСТВУЕТ | `ParryChance` использует `0f` бонус |
| **BlockBonus из щита** | ❌ ОТСУТСТВУЕТ | `BlockChance` использует `0f` бонус |
| **ArmorValue из экипировки** | ❌ ОТСУТСТВУЕТ | `ArmorValue = 0` |
| **DamageReduction из экипировки** | ❌ ОТСУТСТВУЕТ | `DamageReduction = 0f` |
| **ToolMultiplier для добычи** | ❌ ОТСУТСТВУЕТ | `toolMultiplier = 1.0f` hardcoded |
| **EquipmentController на Player** | ❓ НЕ ССЫЛАЕТСЯ | CharacterPanelUI находит через GetComponent — EquipmentController есть на GO, но PlayerController его не держит |
| **EquipmentController на NPC** | ❌ НЕТ | NPCRuntimeSpawner НЕ добавляет EquipmentController |

### Что есть — Слоты техник (TechniqueController)

| Аспект | Статус | Комментарий |
|--------|--------|-------------|
| `LearnedTechnique` struct | ✅ Существует | Data, Mastery, QuickSlot, CooldownRemaining |
| `quickSlots[]` (max=10) | ✅ Существует | Массив слотов быстрого доступа |
| `AssignToQuickSlot()` | ✅ Существует | Назначение техники в слот |
| `GetQuickSlotTechnique()` | ✅ Существует | Получение техники из слота |
| `UseQuickSlot()` | ✅ Существует | Использование через накачку |
| `CombatUI.techniqueSlots[9]` | ✅ Существует | UI слоты техник (9 шт., клавиши 1-9) |
| `TechniqueSlotUI` компонент | ✅ Существует | Иконка, номер, кулдаун, накачка |
| `TechniqueChargeSystem` | ✅ Существует | Полная система накачки |
| **Назначение техник в слоты (UI)** | ❌ ОТСУТСТВУЕТ | Нет интерфейса drag-drop или окна назначения |
| **Отображение оружия в CombatUI** | ❌ ОТСУТСТВУЕТ | Нет иконки оружия / информации о вооружении |
| **MeleeWeapon → требует оружие** | ❌ ОТСУТСТВУЕТ | Нет проверки: melee_weapon без оружия = fallback к melee_strike |
| **Оружие влияет на технику** | ❌ ОТСУТСТВУЕТ | Нет связи: оружие → бонус урона / скорость накачки |

---

## 🚨 КРИТИЧЕСКИЕ НАХОДКИ ГЛУБОКОГО АУДИТА

### КН-1: PlayerController и NPCController НЕ наследуют CombatantBase

**Оба контроллера реализуют `ICombatant` НАПРЯМУЮ**, через explicit interface implementation. CombatantBase существует, но НЕ используется ни PlayerController, ни NPCController.

**Следствия для плана:**
- ❌ Нельзя просто переопределить виртуальные свойства в CombatantBase
- ✅ Нужно добавлять EquipmentController в КАЖДЫЙ контроллер индивидуально
- ⚖️ Альтернатива: рефакторинг обоих контроллеров → наследование от CombatantBase (рискованно, крупное изменение)

**Решение:** Добавить EquipmentController напрямую в PlayerController и NPCController, без рефакторинга наследования. Это минимизирует риск поломки.

### КН-2: NPCPresetData УЖЕ имеет поля для экипировки и техник

```csharp
// NPCPresetData.cs — УЖЕ СУЩЕСТВУЕТ:
public List<KnownTechnique> knownTechniques;  // techniqueId, mastery, quickSlot
public List<EquippedItem> equipment;           // slot, itemId, grade, durabilityPercent
```

**Но эти поля НЕ ИСПОЛЬЗУЮТСЯ при инициализации NPC!** NPCController.InitializeFromGenerated() полностью игнорирует их.

**Следствие:** Фаза 6 (NPC экипировка) проще, чем предполагалось — нужно только использовать существующие данные.

### КН-3: NPCRuntimeSpawner НЕ добавляет EquipmentController

Спавнер добавляет: NPCController, NPCAI, BodyController, QiController, TechniqueController, NPCVisual, NPCInteractable, NPCMovement. НО НЕ EquipmentController.

**Следствие:** Фаза 6 требует добавить EquipmentController в спавнер и в NPCController.

### КН-4: CombatAI не проверяет экипировку при выборе техники

CombatAI.FindBestTechnique() фильтрует по дальности (melee/ranged), но НЕ проверяет:
- Есть ли оружие для MeleeWeapon техник
- Какая техника эффективнее с текущим оружием

### КН-5: TechniqueChargeSystem.CalculateChargeTime() НЕ учитывает экипировку

Формула: `effectiveTime = baseChargeTime / (1 + cultivationBonus) / (1 + masteryBonus)`
Нет множителя `chargeSpeedBonus` от оружия.

### КН-6: TechniqueController.CalculateQiCost() НЕ учитывает экипировку

Вызывает `TechniqueCapacity.CalculateQiCost()` напрямую, без `qiCostReduction` от оружия.

### КН-7: CombatUI.techniqueSlots[9] vs TechniqueController.maxQuickSlots=10

UI показывает 9 слотов (клавиши 1-9), но TechniqueController поддерживает 10. Слот 0 (или 10-й) не отображается. **Решение:** оставить 9 слотов в UI — это стандарт для RPG.

### КН-8: EquipmentData.statBonuses — УЖЕ поддерживает произвольные бонусы

EquipmentData уже имеет `List<StatBonus>` с гибкой системой бонусов. Вместо добавления новых полей (techniqueDamageBonus и др.), МОЖНО использовать customBonuses через существующую систему.

**Но:** EquipmentController.AddEquipmentStats() обрабатывает только конкретные имена (strength, agility и др.). customBonuses хранятся, но НЕТ методов для их получения в контексте техник.

**Решение:** Добавить выделенные поля в EquipmentData для ясности (techniqueDamageBonus, qiCostReduction, chargeSpeedBonus) + добавить методы-аксессоры в EquipmentController.

### КН-9: DamageCalculator Слой 1b (WeaponBonusDamage) УЖЕ РЕАЛИЗОВАН

```csharp
if (attacker.CombatSubtype == CombatSubtype.MeleeWeapon && attacker.WeaponBonusDamage > 0)
{
    damage += attacker.WeaponBonusDamage;
}
```

**НО:** WeaponBonusDamage всегда 0f, т.к. никто его не заполняет из EquipmentController. Нужно только подключить.

---

## 🔍 НОВЫЕ НАХОДКИ РАСШИРЕННОГО АУДИТА (2026-05-06)

> Дополнительный аудит 25 файлов: Combatant.cs, DefenseProcessor.cs, TechniqueCapacity.cs,
> CombatManager.cs, CombatTrigger.cs, QiBuffer.cs, ChargeState.cs, CombatEvents.cs,
> NPCAI.cs, EquipmentRuntimeSpawner.cs, WeaponGenerator.cs, BodyController.cs,
> CharacterPanelUI.cs, BodyDollPanel.cs, EquipmentData.cs, QiController.cs,
> SaveDataTypes.cs, LootGenerator.cs, NPCPresetData.cs

### НФ-1: CombatantBase.GetAttackerParams() НЕ поддерживает TechniqueDamageBonus

**Файл:** `Combatant.cs:282-299`

`GetAttackerParams()` создаёт `AttackerParams` с hardcoded:
- `IsQiTechnique = false`
- `CombatSubtype = MeleeStrike`
- `WeaponBonusDamage` — берётся из свойства, но **TechniqueDamageBonus НЕ существует в AttackerParams**

**Следствие для ФАЗЫ 2:** Нужно добавить поле `TechniqueDamageBonus` в `AttackerParams` + заполнить его в `CombatManager.ExecuteTechniqueAttack()`.

### НФ-2: DefenseProcessor УЖЕ готов к приёму бонусов оружия

**Файл:** `DefenseProcessor.cs:129-156`

Методы **уже принимают** параметры бонусов:
```csharp
CalculateDodgeChance(int agility, float armorDodgePenalty = 0f)
CalculateParryChance(int agility, float weaponParryBonus = 0f)
CalculateBlockChance(int strength, float shieldBlockBonus = 0f)
```

**Упрощение для ФАЗЫ 1:** DefenseProcessor менять НЕ нужно — только передать бонусы из EquipmentController в ICombatant-свойства.

### НФ-3: TechniqueCapacity.CalculateQiCost() — статический, без доступа к EquipmentController

**Файл:** `TechniqueCapacity.cs:131-135`

```csharp
public static long CalculateQiCost(long baseCapacity, int level)
{
    double levelMultiplier = Mathf.Pow(2, level - 1);
    return (long)(baseCapacity * levelMultiplier);
}
```

**Решение для ФАЗЫ 2:** `qiCostReduction` применяется В TechniqueController.CallCalculateQiCost() ПОСЛЕ вызова TechniqueCapacity, а не внутри TechniqueCapacity:
```csharp
long baseCost = TechniqueCapacity.CalculateQiCost(baseCapacity, level);
return (long)(baseCost * (1f - qiCostReduction));
```

### НФ-4: CombatManager.ExecuteTechniqueAttack() ОВЕРРАЙДИТ AttackerParams после GetAttackerParams()

**Файл:** `CombatManager.cs:337-342`

```csharp
AttackerParams attackerParams = attacker.GetAttackerParams(techResult.Element);
attackerParams.TechniqueLevel = technique.Data.techniqueLevel;
attackerParams.TechniqueGrade = technique.Data.grade;
attackerParams.IsUltimate = technique.Data.isUltimate;
attackerParams.IsQiTechnique = techResult.Type != TechniqueType.Cultivation;
attackerParams.CombatSubtype = technique.Data.combatSubtype;
```

**Следствие для ФАЗЫ 2:** `TechniqueDamageBonus` нужно установить ЗДЕСЬ, из EquipmentController атакующего:
```csharp
attackerParams.TechniqueDamageBonus = equipmentController?.GetTechniqueDamageBonus() ?? 0f;
```

### НФ-5: NPCAI.PerformAttack() — только BasicAttack, техники через CombatAI

**Файл:** `NPCAI.cs:748-782`

NPCAI.PerformAttack() вызывает `cm.ExecuteBasicAttack()` с `MeleeStrike` subtype. NPCAI НЕ использует техники напрямую — техники обрабатываются CombatAI (через CombatManager.ProcessAIActions).

**Следствие для ФАЗЫ 3:** Проверка MeleeWeapon нужна в CombatAI.FindBestTechnique(), НЕ в NPCAI.

### НФ-6: WeaponGenerator УЖЕ генерирует qiCostReduction и qiConductivity

**Файл:** `WeaponGenerator.cs:366-367`

```csharp
weapon.qiConductivity = rng.NextFloat(minCond, maxCond) * gradeEffMult;
weapon.qiCostReduction = weapon.grade >= EquipmentGrade.Refined ? (int)weapon.grade * 0.05f : 0f;
```

**Но:** `GeneratedWeapon` — DTO. При конвертации в `EquipmentData` эти поля НЕ переносятся (EquipmentData не имеет `qiCostReduction`).

**Следствие для ФАЗЫ 2:** Нужно добавить поля в EquipmentData + обновить маппинг GeneratedWeapon → EquipmentData в EquipmentSOFactory.

### НФ-7: EquipmentData НЕ имеет techniqueDamageBonus, qiCostReduction, chargeSpeedBonus

**Файл:** `EquipmentData.cs`

Текущие поля: damage, defense, coverage, damageReduction, dodgeBonus, moveSpeedPenalty, qiFlowPenalty, equippedSprite, materialId, materialTier, grade, itemLevel, statBonuses, specialEffects.

**Отсутствуют:** techniqueDamageBonus, qiCostReduction, chargeSpeedBonus.

**Подтверждает КН-8.**

### НФ-8: CharacterPanelUI УЖЕ ссылается на EquipmentController

**Файл:** `CharacterPanelUI.cs:35`

```csharp
[SerializeField] private EquipmentController equipmentController;
```

Находится через `playerController.GetComponent<EquipmentController>()` в `InitializeUI()`.
Подписан на события: `OnEquipmentEquipped`, `OnEquipmentUnequipped`, `OnStatsChanged`.

**Следствие:** ФАЗА 5 (техники в CharacterPanel) может использовать этот же паттерн.

### НФ-9: BodyDollPanel УЖЕ имеет WeaponMain и WeaponOff слоты

**Файл:** `BodyDollPanel.cs:249-250`

```csharp
[SerializeField] private DollSlotUI weaponMainSlot;
[SerializeField] private DollSlotUI weaponOffSlot;
```

Подписан на `equipmentController.OnWeaponOffBlockChanged` для блокировки 2H.

**Следствие:** WeaponMain/WeaponOff UI в CharacterPanel уже функционален. ФАЗА 4 — отдельный HUD-элемент в CombatUI (бой), а не в кукле.

### НФ-10: NPCPresetData.EquippedItem — достаточно для инициализации NPC

**Файл:** `NPCPresetData.cs:171-178`

```csharp
public class EquippedItem {
    public EquipmentSlot slot;
    public string itemId;
    public EquipmentGrade grade;
    public int durabilityPercent = 100;
}
```

**Подтверждает КН-2.** Данных достаточно для InitializeEquipmentFromPreset().

### НФ-11: SaveDataTypes НЕ имеет EquipmentSaveData

**Файл:** `SaveDataTypes.cs`

Нет классов для сохранения экипировки NPC или слотов техник. EquipmentController имеет customBonuses через CustomBonusEntry, но отдельного сохранения техник/экипировки нет.

**Следствие:** Не блокирует ФАЗЫ 1-6. Можно вынести в отдельную задачу. Но после ФАЗЫ 6 (NPC экипировка) нужно убедиться, что сохранение/загрузка корректно работает.

### НФ-12: LootGenerator НЕ дропает экипировку убитого NPC

**Файл:** `LootGenerator.cs:112-137`

Генерирует generic предметы (qi_core, element_essence, body_material, rare_item, epic_item), но НЕ предметы из экипировки убитого NPC.

**Следствие для ФАЗЫ 6:** После добавления экипировки NPC, LootGenerator нужно обновить — добавить шанс выпадения экипированных предметов убитого NPC. Это новая задача **ФАЗА 7**.

### НФ-13: QiController.Conductivity доступна, но НЕ модифицируется экипировкой

**Файл:** `QiController.cs:57`

```csharp
public float Conductivity => conductivity;
```

EquipmentData имеет `qiFlowPenalty` (-30%..+30%), но это поле НЕ влияет на QiController.Conductivity.

**Следствие для ФАЗЫ 2:** qiFlowPenalty нужно применять через EquipmentController → QiController. Варианты:
- A) EquipmentController вызывает `qiController.SetConductivityBonus(qiFlowBonus)` при смене экипировки
- B) TechniqueChargeSystem получает conductivity из EquipmentController вместо QiController

**Решение:** Вариант A чище — QiController уже имеет `SetConductivityBonus()`.

### НФ-14: CombatTrigger НЕ проверяет экипировку

**Файл:** `CombatTrigger.cs:62-100`

Триггер проверяет Attitude и теги, но не экипировку. Это корректно — экипировка не должна влиять на агро-радиус.

**Изменений НЕ требуется.**

### НФ-15: BodyController НЕ связан с EquipmentController

**Файл:** `BodyController.cs`

BodyController управляет частями тела и уроном, но не ссылается на EquipmentController. Это правильно — бонусы брони применяются ДО BodyController (в ICombatant свойствах), через DamageCalculator → DefenseProcessor.

**Изменений НЕ требуется.**

### НФ-16: CombatEvents НЕ имеет события для MeleeWeaponUnavailable

**Файл:** `CombatEvents.cs`

CombatEventType не включает события для «техника недоступна из-за отсутствия оружия».

**Следствие для ФАЗЫ 3:** При попытке использовать MeleeWeapon без оружия, CombatUI должен показывать причину локально (в TechniqueSlotUI), без глобального события.

---

## Ключевые TODO в коде (подтверждённые)

1. **CombatantBase.cs:177** — `WeaponParryBonus => 0f; // TODO: из EquipmentController`
2. **CombatantBase.cs:178** — `ShieldBlockBonus => 0f; // TODO: из EquipmentController`
3. **CombatantBase.cs:182** — `WeaponBonusDamage => 0f; // TODO: из EquipmentController`
4. **CombatantBase.cs:171** — `ArmorCoverage => 0f` (нет связи с EquipmentController)
5. **PlayerController.cs:172** — `WeaponBonusDamage = 0f // ФАЗА 7: TODO из EquipmentController`
6. **PlayerController.cs:188** — `FormationBuffMultiplier = 1.0f // ФАЗА 7: TODO из FormationSystem`
7. **PlayerController.cs:116-119** — `Strength/Agility/Intelligence/Vitality = 10 // TODO: from StatDevelopment`
8. **NPCController.cs:213** — `WeaponBonusDamage = 0f // ФАЗА 7: TODO из EquipmentController`
9. **NPCController.cs:234** — `FormationBuffMultiplier = 1.0f // ФАЗА 7: TODO из FormationSystem`
10. **PlayerController.cs:753** — `toolMultiplier = 1.0f // TODO: из EquipmentController`
11. **CombatantBase.cs:148** — `DodgeChance: penalty = 0f // TODO: Get from equipped armor`
12. **CombatantBase.cs:157** — `ParryChance: bonus = 0f // TODO: Get from equipped weapon`
13. **CombatantBase.cs:165** — `BlockChance: bonus = 0f // TODO: Get from equipped shield`
14. **CombatantBase.cs:319** — `DodgePenalty = 0f // TODO: из equipped armor`

---

## ПЛАН РЕАЛИЗАЦИИ (ОБНОВЛЁННЫЙ v2)

### ═══════════════════════════════════════════
### ФАЗА 1: Связь EquipmentController → Боевая система
### ═══════════════════════════════════════════

**Цель:** WeaponMain/WeaponOff слоты реально влияют на бой через ICombatant.

**⚠️ КРИТИЧЕСКОЕ РЕШЕНИЕ:** НЕ рефакторить наследование. Добавить EquipmentController напрямую в PlayerController и NPCController.

**✅ УПРОЩЕНИЕ (НФ-2):** DefenseProcessor менять НЕ нужно — методы уже принимают бонусы.

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `PlayerController.cs` | Добавить `[SerializeField] EquipmentController equipmentController` |
| `PlayerController.cs` | `InitializeComponents()` → `equipmentController = GetComponent<EquipmentController>()` |
| `PlayerController.cs` | `ICombatant.WeaponBonusDamage` → из `equipmentController?.GetMainWeapon()?.equipmentData.damage` × durMult |
| `PlayerController.cs` | `ICombatant.ParryChance` → `DefenseProcessor.CalculateParryChance(agility, parryBonus)` с bonus из оружия |
| `PlayerController.cs` | `ICombatant.BlockChance` → `DefenseProcessor.CalculateBlockChance(strength, blockBonus)` с bonus из щита |
| `PlayerController.cs` | `ICombatant.ArmorCoverage` → средний coverage от надетой брони |
| `PlayerController.cs` | `ICombatat.DamageReduction` → `equipmentController.CurrentStats.damageReduction` |
| `PlayerController.cs` | `ICombatat.ArmorValue` → `equipmentController.CurrentStats.totalDefense` |
| `PlayerController.cs` | `ICombatat.DodgeChance` → `DefenseProcessor.CalculateDodgeChance(agility, dodgePenalty)` из брони |
| `PlayerController.cs` | `AttemptHarvestPhysics()` → `toolMultiplier` из EquipmentController |
| `NPCController.cs` | Добавить `[SerializeField] EquipmentController equipmentController` |
| `NPCController.cs` | Аналогичные делегирования ICombatant (WeaponBonusDamage, ArmorValue, и др.) |
| `CombatantBase.cs` | Обновить комментарии TODO → «реализовано через EquipmentController в PlayerController/NPCController» |

**Логика WeaponBonusDamage:**
```
WeaponBonusDamage = equipmentController?.GetMainWeapon()?.equipmentData.damage
                    × GetDurabilityMultiplier(grade) ?? 0f
```

**Логика защитных бонусов (НФ-2 подтверждает — DefenseProcessor менять НЕ нужно):**
```
ParryBonus → из EquipmentData.statBonuses где statName = "parryBonus"
BlockBonus → из EquipmentData.statBonuses где statName = "blockBonus"
DodgePenalty → из брони: EquipmentData.dodgeBonus (отрицательное = штраф)
```

**Логика брони:**
```
ArmorCoverage → среднее покрытие от всех надетых предметов брони (не оружия)
DamageReduction → equipmentController.CurrentStats.damageReduction
ArmorValue → equipmentController.CurrentStats.totalDefense
```

---

### ═══════════════════════════════════════════
### ФАЗА 2: Оружие → Влияние на техники
### ═══════════════════════════════════════════

**Цель:** Оружие влияет на урон и скорость накачки техник.

**Новые поля в EquipmentData (НФ-7 подтверждает отсутствие):**
- `techniqueDamageBonus` — % бонус к урону техник (0% для обычного, 5-50% для магического)
- `qiCostReduction` — % снижения стоимости Ци техник (0-30%)
- `chargeSpeedBonus` — % ускорения накачки (0-25%)

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `EquipmentData.cs` | Добавить `[Header("Technique Bonuses")]` + 3 поля |
| `EquipmentController.cs` | Добавить методы `GetTechniqueDamageBonus()`, `GetQiCostReduction()`, `GetChargeSpeedBonus()` |
| `EquipmentStats` | Добавить `techniqueDamageBonus`, `qiCostReduction`, `chargeSpeedBonus` + включить в RecalculateStats |
| `DamageCalculator.cs` | Слой 1c: TechniqueDamageBonus в AttackerParams + CalculateDamage |
| `AttackerParams` | Добавить поле `float TechniqueDamageBonus` |
| `CombatManager.cs` | `ExecuteTechniqueAttack()` — установить `attackerParams.TechniqueDamageBonus` из EquipmentController (НФ-4) |
| `TechniqueChargeSystem.cs` | `CalculateChargeTime()` — учитывать `chargeSpeedBonus`: `effectiveTime /= (1 + chargeSpeedBonus)` |
| `TechniqueController.cs` | `CalculateQiCost()` — учитывать `qiCostReduction` ПОСЛЕ TechniqueCapacity (НФ-3): `qiCost × (1 - qiCostReduction)` |
| `EquipmentSOFactory.cs` | Маппинг GeneratedWeapon.qiCostReduction → EquipmentData.qiCostReduction (НФ-6) |
| `QiController.cs` | qiFlowPenalty из EquipmentController → `qiController.SetConductivityBonus()` при смене экипировки (НФ-13) |

**⚠️ ПРОБЛЕМА:** TechniqueChargeSystem и TechniqueController не имеют ссылки на EquipmentController.

**Решение:** Добавить метод `SetEquipmentController(EquipmentController)` который вызывается из PlayerController/NPCController при инициализации.

**Логика TechniqueDamageBonus в DamageCalculator:**
```
// Слой 1c: Бонус урона техник от оружия (после слоя 1b)
if (attacker.IsQiTechnique && attacker.TechniqueDamageBonus > 0)
{
    damage += damage * attacker.TechniqueDamageBonus;
}
```

**Логика qiCostReduction (НФ-3):**
```csharp
// В TechniqueController.CalculateQiCost():
long baseCost = TechniqueCapacity.CalculateQiCost(baseCapacity, level);
float reduction = equipmentController?.GetQiCostReduction() ?? 0f;
return (long)(baseCost * (1f - reduction));
```

**Логика chargeSpeedBonus:**
```csharp
// В TechniqueChargeSystem.CalculateChargeTime():
float chargeBonus = equipmentController?.GetChargeSpeedBonus() ?? 0f;
effectiveTime /= (1f + chargeBonus);
```

**Логика qiFlowPenalty → QiController (НФ-13):**
```csharp
// В EquipmentController.OnEquipmentChanged():
float totalFlowBonus = CurrentStats.qiFlowBonus; // суммарный из всех предметов
qiController?.SetConductivityBonus(totalFlowBonus);
```

---

### ═══════════════════════════════════════════
### ФАЗА 3: MeleeWeapon → Требует оружие
### ═══════════════════════════════════════════

**Цель:** Техника melee_weapon не работает без оружия. Fallback → melee_strike.

**⚠️ УТОЧНЕНИЕ (НФ-5):** Проверка MeleeWeapon нужна в CombatAI.FindBestTechnique(), НЕ в NPCAI.PerformAttack().

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `TechniqueController.cs` | `CanUseTechnique()` — проверка наличия оружия для MeleeWeapon |
| `TechniqueChargeSystem.cs` | `BeginCharge()` — проверка оружия для MeleeWeapon (через EquipmentController) |
| `CombatAI.cs` | `FindBestTechnique()` — пропуск MeleeWeapon техник если нет оружия (НФ-5) |
| `CombatUI.cs` | Слот техники: если MeleeWeapon без оружия → Unavailable с причиной «Требуется оружие» (НФ-16) |

**Логика проверки:**
```csharp
// В TechniqueController.CanUseTechnique():
if (technique.Data.combatSubtype == CombatSubtype.MeleeWeapon)
{
    if (equipmentController == null || equipmentController.GetMainWeapon() == null)
    {
        return false; // Нет оружия — нельзя использовать
    }
}
```

**⚠️ ПРОБЛЕМА:** TechniqueController не имеет ссылки на EquipmentController.

**Решение:** Добавить `private EquipmentController equipmentController` + метод `SetEquipmentController()`. Вызывается из PlayerController/NPCController при инициализации.

---

### ═══════════════════════════════════════════
### ФАЗА 4: UI — Оружие в боевом интерфейсе
### ═══════════════════════════════════════════

**Цель:** Игрок видит экипированное оружие в CombatUI.

**⚠️ УТОЧНЕНИЕ (НФ-9):** BodyDollPanel УЖЕ показывает WeaponMain/WeaponOff в CharacterPanel. ФАЗА 4 — отдельный HUD-элемент в CombatUI (бой), не дублирующий куклу.

**Новые элементы CombatUI:**

| Элемент | Описание |
|---------|----------|
| `weaponMainIcon` | Image — иконка оружия в основной руке |
| `weaponOffIcon` | Image — иконка оружия/щита во второй руке |
| `weaponMainText` | TMP_Text — название + урон |
| `weaponOffText` | TMP_Text — название + защита |
| `weaponPanel` | GameObject — панель оружия (показывается в бою) |

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `CombatUI.cs` | Добавить `[Header("Weapon Display")]` + UI-ссылки |
| `CombatUI.cs` | Метод `UpdateWeaponDisplay(EquipmentController eqCtrl)` |
| `CombatUI.cs` | Вызов `UpdateWeaponDisplay()` в `ShowCombatUI()` |

---

### ═══════════════════════════════════════════
### ФАЗА 5: UI — Назначение техник в слоты
### ═══════════════════════════════════════════

**Цель:** Игрок может назначать изученные техники в quickslots.

**⚠️ УТОЧНЕНИЕ (НФ-8):** CharacterPanelUI УЖЕ держит EquipmentController. Паттерн тот же для TechniqueController.

**Новый UI-компонент:** `TechniqueAssignmentPanel`

| Элемент | Описание |
|---------|----------|
| Список изученных техник | ScrollRect с TechniqueAssignmentEntry |
| QuickSlots 0-8 | 9 слотов для назначения (соотв. клавишам 1-9) |
| Click-to-Assign | Нажатие на технику → выбор слота → назначение |

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| Новый: `Scripts/UI/TechniqueAssignmentUI.cs` | Панель назначения техник |
| `CharacterPanelUI.cs` | Вкладка «Техники» → открывает TechniqueAssignmentUI |
| `TechniqueController.cs` | Поддержка переназначения (уже есть AssignToQuickSlot) |
| `CombatUI.cs` | InitializeTechniqueSlots — при изменении слотов |

---

### ═══════════════════════════════════════════
### ФАЗА 6: NPC — Экипировка и техники
### ═══════════════════════════════════════════

**Цель:** NPC получают оружие и техники из NPCPresetData.

**⚠️ КЛЮЧЕВАЯ НАХОДКА:** NPCPresetData УЖЕ имеет `equipment` и `knownTechniques`. Нужно только использовать их при инициализации.

**⚠️ НОВАЯ ЗАДАЧА (НФ-12):** LootGenerator нужно обновить — шанс выпадения экипированных предметов убитого NPC.

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `NPCController.cs` | Добавить `EquipmentController equipmentController` + InitializeEquipmentFromPreset() |
| `NPCController.cs` | InitializeFromGenerated() → вызывать InitializeEquipmentFromPreset() + InitializeTechniquesFromPreset() |
| `NPCRuntimeSpawner.cs` | Добавить `go.AddComponent<EquipmentController>()` в цепочку компонентов |
| `CombatAI.cs` | Учитывать экипировку при принятии решений (MeleeWeapon без оружия → пропуск) |
| `LootGenerator.cs` | Добавить шанс выпадения экипировки убитого NPC (НФ-12) |

**Логика инициализации техник NPC:**
```csharp
private void InitializeTechniquesFromPreset()
{
    if (preset?.knownTechniques == null || techniqueController == null) return;
    
    foreach (var known in preset.knownTechniques)
    {
        var techData = LoadTechniqueData(known.techniqueId);
        if (techData != null)
        {
            techniqueController.LearnTechnique(techData, known.mastery);
            if (known.quickSlot >= 0)
                techniqueController.AssignToQuickSlot(
                    techniqueController.GetTechnique(known.techniqueId), 
                    known.quickSlot);
        }
    }
}
```

**Логика выпадения экипировки NPC (НФ-12):**
```csharp
// В LootGenerator.GenerateLoot() — добавить:
if (defeated is NPCController npc && npc.equipmentController != null)
{
    foreach (var slot in equippedSlots)
    {
        if (RollChance(EQUIPMENT_DROP_RATE)) // ~30%
        {
            var item = npc.equipmentController.GetEquipment(slot);
            result.Items.Add(new LootEntry(item.equipmentData.itemId, 1, item.equipmentData.rarity));
        }
    }
}
```

---

### ═══════════════════════════════════════════
### ФАЗА 7 (НОВАЯ): Сохранение экипировки NPC
### ═══════════════════════════════════════════

**Цель:** Экипировка и техники NPC сохраняются/загружаются корректно.

**⚠️ НАХОДКА (НФ-11):** SaveDataTypes не имеет EquipmentSaveData.

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `SaveDataTypes.cs` | Добавить `NPCEquipmentSaveData`, `NPCTechniqueSlotSaveData` |
| `NPCController.cs` | Методы `GetSaveData()` / `LoadSaveData()` для экипировки и техник |

---

## Сводная таблица файлов

### Новые файлы (1)
| Файл | Фаза | Назначение |
|------|-------|------------|
| `Scripts/UI/TechniqueAssignmentUI.cs` | 5 | Панель назначения техник в слоты |

### Изменяемые файлы (16)
| Файл | Фазы | Изменения |
|------|-------|-----------|
| `PlayerController.cs` | 1 | +EquipmentController, делегирование ICombatat (WeaponBonus, Armor, Dodge), toolMultiplier |
| `NPCController.cs` | 1, 6 | +EquipmentController, делегирование ICombatat, InitializeEquipmentFromPreset/TechniquesFromPreset |
| `CombatantBase.cs` | 1 | Обновить TODO комментарии (не наследуется) |
| `EquipmentData.cs` | 2 | +techniqueDamageBonus, qiCostReduction, chargeSpeedBonus |
| `EquipmentController.cs` | 2 | +GetTechniqueDamageBonus(), GetQiCostReduction(), GetChargeSpeedBonus() |
| `EquipmentStats` | 2 | +techniqueDamageBonus, qiCostReduction, chargeSpeedBonus в RecalculateStats |
| `DamageCalculator.cs` | 2 | +Слой 1c: TechniqueDamageBonus в AttackerParams + CalculateDamage |
| `AttackerParams` | 2 | +TechniqueDamageBonus поле |
| `CombatManager.cs` | 2 | +Установить TechniqueDamageBonus из EquipmentController (НФ-4) |
| `TechniqueChargeSystem.cs` | 2, 3 | +chargeSpeedBonus в CalculateChargeTime, +EquipmentController, MeleeWeapon проверка |
| `TechniqueController.cs` | 2, 3 | +qiCostReduction ПОСЛЕ TechniqueCapacity (НФ-3), +EquipmentController, MeleeWeapon fallback |
| `QiController.cs` | 2 | +qiFlowPenalty из EquipmentController → SetConductivityBonus (НФ-13) |
| `EquipmentSOFactory.cs` | 2 | +маппинг qiCostReduction из GeneratedWeapon (НФ-6) |
| `CombatUI.cs` | 3, 4 | +Weapon Display панель, MeleeWeapon unavailable state |
| `NPCRuntimeSpawner.cs` | 6 | +EquipmentController в цепочку компонентов |
| `CombatAI.cs` | 3, 6 | Пропуск MeleeWeapon без оружия, учёт экипировки |
| `CharacterPanelUI.cs` | 5 | Вкладка «Техники» → TechniqueAssignmentUI |
| `LootGenerator.cs` | 6 | +шанс выпадения экипировки убитого NPC (НФ-12) |
| `SaveDataTypes.cs` | 7 | +NPCEquipmentSaveData, NPCTechniqueSlotSaveData (НФ-11) |

---

## Архитектурные решения

### АД-1: Без рефакторинга наследования
PlayerController и NPCController остаются с прямым ICombatant. EquipmentController добавляется как [SerializeField] поле в каждый. Это безопаснее, чем рефакторинг к CombatantBase.

### АД-2: EquipmentController передаётся через SetEquipmentController()
TechniqueController и TechniqueChargeSystem получают EquipmentController через инъекцию (метод SetEquipmentController), а не через GetComponent. Это позволяет NPC не иметь EquipmentController и работать без него.

### АД-3: TechniqueDamageBonus — отдельный слой 1c в DamageCalculator
Не расширяем слой 1b (WeaponBonusDamage), а добавляем новый слой 1c для техник. Это чистое разделение: 1b = урон оружия для MeleeWeapon, 1c = бонус оружия к Ци-техникам.

### АД-4: NPCPresetData.equipment используется при инициализации
Уже существующие поля KnownTechnique и EquippedItem в NPCPresetData используются для начальной экипировки NPC.

### АД-5: qiFlowPenalty → QiController.SetConductivityBonus() (НФ-13)
При смене экипировки EquipmentController вызывает qiController.SetConductivityBonus(totalFlowBonus). Используется существующий метод QiController, а не создание нового канала.

### АД-6: qiCostReduction применяется ПОСЛЕ TechniqueCapacity (НФ-3)
TechniqueCapacity.CalculateQiCost() остаётся чистым статическим методом. qiCostReduction применяется в TechniqueController поверх результата.

---

## Порядок реализации

1. ✅ **ФАЗА 1** — связь Equipment → Бой (самое критичное, ломает TODO)
2. ✅ **ФАЗА 2** — оружие влияет на техники (+QiController, +EquipmentSOFactory, +CombatManager)
3. ✅ **ФАЗА 3** — MeleeWeapon требует оружие (проверка в CombatAI, не NPCAI)
4. ✅ **ФАЗА 4** — оружие в CombatUI (отдельный HUD, не BodyDollPanel)
5. ✅ **ФАЗА 5** — назначение техник в слоты (UI)
6. ✅ **ФАЗА 6** — NPC экипировка и техники (+LootGenerator обновление)
7. ✅ **ФАЗА 7** — сохранение экипировки NPC (SaveDataTypes)

---

## Риски

| Риск | Вероятность | Влияние | Митигация |
|------|-------------|---------|-----------|
| ICombatat свойства в PlayerController/NPCController рассинхронизируются | Средняя | Среднее | Общий helper-метод GetEquipmentBonuses() |
| TechniqueController + EquipmentController образуют circular dependency | Низкая | Высокое | Однонаправленная инъекция: TC → EquipmentController (не наоборот) |
| NPCPresetData.equipment itemId не найден в базе | Средняя | Низкое | Логирование + fallback (голые кулаки) |
| CombatUI.techniqueSlots[9] vs maxQuickSlots=10 | Низкая | Низкое | Оставить 9 слотов UI, клавиши 1-9 |
| GeneratedWeapon.qiCostReduction не маппится в EquipmentData | Высокая | Среднее | Обновить EquipmentSOFactory маппинг (НФ-6) |
| QiController.SetConductivityBonus вызывается до QiController.Awake | Низкая | Среднее | EquipmentController подписан на OnEquipmentChanged — вызов после инициализации |
| LootGenerator дропает экипировку NPC, которая не существует в ItemDB | Средняя | Низкое | Проверка equipmentData != null перед добавлением в LootResult |

---

## Следующие шаги

1. Подтверждение плана пользователем
2. Реализация по фазам
3. Git push после каждой фазы

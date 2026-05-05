# Чекпоинт: Слот оружия и слоты техник

**Дата:** 2026-05-05 06:49:16 UTC
**Редактировано:** 2026-05-05 09:30:00 UTC — глубокий аудит, критические находки, обновлённый план
**Статус:** in_progress — полный аудит завершён, план обновлён

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

---

## ПЛАН РЕАЛИЗАЦИИ (ОБНОВЛЁННЫЙ)

### ═══════════════════════════════════════════
### ФАЗА 1: Связь EquipmentController → Боевая система
### ═══════════════════════════════════════════

**Цель:** WeaponMain/WeaponOff слоты реально влияют на бой через ICombatant.

**⚠️ КРИТИЧЕСКОЕ РЕШЕНИЕ:** НЕ рефакторить наследование. Добавить EquipmentController напрямую в PlayerController и NPCController.

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `PlayerController.cs` | Добавить `[SerializeField] EquipmentController equipmentController` |
| `PlayerController.cs` | `ICombatant.WeaponBonusDamage` → `equipmentController?.CurrentStats.totalDamage × gradeMult` |
| `PlayerController.cs` | `ICombatant.ParryChance` → `DefenseProcessor.CalculateParryChance(agility, parryBonus)` с `parryBonus` из оружия |
| `PlayerController.cs` | `ICombatant.BlockChance` → `DefenseProcessor.CalculateBlockChance(strength, blockBonus)` с `blockBonus` из щита |
| `PlayerController.cs` | `ICombatant.ArmorCoverage` → `equipmentController?.CurrentStats` → средний coverage брони |
| `PlayerController.cs` | `ICombatant.DamageReduction` → `equipmentController?.CurrentStats.damageReduction` |
| `PlayerController.cs` | `ICombatant.ArmorValue` → `equipmentController?.CurrentStats.totalDefense` |
| `PlayerController.cs` | `ICombatant.DodgeChance` → `DefenseProcessor.CalculateDodgeChance(agility, dodgePenalty)` с `dodgePenalty` из брони |
| `PlayerController.cs` | `AttemptHarvestPhysics()` → `toolMultiplier` из EquipmentController |
| `PlayerController.cs` | `InitializeComponents()` → `equipmentController = GetComponent<EquipmentController>()` |
| `NPCController.cs` | Добавить `[SerializeField] EquipmentController equipmentController` |
| `NPCController.cs` | Аналогичные делегирования ICombatant (WeaponBonusDamage, ArmorValue, и др.) |
| `CombatantBase.cs` | Обновить комментарии TODO → «реализовано через EquipmentController в PlayerController/NPCController» |

**Логика WeaponBonusDamage:**
```
WeaponBonusDamage = equipmentController?.GetMainWeapon()?.equipmentData.damage
                    × GetDurabilityMultiplier(grade) ?? 0f
```

**Логика защитных бонусов:**
```
ParryBonus → из EquipmentData.statBonuses где statName = "parryBonus"
BlockBonus → из EquipmentData.statBonuses где statName = "blockBonus"
DodgePenalty → из брони: EquipmentData.dodgeBonus (отрицательное значение = штраф)
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

**Новые поля в EquipmentData:**
- `techniqueDamageBonus` — % бонус к урону техник (0% для обычного, 5-50% для магического)
- `qiCostReduction` — % снижения стоимости Ци техник (0-30%)
- `chargeSpeedBonus` — % ускорения накачки (0-25%)

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `EquipmentData.cs` | Добавить `[Header("Technique Bonuses")]` + 3 поля |
| `EquipmentController.cs` | Добавить методы `GetTechniqueDamageBonus()`, `GetQiCostReduction()`, `GetChargeSpeedBonus()` |
| `EquipmentStats` | Добавить `techniqueDamageBonus`, `qiCostReduction`, `chargeSpeedBonus` + включить в RecalculateStats |
| `DamageCalculator.cs` | Слой: TechniqueDamageBonus из EquipmentController (ПОСЛЕ слоя 1b) |
| `TechniqueChargeSystem.cs` | `CalculateChargeTime()` — учитывать `chargeSpeedBonus`: `effectiveTime /= (1 + chargeSpeedBonus)` |
| `TechniqueController.cs` | `CalculateQiCost()` — учитывать `qiCostReduction`: `qiCost × (1 - qiCostReduction)` |

**⚠️ ПРОБЛЕМА:** TechniqueChargeSystem и TechniqueController не имеют ссылки на EquipmentController.

**Решение:** Добавить в TechniqueChargeSystem метод `SetEquipmentController(EquipmentController)` который вызывается из PlayerController/NPCController при инициализации.

**Логика TechniqueDamageBonus в DamageCalculator:**
```
// Слой 1c: Бонус урона техник от оружия (после слоя 1b)
if (attacker.IsQiTechnique && attacker.TechniqueDamageBonus > 0)
{
    damage += damage * attacker.TechniqueDamageBonus;
}
```

**Требуется добавить в AttackerParams:**
- `float TechniqueDamageBonus` — бонус от оружия к урону техник

---

### ═══════════════════════════════════════════
### ФАЗА 3: MeleeWeapon → Требует оружие
### ═══════════════════════════════════════════

**Цель:** Техника melee_weapon не работает без оружия. Fallback → melee_strike.

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `TechniqueController.cs` | `CanUseTechnique()` — проверка наличия оружия для MeleeWeapon |
| `TechniqueChargeSystem.cs` | `BeginCharge()` — проверка оружия для MeleeWeapon (через EquipmentController) |
| `CombatAI.cs` | `FindBestTechnique()` — пропуск MeleeWeapon техник если нет оружия |
| `CombatUI.cs` | Слот техники: если MeleeWeapon без оружия → Unavailable с причиной «Требуется оружие» |

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

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `NPCController.cs` | Добавить `EquipmentController equipmentController` + InitializeEquipmentFromPreset() |
| `NPCController.cs` | InitializeFromGenerated() → вызывать InitializeEquipmentFromPreset() + InitializeTechniquesFromPreset() |
| `NPCRuntimeSpawner.cs` | Добавить `go.AddComponent<EquipmentController>()` в цепочку компонентов |
| `CombatAI.cs` | Учитывать экипировку при принятии решений (MeleeWeapon без оружия → пропуск) |

**Логика инициализации техник NPC:**
```csharp
private void InitializeTechniquesFromPreset()
{
    if (preset?.knownTechniques == null || techniqueController == null) return;
    
    foreach (var known in preset.knownTechniques)
    {
        // Загрузить TechniqueData по techniqueId
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

---

## Сводная таблица файлов

### Новые файлы (1)
| Файл | Фаза | Назначение |
|------|-------|------------|
| `Scripts/UI/TechniqueAssignmentUI.cs` | 5 | Панель назначения техник в слоты |

### Изменяемые файлы (13)
| Файл | Фазы | Изменения |
|------|-------|-----------|
| `PlayerController.cs` | 1 | +EquipmentController, делегирование ICombatant (WeaponBonus, Armor, Dodge), toolMultiplier |
| `NPCController.cs` | 1, 6 | +EquipmentController, делегирование ICombatant, InitializeEquipmentFromPreset/TechniquesFromPreset |
| `CombatantBase.cs` | 1 | Обновить TODO комментарии (не наследуется) |
| `EquipmentData.cs` | 2 | +techniqueDamageBonus, qiCostReduction, chargeSpeedBonus |
| `EquipmentController.cs` | 2 | +GetTechniqueDamageBonus(), GetQiCostReduction(), GetChargeSpeedBonus() |
| `EquipmentStats` | 2 | +techniqueDamageBonus, qiCostReduction, chargeSpeedBonus в RecalculateStats |
| `DamageCalculator.cs` | 2 | +Слой 1c: TechniqueDamageBonus в AttackerParams + CalculateDamage |
| `AttackerParams` | 2 | +TechniqueDamageBonus поле |
| `TechniqueChargeSystem.cs` | 2, 3 | +chargeSpeedBonus в CalculateChargeTime, +EquipmentController, MeleeWeapon проверка |
| `TechniqueController.cs` | 2, 3 | +qiCostReduction в CalculateQiCost, +EquipmentController, MeleeWeapon fallback |
| `CombatUI.cs` | 3, 4 | +Weapon Display панель, MeleeWeapon unavailable state |
| `NPCRuntimeSpawner.cs` | 6 | +EquipmentController в цепочку компонентов |
| `CombatAI.cs` | 3, 6 | Пропуск MeleeWeapon без оружия, учёт экипировки |
| `CharacterPanelUI.cs` | 5 | Вкладка «Техники» → TechniqueAssignmentUI |

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

---

## Порядок реализации

1. ✅ **ФАЗА 1** — связь Equipment → Бой (самое критичное, ломает TODO)
2. ✅ **ФАЗА 2** — оружие влияет на техники
3. ✅ **ФАЗА 3** — MeleeWeapon требует оружие
4. ✅ **ФАЗА 4** — оружие в CombatUI
5. ✅ **ФАЗА 5** — назначение техник в слоты (UI)
6. ✅ **ФАЗА 6** — NPC экипировка и техники

---

## Риски

| Риск | Вероятность | Влияние | Митигация |
|------|-------------|---------|-----------|
| ICombatant свойства в PlayerController/NPCController рассинхронизируются | Средняя | Среднее | Общий helper-метод GetEquipmentBonuses() |
| TechniqueController + EquipmentController образуют circular dependency | Низкая | Высокое | Однонаправленная инъекция: TC → EquipmentController (не наоборот) |
| NPCPresetData.equipment itemId не найден в базе | Средняя | Низкое | Логирование + fallback (голые кулаки) |
| CombatUI.techniqueSlots[9] vs maxQuickSlots=10 | Низкая | Низкое | Оставить 9 слотов UI, клавиши 1-9 |

---

## Следующие шаги

1. Подтверждение плана пользователем
2. Реализация по фазам
3. Git push после каждой фазы

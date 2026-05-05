# Чекпоинт: Слот оружия и слоты техник

**Дата:** 2026-05-05 06:49:16 UTC
**Статус:** in_progress — аудит завершён, план составлен

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
| **Связь оружия ↔ бой** | ❌ ОТСУТСТВУЕТ | PlayerController.GetAttackerParams() = hardcoded `WeaponBonusDamage = 0f` |
| **WeaponBonusDamage из EquipmentController** | ❌ ОТСУТСТВУЕТ | CombatantBase.WeaponBonusDamage = 0f (virtual, TODO) |
| **ParryBonus из оружия** | ❌ ОТСУТСТВУЕТ | `ParryChance` использует `0f` бонус |
| **BlockBonus из щита** | ❌ ОТСУТСТВУЕТ | `BlockChance` использует `0f` бонус |
| **ArmorValue из экипировки** | ❌ ОТСУТСТВУЕТ | `ArmorValue = 0` |
| **DamageReduction из экипировки** | ❌ ОТСУТСТВУЕТ | `DamageReduction = 0f` |
| **ToolMultiplier для добычи** | ❌ ОТСУТСТВУЕТ | `toolMultiplier = 1.0f` hardcoded |
| **EquipmentController на Player** | ❓ НЕ ПРОВЕРЕНО | PlayerController не имеет [SerializeField] EquipmentController |
| **EquipmentController на NPC** | ❓ НЕ ПРОВЕРЕНО | NPCController не имеет ссылки |

### Что есть — Слоты техник (TechniqueController)

| Аспект | Статус | Комментарий |
|--------|--------|-------------|
| `LearnedTechnique` struct | ✅ Существует | Data, Mastery, QuickSlot, CooldownRemaining |
| `quickSlots[]` (max=10) | ✅ Существует | Массив слотов быстрого доступа |
| `AssignToQuickSlot()` | ✅ Существует | Назначение техники в слот |
| `GetQuickSlotTechnique()` | ✅ Существует | Получение техники из слота |
| `UseQuickSlot()` | ✅ Существует | Использование через накачку |
| `CombatUI.techniqueSlots[9]` | ✅ Существует | UI слоты техник |
| `TechniqueSlotUI` компонент | ✅ Существует | Иконка, номер, кулдаун, накачка |
| **Назначение техник в слоты (UI)** | ❌ ОТСУТСТВУЕТ | Нет интерфейса drag-drop или окна назначения |
| **Отображение оружия в CombatUI** | ❌ ОТСУТСТВУЕТ | Нет иконки оружия / информации о вооружении |
| **MeleeWeapon → требует оружие** | ❌ ОТСУТСТВУЕТ | Нет проверки: melee_weapon без оружия = fallback к melee_strike |
| **Оружие влияет на технику** | ❌ ОТСУТСТВУЕТ | Нет связи: оружие → бонус урона / скорость накачки |

### Ключевые TODO в коде

1. **CombatantBase.cs:177** — `WeaponParryBonus => 0f; // TODO: из EquipmentController`
2. **CombatantBase.cs:178** — `ShieldBlockBonus => 0f; // TODO: из EquipmentController`
3. **CombatantBase.cs:182** — `WeaponBonusDamage => 0f; // TODO: из EquipmentController`
4. **CombatantBase.cs:171** — `ArmorCoverage => 0f` (нет связи с EquipmentController)
5. **PlayerController.cs:172** — `WeaponBonusDamage = 0f // ФАЗА 7: TODO из EquipmentController`
6. **PlayerController.cs:188** — `FormationBuffMultiplier = 1.0f // ФАЗА 7: TODO из FormationSystem`
7. **PlayerController.cs:116-119** — `Strength/Agility/Intelligence/Vitality = 10 // TODO: from StatDevelopment`

---

## ПЛАН РЕАЛИЗАЦИИ

### ═══════════════════════════════════════════
### ФАЗА 1: Связь EquipmentController → Боевая система
### ═══════════════════════════════════════════

**Цель:** WeaponMain/WeaponOff слоты реально влияют на бой.

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `PlayerController.cs` | Добавить `[SerializeField] EquipmentController equipmentController` |
| `NPCController.cs` | Добавить `[SerializeField] EquipmentController equipmentController` |
| `PlayerController.cs` | `ICombatant` свойства → делегировать к EquipmentController |
| `CombatantBase.cs` | Добавить виртуальный метод `GetEquipmentController()` |
| `CombatantBase.cs` | Переопределить `WeaponBonusDamage`, `WeaponParryBonus`, `ShieldBlockBonus` |
| `CombatantBase.cs` | Переопределить `ArmorValue`, `DamageReduction`, `ArmorCoverage` |

**Логика:**
```
WeaponBonusDamage = equipmentController.GetMainWeapon()?.equipmentData.damage × gradeMult
WeaponParryBonus = оружие в WeaponMain → parryBonus (из EquipmentData.statBonuses)
ShieldBlockBonus = щит в WeaponOff → blockBonus
ArmorValue = суммарное defense от всех слотов брони
DamageReduction = суммарное damageReduction от брони
ArmorCoverage = средний coverage от экипированной брони
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
| `EquipmentData.cs` | Добавить `techniqueDamageBonus`, `qiCostReduction`, `chargeSpeedBonus` |
| `EquipmentController.cs` | Добавить методы `GetTechniqueDamageBonus()`, `GetQiCostReduction()`, `GetChargeSpeedBonus()` |
| `DamageCalculator.cs` | Слой: TechniqueDamageBonus из EquipmentController |
| `TechniqueChargeSystem.cs` | Учитывать `chargeSpeedBonus` в CalculateChargeTime() |
| `TechniqueController.cs` | Учитывать `qiCostReduction` в CalculateQiCost() |

**Логика:**
- melee_weapon техники: урон += weapon.damage × weapon.techniqueDamageBonus
- ranged техники: урон += staff/wand.techniqueDamageBonus (если экипирован)
- chargeTime × (1 - chargeSpeedBonus) — ускорение накачки от оружия
- qiCost × (1 - qiCostReduction) — снижение стоимости от оружия

---

### ═══════════════════════════════════════════
### ФАЗА 3: MeleeWeapon → Требует оружие
### ═══════════════════════════════════════════

**Цель:** Техника melee_weapon не работает без оружия. Fallback → melee_strike.

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `TechniqueChargeSystem.cs` | Проверка: если combatSubtype == MeleeWeapon && нет оружия → fallback |
| `TechniqueController.cs` | `CanUseTechnique()` — проверка наличия оружия для MeleeWeapon |
| `CombatUI.cs` | Слот техники: если MeleeWeapon без оружия → Unavailable |

**Логика:**
```
if (technique.combatSubtype == MeleeWeapon)
{
    var weapon = equipmentController.GetMainWeapon();
    if (weapon == null || weapon.equipmentData.category != ItemCategory.Weapon)
    {
        // Fallback: техника недоступна, или конвертируется в MeleeStrike
        return CanUseAsFallback(technique);
    }
}
```

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
| QuickSlots 0-9 | 10 слотов для назначения |
| Drag-Drop или Click-to-Assign | Способ назначения |

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| Новый: `TechniqueAssignmentUI.cs` | Панель назначения техник |
| `CharacterPanelUI.cs` | Вкладка «Техники» → открывает TechniqueAssignmentUI |
| `TechniqueController.cs` | Поддержка переназначения (уже есть AssignToQuickSlot) |
| `CombatUI.cs` | InitializeTechniqueSlots — при изменении слотов |

---

### ═══════════════════════════════════════════
### ФАЗА 6: NPC — Экипировка и техники
### ═══════════════════════════════════════════

**Цель:** NPC получают оружие и техники из NPCPresetData.

**Изменяемые файлы:**

| Файл | Изменение |
|------|-----------|
| `NPCController.cs` | Добавить EquipmentController + TechniqueController (если нет) |
| `NPCPresetData.cs` | Добавить `defaultWeaponId`, `defaultTechniqueIds[]` |
| `NPCRuntimeSpawner.cs` | При спавне → экипировать оружие + изучить техники |
| `CombatAI.cs` | Учитывать экипировку при принятии решений |

---

## Сводная таблица файлов

### Новые файлы (1)
| Файл | Фаза | Назначение |
|------|-------|------------|
| `Scripts/UI/TechniqueAssignmentUI.cs` | 5 | Панель назначения техник в слоты |

### Изменяемые файлы (10)
| Файл | Фазы | Изменения |
|------|-------|-----------|
| `PlayerController.cs` | 1 | EquipmentController + делегирование ICombatant |
| `NPCController.cs` | 1, 6 | EquipmentController + техника/экипировка |
| `CombatantBase.cs` | 1 | GetEquipmentController(),Weapon/Armor свойства |
| `EquipmentData.cs` | 2 | techniqueDamageBonus, qiCostReduction, chargeSpeedBonus |
| `EquipmentController.cs` | 2 | Методы GetTechniqueDamageBonus() и др. |
| `DamageCalculator.cs` | 2 | Слой: TechniqueDamageBonus |
| `TechniqueChargeSystem.cs` | 2, 3 | chargeSpeedBonus, MeleeWeapon проверка |
| `TechniqueController.cs` | 2, 3 | qiCostReduction, MeleeWeapon fallback |
| `CombatUI.cs` | 4 | Weapon Display панель |
| `NPCPresetData.cs` | 6 | defaultWeaponId, defaultTechniqueIds |

---

## Следующие шаги

1. Подтверждение плана пользователем
2. Реализация по фазам
3. Git push после каждой фазы

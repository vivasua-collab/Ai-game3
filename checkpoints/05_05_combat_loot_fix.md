# Чекпоинт: Исправления базового игрового цикла — БОЙ + ЛУТ
# Дата: 2026-05-05 16:07:41 MSK
# Зависит от: 05_05_inventory_audit.md
# Статус: pending

## Цель
Сделать работоспособным базовый цикл: **Экипировка → Бой → Лут → Инвентарь → Экипировка**

---

## 🔴 P0 — КРИТИЧЕСКИЕ (блокируют игровой цикл)

### ИСП-БЛ-01: Подключить боевой лут к инвентарю игрока
- **Баги:** БАГ-ИНВ-02, BUG-LOOT-01, BUG-SCEN-05, BUG-SCEN-07
- **Файлы:** `Combat/CombatManager.cs:126,440`, новый `Combat/CombatLootHandler.cs`
- **Проблема:** `OnLootGenerated` — 0 подписчиков. LootResult потерян.
- **Решение:**
  1. Создать `CombatLootHandler` компонент на Player GO
  2. Подписаться на `CombatManager.OnLootGenerated`
  3. Конвертировать `LootEntry.ItemId` → `ItemData`/`EquipmentData`
  4. Вызвать `InventoryController.AddItem()` для каждого предмета
  5. Вызвать `QiController.AddQi(loot.QiAbsorbed)`
  6. Применить `CultivationExp` через QiController или CultivationController
- **Объём:** ~50 строк (новый файл) + ~5 строк (подключение в PlayerController)
- **⚠️ Блокируется:** Нужен ItemId → ItemData резолвер (см. ИСП-БЛ-06)

### ИСП-БЛ-02: Применить урон оружия при базовой атаке (MeleeStrike)
- **Баги:** BUG-DMG-01, BUG-SCEN-04, BUG-NPC-04
- **Файлы:** `Combat/DamageCalculator.cs:203-221`, `Player/PlayerController.cs:177`, `NPC/NPCController.cs:213`
- **Проблема:** Базовая атака использует `CombatSubtype.MeleeStrike`.
  Полная формула оружия требует `MeleeWeapon`. Оружейный урон теряется.
- **Решение (вариант A — предпочтительный):**
  ```csharp
  // PlayerController.GetAttackerParams():
  bool hasWeapon = equipmentController?.GetMainWeapon() != null;
  CombatSubtype = hasWeapon ? CombatSubtype.MeleeWeapon : CombatSubtype.MeleeStrike,
  ```
  Аналогично для NPCController.
- **Решение (вариант B — минимальный):**
  В DamageCalculator добавить fallback для MeleeStrike + WeaponBonusDamage:
  ```csharp
  // После основного блока MeleeWeapon:
  else if (attacker.WeaponBonusDamage > 0)
  {
      damage += attacker.WeaponBonusDamage;
  }
  ```
- **Объём:** ~5 строк

### ИСП-БЛ-03: Подключить заряженные техники к боевой системе
- **Баги:** BUG-TECH-01, BUG-NPC-03
- **Файлы:** `Technique/TechniqueChargeSystem.cs:392-417`, `Combat/CombatManager.cs`
- **Проблема:** `FireChargedTechnique()` возвращает `TechniqueUseResult`,
  но **никто не вызывает** `CombatManager.ExecuteTechniqueAttack()`.
  Техники заряжаются, но урон = 0.
- **Решение:**
  В `TechniqueChargeSystem.FireChargedTechnique()`, после получения result:
  ```csharp
  if (result.Success && result.Damage > 0)
  {
      var combatManager = CombatManager.Instance;
      if (combatManager != null)
      {
          combatManager.ExecuteTechniqueAttack(combatant, target, result);
      }
  }
  ```
  Или подписаться на событие `OnTechniqueFired` в CombatManager.
- **Объём:** ~10 строк

### ИСП-БЛ-04: Удаление мёртвых NPC из сцены
- **Баги:** BUG-NPC-01
- **Файлы:** `NPC/NPCController.cs:520-532`
- **Проблема:** `Die()` не уничтожает/не отключает GameObject.
  Мёртвые NPC копятся как призраки.
- **Решение:**
  ```csharp
  // В NPCController.Die(), после существующего кода:
  if (npcVisual != null) npcVisual.SetAlive(false); // скрыть визуал
  GetComponent<Collider2D>().enabled = false; // перестать блокировать
  Destroy(gameObject, 5f); // удалить через 5 секунд
  ```
- **Объём:** ~5 строк

### ИСП-БЛ-05: Исправить CombatTrigger.ShouldEngage — проверять отношение ВЛАДЕЛЬЦА
- **Баги:** BUG-NPC-02
- **Файлы:** `Combat/CombatTrigger.cs:108-136`
- **Проблема:** ShouldEngage проверяет `targetNpc.Attitude` (цель),
  а не `ownerNpc.Attitude` (владелец триггера).
  Враждебные NPC не могут автоматически напасть на игрока.
- **Решение:**
  Переписать ShouldEngage:
  ```csharp
  // Проверять отношение ВЛАДЕЛЬЦА к ЦЕЛИ
  NPCController ownerNpc = GetComponent<NPCController>();
  if (ownerNpc != null && ownerNpc.Attitude <= minAttitudeToEngage)
      return true;
  ```
- **Объём:** ~10 строк

---

## 🟠 P1 — ВЫСОКИЕ (существенно влияют на геймплей)

### ИСП-БЛ-06: Создать ItemId → ItemData резолвер
- **Баги:** БАГ-ИНВ-27, BUG-LOOT-02
- **Файлы:** Новый `Core/ItemDatabase.cs`
- **Проблема:** Нет единого способа конвертировать string itemId → ItemData.
  Блокирует ИСП-БЛ-01.
- **Решение:**
  ```csharp
  public class ItemDatabase : MonoBehaviour {
      private static Dictionary<string, ItemData> cache;
      public static ItemData GetById(string itemId) {
          if (cache == null) BuildCache();
          cache.TryGetValue(itemId, out var data);
          return data;
      }
      private static void BuildCache() {
          cache = new Dictionary<string, ItemData>();
          foreach (var item in Resources.FindObjectsOfTypeAll<ItemData>())
              cache[item.itemId] = item;
      }
  }
  ```
- **Объём:** ~25 строк

### ИСП-БЛ-07: Сохранять старый предмет при замене экипировки
- **Баги:** БАГ-ИНВ-12, BUG-SCEN-02
- **Файлы:** `Inventory/InventoryController.cs:541-562`, `Inventory/EquipmentController.cs:260-265`
- **Проблема:** EquipFromInventory теряет старый предмет.
- **Решение:**
  В `InventoryController.EquipFromInventory()`:
  ```csharp
  var oldInstance = equipmentController.Equip(equipData, slot.grade, slot.Durability);
  if (oldInstance != null)
      AddItem(oldInstance.equipmentData, 1);
  ```
  Изменить `EquipmentController.Equip()` чтобы возвращать `EquipmentInstance` (oldItem).
- **Объём:** ~10 строк

### ИСП-БЛ-08: Подключить статы игрока к StatDevelopment
- **Баги:** BUG-NPC-05, BUG-STATS-01
- **Файлы:** `Player/PlayerController.cs:120-123, 176, 195`
- **Проблема:** STR/AGI/INT/VIT = 10 захардкожены. Прокачка не влияет на бой.
- **Решение:**
  ```csharp
  int ICombatant.Strength => statDevelopment?.GetStat(StatType.Strength) ?? 10;
  int ICombatant.Agility => statDevelopment?.GetStat(StatType.Agility) ?? 10;
  // и т.д.
  ```
  Также обновить GetAttackerParams/GetDefenderParams.
- **Объём:** ~8 строк

### ИСП-БЛ-09: Убрать дублирование атак NPCAI + CombatAI
- **Баги:** BUG-NPC-08
- **Файлы:** `NPC/NPCAI.cs:439-479`, `Combat/CombatManager.cs:596-646`
- **Проблема:** Обе системы вызывают ExecuteBasicAttack — возможна двойная атака.
- **Решение:** NPCAI не должен вызывать CombatManager напрямую.
  Делегировать решение CombatAI, а NPCAI только управлять State Machine.
- **Объём:** ~15 строк

---

## 🟡 P2 — СРЕДНИЕ (улучшают качество)

### ИСП-БЛ-10: FormationBuffMultiplier — подключить к формационной системе
- **Баги:** BUG-FORM-01
- **Файлы:** `Player/PlayerController.cs:207`, `NPC/NPCController.cs:246`
- **Проблема:** Всегда 1.0f. Формационные бонусы защиты не работают в бою.
- **Решение:** Создать FormationCombatBridge, подписаться на FormationController,
  обновлять FormationBuffMultiplier при входе/выходе из формации.
- **Объём:** ~30 строк

### ИСП-БЛ-11: Предотвратить дублирование лута с экипировки NPC
- **Баги:** BUG-SCEN-06
- **Файлы:** `Combat/LootGenerator.cs:344-372`
- **Проблема:** Экипировка NPC добавляется в LootEntry, но не снимается с NPC.
- **Решение:** При генерации дропа — Unequip слот перед добавлением в LootResult.
- **Объём:** ~5 строк

### ИСП-БЛ-12: FormationEffects.ApplyDamage — использовать полную формулу урона
- **Баги:** BUG-FORM-02
- **Файлы:** `Formation/FormationEffects.cs:254`
- **Решение:** Вызывать CombatManager вместо прямого BodyController.ApplyDamage.
- **Объём:** ~10 строк

### ИСП-БЛ-13: UseQuickSlot — Success=true при начале зарядки
- **Баги:** BUG-TECH-02
- **Файлы:** `Technique/TechniqueController.cs:369-373`
- **Решение:** Вернуть Success=true с флагом IsCharging=true.
- **Объём:** ~3 строки

---

## ПОРЯДОК ИСПРАВЛЕНИЙ

```
Шаг 1: ИСП-БЛ-06 (ItemDatabase)        ← базовая зависимость
Шаг 2: ИСП-БЛ-01 (Лут → Инвентарь)     ← зависит от ItemDatabase
Шаг 3: ИСП-БЛ-02 (Урон оружия)          ← делает бой осмысленным
Шаг 4: ИСП-БЛ-03 (Техники → Бой)        ← делает техники работающими
Шаг 5: ИСП-БЛ-04 (Удаление мёртвых NPC) ← чистота сцены
Шаг 6: ИСП-БЛ-05 (CombatTrigger)        ← авто-агро работает
Шаг 7: ИСП-БЛ-07 (Сохранение старого)   ← экипировка не теряется
Шаг 8: ИСП-БЛ-08 (StatDevelopment)      ← прокачка влияет на бой
Шаг 9: ИСП-БЛ-09 (Дублирование атак)    ← стабильный бой
```

После шагов 1-6 базовый игровой цикл **функционален**.
Шаги 7-9 — улучшают качество и предотвращают потерю данных.

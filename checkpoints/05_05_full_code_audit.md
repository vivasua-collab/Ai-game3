# Полный аудит кода проекта Cultivation World Simulator

**Дата начала:** 2026-05-05 14:40:48 UTC  
**Дата завершения:** 2026-05-05 15:20:00 UTC  
**Всего файлов:** 200 .cs  
**Охвачено аудитом:** 200 файлов (7 субагентов)  

---

## Сводка

| Группа | Префикс | 🔴 | 🟠 | 🟡 | 🟢 | Всего |
|--------|---------|----|----|----|----|----|
| Core + Data + Save | БАГ-КОР | 2 | 4 | 4 | 2 | 12 |
| UI + Inventory UI | БАГ-ЮИ | 2 | 5 | 5 | 2 | 14 |
| Combat + Effects | БАГ-БОЙ | 2 | 4 | 5 | 3 | 14 |
| NPC + Player + Interaction | БАГ-НИП | 3 | 5 | 5 | 2 | 15 |
| Inventory + Equip + Body + Buff + Charger | БАГ-ИНВ | 3 | 5 | 5 | 2 | 15 |
| World + Tile + Formation + Qi + Quest | БАГ-МИР | 2 | 3 | 5 | 2 | 12 |
| Editor + SceneBuilder + Tests | БАГ-РЕД | 2 | 2 | 4 | 2 | 10 |
| **ИТОГО** | | **16** | **28** | **33** | **15** | **92** |

---

## 🔴 CRITICAL (16) — Краш / потеря данных

### БАГ-КОР-01 — ConsolidateSleep никогда не повышает характеристику
**Файл:** Core/StatDevelopment.cs:437-444  
**Проблема:** `maxConsolidation = min(hours * 0.025, 0.20)`. При threshold ≥ 1 и availableConsolidation ≤ 0.20 → `0.20 / 1 = 0`. Уровень НИКОГДА не повышается.  
**Исправление:** Увеличить MAX_CONSOLIDATION_PER_SLEEP до ≥1.0 или добавить поле consolidatedDelta.

### БАГ-КОР-02 — Integer overflow в GetRandomQiCapacity
**Файл:** Data/ScriptableObjects/MortalStageData.cs:192  
**Проблема:** `(int.MaxValue) + 1` переполняется в `int.MinValue` → ArgumentException.  
**Исправление:** Добавить проверку `if (maxQiCapacity >= int.MaxValue) return maxQiCapacity;`

### БАГ-ЮИ-01 — Divide by zero в GetHealthColor
**Файл:** UI/CombatUI.cs:1121  
**Проблема:** `float percent = current / max` — при max=0 результат NaN.  
**Исправление:** `float percent = max > 0 ? current / max : 0f;`

### БАГ-ЮИ-02 — Legacy Input.mousePosition — компиляционная ошибка
**Файл:** UI/Inventory/DragDropHandler.cs:395,409,437,450  
**Проблема:** `(Vector2)Input.mousePosition` — legacy Input может быть отключён.  
**Исправление:** Убрать fallback на Input.mousePosition.

### БАГ-БОЙ-01 — Пуллинг эффектов сломан — объекты уничтожаются вместо возврата
**Файл:** Combat/Effects/TechniqueEffect.cs:174-186  
**Проблема:** `Destroy(gameObject)` вместо `ReturnEffect()`. После maxPoolSize эффекты перестают создаваться.  
**Исправление:** Заменить Destroy на `TechniqueEffectFactory.Instance?.ReturnEffect(this)`.

### БАГ-БОЙ-02 — Накачка «замораживается» для дешёвых техник
**Файл:** Combat/TechniqueChargeSystem.cs:345-359  
**Проблема:** `(long)(QiChargeRate * deltaTime)` → 0 при маленькой chargeRate. Накачка зависает навсегда.  
**Исправление:** Накапливать дробный остаток `_qiAccumulator`.

### БАГ-НИП-01 — OnAIStateChanged НИКОГДА не вызывается
**Файл:** NPCAI.cs:533 + NPCController.cs:633  
**Проблема:** NPCAI.ChangeState() сначала устанавливает state, потом SetAIState() видит совпадение → early return.  
**Исправление:** Вызывать SetAIState ДО изменения state, или убрать проверку совпадения.

### БАГ-НИП-02 — CombatTrigger не добавляется враждебным NPC при runtime-спавне
**Файл:** NPCController.cs:346-369  
**Проблема:** InitializeCombatComponents() вызывается когда Attitude ещё Neutral. После установки Hostile — не вызывается повторно.  
**Исправление:** Добавить вызов InitializeCombatComponents() в конец InitializeFromGenerated().

### БАГ-НИП-03 — ICombatant.OnDamageTaken на PlayerController — пустой event
**Файл:** PlayerController.cs:102,146-160  
**Проблема:** `add { } remove { }` — пустая реализация, подписчики молча отбрасываются. Урон не генерирует событие.  
**Исправление:** Заменить на нормальный event с backing field, добавить Invoke.

### БАГ-ИНВ-01 — CreateInstance: предмет с durability=0 восстанавливается
**Файл:** Inventory/EquipmentController.cs:900  
**Проблема:** `durability > 0 ? durability : data.maxDurability` — сломанный предмет (durability=0) чинится.  
**Исправление:** Заменить на `durability >= 0 ? durability : data.maxDurability`.

### БАГ-ИНВ-02 — EquipFromInventory: старая экипировка теряется при замене
**Файл:** Inventory/EquipmentController.cs:560-586  
**Проблема:** Equip() вызывает Unequip(), но InventoryController НЕ подписан на OnEquipmentUnequipped. Старый предмет бесследно исчезает.  
**Исправление:** Подписаться на OnEquipmentUnequipped и возвращать старый предмет в инвентарь.

### БАГ-ИНВ-03 — ChargerController: камни Ци никогда не истощаются
**Файл:** Inventory/ChargerController.cs:387-417  
**Проблема:** Qi добавляется в буфер, но не извлекается из камней. Бесконечная энергия.  
**Исправление:** Вызывать slot.ExtractQi() после AccumulateFromStones.

### БАГ-МИР-01 — QuestController.OnTick() никогда не вызывается
**Файл:** Quest/QuestController.cs:593  
**Проблема:** Метод OnTick не подписан на TimeController.OnWorldTick. Квесты с ограничением времени никогда не истекают.  
**Исправление:** Подписать: `timeController.OnWorldTick += OnTick;`

### БАГ-МИР-02 — LocationController.LoadSaveData: мгновенное завершение путешествия
**Файл:** World/LocationController.cs:473-480  
**Проблема:** travelDuration не сохраняется/не восстанавливается → при загрузке travelDuration=0 → мгновенная телепортация.  
**Исправление:** Добавить travelDuration в LocationSaveData.

### БАГ-РЕД-01 — ClearExtendedAssets не удаляет StorageRings
**Файл:** Editor/AssetGeneratorExtended.cs:950-961  
**Проблема:** Метод очищает 5 папок, но пропускает StorageRings → дублирование при Clear→Generate.  
**Исправление:** Добавить `ClearDirectory(OUTPUT_STORAGE_RINGS);`

### БАГ-РЕД-02 — Дублирование StorageRingData — два генератора создают кольца
**Файл:** Editor/Phase16InventoryData.cs vs AssetGeneratorExtended.cs  
**Проблема:** Два генератора создают 8 файлов вместо 4 с разными именами/ID.  
**Исправление:** Унифицировать или убрать дублирующий код.

---

## 🟠 HIGH (28) — Некорректное поведение

### БАГ-КОР-03 — VFXPool maxPoolSize — глобальный лимит вместо per-prefab
**Файл:** Core/VFXPool.cs:117  
**Исправление:** Per-prefab счётчик или per-prefab maxSize.

### БАГ-КОР-04 — SaveManager сохраняет только ПЕРВЫЙ бафф
**Файл:** Save/SaveManager.cs:329-338  
**Исправление:** Изменить BuffData на `List<BuffSaveData>`.

### БАГ-КОР-05 — SaveManager сохраняет только ОДНУ формацию
**Файл:** Save/SaveManager.cs:314-326  
**Исправление:** Изменить FormationData на `List<FormationSaveEntry>`.

### БАГ-КОР-06 — SaveManager: FindFirstObjectByType вместо ServiceLocator
**Файл:** Save/SaveManager.cs:148-172  
**Исправление:** Заменить на ServiceLocator.GetOrFind<T>().

### БАГ-ЮИ-03 — Анимация скрытия диалога никогда не воспроизводится
**Файл:** UI/DialogUI.cs:200-208  
**Исправление:** Не вызывать SetActive(false) сразу после SetTrigger.

### БАГ-ЮИ-04 — WaitForSeconds зависит от Time.timeScale
**Файл:** UI/HarvestFeedbackUI.cs:334  
**Исправление:** Заменить на WaitForSecondsRealtime.

### БАГ-ЮИ-05 — Отсутствие панелей для GameState.Combat/Cutscene
**Файл:** UI/UIManager.cs:106-113  
**Исправление:** Добавить `panels[GameState.Combat] = characterPanel; panels[GameState.Cutscene] = mapPanel;`

### БАГ-ЮИ-06 — NRE в RefreshTechniqueSlots при null ActiveCharge
**Файл:** UI/CombatUI.cs:1021  
**Исправление:** Добавить null-проверки в цепочку.

### БАГ-ЮИ-07 — TooltipPanel блокирует raycasts — нет CanvasGroup
**Файл:** UI/Inventory/TooltipPanel.cs:34  
**Исправление:** Добавить CanvasGroup с blocksRaycasts=false.

### БАГ-БОЙ-03 — TechniqueController: NRE при отсутствии QiController
**Файл:** Combat/TechniqueController.cs:260,276,311,483  
**Исправление:** Добавить `if (qiController == null) return false;`

### БАГ-БОЙ-04 — Парирование + блок складываются мультипликативно — 85% снижение
**Файл:** Combat/DamageCalculator.cs:316-329  
**Исправление:** Сделать взаимоисключающими или задокументировать stacking.

### БАГ-БОЙ-05 — CombatEvents: статические события — утечка подписок
**Файл:** Combat/CombatEvents.cs:114-140  
**Исправление:** ClearAll() при загрузке сцены, отписка в OnDisable.

### БАГ-БОЙ-06 — CombatManager.ProcessChargeInterrupts — мёртвый код
**Файл:** Combat/CombatManager.cs:675-695  
**Исправление:** Удалить вызов из Update().

### БАГ-НИП-04 — isInDialogue мгновенно сбрасывается
**Файл:** NPCInteractable.cs:235,250  
**Исправление:** Сбрасывать по событию завершения диалога.

### БАГ-НИП-05 — InteractionController: обновление по Count, не по содержимому
**Файл:** InteractionController.cs:124  
**Исправление:** Сравнивать содержимое списков.

### БАГ-НИП-06 — SleepSystem: блокировка в FallingAsleep
**Файл:** Player/SleepSystem.cs:193,199-205  
**Исправление:** Добавить OnDisable или заменить корутину на таймер.

### БАГ-НИП-07 — SleepSystem.ProcessRecovery: восстановление выносливости не реализовано
**Файл:** Player/SleepSystem.cs:330-331  
**Исправление:** Добавить восстановление выносливости.

### БАГ-НИП-08 — NPCAI.CheckAggroRadius игнорирует Threat для Neutral NPC
**Файл:** NPCAI.cs:900-901  
**Исправление:** Добавить условие `|| GetHighestThreat() != null`.

### БАГ-ИНВ-04 — SpiritStorage: потеря предметов при null inventoryController
**Файл:** Inventory/SpiritStorageController.cs:379-436  
**Исправление:** Ранний возврат при null inventoryController.

### БАГ-ИНВ-05 — StorageRing: потеря предметов при null inventoryController
**Файл:** Inventory/StorageRingController.cs:555-614  
**Исправление:** Ранний возврат при null inventoryController.

### БАГ-ИНВ-06 — EquipmentController.RepairEquipment не обновляет статы
**Файл:** Inventory/EquipmentController.cs:864-871  
**Исправление:** Добавить `statsDirty = true; OnStatsChanged?.Invoke(CurrentStats);`

### БАГ-ИНВ-07 — BuffManager: RemoveSpecialEffect(Slow) пересчитывает с учётом удаляемого
**Файл:** Buff/BuffManager.cs:927-940  
**Исправление:** Пересчитывать ПОСЛЕ удаления из activeBuffs.

### БАГ-ИНВ-08 — LoadSaveData: durability/grade теряется при split-stack
**Файл:** Inventory/InventoryController.cs:785-809  
**Исправление:** Использовать CreateSlot с ручным назначением.

### БАГ-ИНВ-09 — CraftingController: NRE при null playerInventory
**Файл:** Inventory/CraftingController.cs:171,202,251  
**Исправление:** Добавить null-guards.

### БАГ-МИР-03 — FactionController.AddMember перезаписывает без уведомления
**Файл:** World/FactionController.cs:274  
**Исправление:** Вызвать RemoveMember для старой фракции.

### БАГ-МИР-04 — ResourceSpawner.OnDestroy уничтожает static shared-материалы
**Файл:** Tile/ResourceSpawner.cs:112-117  
**Исправление:** Подсчёт ссылок или очистка при scene unload.

### БАГ-МИР-05 — FactionController.DestroyFaction не очищает playerMemberships
**Файл:** World/FactionController.cs:227-234  
**Исправление:** Удалить осиротевшие записи.

### БАГ-РЕД-03 — TestLocationSetup: legacy Light вместо Light2D (URP)
**Файл:** Tile/Editor/TestLocationSetup.cs:139-142  
**Исправление:** Использовать Light2D из URP.

### БАГ-РЕД-04 — NPCAssemblyExample без #if UNITY_EDITOR — попадает в build
**Файл:** Examples/NPCAssemblyExample.cs  
**Исправление:** Обернуть в #if UNITY_EDITOR.

---

## 🟡 MEDIUM (33) — Работает, но может вызвать проблемы

### БАГ-КОР-07 — VFXPool prewarmOnStart/initialPoolSize не используются
**Файл:** Core/VFXPool.cs:37-42

### БАГ-КОР-08 — LongMinMaxRange.GetRandom() теряет точность для больших long
**Файл:** Data/ScriptableObjects/SpeciesData.cs:167-170

### БАГ-КОР-09 — SaveManager auto-save: Time.deltaTime вместо unscaledDeltaTime
**Файл:** Save/SaveManager.cs:135

### БАГ-КОР-10 — SaveFileHandler: статический нулевой IV для шифрования
**Файл:** Save/SaveFileHandler.cs:336

### БАГ-КОР-11 — SaveManager: данные 4 систем сохранены но НЕ восстанавливаются
**Файл:** Save/SaveManager.cs:533-535

### БАГ-ЮИ-08 — FindGameObjectWithTag каждый кадр (WeaponDirectionIndicator)
**Файл:** UI/WeaponDirectionIndicator.cs:71-75

### БАГ-ЮИ-09 — GetComponentsInChildren каждый кадр (CharacterPanelUI)
**Файл:** UI/CharacterPanelUI.cs:523

### БАГ-ЮИ-10 — OnNodeChanged не проверяет node на null
**Файл:** UI/DialogUI.cs:132-148

### БАГ-ЮИ-11 — HarvestFeedbackUI: FadeOut использует Time.deltaTime
**Файл:** UI/HarvestFeedbackUI.cs:341

### БАГ-ЮИ-12 — EventSystem.current без кэширования
**Файл:** UI/ContextMenuUI.cs:128

### БАГ-БОЙ-07 — EndCombat вызывается изнутри ExecuteAttack через OnDeath
**Файл:** Combat/CombatManager.cs:313-358

### БАГ-БОЙ-08 — Неиспользуемые аллоцированные буферы Collider2D
**Файл:** Combat/Effects/DirectionalEffect.cs:48, ExpandingEffect.cs:47

### БАГ-БОЙ-09 — CombatTrigger: избыточные приведения ICombatant
**Файл:** Combat/CombatTrigger.cs:83,87

### БАГ-БОЙ-10 — TechniqueChargeSystem: потеря точности long→float
**Файл:** Combat/TechniqueChargeSystem.cs:620-622

### БАГ-БОЙ-11 — TechniqueEffectFactory: лишний Dequeue при создании
**Файл:** Combat/Effects/TechniqueEffectFactory.cs:148-151

### БАГ-НИП-09 — NPCVisual.LateUpdate: Camera.main 2 раза каждый кадр
**Файл:** NPC/NPCVisual.cs:121,128

### БАГ-НИП-10 — InteractionController.ScanForInteractables каждый кадр
**Файл:** Interaction/InteractionController.cs:99-103

### БАГ-НИП-11 — Дублирование смены AI-состояния между NPCAI и NPCController
**Файл:** NPC/NPCController.cs:631-639

### БАГ-НИП-12 — NPCRuntimeSpawner: reflection для private-метода
**Файл:** NPC/NPCRuntimeSpawner.cs:229-235

### БАГ-НИП-13 — PlayerController: прямой опрос Keyboard вместо InputAction
**Файл:** Player/PlayerController.cs:489-522

### БАГ-НИП-14 — NPCAI.ExecuteWandering: WanderAround каждый кадр
**Файл:** NPCAI.cs:372-378

### БАГ-ИНВ-10 — BuffManager.UpdateBuffStackEffects не обновляет спецэффекты
**Файл:** Buff/BuffManager.cs:1063-1093

### БАГ-ИНВ-11 — StorageRingController: NRE при null itemData
**Файл:** Inventory/StorageRingController.cs:360-371

### БАГ-ИНВ-12 — SpiritStorageController.CanStoreWithQi: false при null qiController
**Файл:** Inventory/SpiritStorageController.cs:262-269

### БАГ-ИНВ-13 — BuffManager: conductivityPaybackRate не уменьшается при снятии
**Файл:** Buff/BuffManager.cs:889-902

### БАГ-МИР-06 — QiController: потеря дробной части при regen >= 1.0
**Файл:** Qi/QiController.cs:265

### БАГ-МИР-07 — FormationController: Time.deltaTime вне Update
**Файл:** Formation/FormationController.cs:646

### БАГ-МИР-08 — EventController: первый Update вызывает мгновенную проверку
**Файл:** World/EventController.cs:119

### БАГ-МИР-09 — FormationCore.affectedBuffer не используется
**Файл:** Formation/FormationCore.cs:100

### БАГ-МИР-10 — QiController.AddQi не защищена от отрицательных значений
**Файл:** Qi/QiController.cs:220-228

### БАГ-РЕД-05 — EnsureDirectory: нет AssetDatabase.Refresh после CreateDirectory
**Файл:** Editor/SceneBuilder/SceneBuilderUtils.cs:302-318

### БАГ-РЕД-06 — Phase07UI: MainMenu.SetActive(true) противоречит комментарию
**Файл:** Editor/SceneBuilder/Phase07UI.cs:206-207

### БАГ-РЕД-07 — Phase17InventoryUI: SetActive(false) до подключения ссылок
**Файл:** Editor/SceneBuilder/Phase17InventoryUI.cs:114-120

---

## 🟢 LOW (15) — Стиль, оптимизация, косметика

### БАГ-КОР-12 — VFXPool: неотслеживаемые временные объекты при переполнении
### БАГ-ЮИ-13 — Comment-баг: TMP тексты наследуют Graphic
### БАГ-ЮИ-14 — ContextMenuOption дублируется в двух пространствах имён
### БАГ-БОЙ-12 — OrbitalWeapon._isInitialized — поле не читается
### БАГ-БОЙ-13 — CombatTrigger.attackRange не используется в логике
### БАГ-БОЙ-14 — DamageCalculator: ArmorCoverage без Clamp01
### БАГ-НИП-15 — NPCController.ApplyPreset не инициализирует Agility
### БАГ-ИНВ-14 — InventorySlot: [SerializeField] internal — нестандартный атрибут
### БАГ-ИНВ-15 — ChargerController: lambda подписка без отписки
### БАГ-МИР-11 — FormationQiPool.ProcessDrain возвращает int вместо long
### БАГ-МИР-12 — LocationSaveData.DiscoveredLocations может быть null
### БАГ-РЕД-08 — EquipmentSceneSpawner: UnityEngine.Random вместо SeededRandom
### БАГ-РЕД-09 — AssetGenerator.cs: опечатка в дате (2025 вместо 2026)
### БАГ-РЕД-10 — CheckDuplicateNames не проверяет StorageRings

---

## Топ-10 приоритетных багов для исправления

1. **БАГ-НИП-02** — CombatTrigger не добавляется враждебным NPC → агр не работает
2. **БАГ-ИНВ-02** — Старая экипировка теряется при замене → потеря предметов
3. **БАГ-НИП-01** — OnAIStateChanged никогда не вызывается → AI не уведомляет
4. **БАГ-БОЙ-02** — Накачка зависает для дешёвых техник → боёвка сломана
5. **БАГ-ИНВ-03** — Камни Ци бесконечны → экономика сломана
6. **БАГ-ЮИ-05** — Нет панелей для Combat/Cutscene → застревание UI
7. **БАГ-НИП-03** — PlayerController.OnDamageTaken пустой → урон невидим
8. **БАГ-ЮИ-02** — Legacy Input → компиляционная ошибка
9. **БАГ-ИНВ-01** — Сломанная экипировка чинится при экипировке
10. **БАГ-КОР-04/05** — Сохраняется только 1 бафф/формация

# Чекпоинт: Fix-07 — NPC + Quest + Dialogue

**Дата:** 2026-04-10 13:37:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** ✅ complete
**Приоритет:** P0-HIGH

---

## Описание

NPC: SaveData неполная, SkillLevels не сериализуем, AI поведение сломано, возраст не работает, Disposition → Attitude+PersonalityTrait. Quest: GrantRewards заглушка, LoadSaveData не восстанавливает, objectives на shared SO. Dialogue: JsonUtility array root всегда fails.

---

## Файлы (7 файлов, ~2620 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `NPC/NPCController.cs` | 483 | NPCSaveData, maxQi long, UpdateLifespan, TakeDamage notify, disposition→attitude+personality |
| 2 | `NPC/NPCAI.cs` | 408 | DecideNormalBehavior вероятности, Threat decay, Cultivating Qi, PersonalityTrait weights |
| 3 | `NPC/NPCData.cs` | ~200 | SkillLevels Dictionary→сериализуемая структура |
| 4 | `NPC/RelationshipController.cs` | 425 | ProcessDecay game time, ownerId validation, CalculateRelationshipType, Attitude integration |
| 5 | `Quest/QuestController.cs` | 677 | GrantRewards реализация, LoadSaveData активные квесты |
| 6 | `Quest/QuestData.cs` | 277 | Clone objectives вместо shared SO |
| 7 | `Interaction/DialogueSystem.cs` | 503 | JsonUtility array root fix, LoadDialogueNode stub |

---

## Задачи

### CRITICAL
- [x] NPC-C01: NPCSaveData — добавить MaxQi (long), MaxHealth, MaxStamina, MaxLifespan
- [x] NPC-C02: NPCData.SkillLevels — Dictionary→сериализуемый wrapper (SkillLevelData + SkillLevelEntry)
- [x] QST-C01: QuestData — CloneForInstance() + QuestObjective.Clone() для каждого экземпляра квеста
- [x] QST-C02: QuestController.LoadSaveData — восстановление активных квестов из save data + questDataRegistry
- [x] QST-C03: QuestController.GrantRewards — XP→StatDevelopment, Items→InventoryController, Gold→PlayerState
- [x] DLG-C01: DialogueSystem.LoadDialogueFromJson — DialogueNodeArrayWrapper для JsonUtility

### Disposition → Attitude + PersonalityTrait (HIGH, cascade from Fix-04)
- [x] NPC-ATT-01: NPCController — Attitude + PersonalityTrait поля, ConvertDispositionToAttitude/Personality helpers
- [x] NPC-ATT-02: NPCAI.DecideNormalBehavior — PersonalityTrait weights (Aggressive/Cautious/Ambitious/Treacherous/Loyal/Pacifist)
- [x] NPC-ATT-03: RelationshipController — CalculateAttitude() вместо GetDisposition()
- [x] NPC-ATT-04: NPCSaveData — AttitudeValue (int) + PersonalityFlags (int)

### HIGH
- [x] NPC-H01: NPCAI.DecideNormalBehavior — взвешенные вероятности с cumulative selection
- [x] NPC-H02: NPCController.UpdateLifespan — game time через TimeController, lifespanAccumulator
- [x] NPC-H03: NPCController.TakeDamage — уведомление AI атакера через AddThreat()
- [x] NPC-H05: RelationshipController.ProcessDecay — TimeController вместо Time.time

### MEDIUM
- [x] NPC-M01: NPCAI Threat decay — DecayThreats() с threatDecayInterval/rate
- [x] NPC-M02: NPCAI.ExecuteCultivating — Qi восстановление пропорционально QiController.Conductivity
- [x] NPC-M03: RelationshipController.ownerId — валидация null/empty
- [x] NPC-M05: NPCController.InitializeFromGenerated — использование generated.age
- [x] NPC-M06: RelationshipController.CalculateRelationshipType — Hatred/SwornAlly, диапазоны Attitude
- [x] DLG-M04: DialogueSystem.LoadDialogueNode — загрузка из Resources/Dialogues + кэш

---

## Порядок выполнения

1. NPCData.cs — SkillLevels сериализация
2. NPCController.cs — SaveData + UpdateLifespan + TakeDamage + Attitude/PersonalityTrait поля
3. NPCAI.cs — вероятности + Threat decay + Cultivating + PersonalityTrait weights
4. RelationshipController.cs — game time + validation + Attitude integration
5. QuestData.cs — clone objectives
6. QuestController.cs — GrantRewards + LoadSaveData
7. DialogueSystem.cs — JsonUtility fix + LoadDialogueNode

---

## Зависимости

- **Предшествующие:** Fix-01 (Qi types), Fix-04 (Attitude + PersonalityTrait enum созданы)
- **Последующие:** Fix-08 (Save — NPC/Quest save data)

---

*Чекпоинт обновлён: 2026-04-11 12:00:00 UTC — Fix-07 complete, 8 файлов, 18 задач*

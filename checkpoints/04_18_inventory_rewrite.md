# Чекпоинт: Полная переделка инвентаря

**Дата:** 2026-04-18 18:20:58 UTC
**Редактировано:** 2026-04-18 19:25:00 UTC
**Статус:** in_progress — Этап 0 ✅, Этап 1 ✅, Этап 2 ✅

## Контекст

Решение: ПОЛНАЯ ПЕРЕДЕЛКА кода инвентаря. Старый код содержит ошибки и не соответствует v2.0 дизайну.

**Ключевое условие (добавлено 18:39):** Папка `Assets/` удаляется и пересоздаётся генератором (Phase09). Нет существующих .asset файлов, нет save-данных. Все риски совместимости = 0. EquipmentSlot enum переписывается ПОЛНОСТЬЮ.

## GitHub
- Репозиторий: `vivasua-collab/Ai-game3.git`, ветвь `main`
- Токен: (в контексте сессии, НЕ коммитить в файлы!)

## Аудит — выполнен ✅

| Документ | Статус | Дата |
|----------|--------|------|
| `docs_temp/INVENTORY_FLAGS_AUDIT.md` v2.0 | ✅ Завершён | 2026-04-18 18:35 |
| `checkpoints/04_18_inventory_implementation.md` | ⛔ Superseded | — |
| `checkpoints/04_18_data_model_rewrite.md` | 🆕 Создан | 2026-04-18 18:39 |

**Ключевые выводы аудита:**
- volume + allowNesting — 🟢 ПРОСТО (1-2 ч), нулевой риск
- EquipmentSlot полная замена — 🟢 ПРОСТО (30 мин), Assets пересоздаются
- BackpackData + StorageRingData — 🟢 ПРОСТО (2-3 ч)
- SpiritStorageController — 🔴 СЛОЖНО (5-8 ч), отложено
- StorageRingController — 🔴 СЛОЖНО (5-8 ч), отложено

---

## План (по этапам)

### Этап 0: Переделка моделей данных ⬜ → 🆕 Выделен в отдельный чекпоинт

**Подробный план:** `checkpoints/04_18_data_model_rewrite.md`

- [x] 0.1 Переписать EquipmentSlot enum (полная замена по драфту v2.0) ✅
- [x] 0.2 Добавить NestingFlag enum ✅
- [x] 0.3 Добавить volume + allowNesting в ItemData ✅
- [x] 0.4 Создать BackpackData.cs ScriptableObject ✅
- [x] 0.5 Создать StorageRingData.cs ScriptableObject ✅
- [x] 0.6 Добавить WeaponHandType enum + handType в EquipmentData ✅
- [x] 0.7 Обновить AssetGeneratorExtended под новый enum и новые SO ✅
- [x] 0.8 Обновить все файлы, ссылающиеся на EquipmentSlot ✅
- [x] 0.9 Проверить компиляцию ✅

### Этап 1: Базовая кукла (Doll) ✅

- [x] 1.1 Переписать EquipmentController.cs (7 видимых слотов, 1H/2H логика) ✅
- [x] 1.2 Переписать EquipmentInstance (убран currentLayer, +IsTwoHand) ✅
- [x] 1.3 Переписать EquipmentStats (раздельные GradeMultiplier, +conductivity, +vitality) ✅
- [x] 1.4 Интеграция с ServiceLocator + каскадные фиксы ✅
- [x] 1.5 Проверить компиляцию + git push ✅

**Коммит:** a41fb51 — EquipmentController v2.0
**Каскадные фиксы:** Phase06Player.cs, SceneSetupTools.cs, InventoryUI.cs

**Исправленные баги:**
- EQP-BUG-02: value бонусов рассчитывалось, но не использовалось
- EQP-BUG-04: нет логики двуручного оружия → добавлена
- EQP-BUG-05: GradeMultiplier не совпадает с EQUIPMENT_SYSTEM.md → разделены durability/effectiveness

### Этап 2: Рюкзак (Backpack) ✅

- [x] 2.1 Переписать InventoryController.cs (динамическая сетка от BackpackData) ✅
- [x] 2.2 Исправить все баги INV-BUG-01..07 ✅
- [x] 2.3 Добавить SetBackpack() + effectiveWeight ✅
- [x] 2.4 Добавить EquipFromInventory / UnequipToInventory мост ✅
- [x] 2.5 Обновить CraftingController.cs (CRA-BUG-01..03) ✅
- [x] 2.6 Проверить компиляцию + git push ✅

**Коммит:** f3a5683 — InventoryController v2.0 + CraftingController багфиксы
**Каскадные фиксы:** Phase06Player.cs, SceneSetupTools.cs, InventoryUI.cs

**Исправленные баги Inventory (7):**
- INV-BUG-01: Рекурсивный AddItem → итеративный
- INV-BUG-02: RemoveFromSlot — вес до модификации
- INV-BUG-03: Resize — полная перестройка occupancy
- INV-BUG-04: FreeGrid → RebuildGridFromSlots
- INV-BUG-05: HasDurability при durability=0 → true
- INV-BUG-06: LoadSaveData durability=0 → загружается
- INV-BUG-07: FreeSlots → FreeCells (ячейки, не предметы)

**Исправленные баги Crafting (3):**
- CRA-BUG-01: Уведомление о потере материалов
- CRA-BUG-02: (уже исправлен INV-H01)
- CRA-BUG-03: GetAvailableRecipes O(N²) → O(N)

### Этап 3: UI ⬜

- [ ] 3.1 InventoryScreen.cs — Canvas, открытие/закрытие по I
- [ ] 3.2 BodyDollPanel.cs — 7 видимых слотов
- [ ] 3.3 BackpackPanel.cs — динамическая сетка
- [ ] 3.4 InventorySlotUI.cs — визуальный слот
- [ ] 3.5 DragDropHandler.cs — перетаскивание
- [ ] 3.6 TooltipPanel.cs — карточка предмета
- [ ] 3.7 Проверить компиляцию + визуал

### Отложено (не в текущей сессии):
- Этап 4: Духовное хранилище (SpiritStorageController + каталогизатор)
- Этап 5: Кольца хранения (StorageRingController + объём)
- Этап 6: Пояс + контекстное меню + анимации

---

## Карта зависимостей — что сломается при переделке

| Файл | Что менять | Сложность |
|------|-----------|-----------|
| PlayerController.cs | API вызовы Inventory — проверить | 🟢 Низкая |
| ResourcePickup.cs | API вызовы Inventory — проверить | 🟢 Низкая |
| QuestController.cs | API вызовы Inventory — проверить | 🟢 Низкая |
| Combatant.cs | EquipmentController (TODO, не реализовано) | 🟢 Без изменений |
| InventoryUI.cs | Полная замена | 🔴 ПЕРЕПИСАТЬ |
| CharacterPanelUI.cs | Полная замена | 🔴 ПЕРЕПИСАТЬ |
| CraftingController.cs | API совместим, багфиксы | 🟡 Средняя |
| AssetGeneratorExtended.cs | ParseEquipmentSlot + новые SO | 🟡 Средняя |
| NPCPresetData.cs | EquipmentSlot | 🟢 Обновить |

---

## Изменённые файлы (будут)

### Этап 0 (модели данных):
- `Scripts/Core/Enums.cs` — EquipmentSlot, NestingFlag, WeaponHandType
- `Scripts/Data/ScriptableObjects/ItemData.cs` — volume, allowNesting, fix sizeHeight
- `Scripts/Data/ScriptableObjects/BackpackData.cs` — НОВЫЙ
- `Scripts/Data/ScriptableObjects/StorageRingData.cs` — НОВЫЙ
- `Scripts/Data/ScriptableObjects/EquipmentData.cs` — +handType
- `Scripts/Generators/AssetGeneratorExtended.cs` — обновить генерацию

### Этап 1 (кукла):
- `Scripts/Inventory/EquipmentController.cs` — ПЕРЕПИСАТЬ

### Этап 2 (рюкзак):
- `Scripts/Inventory/InventoryController.cs` — ПЕРЕПИСАТЬ
- `Scripts/Inventory/CraftingController.cs` — багфиксы

### Этап 3 (UI):
- `Scripts/UI/Inventory/*` — НОВЫЕ файлы

---

## Следующий шаг

Начать Этап 3 — UI (InventoryScreen, BodyDollPanel, BackpackPanel).

---

*Чекпоинт создан: 2026-04-18 18:20:58 UTC*
*Редактировано: 2026-04-18 19:25:00 UTC*

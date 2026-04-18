# Чекпоинт: Поэтапное внедрение инвентаря

**Дата:** 2026-04-18 18:06:01 UTC
**Редактировано:** 2026-04-18 18:39:26 UTC
**Статус:** ⛔ superseded — заменён на `04_18_inventory_rewrite.md` + `04_18_data_model_rewrite.md`
**Цель:** ~~Базовая кукла + рюкзак + интерфейс пользователя~~ → Полная переделка кода

---

## История

1. **v1 (18:06)** — Создан как план поэтапного внедрения (6 этапов, патчи поверх существующего кода)
2. **v2 (18:09)** — Добавлен «Вариант B: Полная переделка» — обоснование, карта зависимостей, новая архитектура
3. **v3 (18:20)** — Решение: идём по варианту B (полная переделка). Создан отдельный чекпоинт `04_18_inventory_rewrite.md`
4. **v4 (18:39)** — Проведён аудит флагов (`docs_temp/INVENTORY_FLAGS_AUDIT.md` v2.0). Уточнено, что Assets пересоздаются → EquipmentSlot можно переписать полностью. Создан `04_18_data_model_rewrite.md` для детального плана переделки моделей данных.

## Ссылки на актуальные документы

| Документ | Назначение |
|----------|-----------|
| `checkpoints/04_18_inventory_rewrite.md` | Чекпоинт полной переделки инвентаря |
| `checkpoints/04_18_data_model_rewrite.md` | Чекпоинт переделки моделей данных (EquipmentSlot, volume, allowNesting, BackpackData, StorageRingData) |
| `docs_temp/INVENTORY_FLAGS_AUDIT.md` | Аудит сложности добавления флагов (v2.0) |
| `docs_temp/INVENTORY_UI_DRAFT.md` | Драфт v2.0 инвентаря |

---

## Сохранённые данные аудита (для справки)

### InventoryController.cs — баги

| ID | Серьёзность | Описание |
|----|------------|----------|
| INV-BUG-01 | 🔴 CRITICAL | Рекурсивный AddItem — рассинхрон веса |
| INV-BUG-02 | 🔴 CRITICAL | RemoveFromSlot — порядок weight update |
| INV-BUG-03 | 🟡 HIGH | Resize — не проверяет заполненность ячеек |
| INV-BUG-04 | 🟡 HIGH | FreeGrid — пересечение областей |
| INV-BUG-05 | 🟠 MEDIUM | HasDurability при durability=0 |
| INV-BUG-06 | 🟠 MEDIUM | LoadSaveData durability=0 |
| INV-BUG-07 | 🟢 LOW | FreeSlots считает неверно |

### EquipmentController.cs — баги

| ID | Серьёзность | Описание |
|----|------------|----------|
| EQP-BUG-01 | 🔴 CRITICAL | EquipmentSlot enum не соответствует дизайну |
| EQP-BUG-02 | 🟡 HIGH | gradeMultiplier для stat бонусов |
| EQP-BUG-03 | 🟠 MEDIUM | SwapSlots — currentLayer не обновляется |
| EQP-BUG-04 | 🟠 MEDIUM | Нет логики двуручного оружия |
| EQP-BUG-05 | 🟠 MEDIUM | GradeMultiplier не совпадает с EQUIPMENT_SYSTEM.md |

### CraftingController.cs — баги

| ID | Серьёзность | Описание |
|----|------------|----------|
| CRA-BUG-01 | 🟡 HIGH | Craft failure — нет уведомления о потере |
| CRA-BUG-02 | 🟠 MEDIUM | CraftCustom — grade не доходит до Equip |
| CRA-BUG-03 | 🟠 MEDIUM | GetAvailableRecipes — O(N²) |

---

*Чекпоинт создан: 2026-04-18 18:06:01 UTC*
*Редактировано: 2026-04-18 18:39:26 UTC*
*Статус: SUPERSeded — см. 04_18_inventory_rewrite.md*

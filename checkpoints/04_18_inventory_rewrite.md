# Чекпоинт: Полная переделка инвентаря

**Дата:** 2026-04-18 18:20:58 UTC
**Статус:** in_progress

## Контекст

Решение: ПОЛНАЯ ПЕРЕДЕЛКА кода инвентаря. Старый код от предыдущего агента содержит ошибки и не соответствует v2.0 дизайну.

## GitHub
- Репозиторий: `vivasua-collab/Ai-game3.git`, ветвь `main`
- Токен: (см. контекст сессии, НЕ коммитить в файлы!)

## План (по этапам)

### Этап 0: Подготовка данных ⬜
- [ ] 0.1 Обновить EquipmentSlot enum
- [ ] 0.2 Добавить NestingFlag enum
- [ ] 0.3 Добавить volume + allowNesting в ItemData
- [ ] 0.4 Создать BackpackData SO
- [ ] 0.5 Создать WeaponHandType enum
- [ ] 0.6 Добавить handType в EquipmentData

### Этап 1: Базовая кукла (Doll) ⬜
- [ ] 1.1 Переписать EquipmentController.cs (7 видимых слотов, 1H/2H)
- [ ] 1.2 Обновить EquipmentInstance
- [ ] 1.3 Обновить EquipmentStats
- [ ] 1.4 Интеграция с ServiceLocator

### Этап 2: Рюкзак (Backpack) ⬜
- [ ] 2.1 Переписать InventoryController.cs (динамическая сетка)
- [ ] 2.2 Переписать CraftingController.cs (исправить баги)
- [ ] 2.3 Создать систему рюкзаков

### Этап 3: UI ⬜
- [ ] 3.1 InventoryUI.cs
- [ ] 3.2 BodyDollPanel.cs
- [ ] 3.3 BackpackPanel.cs
- [ ] 3.4 InventorySlotUI.cs
- [ ] 3.5 DragDropHandler.cs
- [ ] 3.6 TooltipPanel.cs

### Отложено:
- Этап 4: Духовное хранилище
- Этап 5: Кольца хранения
- Этап 6: Пояс + доработка

## Аудит текущего кода — найденные ошибки

| # | Файл | Ошибка | Критичность |
|---|------|--------|-------------|
| AUD-01 | InventoryController.cs | FreeSlots считается неверно | СРЕДНЯЯ |
| AUD-02 | EquipmentController.cs | CanEquip пропускает проверки при null playerStats | ВЫСОКАЯ |
| AUD-03 | EquipmentController.cs | SwapSlots бессмысленно меняет слои | СРЕДНЯЯ |
| AUD-04 | EquipmentController.cs | Slot по умолчанию Backpack при null | НИЗКАЯ |
| AUD-05 | CraftingController.cs | CraftCustom всегда успешен | СРЕДНЯЯ |
| AUD-06 | CraftingController.cs | LoadSaveData не восстанавливает craftingExperience | ВЫСОКАЯ |
| AUD-07 | CraftingController.cs | LoadSaveData не восстанавливает рецепты | ВЫСОКАЯ |
| AUD-08 | InventoryController.cs | nextSlotId не сохраняется | НИЗКАЯ |

## Изменённые файлы
- `docs_temp/INVENTORY_IMPLEMENTATION_PLAN.md` — создан (этот план)
- `docs_temp/INVENTORY_UI_DRAFT.md` — создан ранее (v2.0 дизайн)

## Следующий шаг
Начать Этап 0: обновление EquipmentSlot enum и создание новых типов данных.

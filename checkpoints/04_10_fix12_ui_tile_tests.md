# Чекпоинт: Fix-12 — UI System + Tile System + Charger + Tests

**Дата:** 2026-04-10 12:55:00 UTC
**Фаза:** Phase 7 — Integration
**Статус:** pending
**Приоритет:** MEDIUM-HIGH

---

## Описание

UI: Qi long→float потеря точности, FindFirstObjectByType, старый Input, InventoryUI заглушки, DialogUI деактивация. Tile: temperature accumulation, DateTime сериализация, ResourcePickup без Inventory. Charger: [SerializeField] без MonoBehaviour (если не в Fix-01). Tests: Qi типы, мёртвый код.

---

## Файлы (12 файлов, ~5800 строк)

| # | Файл | Строк | Изменение |
|---|------|-------|-----------|
| 1 | `UI/InventoryUI.cs` | 835 | UseItem, сортировка, детали — заглушки |
| 2 | `UI/CombatUI.cs` | 942 | Camera.main, Qi long→float, ProgressBar |
| 3 | `UI/CultivationProgressBar.cs` | 612 | Qi long→double→float, оба бара |
| 4 | `UI/CharacterPanelUI.cs` | 820 | GetComponentsInChildren, Qi slider, CultivationLevel |
| 5 | `UI/HUDController.cs` | 426 | Qi long→float, divide by zero cooldown |
| 6 | `UI/DialogUI.cs` | 375 | HideDialog деактивация |
| 7 | `Tile/TileData.cs` | 342 | Temperature accumulation (+= → =) |
| 8 | `Tile/ResourcePickup.cs` | ~250 | Интеграция с InventoryController |
| 9 | `Tile/DestructibleObjectController.cs` | 369 | Интеграция с Inventory, Texture2D leak |
| 10 | `Tests/IntegrationTestScenarios.cs` | 679 | Qi float→long |
| 11 | `Tests/IntegrationTests.cs` | 969 | SpendQi long, dead loop |
| 12 | `Character/IndependentScale.cs` | ~100 | Namespace CultivationWorld→CultivationGame |

---

## Задачи

### CRITICAL
- [ ] TIL-C01: TileData temperature — `=` вместо `+=` (строка 81)
- [ ] CHR-C01: ChargerHeat/ChargerBuffer/ChargerSlot — [SerializeField] fix (если не в Fix-01)

### HIGH
- [ ] UI-H03: 9 UI файлов FindFirstObjectByType → ServiceLocator (batch замена)
- [ ] UI-H04: Qi long→double→float для слайдеров (использовать форматирование вместо прямого cast)
- [ ] UI-H06: 4 UI файла старый Input System → новый
- [ ] TIL-H01: ResourcePickup + DestructibleObjectController — интеграция с InventoryController

### MEDIUM
- [ ] UI-M06: InventoryUI — реализовать UseItem, сортировку, детали предметов
- [ ] UI-M03: HUDController divide by zero в cooldown
- [ ] UI-M04: CultivationProgressBar — оба бара одинаковое значение
- [ ] CHR-H01: IndependentScale + CharacterSpriteController namespace
- [ ] TIL-C02: TileMapData DateTime → string (Unix timestamp) для JsonUtility

### LOW
- [ ] UI-L02: Camera.main null checks
- [ ] UI-L03: Duplicate helpers в UI файлах
- [ ] TIL-L02: TestLocationGameController Texture2D leak

---

## Порядок выполнения

1. TileData.cs — temperature fix
2. ResourcePickup.cs + DestructibleObjectController.cs — Inventory интеграция
3. IntegrationTestScenarios.cs + IntegrationTests.cs — Qi long + фиксы
4. InventoryUI.cs — заглушки
5. CombatUI.cs — Camera + Qi
6. CultivationProgressBar.cs + HUDController.cs + CharacterPanelUI.cs — Qi + фиксы
7. DialogUI.cs — HideDialog
8. IndependentScale.cs — namespace

---

## Зависимости

- **Предшествующие:** Fix-01 (Qi types), Fix-08 (InventoryController для интеграции)
- **Последующие:** нет

---

*Чекпоинт создан: 2026-04-10 12:55:00 UTC*

# 🐛 Чекпоинт: Исправление ошибок компиляции

**Дата:** 2026-04-27 18:40 UTC
**Статус:** ✅ complete (2 итерации, UnityEditor запущен успешно)
**Цель:** Исправить ошибки компиляции после переработки инвентаря на строчную модель (коммит f756a24)

---

## ИТЕРАЦИЯ 1 — первый запуск UnityEditor после переработки

**Коммит:** f838919 (2026-04-27 18:36 UTC)
**Контекст:** После массовой переработки инвентаря (коммит f756a24 — 4 переписи + 14 редактирований)
первый запуск UnityEditor выявил 4 ошибки компиляции.

### Ошибки итерации 1

| # | Файл | Ошибка | Причина |
|---|------|--------|---------|
| 1 | SaveManager.cs | CS1503: несовместимый тип аргумента — EquipmentData вместо Dictionary | LoadSaveData ожидает Dictionary<string, EquipmentData>, передавался itemDatabase (Dictionary<string, ItemData>) |
| 2 | DragDropHandler.cs:157 | CS1061: GetEquippedItem не найден | Метод переименован в GetEquipment при переписи EquipmentController |
| 3 | IntegrationTestScenarios.cs | CS1061: InventorySlotSaveData не содержит gridX/gridY | Поля удалены из SaveData при переходе на строчную модель |
| 4 | DragDropHandler.cs (ссылка) | CS0246: тип ContextMenuUI не найден | Класс ContextMenuUI ещё не существовал — был в legacy InventoryUI.cs (удалён) |

### Правки итерации 1

#### Правка 1: SaveManager.cs — EquipmentDatabase для LoadSaveData
**Что:** Добавлен метод `BuildEquipmentDatabase()` и изменён вызов:
```csharp
// Было (ломалось):
equipmentController.LoadSaveData(equipDict, realItemDatabase);
// realItemDatabase = Dictionary<string, ItemData> — несоответствие типа

// Стало:
var equipDatabase = BuildEquipmentDatabase(); // Dictionary<string, EquipmentData>
equipmentController.LoadSaveData(equipDict, equipDatabase);
```
**Почему:** EquipmentController.LoadSaveData ожидает `Dictionary<string, EquipmentData>`,
а передавался `Dictionary<string, ItemData>` (itemDatabase). ItemData — базовый класс,
EquipmentData — наследник. C# не поддерживает ковариантность в generic-параметрах Dictionary.
**Риск:** Нет — BuildEquipmentDatabase собирает только EquipmentData-ассеты через Resources.FindObjectsOfTypeAll
**Откат:** Вернуть itemDatabase + изменить сигнатуру LoadSaveData (хуже — теряет типизацию)

Также: `craftingController.LoadSaveData(data.CraftingData, realItemDatabase)` → `LoadSaveData(data.CraftingData)`
(убран лишний аргумент — CraftingController.LoadSaveData не принимает itemDatabase)

#### Правка 2: DragDropHandler.cs — GetEquippedItem → GetEquipment
**Что:** `equipmentController.GetEquippedItem(slot)` → `equipmentController.GetEquipment(slot)`
**Почему:** Метод переименован при переписи EquipmentController (строчная модель)
**Риск:** Нет — прямое переименование, семантика идентична
**Откат:** Вернуть старое имя метода в EquipmentController (НЕ рекомендуется)

#### Правка 3: IntegrationTestScenarios.cs — удалены gridX/gridY
**Что:** Убраны поля `gridX = 0, gridY = 0` из InventorySlotSaveData в тестовых данных
**Почему:** InventorySlotSaveData больше не содержит gridX/gridY (сетка удалена)
**Риск:** Нет — поля не имели смысла в строчной модели
**Откат:** Вернуть поля в InventorySlotSaveData (НЕ рекомендуется — легаси)

#### Правка 4: ContextMenuUI.cs — новый файл (117 строк)
**Что:** Создан `Assets/Scripts/UI/ContextMenuUI.cs` — минимальная реализация контекстного меню
**Почему:** DragDropHandler ссылался на ContextMenuUI, но класс не существовал.
Он был внутри legacy InventoryUI.cs (874 строки, удалён как часть переработки).
Выделен как самостоятельный компонент.
**Риск:** Минимальный — новая реализация упрощённая, но покрывает потребность DragDropHandler
**Откат:** Удалить файл + восстановить InventoryUI.cs (НЕ рекомендуется — легаси)

---

## ИТЕРАЦИЯ 2 — второй запуск UnityEditor

**Коммит:** f0b5b77 (2026-04-27 18:40 UTC)
**Контекст:** После фиксов итерации 1 обнаружены ещё 12 ошибок — не обновлённые
зависимости от sizeWidth/sizeHeight и некорректные API в Phase17InventoryUI.cs.

### Ошибки итерации 2

| # | Файл | Строка | Ошибка | Причина |
|---|------|-------:|--------|---------|
| 1 | AssetGeneratorExtended.cs | 432 | CS1061: ItemData.sizeWidth | Поле удалено в ItemData v2.0 |
| 2 | AssetGeneratorExtended.cs | 433 | CS1061: ItemData.sizeHeight | Поле удалено в ItemData v2.0 |
| 3 | Phase17InventoryUI.cs | 390 | CS0117: TextAlignmentOptions.MiddleRight | Несуществующий enum-член TMP |
| 4 | Phase17InventoryUI.cs | 397 | CS0117: TextAlignmentOptions.MiddleRight | Несуществующий enum-член TMP |
| 5 | Phase17InventoryUI.cs | 404 | CS0117: TextAlignmentOptions.MiddleRight | Несуществующий enum-член TMP |
| 6 | AssetGeneratorExtended.cs | 620 | CS1061: StorageRingData.sizeWidth | Унаследовано от ItemData, поле удалено |
| 7 | AssetGeneratorExtended.cs | 621 | CS1061: StorageRingData.sizeHeight | Унаследовано от ItemData, поле удалено |
| 8 | Phase17InventoryUI.cs | 767 | CS1061: ScrollRect.scrollbarVisibility | Несуществующее свойство |
| 9 | AssetGeneratorExtended.cs | 1148 | CS1061: ItemData.sizeWidth | Поле удалено в ItemData v2.0 |
| 10 | AssetGeneratorExtended.cs | 1148 | CS1061: ItemData.sizeHeight | Поле удалено в ItemData v2.0 |
| 11 | AssetGeneratorExtended.cs | 1150 | CS1061: ItemData.sizeWidth | Поле удалено в ItemData v2.0 |
| 12 | AssetGeneratorExtended.cs | 1150 | CS1061: ItemData.sizeHeight | Поле удалено в ItemData v2.0 |

---

## 🔍 АНАЛИЗ КОРНЕВЫХ ПРИЧИН

### Группа 1: sizeWidth/sizeHeight — 8 ошибок
**Корень:** ItemData.cs переписан (v2.0, 2026-04-27 18:06) — поля sizeWidth/sizeHeight удалены
как часть перехода на строчную модель инвентаря (1 предмет = 1 строка, масса + объём).
**Почему не обновлены ссылки:** AssetGeneratorExtended.cs и ItemJson DTO не были обновлены
одновременно с ItemData.cs — редактирование ItemData было в рамках "✏️ РЕДАКТИРОВАНИЕ #5",
но зависимости не были полностью проверены.

**Цепочка наследования:** StorageRingData → EquipmentData → ItemData
Все три класса не имеют sizeWidth/sizeHeight после правки ItemData.

**Решение:** Удалить присвоения sizeWidth/sizeHeight из AssetGeneratorExtended.cs,
удалить валидацию размера, удалить поля из ItemJson DTO.

**Почему это правильное решение (а не возврат полей):**
- volume уже существует в ItemData (добавлен ранее)
- Строчная модель не использует размер ячейки — только масса + объём
- Возврат sizeWidth/sizeHeight создаст легаси-код, противоречащий архитектурному решению

### Группа 2: TextAlignmentOptions.MiddleRight — 3 ошибки
**Корень:** TextMeshPro не имеет `MiddleRight` в enum TextAlignmentOptions.
Правильное значение: `MidlineRight` (горизонтальное выравнивание Right + вертикальное Midline).

**Справка по TextAlignmentOptions:**
- TopLeft, Top, TopRight
- Left, Center, Right (он же MidlineCenter)
- MidlineLeft, MidlineRight ← ПРАВИЛЬНЫЙ ВАРИАНТ
- BottomLeft, Bottom, BottomRight
- BaselineLeft, Baseline, BaselineRight
- CaplineLeft, Capline, CaplineRight

**Почему ошибка возникла:** При написании Phase17InventoryUI.cs использовано логичное
но несуществующее имя MiddleRight вместо документированного MidlineRight.

### Группа 3: ScrollRect.scrollbarVisibility — 1 ошибка
**Корень:** ScrollRect не имеет свойства `scrollbarVisibility`.
Правильные свойства: `verticalScrollbarVisibility` и `horizontalScrollbarVisibility`,
тип — `ScrollRect.ScrollbarVisibility`.

**Доступные значения ScrollRect.ScrollbarVisibility:**
- Permanent (0) — всегда виден
- AutoHide (1) — скрывается когда не нужен
- AutoHideAndExpandViewport (2) — скрывается + расширяет viewport

**Решение:** Заменить `scrollbarVisibility` → `verticalScrollbarVisibility`.

---

### Правки итерации 2

#### Правка 5: AssetGeneratorExtended.cs — удаление sizeWidth/sizeHeight (строки 432-433)
**Что:** Убрать `asset.sizeWidth = data.sizeWidth;` и `asset.sizeHeight = data.sizeHeight;`
**Почему:** Поля удалены из ItemData v2.0, volume рассчитывается автоматически
**Риск:** Нет — volume уже вычисляется на строке 443
**Откат:** Вернуть строки + вернуть поля в ItemData (НЕ рекомендуется — легаси)

#### Правка 6: AssetGeneratorExtended.cs — удаление sizeWidth/sizeHeight для StorageRingData (строки 620-621)
**Что:** Убрать `asset.sizeWidth = 1;` и `asset.sizeHeight = 1;`
**Почему:** StorageRingData наследует от EquipmentData→ItemData, поля удалены
**Риск:** Нет — volume уже задаётся на строке 631
**Откат:** Вернуть строки + вернуть поля в ItemData (НЕ рекомендуется)

#### Правка 7: AssetGeneratorExtended.cs — удаление валидации размера (строки 1147-1151)
**Что:** Убрать блок проверки `if (asset.sizeWidth <= 0 || asset.sizeHeight <= 0)`
**Почему:** Поля не существуют, валидация бессмысленна. Заменена на валидацию volume (если <= 0)
**Риск:** Нет — потерянная валидация компенсируется проверкой volume
**Откат:** Вернуть блок + вернуть поля (НЕ рекомендуется)

#### Правка 8: AssetGeneratorExtended.cs — удаление полей ItemJson DTO (строки 1402-1403)
**Что:** Убрать `public int sizeWidth;` и `public int sizeHeight;` из ItemJson
**Почему:** DTO должен соответствовать актуальной модели данных
**Риск:** Нет — JSON без этих полей будет десериализован корректно (default 0)
**Откат:** Вернуть поля (безвредно, но бессмысленно)

#### Правка 9: Phase17InventoryUI.cs — MiddleRight → MidlineRight (строки 390, 397, 404)
**Что:** Заменить `TextAlignmentOptions.MiddleRight` → `TextAlignmentOptions.MidlineRight`
**Почему:** MiddleRight не существует в TMP API, MidlineRight — правильное значение
**Риск:** Нет — визуально идентичный результат
**Откат:** Если нужен другой вариант выравнивания — использовать BaselineRight или Right

#### Правка 10: Phase17InventoryUI.cs — scrollbarVisibility → verticalScrollbarVisibility (строка 767)
**Что:** Заменить `scrollRectComp.scrollbarVisibility` → `scrollRectComp.verticalScrollbarVisibility`
**Почему:** ScrollRect не имеет scrollbarVisibility, только vertical/horizontalScrollbarVisibility
**Риск:** Нет — поведение идентичное (AutoHide для вертикального скроллбара)
**Откат:** Нет корректного отката — scrollbarVisibility не существует

---

## ⚠️ ДОПОЛНИТЕЛЬНО: ConsumableGenerator.cs
Файл `ConsumableGenerator.cs` содержит sizeWidth/sizeHeight в собственном внутреннем классе
(строки 92-93) и использует их (247-248, 614). Это НЕ вызывает ошибок компиляции, т.к.
поля определены в собственном классе генератора, а не в ItemData.
**Решение:** Не трогать сейчас, отметить для будущего обновления при переработке генераторов.

---

*Создано: 2026-04-27 18:40 UTC*
*Редактировано: 2026-04-27 18:50 UTC — добавлена итерация 1 (фиксы f838919)*

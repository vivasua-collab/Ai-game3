# 🐛 Чекпоинт: Исправление ошибок компиляции

**Дата:** 2026-04-27 18:40 UTC
**Статус:** ✅ complete
**Цель:** Исправить 12 ошибок компиляции, возникших после переписи ItemData.cs (убраны sizeWidth/sizeHeight) и написания Phase17InventoryUI.cs (некорректные API)

---

## 📋 СПИСОК ОШИБОК

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

## 📝 ИСТОРИЯ ПРАВОК

### Правка 1: AssetGeneratorExtended.cs — удаление sizeWidth/sizeHeight (строки 432-433)
**Что:** Убрать `asset.sizeWidth = data.sizeWidth;` и `asset.sizeHeight = data.sizeHeight;`
**Почему:** Поля удалены из ItemData v2.0, volume рассчитывается автоматически
**Риск:** Нет — volume уже вычисляется на строке 443
**Откат:** Вернуть строки + вернуть поля в ItemData (НЕ рекомендуется — легаси)

### Правка 2: AssetGeneratorExtended.cs — удаление sizeWidth/sizeHeight для StorageRingData (строки 620-621)
**Что:** Убрать `asset.sizeWidth = 1;` и `asset.sizeHeight = 1;`
**Почему:** StorageRingData наследует от EquipmentData→ItemData, поля удалены
**Риск:** Нет — volume уже задаётся на строке 631
**Откат:** Вернуть строки + вернуть поля в ItemData (НЕ рекомендуется)

### Правка 3: AssetGeneratorExtended.cs — удаление валидации размера (строки 1147-1151)
**Что:** Убрать блок проверки `if (asset.sizeWidth <= 0 || asset.sizeHeight <= 0)`
**Почему:** Поля не существуют, валидация бессмысленна. Вместо этого добавим
валидацию volume (если <= 0)
**Риск:** Нет — потерянная валидация компенсируется проверкой volume
**Откат:** Вернуть блок + вернуть поля (НЕ рекомендуется)

### Правка 4: AssetGeneratorExtended.cs — удаление полей ItemJson DTO (строки 1402-1403)
**Что:** Убрать `public int sizeWidth;` и `public int sizeHeight;` из ItemJson
**Почему:** DTO должен соответствовать актуальной модели данных
**Риск:** Нет — JSON без этих полей будет десериализован корректно (default 0)
**Откат:** Вернуть поля (безвредно, но бессмысленно)

### Правка 5: Phase17InventoryUI.cs — MiddleRight → MidlineRight (строки 390, 397, 404)
**Что:** Заменить `TextAlignmentOptions.MiddleRight` → `TextAlignmentOptions.MidlineRight`
**Почему:** MiddleRight не существует в TMP API, MidlineRight — правильное значение
**Риск:** Нет — визуально идентичный результат
**Откат:** Если нужен другой вариант выравнивания — использовать BaselineRight или Right

### Правка 6: Phase17InventoryUI.cs — scrollbarVisibility → verticalScrollbarVisibility (строка 767)
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

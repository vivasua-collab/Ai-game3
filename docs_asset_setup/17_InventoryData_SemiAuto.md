# Настройка данных инвентаря (Полуавтомат)

**Инструмент:** `Tools → Full Scene Builder → Phase 16: Inventory Data`
**Спецификация:** `docs_temp/INVENTORY_UI_DRAFT.md` v2.0

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание папок Backpacks/ и StorageRings/ | ✅ Автоматически |
| Создание 4 BackpackData .asset | ✅ Автоматически |
| Создание 4 StorageRingData .asset | ✅ Автоматически |
| Заполнение всех полей (itemId, nameRu, volume и т.д.) | ✅ Автоматически |
| Обновление ItemData .asset: поле volume | ✅ Автоматически |
| Обновление ItemData .asset: поле allowNesting | ✅ Автоматически |
| Добавление иконок спрайтов | ❌ Руками |

---

## Шаг 1: Запуск генератора (АВТОМАТИЧЕСКИ)

**Меню:** `Tools → Full Scene Builder → Phase 16: Inventory Data`

**Предусловие:** Фаза 09 (Generate Assets) уже выполнена — ItemData .asset файлы существуют.

**Результат:**
```
Assets/Data/Backpacks/
├── Backpack_ClothSack.asset      # 3×4, 0% снижение веса
├── Backpack_LeatherPack.asset    # 4×5, 10% снижение веса
├── Backpack_IronContainer.asset  # 5×5, 15% снижение веса
└── Backpack_SpiritBag.asset      # 6×6, 25% снижение веса

Assets/Data/StorageRings/
├── StorageRing_Slit.asset        # maxVolume=5
├── StorageRing_Pocket.asset      # maxVolume=15
├── StorageRing_Vault.asset       # maxVolume=30
└── StorageRing_Space.asset       # maxVolume=60
```

Также обновляются все существующие ItemData в `Assets/Data/Items/` и `Assets/Data/Equipment/`:
- `volume` = значение по категории (0.1 для пилюль, 1-4 для оружия/брони и т.д.)
- `allowNesting` = `Any` (по умолчанию)

---

## Шаг 2: Проверка (ВРУЧНУЮ)

1. Откройте `Assets/Data/Backpacks/Backpack_ClothSack.asset` в Inspector
2. Проверьте: gridWidth=3, gridHeight=4, weightReduction=0, beltSlots=0
3. Откройте `Assets/Data/StorageRings/StorageRing_Slit.asset`
4. Проверьте: maxVolume=5, qiCostBase=5, qiCostPerUnit=3

---

## Сводная таблица рюкзаков

| Рюкзак | Сетка | Снижение веса | Бонус веса | Слоты пояса | Редкость |
|--------|-------|---------------|-----------|-------------|----------|
| Тканевая сумка | 3×4 (12) | 0% | +0 кг | 0 | Common |
| Кожаный ранец | 4×5 (20) | 10% | +10 кг | 1 | Uncommon |
| Железный контейнер | 5×5 (25) | 15% | +20 кг | 2 | Rare |
| Духовный мешок | 6×6 (36) | 25% | +30 кг | 2 | Epic |

---

## Сводная таблица колец хранения

| Кольцо | Объём | Qi базовая | Qi за единицу | Время доступа | Редкость |
|--------|-------|-----------|---------------|---------------|----------|
| Кольцо-щель | 5 | 5 | ×3 | 1.5 сек | Uncommon |
| Кольцо-карман | 15 | 5 | ×2 | 1.5 сек | Rare |
| Кольцо-кладовая | 30 | 5 | ×1 | 1.5 сек | Epic |
| Кольцо-пространство | 60 | 5 | ×0.5 | 1.5 сек | Legendary |

---

## Шаг 3: Добавление иконок (ВРУЧНУЮ)

Для каждого .asset назначьте спрайт в поле `icon`:
- Рюкзаки: иконки контейнеров (коричневые)
- Кольца: иконки колец (по цвету редкости)

Если спрайтов нет — оставьте пустым. UI покажет цветной квадрат с первой буквой.

---

## Значения volume для существующих предметов

При запуске Phase 16 автоматически обновляются:

| Категория предметов | volume | allowNesting |
|---------------------|--------|--------------|
| Пилюли (4 шт.) | 0.1 | Any |
| Еда (2 шт.) | 0.5 | Any |
| Противоядие | 0.1 | Any |
| Свиток техники | 0.3 | Any |
| Оружие одноручное | 1.0-2.0 | Any |
| Оружие двуручное | 3.0-4.0 | Any |
| Броня тяжёлая | 3.0-4.0 | Any |
| Броня лёгкая | 1.0-2.0 | Any |
| Аксессуары | 0.1-0.3 | Any |
| Материалы | 0.5-1.0 | Any |

---

## Использование в коде

```csharp
// Загрузка BackpackData
var backpack = Resources.Load<BackpackData>("Data/Backpacks/Backpack_ClothSack");

// Установка рюкзака
inventoryController.SetBackpack(backpack);

// Загрузка StorageRingData
var ring = Resources.Load<StorageRingData>("Data/StorageRings/StorageRing_Slit");

// Экипировка кольца
storageRingController.SetRing(ring);
```

---

*Документ создано: 2026-04-19 06:25:00 UTC*

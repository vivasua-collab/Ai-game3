# Настройка данных инвентаря (Полуавтомат)

**Инструмент:** `Tools → Full Scene Builder → Phase 16: Inventory Data`
**Спецификация:** `docs_temp/INVENTORY_UI_DRAFT.md` v3.0

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание папок Backpacks/ и StorageRings/ | ✅ Автоматически |
| Создание 5 BackpackData .asset | ✅ Автоматически |
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
├── Backpack_ClothSack.asset      # maxWeight=15, maxVolume=20, 0% снижение веса
├── Backpack_LeatherPack.asset    # maxWeight=25, maxVolume=35, 10% снижение веса
├── Backpack_IronContainer.asset  # maxWeight=50, maxVolume=60, 15% снижение веса
├── Backpack_SpiritBag.asset      # maxWeight=80, maxVolume=100, 25% снижение веса
└── Backpack_SpatialChest.asset   # maxWeight=150, maxVolume=200, 40% снижение веса

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
2. Проверьте: maxWeight=15, maxVolume=20, weightReduction=0, beltSlots=0
3. Откройте `Assets/Data/Backpacks/Backpack_SpatialChest.asset` в Inspector
4. Проверьте: maxWeight=150, maxVolume=200, weightReduction=40, beltSlots=3
5. Откройте `Assets/Data/StorageRings/StorageRing_Slit.asset`
6. Проверьте: maxVolume=5, qiCostBase=5, qiCostPerUnit=3

---

## Сводная таблица рюкзаков

| Рюкзак | Макс. вес | Макс. объём | Снижение веса | Слоты пояса | Редкость |
|--------|-----------|-------------|---------------|-------------|----------|
| Тканевая сумка | 15 кг | 20 л | 0% | 0 | Common |
| Кожаный ранец | 25 кг | 35 л | 10% | 1 | Uncommon |
| Железный контейнер | 50 кг | 60 л | 15% | 2 | Rare |
| Духовный мешок | 80 кг | 100 л | 25% | 2 | Epic |
| Пространственный сундук | 150 кг | 200 л | 40% | 3 | Legendary |

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
*Редактировано: 2026-04-27 18:00:00 MSK — Миграция на линейную модель инвентаря (maxWeight/maxVolume), добавлен SpatialChest, удалены gridWidth/gridHeight*

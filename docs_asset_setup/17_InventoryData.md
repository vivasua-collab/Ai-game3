# Настройка данных инвентаря (Вручную)

**Категория:** Инвентарь  
**Спецификация:** `docs_temp/INVENTORY_UI_DRAFT.md` v2.0  
**Редактировано:** 2026-04-27

---

## Обзор

Система инвентаря требует три типа данных:
1. **BackpackData** — рюкзаки (ограничения по массе и объёму)
2. **StorageRingData** — кольца хранения (объём-ограниченные)
3. **Обновление ItemData** — поле `volume` (поле `allowNesting` сохранено)

> ⚠️ **Строчная модель (2026-04-27):** BackpackData переведён с сеточной модели (gridWidth×gridHeight) на строчную (maxWeight / maxVolume / ownWeight). Поля sizeWidth/sizeHeight удалены из ItemData.

---

## Шаг 1: Создать папки

В Project window → правый клик → Create → Folder:

```
Assets/Data/Backpacks/
Assets/Data/StorageRings/
```

---

## Шаг 2: Создать BackpackData ассеты

Для каждого рюкзака: правый клик в `Assets/Data/Backpacks/` → Create → Cultivation → Backpack

### 2.1 Тканевая сумка (стартовая)

| Поле | Значение |
|------|----------|
| fileName | `Backpack_ClothSack` |
| itemId | `backpack_cloth_sack` |
| nameRu | Тканевая сумка |
| nameEn | Cloth Sack |
| description | Простейшая тканевая сумка. Вмещает немного вещей. |
| category | Misc |
| rarity | Common |
| stackable | ✗ |
| ~~sizeWidth~~ | ~~2~~ ⚠️ Удалено |
| ~~sizeHeight~~ | ~~2~~ ⚠️ Удалено |
| weight | 0.5 |
| value | 10 |
| hasDurability | ✗ |
| volume | 2.0 |
| allowNesting | Any |
| **maxWeight** | **30** |
| **maxVolume** | **50** |
| **weightReduction** | **0** |
| **beltSlots** | **0** |
| **ownWeight** | **0.5** |

### 2.2 Кожаный ранец

| Поле | Значение |
|------|----------|
| fileName | `Backpack_LeatherPack` |
| itemId | `backpack_leather_pack` |
| nameRu | Кожаный ранец |
| nameEn | Leather Pack |
| description | Надёжный кожаный ранец. Снижает вес содержимого на 10%. |
| category | Misc |
| rarity | Uncommon |
| stackable | ✗ |
| ~~sizeWidth~~ | ~~2~~ ⚠️ Удалено |
| ~~sizeHeight~~ | ~~2~~ ⚠️ Удалено |
| weight | 2.0 |
| value | 100 |
| hasDurability | ✗ |
| volume | 3.0 |
| allowNesting | Any |
| **maxWeight** | **50** |
| **maxVolume** | **80** |
| **weightReduction** | **10** |
| **beltSlots** | **1** |
| **ownWeight** | **2.0** |

### 2.3 Железный контейнер

| Поле | Значение |
|------|----------|
| fileName | `Backpack_IronContainer` |
| itemId | `backpack_iron_container` |
| nameRu | Железный контейнер |
| nameEn | Iron Container |
| description | Прочный железный контейнер. Значительно снижает вес. |
| category | Misc |
| rarity | Rare |
| stackable | ✗ |
| ~~sizeWidth~~ | ~~2~~ ⚠️ Удалено |
| ~~sizeHeight~~ | ~~2~~ ⚠️ Удалено |
| weight | 5.0 |
| value | 500 |
| hasDurability | ✓ |
| maxDurability | 200 |
| volume | 4.0 |
| allowNesting | Any |
| **maxWeight** | **80** |
| **maxVolume** | **120** |
| **weightReduction** | **15** |
| **beltSlots** | **2** |
| **ownWeight** | **5.0** |

### 2.4 Духовный мешок

| Поле | Значение |
|------|----------|
| fileName | `Backpack_SpiritBag` |
| itemId | `backpack_spirit_bag` |
| nameRu | Духовный мешок |
| nameEn | Spirit Bag |
| description | Мешок, сотканный из духовной энергии. Большой и лёгкий. |
| category | Misc |
| rarity | Epic |
| stackable | ✗ |
| ~~sizeWidth~~ | ~~2~~ ⚠️ Удалено |
| ~~sizeHeight~~ | ~~2~~ ⚠️ Удалено |
| weight | 3.0 |
| value | 2000 |
| hasDurability | ✗ |
| volume | 5.0 |
| allowNesting | Any |
| **maxWeight** | **120** |
| **maxVolume** | **200** |
| **weightReduction** | **25** |
| **beltSlots** | **2** |
| **ownWeight** | **3.0** |

### 2.5 Пространственный сундук

| Поле | Значение |
|------|----------|
| fileName | `Backpack_SpatialChest` |
| itemId | `backpack_spatial_chest` |
| nameRu | Пространственный сундук |
| nameEn | Spatial Chest |
| description | Мифический сундук с пространственным карманом. Огромная вместимость при минимальном весе. |
| category | Misc |
| rarity | Legendary |
| stackable | ✗ |
| ~~sizeWidth~~ | ~~—~~ ⚠️ Удалено |
| ~~sizeHeight~~ | ~~—~~ ⚠️ Удалено |
| weight | 1.0 |
| value | 10000 |
| hasDurability | ✗ |
| volume | 5.0 |
| allowNesting | Any |
| **maxWeight** | **200** |
| **maxVolume** | **500** |
| **weightReduction** | **40** |
| **beltSlots** | **4** |
| **ownWeight** | **1.0** |

---

## Шаг 3: Создать StorageRingData ассеты

Для каждого кольца: правый клик в `Assets/Data/StorageRings/` → Create → Cultivation → Storage Ring

### 3.1 Кольцо-щель

| Поле | Значение |
|------|----------|
| fileName | `StorageRing_Slit` |
| itemId | `storage_ring_slit` |
| nameRu | Кольцо-щель |
| nameEn | Ring-Slit |
| description | Маленькое пространственное кольцо. Вмещает немного. |
| category | Accessory |
| rarity | Uncommon |
| stackable | ✗ |
| ~~sizeWidth~~ | ~~1~~ ⚠️ Удалено |
| ~~sizeHeight~~ | ~~1~~ ⚠️ Удалено |
| weight | 0.1 |
| value | 200 |
| volume | 0.3 |
| allowNesting | **None** |
| **maxVolume** | **5** |
| **qiCostBase** | **5** |
| **qiCostPerUnit** | **3** |
| **accessTime** | **1.5** |

### 3.2 Кольцо-карман

| Поле | Значение |
|------|----------|
| fileName | `StorageRing_Pocket` |
| itemId | `storage_ring_pocket` |
| nameRu | Кольцо-карман |
| nameEn | Ring-Pocket |
| description | Пространственное кольцо среднего объёма. |
| category | Accessory |
| rarity | Rare |
| stackable | ✗ |
| ~~sizeWidth~~ | ~~1~~ ⚠️ Удалено |
| ~~sizeHeight~~ | ~~1~~ ⚠️ Удалено |
| weight | 0.1 |
| value | 800 |
| volume | 0.3 |
| allowNesting | **None** |
| **maxVolume** | **15** |
| **qiCostBase** | **5** |
| **qiCostPerUnit** | **2** |
| **accessTime** | **1.5** |

### 3.3 Кольцо-кладовая

| Поле | Значение |
|------|----------|
| fileName | `StorageRing_Vault` |
| itemId | `storage_ring_vault` |
| nameRu | Кольцо-кладовая |
| nameEn | Ring-Vault |
| description | Вместительное пространственное кольцо. |
| category | Accessory |
| rarity | Epic |
| stackable | ✗ |
| ~~sizeWidth~~ | ~~1~~ ⚠️ Удалено |
| ~~sizeHeight~~ | ~~1~~ ⚠️ Удалено |
| weight | 0.1 |
| value | 3000 |
| volume | 0.3 |
| allowNesting | **None** |
| **maxVolume** | **30** |
| **qiCostBase** | **5** |
| **qiCostPerUnit** | **1** |
| **accessTime** | **1.5** |

### 3.4 Кольцо-пространство

| Поле | Значение |
|------|----------|
| fileName | `StorageRing_Space` |
| itemId | `storage_ring_space` |
| nameRu | Кольцо-пространство |
| nameEn | Ring-Space |
| description | Мифическое кольцо с огромным пространством внутри. |
| category | Accessory |
| rarity | Legendary |
| stackable | ✗ |
| ~~sizeWidth~~ | ~~1~~ ⚠️ Удалено |
| ~~sizeHeight~~ | ~~1~~ ⚠️ Удалено |
| weight | 0.1 |
| value | 10000 |
| volume | 0.3 |
| allowNesting | **None** |
| **maxVolume** | **60** |
| **qiCostBase** | **5** |
| **qiCostPerUnit** | **0.5** |
| **accessTime** | **1.5** |

---

## Шаг 4: Обновить ItemData — поля volume и allowNesting

Для **каждого** существующего ItemData .asset в `Assets/Data/Items/` и `Assets/Data/Equipment/`:

### Значения volume по умолчанию

| Категория | volume | allowNesting |
|-----------|--------|--------------|
| Consumable (pill) | 0.1 | Any |
| Food | 0.5 | Any |
| Scroll (1×2) | 0.3 | Any |
| Weapon (1H) | 1.0 | Any |
| Weapon (2H) | 3.0-4.0 | Any |
| Armor (light) | 1.0-2.0 | Any |
| Armor (heavy) | 3.0-4.0 | Any |
| Material (ore, wood) | 0.5-1.0 | Any |
| Quest item | 0.1 | **None** |

### Конкретные значения для существующих предметов

**Items (8 шт.):**

| Предмет | volume | allowNesting |
|---------|--------|--------------|
| Лечебная пилюля | 0.1 | Any |
| Пилюля Ци | 0.1 | Any |
| Пилюля выносливости | 0.1 | Any |
| Пилюля прорыва | 0.1 | Any |
| Хлеб | 0.5 | Any |
| Мясо | 0.5 | Any |
| Противоядие | 0.1 | Any |
| Свиток техники | 0.3 | Any |

**Equipment (39 шт.):** volume зависит от типа:
- Одноручное оружие: 1.0-2.0
- Двуручное оружие: 3.0-4.0
- Броня (нагрудник): 3.0-4.0
- Броня (лёгкая, перчатки, сапоги): 1.0-2.0
- Аксессуар: 0.1-0.3
- Все allowNesting = Any

**Materials (17 шт.):** volume = 0.5-1.0, allowNesting = Any

---

## Результат

```
Assets/Data/Backpacks/
├── Backpack_ClothSack.asset
├── Backpack_LeatherPack.asset
├── Backpack_IronContainer.asset
├── Backpack_SpiritBag.asset
└── Backpack_SpatialChest.asset

Assets/Data/StorageRings/
├── StorageRing_Slit.asset
├── StorageRing_Pocket.asset
├── StorageRing_Vault.asset
└── StorageRing_Space.asset
```

---

*Документ создано: 2026-04-19 06:25:00 UTC*  
*Редактировано: 2026-04-27 — Переход на строчную модель (grid→maxWeight/maxVolume), удаление sizeWidth/sizeHeight, добавление SpatialChest*

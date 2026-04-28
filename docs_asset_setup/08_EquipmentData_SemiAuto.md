# Настройка EquipmentData (Полуавтомат)

**Инструмент:** `Tools → Generate Assets → Equipment (39)`

---

## Что делает скрипт АВТОМАТИЧЕСКИ:

| Действие | Статус |
|----------|--------|
| Создание папки Equipment/ | ✅ Автоматически |
| Чтение JSON данных | ✅ Автоматически |
| Создание EquipmentData assets | ✅ Автоматически |
| Заполнение всех полей | ✅ Автоматически |
| Парсинг слотов экипировки | ✅ Автоматически |
| Расчёт volume (clamp(weight,1,4) / 0.2) | ✅ Автоматически |
| Установка allowNesting = NestingFlag.Any | ✅ Автоматически |
| Добавление иконок | ❌ Руками |

---

## Шаг 1: Запуск генератора (АВТОМАТИЧЕСКИ)

**Меню:** `Tools → Generate Assets → Equipment (39)`

**Результат:**
```
Assets/Data/Equipment/
├── Weapon_Fists.asset
├── Weapon_Iron_Dagger.asset
├── Weapon_Iron_Sword.asset
├── Weapon_Spirit_Sword.asset
├── ... (20 оружий)
├── Armor_Cloth_Robe.asset
├── Armor_Iron_Breastplate.asset
├── Armor_Spirit_Robe.asset
├── ... (19 бронь)
```

---

## Сводная таблица оружия (20)

| ID | Название | Урон | Слот | Требования | Объём |
|----|----------|------|------|------------|-------|
| weapon_unarmed_fists | Кулаки | 1-3 | hands | — | 1 |
| weapon_dagger_iron | Железный кинжал | 4-8 | weapon_main/off | Str 5, Agi 8 | 1 |
| weapon_sword_iron | Железный меч | 8-14 | weapon_main | Str 10, Agi 8 | 2.5 |
| weapon_sword_spirit | Духовный меч | 18-30 | weapon_main | L3, Str 25 | 2.0 |
| weapon_greatsword_iron | Железный двуручник | 18-30 | weapon_twohanded | Str 18 | 4 |
| weapon_axe_iron | Железный топор | 10-18 | weapon_main | Str 14 | 3.5 |
| weapon_spear_iron | Железное копьё | 12-20 | weapon_twohanded | Str 12 | 3.0 |
| weapon_bow_wood | Деревянный лук | 6-12 | weapon_twohanded | Agi 12 | 1.2 |
| weapon_staff_wood | Деревянный посох | 3-7 | weapon_twohanded | L1 | 1.5 |
| ... | ... | ... | ... | ... | ... |

---

## Сводная таблица брони (19)

| ID | Название | Защита | Слот | Покрытие | Объём |
|----|----------|--------|------|----------|-------|
| armor_torso_cloth_robe | Тканевая роба | 3 | torso_clothing | 60% | 1 |
| armor_torso_leather_vest | Кожаный жилет | 10 | torso_armor | 70% | 2.0 |
| armor_torso_iron_plate | Железный нагрудник | 25 | torso_armor | 80% | 4 |
| armor_torso_spirit_robe | Духовная роба | 15 | torso_clothing | 75% | 1 |
| armor_helmet_iron | Железный шлем | 12 | head_armor | 75% | 2.5 |
| ... | ... | ... | ... | ... | ... |

---

## Система слотов

| Слот | Описание |
|------|----------|
| weapon_main | Основное оружие |
| weapon_off | Вторичное оружие |
| weapon_twohanded | Двуручное оружие |
| head_armor | Шлем (броня) |
| head_clothing | Головной убор (одежда) |
| torso_armor | Нагрудник (броня) |
| torso_clothing | Роба (одежда) |
| legs_armor | Поножи (броня) |
| legs_clothing | Штаны (одежда) |
| feet_armor | Сабатоны (броня) |
| feet_clothing | Обувь (одежда) |
| hands_armor | Перчатки (броня) |
| hands_clothing | Перчатки (одежда) |

---

*Документ создан: 2026-04-02*
*Обновлено: 2026-04-28 — Миграция инвентаря (сетка→строка): добавлены поля volume и allowNesting, обновлены таблицы*

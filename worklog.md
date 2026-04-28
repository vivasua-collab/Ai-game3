---
Task ID: 1
Agent: Main
Task: Переработка структуры объектов и генераторов по результатам аудита

Work Log:
- Прочитаны оба чекпоинта: audit plan (7 проблем кода + 2 проблемы документации) и rework plan (4 этапа)
- Прочитаны все 6 исходных файлов: WeaponGenerator.cs, ArmorGenerator.cs, ConsumableGenerator.cs, Phase16InventoryData.cs, AssetGeneratorExtended.cs, ItemData.cs
- Проверены enum NestingFlag, ItemCategory в Enums.cs
- Прочитана документация EQUIPMENT_SYSTEM.md (§3.2, §8)

- P1: WeaponGenerator — добавлены BaseWeightBySubtype (12 записей), MaterialWeightMult (5 тиров), 6 инвентарных полей в DTO, расчёт веса в Generate(), вывод в GenerateExamples()
- P2: ArmorGenerator — добавлены BaseWeightBySubtypeAndClass (21 запись), MaterialWeightMult, 6 инвентарных полей в DTO, расчёт веса в Generate(), вывод в GenerateExamples()
- P3+P7: ConsumableGenerator — удалены sizeWidth/sizeHeight из DTO, SizeByType заменён на VolumeByType, добавлены volume/allowNesting/category в DTO
- P5: ConsumableGenerator — добавлено масштабирование веса по уровню: weight *= 1 + (level-1) × 0.1
- P4: Phase16InventoryData — volume = Mathf.Clamp(weight, 1f, 4f) вместо (damage > 0) ? 2f : 2f
- P6: AssetGeneratorExtended — ItemJson DTO +volume (sentinel -1f) и +allowNesting, условная логика в ApplyItemData(), добавлен ParseNestingFlag()
- D3: EQUIPMENT_SYSTEM.md §3.2 — добавлена строка volume материалов
- D7: EQUIPMENT_SYSTEM.md §8.4 — добавлен раздел «Объём экипировки» с формулами и примерами

- Проверено отсутствие ссылок на удалённые поля (sizeWidth, sizeHeight, SizeByType) в других файлах
- Создан чекпоинт: checkpoints/04_29_object_system_rework_complete.md

Stage Summary:
- Все 7 проблем кода (P1-P7) и 2 проблемы документации (D3, D7) исправлены
- 6 файлов изменено, ~156 строк кода
- Формулы веса и объёма единообразны: Weapon/Armor → clamp(weight, 1, 4), Consumable → 0.1
- Компиляция будет проверена при следующем запуске Unity Editor

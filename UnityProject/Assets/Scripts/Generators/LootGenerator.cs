// ============================================================================
// LootGenerator.cs — Runtime генератор лута
// Cultivation World Simulator
// Создано: 2026-04-29 09:32:00 UTC
// ============================================================================
//
// Этап 3 чекпоинта 04_29_equipment_generator_integration_plan.md
//
// Генерирует экипировку в рантайме (лут с монстров, награды, торговцы).
// Использует WeaponGenerator/ArmorGenerator для DTO
// и EquipmentSOFactory.CreateRuntimeFrom* для создания runtime SO.
//
// В отличие от EquipmentGeneratorMenu (Editor-меню),
// этот класс работает в рантайме БЕЗ AssetDatabase.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Runtime генератор лута.
    /// Создаёт EquipmentData SO в памяти (без .asset файлов на диске).
    /// Использует EquipmentSOFactory.CreateRuntimeFrom*.
    /// </summary>
    public static class LootGenerator
    {
        // ================================================================
        //  ГЕНЕРАЦИЯ ОДИНОЧНОГО ПРЕДМЕТА
        // ================================================================

        /// <summary>
        /// Сгенерировать случайный предмет экипировки как runtime SO.
        /// 50% шанс оружия, 50% шанс брони.
        /// </summary>
        /// <param name="playerLevel">Уровень игрока (влияет на itemLevel, materialTier, grade)</param>
        /// <param name="rng">Детерминированный генератор (null = случайный)</param>
        public static EquipmentData GenerateRandomEquipment(int playerLevel, SeededRandom rng = null)
        {
            rng ??= new SeededRandom();

            if (rng.NextBool(0.5f))
            {
                var dto = WeaponGenerator.GenerateForLevel(playerLevel, rng);
                return EquipmentSOFactory.CreateRuntimeFromWeapon(dto);
            }
            else
            {
                var dto = ArmorGenerator.GenerateForLevel(playerLevel, rng);
                return EquipmentSOFactory.CreateRuntimeFromArmor(dto);
            }
        }

        /// <summary>
        /// Сгенерировать случайное оружие как runtime SO.
        /// </summary>
        public static EquipmentData GenerateRandomWeapon(int playerLevel, SeededRandom rng = null)
        {
            rng ??= new SeededRandom();
            var dto = WeaponGenerator.GenerateForLevel(playerLevel, rng);
            return EquipmentSOFactory.CreateRuntimeFromWeapon(dto);
        }

        /// <summary>
        /// Сгенерировать случайную броню как runtime SO.
        /// </summary>
        public static EquipmentData GenerateRandomArmor(int playerLevel, SeededRandom rng = null)
        {
            rng ??= new SeededRandom();
            var dto = ArmorGenerator.GenerateForLevel(playerLevel, rng);
            return EquipmentSOFactory.CreateRuntimeFromArmor(dto);
        }

        // ================================================================
        //  ГЕНЕРАЦИЯ МАССИВА ЛУТА
        // ================================================================

        /// <summary>
        /// Сгенерировать массив лута по уровню.
        /// </summary>
        /// <param name="playerLevel">Уровень игрока</param>
        /// <param name="count">Количество предметов</param>
        /// <param name="rng">Детерминированный генератор</param>
        public static List<EquipmentData> GenerateLoot(int playerLevel, int count, SeededRandom rng = null)
        {
            rng ??= new SeededRandom();
            var result = new List<EquipmentData>(count);

            for (int i = 0; i < count; i++)
                result.Add(GenerateRandomEquipment(playerLevel, rng));

            return result;
        }

        /// <summary>
        /// Сгенерировать массив оружия.
        /// </summary>
        public static List<EquipmentData> GenerateWeaponLoot(int playerLevel, int count, SeededRandom rng = null)
        {
            rng ??= new SeededRandom();
            var result = new List<EquipmentData>(count);

            for (int i = 0; i < count; i++)
                result.Add(GenerateRandomWeapon(playerLevel, rng));

            return result;
        }

        /// <summary>
        /// Сгенерировать массив брони.
        /// </summary>
        public static List<EquipmentData> GenerateArmorLoot(int playerLevel, int count, SeededRandom rng = null)
        {
            rng ??= new SeededRandom();
            var result = new List<EquipmentData>(count);

            for (int i = 0; i < count; i++)
                result.Add(GenerateRandomArmor(playerLevel, rng));

            return result;
        }

        // ================================================================
        //  СМЕШАННЫЙ ЛУТ С КОНТРОЛЕМ СОСТАВА
        // ================================================================

        /// <summary>
        /// Сгенерировать смешанный лут с заданным соотношением оружия/брони.
        /// </summary>
        /// <param name="playerLevel">Уровень игрока</param>
        /// <param name="weaponCount">Количество оружия</param>
        /// <param name="armorCount">Количество брони</param>
        /// <param name="rng">Детерминированный генератор</param>
        public static List<EquipmentData> GenerateMixedLoot(
            int playerLevel, int weaponCount, int armorCount, SeededRandom rng = null)
        {
            rng ??= new SeededRandom();
            var result = new List<EquipmentData>(weaponCount + armorCount);

            for (int i = 0; i < weaponCount; i++)
                result.Add(GenerateRandomWeapon(playerLevel, rng));

            for (int i = 0; i < armorCount; i++)
                result.Add(GenerateRandomArmor(playerLevel, rng));

            // Перемешать, чтобы оружие и броня не шли группами
            rng.Shuffle(result);

            return result;
        }

        // ================================================================
        //  РАСХОДНИКИ (заглушка — TODO: ConsumableSOFactory)
        // ================================================================

        /// <summary>
        /// Генерация расходников (заглушка).
        /// TODO: Создать ConsumableSOFactory (аналог EquipmentSOFactory для ItemData).
        /// </summary>
        public static List<ItemData> GenerateConsumableLoot(int playerLevel, int count, SeededRandom rng = null)
        {
            // TODO: ConsumableSOFactory (аналог EquipmentSOFactory для ItemData)
            Debug.LogWarning("[LootGenerator] GenerateConsumableLoot: ConsumableSOFactory не реализован");
            return new List<ItemData>();
        }
    }
}

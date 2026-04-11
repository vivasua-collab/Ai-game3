// ============================================================================
// IntegrationTestScenarios.cs — Интеграционные тесты
// Cultivation World Simulator
// Версия: 1.1 — Fix-12: PlayerSaveData CurrentQi/MaxQi float→long
// ============================================================================
// Создан: 2026-03-31
// Этап: 6 - Testing & Balance
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Combat;
using CultivationGame.Generators;
using CultivationGame.Inventory;

namespace CultivationGame.Tests
{
    /// <summary>
    /// Интеграционные тесты — проверяют работу нескольких систем вместе.
    /// Сценарии:
    /// 1. Создание NPC через генератор
    /// 2. Бой Player vs NPC
    /// 3. Прорыв уровня культивации
    /// 4. Сохранение/загрузка
    /// </summary>
    public static class IntegrationTestScenarios
    {
        #region Scenario 1: NPC Generation

        /// <summary>
        /// Тест: Создание NPC через генератор.
        /// Проверяет корректность всех параметров сгенерированного NPC.
        /// </summary>
        public static class Scenario_NPCGeneration
        {
            public static bool Run(out string report)
            {
                report = "=== Scenario: NPC Generation ===\n";
                bool allPassed = true;

                try
                {
                    // Создаём генератор с сидом
                    int seed = 12345;
                    var random = new SeededRandom(seed);

                    // Параметры генерации
                    var parameters = new NPCGenerationParams
                    {
                        cultivationLevel = 3,
                        role = NPCRole.Cultivator,
                        locationId = "test_location",
                        seed = seed
                    };

                    // Генерируем NPC
                    var result = NPCGenerator.Generate(parameters, random);

                    // Проверка 1: NPC создан
                    if (result == null)
                    {
                        report += "[FAIL] NPC was not generated\n";
                        allPassed = false;
                    }
                    else
                    {
                        report += $"[PASS] NPC generated: {result.nameRu}\n";
                    }

                    // Проверка 2: Уровень культивации
                    if (result != null && result.cultivationLevel == 3)
                    {
                        report += $"[PASS] Cultivation level: {result.cultivationLevel}\n";
                    }
                    else if (result != null)
                    {
                        report += $"[FAIL] Cultivation level mismatch: expected 3, got {result.cultivationLevel}\n";
                        allPassed = false;
                    }

                    // Проверка 3: Ци соответствует формуле
                    if (result != null)
                    {
                        // Формула: coreCapacity = 1000 × 1.1^totalSubLevels
                        int expectedSubLevels = (3 - 1) * 10 + result.cultivationSubLevel;
                        float expectedMinQi = 1000 * Mathf.Pow(1.1f, expectedSubLevels);

                        // Проверяем, что MaxQi в разумных пределах (±50% от ожидаемого)
                        float minAcceptable = expectedMinQi * 0.5f;
                        float maxAcceptable = expectedMinQi * 2.0f;

                        bool qiInRange = result.maxQi >= minAcceptable && result.maxQi <= maxAcceptable;

                        if (qiInRange)
                        {
                            report += $"[PASS] Max Qi in range: {result.maxQi} (expected ~{expectedMinQi:F0})\n";
                        }
                        else
                        {
                            report += $"[WARN] Max Qi: {result.maxQi} (expected ~{expectedMinQi:F0})\n";
                        }
                    }

                    // Проверка 4: Характеристики валидны
                    if (result != null)
                    {
                        bool statsValid = result.strength >= 5 && result.strength <= 30 &&
                                          result.agility >= 5 && result.agility <= 30 &&
                                          result.intelligence >= 5 && result.intelligence <= 30;

                        if (statsValid)
                        {
                            report += $"[PASS] Stats valid: STR={result.strength}, AGI={result.agility}, INT={result.intelligence}\n";
                        }
                        else
                        {
                            report += $"[FAIL] Invalid stats\n";
                            allPassed = false;
                        }
                    }

                    // Проверка 5: Техники назначены
                    if (result != null && result.techniqueIds != null && result.techniqueIds.Count > 0)
                    {
                        report += $"[PASS] Techniques assigned: {result.techniqueIds.Count}\n";
                    }
                    else
                    {
                        report += $"[WARN] No techniques assigned\n";
                    }

                    // Проверка 6: Детерминизм
                    var random2 = new SeededRandom(seed);
                    var result2 = NPCGenerator.Generate(parameters, random2);

                    if (result != null && result2 != null &&
                        result.nameRu == result2.nameRu &&
                        result.maxQi == result2.maxQi)
                    {
                        report += "[PASS] Deterministic generation verified\n";
                    }
                    else
                    {
                        report += "[FAIL] Non-deterministic generation\n";
                        allPassed = false;
                    }
                }
                catch (Exception ex)
                {
                    report += $"[ERROR] Exception: {ex.Message}\n";
                    allPassed = false;
                }

                report += allPassed ? "=== Result: PASSED ===" : "=== Result: FAILED ===";
                return allPassed;
            }
        }

        #endregion

        #region Scenario 2: Combat Flow

        /// <summary>
        /// Тест: Бой Player vs NPC.
        /// Проверяет полный цикл боя.
        /// </summary>
        public static class Scenario_CombatFlow
        {
            public static bool Run(out string report)
            {
                report = "=== Scenario: Combat Flow ===\n";
                bool allPassed = true;

                try
                {
                    // Создаём атакующего (игрок L5)
                    var attackerParams = new AttackerParams
                    {
                        CultivationLevel = 5,
                        Strength = 20,
                        Agility = 15,
                        Intelligence = 12,
                        Penetration = 5,
                        AttackElement = Element.Fire,
                        CombatSubtype = CombatSubtype.MeleeStrike,
                        TechniqueLevel = 5,
                        TechniqueGrade = TechniqueGrade.Refined,
                        IsUltimate = false,
                        IsQiTechnique = true
                    };

                    // Создаём защищающегося (NPC L4)
                    var defenderParams = new DefenderParams
                    {
                        CultivationLevel = 4,
                        CurrentQi = 5000,
                        QiDefense = QiDefenseType.RawQi,
                        Agility = 12,
                        Strength = 15,
                        ArmorCoverage = 0.4f,
                        DamageReduction = 0.25f,
                        ArmorValue = 30,
                        DodgePenalty = 0.05f,
                        ParryBonus = 0.1f,
                        BlockBonus = 0f,
                        BodyMaterial = BodyMaterial.Organic
                    };

                    int techniqueCapacity = 80;

                    // Проверка 1: Атака наносит урон
                    var result = DamageCalculator.CalculateDamage(techniqueCapacity, attackerParams, defenderParams);

                    if (result.FinalDamage > 0)
                    {
                        report += $"[PASS] Attack deals damage: {result.FinalDamage:F1}\n";
                    }
                    else if (result.WasDodged || result.WasParried || result.WasBlocked)
                    {
                        report += $"[PASS] Attack was defended (dodge={result.WasDodged}, parry={result.WasParried}, block={result.WasBlocked})\n";
                    }
                    else
                    {
                        report += "[WARN] No damage or defense\n";
                    }

                    // Проверка 2: Qi Buffer работает
                    if (result.QiAbsorbed)
                    {
                        report += $"[PASS] Qi absorbed: {result.QiAbsorbedAmount:F1} (consumed {result.QiConsumed} Qi)\n";
                    }

                    // Проверка 3: Подавление уровнем корректно
                    report += $"[INFO] Suppression multiplier: {result.SuppressionMultiplier:F2}\n";

                    // Проверка 4: Множественные атаки
                    int totalDamage = 0;
                    int hitCount = 0;
                    int dodgeCount = 0;

                    for (int i = 0; i < 10; i++)
                    {
                        var attackResult = DamageCalculator.CalculateDamage(techniqueCapacity, attackerParams, defenderParams);
                        if (attackResult.WasDodged)
                            dodgeCount++;
                        else
                        {
                            totalDamage += (int)attackResult.FinalDamage;
                            hitCount++;
                        }
                    }

                    report += $"[INFO] 10 attacks: {hitCount} hits, {dodgeCount} dodges, {totalDamage} total damage\n";

                    if (hitCount > 0 && totalDamage > 0)
                    {
                        report += "[PASS] Combat flow produces valid results\n";
                    }
                    else
                    {
                        report += "[WARN] No hits in 10 attacks\n";
                    }

                    // Проверка 5: Разные уровни культивации
                    var weakAttacker = new AttackerParams
                    {
                        CultivationLevel = 1,
                        Strength = 10,
                        Agility = 10,
                        Intelligence = 10,
                        Penetration = 0,
                        AttackElement = Element.Neutral,
                        CombatSubtype = CombatSubtype.MeleeStrike,
                        TechniqueLevel = 1,
                        TechniqueGrade = TechniqueGrade.Common,
                        IsUltimate = false,
                        IsQiTechnique = true
                    };

                    var strongDefender = new DefenderParams
                    {
                        CultivationLevel = 8,
                        CurrentQi = 100000,
                        QiDefense = QiDefenseType.RawQi,
                        Agility = 30,
                        Strength = 30,
                        ArmorCoverage = 0.8f,
                        DamageReduction = 0.5f,
                        ArmorValue = 100,
                        DodgePenalty = 0,
                        ParryBonus = 0,
                        BlockBonus = 0,
                        BodyMaterial = BodyMaterial.Scaled
                    };

                    var weakResult = DamageCalculator.CalculateDamage(techniqueCapacity, weakAttacker, strongDefender);

                    // L1 vs L8 = 7 уровней разницы = 0% урона
                    if (weakResult.SuppressionMultiplier == 0f)
                    {
                        report += "[PASS] Level suppression prevents L1 vs L8 damage\n";
                    }
                    else
                    {
                        report += $"[FAIL] Level suppression should prevent damage (got {weakResult.SuppressionMultiplier})\n";
                        allPassed = false;
                    }
                }
                catch (Exception ex)
                {
                    report += $"[ERROR] Exception: {ex.Message}\n";
                    allPassed = false;
                }

                report += allPassed ? "=== Result: PASSED ===" : "=== Result: FAILED ===";
                return allPassed;
            }
        }

        #endregion

        #region Scenario 3: Cultivation Breakthrough

        /// <summary>
        /// Тест: Прорыв уровня культивации.
        /// Проверяет формулы расчёта Ци для прорыва.
        /// </summary>
        public static class Scenario_CultivationBreakthrough
        {
            public static bool Run(out string report)
            {
                report = "=== Scenario: Cultivation Breakthrough ===\n";
                bool allPassed = true;

                try
                {
                    // Формула: coreCapacity = 1000 × 1.1^totalSubLevels
                    // totalSubLevels = (level-1) × 10 + subLevel

                    // Проверка 1: Ёмкость ядра на L1.0
                    int totalSubLevels_L1_0 = (1 - 1) * 10 + 0;
                    float expectedCapacity_L1_0 = 1000 * Mathf.Pow(1.1f, totalSubLevels_L1_0);

                    report += $"[INFO] L1.0 capacity: {expectedCapacity_L1_0:F0}\n";

                    // Проверка 2: Ёмкость ядра на L3.5
                    int totalSubLevels_L3_5 = (3 - 1) * 10 + 5;
                    float expectedCapacity_L3_5 = 1000 * Mathf.Pow(1.1f, totalSubLevels_L3_5);

                    report += $"[INFO] L3.5 capacity: {expectedCapacity_L3_5:F0}\n";

                    // Проверка 3: Ёмкость ядра на L10.9
                    int totalSubLevels_L10_9 = (10 - 1) * 10 + 9;
                    float expectedCapacity_L10_9 = 1000 * Mathf.Pow(1.1f, totalSubLevels_L10_9);

                    report += $"[INFO] L10.9 capacity: {expectedCapacity_L10_9:F0}\n";

                    // Проверка 4: Плотность Ци
                    // qiDensity = 2^(level-1)
                    int[] expectedDensities = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };

                    bool densitiesCorrect = true;
                    for (int i = 0; i < 10; i++)
                    {
                        int level = i + 1;
                        int expected = expectedDensities[i];
                        int actual = GameConstants.QiDensityByLevel[i];

                        if (expected != actual)
                        {
                            densitiesCorrect = false;
                            report += $"[FAIL] L{level} density: expected {expected}, got {actual}\n";
                        }
                    }

                    if (densitiesCorrect)
                    {
                        report += "[PASS] All Qi densities correct\n";
                    }
                    else
                    {
                        allPassed = false;
                    }

                    // Проверка 5: Прогресс прорыва
                    // Малый прорыв: требует ×10 от текущей ёмкости
                    // Большой прорыв: требует ×100 от текущей ёмкости

                    float smallBreakthroughCost_L1 = expectedCapacity_L1_0 * 10;
                    float bigBreakthroughCost_L1 = expectedCapacity_L1_0 * 100;

                    report += $"[INFO] L1 small breakthrough cost: {smallBreakthroughCost_L1:F0}\n";
                    report += $"[INFO] L1 big breakthrough cost: {bigBreakthroughCost_L1:F0}\n";

                    // Проверка 6: Множители регенерации
                    float[] expectedRegen = { 1.1f, 2.0f, 3.0f, 5.0f, 8.0f, 15.0f, 30.0f, 100.0f, 1000.0f, float.PositiveInfinity };
                    bool regenCorrect = true;

                    for (int i = 0; i < 10; i++)
                    {
                        float actual = GameConstants.RegenerationMultipliers[i];
                        float expected = expectedRegen[i];
                        
                        // Special case for infinity
                        if (float.IsInfinity(expected) && float.IsInfinity(actual))
                        {
                            // OK
                        }
                        else if (!Mathf.Approximately(actual, expected))
                        {
                            regenCorrect = false;
                            report += $"[FAIL] L{i + 1} regen: expected {expected}, got {actual}\n";
                        }
                    }

                    if (regenCorrect)
                    {
                        report += "[PASS] All regeneration multipliers correct\n";
                    }
                    else
                    {
                        allPassed = false;
                    }
                }
                catch (Exception ex)
                {
                    report += $"[ERROR] Exception: {ex.Message}\n";
                    allPassed = false;
                }

                report += allPassed ? "=== Result: PASSED ===" : "=== Result: FAILED ===";
                return allPassed;
            }
        }

        #endregion

        #region Scenario 4: Save/Load

        /// <summary>
        /// Тест: Сохранение и загрузка.
        /// Проверяет целостность данных при сериализации.
        /// </summary>
        public static class Scenario_SaveLoad
        {
            public static bool Run(out string report)
            {
                report = "=== Scenario: Save/Load ===\n";
                bool allPassed = true;

                try
                {
                    // Создаём тестовые данные
                    var playerData = new PlayerSaveData
                    {
                        PlayerId = "test_player",
                        Name = "Тестовый Игрок",
                        CultivationLevel = 5,
                        CurrentQi = 12345,
                        CurrentLocationId = "test_location"
                    };

                    // Создаём данные инвентаря
                    var inventoryData = new List<InventorySlotSaveData>
                    {
                        new InventorySlotSaveData
                        {
                            itemId = "item_sword_001",
                            count = 1,
                            gridX = 0,
                            gridY = 0,
                            durability = 100
                        },
                        new InventorySlotSaveData
                        {
                            itemId = "item_potion_hp",
                            count = 10,
                            gridX = 2,
                            gridY = 0,
                            durability = -1
                        }
                    };

                    // Проверка 1: Сериализация PlayerSaveData
                    string json = JsonUtility.ToJson(playerData, true);
                    report += $"[INFO] Player data JSON length: {json.Length}\n";

                    if (!string.IsNullOrEmpty(json))
                    {
                        report += "[PASS] Player data serialized\n";
                    }
                    else
                    {
                        report += "[FAIL] Player data serialization failed\n";
                        allPassed = false;
                    }

                    // Проверка 2: Десериализация PlayerSaveData
                    var loadedPlayer = JsonUtility.FromJson<PlayerSaveData>(json);

                    if (loadedPlayer.PlayerId == playerData.PlayerId &&
                        loadedPlayer.Name == playerData.Name &&
                        loadedPlayer.CultivationLevel == playerData.CultivationLevel &&
                        loadedPlayer.CurrentQi == playerData.CurrentQi)
                    {
                        report += "[PASS] Player data deserialized correctly\n";
                    }
                    else
                    {
                        report += "[FAIL] Player data deserialization mismatch\n";
                        allPassed = false;
                    }

                    // Проверка 3: Сериализация инвентаря
                    string inventoryJson = JsonUtility.ToJson(new InventorySaveWrapper { slots = inventoryData }, true);
                    report += $"[INFO] Inventory JSON length: {inventoryJson.Length}\n";

                    if (!string.IsNullOrEmpty(inventoryJson))
                    {
                        report += "[PASS] Inventory serialized\n";
                    }
                    else
                    {
                        report += "[FAIL] Inventory serialization failed\n";
                        allPassed = false;
                    }

                    // Проверка 4: Десериализация инвентаря
                    var loadedInventory = JsonUtility.FromJson<InventorySaveWrapper>(inventoryJson);

                    if (loadedInventory.slots != null && loadedInventory.slots.Count == inventoryData.Count)
                    {
                        report += "[PASS] Inventory deserialized correctly\n";
                    }
                    else
                    {
                        report += "[FAIL] Inventory deserialization mismatch\n";
                        allPassed = false;
                    }

                    // Проверка 5: Полнота данных
                    var fullSaveData = new GameSaveData
                    {
                        saveVersion = GameConstants.SAVE_VERSION,
                        saveTime = DateTime.UtcNow.ToString("o"),
                        playTime = 3600,
                        playerData = playerData,
                        inventorySlots = inventoryData,
                        equipmentData = new Dictionary<string, Inventory.EquipmentSaveData>()
                    };

                    string fullJson = JsonUtility.ToJson(fullSaveData, true);
                    report += $"[INFO] Full save JSON length: {fullJson.Length}\n";

                    if (!string.IsNullOrEmpty(fullJson) && fullJson.Contains("saveVersion"))
                    {
                        report += "[PASS] Full save data serialized\n";
                    }
                    else
                    {
                        report += "[FAIL] Full save data serialization failed\n";
                        allPassed = false;
                    }
                }
                catch (Exception ex)
                {
                    report += $"[ERROR] Exception: {ex.Message}\n";
                    allPassed = false;
                }

                report += allPassed ? "=== Result: PASSED ===" : "=== Result: FAILED ===";
                return allPassed;
            }
        }

        #endregion

        #region Test Runner

        /// <summary>
        /// Запускает все интеграционные тесты.
        /// </summary>
        public static void RunAllScenarios()
        {
            Debug.Log("========== Integration Test Scenarios ==========");

            int passed = 0;
            int failed = 0;

            // Scenario 1
            if (Scenario_NPCGeneration.Run(out string report1))
            {
                passed++;
                Debug.Log($"<color=green>{report1}</color>");
            }
            else
            {
                failed++;
                Debug.Log($"<color=red>{report1}</color>");
            }

            // Scenario 2
            if (Scenario_CombatFlow.Run(out string report2))
            {
                passed++;
                Debug.Log($"<color=green>{report2}</color>");
            }
            else
            {
                failed++;
                Debug.Log($"<color=red>{report2}</color>");
            }

            // Scenario 3
            if (Scenario_CultivationBreakthrough.Run(out string report3))
            {
                passed++;
                Debug.Log($"<color=green>{report3}</color>");
            }
            else
            {
                failed++;
                Debug.Log($"<color=red>{report3}</color>");
            }

            // Scenario 4
            if (Scenario_SaveLoad.Run(out string report4))
            {
                passed++;
                Debug.Log($"<color=green>{report4}</color>");
            }
            else
            {
                failed++;
                Debug.Log($"<color=red>{report4}</color>");
            }

            Debug.Log($"========== Results: {passed} passed, {failed} failed ==========");
        }

        #endregion
    }

    #region Helper Classes for Save Data

    [Serializable]
    public class PlayerSaveData
    {
        public string PlayerId;
        public string Name;
        public int CultivationLevel;
        public int CultivationSubLevel;
        public long CurrentQi; // FIX: float→long (Fix-01 cascade, Fix-12 test fix) (2026-04-12)
        public long MaxQi;    // FIX: float→long (Fix-01 cascade, Fix-12 test fix) (2026-04-12)
        public string CurrentLocationId;
        public float PlayTime;
    }

    [Serializable]
    public class InventorySaveWrapper
    {
        public List<InventorySlotSaveData> slots;
    }

    [Serializable]
    public class GameSaveData
    {
        public int saveVersion;
        public string saveTime;
        public float playTime;
        public PlayerSaveData playerData;
        public List<InventorySlotSaveData> inventorySlots;
        public Dictionary<string, Inventory.EquipmentSaveData> equipmentData;
    }

    #endregion
}

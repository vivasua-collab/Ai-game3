// ============================================================================
// GeneratorRegistry.cs — Реестр генераторов
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-31 14:22:00 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Реестр генераторов — централизованный доступ ко всем генераторам.
    /// 
    /// Инициализируется с сидом мира и предоставляет единый доступ к:
    /// - NPCGenerator
    /// - TechniqueGenerator
    /// - WeaponGenerator
    /// - ArmorGenerator
    /// - ConsumableGenerator
    /// 
    /// Источник: IMPLEMENTATION_PLAN_NEXT.md §"ЭТАП 3: Generator System Integration"
    /// </summary>
    public class GeneratorRegistry : MonoBehaviour
    {
        #region Singleton

        public static GeneratorRegistry Instance { get; private set; }

        #endregion

        #region Fields

        [Header("Configuration")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private long defaultSeed = 12345;

        // === Runtime ===
        private SeededRandom worldRng;
        private long currentSeed;
        private bool isInitialized = false;

        // === Cache ===
        private Dictionary<string, GeneratedNPC> cachedNPCs = new Dictionary<string, GeneratedNPC>();
        private Dictionary<string, GeneratedTechnique> cachedTechniques = new Dictionary<string, GeneratedTechnique>();

        #endregion

        #region Events

        public event Action<long> OnInitialized;
        public event Action<GeneratedNPC> OnNPCGenerated;
        public event Action<GeneratedTechnique> OnTechniqueGenerated;

        #endregion

        #region Properties

        public bool IsInitialized => isInitialized;
        public long CurrentSeed => currentSeed;
        public int CachedNPCCount => cachedNPCs.Count;
        public int CachedTechniqueCount => cachedTechniques.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (autoInitialize)
            {
                Initialize(defaultSeed);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализировать реестр с сидом мира.
        /// </summary>
        public void Initialize(long worldSeed)
        {
            currentSeed = worldSeed;
            worldRng = new SeededRandom((int)(worldSeed % int.MaxValue));
            isInitialized = true;

            // Очищаем кэш
            cachedNPCs.Clear();
            cachedTechniques.Clear();

            OnInitialized?.Invoke(worldSeed);

            Debug.Log($"[GeneratorRegistry] Initialized with seed: {worldSeed}");
        }

        /// <summary>
        /// Переинициализировать с новым сидом.
        /// </summary>
        public void Reinitialize(long newSeed)
        {
            Initialize(newSeed);
        }

        #endregion

        #region NPC Generation

        /// <summary>
        /// Сгенерировать NPC с параметрами.
        /// </summary>
        public GeneratedNPC GenerateNPC(NPCGenerationParams parameters)
        {
            EnsureInitialized();

            var npc = NPCGenerator.Generate(parameters, worldRng);

            // Кэшируем
            cachedNPCs[npc.id] = npc;

            OnNPCGenerated?.Invoke(npc);

            return npc;
        }

        /// <summary>
        /// Сгенерировать несколько NPC.
        /// </summary>
        public List<GeneratedNPC> GenerateNPCs(NPCGenerationParams parameters)
        {
            EnsureInitialized();

            var npcs = new List<GeneratedNPC>();

            for (int i = 0; i < parameters.count; i++)
            {
                var npc = NPCGenerator.Generate(parameters, worldRng);
                cachedNPCs[npc.id] = npc;
                npcs.Add(npc);
                OnNPCGenerated?.Invoke(npc);
            }

            return npcs;
        }

        /// <summary>
        /// Сгенерировать врага для игрока указанного уровня.
        /// </summary>
        public GeneratedNPC GenerateEnemyForPlayer(int playerLevel)
        {
            EnsureInitialized();

            var npc = NPCGenerator.GenerateEnemyForPlayer(playerLevel, worldRng);
            cachedNPCs[npc.id] = npc;
            OnNPCGenerated?.Invoke(npc);

            return npc;
        }

        /// <summary>
        /// Сгенерировать NPC по роли.
        /// </summary>
        public GeneratedNPC GenerateNPCByRole(NPCRole role, int cultivationLevel = 0)
        {
            var parameters = new NPCGenerationParams
            {
                role = role,
                cultivationLevel = cultivationLevel
            };

            return GenerateNPC(parameters);
        }

        /// <summary>
        /// Получить сгенерированного NPC из кэша.
        /// </summary>
        public GeneratedNPC GetCachedNPC(string id)
        {
            if (cachedNPCs.TryGetValue(id, out var npc))
                return npc;
            return null;
        }

        #endregion

        #region Technique Generation

        /// <summary>
        /// Сгенерировать технику с параметрами.
        /// </summary>
        public GeneratedTechnique GenerateTechnique(TechniqueGenerationParams parameters)
        {
            EnsureInitialized();

            var technique = TechniqueGenerator.Generate(parameters, worldRng);

            // Кэшируем
            cachedTechniques[technique.id] = technique;

            OnTechniqueGenerated?.Invoke(technique);

            return technique;
        }

        /// <summary>
        /// Сгенерировать несколько техник.
        /// </summary>
        public List<GeneratedTechnique> GenerateTechniques(TechniqueGenerationParams parameters)
        {
            EnsureInitialized();

            var techniques = new List<GeneratedTechnique>();

            for (int i = 0; i < parameters.count; i++)
            {
                var technique = TechniqueGenerator.Generate(parameters, worldRng);
                cachedTechniques[technique.id] = technique;
                techniques.Add(technique);
                OnTechniqueGenerated?.Invoke(technique);
            }

            return techniques;
        }

        /// <summary>
        /// Сгенерировать технику для уровня культивации.
        /// </summary>
        public GeneratedTechnique GenerateTechniqueForLevel(int cultivationLevel)
        {
            EnsureInitialized();

            var technique = TechniqueGenerator.GenerateForLevel(cultivationLevel, worldRng);
            cachedTechniques[technique.id] = technique;
            OnTechniqueGenerated?.Invoke(technique);

            return technique;
        }

        /// <summary>
        /// Сгенерировать технику по типу.
        /// </summary>
        public GeneratedTechnique GenerateTechniqueByType(TechniqueType type, int level = 1, TechniqueGrade grade = TechniqueGrade.Common)
        {
            var parameters = new TechniqueGenerationParams
            {
                type = type,
                level = level,
                grade = grade
            };

            return GenerateTechnique(parameters);
        }

        /// <summary>
        /// Получить сгенерированную технику из кэша.
        /// </summary>
        public GeneratedTechnique GetCachedTechnique(string id)
        {
            if (cachedTechniques.TryGetValue(id, out var technique))
                return technique;
            return null;
        }

        #endregion

        #region Weapon Generation

        /// <summary>
        /// Сгенерировать оружие.
        /// </summary>
        public GeneratedWeapon GenerateWeapon(WeaponGenerationParams parameters)
        {
            EnsureInitialized();
            return WeaponGenerator.Generate(parameters, worldRng);
        }

        /// <summary>
        /// Сгенерировать оружие для уровня.
        /// </summary>
        public GeneratedWeapon GenerateWeaponForLevel(int cultivationLevel)
        {
            EnsureInitialized();
            return WeaponGenerator.GenerateForLevel(cultivationLevel, worldRng);
        }

        #endregion

        #region Armor Generation

        /// <summary>
        /// Сгенерировать броню.
        /// </summary>
        public GeneratedArmor GenerateArmor(ArmorGenerationParams parameters)
        {
            EnsureInitialized();
            return ArmorGenerator.Generate(parameters, worldRng);
        }

        /// <summary>
        /// Сгенерировать броню для уровня.
        /// </summary>
        public GeneratedArmor GenerateArmorForLevel(int cultivationLevel)
        {
            EnsureInitialized();
            return ArmorGenerator.GenerateForLevel(cultivationLevel, worldRng);
        }

        #endregion

        #region Consumable Generation

        /// <summary>
        /// Сгенерировать расходник.
        /// </summary>
        public GeneratedConsumable GenerateConsumable(ConsumableGenerationParams parameters)
        {
            EnsureInitialized();
            return ConsumableGenerator.Generate(parameters, worldRng);
        }

        /// <summary>
        /// Сгенерировать случайный расходник.
        /// </summary>
        public GeneratedConsumable GenerateRandomConsumable()
        {
            var parameters = new ConsumableGenerationParams();
            return GenerateConsumable(parameters);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Получить новый RNG с уникальным сидом.
        /// </summary>
        public SeededRandom CreateNewRng()
        {
            EnsureInitialized();
            return new SeededRandom(worldRng.Next());
        }

        /// <summary>
        /// Сбросить кэш генераторов.
        /// </summary>
        public void ClearCache()
        {
            cachedNPCs.Clear();
            cachedTechniques.Clear();
        }

        private void EnsureInitialized()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[GeneratorRegistry] Not initialized, auto-initializing with default seed");
                Initialize(defaultSeed);
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Получить статистику генерации.
        /// </summary>
        public GeneratorStatistics GetStatistics()
        {
            return new GeneratorStatistics
            {
                Seed = currentSeed,
                TotalNPCsGenerated = cachedNPCs.Count,
                TotalTechniquesGenerated = cachedTechniques.Count,
                IsInitialized = isInitialized
            };
        }

        #endregion
    }

    /// <summary>
    /// Статистика генераторов.
    /// </summary>
    public struct GeneratorStatistics
    {
        public long Seed;
        public int TotalNPCsGenerated;
        public int TotalTechniquesGenerated;
        public bool IsInitialized;

        public override string ToString()
        {
            return $"Seed: {Seed}, NPCs: {TotalNPCsGenerated}, Techniques: {TotalTechniquesGenerated}";
        }
    }
}

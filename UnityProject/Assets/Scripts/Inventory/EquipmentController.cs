// ============================================================================
// EquipmentController.cs — Система экипировки
// Cultivation World Simulator
// Создано: 2026-04-03
// Редактировано: 2026-04-11 06:38:02 UTC — INV-H03: проверка требований экипировки, SAV-H01: сериализация customBonuses, Qi int→long

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;
using CultivationGame.Save;
using CultivationGame.Qi;
using CultivationGame.Player;

namespace CultivationGame.Inventory
{
    /// <summary>
    /// Контроллер экипировки персонажа.
    /// Реализует принцип "матрёшка" — несколько слоёв одежды.
    /// </summary>
    public class EquipmentController : MonoBehaviour
    {
        #region Configuration

        [Header("Equipment Settings")]
        [Tooltip("Включить систему слоёв")]
        public bool useLayerSystem = true;

        [Tooltip("Максимум слоёв на один слот")]
        [Range(1, 3)]
        public int maxLayersPerSlot = 2;

        #endregion

        #region Runtime Data

        // Основные слоты экипировки
        private Dictionary<EquipmentSlot, List<EquipmentInstance>> equipmentSlots;

        // Кэш вычисленных статов
        private EquipmentStats cachedStats;

        // Флаг_dirty для пересчёта статов
        private bool statsDirty = true;

        #endregion

        #region Events

        public event Action<EquipmentSlot, EquipmentInstance> OnEquipmentEquipped;
        public event Action<EquipmentSlot, EquipmentInstance> OnEquipmentUnequipped;
        public event Action<EquipmentStats> OnStatsChanged;
        public event Action<EquipmentSlot, bool> OnSlotAvailabilityChanged;

        #endregion

        #region Properties

        public EquipmentStats CurrentStats
        {
            get
            {
                if (statsDirty)
                    RecalculateStats();
                return cachedStats;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSlots();
        }

        #endregion

        #region Initialization

        private void InitializeSlots()
        {
            equipmentSlots = new Dictionary<EquipmentSlot, List<EquipmentInstance>>();

            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                equipmentSlots[slot] = new List<EquipmentInstance>();
            }

            cachedStats = new EquipmentStats();
        }

        #endregion

        #region Equip/Unequip

        /// <summary>
        /// Экипирует предмет
        /// </summary>
        public bool Equip(EquipmentData equipmentData, EquipmentGrade grade = EquipmentGrade.Common, int durability = -1)
        {
            if (equipmentData == null)
                return false;

            EquipmentSlot slot = equipmentData.slot;

            // FIX: Получаем параметры игрока для проверки требований
            int playerCultivationLevel = 0;
            Dictionary<string, float> playerStats = null;
            
            var qiCtrl = ServiceLocator.GetOrFind<QiController>();
            if (qiCtrl != null)
                playerCultivationLevel = qiCtrl.CultivationLevel;
            
            var playerCtrl = ServiceLocator.GetOrFind<PlayerController>();
            if (playerCtrl != null && playerCtrl.StatDevelopment != null)
                playerStats = playerCtrl.StatDevelopment.GetAllStatsAsDictionary();

            // Проверяем требования
            if (!CanEquip(equipmentData, playerCultivationLevel, playerStats))
                return false;

            // Создаём экземпляр
            var instance = new EquipmentInstance
            {
                equipmentData = equipmentData,
                grade = grade,
                durability = durability > 0 ? durability : equipmentData.maxDurability,
                currentLayer = equipmentSlots[slot].Count
            };

            // Если слот занят и нет системы слоёв — снимаем старое
            if (!useLayerSystem && equipmentSlots[slot].Count > 0)
            {
                Unequip(slot);
            }

            // Проверяем лимит слоёв
            if (equipmentSlots[slot].Count >= maxLayersPerSlot)
            {
                // Снимаем самый внутренний слой
                UnequipLayer(slot, 0);
            }

            // Добавляем экипировку
            equipmentSlots[slot].Add(instance);
            statsDirty = true;

            OnEquipmentEquipped?.Invoke(slot, instance);
            OnStatsChanged?.Invoke(CurrentStats);

            return true;
        }

        /// <summary>
        /// Снимает экипировку с указанного слота
        /// </summary>
        public EquipmentInstance Unequip(EquipmentSlot slot)
        {
            if (equipmentSlots[slot].Count == 0)
                return null;

            // Снимаем внешний слой (последний добавленный)
            int lastIndex = equipmentSlots[slot].Count - 1;
            var instance = equipmentSlots[slot][lastIndex];
            equipmentSlots[slot].RemoveAt(lastIndex);

            statsDirty = true;

            OnEquipmentUnequipped?.Invoke(slot, instance);
            OnStatsChanged?.Invoke(CurrentStats);

            return instance;
        }

        /// <summary>
        /// Снимает экипировку с указанного слоя
        /// </summary>
        public EquipmentInstance UnequipLayer(EquipmentSlot slot, int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= equipmentSlots[slot].Count)
                return null;

            var instance = equipmentSlots[slot][layerIndex];
            equipmentSlots[slot].RemoveAt(layerIndex);

            // Обновляем индексы слоёв
            for (int i = 0; i < equipmentSlots[slot].Count; i++)
            {
                equipmentSlots[slot][i].currentLayer = i;
            }

            statsDirty = true;

            OnEquipmentUnequipped?.Invoke(slot, instance);
            OnStatsChanged?.Invoke(CurrentStats);

            return instance;
        }

        /// <summary>
        /// Снимает всю экипировку
        /// </summary>
        public List<EquipmentInstance> UnequipAll()
        {
            var allEquipment = new List<EquipmentInstance>();

            foreach (var slot in equipmentSlots.Keys)
            {
                while (equipmentSlots[slot].Count > 0)
                {
                    var instance = Unequip(slot);
                    if (instance != null)
                        allEquipment.Add(instance);
                }
            }

            return allEquipment;
        }

        /// <summary>
        /// Меняет экипировку местами (для двуручного/одноручного)
        /// </summary>
        public bool SwapSlots(EquipmentSlot slot1, EquipmentSlot slot2)
        {
            var list1 = equipmentSlots[slot1];
            var list2 = equipmentSlots[slot2];

            if (list1.Count == 0 && list2.Count == 0)
                return false;

            // Меняем местами списки
            var temp = new List<EquipmentInstance>(list1);
            list1.Clear();
            list1.AddRange(list2);
            list2.Clear();
            list2.AddRange(temp);

            statsDirty = true;
            OnStatsChanged?.Invoke(CurrentStats);

            return true;
        }

        #endregion

        #region Query

        /// <summary>
        /// Проверяет, можно ли экипировать предмет
        /// FIX INV-H03: Реализована проверка требований (2026-04-11)
        /// </summary>
        public bool CanEquip(EquipmentData equipmentData, int playerCultivationLevel = 0, Dictionary<string, float> playerStats = null)
        {
            if (equipmentData == null)
                return false;

            // FIX INV-H03: Check cultivation level requirement (2026-04-11)
            if (equipmentData.requiredCultivationLevel > 0 && playerCultivationLevel < equipmentData.requiredCultivationLevel)
            {
                return false;
            }

            // FIX INV-H03: Check stat requirements (2026-04-11)
            if (equipmentData.statRequirements != null && equipmentData.statRequirements.Count > 0 && playerStats != null)
            {
                foreach (var req in equipmentData.statRequirements)
                {
                    if (playerStats.TryGetValue(req.statName, out float value))
                    {
                        if (value < req.minValue) return false;
                    }
                    else
                    {
                        // Stat not found — requirement not met
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Получает экипировку в указанном слоте (внешний слой)
        /// </summary>
        public EquipmentInstance GetEquipment(EquipmentSlot slot)
        {
            if (equipmentSlots[slot].Count == 0)
                return null;

            return equipmentSlots[slot][equipmentSlots[slot].Count - 1];
        }

        /// <summary>
        /// Получает все слои экипировки в слоте
        /// </summary>
        public List<EquipmentInstance> GetAllLayers(EquipmentSlot slot)
        {
            return new List<EquipmentInstance>(equipmentSlots[slot]);
        }

        /// <summary>
        /// Получает экипировку по слою
        /// </summary>
        public EquipmentInstance GetEquipmentAtLayer(EquipmentSlot slot, int layer)
        {
            if (layer < 0 || layer >= equipmentSlots[slot].Count)
                return null;

            return equipmentSlots[slot][layer];
        }

        /// <summary>
        /// Проверяет, занят ли слот
        /// </summary>
        public bool IsSlotOccupied(EquipmentSlot slot)
        {
            return equipmentSlots[slot].Count > 0;
        }

        /// <summary>
        /// Подсчитывает количество слоёв в слоте
        /// </summary>
        public int GetLayerCount(EquipmentSlot slot)
        {
            return equipmentSlots[slot].Count;
        }

        /// <summary>
        /// Получает все предметы определённого типа
        /// </summary>
        public List<EquipmentInstance> GetEquipmentByCategory(ItemCategory category)
        {
            var result = new List<EquipmentInstance>();

            foreach (var slotList in equipmentSlots.Values)
            {
                foreach (var instance in slotList)
                {
                    if (instance.equipmentData.category == category)
                        result.Add(instance);
                }
            }

            return result;
        }

        #endregion

        #region Stats Calculation

        /// <summary>
        /// Пересчитывает все статы от экипировки
        /// </summary>
        private void RecalculateStats()
        {
            cachedStats = new EquipmentStats();

            foreach (var slotList in equipmentSlots.Values)
            {
                foreach (var instance in slotList)
                {
                    AddEquipmentStats(instance);
                }
            }

            statsDirty = false;
        }

        private void AddEquipmentStats(EquipmentInstance instance)
        {
            var data = instance.equipmentData;
            float gradeMultiplier = GetGradeMultiplier(instance.grade);

            // Базовые статы
            cachedStats.totalDamage += Mathf.RoundToInt(data.damage * gradeMultiplier);
            cachedStats.totalDefense += Mathf.RoundToInt(data.defense * gradeMultiplier);
            cachedStats.damageReduction += data.damageReduction;
            cachedStats.dodgeBonus += data.dodgeBonus;

            // Бонусы к характеристикам
            foreach (var bonus in data.statBonuses)
            {
                float value = bonus.isPercentage ? bonus.bonus : bonus.bonus * gradeMultiplier;

                switch (bonus.statName.ToLower())
                {
                    case "strength":
                    case "str":
                        cachedStats.strength += bonus.bonus;
                        break;
                    case "agility":
                    case "agi":
                        cachedStats.agility += bonus.bonus;
                        break;
                    case "constitution":
                    case "con":
                        cachedStats.constitution += bonus.bonus;
                        break;
                    case "intelligence":
                    case "int":
                        cachedStats.intelligence += bonus.bonus;
                        break;
                    case "qi":
                    case "maxqi":
                        cachedStats.maxQi += bonus.bonus;
                        break;
                    case "qiregen":
                        cachedStats.qiRegen += bonus.bonus;
                        break;
                    default:
                        cachedStats.customBonuses[bonus.statName] =
                            cachedStats.GetCustomBonus(bonus.statName) + bonus.bonus;
                        break;
                }
            }
        }

        private float GetGradeMultiplier(EquipmentGrade grade)
        {
            return grade switch
            {
                EquipmentGrade.Damaged => 0.5f,
                EquipmentGrade.Common => 1.0f,
                EquipmentGrade.Refined => 1.5f,
                EquipmentGrade.Perfect => 2.5f,
                EquipmentGrade.Transcendent => 4.0f,
                _ => 1.0f
            };
        }

        #endregion

        #region Durability

        /// <summary>
        /// Наносит урон прочности экипировке
        /// </summary>
        public void DamageEquipment(EquipmentSlot slot, int amount)
        {
            var instance = GetEquipment(slot);
            if (instance == null || instance.durability < 0)
                return;

            instance.durability = Mathf.Max(0, instance.durability - amount);

            // Если прочность 0 — экипировка ломается
            if (instance.durability == 0)
            {
                // TODO: Уведомление о поломке
            }
        }

        /// <summary>
        /// Чинит экипировку
        /// </summary>
        public void RepairEquipment(EquipmentSlot slot, int amount)
        {
            var instance = GetEquipment(slot);
            if (instance == null || instance.durability < 0)
                return;

            instance.durability = Mathf.Min(instance.equipmentData.maxDurability, instance.durability + amount);
        }

        /// <summary>
        /// Полностью чинит всю экипировку
        /// </summary>
        public void RepairAll()
        {
            foreach (var slotList in equipmentSlots.Values)
            {
                foreach (var instance in slotList)
                {
                    if (instance.durability >= 0)
                    {
                        instance.durability = instance.equipmentData.maxDurability;
                    }
                }
            }
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Получает данные для сохранения
        /// </summary>
        public Dictionary<string, EquipmentSaveData> GetSaveData()
        {
            var data = new Dictionary<string, EquipmentSaveData>();

            foreach (var kvp in equipmentSlots)
            {
                if (kvp.Value.Count > 0)
                {
                    var layers = new List<EquipmentLayerSaveData>();
                    foreach (var instance in kvp.Value)
                    {
                        layers.Add(new EquipmentLayerSaveData
                        {
                            itemId = instance.equipmentData.itemId,
                            grade = instance.grade,
                            durability = instance.durability,
                            layer = instance.currentLayer
                        });
                    }

                    data[kvp.Key.ToString()] = new EquipmentSaveData
                    {
                        slot = kvp.Key,
                        layers = layers
                    };
                }
            }

            return data;
        }

        /// <summary>
        /// Загружает данные
        /// </summary>
        public void LoadSaveData(Dictionary<string, EquipmentSaveData> data, Dictionary<string, EquipmentData> itemDatabase)
        {
            UnequipAll();

            if (data == null) return;

            foreach (var kvp in data)
            {
                var slotData = kvp.Value;

                foreach (var layerData in slotData.layers)
                {
                    if (itemDatabase.TryGetValue(layerData.itemId, out var equipmentData))
                    {
                        Equip(equipmentData, layerData.grade, layerData.durability);
                    }
                }
            }
        }

        #endregion
    }

    // ============================================================================
    // EquipmentInstance — Экземпляр экипировки
    // ============================================================================

    [Serializable]
    public class EquipmentInstance
    {
        public EquipmentData equipmentData;
        public EquipmentGrade grade;
        public int durability;
        public int currentLayer;

        // Properties
        public string ItemId => equipmentData?.itemId ?? "";
        public string Name => equipmentData?.nameRu ?? "Unknown";
        public EquipmentSlot Slot => equipmentData?.slot ?? EquipmentSlot.Backpack;
        public int MaxDurability => equipmentData?.maxDurability ?? 100;
        public float DurabilityPercent => MaxDurability > 0 ? (float)durability / MaxDurability : 1f;
        public DurabilityCondition Condition => GetCondition();

        private DurabilityCondition GetCondition()
        {
            // FIX INV-C01: durability=0 → Broken, durability<0 → Pristine (no durability system) (2026-04-11)
            if (durability < 0) return DurabilityCondition.Pristine;
            if (durability == 0) return DurabilityCondition.Broken;

            float percent = DurabilityPercent * 100f;
            if (percent >= 100f) return DurabilityCondition.Pristine;
            if (percent >= 80f) return DurabilityCondition.Excellent;
            if (percent >= 60f) return DurabilityCondition.Good;
            if (percent >= 40f) return DurabilityCondition.Worn;
            if (percent >= 20f) return DurabilityCondition.Damaged;
            return DurabilityCondition.Broken;
        }
    }

    // ============================================================================
    // EquipmentSlots — Визуальные слоты
    // ============================================================================

    [Serializable]
    public class EquipmentSlotsUI
    {
        public EquipmentSlot slotType;
        public RectTransform slotTransform;
        public UnityEngine.UI.Image iconImage;
        public UnityEngine.UI.Image durabilityBar;

        private EquipmentInstance currentEquipment;

        public void SetEquipment(EquipmentInstance instance)
        {
            currentEquipment = instance;

            if (instance != null && instance.equipmentData != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = instance.equipmentData.icon;
                    iconImage.enabled = true;
                }

                UpdateDurabilityBar();
            }
            else
            {
                Clear();
            }
        }

        public void UpdateDurabilityBar()
        {
            if (durabilityBar != null && currentEquipment != null)
            {
                durabilityBar.fillAmount = currentEquipment.DurabilityPercent;
            }
        }

        public void Clear()
        {
            currentEquipment = null;

            if (iconImage != null)
                iconImage.enabled = false;

            if (durabilityBar != null)
                durabilityBar.fillAmount = 1f;
        }
    }

    // ============================================================================
    // EquipmentStats — Вычисленные статы
    // ============================================================================

    [Serializable]
    public class EquipmentStats
    {
        // Combat
        public int totalDamage;
        public int totalDefense;
        public float damageReduction;
        public float dodgeBonus;

        // Attributes
        public float strength;
        public float agility;
        public float constitution;
        public float intelligence;

        // Qi
        public float maxQi;
        public float qiRegen;

        // Custom bonuses — runtime Dictionary, use ToSerializable/FromSerializable for save
        public Dictionary<string, float> customBonuses = new Dictionary<string, float>();

        public float GetCustomBonus(string statName)
        {
            return customBonuses.TryGetValue(statName, out var value) ? value : 0f;
        }

        /// <summary>
        /// FIX SAV-H01: Convert customBonuses to serializable array for JsonUtility (2026-04-11)
        /// </summary>
        public CustomBonusEntry[] CustomBonusesToSerializable()
        {
            var entries = new CustomBonusEntry[customBonuses.Count];
            int i = 0;
            foreach (var kvp in customBonuses)
            {
                entries[i++] = new CustomBonusEntry(kvp.Key, kvp.Value);
            }
            return entries;
        }

        /// <summary>
        /// FIX SAV-H01: Restore customBonuses from serializable array (2026-04-11)
        /// </summary>
        public void CustomBonusesFromSerializable(CustomBonusEntry[] entries)
        {
            customBonuses.Clear();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    customBonuses[entry.statName] = entry.bonus;
                }
            }
        }

        /// <summary>
        /// Объединяет статы от нескольких источников
        /// </summary>
        public static EquipmentStats Combine(params EquipmentStats[] stats)
        {
            var result = new EquipmentStats();

            foreach (var stat in stats)
            {
                result.totalDamage += stat.totalDamage;
                result.totalDefense += stat.totalDefense;
                result.damageReduction += stat.damageReduction;
                result.dodgeBonus += stat.dodgeBonus;
                result.strength += stat.strength;
                result.agility += stat.agility;
                result.constitution += stat.constitution;
                result.intelligence += stat.intelligence;
                result.maxQi += stat.maxQi;
                result.qiRegen += stat.qiRegen;

                foreach (var kvp in stat.customBonuses)
                {
                    result.customBonuses[kvp.Key] = result.GetCustomBonus(kvp.Key) + kvp.Value;
                }
            }

            return result;
        }
    }

    // ============================================================================
    // Save Data
    // ============================================================================

    [Serializable]
    public class EquipmentSaveData
    {
        public EquipmentSlot slot;
        public List<EquipmentLayerSaveData> layers;
    }

    [Serializable]
    public class EquipmentLayerSaveData
    {
        public string itemId;
        public EquipmentGrade grade;
        public int durability;
        public int layer;
    }
}

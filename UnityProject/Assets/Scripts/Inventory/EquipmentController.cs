// ============================================================================
// EquipmentController.cs — Система экипировки (v2.0 — переработка куклы)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-03
// Редактировано: 2026-04-18 19:00:00 UTC — ПОЛНАЯ ПЕРЕРАБОТКА по INVENTORY_UI_DRAFT.md v2.0
// Редактировано: 2026-04-20 06:50:00 UTC — StorageRingData наследует от EquipmentData (FIX CS0184)
// Редактировано: 2026-04-27 18:15:00 UTC — строчная модель инвентаря
// Редактировано: 2026-05-07 10:00:00 UTC — ФАЗА 1: бонусы оружия/брони для боевой системы
// Редактировано: 2026-05-05 14:30:00 MSK — FIX: bonus.bonus → bonus.value (С-07)
// ============================================================================
// Изменения v2.0:
// - Убрана система слоёв («матрёшка») для видимых слотов — 1 предмет на слот
// - Добавлена логика 1H/2H оружия (TwoHand блокирует WeaponOff)
// - Разделены GradeMultiplier: durability (урон/защита) и effectiveness (бонусы)
// - Исправлен баг EQP-BUG-02: рассчитанное value бонусов не использовалось
// - Исправлен баг EQP-BUG-03: SwapSlots не обновлял currentLayer
// - Исправлен баг EQP-BUG-04: нет логики двуручного оружия
// - Исправлен баг EQP-BUG-05: GradeMultiplier не совпадает с EQUIPMENT_SYSTEM.md
// - Скрытые слоты (Amulet, Ring*, Charger, Hands, Back) — заглушки
// ============================================================================

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
    /// Контроллер экипировки персонажа (v2.0).
    /// 
    /// Видимые слоты куклы (7): Head, Torso, Belt, Legs, Feet, WeaponMain, WeaponOff
    /// Скрытые слоты (заглушки): Amulet, RingLeft1/2, RingRight1/2, Charger, Hands, Back
    /// 
    /// Правила:
    /// - Каждый видимый слот = 1 предмет (нет слоёв)
    /// - Одноручное оружие занимает 1 слот руки
    /// - Двуручное оружие занимает WeaponMain + блокирует WeaponOff
    /// - Скрытые слоты не функциональны (заглушки на будущее)
    /// </summary>
    public class EquipmentController : MonoBehaviour
    {
        #region Constants

        /// <summary>Видимые слоты куклы — 7 основных</summary>
        public static readonly EquipmentSlot[] VisibleSlots = new[]
        {
            EquipmentSlot.Head,
            EquipmentSlot.Torso,
            EquipmentSlot.Belt,
            EquipmentSlot.Legs,
            EquipmentSlot.Feet,
            EquipmentSlot.WeaponMain,
            EquipmentSlot.WeaponOff
        };

        /// <summary>Скрытые слоты — заглушки на будущее</summary>
        public static readonly EquipmentSlot[] HiddenSlots = new[]
        {
            EquipmentSlot.Amulet,
            EquipmentSlot.RingLeft1,
            EquipmentSlot.RingLeft2,
            EquipmentSlot.RingRight1,
            EquipmentSlot.RingRight2,
            EquipmentSlot.Charger,
            EquipmentSlot.Hands,
            EquipmentSlot.Back
        };

        #endregion

        #region Configuration

        [Header("Equipment Settings")]
        [Tooltip("Включить ограничение требований экипировки")]
        public bool enforceRequirements = true;

        #endregion

        #region Runtime Data

        /// <summary>Экипировка: слот → экземпляр (1 предмет на слот)</summary>
        private Dictionary<EquipmentSlot, EquipmentInstance> equippedItems;

        /// <summary>Заблокирован ли WeaponOff двуручным оружием</summary>
        private bool isWeaponOffBlocked = false;

        /// <summary>Кэш вычисленных статов</summary>
        private EquipmentStats cachedStats;

        /// <summary>Флаг dirty для пересчёта статов</summary>
        private bool statsDirty = true;

        // ФАЗА 2: QiController для обновления проводимости при смене экипировки
        private QiController qiController;

        #endregion

        #region Events

        /// <summary>Предмет экипирован в слот</summary>
        public event Action<EquipmentSlot, EquipmentInstance> OnEquipmentEquipped;

        /// <summary>Предмет снят со слота</summary>
        public event Action<EquipmentSlot, EquipmentInstance> OnEquipmentUnequipped;

        /// <summary>Статы от экипировки изменились</summary>
        public event Action<EquipmentStats> OnStatsChanged;

        /// <summary>Слот WeaponOff заблокирован/разблокирован двуручным оружием</summary>
        public event Action<bool> OnWeaponOffBlockChanged;

        // ФАЗА 2: Подписка на OnStatsChanged для обновления QiController
        private bool qiUpdateSubscribed = false;

        #endregion

        #region Properties

        /// <summary>Текущие статы от экипировки (пересчитываются при необходимости)</summary>
        public EquipmentStats CurrentStats
        {
            get
            {
                if (statsDirty)
                    RecalculateStats();
                return cachedStats;
            }
        }

        /// <summary>Заблокирован ли WeaponOff двуручным оружием</summary>
        public bool IsWeaponOffBlocked => isWeaponOffBlocked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSlots();
            // ФАЗА 2: Получаем QiController для обновления проводимости
            qiController = GetComponent<QiController>();
            // Подписываемся на собственное событие для обновления QiController
            if (!qiUpdateSubscribed)
            {
                OnStatsChanged += ApplyQiFlowBonus;
                qiUpdateSubscribed = true;
            }
        }

        private void OnDestroy()
        {
            // ФАЗА 2: Отписка
            if (qiUpdateSubscribed)
            {
                OnStatsChanged -= ApplyQiFlowBonus;
                qiUpdateSubscribed = false;
            }
        }

        /// <summary>
        /// ФАЗА 2: Обновить бонус проводимости Ци в QiController при смене экипировки.
        /// qiFlowBonus (из EquipmentStats) → QiController.SetConductivityBonus()
        /// </summary>
        private void ApplyQiFlowBonus(EquipmentStats stats)
        {
            if (qiController != null && stats != null)
            {
                qiController.SetConductivityBonus(stats.qiFlowBonus);
            }
        }

        #endregion

        #region Initialization

        private void InitializeSlots()
        {
            equippedItems = new Dictionary<EquipmentSlot, EquipmentInstance>();

            // Инициализируем все слоты как пустые
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                equippedItems[slot] = null;
            }

            cachedStats = new EquipmentStats();
            isWeaponOffBlocked = false;
        }

        #endregion

        #region Equip / Unequip

        /// <summary>
        /// Экипирует предмет в соответствующий слот.
        /// 
        /// Логика 1H/2H:
        /// - Одноручное: занимает 1 слот (WeaponMain или WeaponOff)
        /// - Двуручное: занимает WeaponMain, блокирует WeaponOff
        ///   - Если WeaponOff занят → сначала снимается
        /// 
        /// Если слот занят → старый предмет снимается автоматически.
        /// </summary>
        /// <param name="equipmentData">Данные экипировки</param>
        /// <param name="grade">Грейд предмета</param>
        /// <param name="durability">Прочность (−1 = максимальная)</param>
        /// <returns>Экземпляр экипировки или null при ошибке</returns>
        public EquipmentInstance Equip(EquipmentData equipmentData, EquipmentGrade grade = EquipmentGrade.Common, int durability = -1)
        {
            if (equipmentData == null)
                return null;

            EquipmentSlot slot = equipmentData.slot;

            // Скрытые слоты — разрешаем ТОЛЬКО для StorageRingData на слотах колец
            if (!IsSlotVisible(slot))
            {
                bool isRingWithStorageRing = IsRingSlot(slot) && equipmentData is StorageRingData;
                if (!isRingWithStorageRing)
                {
                    Debug.LogWarning($"[EquipmentController] Слот {slot} — скрытый (заглушка), экипировка невозможна");
                    return null;
                }
            }

            // Проверяем требования
            if (enforceRequirements && !CanEquip(equipmentData))
                return null;

            // === Логика двуручного оружия ===
            if (equipmentData.handType == WeaponHandType.TwoHand)
            {
                return EquipTwoHand(equipmentData, grade, durability);
            }

            // === Логика WeaponOff при заблокированном слоте ===
            if (slot == EquipmentSlot.WeaponOff && isWeaponOffBlocked)
            {
                Debug.LogWarning("[EquipmentController] WeaponOff заблокирован двуручным оружием");
                return null;
            }

            // === Логика WeaponMain — если экипируем 1H, а там двуручное ===
            if (slot == EquipmentSlot.WeaponMain && equippedItems[EquipmentSlot.WeaponMain] != null)
            {
                var currentMain = equippedItems[EquipmentSlot.WeaponMain];
                if (currentMain.equipmentData.handType == WeaponHandType.TwoHand)
                {
                    // Снимаем двуручное оружие (освобождает оба слота)
                    Unequip(EquipmentSlot.WeaponMain);
                }
            }

            // === Стандартная экипировка — если слот занят, снимаем старое ===
            EquipmentInstance oldItem = null;
            if (equippedItems[slot] != null)
            {
                oldItem = Unequip(slot);
            }

            // Создаём экземпляр и надеваем
            var instance = CreateInstance(equipmentData, grade, durability);
            equippedItems[slot] = instance;
            statsDirty = true;

            OnEquipmentEquipped?.Invoke(slot, instance);
            OnStatsChanged?.Invoke(CurrentStats);

            return instance;
        }

        /// <summary>
        /// Экипирует двуручное оружие на WeaponMain + блокирует WeaponOff.
        /// </summary>
        private EquipmentInstance EquipTwoHand(EquipmentData equipmentData, EquipmentGrade grade, int durability)
        {
            // Если WeaponOff занят → снимаем
            if (equippedItems[EquipmentSlot.WeaponOff] != null)
            {
                Unequip(EquipmentSlot.WeaponOff);
            }

            // Если WeaponMain занят → снимаем
            if (equippedItems[EquipmentSlot.WeaponMain] != null)
            {
                Unequip(EquipmentSlot.WeaponMain);
            }

            // Экипируем двуручное на WeaponMain
            var instance = CreateInstance(equipmentData, grade, durability);
            equippedItems[EquipmentSlot.WeaponMain] = instance;

            // Блокируем WeaponOff
            isWeaponOffBlocked = true;
            OnWeaponOffBlockChanged?.Invoke(true);

            statsDirty = true;

            OnEquipmentEquipped?.Invoke(EquipmentSlot.WeaponMain, instance);
            OnStatsChanged?.Invoke(CurrentStats);

            return instance;
        }

        /// <summary>
        /// Снимает экипировку с указанного слота.
        /// 
        /// Для двуручного оружия: при снятии с WeaponMain — освобождает WeaponOff.
        /// </summary>
        /// <param name="slot">Слот для снятия</param>
        /// <returns>Снятый экземпляр или null</returns>
        public EquipmentInstance Unequip(EquipmentSlot slot)
        {
            var instance = equippedItems[slot];
            if (instance == null)
                return null;

            // Снимаем предмет
            equippedItems[slot] = null;

            // Если снимаем двуручное с WeaponMain — разблокируем WeaponOff
            if (slot == EquipmentSlot.WeaponMain && instance.equipmentData.handType == WeaponHandType.TwoHand)
            {
                isWeaponOffBlocked = false;
                OnWeaponOffBlockChanged?.Invoke(false);
            }

            statsDirty = true;

            OnEquipmentUnequipped?.Invoke(slot, instance);
            OnStatsChanged?.Invoke(CurrentStats);

            return instance;
        }

        /// <summary>
        /// Снимает всю экипировку.
        /// </summary>
        /// <returns>Список снятых предметов</returns>
        public List<EquipmentInstance> UnequipAll()
        {
            var allEquipment = new List<EquipmentInstance>();

            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                var instance = Unequip(slot);
                if (instance != null)
                    allEquipment.Add(instance);
            }

            return allEquipment;
        }

        #endregion

        #region Query

        /// <summary>
        /// Проверяет, можно ли экипировать предмет.
        /// Проверяет: уровень культивации, требования к статам.
        /// </summary>
        public bool CanEquip(EquipmentData equipmentData, int playerCultivationLevel = -1, Dictionary<string, float> playerStats = null)
        {
            if (equipmentData == null)
                return false;

            // Получаем уровень культивации, если не передан
            // BUG-8 fix: кэшируем ServiceLocator.GetOrFind вместо повторного вызова
            if (playerCultivationLevel < 0)
            {
                var qiCtrl = ServiceLocator.GetOrFind<QiController>();
                playerCultivationLevel = (qiCtrl != null) ? qiCtrl.CultivationLevel : 0;
            }

            // Получаем статы игрока, если не переданы
            if (playerStats == null)
            {
                var playerCtrl = ServiceLocator.GetOrFind<PlayerController>();
                if (playerCtrl != null && playerCtrl.StatDevelopment != null)
                    playerStats = playerCtrl.StatDevelopment.GetAllStatsAsDictionary();
            }

            // Проверка уровня культивации
            if (equipmentData.requiredCultivationLevel > 0 && playerCultivationLevel < equipmentData.requiredCultivationLevel)
            {
                return false;
            }

            // Проверка требований к статам
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
                        // Стат не найден — требование не выполнено
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Получает экипировку в указанном слоте.
        /// </summary>
        public EquipmentInstance GetEquipment(EquipmentSlot slot)
        {
            return equippedItems[slot];
        }

        /// <summary>
        /// Проверяет, занят ли слот.
        /// </summary>
        public bool IsSlotOccupied(EquipmentSlot slot)
        {
            return equippedItems[slot] != null;
        }

        /// <summary>
        /// Проверяет, является ли слот видимым (на кукле).
        /// </summary>
        public static bool IsSlotVisible(EquipmentSlot slot)
        {
            return Array.IndexOf(VisibleSlots, slot) >= 0;
        }

        /// <summary>
        /// Проверяет, является ли слот скрытым (заглушка).
        /// </summary>
        public static bool IsSlotHidden(EquipmentSlot slot)
        {
            return Array.IndexOf(HiddenSlots, slot) >= 0;
        }

        /// <summary>Слоты колец — скрытые, но функциональные для StorageRingData</summary>
        public static readonly EquipmentSlot[] RingSlots = new[]
        {
            EquipmentSlot.RingLeft1, EquipmentSlot.RingLeft2,
            EquipmentSlot.RingRight1, EquipmentSlot.RingRight2
        };

        /// <summary>
        /// Проверяет, является ли слот слотом кольца.
        /// </summary>
        public static bool IsRingSlot(EquipmentSlot slot)
        {
            return Array.IndexOf(RingSlots, slot) >= 0;
        }

        /// <summary>
        /// Получает все экипированные предметы.
        /// </summary>
        public List<EquipmentInstance> GetAllEquipped()
        {
            var result = new List<EquipmentInstance>();
            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null)
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>
        /// Получает все предметы определённой категории.
        /// </summary>
        public List<EquipmentInstance> GetEquipmentByCategory(ItemCategory category)
        {
            var result = new List<EquipmentInstance>();
            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null && kvp.Value.equipmentData.category == category)
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>
        /// Получает оружие в основной руке (с учётом двуручности).
        /// </summary>
        public EquipmentInstance GetMainWeapon()
        {
            return equippedItems[EquipmentSlot.WeaponMain];
        }

        /// <summary>
        /// Получает оружие во второй руке (если не заблокировано двуручным).
        /// </summary>
        public EquipmentInstance GetOffWeapon()
        {
            if (isWeaponOffBlocked) return null;
            return equippedItems[EquipmentSlot.WeaponOff];
        }

        // === ФАЗА 1: Боевые бонусы из экипировки ===

        /// <summary>
        /// Бонусный урон от оружия в основной руке (WeaponBonusDamage).
        /// Урон × множитель прочности по грейду.
        /// </summary>
        public float GetWeaponBonusDamage()
        {
            var mainWeapon = GetMainWeapon();
            if (mainWeapon == null) return 0f;
            return mainWeapon.equipmentData.damage * GetDurabilityMultiplier(mainWeapon.grade);
        }

        /// <summary>
        /// Базовый урон оружия в основной руке (WeaponDamage).
        /// FIX С-02: Добавлен для полной формулы урона оружия (EQUIPMENT_SYSTEM.md §7.3).
        /// Возвращает damage из EquipmentData без множителей грейда.
        /// </summary>
        public float GetWeaponDamage() 
        { 
            var weapon = GetEquipment(EquipmentSlot.WeaponMain);
            if (weapon?.equipmentData == null) return 0f;
            return weapon.equipmentData.damage;
        }

        /// <summary>
        /// Бонус парирования от оружия в основной руке.
        /// Извлекается из statBonuses с именем «parryBonus».
        /// </summary>
        public float GetParryBonus()
        {
            var mainWeapon = GetMainWeapon();
            if (mainWeapon == null) return 0f;
            return GetStatBonusFromItem(mainWeapon, "parryBonus");
        }

        /// <summary>
        /// Бонус блокирования от щита/оружия во второй руке.
        /// Извлекается из statBonuses с именем «blockBonus».
        /// </summary>
        public float GetBlockBonus()
        {
            var offItem = GetOffWeapon();
            if (offItem == null) return 0f;
            return GetStatBonusFromItem(offItem, "blockBonus");
        }

        /// <summary>
        /// Штраф к уклонению от надетой брони.
        /// EquipmentData.dodgeBonus: отрицательное = штраф, положительное = бонус.
        /// Возвращает штраф (положительное число = хуже уклонение).
        /// </summary>
        public float GetDodgePenalty()
        {
            return -CurrentStats.dodgeBonus;
        }

        /// <summary>
        /// Среднее покрытие брони (не оружия) в процентах.
        /// Учитывает только слоты: Head, Torso, Belt, Legs, Feet.
        /// </summary>
        public float GetArmorCoverage()
        {
            return CurrentStats.coverage;
        }

        /// <summary>
        /// Снижение урона от всей экипировки (%).
        /// </summary>
        public float GetDamageReduction()
        {
            return CurrentStats.damageReduction;
        }

        /// <summary>
        /// Общая защита от всей экипировки.
        /// </summary>
        public int GetArmorValue()
        {
            return CurrentStats.totalDefense;
        }

        /// <summary>
        /// Множитель инструмента для добычи ресурсов.
        /// Основан на уроне оружия в основной руке.
        /// Без оружия = 1.0f.
        /// </summary>
        public float GetToolMultiplier()
        {
            float weaponDamage = GetWeaponBonusDamage();
            if (weaponDamage <= 0f) return 1.0f;
            // Формула: 1.0 + урон/50 — железный меч (10 урон) = 1.2x, духовный клинок (50) = 2.0x
            return 1.0f + weaponDamage / 50f;
        }

        // === ФАЗА 2: Бонусы техник от оружия ===

        /// <summary>
        /// Бонус к урону техник от оружия (%).
        /// Сумма techniqueDamageBonus от WeaponMain + WeaponOff.
        /// </summary>
        public float GetTechniqueDamageBonus()
        {
            return CurrentStats.techniqueDamageBonus;
        }

        /// <summary>
        /// Снижение стоимости Ци техник от оружия (%).
        /// Сумма qiCostReduction от WeaponMain + WeaponOff.
        /// </summary>
        public float GetQiCostReduction()
        {
            return CurrentStats.qiCostReduction;
        }

        /// <summary>
        /// Ускорение накачки техник от оружия (%).
        /// Сумма chargeSpeedBonus от WeaponMain + WeaponOff.
        /// </summary>
        public float GetChargeSpeedBonus()
        {
            return CurrentStats.chargeSpeedBonus;
        }

        /// <summary>
        /// Эффективность парирования (0-1) из оружия в основной руке.
        /// В-01: Замена захардкоженного множителя 0.5 в DamageCalculator.
        /// Извлекается из statBonuses с именем «blockEffectiveness».
        /// Если не задано — по умолчанию 0.5 (парирование снижает урон на 50%).
        /// </summary>
        public float GetBlockEffectiveness()
        {
            var mainWeapon = GetMainWeapon();
            if (mainWeapon == null) return 0.5f;
            float bonus = GetStatBonusFromItem(mainWeapon, "blockEffectiveness");
            return bonus > 0f ? Mathf.Clamp01(bonus) : 0.5f;
        }

        /// <summary>
        /// Эффективность блокирования щитом (0-1) из оружия/щита во второй руке.
        /// В-01: Замена захардкоженного множителя 0.7 в DamageCalculator.
        /// Извлекается из statBonuses с именем «shieldEffectiveness».
        /// Если нет щита — возвращает 0.7 по умолчанию (блок снижает урон на 70%).
        /// </summary>
        public float GetShieldEffectiveness()
        {
            var offItem = GetOffWeapon();
            if (offItem == null) return 0.7f;
            float bonus = GetStatBonusFromItem(offItem, "shieldEffectiveness");
            return bonus > 0f ? Mathf.Clamp01(bonus) : 0.7f;
        }

        /// <summary>
        /// Извлечь значение бонуса из statBonuses предмета по имени.
        /// </summary>
        private float GetStatBonusFromItem(EquipmentInstance item, string statName)
        {
            if (item?.equipmentData?.statBonuses == null) return 0f;
            float effectivenessMult = GetEffectivenessMultiplier(item.grade);
            foreach (var bonus in item.equipmentData.statBonuses)
            {
                if (string.Equals(bonus.statName, statName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return bonus.isPercentage ? bonus.value : bonus.value * effectivenessMult;
                }
            }
            return 0f;
        }

        #endregion

        #region Stats Calculation

        /// <summary>
        /// Пересчитывает все статы от экипировки.
        /// </summary>
        private void RecalculateStats()
        {
            cachedStats = new EquipmentStats();

            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null)
                {
                    AddEquipmentStats(kvp.Value);
                }
            }

            statsDirty = false;
        }

        /// <summary>
        /// Добавляет статы одного предмета к кэшу.
        /// Использует РАЗНЫЕ множители для урона/защиты и бонусов.
        /// </summary>
        private void AddEquipmentStats(EquipmentInstance instance)
        {
            var data = instance.equipmentData;
            float durabilityMult = GetDurabilityMultiplier(instance.grade);
            float effectivenessMult = GetEffectivenessMultiplier(instance.grade);

            // Урон и защита — множитель прочности (×0.5..×4.0)
            cachedStats.totalDamage += Mathf.RoundToInt(data.damage * durabilityMult);
            cachedStats.totalDefense += Mathf.RoundToInt(data.defense * durabilityMult);
            cachedStats.damageReduction += data.damageReduction;
            cachedStats.dodgeBonus += data.dodgeBonus;

            // ФАЗА 1: Покрытие брони — только для не-оружейных слотов
            bool isWeaponSlot = (data.slot == EquipmentSlot.WeaponMain || data.slot == EquipmentSlot.WeaponOff);
            if (!isWeaponSlot)
            {
                cachedStats.coverage += data.coverage;
                cachedStats.moveSpeedPenalty += data.moveSpeedPenalty;
                cachedStats.qiFlowBonus -= data.qiFlowPenalty; // Инверсия: штраф → бонус (отрицательный qiFlowPenalty = положительный qiFlowBonus)
            }

            // ФАЗА 2: Бонусы техник от оружия — множитель эффективности
            if (isWeaponSlot)
            {
                cachedStats.techniqueDamageBonus += data.techniqueDamageBonus * effectivenessMult;
                cachedStats.qiCostReduction += data.qiCostReduction;
                cachedStats.chargeSpeedBonus += data.chargeSpeedBonus * effectivenessMult;
            }

            // Бонусы к характеристикам — множитель эффективности (×0.5..×3.25)
            foreach (var bonus in data.statBonuses)
            {
                // FIX EQP-BUG-02: используем рассчитанное value из StatBonus.value
                float val = bonus.isPercentage ? bonus.value : bonus.value * effectivenessMult;

                switch (bonus.statName.ToLower())
                {
                    case "strength":
                    case "str":
                        cachedStats.strength += val;
                        break;
                    case "agility":
                    case "agi":
                        cachedStats.agility += val;
                        break;
                    case "constitution":
                    case "con":
                        cachedStats.constitution += val;
                        break;
                    case "intelligence":
                    case "int":
                        cachedStats.intelligence += val;
                        break;
                    case "conductivity":
                    case "cond":
                        cachedStats.conductivity += val;
                        break;
                    case "qi":
                    case "maxqi":
                        cachedStats.maxQi += val;
                        break;
                    case "qiregen":
                        cachedStats.qiRegen += val;
                        break;
                    case "vitality":
                    case "vit":
                        cachedStats.vitality += val;
                        break;
                    default:
                        cachedStats.customBonuses[bonus.statName] =
                            cachedStats.GetCustomBonus(bonus.statName) + val;
                        break;
                }
            }
        }

        /// <summary>
        /// Множитель прочности — для урона и защиты.
        /// Источник: EQUIPMENT_SYSTEM.md §2.1 "Уровни качества"
        /// 
        /// | Грейд | Прочность |
        /// |-------|-----------|
        /// | Damaged | ×0.5 |
        /// | Common | ×1.0 |
        /// | Refined | ×1.5 |
        /// | Perfect | ×2.5 |
        /// | Transcendent | ×4.0 |
        /// </summary>
        private float GetDurabilityMultiplier(EquipmentGrade grade)
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

        /// <summary>
        /// Множитель эффективности — для бонусов к характеристикам.
        /// Источник: EQUIPMENT_SYSTEM.md §2.1 "Уровни качества"
        /// 
        /// | Грейд | Эффективность |
        /// |-------|---------------|
        /// | Damaged | ×0.5 |
        /// | Common | ×1.0 |
        /// FIX К-03: Множители приведены к EQUIPMENT_SYSTEM.md §2.1
        /// | Damaged      | ×0.5 |
        /// | Common       | ×1.0 |
        /// | Refined      | ×1.3 |
        /// | Perfect      | ×1.6 |
        /// | Transcendent | ×2.0 |
        /// </summary>
        private float GetEffectivenessMultiplier(EquipmentGrade grade)
        {
            return grade switch
            {
                EquipmentGrade.Damaged => 0.5f,
                EquipmentGrade.Common => 1.0f,
                EquipmentGrade.Refined => 1.3f,       // FIX К-03: было 1.4
                EquipmentGrade.Perfect => 1.6f,       // FIX К-03: было 2.1
                EquipmentGrade.Transcendent => 2.0f,  // FIX К-03: было 3.25
                _ => 1.0f
            };
        }

        #endregion

        #region Durability

        /// <summary>
        /// Наносит урон прочности экипировке в указанном слоте.
        /// </summary>
        public void DamageEquipment(EquipmentSlot slot, int amount)
        {
            var instance = GetEquipment(slot);
            if (instance == null || instance.durability < 0)
                return;

            instance.durability = Mathf.Max(0, instance.durability - amount);

            // Снижаем статы при поломке
            statsDirty = true;
            OnStatsChanged?.Invoke(CurrentStats);

            if (instance.durability == 0)
            {
                // Экипировка сломана — уведомление
                Debug.Log($"[EquipmentController] Экипировка в слоте {slot} сломана!");
            }
        }

        /// <summary>
        /// Чинит экипировку в указанном слоте.
        /// </summary>
        public void RepairEquipment(EquipmentSlot slot, int amount)
        {
            var instance = GetEquipment(slot);
            if (instance == null || instance.durability < 0)
                return;

            instance.durability = Mathf.Min(instance.equipmentData.maxDurability, instance.durability + amount);
        }

        /// <summary>
        /// Полностью чинит всю экипировку.
        /// </summary>
        public void RepairAll()
        {
            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null && kvp.Value.durability >= 0)
                {
                    kvp.Value.durability = kvp.Value.equipmentData.maxDurability;
                }
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Создаёт экземпляр экипировки.
        /// </summary>
        private EquipmentInstance CreateInstance(EquipmentData data, EquipmentGrade grade, int durability)
        {
            return new EquipmentInstance
            {
                equipmentData = data,
                grade = grade,
                durability = durability > 0 ? durability : data.maxDurability
            };
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Получает данные для сохранения.
        /// Формат упрощён — 1 предмет на слот, без слоёв.
        /// </summary>
        public Dictionary<string, EquipmentSaveData> GetSaveData()
        {
            var data = new Dictionary<string, EquipmentSaveData>();

            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null)
                {
                    data[kvp.Key.ToString()] = new EquipmentSaveData
                    {
                        slot = kvp.Key,
                        itemId = kvp.Value.equipmentData.itemId,
                        grade = kvp.Value.grade,
                        durability = kvp.Value.durability,
                        isTwoHandBlocking = (kvp.Key == EquipmentSlot.WeaponMain &&
                                             kvp.Value.equipmentData.handType == WeaponHandType.TwoHand)
                    };
                }
            }

            return data;
        }

        /// <summary>
        /// Загружает данные.
        /// </summary>
        public void LoadSaveData(Dictionary<string, EquipmentSaveData> data, Dictionary<string, EquipmentData> itemDatabase)
        {
            UnequipAll();

            if (data == null) return;

            foreach (var kvp in data)
            {
                var slotData = kvp.Value;

                if (itemDatabase.TryGetValue(slotData.itemId, out var equipmentData))
                {
                    Equip(equipmentData, slotData.grade, slotData.durability);
                }
            }
        }

        #endregion
    }

    // ============================================================================
    // EquipmentInstance — Экземпляр экипировки (v2.0)
    // ============================================================================
    // Убрано поле currentLayer — нет слоёв в v2.0
    // Добавлено свойство IsTwoHand
    // ============================================================================

    [Serializable]
    public class EquipmentInstance
    {
        public EquipmentData equipmentData;
        public EquipmentGrade grade;
        public int durability;

        // Properties
        public string ItemId => equipmentData?.itemId ?? "";
        public string Name => equipmentData?.nameRu ?? "Unknown";
        public EquipmentSlot Slot => equipmentData?.slot ?? EquipmentSlot.None;
        public WeaponHandType HandType => equipmentData?.handType ?? WeaponHandType.OneHand;
        public bool IsTwoHand => HandType == WeaponHandType.TwoHand;
        public int MaxDurability => equipmentData?.maxDurability ?? 100;
        public float DurabilityPercent => MaxDurability > 0 ? (float)durability / MaxDurability : 1f;
        public DurabilityCondition Condition => GetCondition();

        private DurabilityCondition GetCondition()
        {
            if (durability < 0) return DurabilityCondition.Pristine;
            if (durability == 0) return DurabilityCondition.Broken;

            float percent = DurabilityPercent * 100f;
            // FIX С-01: Убрано Excellent, приведено к 5 состояниям (EQUIPMENT_SYSTEM.md §4.1)
            if (percent >= 100f) return DurabilityCondition.Pristine;
            if (percent >= 80f) return DurabilityCondition.Good;
            if (percent >= 60f) return DurabilityCondition.Worn;
            if (percent >= 20f) return DurabilityCondition.Damaged;
            return DurabilityCondition.Broken;
        }
    }

    // ============================================================================
    // EquipmentStats — Вычисленные статы (v2.0)
    // ============================================================================
    // Добавлены: conductivity, vitality
    // ============================================================================

    [Serializable]
    public class EquipmentStats
    {
        // Боевые статы
        public int totalDamage;
        public int totalDefense;
        public float damageReduction;
        public float dodgeBonus;
        public float coverage;           // ФАЗА 1: среднее покрытие брони (%)
        public float moveSpeedPenalty;    // ФАЗА 1: штраф к скорости движения
        public float qiFlowBonus;         // ФАЗА 1: бонус/штраф к потоку Ци (инвертированный qiFlowPenalty)

        // ФАЗА 2: Бонусы техник от оружия
        public float techniqueDamageBonus;  // % бонус к урону техник
        public float qiCostReduction;       // % снижения стоимости Ци техник
        public float chargeSpeedBonus;      // % ускорения накачки техник

        // Характеристики
        public float strength;
        public float agility;
        public float constitution;
        public float intelligence;
        public float conductivity;
        public float vitality;

        // Ци
        public float maxQi;
        public float qiRegen;

        // Произвольные бонусы — runtime Dictionary, использовать ToSerializable/FromSerializable для сохранения
        public Dictionary<string, float> customBonuses = new Dictionary<string, float>();

        public float GetCustomBonus(string statName)
        {
            return customBonuses.TryGetValue(statName, out var value) ? value : 0f;
        }

        /// <summary>
        /// FIX SAV-H01: Convert customBonuses to serializable array for JsonUtility
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
        /// FIX SAV-H01: Restore customBonuses from serializable array
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
                result.coverage += stat.coverage;
                result.moveSpeedPenalty += stat.moveSpeedPenalty;
                result.qiFlowBonus += stat.qiFlowBonus;
                result.techniqueDamageBonus += stat.techniqueDamageBonus;
                result.qiCostReduction += stat.qiCostReduction;
                result.chargeSpeedBonus += stat.chargeSpeedBonus;
                result.strength += stat.strength;
                result.agility += stat.agility;
                result.constitution += stat.constitution;
                result.intelligence += stat.intelligence;
                result.conductivity += stat.conductivity;
                result.vitality += stat.vitality;
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
    // Save Data (v2.0 — упрощённый формат, 1 предмет на слот)
    // ============================================================================

    [Serializable]
    public class EquipmentSaveData
    {
        public EquipmentSlot slot;
        public string itemId;
        public EquipmentGrade grade;
        public int durability;
        public bool isTwoHandBlocking;
    }
}

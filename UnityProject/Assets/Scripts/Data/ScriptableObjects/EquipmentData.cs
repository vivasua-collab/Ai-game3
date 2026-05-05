// ============================================================================
// EquipmentData.cs — Данные экипировки (оружие, броня, аксессуары)
// Cultivation World Simulator
// Создано: 2026-04-13 14:03:25 UTC
// Редактировано: 2026-04-29 08:55:00 UTC — добавлены moveSpeedPenalty, qiFlowPenalty, equippedSprite (Д6, Д7)
// Редактировано: 2026-05-07 10:30:00 UTC — ФАЗА 2: techniqueDamageBonus, qiCostReduction, chargeSpeedBonus
// ============================================================================
//
// ВЫНЕСЕН ИЗ ItemData.cs — Unity требует совпадение имени файла и класса
// для ScriptableObject с [CreateAssetMenu].
//
// УБРАНО: layers: List<EquipmentLayer> — легаси Матрёшка v1, не используется.
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;
using CultivationGame.Data; // С-07: StatBonus перемещён в CultivationGame.Data

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные экипировки (оружие, броня, аксессуары)
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment", menuName = "Cultivation/Equipment")]
    public class EquipmentData : ItemData
    {
        // FIX ИСП-ИНВ-02: Экипировка НЕ стакается — каждый экземпляр уникален (grade, durability)
        protected override void OnEnable()
        {
            base.OnEnable();
            stackable = false;
        }
        [Header("Equipment")]
        [Tooltip("Слот экипировки")]
        public EquipmentSlot slot;

        [Tooltip("Тип хвата (одноручное/двуручное)")]
        public WeaponHandType handType = WeaponHandType.OneHand;

        [Header("Stats")]
        [Tooltip("Урон (для оружия)")]
        public int damage = 0;

        [Tooltip("Защита (для брони)")]
        public int defense = 0;

        [Tooltip("Покрытие брони (%)")]
        [Range(0f, 100f)]
        public float coverage = 100f;

        [Tooltip("Снижение урона (%)")]
        [Range(0f, 80f)]
        public float damageReduction = 0f;

        [Tooltip("Бонус к уклонению (%)")]
        [Range(-50f, 50f)]
        public float dodgeBonus = 0f;

        [Header("Penalties")]
        [Tooltip("Штраф скорости перемещения (%) — отрицательный")]
        [Range(-50f, 0f)]
        public float moveSpeedPenalty = 0f;

        [Tooltip("Штраф проводимости Ци (%) — может быть отрицательным или положительным")]
        [Range(-30f, 30f)]
        public float qiFlowPenalty = 0f;

        [Header("Visuals")]
        [Tooltip("Спрайт надетой экипировки (overlay на персонаже)")]
        public Sprite equippedSprite;

        [Header("Material")]
        [Tooltip("ID материала")]
        public string materialId;

        [Tooltip("Тир материала")]
        [Range(1, 5)]
        public int materialTier = 1;

        [Tooltip("Грейд экипировки")]
        public EquipmentGrade grade = EquipmentGrade.Common;

        [Tooltip("Уровень предмета (1-9)")]
        [Range(1, 9)]
        public int itemLevel = 1;

        [Header("Bonuses")]
        [Tooltip("Бонусы к характеристикам")]
        public List<StatBonus> statBonuses = new List<StatBonus>();

        [Tooltip("Особые эффекты")]
        public List<SpecialEffect> specialEffects = new List<SpecialEffect>();

        [Header("Technique Bonuses")]
        [Tooltip("Бонус к урону техник (%) — для магического оружия")]
        [Range(0f, 50f)]
        public float techniqueDamageBonus = 0f;

        [Tooltip("Снижение стоимости Ци техник (%) — от качества оружия")]
        [Range(0f, 30f)]
        public float qiCostReduction = 0f;

        [Tooltip("Ускорение накачки техник (%) — для духовного оружия")]
        [Range(0f, 25f)]
        public float chargeSpeedBonus = 0f;
    }
}

// ============================================================================
// TooltipPanel.cs — Карточка предмета (tooltip) с volume + nesting
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-18 20:00:00 UTC
// ============================================================================
// Расширенная карточка предмета по INVENTORY_UI_DRAFT.md §5.4:
// - Редкость (цвет рамки)
// - Название, тип, категория
// - Характеристики (урон, защита, бонусы)
// - Прочность
// - Объём + Флаг вложения (NEW v2.0)
// - Требования
// - Описание
// ============================================================================

using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Inventory;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.UI.Inventory
{
    /// <summary>
    /// Карточка предмета — всплывающая подсказка при наведении.
    /// Показывает все данные предмета включая volume и allowNesting.
    /// </summary>
    public class TooltipPanel : MonoBehaviour
    {
        #region Configuration

        [Header("Header")]
        [SerializeField] private Image rarityBorder;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text typeText;

        [Header("Combat Stats")]
        [SerializeField] private GameObject combatSection;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text defenseText;
        [SerializeField] private TMP_Text coverageText;
        [SerializeField] private TMP_Text damageReductionText;
        [SerializeField] private TMP_Text dodgeBonusText;

        [Header("Physical")]
        [SerializeField] private GameObject physicalSection;
        [SerializeField] private TMP_Text weightText;
        [SerializeField] private TMP_Text volumeText;
        [SerializeField] private TMP_Text nestingText;
        [SerializeField] private TMP_Text durabilityText;

        [Header("Bonuses")]
        [SerializeField] private GameObject bonusesSection;
        [SerializeField] private TMP_Text bonusesText;

        [Header("Material / Grade")]
        [SerializeField] private GameObject materialSection;
        [SerializeField] private TMP_Text materialText;
        [SerializeField] private TMP_Text gradeText;

        [Header("Requirements")]
        [SerializeField] private GameObject requirementsSection;
        [SerializeField] private TMP_Text requirementsText;

        [Header("Description")]
        [SerializeField] private TMP_Text descriptionText;

        [Header("Value")]
        [SerializeField] private TMP_Text valueText;

        [Header("Layout")]
        [SerializeField] private float offsetFromCursor = 15f;
        [SerializeField] private float maxScreenWidth = 350f;

        #endregion

        #region Properties

        /// <summary>Отображается ли tooltip</summary>
        public bool IsVisible => gameObject.activeSelf;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Show / Hide

        /// <summary>
        /// Показывает карточку предмета из инвентаря.
        /// </summary>
        public void ShowForInventorySlot(InventorySlot slot, Vector2 cursorPosition)
        {
            if (slot == null || slot.ItemData == null)
            {
                Hide();
                return;
            }

            var itemData = slot.ItemData;
            var equipData = itemData as EquipmentData;
            var materialData = itemData as MaterialData;

            FillCommonFields(itemData, slot.Count);
            FillPhysicalFields(itemData, slot.Durability);
            FillNestingField(itemData);

            // Экипировка
            if (equipData != null)
            {
                FillEquipmentFields(equipData);
            }
            else
            {
                HideEquipmentFields();
            }

            // Материал
            if (materialData != null)
            {
                FillMaterialFields(materialData);
            }
            else if (equipData != null)
            {
                FillEquipmentMaterialFields(equipData);
            }
            else
            {
                HideMaterialFields();
            }

            // Бонусы
            FillBonuses(itemData, equipData);

            // Требования
            FillRequirements(itemData);

            // Описание
            if (descriptionText != null)
                descriptionText.text = itemData.description;

            // Стоимость
            if (valueText != null)
                valueText.text = $"Стоимость: {itemData.value * slot.Count} камней";

            // Позиция
            PositionTooltip(cursorPosition);

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Показывает карточку для экипировки на кукле.
        /// </summary>
        public void ShowForEquipment(EquipmentInstance instance, Vector2 cursorPosition)
        {
            if (instance == null || instance.equipmentData == null)
            {
                Hide();
                return;
            }

            var itemData = instance.equipmentData;
            var equipData = instance.equipmentData;

            FillCommonFields(itemData, 1);
            FillPhysicalFields(itemData, instance.durability);
            FillNestingField(itemData);
            FillEquipmentFields(equipData);
            FillEquipmentMaterialFields(equipData);
            FillBonuses(itemData, equipData);
            FillRequirements(itemData);

            if (descriptionText != null)
                descriptionText.text = itemData.description;

            if (valueText != null)
                valueText.text = $"Стоимость: {itemData.value} камней";

            PositionTooltip(cursorPosition);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Скрывает карточку.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Fill Methods

        private void FillCommonFields(ItemData item, int count)
        {
            // Название
            if (nameText != null)
            {
                nameText.text = count > 1 ? $"{item.nameRu} ×{count}" : item.nameRu;
                nameText.color = InventorySlotUI.GetRarityColor(item.rarity);
            }

            // Редкость
            if (rarityText != null)
                rarityText.text = GetRarityName(item.rarity);

            // Тип
            if (typeText != null)
                typeText.text = GetCategoryName(item.category);

            // Рамка по редкости
            if (rarityBorder != null)
                rarityBorder.color = InventorySlotUI.GetRarityColor(item.rarity);
        }

        private void FillPhysicalFields(ItemData item, int durability)
        {
            if (physicalSection != null)
                physicalSection.SetActive(true);

            if (weightText != null)
                weightText.text = $"Вес: {item.weight:F1} кг";

            if (volumeText != null)
                volumeText.text = $"Объём: {item.volume:F1}";

            if (durabilityText != null)
            {
                if (item.hasDurability)
                {
                    float percent = item.maxDurability > 0 ? (float)durability / item.maxDurability * 100f : 0f;
                    durabilityText.text = $"Прочность: {durability}/{item.maxDurability} ({percent:F0}%)";
                    durabilityText.color = GetDurabilityColor(percent);
                }
                else
                {
                    durabilityText.text = "";
                }
            }
        }

        /// <summary>
        /// Заполняет поле флага вложения (NEW v2.0).
        /// </summary>
        private void FillNestingField(ItemData item)
        {
            if (nestingText != null)
            {
                nestingText.text = item.allowNesting switch
                {
                    NestingFlag.None => "Вложение: ✗ Запрещено",
                    NestingFlag.Spirit => "Вложение: △ Только дух. хранилище",
                    NestingFlag.Ring => "Вложение: ○ Только кольцо хранения",
                    NestingFlag.Any => "Вложение: ✓ Любое",
                    _ => ""
                };

                nestingText.color = item.allowNesting switch
                {
                    NestingFlag.None => new Color(0.8f, 0.3f, 0.3f),
                    NestingFlag.Spirit => new Color(0.5f, 0.5f, 0.9f),
                    NestingFlag.Ring => new Color(0.9f, 0.7f, 0.3f),
                    NestingFlag.Any => new Color(0.3f, 0.8f, 0.3f),
                    _ => Color.white
                };
            }
        }

        private void FillEquipmentFields(EquipmentData equip)
        {
            if (combatSection != null)
                combatSection.SetActive(true);

            if (damageText != null)
                damageText.text = equip.damage > 0 ? $"Урон: {equip.damage}" : "";

            if (defenseText != null)
                defenseText.text = equip.defense > 0 ? $"Защита: {equip.defense}" : "";

            if (coverageText != null)
                coverageText.text = equip.coverage > 0 ? $"Покрытие: {equip.coverage:F0}%" : "";

            if (damageReductionText != null)
                damageReductionText.text = equip.damageReduction > 0 ? $"Снижение урона: {equip.damageReduction:F0}%" : "";

            if (dodgeBonusText != null)
                dodgeBonusText.text = equip.dodgeBonus > 0 ? $"Уклонение: +{equip.dodgeBonus:F0}%" : "";

            // Тип хвата
            if (typeText != null)
            {
                string handInfo = equip.handType == WeaponHandType.TwoHand ? " (Двуручное)" : "";
                typeText.text = GetCategoryName(equip.category) + handInfo;
            }
        }

        private void HideEquipmentFields()
        {
            if (combatSection != null)
                combatSection.SetActive(false);
        }

        private void FillMaterialFields(MaterialData material)
        {
            if (materialSection != null)
                materialSection.SetActive(true);

            if (materialText != null)
                materialText.text = $"{GetMaterialCategoryName(material.materialCategory)} (T{material.tier})";

            if (gradeText != null)
                gradeText.text = "";
        }

        private void FillEquipmentMaterialFields(EquipmentData equip)
        {
            if (materialSection != null)
                materialSection.SetActive(true);

            if (materialText != null)
                materialText.text = !string.IsNullOrEmpty(equip.materialId) ? $"Материал: {equip.materialId}" : "";

            if (gradeText != null)
                gradeText.text = equip.grade != EquipmentGrade.Common ? $"Грейд: {GetGradeName(equip.grade)}" : "";
        }

        private void HideMaterialFields()
        {
            if (materialSection != null)
                materialSection.SetActive(false);
        }

        private void FillBonuses(ItemData item, EquipmentData equip)
        {
            var sb = new StringBuilder();

            // Бонусы экипировки
            if (equip != null && equip.statBonuses != null)
            {
                foreach (var bonus in equip.statBonuses)
                {
                    string sign = bonus.bonus >= 0 ? "+" : "";
                    string pct = bonus.isPercentage ? "%" : "";
                    sb.AppendLine($"  {GetStatDisplayName(bonus.statName)}: {sign}{bonus.bonus:F0}{pct}");
                }
            }

            // Спецэффекты
            if (equip != null && equip.specialEffects != null)
            {
                foreach (var effect in equip.specialEffects)
                {
                    sb.AppendLine($"  ★ {effect.effectName} ({effect.triggerChance:F0}%)");
                }
            }

            // Эффекты расходников
            if (item.effects != null && item.effects.Count > 0)
            {
                foreach (var effect in item.effects)
                {
                    sb.AppendLine($"  • {effect.effectType}: {effect.value} ({effect.duration} сек)");
                }
            }

            if (bonusesSection != null)
                bonusesSection.SetActive(sb.Length > 0);

            if (bonusesText != null)
                bonusesText.text = sb.ToString();
        }

        private void FillRequirements(ItemData item)
        {
            var sb = new StringBuilder();

            if (item.requiredCultivationLevel > 0)
            {
                sb.AppendLine($"  Культивация: {item.requiredCultivationLevel}+");
            }

            if (item.statRequirements != null)
            {
                foreach (var req in item.statRequirements)
                {
                    sb.AppendLine($"  {GetStatDisplayName(req.statName)}: {req.minValue}+");
                }
            }

            if (requirementsSection != null)
                requirementsSection.SetActive(sb.Length > 0);

            if (requirementsText != null)
                requirementsText.text = sb.ToString();
        }

        #endregion

        #region Position

        private void PositionTooltip(Vector2 cursorPosition)
        {
            var rect = GetComponent<RectTransform>();
            if (rect == null) return;

            // Смещение от курсора
            Vector2 position = cursorPosition + new Vector2(offsetFromCursor, -offsetFromCursor);

            // Проверяем, не выходит ли за экран
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    // Ограничение по правому краю
                    float rightEdge = canvasRect.rect.width - maxScreenWidth;
                    if (position.x > rightEdge)
                        position.x = cursorPosition.x - maxScreenWidth - offsetFromCursor;

                    // Ограничение по нижнему краю
                    float tooltipHeight = rect.rect.height > 0 ? rect.rect.height : 300f;
                    if (position.y - tooltipHeight < -canvasRect.rect.height)
                        position.y = cursorPosition.y + tooltipHeight + offsetFromCursor;
                }
            }

            rect.position = position;
        }

        #endregion

        #region Utility

        private static string GetRarityName(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => "Обычный",
                ItemRarity.Uncommon => "Необычный",
                ItemRarity.Rare => "Редкий",
                ItemRarity.Epic => "Эпический",
                ItemRarity.Legendary => "Легендарный",
                ItemRarity.Mythic => "Мифический",
                _ => rarity.ToString()
            };
        }

        private static string GetCategoryName(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Weapon => "Оружие",
                ItemCategory.Armor => "Броня",
                ItemCategory.Accessory => "Аксессуар",
                ItemCategory.Consumable => "Расходник",
                ItemCategory.Material => "Материал",
                ItemCategory.Technique => "Свиток техники",
                ItemCategory.Quest => "Квестовый предмет",
                ItemCategory.Misc => "Разное",
                _ => category.ToString()
            };
        }

        private static string GetMaterialCategoryName(MaterialCategory category)
        {
            return category switch
            {
                MaterialCategory.Metal => "Металл",
                MaterialCategory.Leather => "Кожа",
                MaterialCategory.Cloth => "Ткань",
                MaterialCategory.Wood => "Дерево",
                MaterialCategory.Bone => "Кость",
                MaterialCategory.Crystal => "Кристалл",
                MaterialCategory.Gem => "Драг. камень",
                MaterialCategory.Organic => "Органика",
                MaterialCategory.Spirit => "Духовный",
                MaterialCategory.Void => "Пустотный",
                _ => category.ToString()
            };
        }

        private static string GetGradeName(EquipmentGrade grade)
        {
            return grade switch
            {
                EquipmentGrade.Damaged => "Повреждённый",
                EquipmentGrade.Common => "Обычный",
                EquipmentGrade.Refined => "Очищенный",
                EquipmentGrade.Perfect => "Совершенный",
                EquipmentGrade.Transcendent => "Трансцендентный",
                _ => grade.ToString()
            };
        }

        private static string GetStatDisplayName(string statName)
        {
            return statName.ToLower() switch
            {
                "strength" or "str" => "Сила",
                "agility" or "agi" => "Ловкость",
                "constitution" or "con" => "Телосложение",
                "intelligence" or "int" => "Интеллект",
                "conductivity" or "cond" => "Проводимость",
                "vitality" or "vit" => "Живучесть",
                "qi" or "maxqi" => "Макс. Ци",
                "qiregen" => "Реген. Ци",
                _ => statName
            };
        }

        private static Color GetDurabilityColor(float percent)
        {
            if (percent >= 80) return new Color(0.3f, 0.9f, 0.3f);
            if (percent >= 60) return new Color(0.9f, 0.9f, 0.3f);
            if (percent >= 40) return new Color(0.9f, 0.6f, 0.2f);
            if (percent >= 20) return new Color(0.9f, 0.3f, 0.3f);
            return new Color(0.6f, 0.2f, 0.2f);
        }

        #endregion
    }
}

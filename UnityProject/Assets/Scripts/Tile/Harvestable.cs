// ============================================================================
// Harvestable.cs — Компонент добычи для статичных объектов
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-16
// Чекпоинт: 04_15_harvest_system_plan.md v3 §4
// ============================================================================
//
// Компонент для объектов, которые можно добывать через кнопку F.
// НЕ наследует Interactable — независимый MonoBehaviour.
// Поиск через Physics2D по слою "Harvestable".
//
// Механика:
// - Прогресс-бар заполняется за 1 тик добычи (одно нажатие F)
// - Каждый тик уменьшает durability на harvestDamage
// - При исчерпании — смена спрайта (пень/пустая жила/обломки)
// - НЕТ респауна — объект остаётся «мёртвым» до перегенерации локации
// ============================================================================

using System;
using UnityEngine;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Категория объекта добычи — определяет эффективность инструмента.
    /// См. docs_temp/tool_system_draft.md §Категории объектов добычи.
    /// </summary>
    public enum HarvestableCategory
    {
        None = 0,
        Wood = 1,       // Деревья (Tree_Oak, Tree_Pine, Tree_Birch)
        Stone = 2,      // Камень (Rock_Small, Rock_Medium)
        Ore = 3,        // Руда (OreVein)
        Plant = 4       // Растения (Bush, Bush_Berry, Herb)
    }

    /// <summary>
    /// Компонент добычи для статичных объектов (руды, деревья, камни, растения).
    /// Навешивается на GameObject, спавнится HarvestableSpawner.
    /// </summary>
    public class Harvestable : MonoBehaviour
    {
        // === Identity ===

        [Header("Identity")]
        [Tooltip("Идентификатор ресурса: \"wood\", \"stone\", \"ore\", \"herb\", \"berries\"")]
        [SerializeField] private string resourceId = "wood";

        [Tooltip("Отображаемое имя: \"Древесина\", \"Руда\" (для UI)")]
        [SerializeField] private string displayName = "Древесина";

        [Tooltip("Категория объекта — определяет эффективность инструмента")]
        [SerializeField] private HarvestableCategory category = HarvestableCategory.Wood;

        // === Durability ===

        [Header("Durability")]
        [Tooltip("Максимальная прочность объекта")]
        [SerializeField] private int maxDurability = 200;

        [Tooltip("Текущая прочность")]
        [SerializeField] private int currentDurability = 200;

        // === Harvest ===

        [Header("Harvest")]
        [Tooltip("Урон по durability за один тик добычи")]
        [SerializeField] private int harvestDamage = 25;

        [Tooltip("Длительность одного тика добычи (заполнение прогресс-бара)")]
        [SerializeField] private float harvestDuration = 0.6f;

        [Tooltip("Кулдаун между тиками добычи (секунды)")]
        [SerializeField] private float harvestCooldown = 0.8f;

        [Tooltip("Дальность добычи (радиус поиска)")]
        [SerializeField] private float harvestRange = 2.5f;

        // === Yield ===

        [Header("Yield")]
        [Tooltip("Базовое количество ресурса за тик добычи")]
        [SerializeField] private int baseYieldPerTick = 2;

        [Tooltip("Общее количество ресурса в объекте")]
        [SerializeField] private int totalResourceCount = 10;

        [Tooltip("Сколько уже добыто")]
        [SerializeField] private int harvestedSoFar;

        // === Visuals ===

        [Header("Visuals")]
        [Tooltip("Нормальный спрайт объекта")]
        [SerializeField] private Sprite normalSprite;

        [Tooltip("Спрайт после исчерпания (пень, пустая жила, обломки)")]
        [SerializeField] private Sprite depletedSprite;

        // === State ===

        [Header("State")]
        [Tooltip("Исчерпан ли объект?")]
        [SerializeField] private bool isDepleted = false;

        // === Runtime ===

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D solidCollider;

        // === События ===

        /// <summary>(объект, количество полученного ресурса)</summary>
        public event Action<Harvestable, int> OnHarvested;

        /// <summary>Объект исчерпан</summary>
        public event Action<Harvestable> OnDepleted;

        // === Публичные свойства (только чтение) ===

        public string ResourceId => resourceId;
        public string DisplayName => displayName;
        public HarvestableCategory Category => category;
        public int MaxDurability => maxDurability;
        public int CurrentDurability => currentDurability;
        public int HarvestDamage => harvestDamage;
        public float HarvestDuration => harvestDuration;
        public float HarvestCooldown => harvestCooldown;
        public float HarvestRange => harvestRange;
        public int BaseYieldPerTick => baseYieldPerTick;
        public int TotalResourceCount => totalResourceCount;
        public int HarvestedSoFar => harvestedSoFar;
        public bool IsDepleted => isDepleted;

        // === Методы жизненного цикла ===

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            solidCollider = GetComponent<BoxCollider2D>();
        }

        // === Публичные методы ===

        /// <summary>
        /// Можно ли добывать этот объект?
        /// </summary>
        public bool CanHarvest()
        {
            return !isDepleted && currentDurability > 0;
        }

        /// <summary>
        /// Один тик добычи. Наносит урон по durability, рассчитывает лут.
        /// Возвращает количество полученного ресурса.
        /// </summary>
        /// <param name="damage">Урон по durability (может быть модифицирован инструментом)</param>
        /// <param name="yieldMultiplier">Множитель лута (1.0 без инструмента)</param>
        /// <returns>Количество добытого ресурса за этот тик</returns>
        public int HarvestHit(int damage, float yieldMultiplier = 1.0f)
        {
            if (!CanHarvest())
                return 0;

            // Нанести урон
            currentDurability = Mathf.Max(0, currentDurability - damage);

            // Рассчитать лут
            int baseYield = Mathf.FloorToInt(baseYieldPerTick * yieldMultiplier);
            int remaining = totalResourceCount - harvestedSoFar;
            int actualYield = Mathf.Min(baseYield, remaining);
            actualYield = Mathf.Max(0, actualYield); // Защита от отрицательных

            if (actualYield > 0)
            {
                harvestedSoFar += actualYield;
                OnHarvested?.Invoke(this, actualYield);
            }

            // Проверить исчерпание
            if (currentDurability <= 0)
            {
                Deplete();
            }

            return actualYield;
        }

        /// <summary>
        /// Исчерпание объекта. Смена спрайта, isDepleted=true.
        /// Коллайдер уменьшается (пень меньше дерева).
        /// </summary>
        public void Deplete()
        {
            if (isDepleted) return;

            isDepleted = true;
            currentDurability = 0;

            // Смена спрайта
            if (spriteRenderer != null && depletedSprite != null)
            {
                spriteRenderer.sprite = depletedSprite;
            }
            else if (spriteRenderer != null)
            {
                // Fallback: затемнить спрайт при отсутствии depleted-спрайта
                spriteRenderer.color = new Color(0.5f, 0.4f, 0.3f, 0.7f);
            }

            // Уменьшить коллайдер (пень меньше дерева)
            if (solidCollider != null)
            {
                Vector2 size = solidCollider.size;
                solidCollider.size = new Vector2(size.x * 0.5f, size.y * 0.4f);
                // Сдвинуть центр вниз
                solidCollider.offset = new Vector2(0, -size.y * 0.2f);
            }

            OnDepleted?.Invoke(this);

            Debug.Log($"[Harvestable] Объект \"{displayName}\" исчерпан. Добыто: {harvestedSoFar}/{totalResourceCount}");
        }

        /// <summary>
        /// Инициализация из TileObjectData (используется HarvestableSpawner).
        /// </summary>
        public void Initialize(TileObjectData data)
        {
            if (data == null) return;

            resourceId = data.resourceId ?? "unknown";
            maxDurability = data.maxDurability;
            currentDurability = data.currentDurability;
            totalResourceCount = data.resourceCount;
            isDepleted = false;
            harvestedSoFar = 0;

            // Определить категорию и displayName из TileObjectType
            SetCategoryFromType(data.objectType);
        }

        /// <summary>
        /// Получить прогресс прочности (0..1), где 1 = полный, 0 = разрушен.
        /// </summary>
        public float GetDurabilityProgress()
        {
            if (maxDurability <= 0) return 0f;
            return (float)currentDurability / maxDurability;
        }

        /// <summary>
        /// Получить оставшееся количество ресурса.
        /// </summary>
        public int GetRemainingResource()
        {
            return Mathf.Max(0, totalResourceCount - harvestedSoFar);
        }

        // === Приватные методы ===

        /// <summary>
        /// Определить категорию и displayName из TileObjectType.
        /// Маппинг из чекпоинта §6.4.
        /// </summary>
        private void SetCategoryFromType(TileObjectType objectType)
        {
            switch (objectType)
            {
                case TileObjectType.Tree_Oak:
                    category = HarvestableCategory.Wood;
                    displayName = "Древесина (Дуб)";
                    baseYieldPerTick = 2;
                    totalResourceCount = 10;
                    break;

                case TileObjectType.Tree_Pine:
                    category = HarvestableCategory.Wood;
                    displayName = "Древесина (Сосна)";
                    baseYieldPerTick = 2;
                    totalResourceCount = 10;
                    break;

                case TileObjectType.Tree_Birch:
                    category = HarvestableCategory.Wood;
                    displayName = "Древесина (Берёза)";
                    baseYieldPerTick = 2;
                    totalResourceCount = 8;
                    maxDurability = 150;
                    break;

                case TileObjectType.Rock_Small:
                    category = HarvestableCategory.Stone;
                    displayName = "Камень";
                    baseYieldPerTick = 1;
                    totalResourceCount = 3;
                    maxDurability = 100;
                    break;

                case TileObjectType.Rock_Medium:
                    category = HarvestableCategory.Stone;
                    displayName = "Камень";
                    baseYieldPerTick = 1;
                    totalResourceCount = 10;
                    maxDurability = 300;
                    break;

                case TileObjectType.OreVein:
                    category = HarvestableCategory.Ore;
                    displayName = "Руда";
                    baseYieldPerTick = 1;
                    totalResourceCount = 15;
                    maxDurability = 400;
                    break;

                case TileObjectType.Bush:
                    category = HarvestableCategory.Plant;
                    displayName = "Ягоды";
                    baseYieldPerTick = 3;
                    totalResourceCount = 5;
                    maxDurability = 50;
                    break;

                case TileObjectType.Bush_Berry:
                    category = HarvestableCategory.Plant;
                    displayName = "Ягоды";
                    baseYieldPerTick = 3;
                    totalResourceCount = 5;
                    maxDurability = 50;
                    break;

                case TileObjectType.Herb:
                    category = HarvestableCategory.Plant;
                    displayName = "Трава";
                    baseYieldPerTick = 1;
                    totalResourceCount = 1;
                    maxDurability = 10;
                    harvestDuration = 0.2f; // Мгновенный сбор
                    break;

                default:
                    category = HarvestableCategory.None;
                    displayName = "Ресурс";
                    break;
            }

            // Пересчитать currentDurability если maxDurability был изменён
            if (currentDurability > maxDurability)
                currentDurability = maxDurability;
        }
    }
}

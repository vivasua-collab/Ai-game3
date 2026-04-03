// Создано: 2026-04-03
// Источник: docs/examples/TechniqueEffectsSystem.md

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationWorld.Combat.Effects
{
    /// <summary>
    /// Фабрика для создания эффектов техник.
    /// Использует пулинг для оптимизации.
    /// </summary>
    public class TechniqueEffectFactory : MonoBehaviour
    {
        #region Types

        /// <summary>
        /// Типы эффектов.
        /// </summary>
        public enum EffectType
        {
            // Направленные
            FireSlash,
            WaterWave,
            AirBlade,
            LightningBolt,
            EarthSpike,
            VoidRift,

            // Расширяющиеся
            ExpandingMist,
            PoisonCloud,
            HealingAura,
            QiExplosion,

            // Статические
            DefenseBarrier,
            FormationArray
        }

        [System.Serializable]
        public class EffectPool
        {
            public EffectType type;
            public GameObject prefab;
            public int initialPoolSize = 5;
            public int maxPoolSize = 20;

            [HideInInspector] public Queue<GameObject> pool = new Queue<GameObject>();
            [HideInInspector] public int activeCount = 0;
        }

        #endregion

        #region Configuration

        [Header("Effect Pools")]
        [SerializeField] private List<EffectPool> effectPools = new List<EffectPool>();

        [Header("Settings")]
        [SerializeField] private bool usePooling = true;
        [SerializeField] private Transform poolContainer;

        #endregion

        #region Singleton

        public static TechniqueEffectFactory Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializePools();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Initialization

        private void InitializePools()
        {
            if (poolContainer == null)
            {
                var container = new GameObject("EffectPool_Container");
                container.transform.SetParent(transform);
                poolContainer = container.transform;
            }

            foreach (var pool in effectPools)
            {
                for (int i = 0; i < pool.initialPoolSize; i++)
                {
                    CreatePooledObject(pool);
                }
            }
        }

        private GameObject CreatePooledObject(EffectPool pool)
        {
            if (pool.prefab == null) return null;

            var obj = Instantiate(pool.prefab, poolContainer);
            obj.SetActive(false);
            obj.name = $"{pool.type}_pooled_{pool.pool.Count}";
            pool.pool.Enqueue(obj);

            return obj;
        }

        #endregion

        #region Create Effect

        /// <summary>
        /// Создаёт эффект указанного типа.
        /// </summary>
        public TechniqueEffect CreateEffect(EffectType type, Vector2 position, Vector2 direction = default)
        {
            var pool = GetPool(type);
            if (pool == null)
            {
                Debug.LogWarning($"[EffectFactory] Pool not found for type: {type}");
                return null;
            }

            GameObject obj = null;

            if (usePooling && pool.pool.Count > 0)
            {
                obj = pool.pool.Dequeue();
            }
            else if (pool.activeCount < pool.maxPoolSize)
            {
                obj = CreatePooledObject(pool);
                pool.pool.Dequeue(); // Remove from queue, will be returned later
            }
            else
            {
                Debug.LogWarning($"[EffectFactory] Max pool size reached for type: {type}");
                return null;
            }

            if (obj == null) return null;

            // Активируем и позиционируем
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.identity;
            obj.SetActive(true);
            pool.activeCount++;

            // Получаем компонент эффекта
            var effect = obj.GetComponent<TechniqueEffect>();
            if (effect != null)
            {
                effect.Play(position, direction);
            }

            return effect;
        }

        /// <summary>
        /// Создаёт направленный эффект.
        /// </summary>
        public DirectionalEffect CreateDirectionalEffect(EffectType type, Vector2 position, Vector2 direction)
        {
            var effect = CreateEffect(type, position, direction);
            return effect as DirectionalEffect;
        }

        /// <summary>
        /// Создаёт расширяющийся эффект.
        /// </summary>
        public ExpandingEffect CreateExpandingEffect(EffectType type, Vector2 position)
        {
            var effect = CreateEffect(type, position);
            return effect as ExpandingEffect;
        }

        /// <summary>
        /// Создаёт эффект формации.
        /// </summary>
        public FormationArrayEffect CreateFormationEffect(EffectType type, Vector2 position)
        {
            var effect = CreateEffect(type, position);
            return effect as FormationArrayEffect;
        }

        #endregion

        #region Return to Pool

        /// <summary>
        /// Возвращает эффект в пул.
        /// </summary>
        public void ReturnEffect(EffectType type, GameObject obj)
        {
            var pool = GetPool(type);
            if (pool == null)
            {
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(poolContainer);
            pool.pool.Enqueue(obj);
            pool.activeCount = Mathf.Max(0, pool.activeCount - 1);
        }

        /// <summary>
        /// Возвращает эффект в пул (через компонент).
        /// </summary>
        public void ReturnEffect(TechniqueEffect effect)
        {
            if (effect == null) return;

            var type = DetermineEffectType(effect);
            ReturnEffect(type, effect.gameObject);
        }

        #endregion

        #region Helpers

        private EffectPool GetPool(EffectType type)
        {
            return effectPools.Find(p => p.type == type);
        }

        /// <summary>
        /// Определяет тип эффекта по компоненту.
        /// </summary>
        private EffectType DetermineEffectType(TechniqueEffect effect)
        {
            // Простая эвристика по имени или типу
            if (effect is DirectionalEffect)
            {
                // Редактировано: 2026-04-03
                return effect.Element switch
                {
                    Element.Fire => EffectType.FireSlash,
                    Element.Water => EffectType.WaterWave,
                    Element.Air => EffectType.AirBlade,
                    Element.Lightning => EffectType.LightningBolt,
                    Element.Earth => EffectType.EarthSpike,
                    Element.Void => EffectType.VoidRift,
                    _ => EffectType.FireSlash
                };
            }

            if (effect is ExpandingEffect)
            {
                return effect.Element switch
                {
                    Element.Poison => EffectType.PoisonCloud,
                    Element.Neutral => EffectType.HealingAura,
                    _ => EffectType.ExpandingMist
                };
            }

            if (effect is FormationArrayEffect)
            {
                return EffectType.FormationArray;
            }

            return EffectType.QiExplosion;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Получает тип эффекта по элементу и типу техники.
        /// </summary>
        public static EffectType GetEffectType(Element element, string techniqueType)
        {
            // Направленные (offensive)
            if (techniqueType == "Offensive" || techniqueType == "Melee" || techniqueType == "Ranged")
            {
                return element switch
                {
                    Element.Fire => EffectType.FireSlash,
                    Element.Water => EffectType.WaterWave,
                    Element.Air => EffectType.AirBlade,
                    Element.Lightning => EffectType.LightningBolt,
                    Element.Earth => EffectType.EarthSpike,
                    Element.Void => EffectType.VoidRift,
                    Element.Poison => EffectType.PoisonCloud,
                    _ => EffectType.QiExplosion
                };
            }

            // Исцеление
            if (techniqueType == "Healing")
            {
                return EffectType.HealingAura;
            }

            // Защита
            if (techniqueType == "Defense" || techniqueType == "Defensive")
            {
                return EffectType.DefenseBarrier;
            }

            // Формация
            if (techniqueType == "Formation")
            {
                return EffectType.FormationArray;
            }

            // По умолчанию
            return EffectType.ExpandingMist;
        }

        /// <summary>
        /// Предварительно загружает пулы.
        /// </summary>
        public void PreloadPools()
        {
            foreach (var pool in effectPools)
            {
                while (pool.pool.Count < pool.initialPoolSize)
                {
                    CreatePooledObject(pool);
                }
            }
        }

        /// <summary>
        /// Очищает все пулы.
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in effectPools)
            {
                while (pool.pool.Count > 0)
                {
                    var obj = pool.pool.Dequeue();
                    if (obj != null) Destroy(obj);
                }
                pool.activeCount = 0;
            }
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Preload All Pools")]
        private void PreloadInEditor()
        {
            PreloadPools();
            Debug.Log("[EffectFactory] Pools preloaded");
        }

        [ContextMenu("Clear All Pools")]
        private void ClearInEditor()
        {
            ClearAllPools();
            Debug.Log("[EffectFactory] All pools cleared");
        }

        [ContextMenu("Log Pool Status")]
        private void LogPoolStatus()
        {
            foreach (var pool in effectPools)
            {
                Debug.Log($"[EffectFactory] {pool.type}: {pool.pool.Count} available, {pool.activeCount} active");
            }
        }
#endif

        #endregion
    }
}

// ============================================================================
// VFXPool.cs — Пул визуальных эффектов для оптимизации
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-09 10:40:00 UTC
// ============================================================================
//
// РЕШЕНИЕ ПРОБЛЕМЫ: Instantiate/Destroy — дорогие операции
// Пул переиспользует объекты вместо создания/уничтожения
//
// ИСПОЛЬЗОВАНИЕ:
// 1. VFXPool.Instance.Get(vfxPrefab, position, rotation) — получить эффект
// 2. VFXPool.Instance.Return(vfxInstance, delay) — вернуть в пул с задержкой
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.Core
{
    /// <summary>
    /// Пул визуальных эффектов.
    /// Заменяет Instantiate/Destroy на переиспользование объектов.
    /// </summary>
    public class VFXPool : MonoBehaviour
    {
        #region Singleton

        public static VFXPool Instance { get; private set; }

        #endregion

        #region Settings

        [Header("Settings")]
#pragma warning disable CS0414
        [SerializeField] private int initialPoolSize = 10;
#pragma warning restore CS0414
        [SerializeField] private int maxPoolSize = 50;
#pragma warning disable CS0414
        [SerializeField] private bool prewarmOnStart = true;
#pragma warning restore CS0414

        #endregion

        #region Pool Data

        // Пул: префаб -> список доступных объектов
        private Dictionary<GameObject, Queue<GameObject>> availablePools = new Dictionary<GameObject, Queue<GameObject>>();
        
        // Все созданные объекты: экземпляр -> префаб
        private Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();
        
        // Активные объекты (для отладки)
        private HashSet<GameObject> activeObjects = new HashSet<GameObject>();

        #endregion

        #region Properties

        public int TotalPooledObjects => instanceToPrefab.Count;
        public int ActiveObjectsCount => activeObjects.Count;

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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                ClearAllPools();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Получить VFX из пула или создать новый.
        /// </summary>
        /// <param name="prefab">Префаб эффекта</param>
        /// <param name="position">Позиция</param>
        /// <param name="rotation">Вращение</param>
        /// <returns>Экземпляр эффекта</returns>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            GameObject instance = null;

            // Пытаемся получить из пула
            if (availablePools.TryGetValue(prefab, out var queue) && queue.Count > 0)
            {
                instance = queue.Dequeue();
                
                // Активируем
                instance.transform.position = position;
                instance.transform.rotation = rotation;
                instance.SetActive(true);
            }
            else
            {
                // Создаём новый если пул не полон
                if (instanceToPrefab.Count < maxPoolSize)
                {
                    instance = Instantiate(prefab, position, rotation);
                    instanceToPrefab[instance] = prefab;
                }
                else
                {
                    // Пул полон — используем самый старый активный объект
                    Debug.LogWarning($"[VFXPool] Max pool size reached for {prefab.name}");
                    
                    // Fallback: создаём временный объект
                    instance = Instantiate(prefab, position, rotation);
                    
                    // Автоматически уничтожим через 2 секунды
                    var temp = instance.AddComponent<TemporaryVFX>();
                    temp.Initialize(null, 2f);
                    return instance;
                }
            }

            if (instance != null)
            {
                activeObjects.Add(instance);
            }

            return instance;
        }

        /// <summary>
        /// Вернуть объект в пул.
        /// </summary>
        /// <param name="instance">Экземпляр</param>
        /// <param name="delay">Задержка перед возвратом (0 = сразу)</param>
        public void Return(GameObject instance, float delay = 0f)
        {
            if (instance == null) return;

            if (delay > 0)
            {
                // Отложенный возврат
                var temp = instance.GetComponent<TemporaryVFX>();
                if (temp == null)
                {
                    temp = instance.AddComponent<TemporaryVFX>();
                    temp.Initialize(this, delay);
                }
                return;
            }

            ReturnImmediate(instance);
        }

        /// <summary>
        /// Получить и автоматически вернуть через delay секунд.
        /// </summary>
        public GameObject GetAndReturn(GameObject prefab, Vector3 position, Quaternion rotation, float returnDelay)
        {
            var instance = Get(prefab, position, rotation);
            
            if (instance != null && returnDelay > 0)
            {
                Return(instance, returnDelay);
            }
            
            return instance;
        }

        /// <summary>
        /// Очистить все пулы.
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var kvp in instanceToPrefab)
            {
                if (kvp.Key != null)
                {
                    Destroy(kvp.Key);
                }
            }
            
            availablePools.Clear();
            instanceToPrefab.Clear();
            activeObjects.Clear();
        }

        #endregion

        #region Private Methods

        private void ReturnImmediate(GameObject instance)
        {
            if (instance == null) return;

            // Удаляем временный компонент если есть
            var temp = instance.GetComponent<TemporaryVFX>();
            if (temp != null)
            {
                Destroy(temp);
            }

            // Проверяем что объект из нашего пула
            if (!instanceToPrefab.TryGetValue(instance, out var prefab))
            {
                // Не из пула — уничтожаем
                Destroy(instance);
                return;
            }

            // Деактивируем
            instance.SetActive(false);
            activeObjects.Remove(instance);

            // Добавляем обратно в пул
            if (!availablePools.ContainsKey(prefab))
            {
                availablePools[prefab] = new Queue<GameObject>();
            }
            availablePools[prefab].Enqueue(instance);
        }

        #endregion

        #region Static Convenience Methods

        /// <summary>
        /// Статический метод для удобства. Создаёт пул если не существует.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 0f)
        {
            if (Instance == null)
            {
                // Создаём пул если не существует
                var go = new GameObject("[VFXPool]");
                Instance = go.AddComponent<VFXPool>();
                DontDestroyOnLoad(go);
            }

            if (lifetime > 0)
            {
                return Instance.GetAndReturn(prefab, position, rotation, lifetime);
            }
            
            return Instance.Get(prefab, position, rotation);
        }

        /// <summary>
        /// Спавн VFX с автоматическим возвратом через 2 секунды (стандартное поведение).
        /// </summary>
        public static GameObject SpawnDefault(GameObject prefab, Vector3 position)
        {
            return Spawn(prefab, position, Quaternion.identity, 2f);
        }

        #endregion
    }

    /// <summary>
    /// Вспомогательный компонент для автоматического возврата в пул.
    /// </summary>
    public class TemporaryVFX : MonoBehaviour
    {
        private VFXPool pool;
        private float delay;
        private float timer;
        private bool initialized;

        public void Initialize(VFXPool pool, float delay)
        {
            this.pool = pool;
            this.delay = delay;
            this.timer = 0f;
            this.initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;

            timer += Time.deltaTime;
            
            if (timer >= delay)
            {
                if (pool != null)
                {
                    pool.Return(gameObject, 0f);
                }
                else
                {
                    // Fallback: уничтожаем если пул недоступен
                    Destroy(gameObject);
                }
                initialized = false;
            }
        }
    }
}

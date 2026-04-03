// ============================================================================
// ServiceLocator.cs — Сервис-локатор для замены FindFirstObjectByType
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-04-03 08:45:00 UTC
// ============================================================================
//
// ОПИСАНИЕ:
// Заменяет FindFirstObjectByType на O(1) доступ к сервисам.
// Устраняет проблему производительности при частых запросах.
//
// ИСПОЛЬЗОВАНИЕ:
// 1. Наследуйтесь от RegisteredBehaviour<T> вместо MonoBehaviour
// 2. Получайте сервисы через ServiceLocator.Get<T>()
// 3. Для асинхронной подписки используйте ServiceLocator.Request<T>()
//
// ПРИМЕР:
// public class GameManager : RegisteredBehaviour<GameManager> { ... }
// public class SomeController : MonoBehaviour {
//     private GameManager gameManager;
//     void Start() => gameManager = ServiceLocator.Get<GameManager>();
// }
//
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.Core
{
    /// <summary>
    /// Сервис-локатор — централизованный доступ к игровым сервисам.
    /// Заменяет FindFirstObjectByType на O(1) доступ.
    /// </summary>
    public static class ServiceLocator
    {
        // === Registry ===
        
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, List<Action<object>>> pendingRequests = new Dictionary<Type, List<Action<object>>>();
        
        // === Statistics ===
        
        private static int totalRegistrations = 0;
        private static int totalRequests = 0;
        private static int cacheHits = 0;
        
        // === Registration ===
        
        /// <summary>
        /// Зарегистрировать сервис.
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        /// <param name="service">Экземпляр сервиса</param>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Попытка зарегистрировать null сервис типа {typeof(T).Name}");
                return;
            }
            
            Type type = typeof(T);
            
            if (services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Сервис {type.Name} уже зарегистрирован. Замена.");
                services[type] = service;
            }
            else
            {
                services.Add(type, service);
                totalRegistrations++;
            }
            
            // Обрабатываем отложенные запросы
            ProcessPendingRequests(type, service);
            
            Debug.Log($"[ServiceLocator] Зарегистрирован: {type.Name}");
        }
        
        /// <summary>
        /// Отменить регистрацию сервиса.
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        public static void Unregister<T>() where T : class
        {
            Type type = typeof(T);
            
            if (services.Remove(type))
            {
                Debug.Log($"[ServiceLocator] Отменена регистрация: {type.Name}");
            }
        }
        
        /// <summary>
        /// Отменить регистрацию сервиса (по экземпляру).
        /// </summary>
        public static void Unregister<T>(T service) where T : class
        {
            Type type = typeof(T);
            
            if (services.TryGetValue(type, out object registered) && registered == service)
            {
                services.Remove(type);
                Debug.Log($"[ServiceLocator] Отменена регистрация: {type.Name}");
            }
        }
        
        // === Retrieval ===
        
        /// <summary>
        /// Получить сервис. Возвращает null если не найден.
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        /// <returns>Экземпляр сервиса или null</returns>
        public static T Get<T>() where T : class
        {
            totalRequests++;
            
            if (services.TryGetValue(typeof(T), out object service))
            {
                cacheHits++;
                return service as T;
            }
            
            Debug.LogWarning($"[ServiceLocator] Сервис {typeof(T).Name} не найден");
            return null;
        }
        
        /// <summary>
        /// Получить сервис. Бросает исключение если не найден.
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        /// <returns>Экземпляр сервиса</returns>
        /// <exception cref="InvalidOperationException">Если сервис не найден</exception>
        public static T GetRequired<T>() where T : class
        {
            T service = Get<T>();
            
            if (service == null)
            {
                throw new InvalidOperationException($"[ServiceLocator] Обязательный сервис {typeof(T).Name} не найден");
            }
            
            return service;
        }
        
        /// <summary>
        /// Проверить наличие сервиса.
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        /// <returns>True если сервис зарегистрирован</returns>
        public static bool Has<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Попробовать получить сервис.
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        /// <param name="service">Найденный сервис или null</param>
        /// <returns>True если сервис найден</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            service = Get<T>();
            return service != null;
        }
        
        // === Async Requests ===
        
        /// <summary>
        /// Запросить сервис асинхронно.
        /// Если сервис уже зарегистрирован — callback вызывается немедленно.
        /// Если нет — callback будет вызван при регистрации.
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        /// <param name="callback">Callback для получения сервиса</param>
        public static void Request<T>(Action<T> callback) where T : class
        {
            if (callback == null) return;
            
            Type type = typeof(T);
            
            // Если сервис уже есть — вызываем немедленно
            if (services.TryGetValue(type, out object service))
            {
                callback(service as T);
                return;
            }
            
            // Добавляем в очередь ожидания
            if (!pendingRequests.ContainsKey(type))
            {
                pendingRequests[type] = new List<Action<object>>();
            }
            
            pendingRequests[type].Add(obj => callback(obj as T));
        }
        
        /// <summary>
        /// Отменить отложенный запрос.
        /// </summary>
        public static void CancelRequest<T>(Action<T> callback) where T : class
        {
            Type type = typeof(T);
            
            if (pendingRequests.TryGetValue(type, out List<Action<object>> requests))
            {
                // Удаляем все matching callbacks
                requests.RemoveAll(r => r.Target == callback.Target && r.Method == callback.Method);
            }
        }
        
        // === Internal ===
        
        private static void ProcessPendingRequests(Type type, object service)
        {
            if (!pendingRequests.TryGetValue(type, out List<Action<object>> requests))
                return;
            
            // Вызываем все отложенные callbacks
            foreach (var request in requests)
            {
                try
                {
                    request.Invoke(service);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServiceLocator] Ошибка в callback для {type.Name}: {ex.Message}");
                }
            }
            
            pendingRequests.Remove(type);
        }
        
        // === Statistics ===
        
        /// <summary>
        /// Получить статистику использования.
        /// </summary>
        public static string GetStatistics()
        {
            float hitRate = totalRequests > 0 ? (float)cacheHits / totalRequests * 100f : 0f;
            
            return $"[ServiceLocator] Регистраций: {totalRegistrations} | " +
                   $"Запросов: {totalRequests} | " +
                   $"Cache hits: {cacheHits} ({hitRate:F1}%) | " +
                   $"Сервисов: {services.Count}";
        }
        
        /// <summary>
        /// Очистить все регистрации (для тестов).
        /// </summary>
        public static void Clear()
        {
            services.Clear();
            pendingRequests.Clear();
            
            Debug.Log("[ServiceLocator] Все регистрации очищены");
        }
        
        /// <summary>
        /// Получить список всех зарегистрированных типов.
        /// </summary>
        public static List<Type> GetRegisteredTypes()
        {
            return new List<Type>(services.Keys);
        }
    }
    
    /// <summary>
    /// Базовый класс для MonoBehaviour с автоматической регистрацией в ServiceLocator.
    /// Наследуйтесь от этого класса вместо MonoBehaviour для сервисов.
    /// </summary>
    /// <typeparam name="T">Тип наследника</typeparam>
    public abstract class RegisteredBehaviour<T> : MonoBehaviour where T : RegisteredBehaviour<T>
    {
        /// <summary>
        /// Экземпляр сервиса (для singleton паттерна).
        /// </summary>
        public static T Instance { get; private set; }
        
        /// <summary>
        /// Проверить, зарегистрирован ли сервис.
        /// </summary>
        public static bool IsRegistered => Instance != null;
        
        protected virtual void Awake()
        {
            // Singleton проверка
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[RegisteredBehaviour] Дубликат {typeof(T).Name} уничтожен");
                Destroy(gameObject);
                return;
            }
            
            Instance = (T)this;
            ServiceLocator.Register<T>((T)this);
            
            OnRegistered();
        }
        
        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                ServiceLocator.Unregister<T>();
                
                OnUnregistered();
            }
        }
        
        /// <summary>
        /// Вызывается после регистрации в ServiceLocator.
        /// </summary>
        protected virtual void OnRegistered() { }
        
        /// <summary>
        /// Вызывается после отмены регистрации.
        /// </summary>
        protected virtual void OnUnregistered() { }
    }
    
    /// <summary>
    /// Атрибут для отметки обязательных зависимостей.
    /// Используется для документирования и валидации.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ServiceDependencyAttribute : Attribute
    {
        public bool IsRequired { get; }
        public string Description { get; }
        
        public ServiceDependencyAttribute(bool isRequired = true, string description = "")
        {
            IsRequired = isRequired;
            Description = description;
        }
    }
    
    /// <summary>
    /// Утилита для валидации зависимостей.
    /// </summary>
    public static class DependencyValidator
    {
        /// <summary>
        /// Проверить все обязательные сервисы.
        /// </summary>
        /// <returns>True если все обязательные сервисы зарегистрированы</returns>
        public static bool ValidateRequiredServices()
        {
            Type[] requiredServices = new Type[]
            {
                typeof(GameManager),
                // Добавьте другие обязательные сервисы
            };
            
            bool allValid = true;
            
            foreach (Type serviceType in requiredServices)
            {
                if (!ServiceLocator.Has(serviceType))
                {
                    Debug.LogError($"[DependencyValidator] Обязательный сервис {serviceType.Name} не зарегистрирован!");
                    allValid = false;
                }
            }
            
            return allValid;
        }
        
        /// <summary>
        /// Получить отчёт о статусе сервисов.
        /// </summary>
        public static string GetServiceStatusReport()
        {
            var registeredTypes = ServiceLocator.GetRegisteredTypes();
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Service Locator Status ===");
            sb.AppendLine($"Total services: {registeredTypes.Count}");
            sb.AppendLine();
            sb.AppendLine("Registered services:");
            
            foreach (var type in registeredTypes)
            {
                sb.AppendLine($"  ✓ {type.Name}");
            }
            
            sb.AppendLine();
            sb.AppendLine(ServiceLocator.GetStatistics());
            
            return sb.ToString();
        }
    }
}

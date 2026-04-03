// ============================================================================
// ServiceLocator.cs — Простой сервис-локатор для замены FindFirstObjectByType
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-04-02 14:31:54 UTC
// ============================================================================
// 
// РЕКОМЕНДАЦИЯ: Использовать вместо FindFirstObjectByType для:
// - Улучшения производительности (FindFirstObjectByType — дорогая операция)
// - Централизованного управления зависимостями
// - Возможности mock-тестирования
//
// ИСПОЛЬЗОВАНИЕ:
// 1. При инициализации: ServiceLocator.Register(worldController);
// 2. При необходимости: var world = ServiceLocator.Get<WorldController>();
// 3. При уничтожении: ServiceLocator.Unregister<WorldController>();
//
// АЛЬТЕРНАТИВА: Назначение ссылок в Inspector (предпочтительно для MonoBehaviour)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.Core
{
    /// <summary>
    /// Простой сервис-локатор для управления глобальными сервисами.
    /// Заменяет FindFirstObjectByType для часто используемых менеджеров.
    /// 
    /// Преимущества:
    /// - O(1) доступ вместо O(n) поиск
    /// - Централизованное управление
    /// - Возможность подмены для тестов
    /// 
    /// Ограничения:
    /// - Глобальное состояние (как и Singleton)
    /// - Требует явной регистрации
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, List<Action<object>>> pendingRequests = new Dictionary<Type, List<Action<object>>>();

        /// <summary>
        /// Включить логирование для отладки.
        /// </summary>
        public static bool DebugLogging { get; set; } = false;

        /// <summary>
        /// Зарегистрировать сервис.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Cannot register null service of type {typeof(T).Name}");
                return;
            }

            var type = typeof(T);
            
            if (services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} already registered. Replacing.");
                services[type] = service;
            }
            else
            {
                services.Add(type, service);
            }

            if (DebugLogging)
                Debug.Log($"[ServiceLocator] Registered: {type.Name}");

            // Обрабатаем отложенные запросы
            if (pendingRequests.TryGetValue(type, out var callbacks))
            {
                foreach (var callback in callbacks)
                {
                    callback?.Invoke(service);
                }
                pendingRequests.Remove(type);
            }
        }

        /// <summary>
        /// Отменить регистрацию сервиса.
        /// </summary>
        public static bool Unregister<T>() where T : class
        {
            var type = typeof(T);
            
            if (services.Remove(type))
            {
                if (DebugLogging)
                    Debug.Log($"[ServiceLocator] Unregistered: {type.Name}");
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Получить сервис. Возвращает null если не зарегистрирован.
        /// </summary>
        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            
            if (services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            if (DebugLogging)
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} not found");

            return null;
        }

        /// <summary>
        /// Получить сервис или найти через FindFirstObjectByType и зарегистрировать.
        /// Использовать только как fallback!
        /// </summary>
        public static T GetOrFind<T>() where T : UnityEngine.Object
        {
            var service = Get<T>();
            
            if (service == null)
            {
                service = UnityEngine.Object.FindFirstObjectByType<T>();
                
                if (service != null)
                {
                    Register(service);
                    
                    if (DebugLogging)
                        Debug.Log($"[ServiceLocator] Auto-found and registered: {typeof(T).Name}");
                }
            }
            
            return service;
        }

        /// <summary>
        /// Попытаться получить сервис.
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            service = Get<T>();
            return service != null;
        }

        /// <summary>
        /// Запросить сервис асинхронно. Callback будет вызван когда сервис зарегистрирован.
        /// </summary>
        public static void Request<T>(Action<T> callback) where T : class
        {
            if (callback == null) return;

            var type = typeof(T);
            
            // Если сервис уже есть - вызываем сразу
            if (services.TryGetValue(type, out var service))
            {
                callback(service as T);
                return;
            }

            // Иначе добавляем в очередь ожидания
            if (!pendingRequests.ContainsKey(type))
            {
                pendingRequests[type] = new List<Action<object>>();
            }
            
            pendingRequests[type].Add(obj => callback(obj as T));
        }

        /// <summary>
        /// Проверить, зарегистрирован ли сервис.
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Очистить все регистрации (при смене сцены или выходе).
        /// </summary>
        public static void Clear()
        {
            services.Clear();
            pendingRequests.Clear();
            
            if (DebugLogging)
                Debug.Log("[ServiceLocator] All services cleared");
        }

        /// <summary>
        /// Получить количество зарегистрированных сервисов.
        /// </summary>
        public static int ServiceCount => services.Count;

        /// <summary>
        /// Получить список всех зарегистрированных типов (для отладки).
        /// </summary>
        public static string GetRegisteredServices()
        {
            var types = new System.Text.StringBuilder();
            types.AppendLine("Registered Services:");
            
            foreach (var kvp in services)
            {
                types.AppendLine($"  - {kvp.Key.Name}: {(kvp.Value != null ? "OK" : "NULL")}");
            }
            
            return types.ToString();
        }
    }

    /// <summary>
    /// Базовый класс для MonoBehaviour, которые автоматически регистрируются
    /// в ServiceLocator при Awake и отменяют регистрацию при OnDestroy.
    /// </summary>
    public abstract class RegisteredBehaviour<T> : MonoBehaviour where T : RegisteredBehaviour<T>
    {
        protected virtual void Awake()
        {
            ServiceLocator.Register<T>(this as T);
        }

        protected virtual void OnDestroy()
        {
            ServiceLocator.Unregister<T>();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace _LumenLib.ServiceLocator
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Services.Clear();
        }

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            Services[typeof(T)] = service;
            Debug.Log($"[ServiceLocator] Registered {typeof(T).Name}: {service.GetType().Name}");
        }

        public static bool Unregister<T>() where T : class
        {
            bool removed = Services.Remove(typeof(T));

            if (removed)
                Debug.Log($"[ServiceLocator] Unregistered {typeof(T).Name}");

            return removed;
        }

        public static bool Unregister<T>(T expectedService) where T : class
        {
            if (!Services.TryGetValue(typeof(T), out object registered) ||
                !ReferenceEquals(registered, expectedService))
            {
                return false;
            }

            Services.Remove(typeof(T));
            Debug.Log($"[ServiceLocator] Unregistered {typeof(T).Name}");
            return true;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object registered) && registered is T typed)
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public static T Get<T>() where T : class
        {
            if (TryGet(out T service))
                return service;

            Debug.LogWarning($"[ServiceLocator] {typeof(T).Name} is not registered.");
            return null;
        }
    }
}

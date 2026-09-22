using Cachacos;
using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Instance-safe wrapper around <see cref="ServiceLocator"/> registration.
    /// <para>
    /// <c>ServiceLocator.TryDeregisterService</c> removes whatever is registered for the
    /// TYPE, not the given instance, and <c>RegisterService</c> silently overwrites. So a
    /// second input service in the scene would replace the first, and destroying that
    /// duplicate would then deregister the survivor. These helpers make both operations
    /// instance-aware without modifying the legacy ServiceLocator.
    /// </para>
    /// </summary>
    internal static class TutorialServiceRegistration
    {
        /// <summary>
        /// Registers <paramref name="service"/> unless a different, still-alive instance
        /// already owns the slot. A destroyed Unity object left behind by a previous scene
        /// counts as stale and is replaced.
        /// </summary>
        public static bool TryRegister<T>(T service, Object context) where T : class
        {
            if (service == null)
                return false;

            T existing = ServiceLocator.Instance.RequestService<T>();
            if (existing != null && !ReferenceEquals(existing, service) && !IsDestroyed(existing))
            {
                Debug.LogError("[Tutorial] A " + typeof(T).Name + " is already registered by '" +
                               Describe(existing) + "'. '" + Describe(service) +
                               "' was NOT registered and will stay inactive. Keep one per scene.", context);
                return false;
            }

            ServiceLocator.Instance.RegisterService<T>(service);
            return true;
        }

        /// <summary>Deregisters only if <paramref name="service"/> is the registered instance.</summary>
        public static void Deregister<T>(T service) where T : class
        {
            if (service == null)
                return;

            T existing = ServiceLocator.Instance.RequestService<T>();
            if (ReferenceEquals(existing, service))
                ServiceLocator.Instance.TryDeregisterService<T>(service);
        }

        /// <summary>
        /// True for a Unity object whose native side was destroyed: the C# reference still
        /// exists (so the locator keeps it) but Unity's overloaded == reports it as null.
        /// </summary>
        private static bool IsDestroyed(object candidate)
        {
            Object unityObject = candidate as Object;
            return !ReferenceEquals(unityObject, null) && unityObject == null;
        }

        private static string Describe(object service)
        {
            Object unityObject = service as Object;
            return unityObject != null ? unityObject.name : service.GetType().Name;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Cachacos
{
    /// <summary>
    /// Provides a singleton-based service locator for managing dependencies and services throughout the application.
    /// This enables loose coupling by allowing components to request and register services without direct references.
    /// </summary>
    public class ServiceLocator
    {
        private static ServiceLocator _instance;
        /// <summary>
        /// Gets the singleton instance of the <see cref="ServiceLocator"/>.
        /// If the instance does not exist, it is created lazily.
        /// </summary>
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ServiceLocator();
                return _instance;
            }
        }
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Action<object>> _pendingRequests = new Dictionary<Type, Action<object>>();
        /// <summary>
        /// Registers a service with the <see cref="ServiceLocator"/>.
        /// Services should be registered only once and are globally accessible.
        /// </summary>
        /// <typeparam name="T">The type of the service to register.</typeparam>
        /// <param name="service">The instance of the service to register.</param>
        /// <exception cref="InvalidOperationException">Thrown if a service of the same type is already registered.</exception>
        /// <remarks>
        /// If there are pending requests for this service type, they will be resolved upon registration.
        /// </remarks>
        public void RegisterService<T>(T service) where T : class
        {
            Type type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Assert.IsTrue(_services[type] != null, $"///// SERVICE LOCATOR ///// \nService {type} already registered");
                _services[type] = service;
                UnityEngine.Debug.Log($"///// SERVICE LOCATOR ///// \nOverwriting null service {type} with new service.");
            }
            else
            {
                _services.Add(type, service);
                UnityEngine.Debug.Log($"///// SERVICE LOCATOR ///// \nService {service} registered");
            }
            LookPedingRequests(service);
        }
        public void TryDeregisterService<T>(T service) where T : class
        {
            Type type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Assert.IsTrue(_services[type] != null, $"///// SERVICE LOCATOR ///// \nService {type} desregistered");
                _services.Remove(type);
            }
            UnityEngine.Debug.Log($"///// SERVICE LOCATOR ///// \nService {service} is not register");
        }
        private void LookPedingRequests<T>(T service) where T : class
        {
            if (_pendingRequests.TryGetValue(typeof(T), out var pendingActions))
            {
                pendingActions.Invoke(service);
                _pendingRequests.Remove(typeof(T));
            }
        }
        /// <summary>
        /// Requests a service of type <typeparamref name="T"/> from the <see cref="ServiceLocator"/>.
        /// If the service is already registered, it is returned immediately.
        /// If the service is not yet registered and a callback is provided, the callback will be stored and invoked
        /// once the service becomes available. If no callback is provided, this method returns <c>null</c>.
        /// </summary>
        /// <typeparam name="T">The type of the service to request.</typeparam>
        /// <param name="onServiceAvailable">
        /// Optional callback to invoke when the service becomes available. If provided, it will be called with the service instance once it is registered.
        /// </param>
        /// <returns>
        /// The instance of the service if it is already registered; otherwise, <c>null</c>.
        /// </returns>
        public T RequestService<T>(Action<T> onServiceAvailable = null) where T : class
        {
            Type type = typeof(T);
            if (_services.TryGetValue(type, out object service) && service != null)
            {
                UnityEngine.Debug.Log($"///// SERVICE LOCATOR ///// \nService {service} getted");
                onServiceAvailable?.Invoke((T)service);
                return (T)service;
            }
            if (onServiceAvailable == null)
                return null;
            UnityEngine.Debug.Log($"///// SERVICE LOCATOR ///// \nService of type {type} is not registered. Queuing callback for resolution upon registration.");
            if (_pendingRequests.ContainsKey(type))
            {
                _pendingRequests[type] += (obj => onServiceAvailable((T)obj));
                return null;
            }
            _pendingRequests.Add(type, obj => onServiceAvailable((T)obj));
            return null;
        }
    }
}

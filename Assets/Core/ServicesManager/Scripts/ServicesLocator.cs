using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.ServicesManager
{
	public class ServicesLocator : MonoBehaviour
	{
		public static ServicesLocator Instance { get; private set; }

		private Dictionary<Type, IService> _services;
		private List<IService> _orderedServices;
		private bool _isInitialized;

		private event Action OnAllServicesInitializedInternal;
		public event Action OnAllServicesInitialized
		{
			add
			{
				if(_isInitialized) value?.Invoke();
				else OnAllServicesInitializedInternal += value;
			}
			remove => OnAllServicesInitializedInternal -= value;
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);

			DiscoverAndInitializeServices().Forget();
		}

		private async UniTask DiscoverAndInitializeServices()
		{
			_isInitialized = false;
			List<Type> serviceTypes = DiscoverServices();

			_services = new Dictionary<Type, IService>();
			foreach (Type serviceType in serviceTypes)
			{
				IService serviceInstance = (IService)Activator.CreateInstance(serviceType);
				_services[serviceType] = serviceInstance;
			}

			_orderedServices = OrderServicesByDependencies();

			foreach (IService orderedService in _orderedServices)
			{
				if (await orderedService.Initialize()) continue;

				Debug.LogError($"Service {orderedService.GetType().Name} could not be initialized. Fix the error and restart the game.");
				return;
			}

			_isInitialized = true;
			OnAllServicesInitializedInternal?.Invoke();
		}

		private List<Type> DiscoverServices()
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			List<Type> serviceTypes = new List<Type>();

			foreach (Assembly assembly in assemblies)
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (typeof(IService).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
					{
						serviceTypes.Add(type);
					}
				}
			}

			return serviceTypes;
		}

		private List<IService> OrderServicesByDependencies()
		{
			List<IService> orderedList = new List<IService>();
			HashSet<Type> visited = new HashSet<Type>();
			HashSet<Type> visiting = new HashSet<Type>();

			foreach (KeyValuePair<Type, IService> kvp in _services)
			{
				if (!visited.Contains(kvp.Key))
				{
					VisitService(kvp.Value, orderedList, visited, visiting);
				}
			}

			return orderedList;
		}

		private void VisitService(IService service, List<IService> orderedList, HashSet<Type> visited, HashSet<Type> visiting)
		{
			Type serviceType = service.GetType();

			if (visiting.Contains(serviceType))
			{
				Debug.LogError($"Circular dependency detected for service: {serviceType.Name}");
				return;
			}

			if (visited.Contains(serviceType))
			{
				return;
			}

			visiting.Add(serviceType);

			Type[] dependencies = service.GetDependencies();
			if (dependencies != null)
			{
				foreach (Type dependencyType in dependencies)
				{
					if (_services.TryGetValue(dependencyType, out IService dependencyService))
					{
						if (!visited.Contains(dependencyType))
						{
							VisitService(dependencyService, orderedList, visited, visiting);
						}
					}
					else
					{
						Debug.LogWarning($"Service {serviceType.Name} depends on {dependencyType.Name}, but it was not found.");
					}
				}
			}

			visiting.Remove(serviceType);
			visited.Add(serviceType);
			orderedList.Add(service);
		}

		public T GetService<T>() where T : class, IService
		{
			Type serviceType = typeof(T);
			if (_services.TryGetValue(serviceType, out IService service))
			{
				return service as T;
			}

			Debug.LogError($"Service of type {serviceType.Name} not found.");
			return null;
		}

		private void OnDestroy() => ResetServices().Forget();
		private async UniTaskVoid ResetServices()
		{
			for (int i = _orderedServices.Count - 1; i >= 0; i--)
			{
				try
				{
					await _orderedServices[i].Reset();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}
	}
}
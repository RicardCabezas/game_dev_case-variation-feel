using System;
using Cysharp.Threading.Tasks;
using Core.ServicesManager;
using Object = UnityEngine.Object;

namespace Game.World
{
	/// <summary>Owns lifetime of world prefab instantiated for current application session.</summary>
	public class WorldService : IService
	{
		/// <inheritdoc/>
		public Type[] GetDependencies() => null;

		/// <summary>Gets persistent instantiated world, or <see langword="null"/> before initialization and after reset.</summary>
		public WorldView World { get; private set; }

		/// <inheritdoc/>
		public UniTask<bool> Initialize()
		{
			World = Object.Instantiate(WorldConfig.Instance.WorldPrefab);
			Object.DontDestroyOnLoad(World);
			return UniTask.FromResult(true);
		}

		/// <inheritdoc/>
		public UniTask Reset()
		{ 
			if (World != null) Object.Destroy(World);
			return default;
		}
	}
}

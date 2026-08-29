using System;
using Cysharp.Threading.Tasks;
using Core.ServicesManager;
using Object = UnityEngine.Object;

namespace Game.World
{
	public class WorldService : IService
	{
		public Type[] GetDependencies() => null;

		public WorldView World { get; private set; }

		public UniTask<bool> Initialize()
		{
			World = Object.Instantiate(WorldConfig.Instance.WorldPrefab);
			Object.DontDestroyOnLoad(World);
			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{ 
			if (World != null) Object.Destroy(World);
			return default;
		}
	}
}
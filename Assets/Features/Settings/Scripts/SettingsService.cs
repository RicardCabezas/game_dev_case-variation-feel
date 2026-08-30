using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Settings
{
	public class SettingsService : IService
	{
		public const int TargetFrameRate = 60;

		public Type[] GetDependencies() => Array.Empty<Type>();

		public UniTask<bool> Initialize()
		{
			Application.targetFrameRate = TargetFrameRate;
			return UniTask.FromResult(true);
		}

		public UniTask Reset() => UniTask.CompletedTask;
	}
}

using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Settings
{
	/// <summary>Applies global runtime settings during service initialization.</summary>
	public class SettingsService : IService
	{
		/// <summary>Target application frame rate configured by this service.</summary>
		public const int TargetFrameRate = 60;

		/// <inheritdoc/>
		public Type[] GetDependencies() => Array.Empty<Type>();

		/// <inheritdoc/>
		public UniTask<bool> Initialize()
		{
			Application.targetFrameRate = TargetFrameRate;
			return UniTask.FromResult(true);
		}

		/// <inheritdoc/>
		public UniTask Reset() => UniTask.CompletedTask;
	}
}

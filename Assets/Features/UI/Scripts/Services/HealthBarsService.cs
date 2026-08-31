using System;
using System.Threading;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Entities;
using Game.GamePlay.Enemies;
using UnityEngine;

namespace Game.UI
{
	/// <summary>Composes and updates the canvas health-bar controller.</summary>
	public sealed class HealthBarsService : IService
	{
		private CancellationTokenSource _cancellationTokenSource;

		public HealthBarsCanvasController Controller { get; private set; }

		public Type[] GetDependencies() => new[] { typeof(EntitiesService) };

		public UniTask<bool> Initialize()
		{
			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			Controller = new HealthBarsCanvasController(
				entitiesService.HeroController,
				entitiesService.EnemiesController,
				EnemiesConfig.Instance.MaxEnemies);
			Controller.Initialize(Time.time);
			_cancellationTokenSource = new CancellationTokenSource();
			UpdateLoop(_cancellationTokenSource.Token).Forget();
			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = null;
			Controller?.Reset();
			Controller = null;
			return UniTask.CompletedTask;
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				Controller?.Tick(Time.time);
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}
	}
}

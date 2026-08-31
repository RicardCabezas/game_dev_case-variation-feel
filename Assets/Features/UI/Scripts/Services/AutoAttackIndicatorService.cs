using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Entities;
using Game.JoystickInput;

namespace Game.UI
{
	/// <summary>Composes and owns UI controller for automatic-attack cooldown presentation.</summary>
	public class AutoAttackIndicatorService : IService
	{
		/// <summary>Gets initialized UI controller, or <see langword="null"/> before initialization.</summary>
		public AutoAttackIndicatorController Controller { get; private set; }

		/// <inheritdoc/>
		public Type[] GetDependencies() => new[] { typeof(EntitiesService), typeof(JoystickInputService) };

		/// <inheritdoc/>
		public UniTask<bool> Initialize()
		{
			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			JoystickInputService joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();

			Controller = new AutoAttackIndicatorController();
			Controller.Initialize(entitiesService.HeroController, joystickInputService);

			return UniTask.FromResult(true);
		}

		/// <inheritdoc/>
		public UniTask Reset()
		{
			Controller?.Reset();
			return UniTask.CompletedTask;
		}
	}
}

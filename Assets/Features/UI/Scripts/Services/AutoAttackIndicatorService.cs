using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Entities;
using Game.JoystickInput;

namespace Game.UI
{
	public class AutoAttackIndicatorService : IService
	{
		public AutoAttackIndicatorController Controller { get; private set; }

		public Type[] GetDependencies() => new[] { typeof(EntitiesService), typeof(JoystickInputService) };

		public UniTask<bool> Initialize()
		{
			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			JoystickInputService joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();

			Controller = new AutoAttackIndicatorController();
			Controller.Initialize(entitiesService.HeroController, joystickInputService);

			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			Controller?.Reset();
			return UniTask.CompletedTask;
		}
	}
}

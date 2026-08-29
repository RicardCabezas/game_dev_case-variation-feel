using System;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Enemies;
using Game.GamePlay.Heroes;
using Game.JoystickInput;
using Game.Weapons;
using Core.ServicesManager;

namespace Game.GamePlay.Entities
{
	public class EntitiesService : IService
	{
		public Type[] GetDependencies() => new[] { typeof(JoystickInputService), typeof(WeaponsService) };

		public EnemiesController EnemiesController { get; private set; }
		public HeroController HeroController { get; private set; }

		public async UniTask<bool> Initialize()
		{
			JoystickInputService joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			WeaponsService weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();

			EnemiesController = new EnemiesController();
			HeroController = new HeroController();

			await HeroController.Initialize(EnemiesController, joystickInputService, weaponsService);
			await EnemiesController.Initialize(HeroController);

			return true;
		}

		public UniTask Reset() => default;
	}
}
using System;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Enemies;
using Game.GamePlay.Heroes;
using Game.JoystickInput;
using Game.Weapons;
using Core.ServicesManager;

namespace Game.GamePlay.Entities
{
	/// <summary>Service composition root for hero and enemy controllers.</summary>
	/// <remarks>Creates controllers after input and weapon services. Current <see cref="Reset"/> does not forward reset calls to controllers.</remarks>
	public class EntitiesService : IService
	{
		/// <inheritdoc/>
		public Type[] GetDependencies() => new[] { typeof(JoystickInputService), typeof(WeaponsService) };

		/// <summary>Gets enemy controller after successful initialization.</summary>
		public EnemiesController EnemiesController { get; private set; }
		/// <summary>Gets hero controller after successful initialization.</summary>
		public HeroController HeroController { get; private set; }

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		/// <remarks>Currently completes without resetting contained controllers.</remarks>
		public UniTask Reset() => default;
	}
}

using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;

namespace Game.Weapons
{
	/// <summary>Owns currently equipped weapon and reports successful weapon changes.</summary>
	public class WeaponsService : IService
	{
		/// <inheritdoc/>
		public Type[] GetDependencies() => null;

		/// <summary>Gets equipped weapon, or <see langword="null"/> when catalog is empty or service reset.</summary>
		public WeaponConfig CurrentWeapon { get; private set; }

		/// <summary>Raised after <see cref="CurrentWeapon"/> changes; payload is new configuration.</summary>
		public event Action<WeaponConfig> OnWeaponChanged;

		/// <inheritdoc/>
		public UniTask<bool> Initialize()
		{
			if (WeaponsConfig.Instance.Weapons.Count > 0)
			{
				CurrentWeapon = WeaponsConfig.Instance.Weapons[0];
			}

			return UniTask.FromResult(true);
		}

		/// <inheritdoc/>
		public UniTask Reset()
		{
			CurrentWeapon = null;
			return UniTask.CompletedTask;
		}

		/// <summary>Equips configured weapon selected by identifier.</summary>
		/// <param name="weaponId">Identifier from <see cref="WeaponConfig.Id"/>.</param>
		/// <returns><see langword="true"/> when matching weapon was equipped; otherwise <see langword="false"/> without an event.</returns>
		public bool SwitchWeapon(string weaponId)
		{
			WeaponConfig newWeapon = WeaponsConfig.Instance.GetWeaponById(weaponId);

			if (newWeapon == null) return false;

			CurrentWeapon = newWeapon;
			OnWeaponChanged?.Invoke(CurrentWeapon);

			return true;
		}
	}
}

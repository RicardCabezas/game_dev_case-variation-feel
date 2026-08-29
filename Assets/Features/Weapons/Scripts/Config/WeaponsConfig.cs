using System.Collections.Generic;
using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.Weapons
{
	[CreateAssetMenu(fileName = "WeaponsConfig", menuName = "Game/WeaponsConfig")]
	public class WeaponsConfig : ScriptableObjectSingleton<WeaponsConfig>
	{
		[SerializeField]
		[Tooltip("List of all available weapons in the game")]
		private List<WeaponConfig> weapons;

		public IReadOnlyList<WeaponConfig> Weapons => weapons;

		private Dictionary<string, WeaponConfig> _weaponCache;

		public WeaponConfig GetWeaponById(string weaponId)
		{
			if (_weaponCache == null)
			{
				_weaponCache = new Dictionary<string, WeaponConfig>();
				foreach (WeaponConfig weapon in weapons)
				{
					_weaponCache[weapon.Id] = weapon;
				}
			}

			_weaponCache.TryGetValue(weaponId, out WeaponConfig weaponConfig);
			return weaponConfig;
		}
	}
}
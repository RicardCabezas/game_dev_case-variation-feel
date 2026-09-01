using System.Collections.Generic;
using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.Weapons
{
    [CreateAssetMenu(fileName = "WeaponsConfig", menuName = "Game/WeaponsConfig")]
    /// <summary>Resources-backed weapon catalog; startup currently equips index zero.</summary>
    public class WeaponsConfig : ScriptableObjectSingleton<WeaponsConfig>
    {
        [SerializeField]
        [Tooltip("List of all available weapons in the game")]
        private List<WeaponConfig> weapons;

        /// <summary>Gets configured weapons in authoring order.</summary>
        public IReadOnlyList<WeaponConfig> Weapons => weapons;

        private Dictionary<string, WeaponConfig> _weaponCache;

        /// <summary>Finds configured weapon by content identifier.</summary>
        /// <param name="weaponId">Identifier from <see cref="WeaponConfig.Id"/>.</param>
        /// <returns>Matching configuration, or <see langword="null"/> when no match exists.</returns>
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

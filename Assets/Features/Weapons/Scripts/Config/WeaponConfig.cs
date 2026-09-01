using UnityEngine;

namespace Game.Weapons
{
    [CreateAssetMenu(fileName = "WeaponX", menuName = "Content/Weapon")]
    /// <summary>Authoring data for one equippable weapon and its presentation prefab.</summary>
    public class WeaponConfig : ScriptableObject
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private int damage;

        [SerializeField]
        private float range;

        [SerializeField]
        private float cooldown;

        [SerializeField]
        private WeaponView prefab;

        /// <summary>Gets stable content identifier used for weapon selection.</summary>
        public string Id => id;

        /// <summary>Gets integer damage supplied to enemy attacks.</summary>
        public int Damage => damage;

        /// <summary>Gets maximum hero-to-enemy targeting distance in world units.</summary>
        public float Range => range;

        /// <summary>Gets seconds between eligible automatic attacks.</summary>
        public float Cooldown => cooldown;

        /// <summary>Gets visual prefab owned by <see cref="Game.GamePlay.Heroes.HeroView"/>.</summary>
        public WeaponView Prefab => prefab;
    }
}

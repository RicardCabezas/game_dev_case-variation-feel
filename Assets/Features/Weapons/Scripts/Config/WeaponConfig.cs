using UnityEngine;

namespace Game.Weapons
{
    [CreateAssetMenu(fileName = "WeaponX", menuName = "Content/Weapon")]
    /// <summary>Authoring data for one equippable weapon and its presentation prefab.</summary>
    public class WeaponConfig : ScriptableObject
    {
        [SerializeField]
        private string id;
        [SerializeField] private string displayName;

        [SerializeField]
        private int damage;

        [SerializeField]
        private float range;

        [SerializeField]
        private float cooldown;
        [SerializeField, Min(1)] private int maxUses = 1;
        [SerializeField, Min(0f)] private float spawnChance = 1f;

        [SerializeField]
        private WeaponView prefab;

        [SerializeField] private Color pickupColor = Color.white;

        /// <summary>Gets stable content identifier used for weapon selection.</summary>
        public string Id => id;
        public string DisplayName => displayName;

        /// <summary>Gets integer damage supplied to enemy attacks.</summary>
        public int Damage => damage;

        /// <summary>Gets maximum hero-to-enemy targeting distance in world units.</summary>
        public float Range => range;

        /// <summary>Gets seconds between eligible automatic attacks.</summary>
        public float Cooldown => cooldown;
        public int MaxUses => maxUses;
        public float SpawnChance => spawnChance;

        /// <summary>Gets visual prefab owned by <see cref="Game.GamePlay.Heroes.HeroView"/>.</summary>
        public WeaponView Prefab => prefab;

        /// <summary>Gets pickup Quad tint owned by <see cref="WeaponPickupView"/> presentation.</summary>
        public Color PickupColor => pickupColor;
    }
}

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
        [SerializeField, Min(0f)] private float spawnInterval = 10f;
        [SerializeField, Min(0f)] private float minSpawnRadius = 3f;
        [SerializeField, Min(0f)] private float maxSpawnRadius = 15f;
        [SerializeField, Min(0)] private int maxSpawnedWeapons = 5;
        [SerializeField] private WeaponPickupView pickupPrefab;

        /// <summary>Gets configured weapons in authoring order.</summary>
        public IReadOnlyList<WeaponConfig> Weapons => weapons;
        public float SpawnInterval => spawnInterval;
        public float MinSpawnRadius => minSpawnRadius;
        public float MaxSpawnRadius => Mathf.Max(minSpawnRadius, maxSpawnRadius);
        public int MaxSpawnedWeapons => maxSpawnedWeapons;
        public WeaponPickupView PickupPrefab => pickupPrefab;

    }
}

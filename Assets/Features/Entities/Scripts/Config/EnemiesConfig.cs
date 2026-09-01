using System.Collections.Generic;
using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
    [CreateAssetMenu(fileName = "EnemiesConfig", menuName = "Game/EnemiesConfig")]
    /// <summary>Resources-backed spawn limits and enemy content catalog.</summary>
    public class EnemiesConfig : ScriptableObjectSingleton<EnemiesConfig>
    {
        [SerializeField]
        [Tooltip("Time in seconds between enemy spawns")]
        private float spawnInterval = 2f;

        [SerializeField]
        [Tooltip("Radius in units around the player where enemies spawn")]
        private float spawnRadius = 10f;

        [SerializeField]
        [Tooltip("Maximum number of enemies that can exist at once")]
        private int maxEnemies = 20;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Minimum horizontal distance in units between enemies")]
        private float enemySpacing = 1f;

        [SerializeField]
        [Tooltip("List of all available enemies in the game")]
        private List<EnemyConfig> enemies;

        /// <summary>Gets seconds between spawn-loop attempts.</summary>
        public float SpawnInterval => spawnInterval;

        /// <summary>Gets world-unit radius used to place spawned enemies around hero.</summary>
        public float SpawnRadius => spawnRadius;

        /// <summary>Gets maximum concurrently tracked enemies.</summary>
        public int MaxEnemies => maxEnemies;

        /// <summary>Gets minimum horizontal world-unit distance between enemies.</summary>
        public float EnemySpacing => enemySpacing;

        /// <summary>Gets configured enemy catalog; current spawn path selects index zero.</summary>
        public IReadOnlyList<EnemyConfig> Enemies => enemies;

        private Dictionary<string, EnemyConfig> _enemiesMap;

        /// <summary>Finds configured enemy by content identifier.</summary>
        /// <param name="enemyId">Identifier from <see cref="EnemyConfig.Id"/>.</param>
        /// <returns>Matching configuration, or <see langword="null"/> when no match exists.</returns>
        public EnemyConfig GetEnemyById(string enemyId)
        {
            if (_enemiesMap == null)
            {
                _enemiesMap = new Dictionary<string, EnemyConfig>();

                foreach (EnemyConfig enemy in enemies)
                {
                    _enemiesMap[enemy.Id] = enemy;
                }
            }

            _enemiesMap.TryGetValue(enemyId, out EnemyConfig config);
            return config;
        }
    }
}

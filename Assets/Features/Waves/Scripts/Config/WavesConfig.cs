using System;
using System.Collections.Generic;
using Core.ScriptableObjectSingleton;
using Game.GamePlay.Enemies;
using UnityEngine;

namespace Game.Waves
{
    [CreateAssetMenu(fileName = "WavesConfig", menuName = "Game/WavesConfig")]
    /// <summary>Resources-backed authored sequence of enemy waves.</summary>
    public sealed class WavesConfig : ScriptableObjectSingleton<WavesConfig>
    {
        [SerializeField]
        private List<WaveDefinition> waves = new List<WaveDefinition>();

        [SerializeField]
        [Min(0f)]
        [Tooltip("Minimum horizontal distance in units between all enemies")]
        private float enemySpacing = 1f;

        /// <summary>Gets waves in authored progression order.</summary>
        public IReadOnlyList<WaveDefinition> Waves => waves;

        /// <summary>Gets shared minimum horizontal world-unit distance between enemies.</summary>
        public float EnemySpacing => enemySpacing;

        /// <summary>Gets largest authored concurrent-enemy cap for presentation preallocation.</summary>
        public int MaximumConcurrentEnemies
        {
            get
            {
                var maximum = 0;

                if (waves == null)
                {
                    return maximum;
                }

                foreach (WaveDefinition wave in waves)
                {
                    if (wave != null)
                    {
                        maximum = Mathf.Max(maximum, wave.MaximumConcurrentEnemies);
                    }
                }

                return maximum;
            }
        }
    }

    /// <summary>Authoring timing and ordered enemy batches for one wave.</summary>
    [Serializable]
    public sealed class WaveDefinition
    {
        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds before this wave's first spawn")]
        private float startDelay;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds between accepted spawns and failed-spawn retries")]
        private float spawnInterval = 1f;

        [SerializeField]
        [Min(1)]
        [Tooltip("Maximum enemies this wave may have concurrently tracked")]
        private int maximumConcurrentEnemies = 20;

        [SerializeField]
        private List<WaveEnemyGroup> enemyGroups = new List<WaveEnemyGroup>();

        /// <summary>Gets seconds before this wave first requests an enemy spawn.</summary>
        public float StartDelay => startDelay;

        /// <summary>Gets seconds between this wave's accepted spawns or rejected-spawn retries.</summary>
        public float SpawnInterval => spawnInterval;

        /// <summary>Gets concurrent enemy cap for this wave, clamped to at least one.</summary>
        public int MaximumConcurrentEnemies => Mathf.Max(1, maximumConcurrentEnemies);

        /// <summary>Gets enemy batches in deterministic authored order.</summary>
        public IReadOnlyList<WaveEnemyGroup> EnemyGroups => enemyGroups;

        internal WaveDefinition(
            float startDelay,
            float spawnInterval,
            int maximumConcurrentEnemies,
            params WaveEnemyGroup[] enemyGroups
        )
        {
            this.startDelay = startDelay;
            this.spawnInterval = spawnInterval;
            this.maximumConcurrentEnemies = maximumConcurrentEnemies;
            this.enemyGroups = new List<WaveEnemyGroup>(enemyGroups);
        }
    }

    /// <summary>Authoring entry that requests one enemy type a fixed number of times.</summary>
    [Serializable]
    public sealed class WaveEnemyGroup
    {
        [SerializeField]
        private EnemyConfig enemy;

        [SerializeField]
        [Min(1)]
        private int amount = 1;

        /// <summary>Gets enemy type requested by this batch.</summary>
        public EnemyConfig Enemy => enemy;

        /// <summary>Gets requested count for this enemy type.</summary>
        public int Amount => amount;

        internal WaveEnemyGroup(EnemyConfig enemy, int amount)
        {
            this.enemy = enemy;
            this.amount = amount;
        }
    }
}

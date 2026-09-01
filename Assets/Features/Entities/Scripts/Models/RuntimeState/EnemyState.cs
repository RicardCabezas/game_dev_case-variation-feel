using UnityEngine;

namespace Game.GamePlay.Enemies
{
    /// <summary>Immutable authoritative snapshot of one runtime enemy.</summary>
    public struct EnemyState
    {
        /// <summary>Gets controller-assigned runtime identity, unique while enemy is tracked.</summary>
        public int Id { get; }

        /// <summary>Gets enemy world-space position.</summary>
        public Vector3 Position { get; }

        /// <summary>Gets current enemy health.</summary>
        public int Health { get; }

        /// <summary>Gets immutable content configuration for this enemy.</summary>
        public EnemyConfig Config { get; }

        /// <summary>Gets Unity scaled time in seconds when enemy last attacked hero.</summary>
        public float LastAttackTime { get; }

        /// <summary>Creates enemy state snapshot.</summary>
        /// <param name="id">Runtime enemy identity.</param>
        /// <param name="position">World-space position.</param>
        /// <param name="health">Current health.</param>
        /// <param name="config">Content configuration used for movement, attacks, and presentation.</param>
        /// <param name="lastAttackTime">Scaled time of latest hero attack.</param>
        public EnemyState(
            int id,
            Vector3 position,
            int health,
            EnemyConfig config,
            float lastAttackTime = 0f
        )
        {
            Id = id;
            Position = position;
            Health = health;
            Config = config;
            LastAttackTime = lastAttackTime;
        }
    }
}

using UnityEngine;

namespace Game.GamePlay.Enemies
{
    /// <summary>
    /// Self-sufficient payload emitted after accepted enemy damage and before lethal removal
    /// notification.
    /// </summary>
    public readonly struct EnemyHitResult
    {
        /// <summary>Gets runtime identity of damaged enemy.</summary>
        public int EnemyId { get; }

        /// <summary>Gets clamped health after damage.</summary>
        public int RemainingHealth { get; }

        /// <summary>Gets configured maximum health captured with this hit.</summary>
        public int MaximumHealth { get; }

        /// <summary>Gets enemy world position captured with this hit.</summary>
        public Vector3 Position { get; }

        /// <summary>Gets whether authoritative state was removed before this event.</summary>
        public bool IsLethal { get; }

        /// <summary>Creates hit event payload.</summary>
        /// <param name="enemyId">Damaged enemy identity.</param>
        /// <param name="damage">Applied damage amount.</param>
        /// <param name="remainingHealth">Health after damage.</param>
        /// <param name="maximumHealth">Configured maximum health.</param>
        /// <param name="position">Captured enemy world position.</param>
        /// <param name="isLethal">Whether removal follows event emission.</param>
        public EnemyHitResult(
            int enemyId,
            int remainingHealth,
            int maximumHealth,
            Vector3 position,
            bool isLethal
        )
        {
            EnemyId = enemyId;
            RemainingHealth = remainingHealth;
            MaximumHealth = maximumHealth;
            Position = position;
            IsLethal = isLethal;
        }
    }
}

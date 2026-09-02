using System;
using System.Collections.Generic;
using Game.GamePlay.Enemies;

namespace Game.Entities
{
    /// <summary>Read-only enemy state and notifications available to presentation consumers.</summary>
    public interface IEnemiesPresentationSource
    {
        /// <summary>Gets tracked enemy states by ID.</summary>
        IReadOnlyDictionary<int, EnemyState> CurrentStates { get; }
        /// <summary>Raised after enemy spawn.</summary>
        event Action<EnemyState> OnEnemySpawned;
        /// <summary>Raised after authoritative enemy removal; presentation may defer visual cleanup.</summary>
        event Action<int> OnEnemyRemoved;
        /// <summary>Raised after enemy movement.</summary>
        event Action<EnemyState> OnEnemyPositionChanged;
        /// <summary>Raised after accepted enemy damage.</summary>
        event Action<EnemyHitResult> OnEnemyHit;
        /// <summary>Raised after confirmed enemy attack.</summary>
        event Action<int> OnEnemyAttackPerformed;
    }
}

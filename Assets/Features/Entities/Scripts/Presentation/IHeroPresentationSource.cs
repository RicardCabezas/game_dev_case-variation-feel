using System;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.Entities
{
    /// <summary>Read-only hero state and notifications available to presentation consumers.</summary>
    public interface IHeroPresentationSource
    {
        /// <summary>Gets current authoritative hero state snapshot.</summary>
        HeroState CurrentState { get; }

        /// <summary>Raised after hero world position changes; payload is new world position.</summary>
        event Action<Vector3> OnHeroPositionChanged;

        /// <summary>
        /// Raised after incoming damage is accepted; payload contains damage, remaining health,
        /// world position, and lethality.
        /// </summary>
        event Action<HeroHitResult> OnHeroHit;

        /// <summary>Raised after a hero attack is confirmed; payload is target world position.</summary>
        event Action<Vector3> OnAttackPerformed;

        /// <summary>Raised when hero auto-attack cooldown starts; payload is duration in seconds.</summary>
        event Action<float> OnAttackCooldownStarted;

        /// <summary>Raised after hero state is restored for restart; payload is restored snapshot.</summary>
        event Action<HeroState> OnRestarted;
    }
}

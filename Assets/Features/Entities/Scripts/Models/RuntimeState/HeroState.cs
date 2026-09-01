using UnityEngine;

namespace Game.GamePlay.Heroes
{
    /// <summary>Immutable authoritative snapshot of hero gameplay state.</summary>
    public readonly struct HeroState
    {
        /// <summary>Gets hero position in world units.</summary>
        public Vector3 Position { get; }

        /// <summary>Gets current health; zero or less means dead.</summary>
        public int Health { get; }

        /// <summary>Gets Unity scaled time in seconds when last attack occurred.</summary>
        public float LastAttackTime { get; }

        /// <summary>Gets Unity scaled time in seconds before which next attack is disallowed.</summary>
        public float NextAttackTime { get; }

        /// <summary>Gets whether <see cref="Health"/> is zero or below.</summary>
        public bool IsDead => Health <= 0;

        /// <summary>Creates hero state snapshot.</summary>
        /// <param name="position">World-space hero position.</param>
        /// <param name="health">Current health.</param>
        /// <param name="lastAttackTime">Scaled time of most recent performed attack.</param>
        /// <param name="nextAttackTime">Scaled time when next attack becomes eligible.</param>
        public HeroState(
            Vector3 position,
            int health,
            float lastAttackTime,
            float nextAttackTime = 0f
        )
        {
            Position = position;
            Health = health;
            LastAttackTime = lastAttackTime;
            NextAttackTime = nextAttackTime;
        }
    }
}

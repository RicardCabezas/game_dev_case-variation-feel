using UnityEngine;

namespace Game.UI
{
    /// <summary>Immutable state displayed by one health bar.</summary>
    public readonly struct HealthBarState
    {
        /// <summary>Gets bar identity.</summary>
        public HealthBarId Id { get; }
        /// <summary>Gets current health.</summary>
        public int Health { get; }
        /// <summary>Gets maximum health.</summary>
        public int MaxHealth { get; }
        /// <summary>Gets tracked world position.</summary>
        public Vector3 WorldPosition { get; }
        /// <summary>Gets whether bar should be shown.</summary>
        public bool IsVisible { get; }
        /// <summary>Gets health normalized to 0..1.</summary>
        public float NormalizedHealth =>
            MaxHealth > 0 ? Mathf.Clamp01((float)Health / MaxHealth) : 0f;

        /// <summary>Creates health-bar state.</summary>
        /// <param name="id">Bar identity.</param>
        /// <param name="health">Current health.</param>
        /// <param name="maxHealth">Maximum health.</param>
        /// <param name="worldPosition">Tracked world position.</param>
        /// <param name="isVisible">Whether bar should be shown.</param>
        public HealthBarState(
            HealthBarId id,
            int health,
            int maxHealth,
            Vector3 worldPosition,
            bool isVisible
        )
        {
            Id = id;
            Health = health;
            MaxHealth = maxHealth;
            WorldPosition = worldPosition;
            IsVisible = isVisible;
        }

        /// <summary>Checks whether state values match.</summary>
        public bool Matches(HealthBarState other) =>
            Id.Equals(other.Id)
            && Health == other.Health
            && MaxHealth == other.MaxHealth
            && WorldPosition == other.WorldPosition
            && IsVisible == other.IsVisible;
    }
}

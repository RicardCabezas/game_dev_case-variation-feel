using System;

namespace Game.UI
{
    /// <summary>Identifies health-bar owner type.</summary>
    public enum HealthBarOwner
    {
        /// <summary>Hero health bar.</summary>
        Hero = 0,
        /// <summary>Enemy health bar.</summary>
        Enemy = 1,
    }

    /// <summary>Identifies one health bar.</summary>
    public readonly struct HealthBarId : IEquatable<HealthBarId>
    {
        /// <summary>Gets owner type.</summary>
        public HealthBarOwner Owner { get; }
        /// <summary>Gets owner-specific runtime ID.</summary>
        public int EntityId { get; }

        /// <summary>Creates health-bar identity.</summary>
        /// <param name="owner">Owner type.</param>
        /// <param name="entityId">Owner-specific ID.</param>
        public HealthBarId(HealthBarOwner owner, int entityId)
        {
            Owner = owner;
            EntityId = entityId;
        }

        /// <summary>Compares owner and entity ID.</summary>
        public bool Equals(HealthBarId other) => Owner == other.Owner && EntityId == other.EntityId;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is HealthBarId other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => unchecked(((int)Owner * 397) ^ EntityId);
    }
}

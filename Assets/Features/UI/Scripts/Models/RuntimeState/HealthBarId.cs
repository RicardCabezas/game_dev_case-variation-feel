using System;

namespace Game.UI
{
	public enum HealthBarOwner
	{
		Hero = 0,
		Enemy = 1
	}

	public readonly struct HealthBarId : IEquatable<HealthBarId>
	{
		public HealthBarOwner Owner { get; }
		public int EntityId { get; }

		public HealthBarId(HealthBarOwner owner, int entityId)
		{
			Owner = owner;
			EntityId = entityId;
		}

		public bool Equals(HealthBarId other) => Owner == other.Owner && EntityId == other.EntityId;
		public override bool Equals(object obj) => obj is HealthBarId other && Equals(other);
		public override int GetHashCode() => unchecked(((int)Owner * 397) ^ EntityId);
	}
}

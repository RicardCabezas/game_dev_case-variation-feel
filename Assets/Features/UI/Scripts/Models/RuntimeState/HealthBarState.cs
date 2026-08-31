using UnityEngine;

namespace Game.UI
{
	public readonly struct HealthBarState
	{
		public HealthBarId Id { get; }
		public int Health { get; }
		public int MaxHealth { get; }
		public Vector3 WorldPosition { get; }
		public bool IsVisible { get; }
		public float NormalizedHealth => MaxHealth > 0 ? Mathf.Clamp01((float)Health / MaxHealth) : 0f;

		public HealthBarState(HealthBarId id, int health, int maxHealth, Vector3 worldPosition, bool isVisible)
		{
			Id = id;
			Health = health;
			MaxHealth = maxHealth;
			WorldPosition = worldPosition;
			IsVisible = isVisible;
		}

		public bool Matches(HealthBarState other) =>
			Id.Equals(other.Id) &&
			Health == other.Health &&
			MaxHealth == other.MaxHealth &&
			WorldPosition == other.WorldPosition &&
			IsVisible == other.IsVisible;
	}
}

using UnityEngine;

namespace Game.JoystickInput
{
	public struct JoystickState
	{
		public static JoystickState Inactive => new JoystickState(Vector2.zero, Vector2.zero, false);

		public Vector2 JoystickCenter { get; }
		public Vector2 MovementVector { get; }
		public bool IsActive { get; }

		public JoystickState(Vector2 center, Vector2 movement, bool isActive)
		{
			JoystickCenter = center;
			MovementVector = movement;
			IsActive = isActive;
		}

		public bool Equals(JoystickState other)
		{
			return IsActive == other.IsActive &&
			       JoystickCenter == other.JoystickCenter &&
			       MovementVector == other.MovementVector;
		}
	}
}

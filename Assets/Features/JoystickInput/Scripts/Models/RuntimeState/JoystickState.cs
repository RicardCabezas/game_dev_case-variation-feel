using UnityEngine;

namespace Game.JoystickInput
{
    /// <summary>Immutable screen-space virtual joystick snapshot.</summary>
    public struct JoystickState
    {
        /// <summary>Gets inactive zero-valued joystick state.</summary>
        public static JoystickState Inactive =>
            new JoystickState(Vector2.zero, Vector2.zero, false);

        /// <summary>Gets screen-pixel center captured when input began.</summary>
        public Vector2 JoystickCenter { get; }

        /// <summary>Gets normalized movement direction, clamped to magnitude one.</summary>
        public Vector2 MovementVector { get; }

        /// <summary>Gets whether touch or mouse drag currently controls joystick.</summary>
        public bool IsActive { get; }

        /// <summary>Creates joystick state.</summary>
        /// <param name="center">Screen-pixel joystick center.</param>
        /// <param name="movement">Normalized movement vector.</param>
        /// <param name="isActive">Whether input is active.</param>
        public JoystickState(Vector2 center, Vector2 movement, bool isActive)
        {
            JoystickCenter = center;
            MovementVector = movement;
            IsActive = isActive;
        }

        /// <summary>Compares all joystick state values.</summary>
        /// <param name="other">State to compare.</param>
        /// <returns><see langword="true"/> when activity, center, and movement match.</returns>
        public bool Equals(JoystickState other)
        {
            return IsActive == other.IsActive
                && JoystickCenter == other.JoystickCenter
                && MovementVector == other.MovementVector;
        }
    }
}

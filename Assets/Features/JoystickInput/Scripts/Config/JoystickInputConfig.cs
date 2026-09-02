using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.JoystickInput
{
    [CreateAssetMenu(fileName = "JoystickInputConfig", menuName = "Game/JoystickInputConfig")]
    /// <summary>Resources-backed configuration for screen-space virtual joystick input.</summary>
    public class JoystickInputConfig : ScriptableObjectSingleton<JoystickInputConfig>
    {
        [SerializeField]
        [Tooltip("Maximum radius in pixels for joystick movement")]
        private float _maxRadius = 100f;

        [SerializeField]
        [Tooltip("Maximum seconds between primary release and secondary second press")]
        private float _secondTapWindow = .25f;

        [SerializeField]
        [Tooltip("Minimum normalized secondary joystick magnitude required on release")]
        private float _secondaryMinimumInputMagnitude = .2f;

        [SerializeField]
        [Tooltip("Joystick tint used while secondary input is active")]
        private Color _secondaryJoystickTint = new Color(1f, .35f, .1f, 1f);

        /// <summary>
        /// Gets maximum joystick displacement in screen pixels; must be positive for normalized
        /// input.
        /// </summary>
        public float MaxRadius => _maxRadius;

        /// <summary>Gets maximum interval from primary release to secondary second press.</summary>
        public float SecondTapWindow => Mathf.Max(0f, _secondTapWindow);

        /// <summary>Gets minimum normalized secondary input magnitude required on release.</summary>
        public float SecondaryMinimumInputMagnitude => Mathf.Clamp01(_secondaryMinimumInputMagnitude);

        /// <summary>Gets joystick tint used while secondary input is active.</summary>
        public Color SecondaryJoystickTint => _secondaryJoystickTint;
    }
}

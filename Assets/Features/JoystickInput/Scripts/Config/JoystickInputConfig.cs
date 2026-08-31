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

		/// <summary>Gets maximum joystick displacement in screen pixels; must be positive for normalized input.</summary>
		public float MaxRadius => _maxRadius;
	}
}

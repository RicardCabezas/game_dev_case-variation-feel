using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.JoystickInput
{
	[CreateAssetMenu(fileName = "JoystickInputConfig", menuName = "Game/JoystickInputConfig")]
	public class JoystickInputConfig : ScriptableObjectSingleton<JoystickInputConfig>
	{
		[SerializeField]
		[Tooltip("Maximum radius in pixels for joystick movement")]
		private float _maxRadius = 100f;

		public float MaxRadius => _maxRadius;
	}
}

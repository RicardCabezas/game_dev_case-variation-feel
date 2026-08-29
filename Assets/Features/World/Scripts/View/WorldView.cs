using Cinemachine;
using UnityEngine;

namespace Game.World
{
	public class WorldView : MonoBehaviour
	{
		[SerializeField] private CinemachineVirtualCamera camera;

		public CinemachineVirtualCamera Camera => camera;
	}
}
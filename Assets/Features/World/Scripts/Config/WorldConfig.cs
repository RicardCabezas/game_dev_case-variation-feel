using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.World
{
	[CreateAssetMenu(fileName = "WorldConfig", menuName = "Game/WorldConfig")]
	public class WorldConfig : ScriptableObjectSingleton<WorldConfig>
	{
		[SerializeField]
		[Tooltip("The world prefab to instantiate")]
		private WorldView worldPrefab;

		public WorldView WorldPrefab => worldPrefab;
	}
}

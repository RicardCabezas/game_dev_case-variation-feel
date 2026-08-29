using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.Biomes
{
	[CreateAssetMenu(fileName = "BiomeConfig", menuName = "Game/BiomeConfig")]
	public class BiomeConfig : ScriptableObjectSingleton<BiomeConfig>
	{
		[SerializeField]
		[Tooltip("The default biome prefab to instantiate")]
		private GameObject defaultBiomePrefab;

		public GameObject DefaultBiomePrefab => defaultBiomePrefab;
	}
}

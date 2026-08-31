using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.Biomes
{
	[CreateAssetMenu(fileName = "BiomeConfig", menuName = "Game/BiomeConfig")]
	/// <summary>Resources-backed configuration for default biome presentation.</summary>
	public class BiomeConfig : ScriptableObjectSingleton<BiomeConfig>
	{
		[SerializeField]
		[Tooltip("The default biome prefab to instantiate")]
		private GameObject defaultBiomePrefab;

		/// <summary>Gets prefab instantiated by <see cref="BiomeContainerView"/> after services initialize.</summary>
		public GameObject DefaultBiomePrefab => defaultBiomePrefab;
	}
}

using Core.ScriptableObjectSingleton;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Biomes
{
    [CreateAssetMenu(fileName = "BiomeConfig", menuName = "Game/BiomeConfig")]
    /// <summary>Resources-backed wave-indexed biome presentation configuration.</summary>
    public class BiomeConfig : ScriptableObjectSingleton<BiomeConfig>
    {
        [SerializeField]
        private BiomeDefinition defaultBiome;

        [SerializeField]
        private List<BiomeDefinition> biomes = new List<BiomeDefinition>();

        /// <summary>Gets fallback biome used at startup and wave indices without mapping.</summary>
        public BiomeDefinition DefaultBiome => defaultBiome;

        /// <summary>Gets wave-indexed biome presentation entries.</summary>
        public IReadOnlyList<BiomeDefinition> Biomes => biomes;
    }

    [Serializable]
    public sealed class BiomeDefinition
    {
        [SerializeField] private int waveIndex;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Material skybox;

        /// <summary>Gets zero-based wave index selecting this biome.</summary>
        public int WaveIndex => waveIndex;

        /// <summary>Gets arena prefab instantiated for this biome.</summary>
        public GameObject Prefab => prefab;

        /// <summary>Gets optional skybox applied while this biome is active.</summary>
        public Material Skybox => skybox;
    }
}

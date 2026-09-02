using UnityEngine;

namespace Game.Biomes
{
    /// <summary>Immutable presentation snapshot for the active wave biome.</summary>
    public sealed class BiomeState
    {
        /// <summary>Gets wave index that selected this biome.</summary>
        public int WaveIndex { get; }

        /// <summary>Gets arena prefab to instantiate.</summary>
        public GameObject Prefab { get; }

        /// <summary>Gets optional skybox material.</summary>
        public Material Skybox { get; }

        public BiomeState(int waveIndex, GameObject prefab, Material skybox)
        {
            WaveIndex = waveIndex;
            Prefab = prefab;
            Skybox = skybox;
        }
    }
}

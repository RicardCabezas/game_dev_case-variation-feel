using Core.ServicesManager;
using UnityEngine;

namespace Game.Biomes
{
    /// <summary>Presentation container that instantiates configured default biome after service startup.</summary>
    /// <remarks>Owns instantiated biome hierarchy; contains no gameplay state or decisions.</remarks>
    public class BiomeContainerView : MonoBehaviour
    {
        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            Instantiate(BiomeConfig.Instance.DefaultBiomePrefab, transform);
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
        }
    }
}

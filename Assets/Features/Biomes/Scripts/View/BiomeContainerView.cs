using Core.ServicesManager;
using UnityEngine;

namespace Game.Biomes
{
    /// <summary>Presentation container that follows the active wave-indexed biome.</summary>
    /// <remarks>Owns instantiated biome hierarchy; contains no gameplay state or decisions.</remarks>
    public class BiomeContainerView : MonoBehaviour
    {
        private BiomesService _biomes;
        private GameObject _currentBiome;

        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            _biomes = ServicesLocator.Instance.GetService<BiomesService>();
            _biomes.Presentation.OnStateChanged += OnBiomeChanged;
            OnBiomeChanged(_biomes.Presentation.CurrentState);
        }

        private void OnBiomeChanged(BiomeState state)
        {
            if (state == null || state.Prefab == null)
            {
                return;
            }

            if (_currentBiome != null)
            {
                Destroy(_currentBiome);
            }

            _currentBiome = Instantiate(state.Prefab, transform);
            if (state.Skybox != null)
            {
                RenderSettings.skybox = state.Skybox;
            }
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
            if (_biomes?.Presentation != null)
            {
                _biomes.Presentation.OnStateChanged -= OnBiomeChanged;
            }
        }
    }
}

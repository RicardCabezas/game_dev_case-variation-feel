using Core.ServicesManager;
using UnityEngine;

namespace Game.Biomes
{
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
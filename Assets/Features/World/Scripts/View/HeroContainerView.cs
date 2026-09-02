using Core.ServicesManager;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.World
{
    /// <summary>Presentation container that instantiates hero and assigns world camera follow target.</summary>
    /// <remarks>Owns no hero gameplay decisions.</remarks>
    public class HeroContainerView : MonoBehaviour
    {
        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            WorldService worldService = ServicesLocator.Instance.GetService<WorldService>();
            HeroView heroView = Instantiate(HeroConfig.Instance.HeroPrefab, transform);
            worldService.World.Camera.Follow = heroView.transform;
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
        }
    }
}

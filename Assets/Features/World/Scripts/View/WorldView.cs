using Core.ServicesManager;
using Cinemachine;
using Game.Entities;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.World
{
    /// <summary>Unity presentation root for instantiated world and its camera reference.</summary>
    public class WorldView : MonoBehaviour
    {
        [SerializeField]
        private new CinemachineVirtualCamera camera;

        [SerializeField]
        private CameraShakeView cameraShakeView;

        private IHeroPresentationSource _heroPresentation;

        private void Awake()
        {
            if (cameraShakeView == null)
            {
                cameraShakeView = GetComponent<CameraShakeView>();
            }
        }

        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        /// <summary>Gets camera configured to follow runtime hero presentation.</summary>
        public CinemachineVirtualCamera Camera => camera;

        private void OnServicesInitialized()
        {
            _heroPresentation = ServicesLocator.Instance
                .GetService<EntitiesService>()
                .HeroPresentation;
            _heroPresentation.OnHeroHit += OnHeroHit;
        }

        private void OnHeroHit(HeroHitResult hitResult)
        {
            cameraShakeView?.Play();
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;

            if (_heroPresentation != null)
            {
                _heroPresentation.OnHeroHit -= OnHeroHit;
            }
        }
    }
}

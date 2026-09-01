using Core.ServicesManager;
using Game.Entities;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Unity UI view that presents hero death and requests restart through controllers.</summary>
    /// <remarks>
    /// Listens to authoritative hero state; restart clears enemy presentation via controller events
    /// before restoring hero.
    /// </remarks>
    public class GameOverOverlayView : MonoBehaviour
    {
        [SerializeField]
        private Button restartButton;

        private IHeroPresentationSource _heroPresentation;
        private EntitiesService _entitiesService;

        private void Start()
        {
            gameObject.SetActive(false);
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            _entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
            _heroPresentation = _entitiesService.HeroPresentation;
            _heroPresentation.OnHeroHit += OnHeroHit;
            _heroPresentation.OnRestarted += OnRestarted;

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            }

            OnHeroStateChanged(_heroPresentation.CurrentState);
        }

        private void OnHeroStateChanged(HeroState heroState)
        {
            gameObject.SetActive(heroState.IsDead);
        }

        private void OnHeroHit(HeroHitResult hit)
        {

            if (hit.IsLethal)
            {
                gameObject.SetActive(true);
            }
        }

        private void OnRestarted(HeroState state) => OnHeroStateChanged(state);

        private void OnRestartButtonClicked()
        {
            _entitiesService.RestartGame();
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;

            if (_heroPresentation != null)
            {
                _heroPresentation.OnHeroHit -= OnHeroHit;
                _heroPresentation.OnRestarted -= OnRestarted;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartButtonClicked);
            }
        }
    }
}

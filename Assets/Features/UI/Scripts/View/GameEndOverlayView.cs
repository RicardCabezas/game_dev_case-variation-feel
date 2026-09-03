using Core.ServicesManager;
using Game.Entities;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Game.Waves;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Presents terminal run states, requests a restart, and displays completed-run statistics.
    /// </summary>
    public sealed class GameEndOverlayView : MonoBehaviour
    {
        [SerializeField]
        private Button restartButton;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text statisticsText;

        private WavesService _wavesService;
        private IHeroPresentationSource _heroPresentation;
        private IWavesPresentationSource _wavesPresentation;
        private GameEndAnalyticsService _analytics;

        private void Start()
        {
            gameObject.SetActive(false);
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            _wavesService = ServicesLocator.Instance.GetService<WavesService>();
            _heroPresentation = ServicesLocator.Instance.GetService<EntitiesService>().HeroPresentation;
            _wavesPresentation = _wavesService.Presentation;
            _analytics = ServicesLocator.Instance.GetService<GameEndAnalyticsService>();

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            }

            _heroPresentation.OnHeroHit += OnHeroHit;
            _heroPresentation.OnRestarted += OnRestarted;
            _wavesPresentation.OnStateChanged += OnWaveStateChanged;

            OnHeroStateChanged(_heroPresentation.CurrentState);
            OnWaveStateChanged(_wavesPresentation.CurrentState);
        }

        private void OnHeroStateChanged(HeroState heroState)
        {
            if (heroState.IsDead)
            {
                ShowGameOver();
            }
        }

        private void OnHeroHit(HeroHitResult hit)
        {
            if (hit.IsLethal)
            {
                ShowGameOver();
            }
        }

        private void OnWaveStateChanged(WaveState state)
        {
            if (state.IsComplete && !_heroPresentation.CurrentState.IsDead)
            {
                ShowGameWon(_analytics.CurrentStats);
            }
        }

        private void OnRestarted(HeroState _) => Hide();

        private void ShowGameOver()
        {
            SetTitle("GAME OVER");
            SetStatistics(null);
            gameObject.SetActive(true);
        }

        private void ShowGameWon(GameEndStats stats)
        {
            SetTitle("GAME WON");
            SetStatistics(
                $"Damage received: {stats.DamageReceived}\n"
                + $"Weapons used: {stats.WeaponsUsed}\n"
                + $"Dashes used: {stats.DashesUsed}"
            );
            gameObject.SetActive(true);
        }

        private void Hide() => gameObject.SetActive(false);

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
            if (_heroPresentation != null)
            {
                _heroPresentation.OnHeroHit -= OnHeroHit;
                _heroPresentation.OnRestarted -= OnRestarted;
            }

            if (_wavesPresentation != null)
            {
                _wavesPresentation.OnStateChanged -= OnWaveStateChanged;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartButtonClicked);
            }
        }

        private void SetTitle(string text)
        {
            if (titleText != null)
            {
                titleText.text = text;
            }
        }

        private void SetStatistics(string text)
        {
            if (statisticsText != null)
            {
                statisticsText.gameObject.SetActive(!string.IsNullOrEmpty(text));
                statisticsText.text = text;
            }
        }

        private void OnRestartButtonClicked() => _wavesService.RestartGame();
    }
}

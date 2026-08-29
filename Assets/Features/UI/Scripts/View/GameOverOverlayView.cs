using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Core.ServicesManager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	public class GameOverOverlayView : MonoBehaviour
	{
		[SerializeField] private Button restartButton;

		private HeroController _heroController;
		private EnemiesController _enemiesController;

		private void Start()
		{
			gameObject.SetActive(false);
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			_heroController = entitiesService.HeroController;
			_enemiesController = entitiesService.EnemiesController;

			_heroController.OnStateChanged += OnHeroStateChanged;

			if (restartButton != null)
			{
				restartButton.onClick.AddListener(OnRestartButtonClicked);
			}

			OnHeroStateChanged(_heroController.CurrentState);
		}

		private void OnHeroStateChanged(HeroState heroState)
		{
			gameObject.SetActive(heroState.IsDead);
		}

		private void OnRestartButtonClicked()
		{
			_enemiesController.ClearAllEnemies();
			_heroController.Restart();
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_heroController != null)
			{
				_heroController.OnStateChanged -= OnHeroStateChanged;
			}
			if (restartButton != null)
			{
				restartButton.onClick.RemoveListener(OnRestartButtonClicked);
			}
		}
	}
}
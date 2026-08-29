using Core.ServicesManager;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.JoystickInput
{
	public class JoystickView : MonoBehaviour
	{
		[SerializeField] private RectTransform joystickOuterStick;
		[SerializeField] private RectTransform joystickInnerStick;

		private float _containerRadius;
		private HeroController _heroController;
		private bool _isHeroDead;

		private void Awake()
		{
			if (joystickOuterStick != null)
			{
				_containerRadius = joystickOuterStick.sizeDelta.x * 0.5f;
			}

			joystickOuterStick.gameObject.SetActive(false);
		}

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			JoystickInputService joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			joystickInputService.OnStateChanged += OnJoystickStateChanged;

			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
			_heroController.OnStateChanged += OnHeroStateChanged;

			OnJoystickStateChanged(joystickInputService.CurrentState);
			OnHeroStateChanged(_heroController.CurrentState);
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			JoystickInputService joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			if(joystickInputService != null) joystickInputService.OnStateChanged -= OnJoystickStateChanged;
			if (_heroController != null) _heroController.OnStateChanged -= OnHeroStateChanged;
		}

		private void OnHeroStateChanged(HeroState heroState)
		{
			_isHeroDead = heroState.IsDead;
			if (_isHeroDead)
			{
				joystickOuterStick.gameObject.SetActive(false);
			}
		}

		private void OnJoystickStateChanged(JoystickState state)
		{
			if (_isHeroDead)
			{
				joystickOuterStick.gameObject.SetActive(false);
				return;
			}

			joystickOuterStick.gameObject.SetActive(state.IsActive);
			if (state.IsActive) UpdateJoystickVisuals(state);
		}

		private void UpdateJoystickVisuals(JoystickState state)
		{
			joystickOuterStick.position = state.JoystickCenter;

			Vector2 innerStickOffset = state.MovementVector * _containerRadius;
			joystickInnerStick.anchoredPosition = innerStickOffset;
		}
	}
}
using System;
using Game.GamePlay.Heroes;
using Game.JoystickInput;

namespace Game.UI
{
	public class AutoAttackIndicatorController
	{
		private HeroController _heroController;
		private JoystickInputService _joystickInputService;
		private AutoAttackIndicatorState _currentState;

		public AutoAttackIndicatorState CurrentState => _currentState;

		public event Action<AutoAttackIndicatorState> OnStateChanged;

		public void Initialize(HeroController heroController, JoystickInputService joystickInputService)
		{
			_heroController = heroController;
			_joystickInputService = joystickInputService;
			_currentState = AutoAttackIndicatorState.Hidden;

			_joystickInputService.OnStateChanged += OnJoystickStateChanged;
			_heroController.OnAttackCooldownStarted += OnAttackCooldownStarted;
		}

		public void Reset()
		{
			if (_joystickInputService != null)
			{
				_joystickInputService.OnStateChanged -= OnJoystickStateChanged;
			}

			if (_heroController != null)
			{
				_heroController.OnAttackCooldownStarted -= OnAttackCooldownStarted;
			}
		}

		private void OnJoystickStateChanged(JoystickState state)
		{
			if (state.IsActive)
			{
				Hide();
			}
		}

		private void OnAttackCooldownStarted(float duration)
		{
			if (_heroController.CurrentState.IsDead || _joystickInputService.CurrentState.IsActive) return;

			SetState(new AutoAttackIndicatorState(true, duration));
		}

		private void Hide()
		{
			if (!_currentState.IsVisible) return;

			SetState(AutoAttackIndicatorState.Hidden);
		}

		private void SetState(AutoAttackIndicatorState state)
		{
			_currentState = state;
			OnStateChanged?.Invoke(_currentState);
		}
	}
}

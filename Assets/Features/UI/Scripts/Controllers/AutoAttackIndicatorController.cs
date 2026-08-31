using System;
using Game.GamePlay.Heroes;
using Game.JoystickInput;

namespace Game.UI
{
	/// <summary>Owns automatic-attack indicator state from hero cooldown and joystick mode events.</summary>
	/// <remarks>Plain C# UI controller; view consumes state event and owns Unity UI animation.</remarks>
	public class AutoAttackIndicatorController
	{
		private HeroController _heroController;
		private JoystickInputService _joystickInputService;
		private AutoAttackIndicatorState _currentState;

		/// <summary>Gets current indicator presentation state.</summary>
		public AutoAttackIndicatorState CurrentState => _currentState;

		/// <summary>Raised after indicator state changes; payload replaces complete UI state.</summary>
		public event Action<AutoAttackIndicatorState> OnStateChanged;

		/// <summary>Subscribes to supplied gameplay contracts and initializes hidden state.</summary>
		/// <param name="heroController">Hero cooldown and death state source.</param>
		/// <param name="joystickInputService">Input mode source that hides indicator while active.</param>
		public void Initialize(HeroController heroController, JoystickInputService joystickInputService)
		{
			_heroController = heroController;
			_joystickInputService = joystickInputService;
			_currentState = AutoAttackIndicatorState.Hidden;

			_joystickInputService.OnStateChanged += OnJoystickStateChanged;
			_heroController.OnAttackCooldownStarted += OnAttackCooldownStarted;
		}

		/// <summary>Removes gameplay event subscriptions.</summary>
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

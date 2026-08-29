using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Core.ServicesManager;
using UnityEngine;

namespace Game.JoystickInput
{
	public class JoystickInputService : IService
	{
		public event Action<JoystickState> OnStateChanged;

		private JoystickState _currentState;
		private CancellationTokenSource _cancellationTokenSource;

		public JoystickState CurrentState => _currentState;

		public UniTask<bool> Initialize()
		{
			_currentState = JoystickState.Inactive;
			_cancellationTokenSource = new CancellationTokenSource();

			UpdateLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		public Type[] GetDependencies()
		{
			return Array.Empty<Type>();
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				HandleInput();
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		private void HandleInput()
		{
			bool hasInput = false;
			Vector2 inputPosition = Vector2.zero;
			bool isPressed = false;
			bool isReleased = false;

			if (Input.touchCount > 0)
			{
				Touch touch = Input.GetTouch(0);
				inputPosition = touch.position;
				hasInput = true;
				isPressed = touch.phase == TouchPhase.Began;
				isReleased = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
			}
			else if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
			{
				inputPosition = Input.mousePosition;
				hasInput = true;
				isPressed = Input.GetMouseButtonDown(0);
				isReleased = Input.GetMouseButtonUp(0);
			}

			if (isPressed)
			{
				UpdateState(new JoystickState(inputPosition, Vector2.zero, true));
			}
			else if (isReleased)
			{
				UpdateState(JoystickState.Inactive);
			}
			else if (hasInput && _currentState.IsActive)
			{
				float maxRadius = JoystickInputConfig.Instance.MaxRadius;
				Vector2 delta = inputPosition - _currentState.JoystickCenter;
				Vector2 clampedDelta = Vector2.ClampMagnitude(delta, maxRadius);
				Vector2 normalizedMovement = clampedDelta / maxRadius;

				UpdateState(new JoystickState(_currentState.JoystickCenter, normalizedMovement, true));
			}
		}

		private void UpdateState(JoystickState newState)
		{
			if (_currentState.Equals(newState)) return;

			_currentState = newState;
			OnStateChanged?.Invoke(_currentState);
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			return default;
		}
	}
}
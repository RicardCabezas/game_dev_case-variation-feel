using System;
using System.Threading;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.JoystickInput
{
    /// <summary>Polls touch or mouse input and owns virtual joystick runtime state.</summary>
    public class JoystickInputService : IService
    {
        /// <summary>Raised only when joystick state changes; payload is complete replacement state.</summary>
        public event Action<JoystickState> OnStateChanged;
        /// <summary>Raised on valid secondary release; payload is normalized screen input direction.</summary>
        public event Action<Vector2> OnSecondaryInputReleased;

        private JoystickState _currentState;
        private CancellationTokenSource _cancellationTokenSource;
        private float _lastNormalReleaseTime = float.NegativeInfinity;
        private float _normalPressTime = float.NegativeInfinity;

        /// <summary>Gets current input snapshot.</summary>
        public JoystickState CurrentState => _currentState;

        /// <summary>Forces inactive input, clears pending double-tap state, and emits only when state changes.</summary>
        public void DeactivateInput()
        {
            _lastNormalReleaseTime = float.NegativeInfinity;
            _normalPressTime = float.NegativeInfinity;
            UpdateState(JoystickState.Inactive);
        }

        /// <inheritdoc/>
        public UniTask<bool> Initialize()
        {
            _currentState = JoystickState.Inactive;
            _lastNormalReleaseTime = float.NegativeInfinity;
            _normalPressTime = float.NegativeInfinity;
            _cancellationTokenSource = new CancellationTokenSource();

            UpdateLoop(_cancellationTokenSource.Token).Forget();

            return UniTask.FromResult(true);
        }

        /// <inheritdoc/>
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
            var hasInput = false;
            Vector2 inputPosition = Vector2.zero;
            var isPressed = false;
            var isReleased = false;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                inputPosition = touch.position;
                hasInput = true;
                isPressed = touch.phase == TouchPhase.Began;
                isReleased = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            }
            else if (
                Input.GetMouseButton(0)
                || Input.GetMouseButtonDown(0)
                || Input.GetMouseButtonUp(0)
            )
            {
                inputPosition = Input.mousePosition;
                hasInput = true;
                isPressed = Input.GetMouseButtonDown(0);
                isReleased = Input.GetMouseButtonUp(0);
            }

            if (isPressed)
            {
                var secondaryInput = Time.time - _lastNormalReleaseTime
                    <= JoystickInputConfig.Instance.SecondTapWindow;
                _lastNormalReleaseTime = float.NegativeInfinity;
                _normalPressTime = secondaryInput ? float.NegativeInfinity : Time.time;
                UpdateState(
                    new JoystickState(
                        inputPosition,
                        Vector2.zero,
                        true,
                        secondaryInput ? JoystickInputMode.Secondary : JoystickInputMode.Normal
                    )
                );
            }
            else if (isReleased)
            {
                if (_currentState.Mode == JoystickInputMode.Secondary)
                {
                    Vector2 direction = GetMovementVector(inputPosition);
                    if (direction.magnitude >= JoystickInputConfig.Instance.SecondaryMinimumInputMagnitude)
                    {
                        OnSecondaryInputReleased?.Invoke(direction.normalized);
                    }
                }
                else
                {
                    _lastNormalReleaseTime = Time.time - _normalPressTime
                        <= JoystickInputConfig.Instance.SecondTapWindow
                        ? Time.time
                        : float.NegativeInfinity;
                }
                UpdateState(JoystickState.Inactive);
            }
            else if (hasInput && _currentState.IsActive)
            {
                UpdateState(
                    new JoystickState(
                        _currentState.JoystickCenter,
                        GetMovementVector(inputPosition),
                        true,
                        _currentState.Mode
                    )
                );
            }
        }

        private Vector2 GetMovementVector(Vector2 inputPosition)
        {
            var maxRadius = JoystickInputConfig.Instance.MaxRadius;
            if (maxRadius <= 0f)
            {
                return Vector2.zero;
            }

            Vector2 delta = inputPosition - _currentState.JoystickCenter;
            return Vector2.ClampMagnitude(delta, maxRadius) / maxRadius;
        }

        private void UpdateState(JoystickState newState)
        {
            if (_currentState.Equals(newState))
            {
                return;
            }

            _currentState = newState;
            OnStateChanged?.Invoke(_currentState);
        }

        /// <inheritdoc/>
        public UniTask Reset()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            return default;
        }
    }
}

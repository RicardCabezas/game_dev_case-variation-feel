using System;
using Game.JoystickInput;

namespace Game.UI
{
    /// <summary>Owns indicator state from explicit presentation and input values.</summary>
    internal sealed class AutoAttackIndicatorController : IAutoAttackIndicatorPresentationSource
    {
        private AutoAttackIndicatorState _state = AutoAttackIndicatorState.Hidden;

        /// <summary>Gets current indicator state.</summary>
        public AutoAttackIndicatorState CurrentState => _state;
        /// <summary>Raised after indicator state replacement.</summary>
        public event Action<AutoAttackIndicatorState> OnStateChanged;

        /// <summary>Hides indicator while input is active.</summary>
        public void ApplyJoystick(JoystickState state)
        {

            if (state.IsActive)
            {
                Hide();
            }
        }

        /// <summary>Hides indicator after hero death.</summary>
        public void ApplyHeroDeath() => Hide();

        /// <summary>Hides indicator after restart.</summary>
        public void ApplyRestart()
        {
            Hide();
        }

        /// <summary>Shows cooldown when hero is alive and input is inactive.</summary>
        public void StartCooldown(float duration, bool heroDead, bool inputActive)
        {

            if (!heroDead && !inputActive)
            {
                Set(new AutoAttackIndicatorState(true, duration));
            }
        }

        private void Hide()
        {

            if (_state.IsVisible)
            {
                Set(AutoAttackIndicatorState.Hidden);
            }
        }

        private void Set(AutoAttackIndicatorState state)
        {
            _state = state;
            OnStateChanged?.Invoke(state);
        }
    }
}

using System;
using Game.Weapons;

namespace Game.UI
{
    public sealed class WeaponUsesIndicatorController : IWeaponUsesIndicatorPresentationSource
    {
        public WeaponUsesIndicatorState CurrentState { get; private set; }
        public event Action<WeaponUsesIndicatorState> OnStateChanged;

        public void Apply(EquippedWeaponState state)
        {
            string label = state.IsArmed ? state.Weapon.DisplayName : "Unarmed";
            CurrentState = new WeaponUsesIndicatorState(
                $"{label} — {state.RemainingUses} / {state.MaxUses}",
                state.RemainingUses,
                state.MaxUses
            );
            OnStateChanged?.Invoke(CurrentState);
        }
    }
}

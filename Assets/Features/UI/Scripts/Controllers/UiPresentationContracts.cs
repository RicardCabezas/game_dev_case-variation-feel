using System;
using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>Read-only current weapon durability state and events.</summary>
    public interface IWeaponUsesIndicatorPresentationSource
    {
        WeaponUsesIndicatorState CurrentState { get; }
        event Action<WeaponUsesIndicatorState> OnStateChanged;
    }
    /// <summary>Read-only automatic-attack indicator state and events.</summary>
    public interface IAutoAttackIndicatorPresentationSource
    {
        /// <summary>Gets current indicator state.</summary>
        AutoAttackIndicatorState CurrentState { get; }
        /// <summary>Raised after indicator state replacement.</summary>
        event Action<AutoAttackIndicatorState> OnStateChanged;
    }

    /// <summary>Read-only health-bar state and events.</summary>
    public interface IHealthBarsPresentationSource
    {
        /// <summary>Gets current health-bar states.</summary>
        IReadOnlyDictionary<HealthBarId, HealthBarState> CurrentStates { get; }
        /// <summary>Raised when a bar is added.</summary>
        event Action<HealthBarState> OnHealthBarAdded;
        /// <summary>Raised when a bar changes.</summary>
        event Action<HealthBarState> OnHealthBarChanged;
        /// <summary>Raised when a bar is removed.</summary>
        event Action<HealthBarId> OnHealthBarRemoved;
    }
}

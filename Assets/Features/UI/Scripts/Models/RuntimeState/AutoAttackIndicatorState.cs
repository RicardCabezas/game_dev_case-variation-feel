namespace Game.UI
{
    /// <summary>Immutable presentation state for automatic-attack cooldown indicator.</summary>
    public struct AutoAttackIndicatorState
    {
        /// <summary>Gets hidden indicator state with zero fill duration.</summary>
        public static AutoAttackIndicatorState Hidden => new AutoAttackIndicatorState(false, 0f);

        /// <summary>Gets whether indicator should be visible.</summary>
        public bool IsVisible { get; }

        /// <summary>Gets cooldown fill duration in seconds when visible.</summary>
        public float FillDuration { get; }

        /// <summary>Creates indicator state.</summary>
        /// <param name="isVisible">Whether presentation should show indicator.</param>
        /// <param name="fillDuration">Seconds used to fill indicator.</param>
        public AutoAttackIndicatorState(bool isVisible, float fillDuration)
        {
            IsVisible = isVisible;
            FillDuration = fillDuration;
        }
    }
}

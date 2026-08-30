namespace Game.UI
{
	public struct AutoAttackIndicatorState
	{
		public static AutoAttackIndicatorState Hidden => new AutoAttackIndicatorState(false, 0f);

		public bool IsVisible { get; }
		public float FillDuration { get; }

		public AutoAttackIndicatorState(bool isVisible, float fillDuration)
		{
			IsVisible = isVisible;
			FillDuration = fillDuration;
		}
	}
}

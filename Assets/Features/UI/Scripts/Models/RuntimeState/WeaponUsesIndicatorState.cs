namespace Game.UI
{
    public readonly struct WeaponUsesIndicatorState
    {
        public readonly string Label;
        public readonly int Remaining;
        public readonly int Maximum;
        public float Fill => Maximum > 0 ? (float)Remaining / Maximum : 0f;

        public WeaponUsesIndicatorState(string label, int remaining, int maximum)
        {
            Label = label;
            Remaining = remaining;
            Maximum = maximum;
        }
    }
}

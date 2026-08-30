namespace Game.GamePlay.Heroes
{
	/// <summary>Describes one applied incoming hit after hero health is updated.</summary>
	public readonly struct HeroHitResult
	{
		/// <summary>Gets applied incoming damage.</summary>
		public int Damage { get; }
		/// <summary>Gets hero health after damage is applied.</summary>
		public int RemainingHealth { get; }
		/// <summary>Gets whether applied damage reduced health to zero.</summary>
		public bool IsLethal { get; }

		/// <summary>Creates an incoming-hit result.</summary>
		/// <param name="damage">Applied incoming damage.</param>
		/// <param name="remainingHealth">Hero health after damage is applied.</param>
		/// <param name="isLethal">Whether applied damage reduced health to zero.</param>
		public HeroHitResult(int damage, int remainingHealth, bool isLethal)
		{
			Damage = damage;
			RemainingHealth = remainingHealth;
			IsLethal = isLethal;
		}
	}
}

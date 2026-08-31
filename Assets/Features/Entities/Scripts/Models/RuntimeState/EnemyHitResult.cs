namespace Game.GamePlay.Enemies
{
	/// <summary>Payload emitted when an enemy receives hero damage, before lethal removal.</summary>
	public readonly struct EnemyHitResult
	{
		/// <summary>Gets runtime identity of damaged enemy.</summary>
		public int EnemyId { get; }
		/// <summary>Gets damage amount supplied to attack operation.</summary>
		public int Damage { get; }
		/// <summary>Gets health after damage; may be zero or negative for lethal hit.</summary>
		public int RemainingHealth { get; }
		/// <summary>Gets whether controller will remove enemy after this event.</summary>
		public bool IsLethal { get; }

		/// <summary>Creates hit event payload.</summary>
		/// <param name="enemyId">Damaged enemy identity.</param>
		/// <param name="damage">Applied damage amount.</param>
		/// <param name="remainingHealth">Health after damage.</param>
		/// <param name="isLethal">Whether removal follows event emission.</param>
		public EnemyHitResult(int enemyId, int damage, int remainingHealth, bool isLethal)
		{
			EnemyId = enemyId;
			Damage = damage;
			RemainingHealth = remainingHealth;
			IsLethal = isLethal;
		}
	}
}

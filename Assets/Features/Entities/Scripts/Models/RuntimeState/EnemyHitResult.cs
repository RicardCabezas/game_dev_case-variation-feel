namespace Game.GamePlay.Enemies
{
	public readonly struct EnemyHitResult
	{
		public int EnemyId { get; }
		public int Damage { get; }
		public int RemainingHealth { get; }
		public bool IsLethal { get; }

		public EnemyHitResult(int enemyId, int damage, int remainingHealth, bool isLethal)
		{
			EnemyId = enemyId;
			Damage = damage;
			RemainingHealth = remainingHealth;
			IsLethal = isLethal;
		}
	}
}

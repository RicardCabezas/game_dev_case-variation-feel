namespace Game.GamePlay.Entities
{
    /// <summary>Queued enemy attack command.</summary>
    internal readonly struct EnemyAttackRequest
    {
        /// <summary>Gets attacking enemy ID.</summary>
        public int EnemyId { get; }
        /// <summary>Gets damage to apply.</summary>
        public int Damage { get; }

        /// <summary>Creates an attack command.</summary>
        /// <param name="enemyId">Attacking enemy ID.</param>
        /// <param name="damage">Damage to apply.</param>
        public EnemyAttackRequest(int enemyId, int damage)
        {
            EnemyId = enemyId;
            Damage = damage;
        }
    }
}

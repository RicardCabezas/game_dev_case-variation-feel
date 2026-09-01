using UnityEngine;

namespace Game.GamePlay.Entities
{
    /// <summary>Queued hero attack command.</summary>
    internal readonly struct HeroAttackRequest
    {
        /// <summary>Gets target enemy ID.</summary>
        public int EnemyId { get; }
        /// <summary>Gets captured target position.</summary>
        public Vector3 TargetPosition { get; }
        /// <summary>Gets damage to apply.</summary>
        public int Damage { get; }
        /// <summary>Gets cooldown duration in seconds.</summary>
        public float Cooldown { get; }

        /// <summary>Creates an attack command.</summary>
        /// <param name="enemyId">Target enemy ID.</param>
        /// <param name="targetPosition">Captured target position.</param>
        /// <param name="damage">Damage to apply.</param>
        /// <param name="cooldown">Cooldown duration in seconds.</param>
        public HeroAttackRequest(int enemyId, Vector3 targetPosition, int damage, float cooldown)
        {
            EnemyId = enemyId;
            TargetPosition = targetPosition;
            Damage = damage;
            Cooldown = cooldown;
        }
    }
}

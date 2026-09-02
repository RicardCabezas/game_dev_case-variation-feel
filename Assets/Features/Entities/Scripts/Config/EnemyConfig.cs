using UnityEngine;

namespace Game.GamePlay.Enemies
{
    [CreateAssetMenu(fileName = "EnemyX", menuName = "Content/Enemy")]
    /// <summary>Authoring data for one spawnable enemy kind.</summary>
    public class EnemyConfig : ScriptableObject
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private int initialHealth;

        [SerializeField]
        private float speed;

        [SerializeField]
        private float attackCooldown;

        [SerializeField]
        private int attackDamage;

        [SerializeField]
        private float attackRange;

        [SerializeField]
        private EnemyView prefab;

        [SerializeField]
        [Min(0f)]
        private float spawnRadius = 10f;

        /// <summary>Gets stable content identifier for authored content.</summary>
        public string Id => id;

        /// <summary>Gets health assigned to each spawned enemy.</summary>
        public int InitialHealth => initialHealth;

        /// <summary>Gets chase speed in world units per second.</summary>
        public float Speed => speed;

        /// <summary>Gets minimum seconds between enemy attacks.</summary>
        public float AttackCooldown => attackCooldown;

        /// <summary>Gets integer damage applied to hero for each attack.</summary>
        public int AttackDamage => attackDamage;

        /// <summary>Gets world-unit distance at which enemy stops chasing and may attack.</summary>
        public float AttackRange => attackRange;

        /// <summary>Gets world-unit radius around hero used when this enemy is created.</summary>
        public float SpawnRadius => spawnRadius;

        /// <summary>Gets presentation prefab instantiated by <see cref="EnemiesContainerView"/>.</summary>
        public EnemyView Prefab => prefab;
    }
}

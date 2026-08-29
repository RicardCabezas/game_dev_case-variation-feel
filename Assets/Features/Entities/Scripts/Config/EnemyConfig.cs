using UnityEngine;

namespace Game.GamePlay.Enemies
{
	[CreateAssetMenu(fileName = "EnemyX", menuName = "Content/Enemy")]
	public class EnemyConfig : ScriptableObject
	{
		[SerializeField] private string id;
		[SerializeField] private int initialHealth;
		[SerializeField] private float speed;
		[SerializeField] private float attackCooldown;
		[SerializeField] private int attackDamage;
		[SerializeField] private float attackRange;
		[SerializeField] private EnemyView prefab;

		public string Id => id;
		public int InitialHealth => initialHealth;
		public float Speed => speed;
		public float AttackCooldown => attackCooldown;
		public int AttackDamage => attackDamage;
		public float AttackRange => attackRange;
		public EnemyView Prefab => prefab;
	}
}

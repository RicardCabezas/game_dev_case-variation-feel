using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public struct EnemyState
	{
		public int Id { get; }
		public Vector3 Position { get; }
		public int Health { get; }
		public EnemyConfig Config { get; }
		public float LastAttackTime { get; }

		public EnemyState(int id, Vector3 position, int health, EnemyConfig config, float lastAttackTime = 0f)
		{
			Id = id;
			Position = position;
			Health = health;
			Config = config;
			LastAttackTime = lastAttackTime;
		}
	}
}
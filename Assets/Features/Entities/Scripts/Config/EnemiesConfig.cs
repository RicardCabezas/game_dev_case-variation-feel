using System.Collections.Generic;
using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	[CreateAssetMenu(fileName = "EnemiesConfig", menuName = "Game/EnemiesConfig")]
	public class EnemiesConfig : ScriptableObjectSingleton<EnemiesConfig>
	{
		[SerializeField]
		[Tooltip("Time in seconds between enemy spawns")]
		private float spawnInterval = 2f;

		[SerializeField]
		[Tooltip("Radius in units around the player where enemies spawn")]
		private float spawnRadius = 10f;

		[SerializeField]
		[Tooltip("Maximum number of enemies that can exist at once")]
		private int maxEnemies = 20;

		[SerializeField]
		[Tooltip("List of all available enemies in the game")]
		private List<EnemyConfig> enemies;

		public float SpawnInterval => spawnInterval;
		public float SpawnRadius => spawnRadius;
		public int MaxEnemies => maxEnemies;
		public IReadOnlyList<EnemyConfig> Enemies => enemies;

		private Dictionary<string, EnemyConfig> _enemiesMap;

		public EnemyConfig GetEnemyById(string enemyId)
		{
			if (_enemiesMap == null)
			{
				_enemiesMap = new Dictionary<string, EnemyConfig>();
				foreach (EnemyConfig enemy in enemies) _enemiesMap[enemy.Id] = enemy;
			}

			_enemiesMap.TryGetValue(enemyId, out EnemyConfig config);
			return config;
		}
	}
}

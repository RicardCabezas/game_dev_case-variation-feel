using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	/// <summary>Owns runtime enemy state, spawning, chase movement, attacks, damage, and removal.</summary>
	/// <remarks>Plain C# controller; views consume its events but do not own enemy decisions or state.</remarks>
	public class EnemiesController
	{
		private HeroController _heroController;

		// Events
		/// <summary>Raised after enemy is added; payload is its initial state.</summary>
		public event Action<EnemyState> OnEnemySpawned;
		/// <summary>Raised after enemy leaves controller state; payload is removed runtime identity.</summary>
		public event Action<int> OnEnemyRemoved;
		/// <summary>Raised when chase movement changes enemy position; payload contains replacement state.</summary>
		public event Action<EnemyState> OnEnemyPositionChanged;
		/// <summary>Raised for every damage attempt before lethal removal; views use nonlethal payloads for hit feedback.</summary>
		public event Action<EnemyHitResult> OnEnemyHit;
		/// <summary>Raised after enemy damage is applied to hero; payload is attacking enemy identity.</summary>
		public event Action<int> OnEnemyAttackPerformed;

		// State
		private Dictionary<int, EnemyState> _enemies;
		private CancellationTokenSource _cancellationTokenSource;
		private int _nextEnemyId;

		/// <summary>Gets authoritative enemies indexed by runtime identity.</summary>
		public IReadOnlyDictionary<int, EnemyState> Enemies => _enemies;

		/// <summary>Allocates runtime state and starts spawn and update loops.</summary>
		/// <param name="heroController">Hero state owner used for spawning, targeting, and receiving attacks.</param>
		/// <returns>Completed successful initialization task.</returns>
		public UniTask<bool> Initialize(HeroController heroController)
		{
			_heroController = heroController;

			_enemies = new Dictionary<int, EnemyState>();
			_nextEnemyId = 0;
			_cancellationTokenSource = new CancellationTokenSource();

			SpawnLoop(_cancellationTokenSource.Token).Forget();
			UpdateLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		/// <summary>Cancels loops, disposes their token source, and clears tracked enemies.</summary>
		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_enemies.Clear();

			return UniTask.CompletedTask;
		}

		/// <summary>Removes every tracked enemy and raises <see cref="OnEnemyRemoved"/> for each.</summary>
		public void ClearAllEnemies()
		{
			List<int> enemyIds = new List<int>(_enemies.Keys);
			foreach (int enemyId in enemyIds)
			{
				RemoveEnemy(enemyId);
			}
		}

		/// <summary>Removes one enemy if present.</summary>
		/// <param name="enemyId">Runtime identity to remove.</param>
		public void RemoveEnemy(int enemyId)
		{
			if (_enemies.Remove(enemyId))
			{
				OnEnemyRemoved?.Invoke(enemyId);
			}
		}

		/// <summary>Applies hero damage to currently tracked enemy and emits hit/removal transitions.</summary>
		/// <param name="enemyState">Target snapshot; its identity selects current authoritative enemy.</param>
		/// <param name="damage">Damage subtracted from current health; positive values are expected.</param>
		/// <remarks>Unknown targets are ignored. Lethal hits emit <see cref="OnEnemyHit"/> before <see cref="OnEnemyRemoved"/>.</remarks>
		public void AttackEnemy(EnemyState enemyState, int damage)
		{
			if (!_enemies.TryGetValue(enemyState.Id, out EnemyState currentEnemy)) return;

			int newHealth = currentEnemy.Health - damage;
			bool isLethal = newHealth <= 0;

			Debug.Log($"Attacked enemy id°{currentEnemy.Id}. Health : {currentEnemy.Health} -> {newHealth}");
			OnEnemyHit?.Invoke(new EnemyHitResult(currentEnemy.Id, damage, newHealth, isLethal));

			if (isLethal)
			{
				Debug.Log($"Enemy id°{currentEnemy.Id} is dead. Removing it.");
				RemoveEnemy(currentEnemy.Id);
			}
			else
			{
				EnemyState updatedEnemy = new EnemyState(currentEnemy.Id, currentEnemy.Position, newHealth, currentEnemy.Config, currentEnemy.LastAttackTime);
				_enemies[currentEnemy.Id] = updatedEnemy;
			}
		}

		private async UniTaskVoid SpawnLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (!_heroController.CurrentState.IsDead && _enemies.Count < EnemiesConfig.Instance.MaxEnemies)
				{
					SpawnEnemy();
				}

				await UniTask.Delay(TimeSpan.FromSeconds(EnemiesConfig.Instance.SpawnInterval), cancellationToken: cancellationToken);
			}
		}

		private void SpawnEnemy()
		{
			if (EnemiesConfig.Instance.Enemies.Count == 0) return;

			Vector3 playerPosition = _heroController.CurrentState.Position;
			Vector3 spawnPosition = GetRandomPositionAroundPlayer(playerPosition);

			int enemyId = _nextEnemyId++;
			EnemyConfig enemyConfig = EnemiesConfig.Instance.Enemies[0];
			EnemyState newEnemy = new EnemyState(enemyId, spawnPosition, enemyConfig.InitialHealth, enemyConfig);

			_enemies[enemyId] = newEnemy;
			OnEnemySpawned?.Invoke(newEnemy);
		}

		private Vector3 GetRandomPositionAroundPlayer(Vector3 playerPosition)
		{
			float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
			float x = playerPosition.x + EnemiesConfig.Instance.SpawnRadius * Mathf.Cos(angle);
			float z = playerPosition.z + EnemiesConfig.Instance.SpawnRadius * Mathf.Sin(angle);

			return new Vector3(x, playerPosition.y, z);
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (_heroController.CurrentState.IsDead)
				{
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
					continue;
				}

				List<int> enemiesToUpdate = new List<int>(_enemies.Keys);
				for (int i = 0; i < enemiesToUpdate.Count; i++)
				{
					int enemyId = enemiesToUpdate[i];
					if (!_enemies.TryGetValue(enemyId, out EnemyState enemy)) continue;

					UpdateEnemy(enemy);
				}

				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		private void UpdateEnemy(EnemyState enemy)
		{
			Vector3 heroPosition = _heroController.CurrentState.Position;
			float distanceToHero = Vector3.Distance(enemy.Position, heroPosition);

			if (distanceToHero > enemy.Config.AttackRange)
			{
				Vector3 direction = (heroPosition - enemy.Position).normalized;
				Vector3 newPosition = enemy.Position + direction * (enemy.Config.Speed * Time.deltaTime);

				EnemyState updatedEnemy = new EnemyState(enemy.Id, newPosition, enemy.Health, enemy.Config, enemy.LastAttackTime);
				_enemies[enemy.Id] = updatedEnemy;
				OnEnemyPositionChanged?.Invoke(updatedEnemy);
			}
			else
			{
				if (Time.time - enemy.LastAttackTime >= enemy.Config.AttackCooldown)
				{
					_heroController.TakeHit(enemy.Config.AttackDamage);
					OnEnemyAttackPerformed?.Invoke(enemy.Id);

					EnemyState updatedEnemy = new EnemyState(enemy.Id, enemy.Position, enemy.Health, enemy.Config, Time.time);
					_enemies[enemy.Id] = updatedEnemy;
				}
			}
		}
	}
}

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
		private const int SeparationPasses = 2;
		private const int SpawnPositionAttempts = 8;

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
		private List<int> _enemyIdsBuffer;
		private List<EnemyState> _updatedEnemiesBuffer;
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
			_enemyIdsBuffer = new List<int>(EnemiesConfig.Instance.MaxEnemies);
			_updatedEnemiesBuffer = new List<EnemyState>(EnemiesConfig.Instance.MaxEnemies);
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
			_enemyIdsBuffer.Clear();
			_updatedEnemiesBuffer.Clear();

			return UniTask.CompletedTask;
		}

		/// <summary>Removes every tracked enemy and raises <see cref="OnEnemyRemoved"/> for each.</summary>
		public void ClearAllEnemies()
		{
			CollectEnemyIds();
			for (int i = 0; i < _enemyIdsBuffer.Count; i++)
			{
				RemoveEnemy(_enemyIdsBuffer[i]);
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
			if (!TryGetSpawnPosition(playerPosition, out Vector3 spawnPosition)) return;

			int enemyId = _nextEnemyId++;
			EnemyConfig enemyConfig = EnemiesConfig.Instance.Enemies[0];
			EnemyState newEnemy = new EnemyState(enemyId, spawnPosition, enemyConfig.InitialHealth, enemyConfig);

			_enemies[enemyId] = newEnemy;
			OnEnemySpawned?.Invoke(newEnemy);
		}

		private bool TryGetSpawnPosition(Vector3 playerPosition, out Vector3 spawnPosition)
		{
			float spacing = EnemiesConfig.Instance.EnemySpacing;
			float spacingSqr = spacing * spacing;

			for (int i = 0; i < SpawnPositionAttempts; i++)
			{
				float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
				float x = playerPosition.x + EnemiesConfig.Instance.SpawnRadius * Mathf.Cos(angle);
				float z = playerPosition.z + EnemiesConfig.Instance.SpawnRadius * Mathf.Sin(angle);
				spawnPosition = new Vector3(x, playerPosition.y, z);

				if (IsPositionClear(spawnPosition, spacingSqr)) return true;
			}

			spawnPosition = default;
			return false;
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

				UpdateEnemies();

				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		private void UpdateEnemies()
		{
			Vector3 heroPosition = _heroController.CurrentState.Position;
			CollectEnemyIds();
			_updatedEnemiesBuffer.Clear();

			for (int i = 0; i < _enemyIdsBuffer.Count; i++)
			{
				if (_enemies.TryGetValue(_enemyIdsBuffer[i], out EnemyState enemy))
				{
					_updatedEnemiesBuffer.Add(UpdateEnemy(enemy, heroPosition));
				}
			}

			ResolveEnemySpacing();

			for (int i = 0; i < _updatedEnemiesBuffer.Count; i++)
			{
				EnemyState updatedEnemy = _updatedEnemiesBuffer[i];
				if (!_enemies.TryGetValue(updatedEnemy.Id, out EnemyState previousEnemy)) continue;

				_enemies[updatedEnemy.Id] = updatedEnemy;
				if (updatedEnemy.Position != previousEnemy.Position)
				{
					OnEnemyPositionChanged?.Invoke(updatedEnemy);
				}
			}
		}

		private EnemyState UpdateEnemy(EnemyState enemy, Vector3 heroPosition)
		{
			float attackRange = enemy.Config.AttackRange;
			if ((heroPosition - enemy.Position).sqrMagnitude > attackRange * attackRange)
			{
				Vector3 direction = heroPosition - enemy.Position;
				direction.y = 0f;
				direction.Normalize();
				Vector3 newPosition = enemy.Position + direction * (enemy.Config.Speed * Time.deltaTime);
				return new EnemyState(enemy.Id, newPosition, enemy.Health, enemy.Config, enemy.LastAttackTime);
			}

			if (Time.time - enemy.LastAttackTime < enemy.Config.AttackCooldown) return enemy;

			_heroController.TakeHit(enemy.Config.AttackDamage);
			OnEnemyAttackPerformed?.Invoke(enemy.Id);
			return new EnemyState(enemy.Id, enemy.Position, enemy.Health, enemy.Config, Time.time);
		}

		private void ResolveEnemySpacing()
		{
			float spacing = EnemiesConfig.Instance.EnemySpacing;
			if (spacing <= 0f) return;

			float spacingSqr = spacing * spacing;
			for (int pass = 0; pass < SeparationPasses; pass++)
			{
				for (int firstIndex = 0; firstIndex < _updatedEnemiesBuffer.Count - 1; firstIndex++)
				{
					for (int secondIndex = firstIndex + 1; secondIndex < _updatedEnemiesBuffer.Count; secondIndex++)
					{
						SeparatePair(firstIndex, secondIndex, spacing, spacingSqr);
					}
				}
			}
		}

		private void SeparatePair(int firstIndex, int secondIndex, float spacing, float spacingSqr)
		{
			EnemyState firstEnemy = _updatedEnemiesBuffer[firstIndex];
			EnemyState secondEnemy = _updatedEnemiesBuffer[secondIndex];
			Vector3 difference = firstEnemy.Position - secondEnemy.Position;
			difference.y = 0f;
			float distanceSqr = difference.sqrMagnitude;
			if (distanceSqr >= spacingSqr) return;

			float distance = Mathf.Sqrt(distanceSqr);
			Vector3 direction = distance > 0f ? difference / distance : GetOverlapDirection(firstEnemy.Id, secondEnemy.Id);
			Vector3 correction = direction * ((spacing - distance) * 0.5f);

			_updatedEnemiesBuffer[firstIndex] = new EnemyState(firstEnemy.Id, firstEnemy.Position + correction, firstEnemy.Health, firstEnemy.Config, firstEnemy.LastAttackTime);
			_updatedEnemiesBuffer[secondIndex] = new EnemyState(secondEnemy.Id, secondEnemy.Position - correction, secondEnemy.Health, secondEnemy.Config, secondEnemy.LastAttackTime);
		}

		private bool IsPositionClear(Vector3 position, float spacingSqr)
		{
			if (spacingSqr <= 0f) return true;

			foreach (EnemyState enemy in _enemies.Values)
			{
				if (GetHorizontalSqrDistance(position, enemy.Position) < spacingSqr) return false;
			}

			return true;
		}

		private void CollectEnemyIds()
		{
			_enemyIdsBuffer.Clear();
			foreach (int enemyId in _enemies.Keys)
			{
				_enemyIdsBuffer.Add(enemyId);
			}
		}

		private static float GetHorizontalSqrDistance(Vector3 firstPosition, Vector3 secondPosition)
		{
			float x = firstPosition.x - secondPosition.x;
			float z = firstPosition.z - secondPosition.z;
			return x * x + z * z;
		}

		private static Vector3 GetOverlapDirection(int firstId, int secondId)
		{
			float angle = (firstId * 0.61803398875f + secondId * 0.38196601125f) * Mathf.PI * 2f;
			return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
		}
	}
}

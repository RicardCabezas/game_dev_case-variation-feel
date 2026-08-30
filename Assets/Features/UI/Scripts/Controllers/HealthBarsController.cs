using System;
using System.Collections.Generic;
using Game.GamePlay.Enemies;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.UI
{
	public sealed class HealthBarsController
	{
		public const float DefaultEnemyVisibilityDuration = 2f;

		private readonly HeroController _heroController;
		private readonly EnemiesController _enemiesController;
		private readonly float _enemyVisibilityDuration;
		private readonly Dictionary<HealthBarId, HealthBarState> _states;
		private readonly Dictionary<int, float> _enemyHideTimes;
		private readonly List<int> _visibleEnemyIds;
		private bool _isInitialized;
		private float _currentTime;

		public IReadOnlyDictionary<HealthBarId, HealthBarState> CurrentStates => _states;

		public event Action<HealthBarState> OnHealthBarAdded;
		public event Action<HealthBarState> OnHealthBarChanged;
		public event Action<HealthBarId> OnHealthBarRemoved;

		public HealthBarsController(HeroController heroController, EnemiesController enemiesController, int maxEnemies, float enemyVisibilityDuration = DefaultEnemyVisibilityDuration)
		{
			_heroController = heroController;
			_enemiesController = enemiesController;
			_enemyVisibilityDuration = Mathf.Max(0f, enemyVisibilityDuration);
			int capacity = Mathf.Max(1, maxEnemies + 1);
			_states = new Dictionary<HealthBarId, HealthBarState>(capacity);
			_enemyHideTimes = new Dictionary<int, float>(Mathf.Max(1, maxEnemies));
			_visibleEnemyIds = new List<int>(Mathf.Max(1, maxEnemies));
		}

		public void Initialize(float initialTime)
		{
			if (_isInitialized) return;

			_currentTime = initialTime;
			_isInitialized = true;
			_heroController.OnStateChanged += OnHeroStateChanged;
			_enemiesController.OnEnemyHit += OnEnemyHit;
			_enemiesController.OnEnemyPositionChanged += OnEnemyPositionChanged;
			_enemiesController.OnEnemyRemoved += OnEnemyRemoved;

			HealthBarId heroId = new HealthBarId(HealthBarOwner.Hero, 0);
			HealthBarState heroState = CreateHeroState(_heroController.CurrentState, true);
			_states.Add(heroId, heroState);
			OnHealthBarAdded?.Invoke(heroState);
		}

		public void Tick(float currentTime)
		{
			_currentTime = currentTime;
			for (int i = _visibleEnemyIds.Count - 1; i >= 0; i--)
			{
				int enemyId = _visibleEnemyIds[i];
				if (!_enemyHideTimes.TryGetValue(enemyId, out float hideTime) || hideTime > currentTime) continue;

				_visibleEnemyIds.RemoveAt(i);
				if (!_states.TryGetValue(new HealthBarId(HealthBarOwner.Enemy, enemyId), out HealthBarState state) || !state.IsVisible) continue;

				HealthBarState hiddenState = new HealthBarState(state.Id, state.Health, state.MaxHealth, state.WorldPosition, false);
				_states[state.Id] = hiddenState;
				OnHealthBarChanged?.Invoke(hiddenState);
			}
		}

		public void Reset()
		{
			if (_isInitialized)
			{
				_heroController.OnStateChanged -= OnHeroStateChanged;
				_enemiesController.OnEnemyHit -= OnEnemyHit;
				_enemiesController.OnEnemyPositionChanged -= OnEnemyPositionChanged;
				_enemiesController.OnEnemyRemoved -= OnEnemyRemoved;
			}

			_isInitialized = false;
			_states.Clear();
			_enemyHideTimes.Clear();
			_visibleEnemyIds.Clear();
		}

		private void OnHeroStateChanged(HeroState heroState)
		{
			HealthBarId heroId = new HealthBarId(HealthBarOwner.Hero, 0);
			HealthBarState newState = CreateHeroState(heroState, true);
			if (!_states.TryGetValue(heroId, out HealthBarState oldState))
			{
				_states.Add(heroId, newState);
				OnHealthBarAdded?.Invoke(newState);
				return;
			}

			if (oldState.Matches(newState)) return;
			_states[heroId] = newState;
			OnHealthBarChanged?.Invoke(newState);
		}

		private void OnEnemyHit(EnemyHitResult hitResult)
		{
			if (hitResult.IsLethal || !_enemiesController.Enemies.TryGetValue(hitResult.EnemyId, out EnemyState enemy)) return;

			_currentTime = Time.time;
			HealthBarId id = new HealthBarId(HealthBarOwner.Enemy, hitResult.EnemyId);
			HealthBarState newState = new HealthBarState(id, Mathf.Max(0, hitResult.RemainingHealth), enemy.Config.InitialHealth, enemy.Position, true);
			bool isNew = !_states.ContainsKey(id);
			_states[id] = newState;
			_enemyHideTimes[hitResult.EnemyId] = _currentTime + _enemyVisibilityDuration;
			if (isNew)
			{
				_visibleEnemyIds.Add(hitResult.EnemyId);
				OnHealthBarAdded?.Invoke(newState);
			}
			else
			{
				EnsureEnemyVisibleId(hitResult.EnemyId);
				OnHealthBarChanged?.Invoke(newState);
			}
		}

		private void OnEnemyPositionChanged(EnemyState enemyState)
		{
			HealthBarId id = new HealthBarId(HealthBarOwner.Enemy, enemyState.Id);
			if (!_states.TryGetValue(id, out HealthBarState state) || state.WorldPosition == enemyState.Position) return;

			HealthBarState newState = new HealthBarState(id, state.Health, state.MaxHealth, enemyState.Position, state.IsVisible);
			_states[id] = newState;
			if (newState.IsVisible) OnHealthBarChanged?.Invoke(newState);
		}

		private void OnEnemyRemoved(int enemyId)
		{
			HealthBarId id = new HealthBarId(HealthBarOwner.Enemy, enemyId);
			if (_states.Remove(id)) OnHealthBarRemoved?.Invoke(id);
			_enemyHideTimes.Remove(enemyId);
			RemoveVisibleEnemyId(enemyId);
		}

		private void EnsureEnemyVisibleId(int enemyId)
		{
			for (int i = 0; i < _visibleEnemyIds.Count; i++)
			{
				if (_visibleEnemyIds[i] == enemyId) return;
			}

			_visibleEnemyIds.Add(enemyId);
		}

		private void RemoveVisibleEnemyId(int enemyId)
		{
			for (int i = 0; i < _visibleEnemyIds.Count; i++)
			{
				if (_visibleEnemyIds[i] != enemyId) continue;
				int lastIndex = _visibleEnemyIds.Count - 1;
				_visibleEnemyIds[i] = _visibleEnemyIds[lastIndex];
				_visibleEnemyIds.RemoveAt(lastIndex);
				return;
			}
		}

		private static HealthBarState CreateHeroState(HeroState heroState, bool isVisible)
		{
			return new HealthBarState(new HealthBarId(HealthBarOwner.Hero, 0), heroState.Health, HeroConfig.Instance.InitialHealth, heroState.Position, isVisible);
		}
	}
}

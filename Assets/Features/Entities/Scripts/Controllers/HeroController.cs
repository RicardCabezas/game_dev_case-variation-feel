using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Enemies;
using Game.JoystickInput;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	public class HeroController
	{
		// Services
		private EnemiesController _enemiesController;
		private JoystickInputService _joystickInputService;
		private WeaponsService _weaponsService;

		// Internal State
		private CancellationTokenSource _cancellationTokenSource;
		private HeroState _currentState;

		// Public State
		public HeroState CurrentState => _currentState;

		// Events
		public event Action<HeroState> OnStateChanged;

		public UniTask<bool> Initialize(EnemiesController enemiesController, JoystickInputService joystickInputService, WeaponsService weaponsService)
		{
			_enemiesController = enemiesController;
			_joystickInputService = joystickInputService;
			_weaponsService = weaponsService;

			_currentState = new HeroState(Vector3.zero, HeroConfig.Instance.InitialHealth, 0f);
			_cancellationTokenSource = new CancellationTokenSource();

			UpdateLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		public void TakeHit(int damage)
		{
			if (_currentState.IsDead) return;

			int newHealth = Mathf.Max(0, _currentState.Health - damage);
			Debug.Log($"Hero is taking a hit. Health : {_currentState.Health} -> {newHealth}");
			_currentState = new HeroState(_currentState.Position, newHealth, _currentState.LastAttackTime);
			OnStateChanged?.Invoke(_currentState);

			if (_currentState.IsDead)
			{
				Debug.Log("Hero is dead!");
			}
		}

		public void Restart()
		{
			_currentState = new HeroState(Vector3.zero, HeroConfig.Instance.InitialHealth, 0f);
			OnStateChanged?.Invoke(_currentState);
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();

			return UniTask.CompletedTask;
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (!_currentState.IsDead)
				{
					if (_joystickInputService.CurrentState.IsActive) UpdatePosition();
					else AttackClosestEnemy();
				}
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		private void UpdatePosition()
		{
			Vector2 currentMovementInput = _joystickInputService.CurrentState.IsActive ? _joystickInputService.CurrentState.MovementVector : Vector2.zero;
			if (currentMovementInput.sqrMagnitude <= 0.01f) return;

			Vector3 movement = new Vector3(-currentMovementInput.x, 0f, -currentMovementInput.y);
			Vector3 newPosition = _currentState.Position + movement * (HeroConfig.Instance.MoveSpeed * Time.deltaTime);

			_currentState = new HeroState(newPosition, _currentState.Health, _currentState.LastAttackTime);
			OnStateChanged?.Invoke(_currentState);
		}

		private void AttackClosestEnemy()
		{
			if (_weaponsService.CurrentWeapon == null) return;
			if (Time.time - _currentState.LastAttackTime < _weaponsService.CurrentWeapon.Cooldown) return;

			if (TryFindClosestEnemy(out EnemyState closestEnemy))
			{
				_enemiesController.AttackEnemy(closestEnemy, _weaponsService.CurrentWeapon.Damage);
				_currentState = new HeroState(_currentState.Position, _currentState.Health, Time.time);
			}
		}

		private bool TryFindClosestEnemy(out EnemyState closestEnemy)
		{
			closestEnemy = default;
			if (_weaponsService.CurrentWeapon == null) return false;
			float closestDistance = _weaponsService.CurrentWeapon.Range;
			bool found = false;

			foreach (EnemyState enemy in _enemiesController.Enemies.Values)
			{
				float distance = Vector3.Distance(_currentState.Position, enemy.Position);
				if (distance < closestDistance)
				{
					closestDistance = distance;
					closestEnemy = enemy;
					found = true;
				}
			}

			return found;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Threading;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.Entities;
using Game.GamePlay.Enemies;
using Game.GamePlay.Heroes;
using Game.JoystickInput;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Entities
{
    /// <summary>
    /// Owns entity lifecycle, frame order, cross-controller routing, enemy creation, restart,
    /// and presentation interfaces. Wave scheduling belongs to <c>WavesService</c>.
    /// </summary>
    public sealed class EntitiesService : IService
    {
        private HeroController _hero;
        private EnemiesController _enemies;
        private JoystickInputService _joystick;
        private WeaponsService _weapons;
        private CancellationTokenSource _cancellation;
        private UniTask _loop;
        private float _enemySpacing;
        private bool _hasPendingDash;
        private Vector2 _pendingDashDirection;
        /// <summary>Gets read-only hero state and presentation events.</summary>
        public IHeroPresentationSource HeroPresentation { get; private set; }
        /// <summary>Gets read-only enemy state and presentation events.</summary>
        public IEnemiesPresentationSource EnemiesPresentation { get; private set; }

        /// <inheritdoc/>
        public Type[] GetDependencies()
        {
            return new[] { typeof(JoystickInputService), typeof(WeaponsService) };
        }

        /// <inheritdoc/>
        public UniTask<bool> Initialize()
        {
            _joystick = ServicesLocator.Instance.GetService<JoystickInputService>();
            _weapons = ServicesLocator.Instance.GetService<WeaponsService>();
            _joystick.OnSecondaryInputReleased += OnSecondaryInputReleased;
            _hero = new HeroController();
            _enemies = new EnemiesController();
            HeroPresentation = _hero;
            EnemiesPresentation = _enemies;
            _cancellation = new CancellationTokenSource();
            _loop = RunLoop(_cancellation.Token);
            return UniTask.FromResult(true);
        }

        /// <summary>
        /// Restarts entity state and emits normal enemy-removal notifications.
        /// </summary>
        public void RestartGame()
        {
            if (_hero == null)
            {
                return;
            }
            _joystick.DeactivateInput();
            _hasPendingDash = false;
            _enemies.ClearAll(true);
            _hero.Restart();
            _weapons.Restart(Time.time);
        }

        /// <summary>Sets minimum horizontal spacing used by authoritative enemy movement.</summary>
        /// <param name="enemySpacing">World-unit spacing; negative values clamp to zero.</param>
        public void ConfigureEnemySpacing(float enemySpacing)
        {
            _enemySpacing = Mathf.Max(0f, enemySpacing);
        }

        /// <summary>Creates one enemy through authoritative entity ownership.</summary>
        /// <param name="config">Enemy type requested by an external scheduler.</param>
        /// <param name="maximumConcurrentEnemies">Current wave cap; values below one reject creation.</param>
        /// <returns>
        /// <see langword="true"/> after IDs, capacity, placement, state, and spawn notification
        /// commit; otherwise <see langword="false"/> for invalid input, a dead hero, or no capacity.
        /// </returns>
        public bool TrySpawnEnemy(EnemyConfig config, int maximumConcurrentEnemies)
        {
            if (_hero == null || _hero.CurrentState.IsDead)
            {
                return false;
            }

            Vector3 position = _hero.CurrentState.Position;
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            position += new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * config.SpawnRadius;
            position.x = Mathf.Clamp(
                position.x,
                -Constants.World.ArenaLimit,
                Constants.World.ArenaLimit
            );
            position.z = Mathf.Clamp(
                position.z,
                -Constants.World.ArenaLimit,
                Constants.World.ArenaLimit
            );

            return _enemies.TrySpawn(config, position, maximumConcurrentEnemies);
        }

        /// <inheritdoc/>
        public async UniTask Reset()
        {
            if (_cancellation != null)
            {
                _cancellation.Cancel();
                try
                {
                    await _loop;
                }
                catch (OperationCanceledException) { }
                _cancellation.Dispose();
                _cancellation = null;
            }
            _enemies?.ClearAll(true);
            _hero?.ClearPresentationSubscribers();
            _enemies?.ClearPresentationSubscribers();
            if (_joystick != null)
            {
                _joystick.OnSecondaryInputReleased -= OnSecondaryInputReleased;
            }
            _hero = null;
            _enemies = null;
            HeroPresentation = null;
            EnemiesPresentation = null;
            _joystick = null;
            _weapons = null;
        }

        private async UniTask RunLoop(CancellationToken token)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);

            while (!token.IsCancellationRequested)
            {
                float time = Time.time;
                _hero.Tick(
                    _joystick.CurrentState,
                    _weapons.CurrentWeapon,
                    time,
                    Time.deltaTime
                );
                bool dashCommitted = TryResolvePendingDash();
                _weapons.Tick(time, _hero.CurrentState.Position, !_hero.CurrentState.IsDead);

                if (!dashCommitted && _hero.TryCreateAttackRequest(
                        _weapons.CurrentWeapon,
                        _enemies.CurrentStates,
                        time,
                        out HeroAttackRequest heroAttack
                    )
                    && _enemies.TryApplyDamage(heroAttack.EnemyId, heroAttack.Damage, out _)
                )
                {
                    _hero.ConfirmAttack(heroAttack, time);
                    _weapons.RegisterConfirmedAttack();
                }

                IReadOnlyList<EnemyAttackRequest> attacks = _enemies.CollectAttackRequests(
                    _hero.CurrentState,
                    time
                );
                _enemies.Tick(_hero.CurrentState, Time.deltaTime, _enemySpacing);

                foreach (var request in attacks)
                {

                    if (_hero.CurrentState.IsDead)
                    {
                        break;
                    }

                    if (_hero.TakeHit(request.Damage))
                    {
                        _enemies.ConfirmAttack(request.EnemyId, time);
                    }
                }
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        private void OnSecondaryInputReleased(Vector2 direction)
        {
            _pendingDashDirection = direction;
            _hasPendingDash = true;
        }

        private bool TryResolvePendingDash()
        {
            if (!_hasPendingDash)
            {
                return false;
            }

            _hasPendingDash = false;

            if (!_hero.TryCreateDashRequest(
                    _pendingDashDirection,
                    _weapons.CurrentWeapon,
                    out HeroDashRequest dash
                ))
            {
                return false;
            }

            IReadOnlyList<int> hitEnemyIds = _enemies.CollectDashHitEnemyIds(
                dash,
                HeroConfig.Instance.DashHitRadius
            );
            for (var i = 0; i < hitEnemyIds.Count; i++)
            {
                _enemies.TryApplyDamage(hitEnemyIds[i], int.MaxValue, out _);
            }

            _weapons.DestroyEquippedWeapon();
            return true;
        }
    }
}

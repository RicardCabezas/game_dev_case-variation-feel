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
    /// Owns entity lifecycle, frame order, cross-controller routing, spawning, restart, and
    /// presentation interfaces.
    /// </summary>
    public sealed class EntitiesService : IService
    {
        private HeroController _hero;
        private EnemiesController _enemies;
        private JoystickInputService _joystick;
        private WeaponsService _weapons;
        private CancellationTokenSource _cancellation;
        private UniTask _loop;
        private float _nextSpawnTime;
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
            _hero = new HeroController();
            _enemies = new EnemiesController();
            HeroPresentation = _hero;
            EnemiesPresentation = _enemies;
            _nextSpawnTime = Time.time + EnemiesConfig.Instance.SpawnInterval;
            _cancellation = new CancellationTokenSource();
            _loop = RunLoop(_cancellation.Token);
            return UniTask.FromResult(true);
        }

        /// <summary>
        /// Restarts entity state, emits normal removals, and delays next spawn by full configured
        /// interval.
        /// </summary>
        public void RestartGame()
        {
            if (_hero == null)
            {
                return;
            }
            _joystick.DeactivateInput();
            _enemies.ClearAll(true);
            _hero.Restart();
            _nextSpawnTime = Time.time + EnemiesConfig.Instance.SpawnInterval;
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

                if (_hero.TryCreateAttackRequest(
                        _weapons.CurrentWeapon,
                        _enemies.CurrentStates,
                        time,
                        out HeroAttackRequest heroAttack
                    )
                    && _enemies.TryApplyDamage(heroAttack.EnemyId, heroAttack.Damage, out _)
                )
                {
                    _hero.ConfirmAttack(heroAttack, time);
                }

                if (time >= _nextSpawnTime)
                {

                    if (EnemiesConfig.Instance.Enemies.Count > 0 && !_hero.CurrentState.IsDead)
                    {
                        _enemies.TrySpawn(
                            EnemiesConfig.Instance.Enemies[0],
                            _hero.CurrentState.Position,
                            UnityEngine.Random.Range(0f, Mathf.PI * 2f)
                        );
                    }

                    _nextSpawnTime = time + EnemiesConfig.Instance.SpawnInterval;
                }

                IReadOnlyList<EnemyAttackRequest> attacks = _enemies.CollectAttackRequests(
                    _hero.CurrentState,
                    time
                );
                _enemies.Tick(_hero.CurrentState, Time.deltaTime);

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
    }
}

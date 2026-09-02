using System;
using System.Threading;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.Entities;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using UnityEngine;

namespace Game.Waves
{
    /// <summary>
    /// Owns wave-controller ticking, routes its requests through entities, and consumes enemy
    /// lifecycle notifications to advance authoritative wave state.
    /// </summary>
    public sealed class WavesService : IService
    {
        private EntitiesService _entities;
        private IEnemiesPresentationSource _enemies;
        private WaveController _controller;
        private CancellationTokenSource _cancellation;
        private UniTask _loop;

        /// <summary>Gets read-only wave state and presentation events.</summary>
        public IWavesPresentationSource Presentation { get; private set; }

        /// <inheritdoc/>
        public Type[] GetDependencies() => new[] { typeof(EntitiesService) };

        /// <inheritdoc/>
        public UniTask<bool> Initialize()
        {
            WavesConfig config = WavesConfig.Instance;
            _entities = ServicesLocator.Instance.GetService<EntitiesService>();
            _enemies = _entities.EnemiesPresentation;
            _entities.ConfigureEnemySpacing(config.EnemySpacing);
            _controller = new WaveController(config.Waves, Time.time);
            Presentation = _controller;
            _enemies.OnEnemySpawned += OnEnemySpawned;
            _enemies.OnEnemyRemoved += OnEnemyRemoved;
            _cancellation = new CancellationTokenSource();
            _loop = RunLoop(_cancellation.Token);
            return UniTask.FromResult(true);
        }

        /// <summary>
        /// Restarts wave progression at wave zero before entities clear old enemies, so old removal
        /// notifications cannot advance the newly reset run.
        /// </summary>
        public void RestartGame()
        {
            if (_controller == null)
            {
                return;
            }

            _controller.Restart(Time.time);
            _entities.RestartGame();
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

            if (_enemies != null)
            {
                _enemies.OnEnemySpawned -= OnEnemySpawned;
                _enemies.OnEnemyRemoved -= OnEnemyRemoved;
            }

            _controller?.ClearPresentationSubscribers();
            _controller = null;
            Presentation = null;
            _enemies = null;
            _entities = null;
        }

        private async UniTask RunLoop(CancellationToken token)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);

            while (!token.IsCancellationRequested)
            {
                if (!_entities.HeroPresentation.CurrentState.IsDead)
                {
                    var time = Time.time;

                    if (_controller.TryCreateSpawnRequest(time, out WaveSpawnRequest request))
                    {
                        if (!_entities.TrySpawnEnemy(
                                request.Enemy,
                                request.MaximumConcurrentEnemies)
                        )
                        {
                            _controller.RejectSpawn(time);
                        }
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        private void OnEnemySpawned(EnemyState state) => _controller.ConfirmSpawn(state.Id, Time.time);

        private void OnEnemyRemoved(int enemyId) => _controller.RemoveEnemy(enemyId, Time.time);
    }
}

using System;
using System.Threading;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.Entities;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.UI
{
    /// <summary>Adapts entity presentation sources into health-bar commands and owns its tracked loop.</summary>
    public sealed class HealthBarsService : IService
    {
        private HealthBarsCanvasController _controller;
        private IHeroPresentationSource _hero;
        private IEnemiesPresentationSource _enemies;
        private CancellationTokenSource _cancellation;
        private UniTask _loop;
        /// <summary>Gets health-bar presentation state and events.</summary>
        public IHealthBarsPresentationSource Presentation => _controller;

        /// <inheritdoc/>
        public Type[] GetDependencies() => new[] { typeof(EntitiesService) };

        /// <inheritdoc/>
        public UniTask<bool> Initialize()
        {
            EntitiesService entities = ServicesLocator.Instance.GetService<EntitiesService>();
            _hero = entities.HeroPresentation;
            _enemies = entities.EnemiesPresentation;
            _controller = new HealthBarsCanvasController();
            _hero.OnHeroPositionChanged += OnHeroPosition;
            _hero.OnHeroHit += OnHeroHit;
            _hero.OnRestarted += OnRestarted;
            _enemies.OnEnemyHit += OnHit;
            _enemies.OnEnemyPositionChanged += OnPosition;
            _enemies.OnEnemyRemoved += OnRemoved;
            _controller.ApplyHeroState(_hero.CurrentState);
            _cancellation = new CancellationTokenSource();
            _loop = Loop(_cancellation.Token);
            return UniTask.FromResult(true);
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
            }

            if (_hero != null)
            {
                _hero.OnHeroPositionChanged -= OnHeroPosition;
                _hero.OnHeroHit -= OnHeroHit;
                _hero.OnRestarted -= OnRestarted;
            }

            if (_enemies != null)
            {
                _enemies.OnEnemyHit -= OnHit;
                _enemies.OnEnemyPositionChanged -= OnPosition;
                _enemies.OnEnemyRemoved -= OnRemoved;
            }
            _controller?.Clear();
            _controller = null;
            _hero = null;
            _enemies = null;
            _cancellation = null;
        }

        private async UniTask Loop(CancellationToken token)
        {

            while (!token.IsCancellationRequested)
            {
                _controller.Tick(Time.time);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        private void OnHeroPosition(Vector3 position) => _controller.ApplyHeroPosition(position);

        private void OnHeroHit(HeroHitResult hit) => _controller.ApplyHeroHit(hit);

        private void OnRestarted(HeroState state) => _controller.ApplyHeroState(state);

        private void OnHit(EnemyHitResult hit) => _controller.ApplyEnemyHit(hit, Time.time);

        private void OnPosition(EnemyState state) => _controller.ApplyEnemyPosition(state);

        private void OnRemoved(int id) => _controller.RemoveEnemy(id);
    }
}

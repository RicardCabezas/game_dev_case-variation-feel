using System.Collections.Generic;
using Core.ServicesManager;
using Game.Entities;
using Game.GamePlay.Entities;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
    /// <summary>Presentation owner mapping runtime enemy identities to instantiated enemy views.</summary>
    /// <remarks>
    /// Subscribes after service initialization and mirrors controller events; never owns enemy
    /// gameplay state.
    /// </remarks>
    public class EnemiesContainerView : MonoBehaviour
    {
        /// <summary>Current Bee death clip duration in scaled seconds.</summary>
        private const float EnemyDeathAnimationDuration = 1f;

        private IEnemiesPresentationSource _enemiesPresentation;
        private Dictionary<int, EnemyView> _enemyViews;
        private HashSet<int> _deadEnemyIds;

        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            _enemiesPresentation = ServicesLocator
                .Instance.GetService<EntitiesService>()
                .EnemiesPresentation;
            _enemyViews = new Dictionary<int, EnemyView>();
            _deadEnemyIds = new HashSet<int>();

            _enemiesPresentation.OnEnemySpawned += OnEnemySpawned;
            _enemiesPresentation.OnEnemyRemoved += OnEnemyRemoved;
            _enemiesPresentation.OnEnemyPositionChanged += OnEnemyPositionChanged;
            _enemiesPresentation.OnEnemyHit += OnEnemyHit;
            _enemiesPresentation.OnEnemyAttackPerformed += OnEnemyAttackPerformed;

            foreach (var pair in _enemiesPresentation.CurrentStates)
            {
                OnEnemySpawned(pair.Value);
            }
        }

        private void OnEnemySpawned(EnemyState enemyState)
        {
            var enemyView = Instantiate(enemyState.Config.Prefab, transform);
            enemyView.transform.position = enemyState.Position;
            _enemyViews[enemyState.Id] = enemyView;
        }

        private void OnEnemyRemoved(int enemyId)
        {
            var isDead = _deadEnemyIds.Remove(enemyId);

            if (_enemyViews.Remove(enemyId, out var enemyView))
            {
                if (isDead)
                {
                    Destroy(enemyView.gameObject, EnemyDeathAnimationDuration);
                }
                else
                {
                    Destroy(enemyView.gameObject);
                }
            }
        }

        private void OnEnemyPositionChanged(EnemyState enemyState)
        {
            if (_enemyViews.TryGetValue(enemyState.Id, out var enemyView))
            {
                enemyView.SetPosition(enemyState.Position);
            }
        }

        private void OnEnemyHit(EnemyHitResult hitResult)
        {
            if (_enemyViews.TryGetValue(hitResult.EnemyId, out var enemyView))
            {
                if (hitResult.IsLethal)
                {
                    _deadEnemyIds.Add(hitResult.EnemyId);
                    enemyView.PlayDeath();
                }
                else
                {
                    enemyView.PlayDamage();
                }
            }
        }

        private void OnEnemyAttackPerformed(int enemyId)
        {
            if (_enemyViews.TryGetValue(enemyId, out var enemyView))
            {
                enemyView.PlayAttack();
            }
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;

            if (_enemiesPresentation != null)
            {
                _enemiesPresentation.OnEnemySpawned -= OnEnemySpawned;
                _enemiesPresentation.OnEnemyRemoved -= OnEnemyRemoved;
                _enemiesPresentation.OnEnemyPositionChanged -= OnEnemyPositionChanged;
                _enemiesPresentation.OnEnemyHit -= OnEnemyHit;
                _enemiesPresentation.OnEnemyAttackPerformed -= OnEnemyAttackPerformed;
            }
        }
    }
}

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
        private IEnemiesPresentationSource _enemiesPresentation;
        private Dictionary<int, EnemyView> _enemyViews;

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

            _enemiesPresentation.OnEnemySpawned += OnEnemySpawned;
            _enemiesPresentation.OnEnemyRemoved += OnEnemyRemoved;
            _enemiesPresentation.OnEnemyPositionChanged += OnEnemyPositionChanged;
            _enemiesPresentation.OnEnemyHit += OnEnemyHit;
            _enemiesPresentation.OnEnemyAttackPerformed += OnEnemyAttackPerformed;

            foreach (KeyValuePair<int, EnemyState> pair in _enemiesPresentation.CurrentStates)
            {
                OnEnemySpawned(pair.Value);
            }
        }

        private void OnEnemySpawned(EnemyState enemyState)
        {
            EnemyView enemyView = Instantiate(enemyState.Config.Prefab, transform);
            enemyView.transform.position = enemyState.Position;
            _enemyViews[enemyState.Id] = enemyView;
        }

        private void OnEnemyRemoved(int enemyId)
        {
            if (_enemyViews.Remove(enemyId, out EnemyView enemyView))
            {
                Destroy(enemyView.gameObject);
            }
        }

        private void OnEnemyPositionChanged(EnemyState enemyState)
        {
            if (_enemyViews.TryGetValue(enemyState.Id, out EnemyView enemyView))
            {
                enemyView.SetPosition(enemyState.Position);
            }
        }

        private void OnEnemyHit(EnemyHitResult hitResult)
        {
            if (_enemyViews.TryGetValue(hitResult.EnemyId, out EnemyView enemyView))
            {
                enemyView.PlayDamage();
            }
        }

        private void OnEnemyAttackPerformed(int enemyId)
        {
            if (_enemyViews.TryGetValue(enemyId, out EnemyView enemyView))
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

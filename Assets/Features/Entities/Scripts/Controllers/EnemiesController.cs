using System;
using System.Collections.Generic;
using Game.Entities;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
    /// <summary>Owns enemy state and rules. Service owns timing and cross-entity routing.</summary>
    internal sealed class EnemiesController : IEnemiesPresentationSource
    {
        /// <summary>Number of spacing passes per frame.</summary>
        private const int SeparationPasses = 2;

        private readonly Dictionary<int, EnemyState> _enemies = new();
        private readonly List<int> _ids = new();
        private readonly List<EnemyState> _updated = new();
        private readonly List<EnemyAttackRequest> _attacks = new();
        private int _nextEnemyId;

        /// <summary>Gets tracked enemies by runtime ID.</summary>
        public IReadOnlyDictionary<int, EnemyState> CurrentStates => _enemies;
        /// <summary>Raised after an enemy enters tracking.</summary>
        public event Action<EnemyState> OnEnemySpawned;
        /// <summary>Raised after an enemy leaves authoritative tracking.</summary>
        public event Action<int> OnEnemyRemoved;
        /// <summary>Raised after an enemy position changes.</summary>
        public event Action<EnemyState> OnEnemyPositionChanged;
        /// <summary>Raised after accepted damage is applied.</summary>
        public event Action<EnemyHitResult> OnEnemyHit;
        /// <summary>Raised after an enemy attack is confirmed.</summary>
        public event Action<int> OnEnemyAttackPerformed;


        /// <summary>Spawns an enemy when configuration and capacity allow.</summary>
        /// <returns><see langword="true"/> after authoritative state and spawn notification commit.</returns>
        public bool TrySpawn(
            EnemyConfig config,
            Vector3 position,
            int maximumConcurrentEnemies
        )
        {
            if (
                config == null
                || maximumConcurrentEnemies <= 0
                || _enemies.Count >= maximumConcurrentEnemies
            )
            {
                return false;
            }

            var state = new EnemyState(
                _nextEnemyId++,
                position,
                config.InitialHealth,
                config
            );
            _enemies.Add(state.Id, state);
            OnEnemySpawned?.Invoke(state);
            return true;
        }

        /// <summary>Applies damage and returns its self-contained result.</summary>
        public bool TryApplyDamage(int enemyId, int damage, out EnemyHitResult hit)
        {
            hit = default;

            if (!_enemies.TryGetValue(enemyId, out var enemy))
            {
                return false;
            }

            var health = Mathf.Max(0, enemy.Health - damage);
            var lethal = health == 0;
            hit = new EnemyHitResult(
                enemy.Id,
                health,
                enemy.Config.InitialHealth,
                enemy.Position,
                lethal
            );

            if (lethal)
            {
                _enemies.Remove(enemy.Id);
            }
            else
            {
                _enemies[enemy.Id] = new EnemyState(
                    enemy.Id,
                    enemy.Position,
                    health,
                    enemy.Config,
                    enemy.LastAttackTime
                );
            }
            OnEnemyHit?.Invoke(hit);

            if (lethal)
            {
                OnEnemyRemoved?.Invoke(enemy.Id);
            }

            return true;
        }

        /// <summary>Advances enemy movement and spacing for one frame.</summary>
        public void Tick(HeroState hero, float deltaTime, float enemySpacing)
        {
            if (hero.IsDead)
            {
                return;
            }

            _ids.Clear();

            foreach (var id in _enemies.Keys)
            {
                _ids.Add(id);
            }
            _ids.Sort();
            _updated.Clear();

            foreach (var id in _ids)
            {

                if (_enemies.TryGetValue(id, out var enemy))
                {
                    _updated.Add(Move(enemy, hero.Position, deltaTime));
                }
            }

            ResolveSpacing(enemySpacing);

            foreach (var state in _updated)
            {

                if (!_enemies.TryGetValue(state.Id, out var old))
                {
                    continue;
                }
                _enemies[state.Id] = state;

                if (state.Position != old.Position)
                {
                    OnEnemyPositionChanged?.Invoke(state);
                }
            }
        }

        /// <summary>Collects attacks currently eligible against a living hero in stable enemy ID order.</summary>
        public IReadOnlyList<EnemyAttackRequest> CollectAttackRequests(
            HeroState hero,
            float currentTime
        )
        {
            _attacks.Clear();

            if (hero.IsDead)
            {
                return _attacks;
            }

            _ids.Clear();

            foreach (var id in _enemies.Keys)
            {
                _ids.Add(id);
            }

            _ids.Sort();

            foreach (var id in _ids)
            {

                if (!_enemies.TryGetValue(id, out var enemy))
                {
                    continue;
                }

                var delta = hero.Position - enemy.Position;
                delta.y = 0f;

                if (
                    delta.sqrMagnitude <= enemy.Config.AttackRange * enemy.Config.AttackRange
                    && currentTime - enemy.LastAttackTime >= enemy.Config.AttackCooldown
                )
                {
                    _attacks.Add(new EnemyAttackRequest(enemy.Id, enemy.Config.AttackDamage));
                }
            }

            return _attacks;
        }

        /// <summary>Commits an eligible enemy attack at scaled time.</summary>
        public void ConfirmAttack(int enemyId, float currentTime)
        {
            if (!_enemies.TryGetValue(enemyId, out var enemy))
            {
                return;
            }

            _enemies[enemyId] = new EnemyState(
                enemy.Id,
                enemy.Position,
                enemy.Health,
                enemy.Config,
                currentTime
            );
            OnEnemyAttackPerformed?.Invoke(enemyId);
        }

        /// <summary>Removes all enemies and optionally resets ID allocation.</summary>
        public void ClearAll(bool resetIds)
        {
            _ids.Clear();

            foreach (var id in _enemies.Keys)
            {
                _ids.Add(id);
            }

            _ids.Sort();

            for (var i = 0; i < _ids.Count; i++)
            {
                _enemies.Remove(_ids[i]);
                OnEnemyRemoved?.Invoke(_ids[i]);
            }
            _attacks.Clear();
            _updated.Clear();

            if (resetIds)
            {
                _nextEnemyId = 0;
            }
        }

        /// <summary>Clears presentation callbacks during service teardown.</summary>
        internal void ClearPresentationSubscribers()
        {
            OnEnemySpawned = null;
            OnEnemyRemoved = null;
            OnEnemyPositionChanged = null;
            OnEnemyHit = null;
            OnEnemyAttackPerformed = null;
        }

        private static EnemyState Move(EnemyState enemy, Vector3 heroPosition, float deltaTime)
        {
            var delta = heroPosition - enemy.Position;
            delta.y = 0f;

            if (delta.sqrMagnitude <= enemy.Config.AttackRange * enemy.Config.AttackRange)
            {
                return enemy;
            }

            return new EnemyState(
                enemy.Id,
                enemy.Position + delta.normalized * enemy.Config.Speed * deltaTime,
                enemy.Health,
                enemy.Config,
                enemy.LastAttackTime
            );
        }

        private void ResolveSpacing(float space)
        {
            space = Mathf.Max(0f, space);

            if (space <= 0f)
            {
                return;
            }

            var sqr = space * space;

            for (var pass = 0; pass < SeparationPasses; pass++)
            {
                for (var a = 0; a < _updated.Count - 1; a++)
                {
                    for (var b = a + 1; b < _updated.Count; b++)
                    {
                        EnemyState first = _updated[a],
                            second = _updated[b];
                        var difference = first.Position - second.Position;
                        difference.y = 0f;
                        var distanceSqr = difference.sqrMagnitude;

                        if (distanceSqr >= sqr)
                        {
                            continue;
                        }

                        var distance = Mathf.Sqrt(distanceSqr);
                        var direction = distance > 0f ? difference / distance : Vector3.right;
                        var correction = direction * ((space - distance) * .5f);
                        _updated[a] = new EnemyState(
                            first.Id,
                            first.Position + correction,
                            first.Health,
                            first.Config,
                            first.LastAttackTime
                        );
                        _updated[b] = new EnemyState(
                            second.Id,
                            second.Position - correction,
                            second.Health,
                            second.Config,
                            second.LastAttackTime
                        );
                    }
                }
            }
        }
    }
}

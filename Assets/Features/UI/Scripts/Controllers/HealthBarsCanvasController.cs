using System;
using System.Collections.Generic;
using Game.GamePlay.Enemies;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.UI
{
    /// <summary>Owns health-bar presentation state from self-sufficient payloads and supplied time.</summary>
    internal sealed class HealthBarsCanvasController : IHealthBarsPresentationSource
    {
        private readonly Dictionary<HealthBarId, HealthBarState> _states =
            new Dictionary<HealthBarId, HealthBarState>();
        private readonly Dictionary<int, float> _hideTimes = new Dictionary<int, float>();
        private readonly float _duration;
        /// <summary>Gets current health-bar states by owner and ID.</summary>
        public IReadOnlyDictionary<HealthBarId, HealthBarState> CurrentStates => _states;
        /// <summary>Raised when a bar first becomes tracked.</summary>
        public event Action<HealthBarState> OnHealthBarAdded;
        /// <summary>Raised after a tracked bar changes.</summary>
        public event Action<HealthBarState> OnHealthBarChanged;
        /// <summary>Raised after a bar is removed.</summary>
        public event Action<HealthBarId> OnHealthBarRemoved;

        /// <summary>Creates controller with enemy-bar visibility duration.</summary>
        /// <param name="duration">Seconds an enemy bar remains visible after a hit.</param>
        public HealthBarsCanvasController(float duration = 2f)
        {
            _duration = Mathf.Max(0f, duration);
        }

        /// <summary>Replaces hero bar state.</summary>
        public void ApplyHeroState(HeroState hero)
        {
            HealthBarState state = new HealthBarState(
                new HealthBarId(HealthBarOwner.Hero, 0),
                hero.Health,
                HeroConfig.Instance.InitialHealth,
                hero.Position,
                true
            );

            if (_states.ContainsKey(state.Id))
            {
                _states[state.Id] = state;
                OnHealthBarChanged?.Invoke(state);
            }
            else
            {
                _states.Add(state.Id, state);
                OnHealthBarAdded?.Invoke(state);
            }
        }

        /// <summary>Updates hero bar world position.</summary>
        public void ApplyHeroPosition(Vector3 position)
        {
            HealthBarId id = new HealthBarId(HealthBarOwner.Hero, 0);

            if (!_states.TryGetValue(id, out HealthBarState old))
            {
                return;
            }

            HealthBarState state = new HealthBarState(
                id,
                old.Health,
                old.MaxHealth,
                position,
                old.IsVisible
            );
            _states[id] = state;
            OnHealthBarChanged?.Invoke(state);
        }

        /// <summary>Applies accepted hero hit state.</summary>
        public void ApplyHeroHit(HeroHitResult hit)
        {
            HealthBarId id = new HealthBarId(HealthBarOwner.Hero, 0);
            HealthBarState state = new HealthBarState(
                id,
                hit.RemainingHealth,
                HeroConfig.Instance.InitialHealth,
                hit.Position,
                true
            );

            if (_states.ContainsKey(id))
            {
                _states[id] = state;
                OnHealthBarChanged?.Invoke(state);
            }
            else
            {
                _states.Add(id, state);
                OnHealthBarAdded?.Invoke(state);
            }
        }

        /// <summary>Applies nonlethal enemy hit and refreshes visibility timeout.</summary>
        public void ApplyEnemyHit(EnemyHitResult hit, float time)
        {
            if (hit.IsLethal)
            {
                return;
            }

            HealthBarId id = new HealthBarId(HealthBarOwner.Enemy, hit.EnemyId);
            HealthBarState state = new HealthBarState(
                id,
                hit.RemainingHealth,
                hit.MaximumHealth,
                hit.Position,
                true
            );
            bool add = !_states.ContainsKey(id);
            _states[id] = state;
            _hideTimes[hit.EnemyId] = time + _duration;

            if (add)
            {
                OnHealthBarAdded?.Invoke(state);
            }
            else
            {
                OnHealthBarChanged?.Invoke(state);
            }
        }

        /// <summary>Updates tracked enemy bar position.</summary>
        public void ApplyEnemyPosition(EnemyState enemy)
        {
            HealthBarId id = new HealthBarId(HealthBarOwner.Enemy, enemy.Id);

            if (!_states.TryGetValue(id, out HealthBarState old))
            {
                return;
            }

            HealthBarState state = new HealthBarState(
                id,
                old.Health,
                old.MaxHealth,
                enemy.Position,
                old.IsVisible
            );
            _states[id] = state;

            if (state.IsVisible)
            {
                OnHealthBarChanged?.Invoke(state);
            }
        }

        /// <summary>Removes enemy bar and timeout state.</summary>
        public void RemoveEnemy(int id)
        {
            HealthBarId bar = new HealthBarId(HealthBarOwner.Enemy, id);

            if (_states.Remove(bar))
            {
                OnHealthBarRemoved?.Invoke(bar);
            }

            _hideTimes.Remove(id);
        }

        /// <summary>Hides enemy bars whose visibility timeout has elapsed.</summary>
        public void Tick(float time)
        {

            foreach (
                KeyValuePair<int, float> item in new List<KeyValuePair<int, float>>(_hideTimes)
            )
            {

                if (
                    item.Value <= time
                    && _states.TryGetValue(
                        new HealthBarId(HealthBarOwner.Enemy, item.Key),
                        out HealthBarState state
                    )
                    && state.IsVisible
                )
                {
                    state = new HealthBarState(
                        state.Id,
                        state.Health,
                        state.MaxHealth,
                        state.WorldPosition,
                        false
                    );
                    _states[state.Id] = state;
                    OnHealthBarChanged?.Invoke(state);
                    _hideTimes.Remove(item.Key);
                }
            }
        }

        /// <summary>Clears bars and pending visibility timeouts.</summary>
        public void Clear()
        {
            _states.Clear();
            _hideTimes.Clear();
        }
    }
}

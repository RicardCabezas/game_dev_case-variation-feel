using System;
using System.Collections.Generic;
using Game.GamePlay.Enemies;

namespace Game.Waves
{
    /// <summary>
    /// Owns authoritative wave progression, pending spawn order, retry timing, and tracked
    /// wave-enemy membership. Services route its requests and lifecycle notifications.
    /// </summary>
    internal sealed class WaveController : IWavesPresentationSource
    {
        private readonly IReadOnlyList<WaveDefinition> _waves;
        private readonly HashSet<int> _activeEnemyIds = new HashSet<int>();
        private int _waveIndex;
        private int _groupIndex;
        private int _remainingInGroup;
        private int _pendingSpawns;
        private WavePhase _phase;
        private float? _nextSpawnTime;
        private bool _spawnRequestOutstanding;
        private WaveState _currentState;

        /// <summary>Gets current complete wave-run snapshot.</summary>
        public WaveState CurrentState => _currentState;

        /// <summary>Raised after complete wave state replacement.</summary>
        public event Action<WaveState> OnStateChanged;

        /// <summary>Creates controller and starts first valid wave at supplied scaled time.</summary>
        public WaveController(IReadOnlyList<WaveDefinition> waves, float currentTime)
        {
            _waves = waves ?? Array.Empty<WaveDefinition>();
            Restart(currentTime);
        }

        /// <summary>
        /// Returns one due spawn request without creating an enemy. Only one request can remain
        /// outstanding until service confirms or rejects it.
        /// </summary>
        public bool TryCreateSpawnRequest(float currentTime, out WaveSpawnRequest request)
        {
            request = default;

            if (
                _phase != WavePhase.Spawning
                || _spawnRequestOutstanding
                || !_nextSpawnTime.HasValue
                || currentTime < _nextSpawnTime.Value
            )
            {
                return false;
            }

            if (!TryGetCurrentEnemy(out EnemyConfig config))
            {
                TransitionToClearing(currentTime);
                return false;
            }

            _spawnRequestOutstanding = true;
            request = new WaveSpawnRequest(config, CurrentMaximumConcurrentEnemies());
            return true;
        }

        /// <summary>Commits an entity spawn notification for the currently outstanding request.</summary>
        public void ConfirmSpawn(int enemyId, float currentTime)
        {
            if (!_spawnRequestOutstanding || !_activeEnemyIds.Add(enemyId))
            {
                return;
            }

            _spawnRequestOutstanding = false;
            ConsumeCurrentEnemy();

            if (_pendingSpawns == 0)
            {
                TransitionToClearing(currentTime);
                return;
            }

            _nextSpawnTime = currentTime + CurrentSpawnInterval();
            Publish();
        }

        /// <summary>Keeps current spawn pending and schedules its retry after current wave interval.</summary>
        public void RejectSpawn(float currentTime)
        {
            if (!_spawnRequestOutstanding)
            {
                return;
            }

            _spawnRequestOutstanding = false;
            _nextSpawnTime = currentTime + CurrentSpawnInterval();
            Publish();
        }

        /// <summary>Consumes an enemy removal only when it belongs to current wave membership.</summary>
        public void RemoveEnemy(int enemyId, float currentTime)
        {
            if (!_activeEnemyIds.Remove(enemyId))
            {
                return;
            }

            if (_phase == WavePhase.Clearing && _activeEnemyIds.Count == 0)
            {
                StartWaveAtOrAfter(_waveIndex + 1, currentTime);
                return;
            }

            Publish();
        }

        /// <summary>Resets progression to first valid wave and clears tracked runtime enemy IDs.</summary>
        public void Restart(float currentTime)
        {
            _activeEnemyIds.Clear();
            _spawnRequestOutstanding = false;
            StartWaveAtOrAfter(0, currentTime);
        }

        /// <summary>Clears presentation callbacks during owning-service teardown.</summary>
        internal void ClearPresentationSubscribers() => OnStateChanged = null;

        private void StartWaveAtOrAfter(int startIndex, float currentTime)
        {
            for (int index = startIndex; index < _waves.Count; index++)
            {
                WaveDefinition wave = _waves[index];
                int pending = CountValidSpawns(wave);

                if (pending == 0)
                {
                    continue;
                }

                _waveIndex = index;
                _groupIndex = 0;
                _remainingInGroup = 0;
                _pendingSpawns = pending;
                _phase = WavePhase.Spawning;
                _nextSpawnTime = currentTime + ClampSeconds(wave.StartDelay);
                Publish();
                return;
            }

            _waveIndex = _waves.Count == 0 ? -1 : _waves.Count - 1;
            _groupIndex = 0;
            _remainingInGroup = 0;
            _pendingSpawns = 0;
            _phase = WavePhase.Completed;
            _nextSpawnTime = null;
            Publish();
        }

        private bool TryGetCurrentEnemy(out EnemyConfig config)
        {
            config = null;
            WaveDefinition wave = _waves[_waveIndex];
            IReadOnlyList<WaveEnemyGroup> groups = wave.EnemyGroups;

            while (_groupIndex < groups.Count)
            {
                WaveEnemyGroup group = groups[_groupIndex];

                if (group == null || group.Enemy == null || group.Amount <= 0)
                {
                    _groupIndex++;
                    _remainingInGroup = 0;
                    continue;
                }

                if (_remainingInGroup == 0)
                {
                    _remainingInGroup = group.Amount;
                }

                config = group.Enemy;
                return true;
            }

            return false;
        }

        private void ConsumeCurrentEnemy()
        {
            _remainingInGroup--;
            _pendingSpawns--;

            if (_remainingInGroup == 0)
            {
                _groupIndex++;
            }
        }

        private void TransitionToClearing(float currentTime)
        {
            _spawnRequestOutstanding = false;
            _pendingSpawns = 0;
            _nextSpawnTime = null;

            if (_activeEnemyIds.Count == 0)
            {
                StartWaveAtOrAfter(_waveIndex + 1, currentTime);
                return;
            }

            _phase = WavePhase.Clearing;
            Publish();
        }

        private float CurrentSpawnInterval() => ClampSeconds(_waves[_waveIndex].SpawnInterval);

        private int CurrentMaximumConcurrentEnemies() => _waves[_waveIndex].MaximumConcurrentEnemies;

        private void Publish()
        {
            _currentState = new WaveState(
                _waveIndex,
                _waves.Count,
                _phase,
                _pendingSpawns,
                _activeEnemyIds.Count,
                _nextSpawnTime
            );
            OnStateChanged?.Invoke(_currentState);
        }

        private static int CountValidSpawns(WaveDefinition wave)
        {
            if (wave == null || wave.EnemyGroups == null)
            {
                return 0;
            }

            long total = 0;

            foreach (WaveEnemyGroup group in wave.EnemyGroups)
            {
                if (group != null && group.Enemy != null && group.Amount > 0)
                {
                    total += group.Amount;
                }
            }

            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        private static float ClampSeconds(float seconds) => Math.Max(0f, seconds);
    }

    /// <summary>Internal controller request routed by WavesService to authoritative entities.</summary>
    internal readonly struct WaveSpawnRequest
    {
        /// <summary>Gets requested enemy type.</summary>
        public EnemyConfig Enemy { get; }

        /// <summary>Gets current wave concurrent-enemy cap.</summary>
        public int MaximumConcurrentEnemies { get; }

        /// <summary>Creates one routed wave spawn request.</summary>
        public WaveSpawnRequest(EnemyConfig enemy, int maximumConcurrentEnemies)
        {
            Enemy = enemy;
            MaximumConcurrentEnemies = maximumConcurrentEnemies;
        }
    }
}

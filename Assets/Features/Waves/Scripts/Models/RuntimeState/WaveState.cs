namespace Game.Waves
{
    /// <summary>Immutable wave-run snapshot supplied to presentation after each state replacement.</summary>
    public sealed class WaveState
    {
        /// <summary>Gets zero-based active wave index, or -1 when no valid wave exists.</summary>
        public int CurrentWaveIndex { get; }

        /// <summary>Gets total authored wave count, including empty or invalid waves.</summary>
        public int TotalWaves { get; }

        /// <summary>Gets current wave-run phase.</summary>
        public WavePhase Phase { get; }

        /// <summary>Gets enemies still awaiting accepted creation in current wave.</summary>
        public int PendingSpawns { get; }

        /// <summary>Gets current-wave enemies still tracked by entities.</summary>
        public int ActiveEnemies { get; }

        /// <summary>Gets scaled absolute time for next spawn attempt, or null outside spawning.</summary>
        public float? NextSpawnTime { get; }

        /// <summary>Gets whether all authored waves have cleared.</summary>
        public bool IsComplete => Phase == WavePhase.Completed;

        /// <summary>Creates complete presentation snapshot.</summary>
        public WaveState(
            int currentWaveIndex,
            int totalWaves,
            WavePhase phase,
            int pendingSpawns,
            int activeEnemies,
            float? nextSpawnTime
        )
        {
            CurrentWaveIndex = currentWaveIndex;
            TotalWaves = totalWaves;
            Phase = phase;
            PendingSpawns = pendingSpawns;
            ActiveEnemies = activeEnemies;
            NextSpawnTime = nextSpawnTime;
        }
    }
}

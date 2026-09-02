namespace Game.Waves
{
    /// <summary>Current lifecycle phase of an authored wave run.</summary>
    public enum WavePhase
    {
        /// <summary>Wave has pending enemies and may request another spawn when due.</summary>
        Spawning,

        /// <summary>Wave has no pending spawns and waits for its tracked enemies to leave entities.</summary>
        Clearing,

        /// <summary>Every valid authored wave has cleared; no further spawns are requested.</summary>
        Completed,
    }
}

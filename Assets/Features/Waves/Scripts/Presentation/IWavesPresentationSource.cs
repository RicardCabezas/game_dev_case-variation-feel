using System;

namespace Game.Waves
{
    /// <summary>Read-only wave state and notifications available to presentation consumers.</summary>
    public interface IWavesPresentationSource
    {
        /// <summary>Gets current complete wave-run snapshot.</summary>
        WaveState CurrentState { get; }

        /// <summary>Raised after wave state replacement.</summary>
        event Action<WaveState> OnStateChanged;
    }
}

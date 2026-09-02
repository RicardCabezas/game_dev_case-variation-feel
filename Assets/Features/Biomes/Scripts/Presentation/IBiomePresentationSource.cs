using System;

namespace Game.Biomes
{
    /// <summary>Read-only active biome state and change notifications for presentation.</summary>
    public interface IBiomePresentationSource
    {
        /// <summary>Gets current biome snapshot.</summary>
        BiomeState CurrentState { get; }

        /// <summary>Raised after the active biome changes.</summary>
        event Action<BiomeState> OnStateChanged;
    }
}

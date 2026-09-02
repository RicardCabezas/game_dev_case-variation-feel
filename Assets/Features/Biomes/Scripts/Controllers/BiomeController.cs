using System;
using System.Collections.Generic;
using System.Linq;
using Game.Waves;

namespace Game.Biomes
{
    internal sealed class BiomeController : IBiomePresentationSource
    {
        private readonly IReadOnlyList<BiomeDefinition> _definitions;
        private readonly BiomeDefinition _defaultDefinition;
        private BiomeState _currentState;

        public BiomeState CurrentState => _currentState;
        public event Action<BiomeState> OnStateChanged;

        public BiomeController(IReadOnlyList<BiomeDefinition> definitions, BiomeDefinition defaultDefinition)
        {
            _definitions = definitions ?? Array.Empty<BiomeDefinition>();
            _defaultDefinition = defaultDefinition;
        }

        public void Consume(WaveState waveState)
        {
            if (waveState == null || waveState.Phase != WavePhase.Spawning)
            {
                return;
            }

            BiomeDefinition definition = Find(waveState.CurrentWaveIndex);
            if (definition == null || definition.Prefab == null || definition.WaveIndex == _currentState?.WaveIndex)
            {
                return;
            }

            Publish(definition);
        }

        public void Reset()
        {
            if (_defaultDefinition != null && _defaultDefinition.Prefab != null)
            {
                Publish(_defaultDefinition);
            }
        }

        public void ClearPresentationSubscribers() => OnStateChanged = null;

        private BiomeDefinition Find(int waveIndex) =>
            _definitions.FirstOrDefault(definition =>
                definition != null && definition.WaveIndex == waveIndex);

        private void Publish(BiomeDefinition definition)
        {
            _currentState = new BiomeState(definition.WaveIndex, definition.Prefab, definition.Skybox);
            OnStateChanged?.Invoke(_currentState);
        }
    }
}

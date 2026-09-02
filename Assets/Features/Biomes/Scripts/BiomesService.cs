using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.Waves;

namespace Game.Biomes
{
    /// <summary>Maps wave-start snapshots to biome presentation state.</summary>
    public sealed class BiomesService : IService
    {
        private WavesService _waves;
        private BiomeController _controller;

        /// <summary>Gets read-only biome state and presentation events.</summary>
        public IBiomePresentationSource Presentation { get; private set; }

        public Type[] GetDependencies() => new[] { typeof(WavesService) };

        public UniTask<bool> Initialize()
        {
            _waves = ServicesLocator.Instance.GetService<WavesService>();
            BiomeConfig config = BiomeConfig.Instance;
            _controller = new BiomeController(config.Biomes, config.DefaultBiome);
            Presentation = _controller;
            _waves.Presentation.OnStateChanged += OnWaveStateChanged;
            _controller.Consume(_waves.Presentation.CurrentState);
            return UniTask.FromResult(true);
        }

        public void RestartGame() => _controller?.Reset();

        public UniTask Reset()
        {
            if (_waves?.Presentation != null)
            {
                _waves.Presentation.OnStateChanged -= OnWaveStateChanged;
            }

            _controller?.ClearPresentationSubscribers();
            _controller = null;
            Presentation = null;
            _waves = null;
            return default;
        }

        private void OnWaveStateChanged(WaveState state) => _controller?.Consume(state);
    }
}

using Core.ServicesManager;
using Game.Waves;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>Unity UI view that renders read-only wave progress and completion state.</summary>
    public sealed class WaveStateView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text stateText;

        private IWavesPresentationSource _waves;

        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            _waves = ServicesLocator.Instance.GetService<WavesService>().Presentation;
            _waves.OnStateChanged += Render;
            Render(_waves.CurrentState);
        }

        private void Render(WaveState state)
        {
            if (stateText == null)
            {
                return;
            }

            if (state.IsComplete)
            {
                stateText.text = "All waves complete\n0 pending · 0 active";
                return;
            }

            string phase = state.Phase == WavePhase.Spawning ? "Spawning" : "Clear enemies";
            stateText.text =
                $"Wave {state.CurrentWaveIndex + 1}/{state.TotalWaves}\n{phase} · "
                + $"{state.PendingSpawns} pending · {state.ActiveEnemies} active";
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;

            if (_waves != null)
            {
                _waves.OnStateChanged -= Render;
            }
        }
    }
}

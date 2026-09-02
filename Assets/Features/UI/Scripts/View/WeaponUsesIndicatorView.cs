using Core.ServicesManager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class WeaponUsesIndicatorView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Image fill;
        private IWeaponUsesIndicatorPresentationSource _source;

        private void Start() => ServicesLocator.Instance.OnAllServicesInitialized += Initialize;

        private void Initialize()
        {
            _source = ServicesLocator.Instance.GetService<WeaponUsesIndicatorService>().Presentation;
            _source.OnStateChanged += Apply;
            Apply(_source.CurrentState);
        }

        private void Apply(WeaponUsesIndicatorState state)
        {
            if (label != null)
            {
                label.text = state.Label;
            }

            if (fill != null)
            {
                fill.fillAmount = state.Fill;
            }
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= Initialize;
            if (_source != null)
            {
                _source.OnStateChanged -= Apply;
            }
        }
    }
}

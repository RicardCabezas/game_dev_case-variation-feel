using Core.ServicesManager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Displays current weapon uses and switches between equipped and unarmed icons.</summary>
    public sealed class WeaponUsesIndicatorView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite swordIcon;
        [SerializeField] private Sprite emptyHandIcon;
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

            if (icon != null)
            {

                var isUsingSword = state.Maximum > 0;
                icon.sprite = isUsingSword ? swordIcon : emptyHandIcon;
                icon.enabled = icon.sprite != null;

                if (isUsingSword)
                {
                    icon.fillAmount = state.Fill;
                }
                else
                {
                    //Hand must be always visible, but has 0 uses
                    icon.fillAmount = 1f;
                }
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

using Core.ServicesManager;
using Game.Entities;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using UnityEngine;
using UnityEngine.UI;

namespace Game.JoystickInput
{
    /// <summary>Unity UI view that mirrors virtual joystick input and hides it while hero is dead.</summary>
    /// <remarks>
    /// Uses screen-pixel center and normalized movement from <see cref="JoystickState"/>; owns no
    /// input state.
    /// </remarks>
    public class JoystickView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform joystickOuterStick;

        [SerializeField]
        private RectTransform joystickInnerStick;

        [SerializeField]
        private Graphic joystickOuterGraphic;

        [SerializeField]
        private Graphic joystickInnerGraphic;

        private float _containerRadius;
        private IHeroPresentationSource _heroPresentation;
        private JoystickInputService _joystickInputService;
        private bool _isHeroDead;
        private Color _normalOuterTint;
        private Color _normalInnerTint;

        private void Awake()
        {
            if (joystickOuterStick != null)
            {
                _containerRadius = joystickOuterStick.sizeDelta.x * 0.5f;
            }

            joystickOuterGraphic ??= joystickOuterStick.GetComponent<Graphic>();
            joystickInnerGraphic ??= joystickInnerStick.GetComponent<Graphic>();
            _normalOuterTint = joystickOuterGraphic != null ? joystickOuterGraphic.color : Color.white;
            _normalInnerTint = joystickInnerGraphic != null ? joystickInnerGraphic.color : Color.white;

            joystickOuterStick.gameObject.SetActive(false);
        }

        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            _joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
            _joystickInputService.OnStateChanged += OnJoystickStateChanged;

            _heroPresentation = ServicesLocator
                .Instance.GetService<EntitiesService>()
                .HeroPresentation;
            _heroPresentation.OnHeroHit += OnHeroHit;
            _heroPresentation.OnRestarted += OnRestarted;

            OnJoystickStateChanged(_joystickInputService.CurrentState);
            OnHeroStateChanged(_heroPresentation.CurrentState);
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;

            if (_joystickInputService != null)
            {
                _joystickInputService.OnStateChanged -= OnJoystickStateChanged;
            }

            if (_heroPresentation != null)
            {
                _heroPresentation.OnHeroHit -= OnHeroHit;
                _heroPresentation.OnRestarted -= OnRestarted;
            }
        }

        private void OnHeroStateChanged(HeroState heroState)
        {
            _isHeroDead = heroState.IsDead;

            if (_isHeroDead)
            {
                joystickOuterStick.gameObject.SetActive(false);
            }
        }

        private void OnHeroHit(HeroHitResult hit)
        {
            if (hit.IsLethal)
            {
                _isHeroDead = true;
                joystickOuterStick.gameObject.SetActive(false);
            }
        }

        private void OnRestarted(HeroState state) => OnHeroStateChanged(state);

        private void OnJoystickStateChanged(JoystickState state)
        {
            if (_isHeroDead)
            {
                joystickOuterStick.gameObject.SetActive(false);
                return;
            }

            joystickOuterStick.gameObject.SetActive(state.IsActive);

            if (state.IsActive)
            {
                ApplyTint(state.Mode);
                UpdateJoystickVisuals(state);
            }
        }

        private void UpdateJoystickVisuals(JoystickState state)
        {
            joystickOuterStick.position = state.JoystickCenter;

            Vector2 innerStickOffset = state.MovementVector * _containerRadius;
            joystickInnerStick.anchoredPosition = innerStickOffset;
        }

        private void ApplyTint(JoystickInputMode mode)
        {
            Color tint = mode == JoystickInputMode.Secondary
                ? JoystickInputConfig.Instance.SecondaryJoystickTint
                : Color.white;

            if (joystickOuterGraphic != null)
            {
                joystickOuterGraphic.color = mode == JoystickInputMode.Secondary
                    ? tint * _normalOuterTint
                    : _normalOuterTint;
            }

            if (joystickInnerGraphic != null)
            {
                joystickInnerGraphic.color = mode == JoystickInputMode.Secondary
                    ? tint * _normalInnerTint
                    : _normalInnerTint;
            }
        }
    }
}

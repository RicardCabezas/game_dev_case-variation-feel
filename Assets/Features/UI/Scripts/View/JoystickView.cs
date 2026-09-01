using Core.ServicesManager;
using Game.Entities;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using UnityEngine;

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

        private float _containerRadius;
        private IHeroPresentationSource _heroPresentation;
        private JoystickInputService _joystickInputService;
        private bool _isHeroDead;

        private void Awake()
        {

            if (joystickOuterStick != null)
            {
                _containerRadius = joystickOuterStick.sizeDelta.x * 0.5f;
            }

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
                UpdateJoystickVisuals(state);
            }
        }

        private void UpdateJoystickVisuals(JoystickState state)
        {
            joystickOuterStick.position = state.JoystickCenter;

            Vector2 innerStickOffset = state.MovementVector * _containerRadius;
            joystickInnerStick.anchoredPosition = innerStickOffset;
        }
    }
}

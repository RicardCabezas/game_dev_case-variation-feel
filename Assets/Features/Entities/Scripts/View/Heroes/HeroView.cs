using Core.ServicesManager;
using Game.Entities;
using Game.GamePlay.Entities;
using Game.JoystickInput;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
    [RequireComponent(typeof(HitFlashView))]
    /// <summary>Unity presentation for hero transform, movement and attack animation, and weapon visual.</summary>
    /// <remarks>Mirrors controller and service events. Owns instantiated weapon view but no gameplay state.</remarks>
    public class HeroView : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash(Constants.Animator.Hero.Speed);
        private static readonly int AttackHash = Animator.StringToHash(Constants.Animator.Hero.Attack);
        private static readonly int DeathHash = Animator.StringToHash(Constants.Animator.Hero.Death);

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private HitFlashView hitFlashView;

        [SerializeField]
        private float rotationSpeed = 10f;

        [SerializeField]
        private Transform weaponSlot;

        [SerializeField]
        [Tooltip("Presentation-only trail rendered across committed dash path")]
        private TrailRenderer dashTrail;

        private JoystickInputService _joystickInputService;
        private IHeroPresentationSource _heroPresentation;
        private WeaponsService _weaponsService;
        private Vector2 _currentMovementInput;
        private WeaponView _currentWeaponView;

        private void Awake()
        {
            if (hitFlashView == null)
            {
                hitFlashView = GetComponent<HitFlashView>();
            }
        }

        private void Start()
        {
            ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
        }

        private void OnServicesInitialized()
        {
            _joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
            _heroPresentation = ServicesLocator
                .Instance.GetService<EntitiesService>()
                .HeroPresentation;
            _weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();

            _joystickInputService.OnStateChanged += OnJoystickStateChanged;
            _heroPresentation.OnHeroPositionChanged += OnHeroPositionChanged;
            _heroPresentation.OnRestarted += OnRestarted;
            _heroPresentation.OnHeroHit += OnHeroHit;
            _heroPresentation.OnAttackPerformed += OnAttackPerformed;
            _heroPresentation.OnDashPerformed += OnDashPerformed;
            _weaponsService.OnWeaponChanged += OnWeaponChanged;

            OnJoystickStateChanged(_joystickInputService.CurrentState);
            OnHeroPositionChanged(_heroPresentation.CurrentState.Position);
            SpawnCurrentWeapon();
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
                _heroPresentation.OnHeroPositionChanged -= OnHeroPositionChanged;
                _heroPresentation.OnRestarted -= OnRestarted;
                _heroPresentation.OnHeroHit -= OnHeroHit;
                _heroPresentation.OnAttackPerformed -= OnAttackPerformed;
                _heroPresentation.OnDashPerformed -= OnDashPerformed;
            }

            if (_weaponsService != null)
            {
                _weaponsService.OnWeaponChanged -= OnWeaponChanged;
            }

            if (_currentWeaponView != null)
            {
                Destroy(_currentWeaponView.gameObject);
            }
        }

        private void OnJoystickStateChanged(JoystickState state)
        {
            _currentMovementInput = state.IsActive && state.Mode == JoystickInputMode.Normal
                ? state.MovementVector
                : Vector2.zero;
            UpdateAnimator();
        }

        private void OnHeroPositionChanged(Vector3 position)
        {
            transform.position = position;
        }

        private void OnRestarted(HeroState state)
        {
            OnHeroPositionChanged(state.Position);

            if (animator == null)
            {
                return;
            }

            animator.SetBool(DeathHash, false);
            animator.SetFloat(SpeedHash, 0f);
        }

        private void OnHeroHit(HeroHitResult hitResult)
        {
            hitFlashView?.Play();

            if (hitResult.IsLethal)
            {
                if (animator != null)
                {
                    animator.SetFloat(SpeedHash, 0f);
                    animator.SetBool(DeathHash, true);
                }
            }
        }

        private void OnAttackPerformed(Vector3 targetPosition)
        {
            Vector3 targetDirection = targetPosition - transform.position;
            targetDirection.y = 0f;

            if (targetDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(-targetDirection);
            }

            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }
        }

        private void OnDashPerformed(HeroDashRequest dash)
        {
            if (dash.Direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(-dash.Direction);
            }

            if (dashTrail != null)
            {
                dashTrail.Clear();
                dashTrail.emitting = true;
                dashTrail.AddPosition(dash.StartPosition);
                dashTrail.AddPosition(dash.EndPosition);
                dashTrail.emitting = false;
            }

            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }
        }

        private void Update()
        {
            if (_heroPresentation == null || _heroPresentation.CurrentState.IsDead)
            {
                return;
            }

            if (_currentMovementInput.sqrMagnitude <= 0.01f)
            {
                return;
            }

            Vector3 movement = new Vector3(-_currentMovementInput.x, 0f, -_currentMovementInput.y);
            Quaternion targetRotation = Quaternion.LookRotation(-movement);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        private void UpdateAnimator()
        {
            if (animator == null)
            {
                return;
            }

            if (_heroPresentation is { CurrentState: { IsDead: true } })
            {
                animator.SetFloat(SpeedHash, 0f);
                return;
            }

            var speed = _currentMovementInput.magnitude;
            animator.SetFloat(SpeedHash, speed);
        }

        private void OnWeaponChanged(WeaponConfig newWeapon)
        {
            if (_currentWeaponView != null)
            {
                Destroy(_currentWeaponView.gameObject);
                _currentWeaponView = null;
            }

            SpawnCurrentWeapon();
        }

        private void SpawnCurrentWeapon()
        {
            if (_weaponsService.CurrentWeapon == null)
            {
                return;
            }

            Transform parent = weaponSlot != null ? weaponSlot : transform;
            _currentWeaponView = Instantiate(_weaponsService.CurrentWeapon.Prefab, parent);
            _currentWeaponView.transform.localPosition = Vector3.zero;
            _currentWeaponView.transform.localRotation = Quaternion.identity;
        }
    }
}

using Game.GamePlay.Entities;
using Game.JoystickInput;
using Game.Weapons;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	/// <summary>Unity presentation for hero transform, movement and attack animation, and weapon visual.</summary>
	/// <remarks>Mirrors controller and service events. Owns instantiated weapon view but no gameplay state.</remarks>
	public class HeroView : MonoBehaviour
	{
		private static readonly int SpeedHash = Animator.StringToHash("Speed");
		private static readonly int AttackHash = Animator.StringToHash("Attack");

		[SerializeField] private Animator animator;
		[SerializeField] private float rotationSpeed = 10f;
		[SerializeField] private Transform weaponSlot;

		private JoystickInputService _joystickInputService;
		private HeroController _heroController;
		private WeaponsService _weaponsService;
		private Vector2 _currentMovementInput;
		private WeaponView _currentWeaponView;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
			_weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();

			_joystickInputService.OnStateChanged += OnJoystickStateChanged;
			_heroController.OnStateChanged += OnHeroStateChanged;
			_heroController.OnAttackPerformed += OnAttackPerformed;
			_weaponsService.OnWeaponChanged += OnWeaponChanged;

			OnJoystickStateChanged(_joystickInputService.CurrentState);
			OnHeroStateChanged(_heroController.CurrentState);
			SpawnCurrentWeapon();
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_joystickInputService != null)
			{
				_joystickInputService.OnStateChanged -= OnJoystickStateChanged;
			}
			if (_heroController != null)
			{
				_heroController.OnStateChanged -= OnHeroStateChanged;
				_heroController.OnAttackPerformed -= OnAttackPerformed;
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
			_currentMovementInput = state.IsActive ? state.MovementVector : Vector2.zero;
			UpdateAnimator();
		}

		private void OnHeroStateChanged(HeroState heroState)
		{
			transform.position = heroState.Position;
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

		private void Update()
		{
			if (_heroController == null || _heroController.CurrentState.IsDead) return;
			if (_currentMovementInput.sqrMagnitude <= 0.01f) return;

			Vector3 movement = new Vector3(-_currentMovementInput.x, 0f, -_currentMovementInput.y);
			Quaternion targetRotation = Quaternion.LookRotation(-movement);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
		}

		private void UpdateAnimator()
		{
			if (animator == null) return;
			if (_heroController is { CurrentState: { IsDead: true } })
			{
				animator.SetFloat(SpeedHash, 0f);
				return;
			}

			float speed = _currentMovementInput.magnitude;
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
			if (_weaponsService.CurrentWeapon == null) return;

			Transform parent = weaponSlot != null ? weaponSlot : transform;
			_currentWeaponView = Instantiate(_weaponsService.CurrentWeapon.Prefab, parent);
			_currentWeaponView.transform.localPosition = Vector3.zero;
			_currentWeaponView.transform.localRotation = Quaternion.identity;
		}
	}
}

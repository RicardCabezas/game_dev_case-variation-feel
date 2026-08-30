using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	/// <summary>Unity presentation for one enemy: position, facing, animation, and nonlethal hit flash.</summary>
	/// <remarks>Container owns this object's lifetime. Public methods are event-driven presentation commands.</remarks>
	public class EnemyView : MonoBehaviour
	{
		private static readonly int IsMovingHash = Animator.StringToHash(Constants.Animator.Bee.IsMoving);
		private static readonly int DamageHash = Animator.StringToHash(Constants.Animator.Bee.Damage);
		private static readonly int AttackHash = Animator.StringToHash(Constants.Animator.Bee.Attack);

		[SerializeField] private Animator animator;
		[SerializeField] private HitFlashView hitFlash;
		[SerializeField] private Color hitFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
		[SerializeField] private float hitFlashDuration = 0.1f;
		[SerializeField] private float rotationSpeed = 10f;

		private HeroController _heroController;
		private HitFlashView _hitFlashView;

		private void Awake()
		{
			_hitFlashView = hitFlash != null ? hitFlash : GetComponent<HitFlashView>();
			if (_hitFlashView == null) _hitFlashView = gameObject.AddComponent<HitFlashView>();
			_hitFlashView.Configure(hitFlashColor, hitFlashDuration);
		}

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
		}

		private void Update()
		{
			if (_heroController == null || _heroController.CurrentState.IsDead) return;

			Vector3 heroPosition = _heroController.CurrentState.Position;
			Vector3 direction = (heroPosition - transform.position).normalized;

			if (direction.sqrMagnitude > 0.01f)
			{
				Quaternion targetRotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
		}

		/// <summary>Moves presentation to authoritative enemy world position and marks movement animation.</summary>
		/// <param name="position">World-space position from <see cref="EnemyState"/>.</param>
		public void SetPosition(Vector3 position)
		{
			transform.position = position;
			animator?.SetBool(IsMovingHash, true);
		}

		/// <summary>Plays nonlethal damage animation and temporary material flash.</summary>
		public void PlayDamage()
		{
			animator?.SetTrigger(DamageHash);
			_hitFlashView?.Play();
		}

		/// <summary>Plays attack presentation for controller-reported enemy attack.</summary>
		public void PlayAttack()
		{
			if (animator == null) return;
			animator.SetBool(IsMovingHash, false);
			animator.SetTrigger(AttackHash);
		}
		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}
	}
}

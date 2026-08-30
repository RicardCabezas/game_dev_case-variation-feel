using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemyView : MonoBehaviour
	{
		private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

		[SerializeField] private Animator animator;
		[SerializeField] private float rotationSpeed = 10f;

		private HeroController _heroController;
		private Vector3 _previousPosition;
		private bool _isMoving;

		private void Start()
		{
			_previousPosition = transform.position;
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
		}

		private void Update()
		{
			UpdateMovementAnimator();

			if (_heroController == null || _heroController.CurrentState.IsDead) return;

			Vector3 heroPosition = _heroController.CurrentState.Position;
			Vector3 direction = (heroPosition - transform.position).normalized;

			if (direction.sqrMagnitude > 0.01f)
			{
				Quaternion targetRotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
		}

		private void UpdateMovementAnimator()
		{
			bool isMoving = (transform.position - _previousPosition).sqrMagnitude > 0.000001f;
			_previousPosition = transform.position;

			if (isMoving == _isMoving || animator == null) return;

			_isMoving = isMoving;
			animator.SetBool(IsMovingHash, _isMoving);
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}
	}
}

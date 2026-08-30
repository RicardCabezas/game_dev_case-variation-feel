using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemyView : MonoBehaviour
	{
		private static readonly int IsMovingHash = Animator.StringToHash(Constants.Animator.Bee.IsMoving);
		private static readonly int DamageHash = Animator.StringToHash(Constants.Animator.Bee.Damage);
		private static readonly int AttackHash = Animator.StringToHash(Constants.Animator.Bee.Attack);
		private static readonly int BaseColorHash = Shader.PropertyToID("_BaseColor");

		[SerializeField] private Animator animator;
		[SerializeField] private SkinnedMeshRenderer meshRenderer;
		[SerializeField] private Color hitFlashColor = Color.white;
		[SerializeField] private float hitFlashDuration = 0.1f;
		[SerializeField] private float rotationSpeed = 10f;

		private HeroController _heroController;
		private MaterialPropertyBlock _materialPropertyBlock;
		private float _hitFlashEndTime;
		private bool _isHitFlashActive;

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
			UpdateHitFlash();
			if (_heroController == null || _heroController.CurrentState.IsDead) return;

			Vector3 heroPosition = _heroController.CurrentState.Position;
			Vector3 direction = (heroPosition - transform.position).normalized;

			if (direction.sqrMagnitude > 0.01f)
			{
				Quaternion targetRotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
		}

		public void SetPosition(Vector3 position)
		{
			transform.position = position;
			animator?.SetBool(IsMovingHash, true);
		}

		public void PlayDamage()
		{
			animator?.SetTrigger(DamageHash);
			if (meshRenderer == null) return;

			_materialPropertyBlock ??= new MaterialPropertyBlock();
			_materialPropertyBlock.SetColor(BaseColorHash, hitFlashColor);
			meshRenderer.SetPropertyBlock(_materialPropertyBlock);
			_hitFlashEndTime = Time.time + hitFlashDuration;
			_isHitFlashActive = true;
		}

		public void PlayAttack()
		{
			if (animator == null) return;
			animator.SetBool(IsMovingHash, false);
			animator.SetTrigger(AttackHash);
		}
		private void UpdateHitFlash()
		{
			if (!_isHitFlashActive || Time.time < _hitFlashEndTime || meshRenderer == null) return;

			meshRenderer.SetPropertyBlock(null);
			_isHitFlashActive = false;
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}
	}
}

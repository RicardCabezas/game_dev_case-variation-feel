using Core.ServicesManager;
using Game.Entities;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
    [RequireComponent(typeof(HitFlashView))]
    /// <summary>Unity presentation for one enemy: position, facing, animation, and hit flash.</summary>
    /// <remarks>Container owns this object's lifetime. Public methods are event-driven presentation commands.</remarks>
    public class EnemyView : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash(
            Constants.Animator.Bee.IsMoving
        );
        private static readonly int DamageHash = Animator.StringToHash(
            Constants.Animator.Bee.Damage
        );
        private static readonly int AttackHash = Animator.StringToHash(
            Constants.Animator.Bee.Attack
        );
        private static readonly int DeathHash = Animator.StringToHash(
            Constants.Animator.Bee.Death
        );

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private HitFlashView hitFlashView;

        [SerializeField]
        private float rotationSpeed = 10f;

        private IHeroPresentationSource _heroPresentation;
        private bool _isDying;

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
            _heroPresentation = ServicesLocator
                .Instance.GetService<EntitiesService>()
                .HeroPresentation;
        }

        private void Update()
        {
            if (_isDying || _heroPresentation == null || _heroPresentation.CurrentState.IsDead)
            {
                return;
            }

            Vector3 heroPosition = _heroPresentation.CurrentState.Position;
            Vector3 direction = (heroPosition - transform.position).normalized;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        /// <summary>Moves presentation to authoritative enemy world position and marks movement animation.</summary>
        /// <param name="position">World-space position from <see cref="EnemyState"/>.</param>
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
            animator?.SetBool(IsMovingHash, true);
        }

        /// <summary>Plays damage animation and temporary material flash for any accepted hit.</summary>
        public void PlayDamage()
        {
            animator?.SetTrigger(DamageHash);
            hitFlashView?.Play();
        }

        /// <summary>Plays lethal-hit presentation and freezes enemy-facing updates.</summary>
        public void PlayDeath()
        {
            _isDying = true;
            hitFlashView?.Play();

            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsMovingHash, false);
            animator.ResetTrigger(DamageHash);
            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(DeathHash);
        }

        /// <summary>Plays attack presentation for controller-reported enemy attack.</summary>
        public void PlayAttack()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsMovingHash, false);
            animator.SetTrigger(AttackHash);
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
        }
    }
}

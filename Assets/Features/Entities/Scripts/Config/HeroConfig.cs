using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
    [CreateAssetMenu(fileName = "HeroConfig", menuName = "Game/HeroConfig")]
    /// <summary>Resources-backed initial hero state and presentation configuration.</summary>
    public class HeroConfig : ScriptableObjectSingleton<HeroConfig>
    {
        [SerializeField]
        [Tooltip("The hero prefab to instantiate")]
        private HeroView heroPrefab;

        [SerializeField]
        [Tooltip("Movement speed in units per second")]
        private float moveSpeed = 5f;

        [SerializeField]
        [Tooltip("Initial health of the hero")]
        private int initialHealth = 100;

        [SerializeField]
        [Tooltip("World-unit distance travelled by one valid consumable dash")]
        private float dashDistance = 4f;

        [SerializeField]
        [Tooltip("World-unit radius around dash path that damages enemies")]
        private float dashHitRadius = 1f;

        /// <summary>Gets hero presentation prefab instantiated by <see cref="HeroContainerView"/>.</summary>
        public HeroView HeroPrefab => heroPrefab;

        /// <summary>Gets hero movement speed in world units per second.</summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>Gets health assigned on controller initialization and restart.</summary>
        public int InitialHealth => initialHealth;

        /// <summary>Gets world-unit distance travelled by one valid consumable dash.</summary>
        public float DashDistance => Mathf.Max(0f, dashDistance);

        /// <summary>Gets world-unit radius of dash path enemy hits.</summary>
        public float DashHitRadius => Mathf.Max(0f, dashHitRadius);
    }
}

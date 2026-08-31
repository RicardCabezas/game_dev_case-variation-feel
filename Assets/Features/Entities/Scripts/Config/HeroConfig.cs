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

		/// <summary>Gets hero presentation prefab instantiated by <see cref="HeroContainerView"/>.</summary>
		public HeroView HeroPrefab => heroPrefab;
		/// <summary>Gets hero movement speed in world units per second.</summary>
		public float MoveSpeed => moveSpeed;
		/// <summary>Gets health assigned on controller initialization and restart.</summary>
		public int InitialHealth => initialHealth;
	}
}

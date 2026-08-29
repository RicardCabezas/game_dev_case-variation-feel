using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	[CreateAssetMenu(fileName = "HeroConfig", menuName = "Game/HeroConfig")]
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

		public HeroView HeroPrefab => heroPrefab;
		public float MoveSpeed => moveSpeed;
		public int InitialHealth => initialHealth;
	}
}

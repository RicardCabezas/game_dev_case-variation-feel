using UnityEngine;

namespace Game.Weapons
{
	[CreateAssetMenu(fileName = "WeaponX", menuName = "Content/Weapon")]
	public class WeaponConfig : ScriptableObject
	{
		[SerializeField] private string id;
		[SerializeField] private int damage;
		[SerializeField] private float range;
		[SerializeField] private float cooldown;
		[SerializeField] private WeaponView prefab;

		public string Id => id;
		public int Damage => damage;
		public float Range => range;
		public float Cooldown => cooldown;
		public WeaponView Prefab => prefab;
	}
}
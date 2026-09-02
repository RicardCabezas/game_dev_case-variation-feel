using UnityEngine;

namespace Game.Weapons
{
    public readonly struct EquippedWeaponState
    {
        public WeaponConfig Weapon { get; }
        public int ConsumedUses { get; }
        public int MaxUses => Weapon == null ? 0 : Weapon.MaxUses;
        public int RemainingUses => Mathf.Max(0, MaxUses - ConsumedUses);
        public bool IsArmed => Weapon != null && RemainingUses > 0;

        public EquippedWeaponState(WeaponConfig weapon, int consumedUses)
        {
            Weapon = weapon;
            ConsumedUses = weapon == null ? 0 : Mathf.Clamp(consumedUses, 0, weapon.MaxUses);
        }

        public static EquippedWeaponState Unarmed => new EquippedWeaponState(null, 0);
    }

    public readonly struct SpawnedWeaponState
    {
        public int Id { get; }
        public WeaponConfig Weapon { get; }
        public Vector3 Position { get; }

        public SpawnedWeaponState(int id, WeaponConfig weapon, Vector3 position)
        {
            Id = id;
            Weapon = weapon;
            Position = position;
        }
    }
}

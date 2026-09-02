using System;
using UnityEngine;

namespace Game.Weapons
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public sealed class WeaponPickupView : MonoBehaviour
    {
        private int _id;
        private bool _requested;
        public event Action<int> OnPickupRequested;

        public void Initialize(SpawnedWeaponState state)
        {
            _id = state.Id;
            _requested = false;
            transform.position = new Vector3(state.Position.x, transform.position.y, state.Position.z);

            WeaponView prefab = state.Weapon == null ? null : state.Weapon.Prefab;
            if (prefab == null)
            {
                return;
            }

            Instantiate(prefab, transform);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_requested)
            {
                return;
            }
            MonoBehaviour hero = other.GetComponentInParent<MonoBehaviour>();
            if (hero != null && hero.GetType().Name == "HeroView" && hero.enabled)
            {
                _requested = true;
                OnPickupRequested?.Invoke(_id);
            }
        }
    }
}

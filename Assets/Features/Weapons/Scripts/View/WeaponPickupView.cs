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
            transform.position = state.Position;
            WeaponView prefab = state.Weapon == null ? null : state.Weapon.Prefab;
            if (prefab == null)
            {
                return;
            }
            WeaponView visual = Instantiate(prefab, transform);
            visual.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
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

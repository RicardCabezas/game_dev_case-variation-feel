using System;
using UnityEngine;

namespace Game.Weapons
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public sealed class WeaponPickupView : MonoBehaviour
    {
        [SerializeField] private Renderer quadRenderer;
        [SerializeField, Min(0f)] private float rotationDegreesPerSecond = 30f;
        [SerializeField, Min(0f)] private float levitationAmplitude = 0.1f;
        [SerializeField, Min(0f)] private float levitationFrequency = 1f;

        private int _id;
        private bool _requested;
        private Vector3 _basePosition;
        private MaterialPropertyBlock _materialPropertyBlock;
        public event Action<int> OnPickupRequested;

        public void Initialize(SpawnedWeaponState state)
        {
            _id = state.Id;
            _requested = false;
            transform.position = new Vector3(state.Position.x, transform.position.y, state.Position.z);
            _basePosition = transform.position;

            ApplyPickupColor(state.Weapon);

            WeaponView prefab = state.Weapon == null ? null : state.Weapon.Prefab;
            if (prefab == null)
            {
                return;
            }

            Instantiate(prefab, transform);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationDegreesPerSecond * Time.deltaTime, Space.World);
            float offsetY = Mathf.Sin(Time.time * levitationFrequency * Mathf.PI * 2f) * levitationAmplitude;
            transform.position = _basePosition + Vector3.up * offsetY;
        }

        private void ApplyPickupColor(WeaponConfig weapon)
        {
            if (quadRenderer == null || weapon == null)
            {
                return;
            }

            _materialPropertyBlock ??= new MaterialPropertyBlock();
            quadRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor("_BaseColor", weapon.PickupColor);
            quadRenderer.SetPropertyBlock(_materialPropertyBlock);
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

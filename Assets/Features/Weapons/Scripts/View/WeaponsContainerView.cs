using System.Collections.Generic;
using Core.ServicesManager;
using UnityEngine;

namespace Game.Weapons
{
    public sealed class WeaponsContainerView : MonoBehaviour
    {
        private WeaponsService _service;
        private readonly Dictionary<int, WeaponPickupView> _views = new Dictionary<int, WeaponPickupView>();

        private void Start() => ServicesLocator.Instance.OnAllServicesInitialized += Initialize;

        private void Initialize()
        {
            _service = ServicesLocator.Instance.GetService<WeaponsService>();
            _service.OnWeaponSpawned += Spawn;
            _service.OnWeaponRemoved += Remove;
        }

        private void Spawn(SpawnedWeaponState state)
        {
            WeaponPickupView prefab = WeaponsConfig.Instance.PickupPrefab;
            if (prefab == null)
            {
                return;
            }
            WeaponPickupView view = Instantiate(prefab, transform);
            view.Initialize(state);
            view.OnPickupRequested += OnPickup;
            _views[state.Id] = view;
        }

        private void OnPickup(int id) => _service.TryPickup(id);

        private void Remove(int id)
        {
            if (_views.Remove(id, out WeaponPickupView view))
            {
                Destroy(view.gameObject);
            }
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= Initialize;
            if (_service == null)
            {
                return;
            }
            _service.OnWeaponSpawned -= Spawn;
            _service.OnWeaponRemoved -= Remove;
        }
    }
}

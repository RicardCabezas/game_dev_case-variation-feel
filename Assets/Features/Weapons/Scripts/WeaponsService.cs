using System;
using System.Collections.Generic;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Entities;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>Authoritative runtime owner for equipped weapons, pickups, and durability.</summary>
    public class WeaponsService : IService
    {
        private readonly Dictionary<int, SpawnedWeaponState> _spawned =
            new Dictionary<int, SpawnedWeaponState>();
        private EquippedWeaponState _state = EquippedWeaponState.Unarmed;
        private float _nextSpawn;
        private int _nextId;
        public Type[] GetDependencies() => null;
        public WeaponConfig CurrentWeapon => _state.IsArmed ? _state.Weapon : null;
        public EquippedWeaponState CurrentWeaponState => _state;
        public IReadOnlyDictionary<int, SpawnedWeaponState> SpawnedWeapons => _spawned;
        public event Action<WeaponConfig> OnWeaponChanged;
        public event Action<EquippedWeaponState> OnWeaponStateChanged;
        public event Action<SpawnedWeaponState> OnWeaponSpawned;
        public event Action<int> OnWeaponRemoved;

        public UniTask<bool> Initialize()
        {
            _spawned.Clear();
            _nextId = 0;
            _nextSpawn = Time.time;
            _state = EquippedWeaponState.Unarmed;
            
            return UniTask.FromResult(true);
        }

        public UniTask Reset()
        {
            Clear();
            Set(EquippedWeaponState.Unarmed, true);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Schedules eligible weapon pickups while the hero is alive and keeps their X/Z positions inside arena bounds.
        /// </summary>
        public void Tick(float time, Vector3 center, bool alive)
        {
            if (!alive || time < _nextSpawn)
                return;
            WeaponsConfig config = WeaponsConfig.Instance;
            _nextSpawn = time + config.SpawnInterval;
            if (_spawned.Count >= config.MaxSpawnedWeapons)
                return;
            IReadOnlyList<WeaponConfig> list = config.Weapons;
            float total = 0f;
            foreach (WeaponConfig weapon in list)
            {
                if (weapon != null && weapon.MaxUses > 0 && weapon.SpawnChance > 0f)
                    total += weapon.SpawnChance;
            }
            if (total <= 0f)
                return;
            float roll = UnityEngine.Random.value * total;
            WeaponConfig chosen = null;
            foreach (WeaponConfig weapon in list)
            {
                if (weapon == null || weapon.MaxUses <= 0 || weapon.SpawnChance <= 0f)
                    continue;
                roll -= weapon.SpawnChance;
                if (roll <= 0f)
                {
                    chosen = weapon;
                    break;
                }
            }
            if (chosen == null)
                return;
            float min = config.MinSpawnRadius;
            float max = config.MaxSpawnRadius;
            float radius = Mathf.Sqrt(Mathf.Lerp(min * min, max * max, UnityEngine.Random.value));
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 position = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            position.x = Mathf.Clamp(position.x, -Constants.World.ArenaLimit, Constants.World.ArenaLimit);
            position.z = Mathf.Clamp(position.z, -Constants.World.ArenaLimit, Constants.World.ArenaLimit);
            SpawnedWeaponState state = new SpawnedWeaponState(
                _nextId++,
                chosen,
                position
            );
            _spawned.Add(state.Id, state);
            OnWeaponSpawned?.Invoke(state);
        }

        public bool TryPickup(int id)
        {
            if (!_spawned.Remove(id, out SpawnedWeaponState pickup))
                return false;
            OnWeaponRemoved?.Invoke(id);
            Equip(pickup.Weapon);
            return true;
        }
        public bool RegisterConfirmedAttack()
        {
            if (!_state.IsArmed)
                return false;
            int consumed = _state.ConsumedUses + 1;
            if (consumed >= _state.MaxUses)
            {
                Set(EquippedWeaponState.Unarmed, true);
            }
            else
            {
                Set(new EquippedWeaponState(_state.Weapon, consumed), false);
            }

            return true;
        }

        /// <summary>Consumes equipped weapon immediately after a valid dash.</summary>
        public void DestroyEquippedWeapon()
        {
            if (_state.IsArmed)
            {
                Set(EquippedWeaponState.Unarmed, true);
            }
        }

        public void Restart(float time)
        {
            Clear();
            _nextId = 0;
            _nextSpawn = time;
            Set(EquippedWeaponState.Unarmed, true);
        }

        private void Equip(WeaponConfig weapon)
        {
            EquippedWeaponState state = weapon == null || weapon.MaxUses <= 0
                ? EquippedWeaponState.Unarmed
                : new EquippedWeaponState(weapon, 0);
            Set(state, true);
        }

        private void Set(EquippedWeaponState state, bool changed)
        {
            _state = state;
            if (changed)
                OnWeaponChanged?.Invoke(CurrentWeapon);
            OnWeaponStateChanged?.Invoke(_state);
        }

        private void Clear()
        {
            List<int> ids = new List<int>(_spawned.Keys);
            _spawned.Clear();
            foreach (int id in ids)
                OnWeaponRemoved?.Invoke(id);
        }
    }
}

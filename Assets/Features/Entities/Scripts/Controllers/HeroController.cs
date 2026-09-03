using System;
using System.Collections.Generic;
using Game.Entities;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using Game.JoystickInput;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
    /// <summary>Owns hero state and rules. Service supplies external values and routes commands.</summary>
    internal sealed class HeroController : IHeroPresentationSource
    {
        private HeroState _currentState;
        private bool _wasMoving;
        private bool _canRequestAttack;

        /// <summary>Gets current hero state.</summary>
        public HeroState CurrentState => _currentState;
        /// <summary>Raised after hero position changes.</summary>
        public event Action<Vector3> OnHeroPositionChanged;
        /// <summary>Raised after an attack is confirmed.</summary>
        public event Action<Vector3> OnAttackPerformed;
        /// <summary>Raised after a valid dash commits its authoritative endpoint.</summary>
        public event Action<HeroDashRequest> OnDashPerformed;
        /// <summary>Raised after incoming damage is applied.</summary>
        public event Action<HeroHitResult> OnHeroHit;
        /// <summary>Raised when attack cooldown starts; payload is seconds.</summary>
        public event Action<float> OnAttackCooldownStarted;
        /// <summary>Raised after hero state resets.</summary>
        public event Action<HeroState> OnRestarted;

        public HeroController()
        {
            ResetState(false);
        }

        /// <summary>Advances hero movement and release-cooldown state for one frame.</summary>
        public void Tick(
            JoystickState joystick,
            WeaponConfig weapon,
            float currentTime,
            float deltaTime
        )
        {
            if (_currentState.IsDead)
            {
                _canRequestAttack = false;
                return;
            }

            if (joystick.IsActive)
            {
                _canRequestAttack = false;

                if (joystick.Mode == JoystickInputMode.Secondary)
                {
                    return;
                }

                _wasMoving = true;
                var input = joystick.MovementVector;

                if (input.sqrMagnitude <= 0.01f)
                {
                    return;
                }

                var position =
                    _currentState.Position
                    + new Vector3(-input.x, 0f, -input.y)
                        * HeroConfig.Instance.MoveSpeed
                        * deltaTime;
                SetPosition(position);
                return;
            }

            if (_wasMoving)
            {
                _wasMoving = false;
                _canRequestAttack = false;
                if (weapon != null)
                {
                    StartCooldown(weapon, currentTime);
                }

                return;
            }

            _canRequestAttack = true;
        }

        /// <summary>
        /// Validates armed living hero dash input, commits bounded endpoint, and returns complete
        /// path for service-owned damage and weapon routing.
        /// </summary>
        public bool TryCreateDashRequest(
            Vector2 inputDirection,
            WeaponConfig weapon,
            out HeroDashRequest request
        )
        {
            request = default;

            if (_currentState.IsDead || weapon == null || inputDirection.sqrMagnitude <= .0001f)
            {
                return false;
            }

            Vector3 direction = new Vector3(-inputDirection.x, 0f, -inputDirection.y).normalized;
            Vector3 start = _currentState.Position;
            SetPosition(start + direction * HeroConfig.Instance.DashDistance);
            request = new HeroDashRequest(start, _currentState.Position, direction);
            OnDashPerformed?.Invoke(request);
            return true;
        }

        /// <summary>
        /// Creates an attack request when hero is idle, alive, off cooldown, and has a target in
        /// range. Chooses the nearest target; equal-distance targets use the lower runtime ID.
        /// </summary>
        public bool TryCreateAttackRequest(
            WeaponConfig weapon,
            IReadOnlyDictionary<int, EnemyState> enemies,
            float currentTime,
            out HeroAttackRequest request
        )
        {
            request = default;

            if (!_canRequestAttack || weapon == null || currentTime < _currentState.NextAttackTime)
            {
                return false;
            }

            var rangeSqr = weapon.Range * weapon.Range;
            var bestDistanceSqr = rangeSqr;
            var bestTargetId = int.MaxValue;
            var bestTargetPosition = default(Vector3);
            var hasTarget = false;

            foreach (var pair in enemies)
            {
                var enemy = pair.Value;
                var distanceSqr = (enemy.Position - _currentState.Position).sqrMagnitude;

                if (distanceSqr >= rangeSqr)
                {
                    continue;
                }

                if (
                    !hasTarget
                    || distanceSqr < bestDistanceSqr
                    || (distanceSqr == bestDistanceSqr && enemy.Id < bestTargetId)
                )
                {
                    bestDistanceSqr = distanceSqr;
                    bestTargetId = enemy.Id;
                    bestTargetPosition = enemy.Position;
                    hasTarget = true;
                }
            }

            if (!hasTarget)
            {
                return false;
            }

            request = new HeroAttackRequest(
                bestTargetId,
                bestTargetPosition,
                weapon.Damage,
                weapon.Cooldown
            );
            return true;
        }

        /// <summary>Commits confirmed attack and starts its cooldown.</summary>
        public void ConfirmAttack(HeroAttackRequest request, float currentTime)
        {
            StartCooldown(request.Cooldown, currentTime, true);
            OnAttackPerformed?.Invoke(request.TargetPosition);
        }

        /// <summary>Applies incoming damage unless hero is already dead.</summary>
        public bool TakeHit(int damage)
        {
            if (_currentState.IsDead)
            {
                return false;
            }

            var health = Mathf.Max(0, _currentState.Health - damage);
            _currentState = new HeroState(
                _currentState.Position,
                health,
                _currentState.LastAttackTime,
                _currentState.NextAttackTime
            );
            OnHeroHit?.Invoke(
                new HeroHitResult(damage, health, _currentState.Position, health == 0)
            );
            return true;
        }

        /// <summary>Restores initial hero state and publishes it.</summary>
        public void Restart()
        {
            ResetState(true);
        }

        /// <summary>Clears presentation callbacks during service teardown.</summary>
        internal void ClearPresentationSubscribers()
        {
            OnHeroPositionChanged = null;
            OnAttackPerformed = null;
            OnDashPerformed = null;
            OnHeroHit = null;
            OnAttackCooldownStarted = null;
            OnRestarted = null;
        }

        private void ResetState(bool publish)
        {
            _currentState = new HeroState(Vector3.zero, HeroConfig.Instance.InitialHealth, 0f, 0f);
            _wasMoving = false;
            _canRequestAttack = false;

            if (!publish)
            {
                return;
            }

            OnRestarted?.Invoke(_currentState);
        }

        private void StartCooldown(WeaponConfig weapon, float currentTime)
        {
            var cooldown = weapon != null ? weapon.Cooldown : 0f;
            StartCooldown(cooldown, currentTime, false);
        }

        private void StartCooldown(float cooldown, float currentTime, bool attackConfirmed)
        {
            _currentState = new HeroState(
                _currentState.Position,
                _currentState.Health,
                attackConfirmed ? currentTime : _currentState.LastAttackTime,
                currentTime + cooldown
            );
            OnAttackCooldownStarted?.Invoke(cooldown);
        }

        private void SetPosition(Vector3 position)
        {
            position.x = Mathf.Clamp(
                position.x,
                -Constants.World.ArenaLimit,
                Constants.World.ArenaLimit
            );
            position.z = Mathf.Clamp(
                position.z,
                -Constants.World.ArenaLimit,
                Constants.World.ArenaLimit
            );
            _currentState = new HeroState(
                position,
                _currentState.Health,
                _currentState.LastAttackTime,
                _currentState.NextAttackTime
            );
            OnHeroPositionChanged?.Invoke(position);
        }
    }
}

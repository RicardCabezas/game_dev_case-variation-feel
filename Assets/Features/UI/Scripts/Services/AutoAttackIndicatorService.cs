using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.Entities;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Game.JoystickInput;

namespace Game.UI
{
    /// <summary>Adapts hero presentation and joystick events into indicator commands.</summary>
    public sealed class AutoAttackIndicatorService : IService
    {
        private AutoAttackIndicatorController _controller;
        private IHeroPresentationSource _hero;
        private JoystickInputService _joystick;
        /// <summary>Gets indicator presentation state and events.</summary>
        public IAutoAttackIndicatorPresentationSource Presentation => _controller;

        /// <inheritdoc/>
        public Type[] GetDependencies() =>
            new[] { typeof(EntitiesService), typeof(JoystickInputService) };

        /// <inheritdoc/>
        public UniTask<bool> Initialize()
        {
            _hero = ServicesLocator.Instance.GetService<EntitiesService>().HeroPresentation;
            _joystick = ServicesLocator.Instance.GetService<JoystickInputService>();
            _controller = new AutoAttackIndicatorController();
            _hero.OnHeroHit += OnHeroHit;
            _hero.OnAttackCooldownStarted += OnCooldown;
            _hero.OnRestarted += OnRestarted;
            _joystick.OnStateChanged += OnJoystick;

            if (_hero.CurrentState.IsDead)
            {
                _controller.ApplyHeroDeath();
            }

            _controller.ApplyJoystick(_joystick.CurrentState);
            return UniTask.FromResult(true);
        }

        /// <inheritdoc/>
        public UniTask Reset()
        {
            if (_hero != null)
            {
                _hero.OnHeroHit -= OnHeroHit;
                _hero.OnAttackCooldownStarted -= OnCooldown;
                _hero.OnRestarted -= OnRestarted;
            }

            if (_joystick != null)
            {
                _joystick.OnStateChanged -= OnJoystick;
            }

            _hero = null;
            _joystick = null;
            _controller = null;
            return UniTask.CompletedTask;
        }

        private void OnHeroHit(HeroHitResult hit)
        {
            if (hit.IsLethal)
            {
                _controller.ApplyHeroDeath();
            }
        }

        private void OnJoystick(JoystickState state) => _controller.ApplyJoystick(state);

        private void OnRestarted(HeroState state) => _controller.ApplyRestart();

        private void OnCooldown(float duration) =>
            _controller.StartCooldown(
                duration,
                _hero.CurrentState.IsDead,
                _joystick.CurrentState.IsActive
            );
    }
}

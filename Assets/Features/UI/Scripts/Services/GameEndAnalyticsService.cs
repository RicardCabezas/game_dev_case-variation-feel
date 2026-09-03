using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.Entities;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;

namespace Game.UI
{
    /// <summary>
    /// Observes authoritative hero notifications and accumulates statistics for current game run.
    /// </summary>
    public sealed class GameEndAnalyticsService : IService
    {
        private IHeroPresentationSource _hero;

        /// <summary>Gets current game-run statistics.</summary>
        public GameEndStats CurrentStats { get; private set; }

        /// <inheritdoc/>
        public Type[] GetDependencies() => new[] { typeof(EntitiesService) };

        /// <inheritdoc/>
        public UniTask<bool> Initialize()
        {
            _hero = ServicesLocator.Instance.GetService<EntitiesService>().HeroPresentation;
            _hero.OnHeroHit += OnHeroHit;
            _hero.OnAttackPerformed += OnAttackPerformed;
            _hero.OnDashPerformed += OnDashPerformed;
            _hero.OnRestarted += OnRestarted;
            CurrentStats = default;
            return UniTask.FromResult(true);
        }

        /// <inheritdoc/>
        public UniTask Reset()
        {
            if (_hero != null)
            {
                _hero.OnHeroHit -= OnHeroHit;
                _hero.OnAttackPerformed -= OnAttackPerformed;
                _hero.OnDashPerformed -= OnDashPerformed;
                _hero.OnRestarted -= OnRestarted;
            }

            _hero = null;
            CurrentStats = default;
            return UniTask.CompletedTask;
        }

        private void OnHeroHit(HeroHitResult hit) => CurrentStats = CurrentStats.WithDamage(hit.Damage);

        private void OnAttackPerformed(UnityEngine.Vector3 _) => CurrentStats = CurrentStats.WithWeaponUse();

        private void OnDashPerformed(HeroDashRequest _) => CurrentStats = CurrentStats.WithDash();

        private void OnRestarted(HeroState _) => CurrentStats = default;
    }

    /// <summary>Immutable statistics collected during one game run.</summary>
    public readonly struct GameEndStats
    {
        /// <summary>Gets total accepted incoming hero damage.</summary>
        public int DamageReceived { get; }

        /// <summary>Gets confirmed weapon attacks.</summary>
        public int WeaponsUsed { get; }

        /// <summary>Gets committed hero dashes.</summary>
        public int DashesUsed { get; }

        /// <summary>Creates game-run statistics.</summary>
        public GameEndStats(int damageReceived, int weaponsUsed, int dashesUsed)
        {
            DamageReceived = damageReceived;
            WeaponsUsed = weaponsUsed;
            DashesUsed = dashesUsed;
        }

        internal GameEndStats WithDamage(int damage) =>
            new GameEndStats(DamageReceived + damage, WeaponsUsed, DashesUsed);

        internal GameEndStats WithWeaponUse() =>
            new GameEndStats(DamageReceived, WeaponsUsed + 1, DashesUsed);

        internal GameEndStats WithDash() =>
            new GameEndStats(DamageReceived, WeaponsUsed, DashesUsed + 1);
    }
}

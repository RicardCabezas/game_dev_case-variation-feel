using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.Weapons;

namespace Game.UI
{
    public sealed class WeaponUsesIndicatorService : IService
    {
        private WeaponUsesIndicatorController _controller;
        private WeaponsService _weapons;
        public IWeaponUsesIndicatorPresentationSource Presentation => _controller;

        public Type[] GetDependencies() =>
            new[] { typeof(WeaponsService) };

        public UniTask<bool> Initialize()
        {
            _weapons = ServicesLocator.Instance.GetService<WeaponsService>();
            _controller = new WeaponUsesIndicatorController();
            _weapons.OnWeaponStateChanged += Apply;
            _controller.Apply(_weapons.CurrentWeaponState);
            return UniTask.FromResult(true);
        }

        public UniTask Reset()
        {
            if (_weapons != null)
            {
                _weapons.OnWeaponStateChanged -= Apply;
            }
            _weapons = null;
            _controller = null;
            return UniTask.CompletedTask;
        }

        private void Apply(EquippedWeaponState state)
        {
            _controller.Apply(state);
        }
    }
}

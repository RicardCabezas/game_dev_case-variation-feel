using System.Collections;
using Core.ServicesManager;
using Game.Weapons;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Plays the broken-weapon UI animation after equipped weapon durability reaches zero.
    /// </summary>
    [RequireComponent(typeof(Animation), typeof(CanvasGroup))]
    public sealed class BrokenWeaponAnimationView : MonoBehaviour
    {
        [SerializeField] private Animation animationPlayer;
        [SerializeField] private CanvasGroup canvasGroup;

        private WeaponsService _weaponsService;
        private Coroutine _hideRoutine;

        private void Awake()
        {
            animationPlayer ??= GetComponent<Animation>();
            canvasGroup ??= GetComponent<CanvasGroup>();
            Hide();
        }

        private void Start() => ServicesLocator.Instance.OnAllServicesInitialized += Initialize;

        private void Initialize()
        {
            _weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
            _weaponsService.OnEquippedWeaponDestroyed += Show;
        }

        private void Show()
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
            }

            canvasGroup.alpha = 1f;
            animationPlayer.Stop();
            animationPlayer.Rewind();
            animationPlayer.Play();
            _hideRoutine = StartCoroutine(HideWhenAnimationCompletes());
        }

        private IEnumerator HideWhenAnimationCompletes()
        {
            AnimationClip clip = animationPlayer.clip;
            yield return new WaitForSeconds(clip == null ? 0f : clip.length);
            Hide();
            _hideRoutine = null;
        }

        private void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void OnDestroy()
        {
            ServicesLocator.Instance.OnAllServicesInitialized -= Initialize;
            if (_weaponsService != null)
            {
                _weaponsService.OnEquippedWeaponDestroyed -= Show;
            }
        }
    }
}

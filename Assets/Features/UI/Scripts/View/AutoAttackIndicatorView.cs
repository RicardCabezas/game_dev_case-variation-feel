using Core.ServicesManager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	public class AutoAttackIndicatorView : MonoBehaviour
	{
		[SerializeField] private Image fillImage;
		[SerializeField] private GameObject helperText;

		private AutoAttackIndicatorController _controller;
		private bool _isFilling;
		private float _fillDuration;
		private float _fillElapsed;

		private void Awake()
		{
			Hide();
		}

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void Update()
		{
			if (!_isFilling || fillImage == null) return;

			_fillElapsed += Time.deltaTime;
			fillImage.fillAmount = Mathf.Clamp01(_fillElapsed / _fillDuration);
			_isFilling = fillImage.fillAmount < 1f;
		}

		public void StartFilling(float duration)
		{
			_fillDuration = Mathf.Max(0f, duration);
			_fillElapsed = 0f;
			_isFilling = _fillDuration > 0f;

			if (fillImage != null)
			{
				fillImage.fillAmount = _isFilling ? 0f : 1f;
			}

			if (helperText != null)
			{
				helperText.SetActive(true);
			}
		}

		public void Hide()
		{
			_isFilling = false;
			_fillDuration = 0f;
			_fillElapsed = 0f;

			if (fillImage != null)
			{
				fillImage.fillAmount = 0f;
			}

			if (helperText != null)
			{
				helperText.SetActive(false);
			}
		}

		private void OnServicesInitialized()
		{
			_controller = ServicesLocator.Instance.GetService<AutoAttackIndicatorService>().Controller;
			_controller.OnStateChanged += OnIndicatorStateChanged;
			OnIndicatorStateChanged(_controller.CurrentState);
		}

		private void OnIndicatorStateChanged(AutoAttackIndicatorState state)
		{
			if (state.IsVisible)
			{
				StartFilling(state.FillDuration);
			}
			else
			{
				Hide();
			}
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;

			if (_controller != null)
			{
				_controller.OnStateChanged -= OnIndicatorStateChanged;
			}
		}
	}
}

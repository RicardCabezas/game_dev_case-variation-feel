using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [RequireComponent(typeof(RectTransform))]
    /// <summary>Projects one world-space health bar into screen space.</summary>
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField]
        private Image fillImage;

        private RectTransform _rectTransform;
        private RectTransform _canvasRect;
        private Camera _worldCamera;
        private float _fillSmoothDuration;
        private float _targetFill;
        private Vector3 _worldPosition;
        private Vector3 _worldOffset;
        private bool _hasState;
        private bool _stateVisible;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;

            if (fillImage == null)
            {
                fillImage = GetComponentInChildren<Image>(true);
            }
        }

        /// <summary>Configures world-to-canvas positioning and fill smoothing.</summary>
        /// <param name="canvasRect">Canvas coordinate space.</param>
        /// <param name="worldCamera">Camera used for projection.</param>
        /// <param name="worldOffset">World offset above tracked entity.</param>
        /// <param name="fillSmoothDuration">Fill interpolation duration in seconds.</param>
        public void Initialize(
            RectTransform canvasRect,
            Camera worldCamera,
            Vector3 worldOffset,
            float fillSmoothDuration
        )
        {
            _canvasRect = canvasRect;
            _worldCamera = worldCamera;
            _worldOffset = worldOffset;
            _fillSmoothDuration = Mathf.Max(0f, fillSmoothDuration);
        }

        /// <summary>Applies bar state and visibility.</summary>
        public void ApplyState(HealthBarState state)
        {
            _worldPosition = state.WorldPosition;
            _stateVisible = state.IsVisible;
            _targetFill = state.NormalizedHealth;

            if (!_hasState)
            {

                if (fillImage != null)
                {
                    fillImage.fillAmount = _targetFill;
                }

                _hasState = true;
            }

            if (!_stateVisible)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>Projects tracked world position into canvas space.</summary>
        public void UpdateScreenPosition()
        {

            if (!_stateVisible || _worldCamera == null || _canvasRect == null)
            {
                return;
            }

            Vector3 viewportPosition = _worldCamera.WorldToViewportPoint(
                _worldPosition + _worldOffset
            );
            bool isOnScreen =
                viewportPosition.z > 0f
                && viewportPosition.x >= 0f
                && viewportPosition.x <= 1f
                && viewportPosition.y >= 0f
                && viewportPosition.y <= 1f;

            if (!isOnScreen)
            {

                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            Vector2 screenPosition = new Vector2(
                viewportPosition.x * Screen.width,
                viewportPosition.y * Screen.height
            );

            if (
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    screenPosition,
                    null,
                    out Vector2 localPosition
                )
            )
            {
                _rectTransform.anchoredPosition = localPosition;
            }
        }

        private void Update()
        {

            if (!_stateVisible || fillImage == null)
            {
                return;
            }

            if (_fillSmoothDuration <= 0f)
            {
                fillImage.fillAmount = _targetFill;
                return;
            }

            fillImage.fillAmount = Mathf.MoveTowards(
                fillImage.fillAmount,
                _targetFill,
                Time.deltaTime / _fillSmoothDuration
            );
        }
    }
}

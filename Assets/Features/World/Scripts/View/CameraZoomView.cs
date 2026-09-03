using Cinemachine;
using UnityEngine;

namespace Game.World
{
    /// <summary>Reusable timestamp-based in-and-out zoom for the world virtual camera.</summary>
    public sealed class CameraZoomView : MonoBehaviour
    {
        [SerializeField]
        private CinemachineVirtualCamera camera;

        [SerializeField]
        [Tooltip("Total seconds for one zoom-in and zoom-out pulse")]
        private float zoomDuration = .18f;

        [SerializeField]
        [Tooltip("Negative field-of-view offset applied at pulse midpoint")]
        private float zoomInFieldOfViewOffset = -6f;

        private float _baseFieldOfView;
        private float _zoomStartTime;
        private bool _isZooming;

        private void Awake()
        {
            if (camera == null)
            {
                camera = GetComponentInChildren<CinemachineVirtualCamera>(true);
            }

            if (camera != null)
            {
                _baseFieldOfView = camera.m_Lens.FieldOfView;
            }
        }

        /// <summary>Starts or restarts configured zoom pulse when a virtual camera is available.</summary>
        public void Play()
        {
            if (camera == null)
            {
                return;
            }

            _zoomStartTime = Time.time;
            _isZooming = true;
        }

        private void Update()
        {
            if (!_isZooming || camera == null)
            {
                return;
            }

            var duration = Mathf.Max(.01f, zoomDuration);
            var progress = (Time.time - _zoomStartTime) / duration;

            if (progress >= 1f)
            {
                SetFieldOfView(_baseFieldOfView);
                _isZooming = false;
                return;
            }

            var pulse = Mathf.Sin(progress * Mathf.PI);
            SetFieldOfView(_baseFieldOfView + zoomInFieldOfViewOffset * pulse);
        }

        private void OnDestroy()
        {
            if (camera != null)
            {
                SetFieldOfView(_baseFieldOfView);
            }
        }

        private void SetFieldOfView(float fieldOfView)
        {
            LensSettings lens = camera.m_Lens;
            lens.FieldOfView = fieldOfView;
            camera.m_Lens = lens;
        }
    }
}

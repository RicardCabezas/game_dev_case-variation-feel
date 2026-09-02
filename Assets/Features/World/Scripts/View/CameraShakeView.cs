using Cinemachine;
using UnityEngine;

namespace Game.World
{
    /// <summary>Reusable timestamp-based Cinemachine noise shake for the world camera.</summary>
    public sealed class CameraShakeView : MonoBehaviour
    {
        [SerializeField]
        private CinemachineVirtualCamera camera;

        [SerializeField]
        private float shakeDuration = 0.1f;

        [SerializeField]
        private float shakeAmplitude = 0.5f;

        [SerializeField]
        private float shakeFrequency = 2f;

        private CinemachineBasicMultiChannelPerlin _noise;
        private float _shakeEndTime;
        private bool _isShaking;

        private void Awake()
        {
            if (camera == null)
            {
                camera = GetComponentInChildren<CinemachineVirtualCamera>(true);
            }

            if (camera != null)
            {
                _noise = camera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            }
        }

        /// <summary>Starts or restarts camera shake when valid noise is configured.</summary>
        public void Play()
        {
            if (_noise == null || _noise.m_NoiseProfile == null)
            {
                return;
            }

            _noise.m_FrequencyGain = shakeFrequency;
            _noise.m_AmplitudeGain = shakeAmplitude;
            _shakeEndTime = Time.time + shakeDuration;
            _isShaking = true;
        }

        private void Update()
        {
            if (!_isShaking || Time.time < _shakeEndTime)
            {
                return;
            }

            _noise.m_AmplitudeGain = 0f;
            _isShaking = false;
        }

        private void OnDestroy()
        {
            if (_noise != null)
            {
                _noise.m_AmplitudeGain = 0f;
            }
        }
    }
}

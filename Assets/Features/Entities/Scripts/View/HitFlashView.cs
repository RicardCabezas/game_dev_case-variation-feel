using UnityEngine;

namespace Game.GamePlay.Entities
{
    /// <summary>Reusable timestamp-based material flash for one entity hierarchy.</summary>
    public sealed class HitFlashView : MonoBehaviour
    {
        private static readonly int BaseColorHash = Shader.PropertyToID("_BaseColor");

        [SerializeField]
        private Renderer[] renderers;

        [SerializeField]
        private Color hitFlashColor = new Color(1f, 0.2f, 0.2f, 1f);

        [SerializeField]
        private float hitFlashDuration = 0.1f;

        private MaterialPropertyBlock _materialPropertyBlock;
        private float _hitFlashEndTime;
        private bool _isHitFlashActive;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        /// <summary>Starts or restarts hit flash on configured renderers.</summary>
        public void Play()
        {
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            if (_materialPropertyBlock == null)
            {
                _materialPropertyBlock = new MaterialPropertyBlock();
            }

            _materialPropertyBlock.Clear();
            _materialPropertyBlock.SetColor(BaseColorHash, hitFlashColor);

            for (var i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer != null)
                {
                    renderer.SetPropertyBlock(_materialPropertyBlock);
                }
            }

            _hitFlashEndTime = Time.time + hitFlashDuration;
            _isHitFlashActive = true;
        }

        private void Update()
        {
            if (!_isHitFlashActive || Time.time < _hitFlashEndTime)
            {
                return;
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer != null)
                {
                    renderer.SetPropertyBlock(null);
                }
            }

            _isHitFlashActive = false;
        }
    }
}

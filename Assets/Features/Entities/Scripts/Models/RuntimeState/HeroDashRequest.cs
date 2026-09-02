using UnityEngine;

namespace Game.GamePlay.Heroes
{
    /// <summary>Immutable committed dash path used for gameplay routing and presentation.</summary>
    public readonly struct HeroDashRequest
    {
        /// <summary>Gets authoritative hero position before dash.</summary>
        public Vector3 StartPosition { get; }

        /// <summary>Gets authoritative hero position after arena-bound clamping.</summary>
        public Vector3 EndPosition { get; }

        /// <summary>Gets normalized world-space direction from start to requested endpoint.</summary>
        public Vector3 Direction { get; }

        /// <summary>Creates committed dash path.</summary>
        public HeroDashRequest(Vector3 startPosition, Vector3 endPosition, Vector3 direction)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            Direction = direction;
        }
    }
}

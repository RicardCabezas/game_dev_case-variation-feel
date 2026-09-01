using Cinemachine;
using UnityEngine;

namespace Game.World
{
    /// <summary>Unity presentation root for instantiated world and its camera reference.</summary>
    public class WorldView : MonoBehaviour
    {
        [SerializeField]
        private new CinemachineVirtualCamera camera;

        /// <summary>Gets camera configured to follow runtime hero presentation.</summary>
        public CinemachineVirtualCamera Camera => camera;
    }
}

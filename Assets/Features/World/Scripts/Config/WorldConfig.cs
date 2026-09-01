using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.World
{
    [CreateAssetMenu(fileName = "WorldConfig", menuName = "Game/WorldConfig")]
    /// <summary>Resources-backed configuration for persistent world presentation.</summary>
    public class WorldConfig : ScriptableObjectSingleton<WorldConfig>
    {
        [SerializeField]
        [Tooltip("The world prefab to instantiate")]
        private WorldView worldPrefab;

        /// <summary>Gets world prefab instantiated and owned by <see cref="WorldService"/>.</summary>
        public WorldView WorldPrefab => worldPrefab;
    }
}

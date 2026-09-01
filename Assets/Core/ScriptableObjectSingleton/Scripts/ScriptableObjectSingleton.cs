using UnityEngine;

namespace Core.ScriptableObjectSingleton
{
    /// <summary>Provides lazily loaded access to one Resources-backed configuration asset.</summary>
    /// <typeparam name="T">
    /// Concrete ScriptableObject type loaded from a Resources asset with matching type name.
    /// </typeparam>
    public abstract class ScriptableObjectSingleton<T> : ScriptableObject
        where T : ScriptableObject
    {
        private static T _instance;

        /// <summary>Gets cached configuration instance, or loads it from Resources on first access.</summary>
        /// <remarks>Logs an error and returns <see langword="null"/> when required asset is absent.</remarks>
        public static T Instance
        {
            get
            {

                if (_instance != null)
                {
                    return _instance;
                }

                _instance = Resources.Load<T>(typeof(T).Name);

                if (_instance == null)
                {
                    Debug.LogError(
                        $"{typeof(T).Name} not found in Resources folder. "
                            + $"Please create one at Resources/{typeof(T).Name}."
                    );
                }

                return _instance;
            }
        }
    }
}

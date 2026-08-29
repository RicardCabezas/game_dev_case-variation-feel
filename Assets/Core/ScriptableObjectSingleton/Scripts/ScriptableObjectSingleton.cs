using UnityEngine;

namespace Core.ScriptableObjectSingleton
{
	public abstract class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject
	{
		private static T _instance;

		public static T Instance
		{
			get
			{
				if (_instance != null) return _instance;

				_instance = Resources.Load<T>(typeof(T).Name);

				if (_instance == null)
				{
					Debug.LogError($"{typeof(T).Name} not found in Resources folder. Please create one at Resources/{typeof(T).Name}.");
				}

				return _instance;
			}
		}
	}
}

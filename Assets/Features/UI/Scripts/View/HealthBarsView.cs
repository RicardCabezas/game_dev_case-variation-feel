using System.Collections.Generic;
using Core.ServicesManager;
using Game.GamePlay.Enemies;
using UnityEngine;

namespace Game.UI
{
	public sealed class HealthBarsView : MonoBehaviour
	{
		[SerializeField] private HealthBarView healthBarPrefab;
		[SerializeField] private RectTransform canvasRect;
		[SerializeField] private Camera worldCamera;
		[SerializeField] private Vector3 heroWorldOffset = new Vector3(0f, 1.5f, 0f);
		[SerializeField] private Vector3 enemyWorldOffset = new Vector3(0f, 1.5f, 0f);
		[SerializeField] private float fillSmoothDuration = 0.15f;

		private HealthBarsController _controller;
		private Dictionary<HealthBarId, HealthBarView> _views;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_controller = ServicesLocator.Instance.GetService<HealthBarsService>().Controller;
			if (canvasRect == null) canvasRect = (RectTransform)transform;

			int capacity = Mathf.Max(1, EnemiesConfig.Instance.MaxEnemies + 1);
			_views = new Dictionary<HealthBarId, HealthBarView>(capacity);
			_controller.OnHealthBarAdded += OnHealthBarAdded;
			_controller.OnHealthBarChanged += OnHealthBarChanged;
			_controller.OnHealthBarRemoved += OnHealthBarRemoved;

			foreach (KeyValuePair<HealthBarId, HealthBarState> state in _controller.CurrentStates)
			{
				OnHealthBarAdded(state.Value);
			}
		}

		private void LateUpdate()
		{
			if (_views == null) return;
			foreach (KeyValuePair<HealthBarId, HealthBarView> view in _views)
			{
				view.Value.UpdateScreenPosition();
			}
		}

		private void OnHealthBarAdded(HealthBarState state)
		{
			if (_views.ContainsKey(state.Id))
			{
				_views[state.Id].ApplyState(state);
				return;
			}

			if (healthBarPrefab == null) return;
			HealthBarView view = Instantiate(healthBarPrefab, transform);
			Vector3 worldOffset = state.Id.Owner == HealthBarOwner.Hero ? heroWorldOffset : enemyWorldOffset;
			view.Initialize(canvasRect, worldCamera, worldOffset, fillSmoothDuration);
			view.ApplyState(state);
			_views.Add(state.Id, view);
		}

		private void OnHealthBarChanged(HealthBarState state)
		{
			if (_views.TryGetValue(state.Id, out HealthBarView view))
			{
				view.ApplyState(state);
				return;
			}

			if (state.IsVisible) OnHealthBarAdded(state);
		}

		private void OnHealthBarRemoved(HealthBarId id)
		{
			if (!_views.Remove(id, out HealthBarView view)) return;
			Destroy(view.gameObject);
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_controller == null) return;
			_controller.OnHealthBarAdded -= OnHealthBarAdded;
			_controller.OnHealthBarChanged -= OnHealthBarChanged;
			_controller.OnHealthBarRemoved -= OnHealthBarRemoved;
		}
	}
}

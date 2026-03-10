using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵의 포탈을 참조로 보유. 각 포탈의 OnInteracted 시 맵(MapView) 열기.
/// 해금 로직은 나중에 여기서만 관리하면 됨.
/// </summary>
public class PortalController : MonoBehaviour
{
    [SerializeField] [Tooltip("초기화 시점에 Find함수로 모든 포탈 등록")]
    private List<PortalModel> _portalModels = new List<PortalModel>();

    [SerializeField] FlagSystem _flagSystem;

    [SerializeField] [Tooltip("지도 View")]
    MapView _mapView;

    private void OnDisable()
    {
        for (int i = 0; i < _portalModels.Count; i++)
        {
            if (_portalModels[i] != null)
                _portalModels[i].Portal.OnInteracted -= HandlePortalInteracted;
        }
    }

    public void Initialize(MapView mapView, FlagSystem flagSystem)
    {
        Debug.Log($"{gameObject.name} + 초기화함수 실행");
        _mapView = mapView;
        _flagSystem = flagSystem;

        // 기존 구독 해제 및 리스트 초기화 (중복 방지)
        foreach (var model in _portalModels)
        {
            if (model.Portal != null)
                model.Portal.OnInteracted -= HandlePortalInteracted;
        }
        _portalModels.Clear();

        Portal[] worldPortals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        foreach (var p in worldPortals)
        {
            var model = new PortalModel(p, _flagSystem);
            _portalModels.Add(model);

            p.Initialize(model.IsUnlocked);
            p.OnInteracted += HandlePortalInteracted; // 여기서 구독
        }
    }

    private void HandlePortalInteracted(IInteractReceiver receiver, Portal interactedPortal)
    {
        if (receiver == null || interactedPortal == null) return;

        _mapView.ToggleMap();
    }

    public IReadOnlyList<PortalModel> PortalModels => _portalModels;
}

using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapView : PanelViewBase
{
    [Header("UI Hierarchy")]
    [SerializeField] private GameObject _mapPanel;
    [SerializeField] private RectTransform _mapContent;    // ScrollRect의 Content (배경+아이콘 포함)
    [SerializeField] private RectTransform _iconContainer; // 포탈 아이콘이 생성될 부모
    [SerializeField] private RectTransform _playerIcon;    // 플레이어 화살표 아이콘
    [SerializeField] private RawImage _snapshotDisplay;   // 지형 스냅샷을 보여줄 UI

    [Header("Prefabs")]
    [SerializeField] private GameObject _portalIconPrefab;

    [Header("Map Reference")]
    [SerializeField] private Camera _mapCamera;           // 스냅샷 촬영용 카메라
    [SerializeField] private float _worldSize = 300f;      // 월드 실제 가로세로 크기 (1:1 가정)
    [SerializeField] private Vector2 _worldCenter;         // 월드 중앙 좌표 (x, z)

    [Header("Zoom Settings")]
    [SerializeField] private Slider _zoomSlider; // 인스펙터에서 슬라이더 연결
    [SerializeField] private float _minZoom = 1f;
    [SerializeField] private float _maxZoom = 3f;

    [Header("Sensitivity Settings")]
    [Range(0.01f, 1f)]
    [SerializeField] private float _zoomSensitivity = 0.05f; // 줌 속도 (낮을수록 느림)

    private List<Map_PortalIcon> _portalIcons = new List<Map_PortalIcon>();
    private PortalController _portalController;
    private SquadController _squadController;
    private Character _player;
    private Vector3 _initialContentScale;

    public void Initialize(PortalController portalController, Character player, SquadController sc)
    {
        _portalController = portalController;
        _player = player;
        _squadController = sc;
        _initialContentScale = _mapContent.localScale;

        // 초기 카메라 위치를 기준으로 월드 중앙 자동 설정
        if (_mapCamera != null)
        {
            _worldCenter = new Vector2(_mapCamera.transform.position.x, _mapCamera.transform.position.z);
            _mapCamera.enabled = false; // 평소엔 꺼둠
        }

        if (_zoomSlider != null)
        {
            _zoomSlider.minValue = _minZoom;
            _zoomSlider.maxValue = _maxZoom;
            _zoomSlider.value = _mapContent.localScale.x;

            // 슬라이더를 직접 움직일 때 지도가 확대/축소되도록 연결
            _zoomSlider.onValueChanged.RemoveAllListeners();
            _zoomSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (_mapPanel != null) _mapPanel.SetActive(false);
    }

    /// <summary>
    /// 지도를 열거나 닫을 때 호출
    /// </summary>
    public void ToggleMap()
    {
        if (_mapPanel == null) return;
        bool isOpening = !_mapPanel.activeSelf;

        if (isOpening)
        {
            ResetMapView();

            _mapPanel.SetActive(true);
            TakeSnapshot();      // 1. 현재 월드 상태 스냅샷 촬영
            RefreshPortalIcons(); // 2. 해금된 포탈 배치
            OpenPanel();         // 3. UI 애니메이션 실행 (Base 기능)
        }
        else
        {
            _mapPanel.SetActive(false);
            ClearIcons();
            ClosePanel();
        }
    }

    private void ResetMapView()
    {
        // 1. 줌 크기를 기본값(1배)으로 초기화
        _mapContent.localScale = Vector3.one * _minZoom;

        // 2. 슬라이더 값도 초기화
        if (_zoomSlider != null)
            _zoomSlider.SetValueWithoutNotify(_minZoom);

        // 3. 지도의 위치를 중앙으로 초기화 (Content의 위치를 0으로)
        _mapContent.anchoredPosition = Vector2.zero;

        // 4. 실행 중인 트윈이 있다면 중단
        _mapContent.DOKill();
    }

    private void TakeSnapshot()
    {
        if (_mapCamera == null) return;

        // 카메라를 한 번만 렌더링하여 RenderTexture를 최신화합니다.
        _mapCamera.enabled = true;
        _mapCamera.Render();
        _mapCamera.enabled = false;
    }

    private void RefreshPortalIcons()
    {
        ClearIcons();
        var models = _portalController.PortalModels;

        foreach (var model in models)
        {
            if (model.IsUnlocked)
            {
                CreatePortalIcon(model);
            }
        }
    }

    private void CreatePortalIcon(PortalModel model)
    {
        GameObject go = Instantiate(_portalIconPrefab, _iconContainer);
        Map_PortalIcon icon = go.GetComponent<Map_PortalIcon>();

        icon.Initialize(model);
        icon.OnPortalClicked += RaisePortalIconClicked;
        // 포탈 위치를 UI 좌표로 변환하여 배치
        icon.GetComponent<RectTransform>().anchoredPosition = WorldToMapPos(model.Portal.transform.position);

        _portalIcons.Add(icon);
    }

    private void RaisePortalIconClicked(PortalModel model)
    {
        _squadController.TeleportPlayer(model.ArrivalPosition);
        ToggleMap();
    }

    private void Update()
    {
        if (!_mapPanel.activeSelf || _player.transform == null) return;

        // 플레이어 마커 실시간 업데이트 (지형 위에 있으므로 매 프레임 위치 갱신)
        _playerIcon.anchoredPosition = WorldToMapPos(_player.transform.position);

        // 플레이어 회전값 연동
        float rotZ = -_player.transform.eulerAngles.y;
        _playerIcon.localRotation = Quaternion.Euler(0, 0, rotZ);
    }

    // 1. 슬라이더 조작 시 호출되는 함수
    private void OnSliderValueChanged(float value)
    {
        // DOTween을 써도 되고, 슬라이더는 즉각적인 피드백이 중요하므로 바로 scale을 줘도 좋습니다.
        _mapContent.localScale = Vector3.one * value;
    }

    public void ScrollZoom(Vector2 scrollInput)
    {
        if (!_mapPanel.activeSelf) return;

        float scrollY = scrollInput.y;
        if (Mathf.Abs(scrollY) < 0.001f) return;

        float currentScale = _mapContent.localScale.x;

        // 1. 입구 컷: 한계치 도달 시 로직 실행 방지
        if (scrollY > 0 && currentScale >= _maxZoom - 0.001f) return;
        if (scrollY < 0 && currentScale <= _minZoom + 0.001f) return;

        // 2. 마우스 위치 계산 (Viewport 기준이 아니라 Content 기준 로컬 좌표)
        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_mapContent, Input.mousePosition, null, out mousePos);

        // 3. 목표 스케일 계산 및 제한
        float targetScale = currentScale + (scrollY * _zoomSensitivity);
        targetScale = Mathf.Clamp(targetScale, _minZoom, _maxZoom);

        // 4. [핵심] 트윈 없이 즉시 계산
        float scaleRatio = targetScale / currentScale;

        // 현재 anchoredPosition에서 마우스 위치를 기준으로 얼마나 이동해야 하는지 계산
        // Pivot이 (0.5, 0.5)일 때의 공식입니다.
        Vector2 targetPos = _mapContent.anchoredPosition - (mousePos * (targetScale - currentScale));

        // 5. 즉시 적용 (트윈 제거)
        _mapContent.localScale = Vector3.one * targetScale;
        _mapContent.anchoredPosition = targetPos;

        FixIconScales(targetScale);

        // 6. 슬라이더 동기화
        if (_zoomSlider != null)
        {
            _zoomSlider.SetValueWithoutNotify(targetScale);
        }
    }

    private void FixIconScales(float currentMapScale)
    {
        // 아이콘들의 부모인 IconContainer 안의 모든 자식들을 순회
        foreach (var icon in _portalIcons)
        {
            if (icon == null) continue;

            // 아이콘의 스케일을 (1 / 지도의 스케일)로 설정
            // 지도가 2배 커지면 아이콘은 0.5배가 되어 결과적으로 크기가 1로 유지됨
            icon.transform.localScale = Vector3.one / currentMapScale;
        }

        // 플레이어 아이콘도 똑같이 처리
        if (_playerIcon != null)
        {
            _playerIcon.localScale = Vector3.one / currentMapScale;
        }
    }

    /// <summary>
    /// 핵심: 월드 3D 좌표를 지도 UI 2D 좌표로 변환
    /// </summary>
    private Vector2 WorldToMapPos(Vector3 worldPos)
    {
        // 1. 월드 좌표의 정규화 (0 ~ 1 범위)
        float normalizedX = (worldPos.x - (_worldCenter.x - _worldSize * 0.5f)) / _worldSize;
        float normalizedZ = (worldPos.z - (_worldCenter.y - _worldSize * 0.5f)) / _worldSize;

        // 2. UI Content 크기에 비례하여 위치 결정 (Pivot이 0.5, 0.5인 경우)
        float mapX = (normalizedX - 0.5f) * _mapContent.rect.width;
        float mapY = (normalizedZ - 0.5f) * _mapContent.rect.height;

        return new Vector2(mapX, mapY);
    }

    private void ClearIcons()
    {
        foreach (var icon in _portalIcons)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        _portalIcons.Clear();
    }
}
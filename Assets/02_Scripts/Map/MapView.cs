//using DG.Tweening;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems; // 인터페이스 사용을 위해 추가
//using UnityEngine.UI;

//public class MapView : PanelViewBase, IDragHandler, IPointerDownHandler
//{
//    [SerializeField] private GameObject _mapPanel;
//    [SerializeField] private Camera _mapCamera;
//    [SerializeField] private RectTransform _mapRectTransform;
//    [SerializeField] private CanvasGroup _mapCanvasGroup;
//    [SerializeField] UIAnimation _uiAnim;
//    [SerializeField] private Slider _zoomSlider;

//    [Header("Map Settings")]
//    [SerializeField] private float _mapSize = 300f;    // 전체 맵 크기 (100x3)
//    [SerializeField] private float _dragSensitivity = 0.5f; // 드래그 감도
//    private Vector2 _mapCenter; // 초기화 시 자동으로 받아올 중앙 좌표

//    [Header("Zoom Settings")]
//    [SerializeField] private float _minSize = 50f;
//    [SerializeField] private float _maxSize = 150f;
//    [SerializeField] private float _zoomSpeed = 0.1f;

//    [Header("맵 아이콘")]
//    [SerializeField] private RectTransform _iconContainer; // 아이콘이 담길 부모 Rect
//    Map_PortalIcon _portalIcon;

//    private List<Map_PortalIcon> _portalIcons = new List<Map_PortalIcon>();

//    [Header("외부 주입용")]
//    [SerializeField] private PortalController _portalController;

//    public void Initialize(PortalController portalController)
//    {
//        _portalController = portalController;

//        if (_mapPanel != null)
//            _mapPanel.SetActive(false);

//        if (_mapCamera != null)
//        {
//            _mapCamera.enabled = false;
//            _mapCamera.orthographicSize = _maxSize;

//            // [중앙 좌표 자동 설정] 
//            // 초기 카메라 위치를 맵의 중앙으로 간주합니다.
//            _mapCenter = new Vector2(_mapCamera.transform.position.x, _mapCamera.transform.position.z);
//        }

//        _uiAnim.Initialize(_mapRectTransform, _mapCanvasGroup);

//        if (_zoomSlider != null)
//        {
//            _zoomSlider.minValue = _minSize;
//            _zoomSlider.maxValue = _maxSize;
//            _zoomSlider.value = _mapCamera.orthographicSize;
//            _zoomSlider.onValueChanged.RemoveAllListeners();
//            _zoomSlider.onValueChanged.AddListener(OnSliderValueChanged);
//        }
//    }

//    // IDragHandler 인터페이스 구현
//    public void OnDrag(PointerEventData eventData)
//    {
//        if (_mapCamera == null || !_mapPanel.activeSelf) return;

//        // 줌 배율에 따라 드래그 속도를 보정 (확대 시 더 세밀하게 이동)
//        float zoomFactor = _mapCamera.orthographicSize / _maxSize;

//        // 마우스 이동 방향과 지형 이동 방향을 일치시킴 (Inverse Drag)
//        Vector3 move = new Vector3(-eventData.delta.x, 0, -eventData.delta.y) * _dragSensitivity * zoomFactor;

//        _mapCamera.transform.position += move;

//        // 드래그 즉시 경계선 체크
//        ApplyBoundaryLimit();
//    }

//    // 이벤트를 받기 위해 필요한 인터페이스
//    public void OnPointerDown(PointerEventData eventData) { }

//    public void ToggleMap(Portal currentPortal = null)
//    {
//        if (currentPortal != null)
//        {

//        }

//        if (_mapPanel == null) return;
//        bool isOpening = !_mapPanel.activeSelf;     // 지도 열려고 하는거야? 여부

//        if (isOpening)
//        {
//            _mapPanel.SetActive(true);
//            if (_mapCamera != null) _mapCamera.enabled = true;
//            _uiAnim?.PlayOpen();
//            OpenPanel();
//        }
//        else
//        {
//            _uiAnim?.PlayClose(() =>
//            {
//                _mapPanel.SetActive(false);
//                if (_mapCamera != null) _mapCamera.enabled = false;
//            });
//            ClosePanel();
//        }
//    }

//    public void ScrollMap(Vector2 input)
//    {
//        if (_mapCamera == null || !_mapPanel.activeSelf) return;

//        float scrollY = input.y;
//        if (Mathf.Abs(scrollY) < 0.01f) return;

//        float targetSize = _mapCamera.orthographicSize - (scrollY * _zoomSpeed);
//        targetSize = Mathf.Clamp(targetSize, _minSize, _maxSize);

//        ApplyZoom(targetSize);
//    }

//    private void ApplyZoom(float targetSize)
//    {
//        _mapCamera.DOKill();
//        // 줌이 변하는 도중에도 실시간으로 경계선을 체크하여 파란 배경 노출 방지
//        _mapCamera.DOOrthoSize(targetSize, 0.2f)
//            .SetEase(Ease.OutCubic)
//            .OnUpdate(ApplyBoundaryLimit);

//        if (_zoomSlider != null)
//            _zoomSlider.SetValueWithoutNotify(targetSize);
//    }

//    private void OnSliderValueChanged(float value)
//    {
//        if (_mapCamera != null)
//        {
//            _mapCamera.orthographicSize = value;
//            ApplyBoundaryLimit();
//        }
//    }

//    /// <summary>
//    /// 카메라가 맵 경계 밖으로 나가지 않도록 좌표를 고정합니다.
//    /// </summary>
//    private void ApplyBoundaryLimit()
//    {
//        if (_mapCamera == null) return;

//        float currentSize = _mapCamera.orthographicSize;
//        float halfMap = _mapSize * 0.5f;
//        float aspect = _mapCamera.aspect;

//        // 현재 줌 사이즈에서 이동 가능한 한계 거리 계산
//        float xLimit = Mathf.Max(0, halfMap - (currentSize * aspect));
//        float zLimit = Mathf.Max(0, halfMap - currentSize);

//        // 자동 갱신된 _mapCenter를 기준으로 Clamp
//        float clampedX = Mathf.Clamp(_mapCamera.transform.position.x, _mapCenter.x - xLimit, _mapCenter.x + xLimit);
//        float clampedZ = Mathf.Clamp(_mapCamera.transform.position.z, _mapCenter.y - zLimit, _mapCenter.y + zLimit);

//        _mapCamera.transform.position = new Vector3(clampedX, _mapCamera.transform.position.y, clampedZ);
//    }

//    private void ChangePortalPositionToUI()
//    {
//        // 1. Controller가 비어있는지 확인
//        if (_portalController == null) return;

//        // 2. 리스트 가져오기 (IReadOnlyList도 foreach 사용 가능)
//        var portalModels = _portalController.PortalModels;

//        foreach (var model in portalModels)
//        {
//            if (model.IsUnlocked)
//            {

//                // 예: UpdateIconPosition(model.Portal.transform.position);
//            }
//        }
//    }
//}
using DG.Tweening;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField] private float _zoomSpeed = 0.2f;

    [Header("Sensitivity Settings")]
    [Range(0.01f, 1f)]
    [SerializeField] private float _zoomSensitivity = 0.05f; // 줌 속도 (낮을수록 느림)
    [Range(0.1f, 2f)]
    [SerializeField] private float _dragSensitivity = 1.0f; // 드래그 감도 (ScrollRect 제어용)

    private List<Map_PortalIcon> _portalIcons = new List<Map_PortalIcon>();
    private PortalController _portalController;
    private Transform _playerTransform;
    private Vector3 _initialContentScale;

    public void Initialize(PortalController portalController, Transform player)
    {
        _portalController = portalController;
        _playerTransform = player;
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
        // 포탈 위치를 UI 좌표로 변환하여 배치
        icon.GetComponent<RectTransform>().anchoredPosition = WorldToMapPos(model.Portal.transform.position);

        _portalIcons.Add(icon);
    }

    private void Update()
    {
        if (!_mapPanel.activeSelf || _playerTransform == null) return;

        // 플레이어 마커 실시간 업데이트 (지형 위에 있으므로 매 프레임 위치 갱신)
        _playerIcon.anchoredPosition = WorldToMapPos(_playerTransform.position);

        // 플레이어 회전값 연동
        float rotZ = -_playerTransform.eulerAngles.y;
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
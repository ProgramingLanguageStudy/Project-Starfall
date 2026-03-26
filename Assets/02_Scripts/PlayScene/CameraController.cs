using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 카메라 관련 모든 로직을 중앙에서 처리. 순간이동·플레이어 변경·커서 상태에 따른 카메라 제어.
/// PlayScene에서 연결하여 사용하며, CursorController와 PlayScene의 카메라 로직을 통합.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Cinemachine")]
    [Tooltip("씬의 메인 CinemachineCamera")]
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    [Header("커서 제어")]
    [Tooltip("커서 숨김 후 이 프레임 수만큼 입력 무시")]
    [SerializeField] [Min(0)] private int _ignoreLookFramesAfterLock = 2;

    private float _savedPanValue;
    private float _savedTiltValue;
    private int _framesToIgnoreRemaining;
    private CinemachineInputAxisController _inputAxisController;

    private void Awake()
    {
        // CinemachineCamera가 할당되지 않았다면 자동 찾기
        if (_cinemachineCamera == null)
        {
            _cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        }
    }

    private void Update()
    {
        if (_framesToIgnoreRemaining > 0)
        {
            _framesToIgnoreRemaining--;
            if (_framesToIgnoreRemaining == 0)
            {
                var controller = GetInputAxisController();
                if (controller != null)
                    controller.enabled = true;
            }
        }
    }

    /// <summary>
    /// 순간이동 시 카메라 위치 즉시 이동. 맵 노출 문제 방지.
    /// </summary>
    /// <param name="destination">카메라가 이동할 목적지</param>
    public void HandleTeleport(Vector3 destination)
    {
        if (_cinemachineCamera != null)
        {
            StartCoroutine(TeleportSequence(destination));
        }
    }
    
    private IEnumerator TeleportSequence(Vector3 destination)
    {
        // 1. Cinemachine 완전 비활성화
        _cinemachineCamera.enabled = false;
        
        // 2. 카메라 위치 즉시 이동
        _cinemachineCamera.transform.position = destination;
        
        // 3. 2-3프레임 대기 (안정화 시간)
        yield return null;
        yield return null;
        
        // 4. Cinemachine 다시 활성화
        _cinemachineCamera.enabled = true;
    }

    /// <summary>
    /// 플레이어 변경 시 Follow 타겟 설정.
    /// </summary>
    /// <param name="target">카메라가 따라갈 타겟 Transform</param>
    public void SetFollowTarget(Transform target)
    {
        if (_cinemachineCamera != null)
        {
            _cinemachineCamera.Follow = target;
        }
    }

    /// <summary>
    /// 카메라 활성화/비활성화. 초기화 시 사용.
    /// </summary>
    /// <param name="active">카메라 활성화 여부</param>
    public void SetCameraActive(bool active)
    {
        if (_cinemachineCamera != null)
        {
            _cinemachineCamera.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// 커서 표시 상태 변경 시 카메라 제어. CursorController에서 호출.
    /// </summary>
    /// <param name="showCursor">커서 표시 여부</param>
    public void HandleCursorStateChange(bool showCursor)
    {
        var panTilt = GetPanTilt();
        var controller = GetInputAxisController();
        
        if (panTilt != null)
        {
            if (showCursor)
            {
                SaveLookValues(panTilt);
                if (controller != null)
                    controller.enabled = false;
            }
            else
            {
                RestoreLookValues(panTilt);
                if (_ignoreLookFramesAfterLock > 0)
                {
                    _framesToIgnoreRemaining = _ignoreLookFramesAfterLock;
                    if (controller != null)
                        controller.enabled = false;
                }
                else if (controller != null)
                {
                    controller.enabled = true;
                }
            }
        }
    }

    private CinemachineInputAxisController GetInputAxisController()
    {
        if (_inputAxisController == null && _cinemachineCamera != null)
            _inputAxisController = _cinemachineCamera.GetComponent<CinemachineInputAxisController>();
        return _inputAxisController;
    }

    private CinemachinePanTilt GetPanTilt()
    {
        if (_cinemachineCamera == null)
            _cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        return _cinemachineCamera != null ? _cinemachineCamera.GetComponent<CinemachinePanTilt>() : null;
    }

    private void SaveLookValues(CinemachinePanTilt panTilt)
    {
        _savedPanValue = panTilt.PanAxis.Value;
        _savedTiltValue = panTilt.TiltAxis.Value;
    }

    private void RestoreLookValues(CinemachinePanTilt panTilt)
    {
        var pan = panTilt.PanAxis;
        pan.Value = _savedPanValue;
        panTilt.PanAxis = pan;

        var tilt = panTilt.TiltAxis;
        tilt.Value = _savedTiltValue;
        panTilt.TiltAxis = tilt;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// 커서 표시/숨김을 한 곳에서만 처리. GameEvents OnCursorShowRequested/OnCursorHideRequested를 ref count로 구독.
/// UI 열릴 때 카메라 회전값 저장, 닫을 때 복원하여 스핀 방지.
/// </summary>
public class CursorController : MonoBehaviour
{
    [Header("카메라")]
    [Tooltip("CinemachineCamera. 같은 GameObject의 CinemachinePanTilt(PanAxis/TiltAxis)에서 회전값 저장/복원")]
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    [Header("UI 닫을 때")]
    [Tooltip("복원 후 이 프레임 수만큼 InputAxisController 비활성화. 복원값이 덮어쓰기 방지. 2~3 권장")]
    [SerializeField] [Min(0)] private int _ignoreLookFramesAfterLock = 2;

    private int _showRequestCount;
    private int _framesToIgnoreRemaining;
    private float _savedPanValue;
    private float _savedTiltValue;
    private CinemachineInputAxisController _inputAxisController;

    private void Start()
    {
        ApplyCursorState(false);
    }

    private void OnEnable()
    {
        GameEvents.OnCursorShowRequested += HandleShowRequested;
        GameEvents.OnCursorHideRequested += HandleHideRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnCursorShowRequested -= HandleShowRequested;
        GameEvents.OnCursorHideRequested -= HandleHideRequested;
    }

    private void HandleShowRequested()
    {
        _showRequestCount++;
        ApplyCursorState(true);
    }

    private void HandleHideRequested()
    {
        if (_showRequestCount > 0)
            _showRequestCount--;
        ApplyCursorState(_showRequestCount > 0);
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

    private void ApplyCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;

        var panTilt = GetPanTilt();
        var controller = GetInputAxisController();
        if (panTilt != null)
        {
            if (visible)
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

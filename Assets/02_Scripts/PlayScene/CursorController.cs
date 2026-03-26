using System;
using UnityEngine;

/// <summary>
/// 커서 표시/숨김을 한 곳에서만 처리. GameEvents OnCursorShowRequested/OnCursorHideRequested를 ref count로 구독.
/// InputHandler가 FreeCursor(Alt)·UI에서 이벤트 발행 → 여기서 커서 상태 변경 이벤트 발행.
/// </summary>
public class CursorController : MonoBehaviour
{
    /// <summary>커서 상태 변경 이벤트. PlayScene에서 CameraController에 연결.</summary>
    public event Action<bool> OnCursorStateChanged;

    private int _showRequestCount;

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
        ApplyCursorState(ShouldShowCursor());
    }

    private void HandleHideRequested()
    {
        if (_showRequestCount > 0)
            _showRequestCount--;
        ApplyCursorState(ShouldShowCursor());
    }

    private bool ShouldShowCursor() => _showRequestCount > 0;

    private void ApplyCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        
        // 카메라에 상태 변경 알림
        OnCursorStateChanged?.Invoke(visible);
    }
}

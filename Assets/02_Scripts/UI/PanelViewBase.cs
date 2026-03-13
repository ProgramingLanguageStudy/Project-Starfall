using UnityEngine;

/// <summary>
/// 패널을 띄우고 닫을 때 커서 표시/숨김을 자동으로 요청하는 View 베이스.
/// IBlocksInput, IRespondsToEsc 구현. InputHandler 연동 시 Esc로 닫기, 입력 차단 지원.
/// 상속 후 패널 열 때 OpenPanel(), 닫을 때 ClosePanel()만 호출하면 됨.
/// </summary>
public abstract class PanelViewBase : MonoBehaviour, IBlocksInput, IRespondsToEsc
{
    private bool _isOpen;

    /// <summary> 패널이 열려 있는지. IBlocksInput 구현. </summary>
    public bool IsOpen => _isOpen;

    /// <summary> Esc로 닫기. IRespondsToEsc 구현. </summary>
    public void Close() => ClosePanel();

    /// <summary> 패널을 연다. 커서 표시 요청 후 OnPanelOpened() 호출. </summary>
    protected void OpenPanel()
    {
        _isOpen = true;
        GameEvents.OnCursorShowRequested?.Invoke();
        OnPanelOpened();
    }

    /// <summary> 패널을 닫는다. OnPanelClosed() 호출 후 커서 숨김 요청. </summary>
    protected void ClosePanel()
    {
        _isOpen = false;
        OnPanelClosed();
        GameEvents.OnCursorHideRequested?.Invoke();
    }

    /// <summary> 패널 활성화 등 실제 표시 처리. 서브클래스에서 구현. </summary>
    protected abstract void OnPanelOpened();

    /// <summary> 패널 비활성화 등 실제 숨김 처리. 서브클래스에서 구현. </summary>
    protected abstract void OnPanelClosed();
}

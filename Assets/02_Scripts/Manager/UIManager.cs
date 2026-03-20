using UnityEngine;

/// <summary>
/// 전역 UI 관리. ErrorPanel, SceneTransitionLoading 등. GameManager 하위, DontDestroyOnLoad.
/// 자식 계층(예: Canvas 아래)에 뷰를 배치하고 Awake에서만 찾는다. 런타임 생성 없음.
/// </summary>
public class UIManager : MonoBehaviour
{
    private ErrorPanelView _errorPanelView;
    private SceneTransitionLoadingView _transitionView;

    private void Awake()
    {
        _errorPanelView = GetComponentInChildren<ErrorPanelView>(true);
        _transitionView = GetComponentInChildren<SceneTransitionLoadingView>(true);
    }

    #region ErrorPanel

    /// <summary>에러 메시지 표시.</summary>
    public void ShowError(string message)
    {
        if (_errorPanelView == null)
        {
            Debug.LogError("[UIManager] ErrorPanelView is null. UIManager 자식(예: Canvas 아래)에 배치했는지 확인.");
            return;
        }
        _errorPanelView.Show(message);
    }

    /// <summary>에러 패널 숨김.</summary>
    public void HideError()
    {
        if (_errorPanelView == null)
        {
            Debug.LogError("[UIManager] ErrorPanelView is null.");
            return;
        }
        _errorPanelView.Hide();
    }

    #endregion

    #region SceneTransition

    /// <summary>씬 전환 로딩 UI 표시. SceneLoadManager 등에서 호출.</summary>
    public void ShowSceneTransition()
    {
        if (_transitionView == null)
        {
            Debug.LogError("[UIManager] SceneTransitionLoadingView is null. UIManager 자식(예: Canvas 아래)에 배치했는지 확인.");
            return;
        }
        _transitionView.Show();
    }

    /// <summary>씬 전환 로딩 UI 숨김.</summary>
    public void HideSceneTransition()
    {
        if (_transitionView == null)
        {
            Debug.LogError("[UIManager] SceneTransitionLoadingView is null.");
            return;
        }
        _transitionView.Hide();
    }

    /// <summary>로딩바·상태 문구 갱신.</summary>
    public void UpdateSceneTransitionProgress(float? progress, string status)
    {
        if (_transitionView == null)
        {
            Debug.LogError("[UIManager] SceneTransitionLoadingView is null. UpdateSceneTransitionProgress skipped.");
            return;
        }
        _transitionView.UpdateProgress(progress, status);
    }

    /// <summary>디버그용. 없으면 null.</summary>
    public SceneTransitionLoadingView GetTransitionView() => _transitionView;

    #endregion
}

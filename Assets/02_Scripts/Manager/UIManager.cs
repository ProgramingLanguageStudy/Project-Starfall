using UnityEngine;

/// <summary>
/// 전역 UI 관리. ErrorPanel, SceneTransitionLoading 등. GameManager 하위, DontDestroyOnLoad.
/// RM에서 로드 후 인스턴스 유지. Show/Hide는 SetActive로 제어.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("RM 경로 (Addressables)")]
    [SerializeField] [Tooltip("에러 패널 프리팹. 예: UI/ErrorPanel")]
    private string _errorPanelPath = "UI/ErrorPanel";
    [SerializeField] [Tooltip("씬 전환 로딩 프리팹. 예: UI/SceneTransitionLoading")]
    private string _transitionPanelPath = "UI/SceneTransitionLoading";

    private GameObject _errorPanelInstance;
    private ErrorPanelView _errorPanelView;
    private GameObject _transitionPanelInstance;
    private SceneTransitionLoadingView _transitionView;

    #region ErrorPanel

    /// <summary>에러 메시지 표시. 첫 호출 시 RM에서 로드 후 인스턴스 유지.</summary>
    public void ShowError(string message)
    {
        EnsureErrorPanel();
        _errorPanelView?.Show(message);
    }

    /// <summary>에러 패널 숨김.</summary>
    public void HideError()
    {
        _errorPanelView?.Hide();
    }

    private void EnsureErrorPanel()
    {
        if (_errorPanelView != null) return;

        var gm = GameManager.Instance;
        if (gm?.ResourceManager == null) return;

        var prefab = gm.ResourceManager.GetPrefab(_errorPanelPath);
        if (prefab == null)
        {
            Debug.LogWarning("[UIManager] ErrorPanel 프리팹 없음: " + _errorPanelPath);
            return;
        }

        _errorPanelInstance = Instantiate(prefab, transform);
        _errorPanelView = _errorPanelInstance.GetComponent<ErrorPanelView>();
        if (_errorPanelView == null)
            _errorPanelView = _errorPanelInstance.GetComponentInChildren<ErrorPanelView>(true);
        if (_errorPanelView == null)
            Debug.LogWarning("[UIManager] ErrorPanelView 컴포넌트 없음.");
    }

    #endregion

    #region SceneTransition

    /// <summary>씬 전환 로딩 뷰. SceneLoadManager 등에서 사용. 첫 호출 시 RM에서 로드.</summary>
    public SceneTransitionLoadingView GetTransitionView()
    {
        EnsureTransitionPanel();
        return _transitionView;
    }

    private void EnsureTransitionPanel()
    {
        if (_transitionView != null) return;

        var gm = GameManager.Instance;
        if (gm?.ResourceManager == null) return;

        var prefab = gm.ResourceManager.GetPrefab(_transitionPanelPath);
        if (prefab == null)
        {
            Debug.LogWarning("[UIManager] SceneTransition 프리팹 없음: " + _transitionPanelPath);
            return;
        }

        _transitionPanelInstance = Instantiate(prefab, transform);
        _transitionView = _transitionPanelInstance.GetComponent<SceneTransitionLoadingView>();
        if (_transitionView == null)
            _transitionView = _transitionPanelInstance.GetComponentInChildren<SceneTransitionLoadingView>(true);
        if (_transitionView == null)
            Debug.LogWarning("[UIManager] SceneTransitionLoadingView 컴포넌트 없음.");
    }

    #endregion
}

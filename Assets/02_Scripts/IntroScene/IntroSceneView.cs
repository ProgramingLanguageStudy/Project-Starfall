using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Intro 씬 UI 조율. LoginView, ErrorView, MainView, LoadingView를 조합해 흐름 제어.
/// </summary>
public class IntroSceneView : MonoBehaviour
{
    #region Fields

    [Header("배경")]
    [SerializeField] private RectTransform _backgroundImage;

    [Header("하위 View")]
    [SerializeField] private TitleView _titleView;
    [SerializeField] private LoginView _loginView;
    [SerializeField] private ErrorView _errorView;
    [SerializeField] private MainView _mainView;
    [SerializeField] private LoadingView _loadingView;

    [Header("전환 연출")]
    [SerializeField] [Min(0.5f)] private float _transitionDuration = 1.2f;
    [SerializeField] [Min(1f)] private float _transitionScaleEnd = 1.2f;

    public event Action OnGameStartRequested;
    public event Action OnLogoutRequested;
    public event Action<string, string> OnLoginRequested;
    public event Action<string, string> OnSignUpRequested;
    public event Action OnGuestPlayRequested;

    #endregion

    #region Initialize

    public void Initialize()
    {
        if (_titleView != null) _titleView.Initialize();
        if (_loginView != null) _loginView.Initialize();
        if (_errorView != null) _errorView.Initialize();
        if (_mainView != null) _mainView.Initialize();
        if (_loadingView != null) _loadingView.Initialize();

        if (_mainView != null)
        {
            _mainView.OnGameStartRequested += () => OnGameStartRequested?.Invoke();
            _mainView.OnLogoutRequested += () => OnLogoutRequested?.Invoke();
        }
        if (_loginView != null)
        {
            _loginView.OnLoginRequested += (e, p) => OnLoginRequested?.Invoke(e, p);
            _loginView.OnSignUpRequested += (e, p) => OnSignUpRequested?.Invoke(e, p);
            _loginView.OnGuestPlayRequested += () => OnGuestPlayRequested?.Invoke();
        }
    }

    #endregion

    #region Title

    public void ShowTitle(Action onComplete = null)
    {
        if (_titleView != null) _titleView.Show(onComplete);
    }

    #endregion

    #region Login / Main

    public void ShowLoginPanel()
    {
        if (_loginView != null) _loginView.Show();
    }

    public void HideLoginPanel()
    {
        if (_loginView != null) _loginView.Hide();
        if (_errorView != null) _errorView.Hide();
    }

    public void ShowMainPanel()
    {
        if (_mainView != null) _mainView.Show();
        HideLoginPanel();
    }

    public void HideMainPanel()
    {
        if (_mainView != null) _mainView.Hide();
    }

    public void SetLoginInteractable(bool interactable)
    {
        if (_loginView != null) _loginView.SetInteractable(interactable);
    }

    #endregion

    #region Error

    public void ShowErrorPanel(string message)
    {
        if (_errorView != null) _errorView.Show(message);
    }

    public void HideErrorPanel()
    {
        if (_errorView != null) _errorView.Hide();
    }

    #endregion

    #region Loading

    public void ShowLoading(bool clearText = true)
    {
        HideMainPanel();
        if (_loadingView != null) _loadingView.Show(clearText);
    }

    public void HideLoading(Action onComplete = null)
    {
        if (_loadingView != null)
            _loadingView.Hide(onComplete);
        else
            onComplete?.Invoke();
    }

    public void UpdateProgress(float? progress, string status)
    {
        if (_loadingView != null) _loadingView.UpdateProgress(progress, status);
    }

    #endregion

    #region Transition to Play

    /// <summary>패널 숨김 → 배경 확대 연출 → onComplete 호출.</summary>
    public void PlayTransitionToPlayScene(Action onComplete)
    {
        StartCoroutine(TransitionRoutine(onComplete));
    }

    private IEnumerator TransitionRoutine(Action onComplete)
    {
        SetPanelsActive(false);

        if (_backgroundImage == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        var rect = _backgroundImage;
        var startScale = Vector3.one;
        var endScale = Vector3.one * _transitionScaleEnd;
        var duration = _transitionDuration;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            t = 1f - (1f - t) * (1f - t); // ease-out
            rect.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        rect.localScale = endScale;
        onComplete?.Invoke();
    }

    private void SetPanelsActive(bool active)
    {
        if (_titleView != null) _titleView.gameObject.SetActive(active);
        if (_loginView != null) _loginView.gameObject.SetActive(active);
        if (_errorView != null) _errorView.gameObject.SetActive(active);
        if (_mainView != null) _mainView.gameObject.SetActive(active);
        if (_loadingView != null) _loadingView.gameObject.SetActive(active);
    }

    #endregion
}

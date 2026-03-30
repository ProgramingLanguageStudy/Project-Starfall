using System;
using System.Collections;
using UnityEngine;

public class IntroScene : MonoBehaviour
{
    [SerializeField] IntroSceneView _introSceneView;

    /// <summary>씬 첫 프레임 표시 후 발생. 시작 시 켠 전환 뷰를 숨길 때 사용.</summary>
    public static event Action OnSceneReady;

    /// <summary>타이틀 연출 종료 후 true. 로그인/로딩 패널은 이 시점 이후에만 표시.</summary>
    private bool _titleSequenceFinished;

    private void Start()
    {
        _introSceneView.Initialize();
        _introSceneView.OnGameStartRequested += HandleGameStartRequested;
        _introSceneView.OnLogoutRequested += HandleLogoutRequested;
        _introSceneView.OnLoginRequested += HandleLoginRequested;
        _introSceneView.OnSignUpRequested += HandleSignUpRequested;
        _introSceneView.OnGuestPlayRequested += HandleGuestPlayRequested;

        var gm = GameManager.Instance;
        if (gm?.FirebaseAuthManager != null)
            gm.FirebaseAuthManager.SessionChanged += OnAuthSessionChangedForIntro;

        StartCoroutine(RunIntroSequence());
    }

    private void OnDestroy()
    {
        _introSceneView.OnGameStartRequested -= HandleGameStartRequested;
        _introSceneView.OnLogoutRequested -= HandleLogoutRequested;
        _introSceneView.OnLoginRequested -= HandleLoginRequested;
        _introSceneView.OnSignUpRequested -= HandleSignUpRequested;
        _introSceneView.OnGuestPlayRequested -= HandleGuestPlayRequested;

        var gm = GameManager.Instance;
        if (gm?.FirebaseAuthManager != null)
            gm.FirebaseAuthManager.SessionChanged -= OnAuthSessionChangedForIntro;
    }

    private void OnAuthSessionChangedForIntro(FirebaseAuthSessionSnapshot _)
    {
        if (!_titleSequenceFinished)
            return;
        var gm = GameManager.Instance;
        if (gm == null || !gm.BootServicesReady)
            return;
        var auth = gm.FirebaseAuthManager;
        if (auth == null || !auth.IsInitializeComplete)
            return;
        ApplyAuthUiFromSnapshot(auth);
    }

    private IEnumerator RunIntroSequence()
    {
        OnSceneReady?.Invoke();
        var titleDone = false;
        _introSceneView.ShowTitle(() => titleDone = true);
        yield return new WaitUntil(() => titleDone);
        _titleSequenceFinished = true;
        yield return null;

        var gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("[IntroScene] GameManager.Instance is null.");
            yield break;
        }

        var auth = gameManager.FirebaseAuthManager;
        if (auth == null)
        {
            Debug.LogError("[IntroScene] FirebaseAuthManager is null.");
            _introSceneView.ShowLoginPanel();
            _introSceneView.ShowErrorPanel("FirebaseAuthManager를 찾을 수 없습니다.");
            yield break;
        }

        if (auth.IsInitializeComplete && gameManager.BootServicesReady)
        {
            ApplyAuthUiFromSnapshot(auth);
            yield break;
        }

        _introSceneView.ShowLoading(clearText: false);
        yield return new WaitUntil(() =>
            auth != null &&
            auth.IsInitializeComplete &&
            gameManager != null &&
            gameManager.BootServicesReady);
        _introSceneView.HideLoading(() => ApplyAuthUiFromSnapshot(auth));
    }

    /// <summary>FirebaseAuthManager.LastSnapshot Phase에 맞춰 로그인/메인/에러 UI 반영.</summary>
    private void ApplyAuthUiFromSnapshot(FirebaseAuthManager auth)
    {
        if (auth == null)
        {
            Debug.LogError("[IntroScene] ApplyAuthUiFromSnapshot: auth is null.");
            return;
        }

        switch (auth.LastSnapshot.Phase)
        {
            case FirebaseAuthLifecyclePhase.InitFailed:
                _introSceneView.ShowLoginPanel();
                var msg = string.IsNullOrEmpty(auth.LastSnapshot.InitError)
                    ? "Firebase 초기화에 실패했습니다."
                    : auth.LastSnapshot.InitError;
                _introSceneView.ShowErrorPanel(msg);
                return;
            case FirebaseAuthLifecyclePhase.ReadyLoggedIn:
                _introSceneView.ShowMainPanel();
                return;
            case FirebaseAuthLifecyclePhase.ReadyGuest:
                _introSceneView.ShowLoginPanel();
                return;
            default:
                return;
        }
    }

    private void HandleGameStartRequested()
    {
        _introSceneView.PlayTransitionToPlayScene(() =>
        {
            var slm = GameManager.Instance?.SceneLoadManager;
            if (slm == null)
            {
                Debug.LogError("[IntroScene] SceneLoadManager is null.");
                return;
            }
            slm.LoadPlayScene();
        });
    }

    private void HandleLogoutRequested()
    {
        var auth = GameManager.Instance?.FirebaseAuthManager;
        if (auth == null)
        {
            Debug.LogError("[IntroScene] FirebaseAuthManager is null.");
            return;
        }
        auth.SignOut();
        _introSceneView.HideMainPanel();
        _introSceneView.ShowLoginPanel();
    }

    private void HandleLoginRequested(string email, string password)
    {
        var auth = GameManager.Instance?.FirebaseAuthManager;
        if (auth == null)
        {
            Debug.LogError("[IntroScene] FirebaseAuthManager is null.");
            _introSceneView.ShowErrorPanel("인증 서비스를 찾을 수 없습니다.");
            return;
        }

        if (!auth.IsInitializeComplete)
        {
            Debug.LogError("[IntroScene] FirebaseAuth not initialized.");
            _introSceneView.ShowErrorPanel("인증 서비스가 준비되지 않았습니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        _introSceneView.SetLoginInteractable(false);

        try
        {
            auth.SignIn(email, password,
                onSuccess: () =>
                {
                    _introSceneView.SetLoginInteractable(true);
                    _introSceneView.HideLoginPanel();
                    _introSceneView.ShowMainPanel();
                },
                onError: msg =>
                {
                    _introSceneView.SetLoginInteractable(true);
                    _introSceneView.ShowErrorPanel(msg);
                });
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IntroScene] Login request exception: {ex.Message}");
            _introSceneView.SetLoginInteractable(true);
            _introSceneView.ShowErrorPanel("로그인 요청 중 오류가 발생했습니다.");
        }
    }

    private void HandleSignUpRequested(string email, string password)
    {
        var auth = GameManager.Instance?.FirebaseAuthManager;
        if (auth == null)
        {
            Debug.LogError("[IntroScene] FirebaseAuthManager is null.");
            return;
        }

        _introSceneView.SetLoginInteractable(false);

        auth.SignUp(email, password,
            onSuccess: () =>
            {
                _introSceneView.SetLoginInteractable(true);
                _introSceneView.HideLoginPanel();
                _introSceneView.ShowMainPanel();
            },
            onError: msg =>
            {
                _introSceneView.SetLoginInteractable(true);
                _introSceneView.ShowErrorPanel(msg);
            });
    }

    /// <summary>이메일 없이 진행. ReadyGuest·InitFailed(로컬 폴백)에서 메인으로 전환.</summary>
    private void HandleGuestPlayRequested()
    {
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[IntroScene] GameManager.Instance is null.");
            return;
        }
        if (!gm.BootServicesReady)
        {
            Debug.LogWarning("[IntroScene] Guest play: boot not ready yet.");
            return;
        }

        var auth = gm.FirebaseAuthManager;
        if (auth == null)
        {
            Debug.LogError("[IntroScene] FirebaseAuthManager is null.");
            return;
        }

        var phase = auth.LastSnapshot.Phase;
        if (phase != FirebaseAuthLifecyclePhase.ReadyGuest && phase != FirebaseAuthLifecyclePhase.InitFailed)
        {
            Debug.LogWarning("[IntroScene] Guest play ignored: Phase is " + phase + " (이미 로그인됨 등).");
            return;
        }

        _introSceneView.HideErrorPanel();
        _introSceneView.HideLoginPanel();
        _introSceneView.ShowMainPanel();
    }

}

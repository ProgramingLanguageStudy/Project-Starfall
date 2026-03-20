using System;
using System.Collections;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Firebase Auth 래퍼. 외부에는 <see cref="SessionChanged"/>와 <see cref="LastSnapshot"/>만으로 상태를 전달.
/// </summary>
public class FirebaseAuthManager : MonoBehaviour
{
    #region [Internal State]

    private FirebaseAuth _auth;

    /// <summary>의존성 통과 후 <see cref="FirebaseAuth.DefaultInstance"/>를 받아 Auth API를 쓸 수 있는지. 실패 시 false 유지.</summary>
    private bool _isInitialized;

    /// <summary><see cref="FirebaseAuth.StateChanged"/>에 핸들러를 붙였는지. 중복 구독 방지 및 <see cref="OnDestroy"/>에서 해제 판별.</summary>
    private bool _nativeStateHooked;

    /// <summary><see cref="RecordInitResult"/>가 호출되어 초기화 시도 결과가 한 번이라도 기록됐는지. <see cref="BuildSnapshot"/>에서 Initializing 분기.</summary>
    private bool _initCompleted;

    /// <summary>마지막으로 기록된 초기화 시도가 성공인지. false면 <see cref="FirebaseAuthLifecyclePhase.InitFailed"/>와 <see cref="_initError"/> 사용.</summary>
    private bool _initSuccess;
    private string _initError = string.Empty;

    #endregion

    #region [Public API]

    /// <summary>가장 최근에 발행된 세션 스냅샷. 구독 전에는 default(Phase=Initializing).</summary>
    public FirebaseAuthSessionSnapshot LastSnapshot { get; private set; }

    /// <summary>초기화 완료·SDK 상태 변화 시마다 동일 형식으로 통지.</summary>
    public event Action<FirebaseAuthSessionSnapshot> SessionChanged;

    /// <summary><see cref="InitializeAsync"/> 코루틴이 한 번이라도 끝났는지(성공·실패 무관). 부트 집계용.</summary>
    public bool IsInitializeComplete { get; private set; }

    /// <summary>Firebase 의존성 체크 및 초기화. 완료 시 <see cref="SessionChanged"/>로만 결과 전달.</summary>
    public IEnumerator InitializeAsync()
    {
        // 같은 플레이 세션에서 재진입: DefaultInstance는 이미 있음 → 구독만 보강하고 스냅샷 재발행.
        if (_isInitialized)
        {
            // 로그인/로그아웃 등 이후에도 StateChanged로 스냅샷 갱신되도록.
            SubscribeFirebaseAuthStateChanged();
            // 첫 부트 결과는 이미 성공으로 기록된 상태로 간주.
            RecordInitResult(true, null);
            PublishSession();
            IsInitializeComplete = true;
            yield break;
        }

        // 모바일 등에서 Play Services·의존성 설치/복구. 비동기 Task → 코루틴에서 끝날 때까지 한 프레임씩 양보.
        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return task.WaitUntilComplete();

        // Firebase Core를 쓸 수 없으면 Auth 인스턴스도 없음 → InitFailed 스냅샷만 발행하고 종료.
        if (task.Result != DependencyStatus.Available)
        {
            Debug.LogError($"[FirebaseAuthManager] Firebase dependency error: {task.Result}");
            var err = "Firebase 초기화 실패. 재시도해 주세요.";
            RecordInitResult(false, err);
            PublishSession();
            IsInitializeComplete = true;
            yield break;
        }

        // 의존성 통과 후 단일 진입점으로 Auth 핸들 확보. 이 시점부터 SignIn 등 API 호출 가능.
        _auth = FirebaseAuth.DefaultInstance;
        _isInitialized = true;

        // SDK가 로그인/로그아웃 시 알려 주는 이벤트 → 우리 쪽 SessionChanged로 다시 퍼뜨림.
        SubscribeFirebaseAuthStateChanged();

        // 첫 초기화 시도 성공을 내부에 기록한 뒤, ReadyGuest 또는 ReadyLoggedIn 스냅샷 발행.
        RecordInitResult(true, null);
        PublishSession();
        IsInitializeComplete = true;
    }

    public void SignIn(string email, string password, Action onSuccess, Action<string> onError)
    {
        if (!PreCheck(email, password, onError)) return;

        _auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task => HandleAuthTask(task, "로그인", onSuccess, onError));
    }

    public void SignUp(string email, string password, Action onSuccess, Action<string> onError)
    {
        if (!PreCheck(email, password, onError)) return;

        _auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task => HandleAuthTask(task, "회원가입", onSuccess, onError));
    }

    public void SignOut() => _auth?.SignOut();

    #endregion

    #region [Unity Lifecycle]

    private void OnDestroy()
    {
        if (_auth != null && _nativeStateHooked)
        {
            _auth.StateChanged -= OnFirebaseAuthStateChanged;
            _nativeStateHooked = false;
        }
    }

    #endregion

    #region [Private Helpers]

    /// <summary>
    /// Firebase SDK의 StateChanged — 정보는 얇지만, 로그인/로그아웃 시 반드시 올라와
    /// <see cref="PublishSession"/>으로 풍부한 스냅샷을 다시 만들기 위해 필요.
    /// </summary>
    private void SubscribeFirebaseAuthStateChanged()
    {
        if (_auth == null || _nativeStateHooked) return;
        _auth.StateChanged += OnFirebaseAuthStateChanged;
        _nativeStateHooked = true;
    }

    private void OnFirebaseAuthStateChanged(object sender, EventArgs e) => PublishSession();

    private void RecordInitResult(bool success, string error)
    {
        _initCompleted = true;
        _initSuccess = success;
        _initError = error ?? string.Empty;
    }

    private void PublishSession()
    {
        var snap = BuildSnapshot();
        LastSnapshot = snap;
        SessionChanged?.Invoke(snap);
    }

    private FirebaseAuthSessionSnapshot BuildSnapshot()
    {
        if (!_initCompleted)
            return new FirebaseAuthSessionSnapshot(FirebaseAuthLifecyclePhase.Initializing, string.Empty, null, null);

        if (!_initSuccess)
            return new FirebaseAuthSessionSnapshot(FirebaseAuthLifecyclePhase.InitFailed, _initError, null, null);

        var user = _auth?.CurrentUser;
        if (user != null)
        {
            return new FirebaseAuthSessionSnapshot(
                FirebaseAuthLifecyclePhase.ReadyLoggedIn,
                string.Empty,
                user.UserId,
                user.Email);
        }

        return new FirebaseAuthSessionSnapshot(FirebaseAuthLifecyclePhase.ReadyGuest, string.Empty, null, null);
    }

    /// <summary>
    /// SignIn/SignUp 호출 전 검사. 이메일·비밀번호 클라이언트 검증 후, SDK 미초기화면 Firebase 호출 없이 onError 후 false.
    /// </summary>
    private bool PreCheck(string email, string password, Action<string> onError)
    {
        var validationError = GetValidationError(email, password);
        if (validationError != null)
        {
            onError?.Invoke(validationError);
            return false;
        }
        if (!_isInitialized)
        {
            onError?.Invoke("Firebase 초기화 실패. 재시도해 주세요.");
            return false;
        }
        return true;
    }

    private void HandleAuthTask(Task<AuthResult> task, string action, Action onSuccess, Action<string> onError)
    {
        if (task.IsCanceled)
        {
            onError?.Invoke($"{action}이 취소되었습니다.");
            return;
        }
        if (task.IsFaulted)
        {
            onError?.Invoke(GetAuthErrorMessage(task.Exception));
            return;
        }
        onSuccess?.Invoke();
    }

    private static string GetValidationError(string email, string password)
    {
        if (string.IsNullOrEmpty(email)) return "이메일을 입력하세요.";
        if (string.IsNullOrEmpty(password)) return "비밀번호를 입력하세요.";
        if (password.Length < 6) return "비밀번호는 6자 이상이어야 합니다.";
        return null;
    }

    private static string GetAuthErrorMessage(Exception ex)
    {
        FirebaseException firebaseEx = null;
        var currentEx = ex;
        while (currentEx != null)
        {
            if (currentEx is FirebaseException fe)
            {
                firebaseEx = fe;
                break;
            }
            currentEx = currentEx.InnerException;
        }

        if (firebaseEx != null)
        {
            var errorCode = (AuthError)firebaseEx.ErrorCode;
            switch (errorCode)
            {
                case AuthError.InvalidEmail:
                    return "이메일 형식이 올바르지 않습니다.";
                case AuthError.UserNotFound:
                case AuthError.WrongPassword:
                case AuthError.InvalidCredential:
                    return "이메일 또는 비밀번호가 일치하지 않습니다.";
                case AuthError.EmailAlreadyInUse:
                    return "이미 사용 중인 이메일입니다.";
                case AuthError.WeakPassword:
                    return "비밀번호가 너무 취약합니다. (6자 이상)";
                case AuthError.TooManyRequests:
                    return "시도가 너무 많습니다. 잠시 후 다시 시도해 주세요.";
                case AuthError.NetworkRequestFailed:
                    return "네트워크 연결 상태를 확인해 주세요.";
                default:
                    return $"오류가 발생했습니다. (에러 코드: {errorCode})";
            }
        }

        return "알 수 없는 오류가 발생했습니다. 다시 시도해 주세요.";
    }

    #endregion
}

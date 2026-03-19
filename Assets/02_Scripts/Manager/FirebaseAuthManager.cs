using System;
using System.Collections;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Firebase Auth 래퍼. GameManager가 보유하며, 로그인/회원가입/로그아웃 및 인증 상태 제공.
/// FirebaseAuth.DefaultInstance를 단일 접근점으로 감싸, 검증·에러 메시지 변환을 담당.
/// </summary>
public class FirebaseAuthManager : MonoBehaviour
{
    #region [Internal State]

    private FirebaseAuth _auth;
    private bool _isInitialized;

    #endregion

    #region [Public API]

    /// <summary>Firebase 초기화 완료 여부. 초기화 전 SignIn/SignUp 호출 시 PreCheck에서 차단.</summary>
    public bool IsReady => _isInitialized;

    /// <summary>현재 로그인된 사용자 존재 여부.</summary>
    public bool IsLoggedIn => _auth?.CurrentUser != null;

    /// <summary>로그인된 사용자 UID. Firestore 경로 등에 사용. 미로그인 시 null.</summary>
    public string UserUID => _auth?.CurrentUser?.UserId;

    /// <summary>로그인된 사용자 이메일. 미로그인 시 null.</summary>
    public string UserEmail => _auth?.CurrentUser?.Email;

    /// <summary>
    /// Firebase 의존성 체크 및 초기화. IEnumerator로 DataManager/ResourceManager와 동일 패턴.
    /// </summary>
    /// <param name="onProgress">진행 상태 문구. 예: "로그인 확인중...", "확인 완료"</param>
    /// <param name="onComplete">success: 초기화 성공, hasUser: 이미 로그인됨, errorMessage: 실패 시 메시지</param>
    public IEnumerator InitializeAsync(
        Action<string> onProgress,
        Action<bool, bool, string> onComplete)
    {
        onProgress?.Invoke("로그인 확인중...");

        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return task.WaitUntilComplete();

        if (task.Result != DependencyStatus.Available)
        {
            Debug.LogError($"[FirebaseAuthManager] Firebase dependency error: {task.Result}");
            onComplete?.Invoke(false, false, "Firebase 초기화 실패. 재시도해 주세요.");
            yield break;
        }

        _auth = FirebaseAuth.DefaultInstance;
        _isInitialized = true;

        var user = _auth.CurrentUser;
        var statusMsg = user != null ? "로그인 완료" : "확인 완료";
        onProgress?.Invoke(statusMsg);
        onComplete?.Invoke(true, user != null, null);
    }

    /// <summary>이메일/비밀번호 로그인. PreCheck 후 Firebase 호출. 콜백은 메인 스레드에서 실행.</summary>
    public void SignIn(string email, string password, Action onSuccess, Action<string> onError)
    {
        if (!PreCheck(email, password, onError)) return;

        _auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task => HandleAuthTask(task, "로그인", onSuccess, onError));
    }

    /// <summary>이메일/비밀번호 회원가입. PreCheck 후 Firebase 호출. 콜백은 메인 스레드에서 실행.</summary>
    public void SignUp(string email, string password, Action onSuccess, Action<string> onError)
    {
        if (!PreCheck(email, password, onError)) return;

        _auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task => HandleAuthTask(task, "회원가입", onSuccess, onError));
    }

    /// <summary>로그아웃. CurrentUser를 null로 만듦.</summary>
    public void SignOut() => _auth?.SignOut();

    #endregion

    #region [Private Helpers]

    /// <summary>클라이언트 검증 + 초기화 여부. 실패 시 onError 호출 후 false.</summary>
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

    /// <summary>SignIn/SignUp Task 결과 처리. 취소/실패 시 onError, 성공 시 onSuccess.</summary>
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

    /// <summary>이메일/비밀번호 클라이언트 검증. Firebase 호출 전 즉시 피드백.</summary>
    private static string GetValidationError(string email, string password)
    {
        if (string.IsNullOrEmpty(email)) return "이메일을 입력하세요.";
        if (string.IsNullOrEmpty(password)) return "비밀번호를 입력하세요.";
        if (password.Length < 6) return "비밀번호는 6자 이상이어야 합니다.";
        return null;
    }

    /// <summary>Firebase Auth API 에러를 사용자용 메시지로 변환. 로그인/회원가입 공통.</summary>
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

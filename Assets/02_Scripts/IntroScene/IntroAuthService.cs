using System;
using System.Collections;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Intro 씬용 Firebase 인증 서비스. View 독립, 콜백으로 통신.
/// </summary>
public class IntroAuthService
{
    private bool _firebaseReady;

    public bool IsReady => _firebaseReady;

    /// <summary>Firebase 초기화 및 로그인 여부 확인.</summary>
    /// <param name="onProgress">progress: null이면 % 미표시, 0~1이면 % 표시. status: 상태 문구.</param>
    /// <param name="onComplete">success: Firebase 초기화 성공, hasUser: 로그인됨, errorMessage: 실패 시 메시지.</param>
    public IEnumerator InitializeAsync(
        Action<float?, string> onProgress,
        Action<bool, bool, string> onComplete)
    {
        onProgress?.Invoke(null, "로그인 확인중...");

        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result != DependencyStatus.Available)
        {
            Debug.LogError($"[IntroAuthService] Firebase dependency error: {task.Result}");
            onComplete?.Invoke(false, false, "Firebase 초기화 실패. 재시도해 주세요.");
            yield break;
        }

        _firebaseReady = true;
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        var statusMsg = user != null ? "로그인 완료" : "확인 완료";
        onProgress?.Invoke(null, statusMsg);
        onComplete?.Invoke(true, user != null, null);
    }

    public void SignIn(string email, string password, Action onSuccess, Action<string> onError)
    {
        var validationError = GetValidationError(email, password);
        if (validationError != null)
        {
            onError?.Invoke(validationError);
            return;
        }
        if (!_firebaseReady)
        {
            onError?.Invoke("Firebase 초기화 실패. 재시도해 주세요.");
            return;
        }

        FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task => HandleAuthTask(task, "로그인", onSuccess, onError));
    }

    public void SignUp(string email, string password, Action onSuccess, Action<string> onError)
    {
        var validationError = GetValidationError(email, password);
        if (validationError != null)
        {
            onError?.Invoke(validationError);
            return;
        }
        if (!_firebaseReady)
        {
            onError?.Invoke("Firebase 초기화 실패. 재시도해 주세요.");
            return;
        }

        FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task => HandleAuthTask(task, "회원가입", onSuccess, onError));
    }

    public void SignOut()
    {
        FirebaseAuth.DefaultInstance.SignOut();
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

    private static string GetAuthErrorMessage(AggregateException ex)
    {
        if (ex?.InnerExceptions == null || ex.InnerExceptions.Count == 0)
            return "오류가 발생했습니다. 다시 시도해 주세요.";

        var inner = ex.InnerExceptions[0];
        var authError = (inner as FirebaseException)?.ErrorCode ?? (inner.InnerException as FirebaseException)?.ErrorCode;

        if (authError.HasValue)
        {
            switch ((AuthError)authError.Value)
            {
                case AuthError.InvalidEmail:
                    return "올바른 이메일 형식이 아닙니다.";
                case AuthError.WrongPassword:
                case AuthError.InvalidCredential:
                case AuthError.UserNotFound:
                    return "이메일 또는 비밀번호가 올바르지 않습니다.";
                case AuthError.EmailAlreadyInUse:
                    return "이미 사용 중인 이메일입니다.";
                case AuthError.WeakPassword:
                    return "비밀번호는 6자 이상이어야 합니다.";
                case AuthError.TooManyRequests:
                    return "요청이 너무 많습니다. 잠시 후 다시 시도해 주세요.";
            }
        }

        var msg = (inner.InnerException?.Message ?? inner.Message ?? string.Empty).ToLowerInvariant();
        if (msg.Contains("invalid") && msg.Contains("email")) return "올바른 이메일 형식이 아닙니다.";
        if (msg.Contains("badly formatted") || msg.Contains("bad format")) return "올바른 이메일 형식이 아닙니다.";
        if (msg.Contains("wrong_password") || msg.Contains("invalid_credential") || msg.Contains("user_not_found"))
            return "이메일 또는 비밀번호가 올바르지 않습니다.";
        if (msg.Contains("email_exists") || msg.Contains("email_already_in_use")) return "이미 사용 중인 이메일입니다.";
        if (msg.Contains("weak_password")) return "비밀번호는 6자 이상이어야 합니다.";

        return "오류가 발생했습니다. 다시 시도해 주세요.";
    }
}

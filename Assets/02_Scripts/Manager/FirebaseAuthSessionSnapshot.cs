/// <summary>Firebase Auth + 첫 초기화까지의 단계. bool 여러 개 대신 한 축으로 표현.</summary>
public enum FirebaseAuthLifecyclePhase
{
    /// <summary>첫 InitializeAsync가 아직 끝나지 않음. 세이브 I/O 대기.</summary>
    Initializing,

    /// <summary>첫 초기화 시도는 끝났으나 실패(의존성 등). SDK 미사용.</summary>
    InitFailed,

    /// <summary>SDK 준비됨, 현재 미로그인(게스트).</summary>
    ReadyGuest,

    /// <summary>SDK 준비됨, 로그인됨.</summary>
    ReadyLoggedIn
}

/// <summary>
/// FirebaseAuthManager가 외부에 알리는 상태. <see cref="Phase"/>만 보면 완료/성공/준비/로그인을 한 번에 판별.
/// </summary>
public readonly struct FirebaseAuthSessionSnapshot
{
    public FirebaseAuthLifecyclePhase Phase { get; }
    /// <summary><see cref="FirebaseAuthLifecyclePhase.InitFailed"/>일 때 사용자 표시용.</summary>
    public string InitError { get; }
    public string UserId { get; }
    public string UserEmail { get; }

    public FirebaseAuthSessionSnapshot(
        FirebaseAuthLifecyclePhase phase,
        string initError,
        string userId,
        string userEmail)
    {
        Phase = phase;
        InitError = initError ?? string.Empty;
        UserId = userId;
        UserEmail = userEmail;
    }

    /// <summary>첫 부트 시도가 끝났는지(성공·실패 무관).</summary>
    public bool IsInitFinished => Phase != FirebaseAuthLifecyclePhase.Initializing;
}

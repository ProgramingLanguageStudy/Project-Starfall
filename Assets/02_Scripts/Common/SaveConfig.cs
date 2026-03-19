/// <summary>
/// 세이브 관련 상수. SaveManager 등에서 사용.
/// </summary>
public static class SaveConfig
{
    /// <summary>5분 주기 자동 저장 간격(초).</summary>
    public const float PeriodicSaveIntervalSec = 300f;

    /// <summary>앱 종료 시 저장 대기 타임아웃(초).</summary>
    public const float QuitSaveTimeoutSec = 5f;
}

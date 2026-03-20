/// <summary>
/// 세이브 관련 상수. SaveManager 등에서 사용.
/// </summary>
public static class SaveConfig
{
    /// <summary>앱 종료 시 저장 대기 타임아웃(초).</summary>
    public const float QuitSaveTimeoutSec = 5f;

    /// <summary><see cref="SaveAccessPhase.Pending"/>에서 로드 시 Auth·백엔드 확정까지 대기하는 최대 시간(초).</summary>
    public const float LoadWaitForBackendTimeoutSec = 60f;
}

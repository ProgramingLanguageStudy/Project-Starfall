/// <summary>
/// 세이브 I/O가 어떤 의미로 열려 있는지. Pending일 때는 백엔드 없음(디스크/클라우드 미접근).
/// </summary>
public enum SaveAccessPhase
{
    /// <summary>Firebase 첫 InitializeAsync 종료 전. Save/Delete는 미수행. Load는 <see cref="SaveManager.LoadAsync"/>에서 백엔드 확정까지 대기.</summary>
    Pending,

    /// <summary>게스트·강제 로컬·Auth 없음·Firebase 초기화 실패 등 로컬 파일 기준.</summary>
    LocalGuest,

    /// <summary>로그인·UID 확정 후 클라우드(및 동일 규칙의 원격) 백엔드.</summary>
    Cloud
}

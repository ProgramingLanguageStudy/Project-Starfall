using System;
using System.Collections;

/// <summary>
/// 세이브 저장소 백엔드. Firestore, 로컬 파일 등 구현체가 있음.
/// 코루틴 기반. 완료 시 onComplete 콜백 호출.
/// </summary>
public interface ISaveBackend
{
    /// <summary>비동기 로드. 없거나 실패 시 onComplete(null).</summary>
    IEnumerator LoadAsync(Action<SaveData> onComplete);

    /// <summary>비동기 저장. 성공 시 onComplete(true).</summary>
    IEnumerator SaveAsync(SaveData data, Action<bool> onComplete);

    /// <summary>세이브 삭제. 디버그/테스트용.</summary>
    IEnumerator DeleteAsync(Action<bool> onComplete);
}

using System.Threading.Tasks;

/// <summary>
/// 세이브 저장소 백엔드. Firestore, 로컬 파일 등 구현체가 있음.
/// </summary>
public interface ISaveBackend
{
    /// <summary>비동기 저장. 성공 시 true.</summary>
    Task<bool> SaveAsync(SaveData data);

    /// <summary>비동기 로드. 없거나 실패 시 null.</summary>
    Task<SaveData> LoadAsync();

    /// <summary>세이브 삭제. 디버그/테스트용.</summary>
    Task<bool> DeleteAsync();
}

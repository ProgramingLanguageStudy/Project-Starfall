/// <summary>
/// Addressables 관련 상수 (경로, 라벨). ResourceManager·DataManager에서 사용.
/// </summary>
public static class AddressableConfig
{
    /// <summary>프리팹 주소 prefix. 이 뒤 경로가 캐시 키가 됨. 예: Assets/00_Prefabs/UI/CharacterProfile.prefab → UI/CharacterProfile</summary>
    public const string PrefabPrefix = "Assets/00_Prefabs/";

    /// <summary>프리팹 라벨. ResourceManager가 이 라벨로 일괄 로드.</summary>
    public const string PrefabLabel = "Prefab";

    /// <summary>SO 데이터 라벨. DataManager가 이 라벨로 일괄 로드.</summary>
    public const string DataLabel = "Data";
}

/// <summary>
/// 분대원 1명의 저장 데이터. ID + 수치(체력 등)만 저장. 위치는 플레이어 기준으로 로드 시 재배치.
/// </summary>
[System.Serializable]
public class CharacterMemberData
{
    public string id = "";
    /// <summary>캐릭터 레벨. 스탯은 레벨 기반으로 파생 계산.</summary>
    public int level = 1;
    public int currentHp;
    /// <summary>UI 슬롯 인덱스 (0~3). 로드 시 해당 슬롯에 배치.</summary>
    public int slotIndex;
}

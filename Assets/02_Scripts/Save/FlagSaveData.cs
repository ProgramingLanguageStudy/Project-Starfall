using System.Collections.Generic;

/// <summary>
/// 플래그 저장용. JsonUtility는 Dictionary 미지원이라 keys/values 리스트로 직렬화.
/// </summary>
[System.Serializable]
public class FlagSaveData
{
    public List<string> keys = new List<string>();
    public List<int> values = new List<int>();
}

using UnityEngine;

/// <summary>
/// 영입 퀘스트. 완료 시 recruitCharacterId로 분대에 캐릭터 추가. 대화 중이던 NPC는 컨텍스트로 비활성화.
/// </summary>
[CreateAssetMenu(fileName = "NewRecruitmentQuest", menuName = "Quest/RecruitmentQuestData")]
public class RecruitmentQuestData : QuestData
{
    [Header("영입 보상")]
    [Tooltip("분대에 추가할 캐릭터 ID. DataManager/Resources Characters와 매핑")]
    public string recruitCharacterId = "";

    private void OnValidate()
    {
        QuestType = QuestType.Recruitment;
    }
}

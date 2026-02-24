using UnityEngine;

/// <summary>
/// 퀘스트 완료 시 quest_{id}_완료 플래그 설정. IQuestCompletedHandler 구현, QuestCompletedRegistry에 등록.
/// </summary>
public class QuestCompletedFlagHandler : MonoBehaviour, IQuestCompletedHandler
{
    private void Awake()
    {
        PlaySceneServices.QuestCompleted.Register(this);
    }

    private void OnDestroy()
    {
        PlaySceneServices.QuestCompleted.Unregister(this);
    }

    public void OnQuestCompleted(QuestData data)
    {
        if (data == null || string.IsNullOrEmpty(data.QuestId)) return;

        GameManager.Instance?.FlagManager?.SetFlag(GameStateKeys.QuestCompleted(data.QuestId), 1);
    }
}

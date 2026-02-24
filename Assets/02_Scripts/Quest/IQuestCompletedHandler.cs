/// <summary>
/// 퀘스트 완료 시 처리. QuestCompletedRegistry에 등록.
/// </summary>
public interface IQuestCompletedHandler
{
    void OnQuestCompleted(QuestData data);
}

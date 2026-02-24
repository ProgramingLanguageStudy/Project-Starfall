using UnityEngine;

/// <summary>
/// QuestSystem.OnQuestUpdated 구독. 목표 달성 시 QuestObjectivesDone 플래그 설정.
/// 대화 선택(QuestComplete)에서 "목표 달성" 여부 확인용.
/// </summary>
public class QuestFlagSync : MonoBehaviour
{
    [SerializeField] private QuestSystem _questSystem;
    [SerializeField] private FlagManager _flagManager;

    private void OnEnable()
    {
        if (_questSystem != null)
            _questSystem.OnQuestUpdated += HandleQuestUpdated;
    }

    private void OnDisable()
    {
        if (_questSystem != null)
            _questSystem.OnQuestUpdated -= HandleQuestUpdated;
    }

    private void HandleQuestUpdated(QuestModel quest)
    {
        if (quest == null || _flagManager == null) return;
        if (quest.IsCompleted)
            _flagManager.SetFlag(GameStateKeys.QuestObjectivesDone(quest.QuestId), 1);
    }
}

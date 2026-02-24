using UnityEngine;

/// <summary>
/// 대화 종료 시 퀘스트 수락/완료 처리. IDialogueEndedHandler 구현, DialogueEndedRegistry에 등록.
/// Complete 시 CompleteQuest만 호출 → OnQuestCompleted 발행 → QuestCompleted.InvokeAll로 핸들러들에게 전달.
/// PlayScene 컴포넌트. QuestPresenter를 보유.
/// </summary>
public class QuestController : MonoBehaviour, IDialogueEndedHandler
{
    [SerializeField] private QuestPresenter _presenter;

    private QuestSystem QuestSystem => _presenter != null ? _presenter.System : null;

    private void Awake()
    {
        PlaySceneServices.DialogueEnded.Register(this);
    }

    private void OnDestroy()
    {
        PlaySceneServices.DialogueEnded.Unregister(this);
    }

    private void OnEnable()
    {
        if (QuestSystem != null)
            QuestSystem.OnQuestCompleted += HandleQuestCompleted;
    }

    private void OnDisable()
    {
        if (QuestSystem != null)
            QuestSystem.OnQuestCompleted -= HandleQuestCompleted;
    }

    public void OnDialogueEnded(DialogueData data)
    {
        if (data == null || string.IsNullOrEmpty(data.questId)) return;

        switch (data.questDialogueType)
        {
            case QuestDialogueType.Complete:
                HandleQuestComplete(data);
                break;
            case QuestDialogueType.Accept:
                HandleQuestAccept(data);
                break;
            case QuestDialogueType.InProgress:
            case QuestDialogueType.None:
                break;
        }
    }

    private void HandleQuestComplete(DialogueData data)
    {
        if (data == null || string.IsNullOrEmpty(data.questId) || _presenter == null) return;

        _presenter.RequestCompleteQuest(data.questId);
    }

    private void HandleQuestCompleted(QuestData data)
    {
        PlaySceneServices.QuestCompleted.InvokeAll(data);
    }

    private void HandleQuestAccept(DialogueData data)
    {
        if (data == null || string.IsNullOrEmpty(data.questId) || _presenter == null) return;

        _presenter.RequestAcceptQuest(data.questId);
    }
}

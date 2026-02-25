using System.Text;
using UnityEngine;

/// <summary>
/// 퀘스트 디버그/테스트용. Hierarchy의 Debuggers 등에 붙이고, 인스펙터에서 참조 할당.
/// 진행 중 퀘스트 목록 확인·삭제에 사용.
/// </summary>
public class QuestDebugger : MonoBehaviour
{
    [SerializeField] [Tooltip("비어 있으면 씬에서 FindObjectOfType으로 탐색")]
    private QuestPresenter _questPresenter;
    [SerializeField] [Tooltip("퀘스트 삭제 시 accepted/objectives_done 플래그 초기화용")]
    private FlagSystem _flagSystem;

    public QuestPresenter QuestPresenterRef => _questPresenter;

    private void OnValidate()
    {
        if (_questPresenter == null && Application.isPlaying)
            _questPresenter = FindAnyObjectByType<QuestPresenter>();
        if (_flagSystem == null && Application.isPlaying)
            _flagSystem = FindAnyObjectByType<FlagSystem>();
    }

    /// <summary>진행 중 퀘스트 목록을 콘솔에 출력.</summary>
    public void LogActiveQuests()
    {
        var qs = GetQuestSystem();
        if (qs == null) return;

        var quests = qs.GetActiveQuests();
        if (quests == null || quests.Count == 0)
        {
            Debug.Log("[QuestDebugger] 진행 중인 퀘스트 없음.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[QuestDebugger] 진행 중 퀘스트:");
        foreach (var q in quests)
            sb.AppendLine($"  {q.QuestId} ({q.CurrentAmount}/{q.TargetAmount}) - {q.Title}");
        Debug.Log(sb.ToString());
    }

    /// <summary>지정 퀘스트를 목록에서 제거. 플래그(accepted, objectives_done)도 초기화.</summary>
    public void RemoveQuest(string questId)
    {
        var qs = GetQuestSystem();
        var fs = GetFlagSystem();
        if (qs == null || fs == null) return;
        if (string.IsNullOrEmpty(questId)) return;

        if (!qs.RemoveQuest(questId))
        {
            Debug.Log($"[QuestDebugger] 퀘스트 없음: {questId}");
            return;
        }

        fs.SetFlag(GameStateKeys.QuestAccepted(questId), 0);
        fs.SetFlag(GameStateKeys.QuestObjectivesDone(questId), 0);
        Debug.Log($"[QuestDebugger] 퀘스트 제거: {questId}");
    }

    /// <summary>진행 중 퀘스트 전체 제거. 관련 플래그(accepted, objectives_done)도 초기화.</summary>
    public void ClearAllQuests()
    {
        var qs = GetQuestSystem();
        var fs = GetFlagSystem();
        if (qs == null || fs == null) return;

        var quests = qs.GetActiveQuests();
        foreach (var q in quests)
        {
            fs.SetFlag(GameStateKeys.QuestAccepted(q.QuestId), 0);
            fs.SetFlag(GameStateKeys.QuestObjectivesDone(q.QuestId), 0);
        }
        qs.ClearAllQuests();
        Debug.Log("[QuestDebugger] 진행 중 퀘스트 전체 제거 완료.");
    }

    private QuestSystem GetQuestSystem()
    {
        if (_questPresenter == null)
            _questPresenter = FindAnyObjectByType<QuestPresenter>();
        if (_questPresenter?.System == null)
            Debug.LogWarning("[QuestDebugger] QuestPresenter를 찾을 수 없습니다. Play 씬에 QuestPresenter가 있는지 확인하세요.");
        return _questPresenter?.System;
    }

    private FlagSystem GetFlagSystem()
    {
        if (_flagSystem == null)
            _flagSystem = FindAnyObjectByType<FlagSystem>();
        if (_flagSystem == null)
            Debug.LogWarning("[QuestDebugger] FlagSystem을 찾을 수 없습니다. 퀘스트 삭제 시 플래그 동기화가 되지 않습니다.");
        return _flagSystem;
    }
}

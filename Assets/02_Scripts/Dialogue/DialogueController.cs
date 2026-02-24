using UnityEngine;

/// <summary>
/// 대화 흐름. OnNpcInteracted → Select → Presenter. 대화 종료 시 Registry로 라우팅.
/// </summary>
public class DialogueController : MonoBehaviour, IDialogueEndedHandler
{
    [SerializeField] [Tooltip("OnNpcInteracted → Select → Presenter.RequestStartDialogue")]
    private DialogueSelector _selector;
    [SerializeField] [Tooltip("대화 UI Model↔View")]
    private DialoguePresenter _presenter;

    private void OnEnable()
    {
        if (PlaySceneServices.EventHub != null && _selector != null && _presenter != null)
            PlaySceneServices.EventHub.OnNpcInteracted += HandleNpcInteracted;
        if (_presenter != null)
            _presenter.OnDialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        if (PlaySceneServices.EventHub != null && _selector != null && _presenter != null)
            PlaySceneServices.EventHub.OnNpcInteracted -= HandleNpcInteracted;
        if (_presenter != null)
            _presenter.OnDialogueEnded -= HandleDialogueEnded;
    }

    private void HandleNpcInteracted(string npcId)
    {
        var data = _selector != null ? _selector.Select(npcId) : null;
        if (data != null)
            _presenter.RequestStartDialogue(data);
    }

    private void HandleDialogueEnded(DialogueData data)
    {
        PlaySceneServices.DialogueEnded.InvokeAll(data);
    }

    void IDialogueEndedHandler.OnDialogueEnded(DialogueData data) { }
}

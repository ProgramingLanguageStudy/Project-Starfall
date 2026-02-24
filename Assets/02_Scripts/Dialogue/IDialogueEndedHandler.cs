/// <summary>
/// 대화 종료 시 처리. DialogueEndedRegistry에 등록.
/// </summary>
public interface IDialogueEndedHandler
{
    void OnDialogueEnded(DialogueData data);
}

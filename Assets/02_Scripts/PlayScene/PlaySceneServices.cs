/// <summary>
/// Play 씬 전용 서비스 레지스트리. PlayScene에서 Register 후 전역 접근. OnDisable에서 Clear 호출.
/// 전투 상태는 CombatController(주입)가 진실의 원천.
/// </summary>
public static class PlaySceneServices
{
    private static IPlayerProvider _playerProvider;
    private static PlaySceneEventHub _eventHub;

    private static readonly DialogueEndedRegistry _dialogueEnded = new();
    private static readonly QuestCompletedRegistry _questCompleted = new();

    public static IPlayerProvider Player => _playerProvider;
    public static PlaySceneEventHub EventHub => _eventHub;
    public static DialogueEndedRegistry DialogueEnded => _dialogueEnded;
    public static QuestCompletedRegistry QuestCompleted => _questCompleted;

    public static void Register(IPlayerProvider provider)
    {
        _playerProvider = provider;
    }

    public static void RegisterEventHub(PlaySceneEventHub hub)
    {
        _eventHub = hub;
    }

    public static void Clear()
    {
        _playerProvider = null;
        _eventHub = null;
        _dialogueEnded.Clear();
        _questCompleted.Clear();
    }

    public static void ClearEventHub()
    {
        _eventHub = null;
    }
}

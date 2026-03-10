using System;

public static class GameEvents
{
    /// <summary>어떤 ID(아이템, 몬스터, 장소)를 얼마만큼 진행했는지 알림 (조율층에서 QuestSystem.NotifyProgress 호출용)</summary>
    public static Action<string, int> OnQuestGoalProcessed;

    /// <summary>커서 보이기 요청. UI 열릴 때 발행. CursorController가 ref count로 처리.</summary>
    public static Action OnCursorShowRequested;

    /// <summary>커서 숨기기 요청. UI 닫힐 때 발행. CursorController가 ref count로 처리.</summary>
    public static Action OnCursorHideRequested;

    /// <summary>아이템 획득. 월드 아이템(ItemObject)이 발행만 함. Inventory 등이 구독해 AddItem 호출.</summary>
    public static Action<ItemData, int> OnItemPickedUp;

    /// <summary>골드 획득(적 처치 등). 팝업 표시용.</summary>
    public static Action<int> OnGoldAcquired;

    /// <summary>상호작용 대상 변경. Interactor가 발행, InteractionUI 등이 구독해 표시.</summary>
    public static Action<IInteractable> OnInteractTargetChanged;

    /// <summary>캐릭터 부활 요청. CharacterDeadState가 발행. RespawnController가 구독.</summary>
    public static Action<Character> OnCharacterRespawnRequested;
}

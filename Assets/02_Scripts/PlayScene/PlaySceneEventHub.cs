using System;

/// <summary>
/// Play 씬 전용 static 이벤트 허브. GameEvents처럼 Play 씬 컴포넌트들이 이벤트 연결.
/// PlayScene.OnDisable에서 Clear 호출.
/// </summary>
public static class PlaySceneEventHub
{
    /// <summary>NPC와 상호작용됨. Npc가 발행(npcId). DialogueController가 구독.</summary>
    public static Action<string> OnNpcInteracted;

    /// <summary>적 처치. Enemy.HandleDeath에서 발행. QuestController가 구독해 NotifyProgress 호출.</summary>
    public static Action<string> OnEnemyKilled;

    /// <summary>Play 씬 언로드 시 호출. 구독자 클리어.</summary>
    public static void Clear()
    {
        OnNpcInteracted = null;
        OnEnemyKilled = null;
    }
}

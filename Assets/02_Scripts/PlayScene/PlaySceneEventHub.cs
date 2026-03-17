using System;
using UnityEngine;

/// <summary>
/// Play 씬 전용 static 이벤트 허브. GameEvents처럼 Play 씬 컴포넌트들이 이벤트 연결.
/// PlayScene.OnDisable에서 Clear 호출.
/// </summary>
public static class PlaySceneEventHub
{
    /// <summary>NPC와 상호작용됨. Npc가 발행(npcId). DialogueController가 구독.</summary>
    public static Action<string> OnNpcInteracted;

    /// <summary>적 처치. Enemy.HandleDeath에서 발행. QuestController(NotifyProgress)·EnemyGoldRewardHandler(골드) 등이 구독.</summary>
    public static Action<Enemy> OnEnemyKilled;

    /// <summary>데미지 적용됨. (실제 데미지량, 피격자, 월드 좌표, 공격자). EffectManager·SoundManager가 구독.</summary>
    public static Action<int, IDamageable, Vector3, Transform> OnDamageDealt;

    /// <summary>Play 씬 언로드 시 호출. 구독자 클리어.</summary>
    public static void Clear()
    {
        OnNpcInteracted = null;
        OnEnemyKilled = null;
        OnDamageDealt = null;
    }
}

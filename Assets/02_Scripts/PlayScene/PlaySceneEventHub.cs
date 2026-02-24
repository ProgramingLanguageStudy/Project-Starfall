using System;
using UnityEngine;

/// <summary>
/// Play 씬 전용 이벤트 허브. 씬 로드 시 PlaySceneServices에 등록, 언로드 시 Clear.
/// Play 씬에서만 사용되는 이벤트를 모름. static GameEvents 대신 씬 생명주기에 묶임.
/// </summary>
public class PlaySceneEventHub : MonoBehaviour
{
    /// <summary>NPC와 상호작용됨. Npc가 발행(npcId만 전달). DialogueController가 구독.</summary>
    public event Action<string> OnNpcInteracted;

    private void Awake()
    {
        PlaySceneServices.RegisterEventHub(this);
    }

    private void OnDestroy()
    {
        PlaySceneServices.ClearEventHub();
    }

    /// <summary>NPC 상호작용 발행. Npc 등에서 호출.</summary>
    public void RaiseNpcInteracted(string npcId) => OnNpcInteracted?.Invoke(npcId);
}

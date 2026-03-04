using UnityEngine;

/// <summary>
/// 씬 배치 NPC. npcId 보유. 등록·영입 비활성화는 NpcController가 Initialize 시 관리.
/// </summary>
public class Npc : MonoBehaviour, IInteractable
{
    [SerializeField] private string _npcId;

    public string NpcId => _npcId;

    public void Interact(IInteractReceiver receiver)
    {
        if (!string.IsNullOrEmpty(_npcId))
            PlaySceneEventHub.OnNpcInteracted?.Invoke(_npcId);
    }

    public string GetInteractText() => _npcId;
}

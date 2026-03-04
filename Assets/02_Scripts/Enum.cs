public enum QuestType { Gather, Kill, Visit, Recruitment } // 수집, 처치, 방문, 동료 영입

/// <summary>아이템 종류. 일반 인벤토리에는 Consumable, Material만. 장비는 장비 인벤토리로.</summary>
public enum ItemType { Consumable, Material, Equipment }

/// <summary>플레이어 상태머신용. 캐릭터 동작만. Free에서만 이동/공격/구르기 가능.</summary>
public enum PlayerState
{
    Free,      // 자유 행동 (이동, 공격, 구르기, 상호작용)
    Attacking, // 공격 중
    Rolling,   // 구르기 중
}

public enum DialogueType
{
    FirstTalk,  // 첫 대화용. first_talk_플래그가 0일 때만 선택
    Quest,
    QuestComplete, // 퀘스트 제출 후 완료 대사
    Common,
    Affection,
}

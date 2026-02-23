using UnityEngine;

/// <summary>
/// 플레이어 캐릭터 참조 제공. 거리 계산·타겟 설정 등에 사용.
/// 씬 전용. GameServices에 등록.
/// </summary>
public interface IPlayerProvider
{
    Character GetPlayer();
    Transform GetPlayerTransform();
}

using UnityEngine;

/// <summary>
/// 캐릭터(스쿼드 멤버) 기초 스탯·설정 정의 (디자인 타임).
/// SO로 생성 후 CharacterModel 등에 할당. 저장·로드·스폰 시 모두 이 데이터 사용.
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "Character/Data")]
public class CharacterData : ScriptableObject
{
    [Header("식별")]
    [Tooltip("UI·디버그용 표시 이름")]
    public string displayName = "캐릭터";
    [Tooltip("세이브·해금 등용 고유 ID")]
    public string characterId = "";

    [Header("프리팹")]
    [Tooltip("스폰용 프리팹. 씬 배치 캐릭터도 세이브·로드 시 이 프리팹 기준 인스턴스 제어")]
    public GameObject prefab;

    [Header("UI")]
    [Tooltip("분대 프로필 등에 사용할 초상화 아이콘")]
    public Sprite portrait;

    [Header("생존")]
    [Tooltip("최대 체력")]
    public int maxHp = 100;

    [Header("이동")]
    [Tooltip("이동 속도 (CharacterController 이동에 사용)")]
    public float moveSpeed = 6f;

    [Header("공격")]
    [Tooltip("공격력 (데미지 계산 시 사용)")]
    public int attackPower = 10;
    [Tooltip("공격 속도 배율 (1 = 기본. 애니/쿨다운에 적용)")]
    [Range(0.5f, 2f)]
    public float attackSpeed = 1f;

    [Header("방어")]
    [Tooltip("방어력 (받는 데미지 감소 등)")]
    public int defense = 0;

    [Header("따라가기")]
    [Tooltip("AI 모드: 따라갈 대상과 유지할 목표 거리")]
    public float followDistance = 3f;
    [Tooltip("AI 모드: 이 거리 안이면 정지")]
    public float stopDistance = 1.5f;
    [Tooltip("AI 모드: 뒤쳐졌을 때 추가 속도 배율")]
    public float catchUpSpeed = 1.2f;
}

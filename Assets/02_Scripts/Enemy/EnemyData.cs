using UnityEngine;

/// <summary>
/// Enemy 기초 스탯 정의 (디자인 타임). SO로 생성 후 EnemyModel에 할당.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("생존")]
    [Tooltip("최대 체력")]
    public int maxHp = 50;

    [Header("공격")]
    [Tooltip("공격력 (데미지 계산 시 사용)")]
    public int attackPower = 5;

    [Header("방어")]
    [Tooltip("방어력 (받는 데미지 감소)")]
    public int defense = 0;

    [Header("AI - Patrol")]
    [Tooltip("배회 시 이동 속도")]
    public float patrolSpeed = 1.5f;
    [Tooltip("배회 반경 (스폰 위치 기준)")]
    public float patrolRadius = 5f;
    [Tooltip("도착 판정 거리")]
    public float arriveThreshold = 0.5f;
    [Tooltip("걷기 구간 최소 시간(초)")]
    public float patrolWalkDurationMin = 2f;
    [Tooltip("걷기 구간 최대 시간(초)")]
    public float patrolWalkDurationMax = 3f;
    [Tooltip("쉬기(Idle) 구간 시간(초)")]
    public float patrolIdleDuration = 1f;

    [Header("AI - Aggro")]
    [Tooltip("이 수치 도달 시 전투 진입")]
    public float aggroThreshold = 100f;
    [Tooltip("이 거리 이탈 시 어그로 리셋")]
    public float aggroLoseDistance = 10f;
    [Tooltip("거리 1m일 때 어그로 누적")]
    public float aggroAt1m = 50f;
    [Tooltip("거리 3m일 때 어그로 누적")]
    public float aggroAt3m = 30f;
    [Tooltip("탐지 주기(초)")]
    public float detectInterval = 0.5f;
    [Tooltip("타겟 재평가 주기(초)")]
    public float targetReevalInterval = 1.5f;

    [Header("AI - Chase / Attack")]
    [Tooltip("추적 시 이동 속도")]
    public float chaseSpeed = 4f;
    [Tooltip("탐지 반경. 이 거리 안의 Character에 어그로 누적")]
    public float detectionRadius = 10f;
    [Tooltip("Chase 중 이 거리 안이면 Attack")]
    public float attackRadius = 2f;
    [Tooltip("Chase 중 목표가 이 거리 밖이면 Patrol 복귀")]
    public float chaseLoseRadius = 15f;
    [Tooltip("Attack 상태 유지 시간(초). 끝나면 Chase로")]
    public float attackDuration = 0.6f;
}

using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 사망 상태. Mover 정지, Animator.Dead 호출.
/// </summary>
public class EnemyDeadState : EnemyStateBase
{
    public EnemyDeadState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        // 1. 시각적 처리
        Machine.Enemy.Animator.Dead();

        // 2. 물리/AI 기능 완전 박탈
        Enemy enemy = Machine.Enemy;
        if (enemy != null)
        {
            // 이동 컴포넌트 비활성화
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            // 충돌체 비활성화 (플레이어가 시체에 걸리지 않게)
            Collider collider = enemy.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            // 공격 컴포넌트 기능 정지
            enemy.Attacker.Animation_EndHitWindow();
        }

        // 3. 시스템 등록 해제
        Machine.Enemy.NotifyCombatStateChanged(false);
    }

    public override void Update() { }

    public override void Exit() { }
}

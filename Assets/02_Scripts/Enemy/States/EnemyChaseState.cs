using UnityEngine;

/// <summary>
/// 추적 상태. Mover로 목표 지점 설정, 공격/포기 거리는 Machine(Model)에서 참조.
/// </summary>
public class EnemyChaseState : EnemyStateBase
{
    public EnemyChaseState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        Machine.Enemy?.Animator?.Chase();
        if (Machine.Enemy?.Mover != null)
        {
            Machine.Enemy.Mover.SetSpeed(Machine.ChaseSpeed);
            Machine.Enemy.Mover.Resume();
        }
    }

    public override void Update()
    {
        Transform target = Machine.ChaseTarget;
        if (target == null)
        {
            Machine.ChangeState(EnemyStateMachine.EnemyState.Patrol);
            return;
        }

        Vector3 myPos = Machine.Enemy.transform.position;
        float dist = Vector3.Distance(myPos, target.position);

        if (dist <= Machine.AttackRadius)
        {
            Machine.ChangeState(EnemyStateMachine.EnemyState.Attack);
            return;
        }
        // 전투 종료(플레이어 10m 이탈)는 StateMachine에서 처리

        Machine.Enemy?.Mover?.SetDestination(target.position);
    }

    public override void Exit() { }
}

using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI 동료 이동. NavMeshAgent로 대상(플레이어 등)을 따라감.
/// followDistance 안이면 정지, stopDistance 밖이면 이동. 멀어지면 catchUpSpeed로 속도 증가.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CharacterFollowMover : MonoBehaviour
{
    private NavMeshAgent _agent;
    private CharacterModel _model;
    private Transform _followTarget;
    private Transform _combatTarget;
    private float _combatStopDistance;

    public void Initialize(NavMeshAgent agent, CharacterModel model)
    {
        _agent = agent;
        _model = model;
        if (_agent != null)
            _agent.updateRotation = true;
    }

    /// <summary>따라갈 목표(플레이어 등) 설정. null이면 정지.</summary>
    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
    }

    /// <summary>전투 모드: 이 타겟으로 이동. stopDistance 안이면 정지. null로 클리어 시 다시 follow 사용.</summary>
    public void SetCombatTarget(Transform target, float stopDistance)
    {
        _combatTarget = target;
        _combatStopDistance = Mathf.Max(0f, stopDistance);
    }

    /// <summary>전투 타겟 해제. 다시 follow 대상 따라감.</summary>
    public void ClearCombatTarget()
    {
        _combatTarget = null;
    }

    public float GetCurrentSpeed() => _agent != null && !_agent.isStopped ? _agent.velocity.magnitude : 0f;

    private void Update()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh || _model == null || _model.IsDead)
            return;

        // 전투 타겟이 있으면 우선. 없으면 follow 대상 사용
        Transform activeTarget = _combatTarget != null ? _combatTarget : _followTarget;
        float stopDist = _combatTarget != null ? _combatStopDistance : _model.StopDistance;

        if (activeTarget == null)
        {
            _agent.isStopped = true;
            return;
        }

        Vector3 targetPos = activeTarget.position;
        float distSq = (targetPos - transform.position).sqrMagnitude;
        float stopSq = stopDist * stopDist;
        float followSq = _combatTarget != null ? stopSq + 1f : _model.FollowDistance * _model.FollowDistance;

        if (distSq <= stopSq)
        {
            _agent.isStopped = true;
            _agent.speed = 0f;
            return;
        }

        _agent.isStopped = false;
        float baseSpeed = _model.MoveSpeed;
        _agent.speed = distSq > followSq ? baseSpeed * _model.CatchUpSpeed : baseSpeed;
        _agent.SetDestination(targetPos);
    }
}

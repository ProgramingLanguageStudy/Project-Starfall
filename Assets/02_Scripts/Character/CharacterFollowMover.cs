using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI 동료 전용 이동 컴포넌트. NavMeshAgent 기반.
/// 단순 실행자: 목적지만 받아 NavMeshAgent를 제어.
/// 판단(전투/추적/정지)은 Brain/StateMachine이 담당.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CharacterFollowMover : MonoBehaviour
{
    private NavMeshAgent _agent;
    private CharacterModel _model;

    public void SetCurrentMoveSpeed(float speed)
    {
        _model.SetCurrentMoveSpeed(speed);
    }

    public void Stop()
    {
        if (_agent == null || !_agent.enabled) return;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        _agent.ResetPath();
    }

    private void Update()
    {
        if (_agent == null || !_agent.enabled) return;
        SetCurrentMoveSpeed(_agent.velocity.magnitude);
    }

    // ── 초기화 ──────────────────────────────────────────
    public void Initialize(NavMeshAgent agent, CharacterModel model)
    {
        _agent = agent;
        _model = model;

        if (_agent != null)
            _agent.updateRotation = true;
    }

    // ── 단순 실행 API ───────────────────────────────────
    /// <summary>목적지 설정. stopDistanceOverride가 null이면 Model.StopDistance 사용.</summary>
    public void MoveToTarget(Vector3 targetPos, float? stopDistanceOverride = null)
    {
        if (_agent == null || !_agent.enabled) return;

        _agent.isStopped = false;
        _agent.speed = _model != null ? _model.MoveSpeed : 3.5f;
        _agent.stoppingDistance = stopDistanceOverride ?? (_model != null ? _model.StopDistance : 1f);
        _agent.SetDestination(targetPos);
    }
}

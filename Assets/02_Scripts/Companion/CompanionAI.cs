using UnityEngine;

/// <summary>
/// 동료 전투/따라가기 AI. 플레이어가 아닌 Character에 붙여 사용.
/// Follow: 플레이어 따라감. Combat: 가장 가까운 적 추적·공격.
/// </summary>
[RequireComponent(typeof(Character))]
public class CompanionAI : MonoBehaviour
{
    [Header("전투")]
    [SerializeField] [Tooltip("이 거리 안이면 공격 시도")]
    private float _attackRange = 2f;
    [SerializeField] [Tooltip("타겟 재탐색 주기 (초). CombatController.EnemiesInCombat에서 선택")]
    private float _targetUpdateInterval = 0.5f;

    private Character _character;
    private SquadController _squadController;
    private CombatController _combatController;
    private Enemy _currentTarget;
    private float _lastTargetUpdateTime;

    private void Awake()
    {
        _character = GetComponent<Character>();
    }

    private void OnEnable()
    {
        _squadController = Object.FindFirstObjectByType<SquadController>();
        _combatController = Object.FindFirstObjectByType<CombatController>();
        _currentTarget = null;
        _lastTargetUpdateTime = 0f;
    }

    private void Update()
    {
        if (_character == null || _character.Model == null || _character.Model.IsDead)
            return;

        if (_squadController != null && _squadController.PlayerCharacter == _character)
            return;

        var player = _squadController?.PlayerCharacter;
        Transform followTarget = player != null ? player.transform : null;

        // CombatController.IsInCombat 기준으로 Follow / Combat 분기
        if (_combatController != null && _combatController.IsInCombat)
        {
            if (Time.time - _lastTargetUpdateTime >= _targetUpdateInterval)
            {
                _lastTargetUpdateTime = Time.time;
                _currentTarget = FindNearestFromCombatList();
            }

            if (_currentTarget != null && (_currentTarget.Model == null || _currentTarget.Model.IsDead))
                _currentTarget = null;

            if (_currentTarget != null)
            {
                float dist = Vector3.Distance(transform.position, _currentTarget.transform.position);
                _character.SetCombatTarget(_currentTarget.transform, _attackRange);

                if (dist <= _attackRange && _character.StateMachine != null && _character.StateMachine.IsIdle)
                    _character.RequestAttack();
            }
            else
            {
                _character.ClearCombatTarget();
                _character.SetFollowTarget(followTarget);
            }
        }
        else
        {
            _currentTarget = null;
            _character.ClearCombatTarget();
            _character.SetFollowTarget(followTarget);
        }
    }

    private Enemy FindNearestFromCombatList()
    {
        if (_combatController == null || _combatController.EnemiesInCombat.Count == 0)
            return null;

        Vector3 myPos = transform.position;
        Enemy nearest = null;
        float nearestSq = float.MaxValue;

        foreach (var e in _combatController.EnemiesInCombat)
        {
            if (e == null || e.Model == null || e.Model.IsDead) continue;

            float sq = (e.transform.position - myPos).sqrMagnitude;
            if (sq < nearestSq)
            {
                nearestSq = sq;
                nearest = e;
            }
        }

        return nearest;
    }
}

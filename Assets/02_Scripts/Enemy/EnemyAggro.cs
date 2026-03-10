using UnityEngine;

/// <summary>
/// 적(Enemy) 전투 관련 유틸. 플레이어 도주 시 전투 종료 판단 등.
/// 어그로 수치/임계값 제거. 전투 진입은 Detect/TakeDamage에서 Character 기반으로 처리.
/// </summary>
public class EnemyAggro : MonoBehaviour
{
    private EnemyModel _model;

    public void Initialize(EnemyModel model)
    {
        _model = model;
    }

    /// <summary>플레이어(또는 chase target)가 lose 거리 밖이면 true. 전투 종료 판단용.</summary>
    public bool TryResetIfTargetOutOfRange(Vector3 myPos, Transform targetTransform)
    {
        if (targetTransform == null) return false;
        float dist = Vector3.Distance(myPos, targetTransform.position);
        float loseDist = _model != null ? _model.AggroLoseDistance : 10f;
        if (dist > loseDist)
            return true;
        return false;
    }
}

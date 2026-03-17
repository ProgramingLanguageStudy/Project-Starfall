using UnityEngine;

/// <summary>
/// 이펙트 재생. RM 프리팹 로드 + PoolManager 연동.
/// Play: 이펙트 타입 + 위치. ShowDamageNumber: 데미지 숫자 전용.
/// Play(AttackSlash) 호출처: CharacterAttacker.Begin, EnemyAttacker.OnAttackStarted.
/// </summary>
public class EffectManager : MonoBehaviour
{
    private const string DamageTextCategory = "UI";
    private const string DamageTextName = "DamageText";
    private const string EffectCategory = "Effect";
    private const string HitEffectName = "Hit";

    private GameObject _damageTextPrefab;
    private GameObject _hitEffectPrefab;

    private GameObject GetDamageTextPrefab()
    {
        if (_damageTextPrefab != null) return _damageTextPrefab;
        var rm = GameManager.Instance?.ResourceManager;
        if (rm == null)
        {
            Debug.LogError("[EffectManager] ResourceManager 없음.");
            return null;
        }
        _damageTextPrefab = rm.GetPrefab(DamageTextCategory, DamageTextName);
        if (_damageTextPrefab == null)
            Debug.LogError($"[EffectManager] 프리팹 로드 실패: {DamageTextCategory}/{DamageTextName}");
        return _damageTextPrefab;
    }

    /// <summary>이펙트 재생. RM Effect/Hit + Pool. EffectAutoReturn으로 파티클 끝나면 풀 반환.</summary>
    public void Play(EffectType type, Vector3 position)
    {
        if (type == EffectType.Hit)
        {
            var prefab = GetHitEffectPrefab();
            if (prefab == null) return;

            var go = GetOrCreateEffect(prefab);
            go.transform.position = position;

            if (go.GetComponent<EffectAutoReturn>() == null)
                go.AddComponent<EffectAutoReturn>();
        }
    }

    private GameObject GetHitEffectPrefab()
    {
        if (_hitEffectPrefab != null) return _hitEffectPrefab;
        var rm = GameManager.Instance?.ResourceManager;
        if (rm == null) return null;
        _hitEffectPrefab = rm.GetPrefab(EffectCategory, HitEffectName);
        if (_hitEffectPrefab == null)
            Debug.LogError($"[EffectManager] 프리팹 로드 실패: {EffectCategory}/{HitEffectName}");
        return _hitEffectPrefab;
    }

    private GameObject GetOrCreateEffect(GameObject prefab)
    {
        var pm = GameManager.Instance?.PoolManager;
        if (pm != null)
            return pm.Pop(prefab);
        return Instantiate(prefab);
    }

    /// <summary>데미지 숫자 표시. RM UI/DamageText + Pool. DamageView.Show 호출.</summary>
    public void ShowDamageNumber(int damage, Vector3 position)
    {
        var prefab = GetDamageTextPrefab();
        if (prefab == null) return;

        var go = GetOrCreateDamageText(prefab);
        go.transform.position = position;

        var damageView = go.GetComponent<DamageView>();
        if (damageView != null)
            damageView.Show(damage);
    }

    private GameObject GetOrCreateDamageText(GameObject prefab)
    {
        var pm = GameManager.Instance?.PoolManager;
        if (pm != null)
            return pm.Pop(prefab);
        return Instantiate(prefab);
    }

    private void OnEnable()
    {
        PlaySceneEventHub.OnDamageDealt += HandleDamageDealt;
    }

    private void OnDisable()
    {
        PlaySceneEventHub.OnDamageDealt -= HandleDamageDealt;
    }

    private void HandleDamageDealt(int damage, IDamageable target, Vector3 hitPosition, Transform attacker)
    {
        ShowDamageNumber(damage, hitPosition);
        Play(EffectType.Hit, hitPosition);
    }
}

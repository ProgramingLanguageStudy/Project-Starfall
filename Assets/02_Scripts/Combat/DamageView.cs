using System.Collections;
using UnityEngine;
using CartoonFX;

/// <summary>
/// 데미지 숫자 표시. CFXR_ParticleText 연동. ParticleSystem 끝날 때까지 대기 후 풀 반환.
/// EffectManager가 RM+Pool로 스폰 후 Show(damage) 호출.
/// </summary>
[RequireComponent(typeof(CFXR_ParticleText))]
public class DamageView : MonoBehaviour
{
    [Tooltip("ParticleSystem 대기 최대 시간(초). 무한 대기 방지.")]
    [SerializeField] private float _maxWaitDuration = 10f;

    private CFXR_ParticleText _particleText;

    private void Awake()
    {
        _particleText = GetComponent<CFXR_ParticleText>();
    }

    /// <summary>데미지 숫자 표시. CFXR UpdateText 호출 후 ParticleSystem 끝나면 풀 반환.</summary>
    public void Show(int damage)
    {
        if (_particleText != null && _particleText.isDynamic)
            _particleText.UpdateText(damage.ToString());

        StopAllCoroutines();
        StartCoroutine(WaitForParticlesThenExit());
    }

    private IEnumerator WaitForParticlesThenExit()
    {
        yield return null; // 1프레임 대기 (UpdateText 직후 파티클 시작 허용)
        float elapsed = 0f;
        while (IsAnyParticleAlive() && elapsed < _maxWaitDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        OnExitComplete();
    }

    private bool IsAnyParticleAlive()
    {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps.gameObject.activeInHierarchy && ps.IsAlive())
                return true;
        }
        return false;
    }

    private void OnExitComplete()
    {
        var poolable = GetComponent<Poolable>();
        if (poolable != null)
            poolable.ReturnToPool();
        else
            Destroy(gameObject);
    }
}

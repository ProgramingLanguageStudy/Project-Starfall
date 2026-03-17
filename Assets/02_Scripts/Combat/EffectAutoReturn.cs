using System.Collections;
using UnityEngine;

/// <summary>
/// ParticleSystem 재생 후 끝나면 풀 반환 또는 Destroy. Hit 등 일반 이펙트용.
/// EffectManager가 스폰 시 AddComponent로 붙이거나, 프리팹에 미리 넣어둠.
/// </summary>
public class EffectAutoReturn : MonoBehaviour
{
    [Tooltip("대기 최대 시간(초). 무한 대기 방지.")]
    [SerializeField] private float _maxWaitDuration = 5f;

    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(WaitForParticlesThenExit());
    }

    private IEnumerator WaitForParticlesThenExit()
    {
        yield return null;
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

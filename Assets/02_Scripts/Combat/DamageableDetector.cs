using System;
using UnityEngine;

/// <summary>
/// Trigger에 들어온 IDamageable만 감지해 이벤트로 알림.
/// HitboxController와 같은 GO에 두고, 히트박스 Collider도 같은 GO(또는 자식)에 두면 OnTriggerEnter/Stay 수신됨.
/// OnTriggerStay 추가: 히트박스가 켜질 때 이미 겹쳐 있는 적은 OnTriggerEnter가 안 불리므로, Stay로 보완.
/// </summary>
public class DamageableDetector : MonoBehaviour
{
    /// <summary>Trigger에 들어온 대상을 감지했을 때. HitboxController를 통해 구독.</summary>
    public event Action<IDamageable> OnDamageableDetected;

    private void NotifyIfDamageable(Collider other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            Debug.Log($"[DamageableDetector] {gameObject.name} 감지: {other.gameObject.name} (IDamageable={damageable})");
            OnDamageableDetected?.Invoke(damageable);
        }
    }

    private void OnTriggerEnter(Collider other) => NotifyIfDamageable(other);

    private void OnTriggerStay(Collider other) => NotifyIfDamageable(other);
}

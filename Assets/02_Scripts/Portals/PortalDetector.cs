using System;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PortalDetector : MonoBehaviour
{
    public event Action<bool> OnDetectPlayer;
    private bool _isPlayerInside = false;

    private void OnTriggerStay(Collider other)
    {
        // 물리 매트릭스에서 Player만 걸러주므로 별도의 레이어 체크가 필요 없음!
        if (!_isPlayerInside)
        {
            _isPlayerInside = true;
            OnDetectPlayer?.Invoke(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_isPlayerInside) return; // 이미 false면 무시

        // 플레이어 레이어가 나가거나, 플레이어의 레이어가 'Companion' 등으로 바뀌는 순간 즉시 호출됨
        _isPlayerInside = false;
        OnDetectPlayer?.Invoke(false);
    }
}
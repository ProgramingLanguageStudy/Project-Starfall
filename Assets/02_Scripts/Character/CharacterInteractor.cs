using System;
using UnityEngine;

/// <summary>
/// 캐릭터 주변 상호작용 대상 감지. 반경 내 IInteractable 중 가장 가까운 것을 CurrentTarget으로.
/// </summary>
public class CharacterInteractor : MonoBehaviour
{
    [Header("----- Detection Settings -----")]
    [SerializeField] private Transform _detectPoint;
    [SerializeField] private float _radius = 3f;
    [SerializeField] private float _maxAngle = 180f;
    [SerializeField] private LayerMask _targetLayerMask;
    [SerializeField] private int _overlapBufferSize = 16;

    public IInteractable CurrentTarget { get; private set; }

    private IInteractable _lastTarget;
    private Character _character;
    private Collider[] _overlapBuffer;

    public event Action<IInteractable> OnTargetChanged;

    public void Initialize(Character character)
    {
        _character = character;
        _overlapBuffer = new Collider[_overlapBufferSize];
    }

    private void Update()
    {
        Detect();
    }

    private void Detect()
    {
        Vector3 origin = _detectPoint != null ? _detectPoint.position : transform.position + Vector3.up;
        Vector3 forward = _detectPoint != null ? _detectPoint.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = transform.forward;
        forward.Normalize();

        IInteractable found = null;
        float closestSqr = _radius * _radius;

        int count = Physics.OverlapSphereNonAlloc(origin, _radius, _overlapBuffer, _targetLayerMask);
        for (int i = 0; i < count; i++)
        {
            Collider c = _overlapBuffer[i];
            if (c == null) continue;
            if (!c.transform.TryGetComponent<IInteractable>(out var interactable))
                interactable = c.transform.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            Vector3 toTarget = (c.ClosestPoint(origin) - origin);
            toTarget.y = 0f;
            float sqrDist = toTarget.sqrMagnitude;

            if (_maxAngle < 360f && toTarget.sqrMagnitude >= 0.001f)
            {
                toTarget.Normalize();
                if (Vector3.Angle(forward, toTarget) > _maxAngle * 0.5f) continue;
            }

            if (sqrDist >= closestSqr) continue;
            closestSqr = sqrDist;
            found = interactable;
        }

        if (found != _lastTarget)
        {
            _lastTarget = found;
            CurrentTarget = found;
            OnTargetChanged?.Invoke(found);
            GameEvents.OnInteractTargetChanged?.Invoke(found);
        }
    }

    public void TryInteract()
    {
        if (CurrentTarget != null && _character != null)
            CurrentTarget.Interact(_character);
    }
}

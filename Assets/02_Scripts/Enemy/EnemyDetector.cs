using System;
using UnityEngine;

/// <summary>
/// 주기적으로 반경 내 Character를 탐지. 감지 시 (Character, 거리) 이벤트 발행.
/// Enemy가 구독하여 Aggro 등에 전달.
/// </summary>
[RequireComponent(typeof(EnemyModel))]
public class EnemyDetector : MonoBehaviour
{
    /// <summary>캐릭터 감지 시 (Character, 거리). 구독자가 어그로 누적 등 처리.</summary>
    public event Action<Character, float> OnCharacterDetected;

    private EnemyModel _model;
    private Collider[] _detectBuffer;
    private float _detectTimer;

    public void Initialize(EnemyModel model)
    {
        _model = model;
        _detectBuffer = new Collider[16];
        _detectTimer = _model != null ? _model.DetectInterval : 0.5f;
    }

    private void Update()
    {
        if (_model == null || _model.IsDead) return;

        float interval = _model.DetectInterval;
        _detectTimer -= Time.deltaTime;
        if (_detectTimer <= 0f)
        {
            _detectTimer = interval;
            DetectCharacters();
        }
    }

    private void DetectCharacters()
    {
        float radius = _model.DetectionRadius;
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _detectBuffer);

        for (int i = 0; i < count; i++)
        {
            Collider c = _detectBuffer[i];
            if (c == null) continue;
            Character ch = c.GetComponentInParent<Character>();
            if (ch == null || ch.Model == null || ch.Model.IsDead) continue;

            float dist = Vector3.Distance(transform.position, ch.transform.position);
            OnCharacterDetected?.Invoke(ch, dist);
        }
    }
}

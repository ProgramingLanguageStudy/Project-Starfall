using System;
using UnityEngine;

/// <summary>
/// 플레이어 전용 이동 컴포넌트. CharacterController 기반.
/// 월드 방향을 받아 이동·회전·중력을 처리합니다.
/// 카메라/입력 변환은 호출부(PlayerInputHandler 등)에서 처리.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CharacterMover : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 15f;

    private CharacterController _controller;
    private CharacterModel _model;

    // 중력 누적용 별도 변수. _moveVelocity.y에 섞지 않습니다.
    // 이유: SetDestination/Move 호출 시 수평 벡터가 덮어씌워져도 수직 속도는 유지되어야 함.
    private float _verticalVelocity;

    public event Action<float> OnMoved;

    public void SetCurrentMoveSpeed(float speed)
    {
        _model.SetCurrentMoveSpeed(speed);
    }

    public void Stop()
    {
        // CC는 Move()를 안 부르면 멈춤. 수직 속도는 유지(낙하 중 Stop해도 자연스럽게 착지).
        _verticalVelocity = Mathf.Min(_verticalVelocity, 0f);
    }

    public void Initialize(CharacterController controller, CharacterModel model)
    {
        _controller = controller;
        _model = model;
        _verticalVelocity = 0f;
    }

    /// <summary>
    /// 월드 기준 이동 방향을 넘겨주면 이동·회전·중력을 처리합니다.
    /// 방향이 없으면(zero) 중력만 적용합니다. 정규화 여부 무관.
    /// </summary>
    public void Move(Vector3 worldDirection)
    {
        if (_controller == null || !_controller.enabled) return;

        ApplyGravity();

        bool hasInput = worldDirection.sqrMagnitude >= 0.01f;
        Vector3 horizontal = Vector3.zero;

        if (hasInput)
        {
            Vector3 dir = worldDirection;
            dir.y = 0f;
            dir.Normalize();

            RotateToward(dir);

            float speed = _model != null ? _model.MoveSpeed : 6f;
            horizontal = dir * speed;
        }

        // 수평 + 수직(중력)을 합산해 한 번만 Move() 호출
        Vector3 motion = horizontal + Vector3.up * _verticalVelocity;
        _controller.Move(motion * Time.deltaTime);
        float moveSpeed = _controller.velocity.magnitude;
        SetCurrentMoveSpeed(moveSpeed);
        OnMoved?.Invoke(moveSpeed);
    }

    // ── Private ─────────────────────────────────────────────

    private void ApplyGravity()
    {
        if (_controller.isGrounded)
        {
            // 지면에 붙어있도록 살짝 누름 (isGrounded 유지용)
            _verticalVelocity = -2f;
        }
        else
        {
            // 매 프레임 중력 가속도 누적 (v += g * dt)
            // 프레임레이트 독립적인 물리 적분
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }
    }

    private void RotateToward(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, _rotationSpeed * Time.deltaTime);
    }
}
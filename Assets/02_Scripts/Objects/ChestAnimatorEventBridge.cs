using UnityEngine;

/// <summary>
/// 애니메이션 이벤트 → Chest로 전달하는 전용 브릿지.
/// Animator가 자식 오브젝트에 있을 때, Animation Event는 Animator가 붙은 오브젝트의 컴포넌트만 호출 가능하므로
/// 이 컴포넌트를 Animator 오브젝트에 붙여 Chest로 이벤트를 릴레이한다.
/// </summary>

public class ChestAnimatorEventBridge : MonoBehaviour
{
    /// <summary>상위 계층의 Chest. 비워두면 부모에서 자동 탐색</summary>
    [SerializeField] private Chest _chest;

    private void Awake()
    {
        if (_chest == null)
            _chest = GetComponentInParent<Chest>();
    }

    /// <summary>열림 애니메이션 마지막 프레임에서 호출</summary>
    public void Animation_OnOpenComplete()
    {
        if (_chest != null)
            _chest.Animation_OnOpenComplete();
    }
}


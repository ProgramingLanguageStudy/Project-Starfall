using UnityEngine;

/// <summary>
/// 플레이 중 잠깐 뜨는 오버레이 UI 관리. 상호작용 힌트, 픽업 로그, 골드 등.
/// PlayScene이 보유·초기화. 항상 활성 오브젝트에 붙여 이벤트 구독 유지.
/// </summary>
public class PlaySceneOverlayController : MonoBehaviour
{
    [Header("----- 상호작용 안내 -----")]
    [SerializeField] [Tooltip("다가가면 '말걸기' 등 표시")]
    private InteractionView _interactionView;

    [Header("----- 픽업 로그 -----")]
    [SerializeField] [Tooltip("아이템 획득 시 아이콘+x수량 알림")]
    private PickupLogView _pickupLogView;

    private void OnEnable()
    {
        GameEvents.OnInteractTargetChanged += HandleInteractTargetChanged;
        GameEvents.OnItemPickedUp += HandleItemPickedUp;
        GameEvents.OnGoldAcquired += HandleGoldAcquired;
    }

    private void OnDisable()
    {
        GameEvents.OnInteractTargetChanged -= HandleInteractTargetChanged;
        GameEvents.OnItemPickedUp -= HandleItemPickedUp;
        GameEvents.OnGoldAcquired -= HandleGoldAcquired;
    }

    /// <summary>PlayScene에서 호출. 각 View 초기화.</summary>
    public void Initialize()
    {
        _interactionView?.Initialize();
    }

    private void HandleInteractTargetChanged(IInteractable target)
    {
        _interactionView?.Refresh(target != null ? target.GetInteractText() : "");
    }

    private void HandleItemPickedUp(ItemData itemData, int amount)
    {
        _pickupLogView?.AddEntry(itemData, amount);
    }

    private void HandleGoldAcquired(int amount)
    {
        _pickupLogView?.AddGoldEntry(amount);
    }
}

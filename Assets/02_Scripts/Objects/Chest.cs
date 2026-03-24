using UnityEngine;

/// <summary>
/// 상자 상호작용 로직.
/// - Interact 시 애니메이션 트리거 발동
/// - 애니메이션 끝 이벤트(Animation_OnOpenComplete)에서 열린 상태 확정 + 보상 지급
/// - 로드 복원 시 SetOpened()만 호출하여 시각 상태만 열림으로 고정
/// </summary>

public class Chest : MonoBehaviour, IInteractable
{
    /// <summary>
    /// 세이브/로드용 개별 식별자.
    /// 같은 ChestData를 공유하는 상자가 여러 개 있을 수 있으므로 "종류 ID"와 분리한다.
    /// </summary>
    [SerializeField] private string _saveId;

    /// <summary>보상 구성을 담는 데이터</summary>
    [SerializeField] private ChestData _chestData;
    /// <summary>열림 연출에 사용하는 Animator (자식에 있을 수 있음)</summary>
    [SerializeField] private Animator _animator;
    /// <summary>열기 애니메이션을 재생시키는 트리거 파라미터 이름</summary>
    [SerializeField] private string _openTrigger = "Open";
    /// <summary>Opening에서 Opened로 넘어갈 때 사용하는 트리거 파라미터 이름</summary>
    [SerializeField] private string _openedTrigger = "Opened";
    /// <summary>완전히 열린 정지 포즈(Opened 클립) 스테이트 이름. 로드 복원 시 즉시 스냅하는 용도</summary>
    [SerializeField] private string _openedStateName = "Opened";
    /// <summary>Opened 스테이트가 있는 Animator Layer 인덱스</summary>
    [SerializeField] private int _openedLayer = 0;

    private bool _isOpened = false;

    /// <summary>세이브 키(개별 상자 식별)</summary>
    public string SaveId => _saveId;
    /// <summary>종류 키(프리팹/데이터 매칭용)</summary>
    public string TypeId => _chestData != null ? _chestData.Id : string.Empty;
    public bool IsOpened => _isOpened;
    /// <summary>스포너에서 데이터 주입</summary>
    public void SetData(ChestData data) => _chestData = data;
    /// <summary>스포너에서 개별 세이브 ID 주입</summary>
    public void SetSaveId(string saveId)
    {
        _saveId = saveId;
        TryRegisterToRegistryIfNeeded();
    }

    private void OnEnable()
    {
        TryRegisterToRegistryIfNeeded();
    }

    private void OnDisable()
    {
        PlaySceneRegistry.Chests.Unregister(this);
    }

    private void TryRegisterToRegistryIfNeeded()
    {
        if (!gameObject.activeInHierarchy) return;
        if (string.IsNullOrEmpty(_saveId)) return;
        PlaySceneRegistry.Chests.Register(this);
    }

    public string GetInteractText()
    {
        return _isOpened ? "(이미 열림)" : "상자 열기";
    }

    public void Interact(IInteractReceiver receiver)
    {
        // 이미 열림이면 무시
        if (_isOpened) return;
        if (_animator != null && !string.IsNullOrEmpty(_openTrigger))
            _animator.SetTrigger(_openTrigger);
        else
        {
            // 애니메이터가 없으면 즉시 열림 처리
            SetOpened();
            GiveRewards();
        }
    }

    public void SetOpened()
    {
        _isOpened = true;
        // 열린 시각 상태로 고정(로드 복원 시 사용)
        if (_animator != null && !string.IsNullOrEmpty(_openedStateName))
        {
            _animator.Play(_openedStateName, _openedLayer, 0f);
            _animator.Update(0f);
        }
    }

    /// <summary>
    /// 열림 애니메이션의 마지막 프레임에서 애니메이션 이벤트로 호출
    /// </summary>
    public void Animation_OnOpenComplete()
    {
        if (_isOpened) return;
        _isOpened = true;
        if (_animator != null && !string.IsNullOrEmpty(_openedTrigger))
            _animator.SetTrigger(_openedTrigger);
        GiveRewards();
    }

    /// <summary>ChestData에 따라 보상 지급. GameEvents를 통해 인벤토리로 전달</summary>
    private void GiveRewards()
    {
        if (_chestData == null || _chestData.rewards == null) return;
        foreach (var reward in _chestData.rewards)
        {
            if (reward.item == null) continue;
            int amount = reward.amount;
            if (amount <= 0) continue;
            GameEvents.OnItemPickedUp?.Invoke(reward.item, amount);
        }
    }
}

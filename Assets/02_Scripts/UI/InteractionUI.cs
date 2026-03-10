using UnityEngine;
using TMPro;

/// <summary>
/// 상호작용 안내 문구 표시 전용 UI. 
/// GameEvents.OnInteractTargetChanged를 구독하여 대상이 바뀔 때마다 자동으로 갱신됩니다.
/// </summary>
public class InteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private TextMeshProUGUI _msgText;

    // UIAnimation이 있다면 추가해서 쫀득한 피드백을 줄 수 있습니다.
    // [SerializeField] private UIAnimation _uiAnim; 

    public void Awake()
    {
        // 초기화 시 패널을 비활성화합니다.
        _uiPanel.SetActive(false);
    }

    private void OnEnable()
    {
        // 이벤트 구독: 타겟이 바뀌면 HandleTargetChanged 호출
        GameEvents.OnInteractTargetChanged += HandleTargetChanged;
    }

    private void OnDisable()
    {
        // 이벤트 해제: 메모리 누수 방지
        GameEvents.OnInteractTargetChanged -= HandleTargetChanged;
    }

    private void HandleTargetChanged(IInteractable target)
    {
        // 타겟이 있으면 텍스트를 가져오고, 없으면 빈 문자열 전달
        Refresh(target != null ? target.GetInteractText() : "");
    }

    public void Refresh(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            // 타겟이 없을 때: 패널 끄기
            _uiPanel.SetActive(false);
            // _uiAnim?.PlayClose(() => _uiPanel.SetActive(false)); // 연출 버전
        }
        else
        {
            // 타겟이 있을 때: 텍스트 갱신 후 패널 켜기
            _msgText.text = message;
            _uiPanel.SetActive(true);
            // _uiAnim?.PlayOpen(); // 연출 버전
        }
    }
}
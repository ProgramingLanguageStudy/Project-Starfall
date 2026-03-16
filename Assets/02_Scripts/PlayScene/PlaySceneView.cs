using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이 화면 전체 UI. 체력바, 분대 프로필 등.
/// PlayScene이 보유하고 Model.OnHpChanged / SquadController.OnPlayerChanged 구독 후 갱신 요청.
/// </summary>
public class PlaySceneView : MonoBehaviour
{
    [Header("----- 체력바 (현재 조종 캐릭터) -----")]
    [SerializeField] [Tooltip("Image Type = Filled, Fill Method = Horizontal 권장")]
    private Image _healthFillImage;

    [Header("----- 분대 프로필 (최대 4명) -----")]
    [SerializeField] [Tooltip("슬롯 4개가 붙을 부모. 비면 수동 할당 _profiles 사용")]
    private Transform _profileRoot;
    [SerializeField] [Tooltip("CharacterProfileView가 붙은 슬롯 프리팹. RM에 UI/CharacterProfile 없을 때만 사용 (어드레서블 테스트용)")]
    private GameObject _slotPrefab;

    private CharacterProfileView[] _profiles;
    private const int ProfileCount = 4;

    private void Awake()
    {
        EnsureProfiles();
    }

    private const string CharacterProfileCategory = "UI";
    private const string CharacterProfileName = "CharacterProfile";

    private void EnsureProfiles()
    {
        if (_profiles != null) return;

        GameObject slotPrefab = null;
        bool fromRm = false;
        var rm = GameManager.Instance?.ResourceManager;
        if (rm != null)
        {
            slotPrefab = rm.GetPrefab(CharacterProfileCategory, CharacterProfileName);
            fromRm = slotPrefab != null;
        }
        if (slotPrefab == null)
            slotPrefab = _slotPrefab;

        if (_profileRoot != null && slotPrefab != null)
        {
            if (fromRm)
                Debug.Log("[PlaySceneView] CharacterProfile 슬롯: RM에서 로드");
            else
                Debug.Log("[PlaySceneView] CharacterProfile 슬롯: 직접 참조 사용");

            var list = new List<CharacterProfileView>(ProfileCount);
            for (int i = 0; i < ProfileCount; i++)
            {
                var go = Instantiate(slotPrefab, _profileRoot);
                go.name = $"{slotPrefab.name}_{i}";
                var view = go.GetComponent<CharacterProfileView>();
                if (view != null)
                    list.Add(view);
            }
            _profiles = list.ToArray();
        }
        else
        {
            _profiles = Array.Empty<CharacterProfileView>();
        }
    }

    /// <summary>PlayScene이 Model.OnHpChanged 구독 후 호출. 현재 조종 캐릭터 체력 표시.</summary>
    public void RefreshHealth(int currentHp, int maxHp)
    {
        if (_healthFillImage == null) return;
        _healthFillImage.fillAmount = maxHp > 0 ? (float)currentHp / maxHp : 0f;
    }

    /// <summary>처음 분대가 준비되었을 때 슬롯과 캐릭터를 바인딩.</summary>
    public void BindSquad(IReadOnlyList<Character> members)
    {
        EnsureProfiles();
        if (_profiles == null || _profiles.Length == 0) return;
        for (int i = 0; i < _profiles.Length; i++)
        {
            var view = _profiles[i];
            if (view == null) continue;

            if (members != null && i < members.Count && members[i] != null)
            {
                var model = members[i].Model;
                view.Initialize(model);
            }
            else
            {
                view.Initialize(null);
            }
        }
    }

    /// <summary>현재 조종 캐릭터 프로필 강조. 인덱스가 -1이면 전부 해제.</summary>
    public void SetSelectedProfileIndex(int index)
    {
        EnsureProfiles();
        if (_profiles == null || _profiles.Length == 0) return;
        for (int i = 0; i < _profiles.Length; i++)
        {
            var view = _profiles[i];
            if (view == null) continue;
            view.SetSelected(i == index);
        }
    }
}


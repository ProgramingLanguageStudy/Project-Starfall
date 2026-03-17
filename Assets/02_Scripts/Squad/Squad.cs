using System;
using System.Collections.Generic;

/// <summary>
/// 분대 상태. 슬롯이 진실의 원천(Slot → Character). 플레이어는 캐릭터 참조로 보유.
/// </summary>
public class Squad
{
    public const int SlotCount = 4;

    #region Fields

    private readonly Character[] _slots = new Character[SlotCount];
    private Character _player;

    #endregion

    #region Properties

    public Character Player => _player;

    #endregion

    #region Events

    /// <summary>플레이어 변경 시 발행.</summary>
    public event Action<Character> OnPlayerChanged;

    /// <summary>멤버 추가/제거 시 발행. 인자는 슬롯 기준 길이 4 (빈 슬롯 null).</summary>
    public event Action<IReadOnlyList<Character>> OnMembersChanged;

    #endregion

    #region Public API

    /// <summary>슬롯 기준 현재 배치. 스냅샷 반환.</summary>
    public IReadOnlyList<Character> GetSlots()
    {
        var copy = new Character[SlotCount];
        // Array.Copy(원본, 대상, 개수): _slots에서 copy로 SlotCount개만큼 한 칸씩 복사.
        Array.Copy(_slots, copy, SlotCount);
        return copy;
    }

    /// <summary>캐릭터가 차지 중인 슬롯 인덱스. 없으면 -1.</summary>
    public int GetSlotOf(Character c)
    {
        if (c == null) return -1;
        for (int i = 0; i < SlotCount; i++)
            if (_slots[i] == c) return i;
        return -1;
    }

    /// <summary>캐릭터 추가. slotIndex가 있으면 해당 슬롯에, 없으면 첫 빈 슬롯에.</summary>
    public bool AddMember(Character c, int? slotIndex = null)
    {
        if (c == null) return false;
        if (GetSlotOf(c) >= 0) return false; // 이미 소속됨

        int slot;
        if (slotIndex.HasValue)
        {
            slot = slotIndex.Value;
            if (slot < 0 || slot >= SlotCount || _slots[slot] != null) return false;
        }
        else
        {
            slot = GetFirstFreeSlot();
            if (slot < 0) return false;
        }

        _slots[slot] = c;
        OnMembersChanged?.Invoke(GetSlots());
        return true;
    }

    /// <summary>해당 슬롯 비우기. 편성창 등에서 사용.</summary>
    public void ClearSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return;
        var c = _slots[slot];
        _slots[slot] = null;
        if (_player == c)
            _player = GetFirstNonNullCharacter();
        OnMembersChanged?.Invoke(GetSlots());
    }

    /// <summary>캐릭터를 분대에서 제거. 디버깅 등.</summary>
    public void RemoveMember(Character c)
    {
        int slot = GetSlotOf(c);
        if (slot >= 0)
            ClearSlot(slot);
    }

    /// <summary>
    /// 플레이어 교체. 슬롯에 속한 캐릭터만 허용.
    /// Player 필드·이벤트·각 Character의 플레이어/동료 모드를 모두 여기서 갱신한다.
    /// </summary>
    public void SetPlayerCharacter(Character c)
    {
        if (c == null || GetSlotOf(c) < 0) return;

        // 플레이어가 바뀐 경우에만 필드·이벤트 갱신
        if (c != _player)
        {
            _player = c;
            OnPlayerChanged?.Invoke(_player);
        }

        // 항상 역할 전환 적용 (최초 로드 시 c == _player여도 SetAsPlayer/SetAsCompanion 필요)
        foreach (var member in GetSlots())
        {
            if (member == null) continue;
            if (member == _player)
                member.SetAsPlayer();
            else
                member.SetAsCompanion(_player.transform);
        }
    }

    /// <summary>초기화/로드용. 슬롯 배열과 플레이어 설정. OnMembersChanged는 호출하지 않음.</summary>
    public void SetSlots(Character[] slots, Character player)
    {
        for (int i = 0; i < SlotCount; i++)
            _slots[i] = (slots != null && i < slots.Length) ? slots[i] : null;
        _player = (player != null && GetSlotOf(player) >= 0) ? player : GetFirstNonNullCharacter();
    }

    #endregion

    #region Internal Helpers

    private int GetFirstFreeSlot()
    {
        for (int i = 0; i < SlotCount; i++)
            if (_slots[i] == null) return i;
        return -1;
    }

    private Character GetFirstNonNullCharacter()
    {
        for (int i = 0; i < SlotCount; i++)
            if (_slots[i] != null) return _slots[i];
        return null;
    }

    #endregion
}

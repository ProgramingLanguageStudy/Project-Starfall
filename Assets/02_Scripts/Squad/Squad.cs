using System;
using System.Collections.Generic;

/// <summary>
/// 분대 상태. 멤버 목록·Player 단일 진실 원천.
/// GameObject 생성/삭제 없음. Controller가 Factory와 연동.
/// </summary>
public class Squad
{
    private readonly List<Character> _members = new List<Character>();
    private Character _player;

    public List<Character> Members => _members;
    public Character Player => _player;

    /// <summary>플레이어 변경 시 발행.</summary>
    public event Action<Character> OnPlayerChanged;

    /// <summary>멤버 추가/제거 시 발행.</summary>
    public event Action OnMembersChanged;

    public void AddMember(Character c)
    {
        if (c == null || _members.Contains(c)) return;
        _members.Add(c);
        OnMembersChanged?.Invoke();
    }

    public void RemoveMember(Character c)
    {
        if (c == null) return;
        _members.Remove(c);
        if (_player == c)
            _player = _members.Count > 0 ? _members[0] : null;
        OnMembersChanged?.Invoke();
    }

    public void SetPlayer(Character c)
    {
        if (c == null || c == _player) return;
        if (!_members.Contains(c)) return;

        _player = c;
        OnPlayerChanged?.Invoke(_player);
    }

    /// <summary>내부용. 초기화 시 멤버/플레이어 일괄 설정.</summary>
    public void SetMembersAndPlayer(List<Character> members, Character player)
    {
        _members.Clear();
        if (members != null)
        {
            foreach (var m in members)
            {
                if (m != null && !_members.Contains(m))
                    _members.Add(m);
            }
        }
        _player = _members.Contains(player) ? player : (_members.Count > 0 ? _members[0] : null);
    }
}

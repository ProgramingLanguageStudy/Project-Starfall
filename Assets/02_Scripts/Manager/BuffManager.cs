using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 모든 버프의 지속 시간을 중앙에서 관리합니다.
/// GameManager에 의해 관리되며, CharacterModel의 Update 오버헤드를 줄입니다.
/// </summary>
public class BuffManager : MonoBehaviour
{
    private class ActiveBuff
    {
        public CharacterModel Target;
        public StatModifier Modifier;
        public float RemainingTime;

        public ActiveBuff(CharacterModel target, StatModifier modifier, float duration)
        {
            Target = target;
            Modifier = modifier;
            RemainingTime = duration;
        }
    }

    private List<ActiveBuff> _activeBuffs = new List<ActiveBuff>();

    /// <summary>
    /// 대상에게 버프를 추가합니다.
    /// </summary>
    public void AddBuff(CharacterModel target, StatModifier modifier, float duration)
    {
        if (target == null || duration <= 0f) return;

        target.AddModifier(modifier);
        _activeBuffs.Add(new ActiveBuff(target, modifier, duration));
    }

    private void Update()
    {
        if (_activeBuffs.Count == 0) return;

        float deltaTime = Time.deltaTime;
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = _activeBuffs[i];
            
            // 대상이 파괴되었거나 죽었는지 체크 (방어 코드)
            if (buff.Target == null || buff.Target.IsDead)
            {
                _activeBuffs.RemoveAt(i);
                continue;
            }

            buff.RemainingTime -= deltaTime;

            if (buff.RemainingTime <= 0f)
            {
                buff.Target.RemoveModifier(buff.Modifier);
                _activeBuffs.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 특정 캐릭터의 모든 버프를 강제로 제거합니다. (사망 시 등)
    /// </summary>
    public void ClearBuffsForTarget(CharacterModel target)
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            if (_activeBuffs[i].Target == target)
            {
                _activeBuffs.RemoveAt(i);
            }
        }
    }
}

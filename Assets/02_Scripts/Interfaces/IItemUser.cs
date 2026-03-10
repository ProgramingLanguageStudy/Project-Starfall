/// <summary>아이템 사용 시 효과를 받는 대상. 인벤토리는 이 인터페이스만 참조합니다.</summary>
public interface IItemUser
{
    void Heal(int amount);
    void ApplyBuff(StatModifier modifier, float durationSeconds);
}

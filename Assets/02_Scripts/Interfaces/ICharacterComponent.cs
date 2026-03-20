/// <summary>
/// 캐릭터의 하위 시스템(Mover, Attacker 등)들이 구현해야 할 인터페이스.
/// Character(Facade)를 통해 필요한 의존성을 스스로 찾아 초기화합니다.
/// </summary>
public interface ICharacterComponent
{
    /// <summary>
    /// 캐릭터 초기화 시 호출됩니다.
    /// </summary>
    /// <param name="owner">조율자(Facade)인 Character 객체</param>
    void Init(Character owner);
}

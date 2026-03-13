/// <summary>
/// Esc 키가 눌리면 닫히는 UI.
/// InputHandler가 Esc 구독 후 이 인터페이스 스택의 맨 위만 Close 호출.
/// </summary>
public interface IRespondsToEsc
{
    void Close();
}

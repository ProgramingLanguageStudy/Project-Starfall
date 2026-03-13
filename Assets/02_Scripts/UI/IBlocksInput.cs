/// <summary>
/// 열려 있으면 이동/조작 입력을 막는 UI.
/// InputHandler: IsOpen이 true인 UI가 하나라도 있으면 이동/조작 차단.
/// </summary>
public interface IBlocksInput
{
    bool IsOpen { get; }
}

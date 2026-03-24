using UnityEngine;

/// <summary>
/// Play 씬에서만 유효한 레지스트리 허브(깊이 0).
/// 씬 언로드 시 Clear로 반드시 정리해야 한다.
/// </summary>
public static class PlaySceneRegistry
{
    public static ChestRegistry Chests { get; } = new ChestRegistry();

    public static void Clear()
    {
        Chests.Clear();
    }
}


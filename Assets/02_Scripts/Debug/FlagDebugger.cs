using System.Text;
using UnityEngine;

/// <summary>
/// 플래그 디버그/테스트용. Hierarchy의 Debuggers 등에 붙이고, 인스펙터에서 FlagSystem 참조 할당.
/// 퀘스트 테스트 시 플래그 초기화·상태 확인에 사용.
/// </summary>
public class FlagDebugger : MonoBehaviour
{
    [SerializeField] [Tooltip("비어 있으면 씬에서 FindObjectOfType으로 탐색")]
    private FlagSystem _flagSystem;

    public FlagSystem FlagSystemRef => _flagSystem;

    private void OnValidate()
    {
        if (_flagSystem == null && Application.isPlaying)
            _flagSystem = FindAnyObjectByType<FlagSystem>();
    }

    /// <summary>현재 플래그 전체 초기화. 퀘스트 테스트 시 사용.</summary>
    public void ResetFlags()
    {
        var fs = GetFlagSystem();
        if (fs == null) return;

        fs.LoadFromSave(new FlagSaveData());
        Debug.Log("[FlagDebugger] 플래그 전체 초기화 완료.");
    }

    /// <summary>현재 플래그 목록을 콘솔에 출력.</summary>
    public void LogFlags()
    {
        var fs = GetFlagSystem();
        if (fs == null) return;

        var data = fs.GetAllForSave();
        if (data?.keys == null || data.keys.Count == 0)
        {
            Debug.Log("[FlagDebugger] 플래그 없음.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[FlagDebugger] 현재 플래그:");
        for (int i = 0; i < data.keys.Count && i < data.values.Count; i++)
            sb.AppendLine($"  {data.keys[i]} = {data.values[i]}");
        Debug.Log(sb.ToString());
    }

    private FlagSystem GetFlagSystem()
    {
        if (_flagSystem == null)
            _flagSystem = FindAnyObjectByType<FlagSystem>();
        if (_flagSystem == null)
            Debug.LogWarning("[FlagDebugger] FlagSystem을 찾을 수 없습니다. Play 씬에 FlagSystem이 있는지 확인하세요.");
        return _flagSystem;
    }
}

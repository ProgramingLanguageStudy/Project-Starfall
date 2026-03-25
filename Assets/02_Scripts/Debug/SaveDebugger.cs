using System.IO;
using UnityEngine;

/// <summary>
/// 세이브 데이터 디버그용. Hierarchy의 Debuggers 등에 붙이고,
/// 인스펙터에서 로컬 세이브 파일 삭제 기능 제공.
/// 에디터 모드에서만 동작.
/// </summary>
public class SaveDebugger : MonoBehaviour
{
    private static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveConstants.Local.FileName);
    }

    /// <summary>로컬 세이브 파일 삭제. 에디터 모드에서만 동작.</summary>
    public bool DeleteLocalSaveFile()
    {
        var path = GetSavePath();
        if (!File.Exists(path))
        {
            Debug.Log("[SaveDebugger] 삭제할 세이브 파일이 없습니다.");
            return false;
        }

        File.Delete(path);
        Debug.Log($"[SaveDebugger] 세이브 파일 삭제 완료: {path}");
        return true;
    }

    /// <summary>로컬 세이브 파일 존재 여부 확인.</summary>
    public bool HasLocalSaveFile()
    {
        return File.Exists(GetSavePath());
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 로컬 파일 기반 세이브 백엔드.
/// CustomBasePath 설정 시 해당 폴더 사용, 비면 persistentDataPath 사용.
/// </summary>
public class LocalSaveBackend : ISaveBackend
{
    private const string FileName = "save_0.json";

    /// <summary>지정 시 이 경로에 저장. 비면 persistentDataPath. 예: 프로젝트/SaveData</summary>
    public static string CustomBasePath { get; set; }

    private static string GetSavePath()
    {
        var basePath = !string.IsNullOrEmpty(CustomBasePath) ? CustomBasePath : Application.persistentDataPath;
        return Path.Combine(basePath, FileName);
    }

    /// <summary>비동기 저장. 파일 없으면 생성.</summary>
    public Task<bool> SaveAsync(SaveData data)
    {
        if (data == null) return Task.FromResult(false);

        var path = GetSavePath(); // persistentDataPath는 메인 스레드에서만 호출 가능
        return Task.Run(() =>
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonUtility.ToJson(data);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LocalSaveBackend] Save failed: " + e.Message);
                return false;
            }
        });
    }

    /// <summary>비동기 로드. 파일 없으면 null.</summary>
    public Task<SaveData> LoadAsync()
    {
        var path = GetSavePath(); // persistentDataPath는 메인 스레드에서만 호출 가능
        return Task.Run(() =>
        {
            try
            {
                if (!File.Exists(path)) return (SaveData)null;

                var json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json)) return (SaveData)null;

                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LocalSaveBackend] Load failed: " + e.Message);
                return (SaveData)null;
            }
        });
    }

    /// <summary>로컬 세이브 파일 삭제.</summary>
    public Task<bool> DeleteAsync()
    {
        var path = GetSavePath(); // persistentDataPath는 메인 스레드에서만 호출 가능
        return Task.Run(() =>
        {
            try
            {
                if (!File.Exists(path)) return true;
                File.Delete(path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LocalSaveBackend] Delete failed: " + e.Message);
                return false;
            }
        });
    }
}

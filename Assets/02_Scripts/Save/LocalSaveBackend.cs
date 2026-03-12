using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 로컬 파일 기반 세이브 백엔드. Application.persistentDataPath/save_0.json 사용.
/// Firebase 미초기화·미로그인 시 폴백으로 사용.
/// </summary>
public class LocalSaveBackend : ISaveBackend
{
    private const string FileName = "save_0.json";

    private static string GetSavePath() => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>비동기 저장. 파일 없으면 생성.</summary>
    public Task<bool> SaveAsync(SaveData data)
    {
        if (data == null) return Task.FromResult(false);

        return Task.Run(() =>
        {
            try
            {
                var path = GetSavePath();
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
        return Task.Run(() =>
        {
            try
            {
                var path = GetSavePath();
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
        return Task.Run(() =>
        {
            try
            {
                var path = GetSavePath();
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

using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// 로컬 파일 기반 세이브 백엔드.
/// persistentDataPath에 저장.
/// </summary>
public class LocalSaveBackend : ISaveBackend
{
    #region ISaveBackend

    public IEnumerator LoadAsync(Action<SaveData> onComplete)
    {
        SaveData data = null;
        try
        {
            var path = GetSavePath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                if (!string.IsNullOrEmpty(json))
                    data = JsonUtility.FromJson<SaveData>(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LocalSaveBackend] Load failed: " + e.Message);
        }
        onComplete?.Invoke(data);
        yield break;
    }

    public IEnumerator SaveAsync(SaveData data, Action<bool> onComplete)
    {
        var success = false;
        if (data != null)
        {
            try
            {
                var path = GetSavePath();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonUtility.ToJson(data);
                File.WriteAllText(path, json);
                success = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LocalSaveBackend] Save failed: " + e.Message);
            }
        }
        onComplete?.Invoke(success);
        yield break;
    }

    public IEnumerator DeleteAsync(Action<bool> onComplete)
    {
        var success = true;
        try
        {
            var path = GetSavePath();
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LocalSaveBackend] Delete failed: " + e.Message);
            success = false;
        }
        onComplete?.Invoke(success);
        yield break;
    }

    #endregion

    #region Private

    private static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveConstants.Local.FileName);
    }

    #endregion
}

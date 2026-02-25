using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 세이브 I/O 담당. 일반 C# 클래스. SaveManager가 new로 생성해 보유.
/// JSON 파일로 저장. Save(slot, data) / Load(slot) → SaveData.
/// </summary>
public class SaveSystem
{
    private const string FilePrefix = "save_";
    private const string FileSuffix = ".json";
    private const string SaveDataFolderName = "SaveData";

    /// <summary>슬롯 번호에 해당하는 파일 경로. 에디터는 프로젝트 루트/SaveData, 빌드는 persistentDataPath.</summary>
    public string GetSavePath(int slot)
    {
#if UNITY_EDITOR
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string saveDir = Path.Combine(projectRoot, SaveDataFolderName);
        return Path.Combine(saveDir, FilePrefix + slot + FileSuffix);
#else
        return Path.Combine(Application.persistentDataPath, FilePrefix + slot + FileSuffix);
#endif
    }

    /// <summary>데이터를 JSON으로 저장. 실패 시 false.</summary>
    public bool Save(int slot, SaveData data)
    {
        if (data == null) return false;
        try
        {
            string path = GetSavePath(slot);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveSystem] Save failed: " + e.Message);
            return false;
        }
    }

    /// <summary>슬롯에서 로드. 파일 없거나 실패 시 null.</summary>
    public SaveData Load(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path)) return null;
        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveSystem] Load failed: " + e.Message);
            return null;
        }
    }

    /// <summary>해당 슬롯에 세이브 파일이 있는지.</summary>
    public bool HasSave(int slot)
    {
        return File.Exists(GetSavePath(slot));
    }

    /// <summary>해당 슬롯의 세이브 파일 삭제. 디버그/테스트용.</summary>
    public bool Delete(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path)) return false;
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveSystem] Delete failed: " + e.Message);
            return false;
        }
    }
}

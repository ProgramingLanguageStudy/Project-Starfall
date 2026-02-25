using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 세이브 시점 제어 및 API 제공. ISaveHandler 등록·수집·적용, SaveSystem에서 I/O.
/// - Play 진입 시: 로드 후 ApplySaveData.
/// - Play 종료 시(에디터): 저장.
/// - 앱/에디터 종료 시: 저장.
/// GameManager가 보유. SaveSystem은 new로 생성해 보유.
/// </summary>
public class SaveManager : MonoBehaviour
{
    [SerializeField] private int _defaultSlot = 0;

    private readonly List<ISaveHandler> _handlers = new List<ISaveHandler>();
    private SaveSystem _saveSystem;
    private SaveSystem SaveSystem => _saveSystem ??= new SaveSystem();

    /// <summary>세이브/로드에 참여할 핸들러 등록. 각 시스템은 OnEnable에서 호출.</summary>
    public void Register(ISaveHandler handler)
    {
        if (handler == null || _handlers.Contains(handler)) return;
        _handlers.Add(handler);
    }

    /// <summary>등록 해제. 각 시스템은 OnDisable에서 호출.</summary>
    public void Unregister(ISaveHandler handler)
    {
        if (handler == null) return;
        _handlers.Remove(handler);
    }

    /// <summary>등록된 핸들러들로부터 SaveData 수집. 저장 직전에 사용.</summary>
    public SaveData GatherSaveData()
    {
        var data = new SaveData();
        for (int i = 0; i < _handlers.Count; i++)
            _handlers[i].Gather(data);
        return data;
    }

    /// <summary>로드한 SaveData를 등록된 핸들러들에게 적용. 로드 직후에 사용.</summary>
    public void ApplySaveData(SaveData data)
    {
        if (data == null) return;
        for (int i = 0; i < _handlers.Count; i++)
            _handlers[i].Apply(data);
    }

    private void OnApplicationQuit()
    {
        Save();
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            Save();
    }
#endif

    /// <summary>현재 게임 상태를 수집 후 저장. 외부 API.</summary>
    public void Save()
    {
        var data = GatherSaveData();
        if (data == null) return;

        if (SaveSystem.Save(_defaultSlot, data))
            Debug.Log("[SaveManager] Saved to slot " + _defaultSlot);
        else
            Debug.LogWarning("[SaveManager] Save failed.");
    }

    /// <summary>슬롯에서 로드. 적용 없이 SaveData만 반환. PlayScene에서 스폰 위치 등에 사용.</summary>
    public SaveData Load()
    {
        return SaveSystem.Load(_defaultSlot);
    }

    /// <summary>세이브 파일 삭제. 디버그/테스트용. 삭제 성공 시 true.</summary>
    public bool TryDeleteSave()
    {
        return SaveSystem.Delete(_defaultSlot);
    }

    /// <summary>슬롯에서 로드 후 핸들러들에게 적용. 외부 API. PlayScene.Awake에서 호출.</summary>
    public void LoadAndApply()
    {
        var data = SaveSystem.Load(_defaultSlot);
        if (data == null)
        {
            Debug.Log("[SaveManager] No save in slot " + _defaultSlot + ", skip load.");
            return;
        }

        ApplySaveData(data);
        Debug.Log("[SaveManager] Loaded from slot " + _defaultSlot);
    }
}

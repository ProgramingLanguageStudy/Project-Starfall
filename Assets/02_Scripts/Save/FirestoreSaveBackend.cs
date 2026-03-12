using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// Firestore 기반 세이브 백엔드. users/{userId}/save/save_slot0 문서에 JSON 저장.
/// </summary>
public class FirestoreSaveBackend
{
    private const string CollectionUsers = "users";
    private const string CollectionSave = "save";
    private const string DocumentSlot = "save_slot0";
    private const string FieldData = "data";

    private readonly string _userId;
    private readonly FirebaseFirestore _db;

    public FirestoreSaveBackend(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("userId is required.", nameof(userId));

        _userId = userId;
        _db = FirebaseFirestore.DefaultInstance;
    }

    /// <summary>비동기 저장. 성공 시 true.</summary>
    public Task<bool> SaveAsync(SaveData data)
    {
        if (data == null) return Task.FromResult(false);

        var json = JsonUtility.ToJson(data);
        var docRef = _db.Collection(CollectionUsers).Document(_userId)
            .Collection(CollectionSave).Document(DocumentSlot);

        var dict = new Dictionary<string, object>
        {
            [FieldData] = json
        };

        return docRef.SetAsync(dict, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogWarning("[FirestoreSaveBackend] Save failed: " + task.Exception?.Message);
                    return false;
                }
                return true;
            });
    }

    /// <summary>비동기 로드. 없거나 실패 시 null.</summary>
    public Task<SaveData> LoadAsync()
    {
        var docRef = _db.Collection(CollectionUsers).Document(_userId)
            .Collection(CollectionSave).Document(DocumentSlot);

        return docRef.GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogWarning("[FirestoreSaveBackend] Load failed: " + task.Exception?.Message);
                    return (SaveData)null;
                }

                var snapshot = task.Result;
                if (snapshot == null || !snapshot.Exists)
                    return (SaveData)null;

                if (!snapshot.TryGetValue<string>(FieldData, out var json, ServerTimestampBehavior.None))
                    return (SaveData)null;

                if (string.IsNullOrEmpty(json)) return (SaveData)null;

                try
                {
                    return JsonUtility.FromJson<SaveData>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[FirestoreSaveBackend] Parse failed: " + e.Message);
                    return (SaveData)null;
                }
            });
    }

    /// <summary>세이브 존재 여부.</summary>
    public Task<bool> HasSaveAsync()
    {
        var docRef = _db.Collection(CollectionUsers).Document(_userId)
            .Collection(CollectionSave).Document(DocumentSlot);

        return docRef.GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) return false;
                return task.Result?.Exists ?? false;
            });
    }

    /// <summary>세이브 삭제. 디버그/테스트용.</summary>
    public Task<bool> DeleteAsync()
    {
        var docRef = _db.Collection(CollectionUsers).Document(_userId)
            .Collection(CollectionSave).Document(DocumentSlot);

        return docRef.DeleteAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogWarning("[FirestoreSaveBackend] Delete failed: " + task.Exception?.Message);
                    return false;
                }
                return true;
            });
    }
}

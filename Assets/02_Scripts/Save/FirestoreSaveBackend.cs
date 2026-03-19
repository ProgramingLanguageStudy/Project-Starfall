using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// Firestore 기반 세이브 백엔드. users/{userId}/save/save_slot0 문서에 JSON 저장.
/// Task API를 코루틴으로 래핑하여 제공.
/// </summary>
public class FirestoreSaveBackend : ISaveBackend
{
    #region Fields

    private readonly string _userId;
    private readonly FirebaseFirestore _db;

    #endregion

    #region Constructor

    public FirestoreSaveBackend(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("userId is required.", nameof(userId));

        _userId = userId;
        _db = FirebaseFirestore.DefaultInstance;
    }

    #endregion

    #region ISaveBackend

    public IEnumerator LoadAsync(Action<SaveData> onComplete)
    {
        var task = LoadInternalAsync();
        yield return task.WaitUntilComplete();
        var data = task.IsFaulted ? null : task.Result;
        onComplete?.Invoke(data);
    }

    public IEnumerator SaveAsync(SaveData data, Action<bool> onComplete)
    {
        var task = SaveInternalAsync(data);
        yield return task.WaitUntilComplete();
        var success = !task.IsFaulted && task.Result;
        onComplete?.Invoke(success);
    }

    public IEnumerator DeleteAsync(Action<bool> onComplete)
    {
        var task = DeleteInternalAsync();
        yield return task.WaitUntilComplete();
        var success = !task.IsFaulted && task.Result;
        onComplete?.Invoke(success);
    }

    #endregion

    #region Private - Save

    private Task<bool> SaveInternalAsync(SaveData data)
    {
        if (data == null) return Task.FromResult(false);

        var json = JsonUtility.ToJson(data);
        var docRef = _db.Collection(SaveConstants.Firestore.CollectionUsers).Document(_userId)
            .Collection(SaveConstants.Firestore.CollectionSave).Document(SaveConstants.Firestore.DocumentSlot);

        var dict = new Dictionary<string, object>
        {
            [SaveConstants.Firestore.FieldData] = json
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

    #endregion

    #region Private - Load

    private Task<SaveData> LoadInternalAsync()
    {
        var docRef = _db.Collection(SaveConstants.Firestore.CollectionUsers).Document(_userId)
            .Collection(SaveConstants.Firestore.CollectionSave).Document(SaveConstants.Firestore.DocumentSlot);

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

                if (!snapshot.TryGetValue<string>(SaveConstants.Firestore.FieldData, out var json, ServerTimestampBehavior.None))
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

    #endregion

    #region Private - Delete

    private Task<bool> DeleteInternalAsync()
    {
        var docRef = _db.Collection(SaveConstants.Firestore.CollectionUsers).Document(_userId)
            .Collection(SaveConstants.Firestore.CollectionSave).Document(SaveConstants.Firestore.DocumentSlot);

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

    #endregion
}

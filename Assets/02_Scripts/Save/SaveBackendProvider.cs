using Firebase.Auth;
using UnityEngine;

/// <summary>
/// Boot·Auth 상태에 따라 ISaveBackend 생성. SaveManager에 주입.
/// </summary>
public static class SaveBackendProvider
{
    private static bool _backendLogged;

    /// <summary>Boot 경유 시 Firestore 사용 가능.</summary>
    public static bool BootCompleted { get; set; }

    /// <summary>적절한 백엔드 생성. Boot 미경유 또는 미로그인 시 로컬.</summary>
    public static ISaveBackend CreateBackend()
    {
        if (!BootCompleted)
        {
            if (!_backendLogged) { _backendLogged = true; Debug.Log("[SaveBackendProvider] Boot 미경유 → 로컬"); }
            return new LocalSaveBackend();
        }

        try
        {
            var user = FirebaseAuth.DefaultInstance?.CurrentUser;
            if (user != null)
            {
                if (!_backendLogged) { _backendLogged = true; Debug.Log("[SaveBackendProvider] Boot 경유 + 로그인 → Firestore"); }
                return new FirestoreSaveBackend(user.UserId);
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("[SaveBackendProvider] Firebase not ready, using local: " + e.Message);
        }

        if (!_backendLogged) { _backendLogged = true; Debug.Log("[SaveBackendProvider] Boot 경유 + 미로그인 → 로컬"); }
        return new LocalSaveBackend();
    }
}

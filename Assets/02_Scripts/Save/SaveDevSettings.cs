#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

/// <summary>
/// 에디터에서만 의미 있는 개발용 옵션. 제품 플로우(인트로·로그인)와 분리.
/// </summary>
public static class SaveDevSettings
{
#if UNITY_EDITOR
    private const string EditorPrefsKey = "Squad_Save_ForceLocal";
    private const string MenuPath = "Tools/Save/Force Local Save (Editor)";

    /// <summary>켜면 항상 로컬 세이브 백엔드. 빌드에서는 항상 false.</summary>
    public static bool ForceLocalSave => EditorPrefs.GetBool(EditorPrefsKey, false);

    [MenuItem(MenuPath, false, 200)]
    private static void ToggleForceLocalMenu()
    {
        var v = !EditorPrefs.GetBool(EditorPrefsKey, false);
        EditorPrefs.SetBool(EditorPrefsKey, v);
        if (Application.isPlaying && GameManager.Instance != null)
        {
            var sm = GameManager.Instance.SaveManager;
            if (sm == null)
            {
                Debug.LogError("[SaveDevSettings] SaveManager is null; cannot refresh save backend.");
                return;
            }
            sm.ApplySaveBackend(GameManager.Instance.FirebaseAuthManager);
        }
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleForceLocalMenuValidate()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(EditorPrefsKey, false));
        return true;
    }

    private const string EditorPrefsKeyDiag = "Squad_Save_LogDiagnostics";
    private const string MenuPathDiag = "Tools/Save/Log Save Diagnostics (Gold/Contributors)";

    /// <summary>켜면 저장·로드·Gather 시 골드·contributor 목록 등을 콘솔에 출력. 빌드에서는 항상 false.</summary>
    public static bool LogSaveDiagnostics => EditorPrefs.GetBool(EditorPrefsKeyDiag, false);

    [MenuItem(MenuPathDiag, false, 201)]
    private static void ToggleLogDiagnosticsMenu()
    {
        var v = !EditorPrefs.GetBool(EditorPrefsKeyDiag, false);
        EditorPrefs.SetBool(EditorPrefsKeyDiag, v);
        Debug.Log(v ? "[SaveDevSettings] Save diagnostics logging ON." : "[SaveDevSettings] Save diagnostics logging OFF.");
    }

    [MenuItem(MenuPathDiag, true)]
    private static bool ToggleLogDiagnosticsMenuValidate()
    {
        Menu.SetChecked(MenuPathDiag, EditorPrefs.GetBool(EditorPrefsKeyDiag, false));
        return true;
    }
#else
    public static bool ForceLocalSave => false;

    /// <summary>빌드에서는 진단 로그 비활성.</summary>
    public static bool LogSaveDiagnostics => false;
#endif
}

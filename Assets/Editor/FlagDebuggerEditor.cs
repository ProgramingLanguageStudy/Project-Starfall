using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlagDebugger))]
public class FlagDebuggerEditor : Editor
{
    private SerializedProperty _flagSystemProp;

    private void OnEnable()
    {
        _flagSystemProp = serializedObject.FindProperty("_flagSystem");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_flagSystemProp);
        if (_flagSystemProp.objectReferenceValue == null && GUILayout.Button("씬에서 FlagSystem 찾기"))
        {
            var found = FindAnyObjectByType<FlagSystem>();
            if (found != null) _flagSystemProp.objectReferenceValue = found;
        }

        EditorGUILayout.Space(8);

        var debugger = (FlagDebugger)target;
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        if (GUILayout.Button("플래그 초기화 (Reset)")) debugger.ResetFlags();
        if (GUILayout.Button("플래그 목록 출력 (Log)")) debugger.LogFlags();

        EditorGUILayout.Space(4);
        GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
        if (GUILayout.Button("세이브 파일 삭제"))
        {
            if (EditorUtility.DisplayDialog("세이브 삭제", "세이브 파일을 삭제합니다. 다음 플레이 시 새 게임으로 시작됩니다.", "삭제", "취소"))
                debugger.DeleteSaveFile();
        }
        GUI.backgroundColor = Color.white;

        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("플레이 모드에서만 버튼이 동작합니다.", MessageType.Info);
    }
}

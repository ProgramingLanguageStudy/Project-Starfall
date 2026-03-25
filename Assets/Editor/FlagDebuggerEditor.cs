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

        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("플레이 모드에서만 버튼이 동작합니다.", MessageType.Info);
    }
}

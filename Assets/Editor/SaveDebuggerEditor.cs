using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SaveDebugger))]
public class SaveDebuggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var debugger = (SaveDebugger)target;

        EditorGUI.BeginDisabledGroup(Application.isPlaying);

        EditorGUILayout.HelpBox("에디터 모드에서만 동작합니다. 로컬 세이브 파일을 관리합니다.", MessageType.Info);

        EditorGUILayout.Space(4);

        var saveExists = debugger.HasLocalSaveFile();
        EditorGUILayout.LabelField("세이브 파일 상태:", saveExists ? "존재함" : "없음");

        EditorGUILayout.Space(8);

        GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
        EditorGUI.BeginDisabledGroup(!saveExists);

        if (GUILayout.Button("로컬 세이브 파일 삭제", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "세이브 삭제",
                $"로컬 세이브 파일을 삭제합니다.\n경로: {Application.persistentDataPath}\\{SaveConstants.Local.FileName}\n\n다음 플레이 시 새 게임으로 시작됩니다.",
                "삭제",
                "취소"))
            {
                debugger.DeleteLocalSaveFile();
            }
        }

        EditorGUI.EndDisabledGroup();
        GUI.backgroundColor = Color.white;

        EditorGUI.EndDisabledGroup();

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서는 동작하지 않습니다. 에디터 모드로 전환하세요.", MessageType.Warning);
        }
    }
}

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestDebugger))]
public class QuestDebuggerEditor : Editor
{
    private const double RepaintInterval = 0.2;
    private double _lastRepaintTime;

    private SerializedProperty _questPresenterProp;
    private SerializedProperty _flagSystemProp;

    private void OnEnable()
    {
        _questPresenterProp = serializedObject.FindProperty("_questPresenter");
        _flagSystemProp = serializedObject.FindProperty("_flagSystem");
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (!Application.isPlaying || target == null) return;
        if (EditorApplication.timeSinceStartup - _lastRepaintTime >= RepaintInterval)
        {
            _lastRepaintTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_questPresenterProp);
        if (_questPresenterProp.objectReferenceValue == null && GUILayout.Button("씬에서 QuestPresenter 찾기"))
        {
            var found = FindAnyObjectByType<QuestPresenter>();
            if (found != null) _questPresenterProp.objectReferenceValue = found;
        }

        EditorGUILayout.PropertyField(_flagSystemProp);
        if (_flagSystemProp.objectReferenceValue == null && GUILayout.Button("씬에서 FlagSystem 찾기"))
        {
            var found = FindAnyObjectByType<FlagSystem>();
            if (found != null) _flagSystemProp.objectReferenceValue = found;
        }

        EditorGUILayout.Space(8);

        var debugger = (QuestDebugger)target;
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        if (GUILayout.Button("진행 중 퀘스트 목록 출력 (Log)")) debugger.LogActiveQuests();

        var quests = debugger.QuestPresenterRef?.System?.GetActiveQuests();
        if (quests != null && quests.Count > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("진행 중 퀘스트", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var q in quests)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{q.Title} ({q.CurrentAmount}/{q.TargetAmount})");
                if (GUILayout.Button("삭제", GUILayout.Width(50)))
                    debugger.RemoveQuest(q.QuestId);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        if (GUILayout.Button("퀘스트 전체 삭제"))
        {
            if (EditorUtility.DisplayDialog("퀘스트 전체 삭제", "진행 중인 퀘스트를 모두 제거합니다.", "삭제", "취소"))
                debugger.ClearAllQuests();
        }

        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("플레이 모드에서만 버튼이 동작합니다.", MessageType.Info);
    }
}

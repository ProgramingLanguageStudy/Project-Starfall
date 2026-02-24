using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대화 종료 시 플래그 변경 방식. Set=값 지정, Add=현재값+value.
/// </summary>
public enum FlagOp
{
    Set,
    Add
}

/// <summary>
/// 대화 종료 시 적용할 플래그 변경 하나.
/// </summary>
[Serializable]
public struct FlagModification
{
    public string key;
    public FlagOp op;
    public int value;
}

/// <summary>
/// 퀘스트 대화 유형. None이면 퀘스트 처리 없음.
/// </summary>
public enum QuestDialogueType
{
    None,
    Accept,
    InProgress,
    Complete
}

/// <summary>
/// 대화 한 편. 플래그 기반 조건으로 선택, 종료 시 플래그 변경·퀘스트 연동.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Data")]
public class DialogueData : ScriptableObject
{
    [Header("선택 조건 (플래그 기반)")]
    [Tooltip("이 대화를 쓰는 NPC ID")]
    public string npcId;
    [Tooltip("화자 표시명. 비어 있으면 npcId 사용")]
    public string speakerDisplayName;
    [Tooltip("선택 우선순위. 낮을수록 먼저 검사. 0=첫대화, 10=퀘스트제시, 15=진행중, 20=퀘스트완료, 30=일반")]
    public int priority = 30;
    [Tooltip("이 대화가 선택되려면 켜져 있어야 하는 플래그 (1)")]
    public string[] requiredFlagsOn;
    [Tooltip("이 대화가 선택되려면 꺼져 있어야 하는 플래그 (0)")]
    public string[] requiredFlagsOff;

    [Header("종료 시 효과")]
    [Tooltip("대화 종료 시 적용할 플래그 변경. Set=값 지정, Add=현재값+value")]
    public FlagModification[] flagsToModify;

    [Header("퀘스트 연동")]
    [Tooltip("비어 있지 않으면 퀘스트 대화. questDialogueType에 따라 처리")]
    public string questId;
    [Tooltip("퀘스트 대화 유형")]
    public QuestDialogueType questDialogueType = QuestDialogueType.None;

    [Header("내용")]
    [Tooltip("한 문장씩 순서대로 재생 (독백)")]
    [TextArea(1, 3)]
    public string[] lines;

    /// <summary>현재 플래그 상태가 이 대화 선택 조건을 만족하는지.</summary>
    public bool MatchesFlags(Func<string, int> getFlag)
    {
        if (requiredFlagsOn != null)
            foreach (var key in requiredFlagsOn)
                if (!string.IsNullOrEmpty(key) && getFlag(key) == 0) return false;
        if (requiredFlagsOff != null)
            foreach (var key in requiredFlagsOff)
                if (!string.IsNullOrEmpty(key) && getFlag(key) != 0) return false;
        return true;
    }

    /// <summary>화자 표시명. 비어 있으면 npcId 반환.</summary>
    public string SpeakerDisplayName => !string.IsNullOrEmpty(speakerDisplayName) ? speakerDisplayName : npcId ?? "";

    /// <summary>빈 문장 제거·null 방지한 복사. 없으면 길이 0 배열.</summary>
    public string[] Lines
    {
        get
        {
            if (lines == null || lines.Length == 0) return Array.Empty<string>();
            var list = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                var s = lines[i] != null ? lines[i].Trim() : "";
                if (s.Length > 0) list.Add(s);
            }
            return list.Count > 0 ? list.ToArray() : Array.Empty<string>();
        }
    }
}

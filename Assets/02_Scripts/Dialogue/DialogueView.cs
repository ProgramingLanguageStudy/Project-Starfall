using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
/// <summary>
/// 대화창: 문장 표시 + questList 기반 버튼 동적 생성.
/// </summary>
public class DialogueView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _dialogueText;
    [SerializeField] private GameObject _panel;

    [Header("버튼")]
    [SerializeField] private UI_Button _buttonPrefab;
    [SerializeField] private Transform _buttonContainer;

    private bool _isTyping;
    private string _currentSentence;
    private readonly List<GameObject> _buttons = new List<GameObject>();

    public event Action OnNextClicked;
    public event Action OnEndClicked;
    public event Action<DialogueData> OnQuestDialogueSelected;

    /// <summary>category·questList·isLastLine에 따라 버튼 동적 생성.</summary>
    public void SetButtonMode(DialogueCategory category, IReadOnlyList<DialogueData> questList, bool isLastLine = false)
    {
        ClearButtons();

        if (_buttonPrefab == null || _buttonContainer == null) return;

        if (category == DialogueCategory.FirstTalk)
        {
            if (!isLastLine)
                AddButton("다음", () => OnNextClicked?.Invoke());
            else
                AddButton("끝내기", () => OnEndClicked?.Invoke());
            return;
        }

        // VerticalLayoutGroup StartCorner=Lower → 아래→위 배치. 원하는 순서(퀘스트→다음→끝내기)를 위해 역순 추가.
        if (category == DialogueCategory.Quest)
        {
            if (!isLastLine)
                AddButton("다음", () => OnNextClicked?.Invoke());
            else
                AddButton("끝내기", () => OnEndClicked?.Invoke());
        }
        else
        {
            AddButton("끝내기", () => OnEndClicked?.Invoke());
            AddButton("다음", () => OnNextClicked?.Invoke());
        }

        if (questList != null && questList.Count > 0)
        {
            foreach (var q in questList)
            {
                var label = GetQuestButtonLabel(q);
                var capture = q;
                AddButton(label, () => OnQuestDialogueSelected?.Invoke(capture));
            }
        }
    }

    private void AddButton(string label, Action onClick)
    {
        var ui = Instantiate(_buttonPrefab, _buttonContainer);
        ui.Initialize(null, label, onClick);
        _buttons.Add(ui.gameObject);
    }

    private static string GetQuestButtonLabel(DialogueData d)
    {
        var title = "퀘스트";
        if (!string.IsNullOrEmpty(d.questId))
        {
            var questData = Resources.Load<QuestData>($"Quests/{d.questId}");
            if (!string.IsNullOrEmpty(questData?.Title)) title = questData.Title;
        }
        var status = d.questDialogueType switch
        {
            QuestDialogueType.Accept => "수락",
            QuestDialogueType.InProgress => "진행중",
            QuestDialogueType.Complete => "완료",
            _ => ""
        };
        return string.IsNullOrEmpty(status) ? title : $"{title} ({status})";
    }

    private void ClearButtons()
    {
        foreach (var go in _buttons)
        {
            if (go != null) Destroy(go);
        }
        _buttons.Clear();
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace('\u00A0', ' ');
    }

    public void Display(string speakerName, string sentence)
    {
        _panel.SetActive(true);
        _nameText.text = Sanitize(speakerName ?? "");
        _currentSentence = Sanitize(sentence ?? "");
        StopAllCoroutines();
        _isTyping = true;
        StartCoroutine(TypeSentence(_currentSentence));
    }

    public bool TrySkipTyping()
    {
        if (!_isTyping) return false;
        StopAllCoroutines();
        _dialogueText.text = _currentSentence ?? "";
        _isTyping = false;
        return true;
    }

    public void Close()
    {
        StopAllCoroutines();
        _isTyping = false;
        ClearButtons();
        _panel.SetActive(false);
    }

    private IEnumerator TypeSentence(string sentence)
    {
        _isTyping = true;
        _dialogueText.text = "";
        foreach (char letter in (sentence ?? "").ToCharArray())
        {
            _dialogueText.text += letter;
            yield return new WaitForSeconds(0.05f);
        }
        _isTyping = false;
    }
}

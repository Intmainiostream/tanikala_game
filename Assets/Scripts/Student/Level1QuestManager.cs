using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Level1QuestManager : MonoBehaviour
{
    [System.Serializable]
    public class NewspaperEntry
    {
        public GameObject newspaper;

        [Header("Leave empty to not show")]
        [TextArea] public string dialogueText;
        [TextArea] public string innerMonologueText;

        [TextArea] public string believeText;
        [TextArea] public string questionText;

        public string lessonTitle;
        [TextArea] public string lessonDescription;
    }

    [Header("Newspaper Sequence (order: 1,2,3,4,5,Letter,6,7)")]
    public NewspaperEntry[] newspapers;

    [Header("Panels")]
    public GameObject questPanel;
    public GameObject dialoguePanel;
    public GameObject innerMonologuePanel;
    public GameObject choicesPanel;
    public GameObject responsePanel;
    public GameObject lessonPanel;

    [Header("Dialogue")]
    public TMP_Text dialogueText;
    public TMP_Text innerMonologueText;

    [Header("Choices")]
    public Button believeBtn;
    public Button questionBtn;

    [Header("Response")]
    public TMP_Text responseText;

    [Header("Lesson")]
    public TMP_Text lessonTitleText;
    public TMP_Text lessonDescriptionText;

    [Header("Tap Anywhere")]
    public Button tapAnywhereBtn;

    [Header("HUDs to hide during quest")]
    public GameObject[] huds;

    private int currentIndex = 0;

    private enum QuestState { Newspaper, Dialogue, Choices, Response, Lesson }
    private QuestState state;

    void Start()
    {
        questPanel.SetActive(false);
        tapAnywhereBtn.onClick.AddListener(OnTap);
        believeBtn.onClick.AddListener(() => OnChoice(true));
        questionBtn.onClick.AddListener(() => OnChoice(false));
    }

    public void StartQuest()
    {
        currentIndex = 0;
        questPanel.SetActive(true);
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        // Show all newspapers at start (stacked look)
        foreach (NewspaperEntry entry in newspapers)
            if (entry.newspaper != null) entry.newspaper.SetActive(true);

        ShowNewspaper();
    }

    void ShowNewspaper()
    {
        state = QuestState.Newspaper;


        dialoguePanel.SetActive(false);
        innerMonologuePanel.SetActive(false);
        choicesPanel.SetActive(false);
        responsePanel.SetActive(false);
        lessonPanel.SetActive(false);
        tapAnywhereBtn.gameObject.SetActive(true);
    }

    void OnTap()
    {
        switch (state)
        {
            case QuestState.Newspaper:
                ShowDialogue();
                break;

            case QuestState.Dialogue:
                ShowChoices();
                break;

            case QuestState.Response:
                ShowLesson();
                break;

            case QuestState.Lesson:
                NextNewspaper();
                break;
        }
    }

    void ShowDialogue()
    {
        state = QuestState.Dialogue;
        var entry = newspapers[currentIndex];

        dialogueText.text = entry.dialogueText;
        dialoguePanel.SetActive(!string.IsNullOrEmpty(entry.dialogueText));

        innerMonologueText.text = entry.innerMonologueText;
        innerMonologuePanel.SetActive(!string.IsNullOrEmpty(entry.innerMonologueText));
    }

    void ShowChoices()
    {
        state = QuestState.Choices;
        dialoguePanel.SetActive(false);
        innerMonologuePanel.SetActive(false);
        choicesPanel.SetActive(true);
        tapAnywhereBtn.gameObject.SetActive(false);
    }

    void OnChoice(bool believed)
    {
        state = QuestState.Response;
        choicesPanel.SetActive(false);
        var entry = newspapers[currentIndex];

        string text = believed ? entry.believeText : entry.questionText;
        responseText.text = text;
        responsePanel.SetActive(!string.IsNullOrEmpty(text));

        tapAnywhereBtn.gameObject.SetActive(true);
    }

    void ShowLesson()
    {
        state = QuestState.Lesson;
        responsePanel.SetActive(false);

        var entry = newspapers[currentIndex];
        bool hasLesson = !string.IsNullOrEmpty(entry.lessonTitle) || !string.IsNullOrEmpty(entry.lessonDescription);

        if (hasLesson)
        {
            lessonTitleText.text = entry.lessonTitle;
            lessonDescriptionText.text = entry.lessonDescription;
            lessonPanel.SetActive(true);
        }
        else
        {
            lessonPanel.SetActive(false);
            NextNewspaper();
        }
    }

    void NextNewspaper()
    {
        // Hide the current newspaper (remove from stack)
        if (newspapers[currentIndex].newspaper != null)
            newspapers[currentIndex].newspaper.SetActive(false);

        currentIndex++;

        if (currentIndex >= newspapers.Length)
        {
            EndQuest();
            return;
        }

        ShowNewspaper();
    }

    void EndQuest()
    {
        questPanel.SetActive(false);
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(true);
    }
}

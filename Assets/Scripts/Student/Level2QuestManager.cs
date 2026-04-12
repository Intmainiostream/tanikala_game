using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Level2QuestManager : MonoBehaviour
{
    [System.Serializable]
    public class DocumentEntry
    {
        public GameObject document;

        [Header("Correct Answer (tick = must Approve, untick = must Not Approve)")]
        public bool mustApprove;

        [Header("Leave empty to not show")]
        [TextArea] public string dialogueText;
        [TextArea] public string innerMonologueText;

        [TextArea] public string approveText;
        [TextArea] public string notApproveText;

        public string lessonTitle;
        [TextArea] public string lessonDescription;

        [Header("Sounds")]
        public AudioClip dialogueSound;
        public AudioClip innerMonologueSound;
        public AudioClip approveSound;
        public AudioClip notApproveSound;
    }

    [Header("Document Sequence")]
    public DocumentEntry[] documents;

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
    public Button approveBtn;
    public Button notApproveBtn;

    [Header("Response")]
    public TMP_Text responseText;

    [Header("Lesson")]
    public TMP_Text lessonTitleText;
    public TMP_Text lessonDescriptionText;

    [Header("Tap Anywhere")]
    public Button tapAnywhereBtn;

    [Header("HUDs to hide during quest")]
    public GameObject[] huds;

    [Header("Level Complete")]
    public LevelCompleteManager levelCompleteManager;
    public PanelSequence endPanel;

    [Header("Stamp Sound")]
    public AudioClip stampSound;

    [Header("Supervisor Voice")]
    public AudioClip supervisorApproveVoice;
    public AudioClip supervisorDontApproveVoice;

    [Header("Main Character Voice")]
    public AudioClip complyVoice;

    [Header("Stamp Effect")]
    public GameObject stampImage;

    private AudioSource audioSource;
    private int currentIndex = 0;

    private enum QuestState { Document, Dialogue, Choices, Response, Lesson }
    private QuestState state;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        questPanel.SetActive(false);
        tapAnywhereBtn.onClick.AddListener(OnTap);
        approveBtn.onClick.AddListener(() => OnChoice(true));
        notApproveBtn.onClick.AddListener(() => OnChoice(false));
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    System.Collections.IEnumerator PlaySupervisorThenComply(AudioClip supervisorClip)
    {
        if (supervisorClip != null)
        {
            PlaySound(supervisorClip);
            yield return new WaitForSeconds(supervisorClip.length);
        }
        PlaySound(complyVoice);
    }

    System.Collections.IEnumerator LoopAfterVoice(AudioClip supervisorClip)
    {
        if (supervisorClip != null)
        {
            PlaySound(supervisorClip);
            yield return new WaitForSeconds(supervisorClip.length);
        }
        if (complyVoice != null)
        {
            PlaySound(complyVoice);
            yield return new WaitForSeconds(complyVoice.length);
        }
        ShowDocument();
    }

    public void StartQuest()
    {
        currentIndex = 0;
        questPanel.SetActive(true);
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        // Hide all documents, then only show the first
        foreach (DocumentEntry entry in documents)
            if (entry.document != null) entry.document.SetActive(false);

        if (documents[0].document != null) documents[0].document.SetActive(true);

        ShowDocument();
    }

    void ShowDocument()
    {
        state = QuestState.Document;

        dialoguePanel.SetActive(false);
        innerMonologuePanel.SetActive(false);
        choicesPanel.SetActive(false);
        responsePanel.SetActive(false);
        lessonPanel.SetActive(false);
        tapAnywhereBtn.gameObject.SetActive(true);

        if (stampImage != null) stampImage.SetActive(false);

        PlaySound(stampSound);
    }

    void OnTap()
    {
        switch (state)
        {
            case QuestState.Document:
                ShowDialogue();
                break;

            case QuestState.Dialogue:
                ShowChoices();
                break;

            case QuestState.Response:
                ShowLesson();
                break;

            case QuestState.Lesson:
                NextDocument();
                break;
        }
    }

    void ShowDialogue()
    {
        state = QuestState.Dialogue;
        var entry = documents[currentIndex];

        dialogueText.text = entry.dialogueText;
        bool showDialogue = !string.IsNullOrEmpty(entry.dialogueText);
        dialoguePanel.SetActive(showDialogue);
        if (showDialogue) PlaySound(entry.dialogueSound);

        innerMonologueText.text = entry.innerMonologueText;
        bool showInner = !string.IsNullOrEmpty(entry.innerMonologueText);
        innerMonologuePanel.SetActive(showInner);
        if (showInner && !showDialogue) PlaySound(entry.innerMonologueSound);
    }

    void ShowChoices()
    {
        state = QuestState.Choices;
        dialoguePanel.SetActive(false);
        innerMonologuePanel.SetActive(false);
        choicesPanel.SetActive(true);
        tapAnywhereBtn.gameObject.SetActive(false);
    }

    void OnChoice(bool approved)
    {
        var entry = documents[currentIndex];
        choicesPanel.SetActive(false);

        if (approved == entry.mustApprove)
        {
            // Correct — no voice, just proceed
            state = QuestState.Response;

            // Only show stamp if mustApprove is checked
            if (entry.mustApprove && stampImage != null)
            {
                stampImage.SetActive(false);
                stampImage.SetActive(true);
            }
        }
        else
        {
            // Wrong — boss scolds, then loop back
            AudioClip bossVoice = entry.mustApprove ? supervisorApproveVoice : supervisorDontApproveVoice;
            StartCoroutine(LoopAfterVoice(bossVoice));
            return;
        }

        string text = approved ? entry.approveText : entry.notApproveText;
        responseText.text = text;
        bool showResponse = !string.IsNullOrEmpty(text);
        responsePanel.SetActive(showResponse);
        if (showResponse) PlaySound(approved ? entry.approveSound : entry.notApproveSound);

        tapAnywhereBtn.gameObject.SetActive(true);
    }

    void ShowLesson()
    {
        state = QuestState.Lesson;
        responsePanel.SetActive(false);
        if (stampImage != null) stampImage.SetActive(false);

        var entry = documents[currentIndex];
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
            NextDocument();
        }
    }

    void NextDocument()
    {
        if (stampImage != null) stampImage.SetActive(false);

        // Hide current document
        if (documents[currentIndex].document != null)
            documents[currentIndex].document.SetActive(false);

        currentIndex++;

        if (currentIndex >= documents.Length)
        {
            EndQuest();
            return;
        }

        // Show next document
        if (documents[currentIndex].document != null)
            documents[currentIndex].document.SetActive(true);

        ShowDocument();
    }

    void EndQuest()
    {
        audioSource.Stop();
        questPanel.SetActive(false);
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        if (endPanel != null)
        {
            // Show interact btn (huds[1]) so player can tap through end panel
            if (huds.Length > 1 && huds[1] != null) huds[1].SetActive(true);

            endPanel.gameObject.SetActive(true);
            endPanel.onComplete = () =>
            {
                if (huds.Length > 1 && huds[1] != null) huds[1].SetActive(false);
                if (levelCompleteManager != null)
                    levelCompleteManager.OnLevelComplete();
            };
        }
        else
        {
            if (levelCompleteManager != null)
                levelCompleteManager.OnLevelComplete();
        }
    }
}

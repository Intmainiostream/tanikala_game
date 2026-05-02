using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;

public class Level8QuestManager : MonoBehaviour
{
    [System.Serializable]
    public class DocumentEntry
    {
        public GameObject document;

        [Header("Leave empty to not show")]
        [TextArea] public string innerMonologueText;
        [TextArea] public string approveText;
        [TextArea] public string holdText;

        public string lessonTitle;
        [TextArea] public string lessonDescription;

        [Header("Sounds")]
        public AudioClip innerMonologueSound;
        public AudioClip approveSound;
        public AudioClip holdSound;
    }

    [Header("Document Sequence")]
    public DocumentEntry[] documents;

    [Header("Panels")]
    public GameObject questPanel;
    public GameObject innerMonologuePanel;
    public GameObject choicesPanel;
    public GameObject lessonPanel;

    [Header("Inner Monologue")]
    public TMP_Text innerMonologueText;

    [Header("Choices")]
    public Button approveBtn;
    public Button holdBtn;

    [Header("Lesson")]
    public TMP_Text lessonTitleText;
    public TMP_Text lessonDescriptionText;

    [Header("Tap Anywhere")]
    public Button tapAnywhereBtn;

    [Header("HUDs to hide during quest")]
    public GameObject[] huds;

    [Header("Cutscene (plays after all documents)")]
    public PlayableDirector cutscene;
    public GameObject cutscenePanel;

    [Header("End Panel (image, shows for 4 seconds after cutscene)")]
    public GameObject endPanel;

    [Header("Level Complete")]
    public LevelCompleteManager levelCompleteManager;

    [Header("Document Sound")]
    public AudioClip documentSound;

    [Header("Stamp Effect")]
    public GameObject stampImage;

    [Header("Stamp Sound")]
    public AudioClip stampSound;

    private AudioSource audioSource;
    private int currentIndex = 0;

    private enum QuestState { Document, InnerMonologue, Choices, PostChoice, Lesson }
    private QuestState state;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        questPanel.SetActive(false);
        tapAnywhereBtn.onClick.AddListener(OnTap);
        approveBtn.onClick.AddListener(() => OnChoice(true));
        holdBtn.onClick.AddListener(() => OnChoice(false));
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void StartQuest()
    {
        currentIndex = 0;
        questPanel.SetActive(true);
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        foreach (DocumentEntry entry in documents)
            if (entry.document != null) entry.document.SetActive(false);

        if (documents[0].document != null) documents[0].document.SetActive(true);

        ShowDocument();
    }

    void ShowDocument()
    {
        state = QuestState.Document;

        innerMonologuePanel.SetActive(false);
        choicesPanel.SetActive(false);
        lessonPanel.SetActive(false);
        tapAnywhereBtn.gameObject.SetActive(true);
        if (stampImage != null) stampImage.SetActive(false);

        PlaySound(documentSound);
    }

    void OnTap()
    {
        switch (state)
        {
            case QuestState.Document:       ShowInnerMonologue(); break;
            case QuestState.InnerMonologue: ShowChoices();        break;
            case QuestState.PostChoice:     ShowLesson();         break;
            case QuestState.Lesson:         NextDocument();       break;
        }
    }

    void ShowInnerMonologue()
    {
        state = QuestState.InnerMonologue;
        var entry = documents[currentIndex];

        innerMonologueText.text = entry.innerMonologueText;
        bool showInner = !string.IsNullOrEmpty(entry.innerMonologueText);
        innerMonologuePanel.SetActive(showInner);
        if (showInner) PlaySound(entry.innerMonologueSound);
    }

    void ShowChoices()
    {
        state = QuestState.Choices;
        innerMonologuePanel.SetActive(false);
        choicesPanel.SetActive(true);
        tapAnywhereBtn.gameObject.SetActive(false);
    }

    void OnChoice(bool approved)
    {
        choicesPanel.SetActive(false);
        var entry = documents[currentIndex];

        string text = approved ? entry.approveText : entry.holdText;
        AudioClip sound = approved ? entry.approveSound : entry.holdSound;

        if (approved)
        {
            if (stampImage != null) { stampImage.SetActive(false); stampImage.SetActive(true); }
            PlaySound(stampSound);
        }

        bool showText = !string.IsNullOrEmpty(text);
        if (showText)
        {
            state = QuestState.PostChoice;
            innerMonologueText.text = text;
            innerMonologuePanel.SetActive(true);
            tapAnywhereBtn.gameObject.SetActive(true);
            if (sound != null) PlaySound(sound);
        }
        else
        {
            if (sound != null) PlaySound(sound);
            ShowLesson();
        }
    }

    void ShowLesson()
    {
        state = QuestState.Lesson;
        innerMonologuePanel.SetActive(false);
        if (stampImage != null) stampImage.SetActive(false);

        var entry = documents[currentIndex];
        bool hasLesson = !string.IsNullOrEmpty(entry.lessonTitle) || !string.IsNullOrEmpty(entry.lessonDescription);

        if (hasLesson)
        {
            lessonTitleText.text = entry.lessonTitle;
            lessonDescriptionText.text = entry.lessonDescription;
            lessonPanel.SetActive(true);
            tapAnywhereBtn.gameObject.SetActive(true);
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
        if (documents[currentIndex].document != null)
            documents[currentIndex].document.SetActive(false);

        currentIndex++;

        if (currentIndex >= documents.Length)
        {
            EndQuest();
            return;
        }

        if (documents[currentIndex].document != null)
            documents[currentIndex].document.SetActive(true);

        ShowDocument();
    }

    void EndQuest()
    {
        audioSource.Stop();
        questPanel.SetActive(false);
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        // Disable all interactables
        foreach (InteractableObject obj in FindObjectsOfType<InteractableObject>())
        {
            if (obj.questionMark != null) obj.questionMark.SetActive(false);
            obj.enabled = false;
            Collider2D col = obj.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        if (cutscene != null)
        {
            if (cutscenePanel != null) cutscenePanel.SetActive(true);
            cutscene.stopped += OnCutsceneFinished;
            cutscene.Play();
        }
        else
        {
            StartCoroutine(ShowEndSequence());
        }
    }

    void OnCutsceneFinished(PlayableDirector director)
    {
        cutscene.stopped -= OnCutsceneFinished;
        if (cutscenePanel != null) cutscenePanel.SetActive(false);
        StartCoroutine(ShowEndSequence());
    }

    IEnumerator ShowEndSequence()
    {
        if (endPanel != null)
        {
            endPanel.SetActive(true);
            yield return new WaitForSeconds(6f);
            endPanel.SetActive(false);
        }

        if (levelCompleteManager != null) levelCompleteManager.OnLevelComplete();
    }
}

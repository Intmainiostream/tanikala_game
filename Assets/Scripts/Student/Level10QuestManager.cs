using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;

public class Level10QuestManager : MonoBehaviour
{
    // ─── Data ────────────────────────────────────────────────────────────────

    public enum QuestionType { MCQ, TrueOrFalse }

    [System.Serializable]
    public class QuizQuestion
    {
        public QuestionType type;
        [TextArea] public string questionText;

        [Header("MCQ — set correctChoice: 0=A  1=B  2=C  3=D")]
        public string choiceA;
        public string choiceB;
        public string choiceC;
        public string choiceD;
        public int correctChoice;

        [Header("True/False — is the statement TRUE?")]
        public bool isTrue;

        [Header("Wrong answer feedback")]
        [TextArea] public string wrongExplanation;

        [Header("Sounds")]
        public AudioClip correctSound;
        public AudioClip wrongSound;
    }

    // ─── Inspector ───────────────────────────────────────────────────────────

    [Header("Quiz Questions")]
    public QuizQuestion[] questions;

    [Header("Quest Panel (root)")]
    public GameObject questPanel;

    [Header("Question Text")]
    public TMP_Text questionText;

    [Header("MCQ")]
    public GameObject choicesPanel;
    public Button btnA;
    public Button btnB;
    public Button btnC;
    public Button btnD;
    public TMP_Text textA;
    public TMP_Text textB;
    public TMP_Text textC;
    public TMP_Text textD;

    [Header("True / False")]
    public GameObject trueFalseBtnPanel;
    public Button trueBtn;
    public Button falseBtn;

    [Header("Timer")]
    public GameObject timerPanel;
    public TMP_Text timerText;
    public float questionTime = 30f;

    [Header("Wrong Answer Panel")]
    public GameObject falsePanel;
    public TMP_Text falsePanelText;

    [Header("Tap Anywhere (shown after answer)")]
    public Button tapAnywhereBtn;

    [Header("End Dialogue (PanelSequence shown after all questions)")]
    public PanelSequence endPanelSequence;

    [Header("HUDs to hide during quest")]
    public GameObject[] huds;

    [Header("Level Complete")]
    public LevelCompleteManager levelCompleteManager;

    // ─── Private ─────────────────────────────────────────────────────────────

    private AudioSource audioSource;
    private int currentIndex = 0;
    private bool showingFalsePanel = false;
    private int score = 0;
    private int[] shuffledOrder;
    private float timeLeft = 0f;
    private bool timerRunning = false;
    private float totalTimeSpent = 0f;
    private bool isProcessing = false;

    private const int MCQ_POINTS = 3;
    private const int TF_POINTS  = 5;

    // ─── Unity ───────────────────────────────────────────────────────────────

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        questPanel.SetActive(false);

        btnA.onClick.AddListener(() => OnMCQ(0));
        btnB.onClick.AddListener(() => OnMCQ(1));
        btnC.onClick.AddListener(() => OnMCQ(2));
        btnD.onClick.AddListener(() => OnMCQ(3));

        trueBtn.onClick.AddListener(() => OnTrueFalse(true));
        falseBtn.onClick.AddListener(() => OnTrueFalse(false));

        tapAnywhereBtn.onClick.AddListener(OnTapAnywhere);
        tapAnywhereBtn.gameObject.SetActive(false);

        if (timerPanel != null) timerPanel.SetActive(false);
    }

    void Update()
    {
        if (!timerRunning) return;

        timeLeft -= Time.deltaTime;
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, timeLeft)).ToString();

        if (timeLeft <= 0f)
        {
            timerRunning = false;
            OnTimeUp();
        }
    }

    // ─── Entry point ─────────────────────────────────────────────────────────

    public void StartQuest()
    {
        currentIndex = 0;
        score = 0;
        totalTimeSpent = 0f;
        shuffledOrder = ShuffleIndices(questions.Length);
        questPanel.SetActive(true);
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);
        ShowQuestion();
    }

    // ─── Show current question ───────────────────────────────────────────────

    void ShowQuestion()
    {
        isProcessing = false;
        Debug.Log($"[Quiz] ShowQuestion index={currentIndex} — isProcessing reset to false");
        audioSource.Stop();
        HideAllSubPanels();
        tapAnywhereBtn.gameObject.SetActive(false);

        var q = questions[shuffledOrder[currentIndex]];
        questionText.text = q.questionText;

        if (q.type == QuestionType.MCQ)
        {
            textA.text = q.choiceA;
            textB.text = q.choiceB;
            textC.text = q.choiceC;
            textD.text = q.choiceD;
            choicesPanel.SetActive(true);
        }
        else
        {
            trueFalseBtnPanel.SetActive(true);
        }

        StartTimer();
    }

    // ─── Timer ───────────────────────────────────────────────────────────────

    void StartTimer()
    {
        timeLeft = questionTime;
        timerRunning = true;
        if (timerPanel != null) timerPanel.SetActive(true);
        if (timerText != null) timerText.text = Mathf.CeilToInt(questionTime).ToString();
    }

    void StopTimer()
    {
        timerRunning = false;
        if (timerPanel != null) timerPanel.SetActive(false);
    }

    void OnTimeUp()
    {
        totalTimeSpent += questionTime;
        StopTimer();
        var q = questions[shuffledOrder[currentIndex]];
        PlaySound(q.wrongSound);
        ShowFalsePanel(q.wrongExplanation);
    }

    // ─── MCQ ─────────────────────────────────────────────────────────────────

    void OnMCQ(int chosen)
    {
        if (isProcessing) { Debug.Log("[Quiz] OnMCQ BLOCKED by isProcessing"); return; }
        isProcessing = true;
        Debug.Log($"[Quiz] OnMCQ chosen={chosen} index={currentIndex} isProcessing={isProcessing}");
        totalTimeSpent += questionTime - Mathf.Max(0f, timeLeft);
        StopTimer();
        var q = questions[shuffledOrder[currentIndex]];
        if (chosen == q.correctChoice)
        {
            score += MCQ_POINTS;
            PlaySound(q.correctSound);
            Advance();
        }
        else
        {
            PlaySound(q.wrongSound);
            ShowFalsePanel(q.wrongExplanation);
        }
    }

    // ─── True / False ─────────────────────────────────────────────────────────

    void OnTrueFalse(bool playerSaidTrue)
    {
        if (isProcessing) { Debug.Log("[Quiz] OnTrueFalse BLOCKED by isProcessing"); return; }
        isProcessing = true;
        Debug.Log($"[Quiz] OnTrueFalse playerSaidTrue={playerSaidTrue} index={currentIndex}");
        totalTimeSpent += questionTime - Mathf.Max(0f, timeLeft);
        StopTimer();
        var q = questions[shuffledOrder[currentIndex]];
        if (playerSaidTrue == q.isTrue)
        {
            score += TF_POINTS;
            PlaySound(q.correctSound);
            Advance();
        }
        else
        {
            PlaySound(q.wrongSound);
            ShowFalsePanel(q.wrongExplanation);
        }
    }

    // ─── Wrong answer feedback ───────────────────────────────────────────────

    void ShowFalsePanel(string explanation)
    {
        isProcessing = false;
        Debug.Log($"[Quiz] ShowFalsePanel — isProcessing reset to false, tapBtn shown");
        HideAllSubPanels();
        falsePanelText.text = explanation;
        falsePanel.SetActive(true);
        showingFalsePanel = true;
        tapAnywhereBtn.gameObject.SetActive(true);
    }

    // ─── Progress ────────────────────────────────────────────────────────────

    void Advance()
    {
        isProcessing = false;
        Debug.Log($"[Quiz] Advance — isProcessing reset to false, tapBtn shown");
        HideAllSubPanels();
        tapAnywhereBtn.gameObject.SetActive(true);
    }

    void OnTapAnywhere()
    {
        Debug.Log($"[Quiz] OnTapAnywhere fired — index={currentIndex} isProcessing={isProcessing}");
        tapAnywhereBtn.gameObject.SetActive(false);
        showingFalsePanel = false;
        falsePanel.SetActive(false);

        currentIndex++;
        Debug.Log($"[Quiz] OnTapAnywhere advancing to index={currentIndex}");
        if (currentIndex >= questions.Length)
            EndQuiz();
        else
            ShowQuestion();
    }

    void EndQuiz()
    {
        StopTimer();
        questPanel.SetActive(false);

        if (huds.Length > 1 && huds[1] != null)
            huds[1].SetActive(true);

        SaveScoreToFirestore(() =>
        {
            if (endPanelSequence != null)
            {
                endPanelSequence.gameObject.SetActive(true);
                endPanelSequence.onComplete = OnLevelComplete;
            }
            else
            {
                OnLevelComplete();
            }
        });
    }

    void SaveScoreToFirestore(System.Action onDone)
    {
        if (GlobalUserData.IsGuest || string.IsNullOrEmpty(GlobalUserData.UserId))
        {
            onDone?.Invoke();
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("users").Document(GlobalUserData.UserId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsFaulted && task.Result.Exists && task.Result.ToDictionary().ContainsKey("quiz_score"))
                {
                    onDone?.Invoke();
                    return;
                }

                var updates = new Dictionary<string, object>
                {
                    { "quiz_score", score },
                    { "quiz_time", totalTimeSpent },
                    { "quiz_submitted_at", FieldValue.ServerTimestamp }
                };
                db.Collection("users").Document(GlobalUserData.UserId)
                    .UpdateAsync(updates)
                    .ContinueWithOnMainThread(updateTask =>
                    {
                        if (updateTask.IsFaulted)
                            Debug.LogError("Failed to save quiz score: " + updateTask.Exception);
                        onDone?.Invoke();
                    });
            });
    }

    void OnLevelComplete()
    {
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(true);
        if (levelCompleteManager != null)
            levelCompleteManager.OnLevelComplete();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    void HideAllSubPanels()
    {
        choicesPanel.SetActive(false);
        trueFalseBtnPanel.SetActive(false);
        falsePanel.SetActive(false);
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    int[] ShuffleIndices(int count)
    {
        int[] indices = new int[count];
        for (int i = 0; i < count; i++) indices[i] = i;
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = indices[i]; indices[i] = indices[j]; indices[j] = tmp;
        }
        return indices;
    }
}

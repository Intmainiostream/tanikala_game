using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using TMPro;

public class Level4QuestManager : MonoBehaviour
{
    [System.Serializable]
    public class FlyerEntry
    {
        public GameObject flyerImage;
        [TextArea] public string innerMonologueText;
        public AudioClip innerMonologueSound;
    }

    [Header("Flyers (index 0 = Bench1, index 1 = Bench2)")]
    public FlyerEntry[] flyers;

    [Header("Panels")]
    public GameObject questPanel;
    public GameObject innerMonologuePanel;

    [Header("Inner Monologue")]
    public TMP_Text innerMonologueText;

    [Header("Tap Anywhere")]
    public Button tapAnywhereBtn;

    [Header("HUDs")]
    public GameObject[] huds; // huds[0] = controller, huds[1] = interact btn

    [Header("Celia - reveals benches when her dialogue finishes")]
    public PanelSequence celiaPanelSequence;
    public GameObject bench1Object;
    public GameObject bench2Object;

    [Header("Cutscene")]
    public PlayableDirector cutscene;
    public GameObject cutscenePanel; // parent of all the activation panels

    [Header("Level Complete")]
    public LevelCompleteManager levelCompleteManager;

    private AudioSource audioSource;
    private bool[] flyersFound;
    private int currentFlyerIndex = -1;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        flyersFound = new bool[flyers.Length];

        questPanel.SetActive(false);
        tapAnywhereBtn.onClick.AddListener(OnTap);

        // Hide benches until Celia is talked to
        if (bench1Object != null) bench1Object.SetActive(false);
        if (bench2Object != null) bench2Object.SetActive(false);
    }

    // Called by InteractableObject after Celia's PanelSequence ends
    public void StartQuest()
    {
        if (bench1Object != null) bench1Object.SetActive(true);
        if (bench2Object != null) bench2Object.SetActive(true);
        if (huds.Length > 1 && huds[1] != null) huds[1].SetActive(true);
    }

    // Called by InteractableObject on the bench (flyerIndex 0 or 1)
    public void OnFlyerInteracted(int index)
    {
        if (index < 0 || index >= flyers.Length) return;

        currentFlyerIndex = index;

        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        questPanel.SetActive(true);

        // Show only this flyer image
        for (int i = 0; i < flyers.Length; i++)
            if (flyers[i].flyerImage != null)
                flyers[i].flyerImage.SetActive(i == index);

        var entry = flyers[index];
        innerMonologueText.text = entry.innerMonologueText;
        innerMonologuePanel.SetActive(!string.IsNullOrEmpty(entry.innerMonologueText));

        if (entry.innerMonologueSound != null)
        {
            audioSource.Stop();
            audioSource.clip = entry.innerMonologueSound;
            audioSource.Play();
        }

        tapAnywhereBtn.gameObject.SetActive(true);
    }

    void OnTap()
    {
        audioSource.Stop();
        innerMonologuePanel.SetActive(false);

        if (currentFlyerIndex >= 0 && currentFlyerIndex < flyers.Length)
            if (flyers[currentFlyerIndex].flyerImage != null)
                flyers[currentFlyerIndex].flyerImage.SetActive(false);

        questPanel.SetActive(false);
        tapAnywhereBtn.gameObject.SetActive(false);

        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(true);

        if (currentFlyerIndex >= 0) flyersFound[currentFlyerIndex] = true;
        currentFlyerIndex = -1;

        bool allFound = true;
        foreach (bool found in flyersFound) if (!found) { allFound = false; break; }
        if (allFound) TriggerEnd();
    }

    void TriggerEnd()
    {
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        // Disable all interactables so triggers stop firing
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
            if (levelCompleteManager != null) levelCompleteManager.OnLevelComplete();
        }
    }

    void OnCutsceneFinished(PlayableDirector director)
    {
        cutscene.stopped -= OnCutsceneFinished;
        if (levelCompleteManager != null) levelCompleteManager.OnLevelComplete();
    }

}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class Level9QuestManager : MonoBehaviour
{
    [Header("Water Bottle (hidden after interacted)")]
    public GameObject waterBottleObject;

    [Header("NPCs to give water to (order matches Level9WaterRecipient npcIndex)")]
    public InteractableObject[] npcInteractables;

    [Header("HUDs to hide on complete")]
    public GameObject[] huds;

    [Header("Level Complete")]
    public LevelCompleteManager levelCompleteManager;

    [Header("Score Panel")]
    public GameObject scorePanel;
    public TMP_Text firstRecordText;
    public TMP_Text currentRecordText;
    public Button tapAnywhereScoreBtn;

    private bool waterBottlePickedUp = false;
    private bool[] npcGiven;
    private float startTime;

    void Start()
    {
        startTime = Time.time;
        npcGiven = new bool[npcInteractables.Length];
        SetNPCsInteractable(false);
    }

    public void StartQuest()
    {
        Debug.Log("[Level9] StartQuest called. waterBottlePickedUp=" + waterBottlePickedUp);
        if (waterBottlePickedUp) return;

        waterBottlePickedUp = true;
        if (waterBottleObject != null) waterBottleObject.SetActive(false);
        else Debug.LogWarning("[Level9] waterBottleObject is NULL!");
        SetNPCsInteractable(true);
        Debug.Log("[Level9] Water bottle picked up. NPCs unlocked.");
    }

    public void OnNPCGiven(int index, InteractableObject npc)
    {
        Debug.Log($"[Level9] OnNPCGiven index={index}, waterBottlePickedUp={waterBottlePickedUp}, already given={npcGiven[index]}");
        if (!waterBottlePickedUp) return;
        if (index < 0 || index >= npcGiven.Length) return;
        if (npcGiven[index]) return;

        npcGiven[index] = true;

        npc.enabled = false;
        if (npc.questionMark != null) npc.questionMark.SetActive(false);
        Collider2D col = npc.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        int doneCount = 0;
        foreach (bool g in npcGiven) if (g) doneCount++;
        Debug.Log($"[Level9] NPC {index} given. {doneCount}/{npcGiven.Length} total.");

        CheckAllGiven();
    }

    void CheckAllGiven()
    {
        foreach (bool given in npcGiven)
            if (!given) return;

        Debug.Log("[Level9] All NPCs given water! Triggering complete.");
        TriggerComplete();
    }

    void TriggerComplete()
    {
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);
        SaveAndShowScore();
    }

    void SaveAndShowScore()
    {
        float elapsed = Time.time - startTime;

        string uid = GlobalUserData.UserId;
        if (string.IsNullOrEmpty(uid)) { ProceedToEnd(); return; }

        var db = FirebaseFirestore.DefaultInstance;
        var userRef = db.Collection("users").Document(uid);

        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            float firstRecord = elapsed;

            if (task.IsCompletedSuccessfully)
            {
                if (task.Result.TryGetValue("level9_data", out object raw) && raw is Dictionary<string, object> d && d.ContainsKey("time"))
                    firstRecord = System.Convert.ToSingle(d["time"]);
                else
                    userRef.UpdateAsync("level9_data", new Dictionary<string, object> { { "time", elapsed } });
            }

            if (firstRecordText != null)  firstRecordText.text  = $"{Mathf.RoundToInt(firstRecord)}s";
            if (currentRecordText != null) currentRecordText.text = $"{Mathf.RoundToInt(elapsed)}s";

            if (scorePanel != null)
            {
                scorePanel.SetActive(true);
                if (tapAnywhereScoreBtn != null)
                {
                    tapAnywhereScoreBtn.gameObject.SetActive(true);
                    tapAnywhereScoreBtn.onClick.RemoveAllListeners();
                    tapAnywhereScoreBtn.onClick.AddListener(() =>
                    {
                        tapAnywhereScoreBtn.gameObject.SetActive(false);
                        scorePanel.SetActive(false);
                        ProceedToEnd();
                    });
                }
            }
            else
            {
                ProceedToEnd();
            }
        });
    }

    void ProceedToEnd()
    {
        if (levelCompleteManager != null) levelCompleteManager.OnLevelComplete();
        else Debug.LogWarning("[Level9] levelCompleteManager is NULL!");
    }

    void SetNPCsInteractable(bool state)
    {
        foreach (InteractableObject npc in npcInteractables)
        {
            if (npc == null) continue;
            npc.enabled = state;
            Collider2D col = npc.GetComponent<Collider2D>();
            if (col != null) col.enabled = state;
        }
    }
}

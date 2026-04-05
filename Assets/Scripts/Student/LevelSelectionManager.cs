using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using Firebase.Extensions;

public class LevelSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelEntry
    {
        public GameObject finished;
        public GameObject unlocked;
        public GameObject locked;
        public Button button;
    }

    [Header("Levels (assign Lvl1 to Lvl10 in order)")]
    public LevelEntry[] levels; // size 10, index 0 = Level 1

    private const int TOTAL_LEVELS = 10;
    private FirebaseFirestore db;

    void Start()
    {
        if (GlobalUserData.IsGuest)
        {
            InitGuestProgressIfNeeded();
            ApplyProgress(LoadGuestProgress());
        }
        else
        {
            db = FirebaseFirestore.DefaultInstance;
            LoadStudentProgress();
        }
    }

    // ─────────────────────────────────────────────
    // GUEST — PlayerPrefs
    // ─────────────────────────────────────────────

    void InitGuestProgressIfNeeded()
    {
        if (!PlayerPrefs.HasKey("level_1"))
        {
            PlayerPrefs.SetString("level_1", "unlocked");
            for (int i = 2; i <= TOTAL_LEVELS; i++)
                PlayerPrefs.SetString("level_" + i, "locked");
            PlayerPrefs.Save();
        }
    }

    Dictionary<string, string> LoadGuestProgress()
    {
        var progress = new Dictionary<string, string>();
        for (int i = 1; i <= TOTAL_LEVELS; i++)
            progress[i.ToString()] = PlayerPrefs.GetString("level_" + i, "locked");
        return progress;
    }

    // ─────────────────────────────────────────────
    // STUDENT — Firestore
    // ─────────────────────────────────────────────

    void LoadStudentProgress()
    {
        if (string.IsNullOrEmpty(GlobalUserData.UserId))
        {
            Debug.LogWarning("UserId is empty — falling back to default progress.");
            ApplyProgress(BuildDefaultProgress());
            return;
        }

        db.Collection("users").Document(GlobalUserData.UserId)
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || !task.Result.Exists)
                {
                    Debug.LogError("Failed to load level progress: " + task.Exception);
                    ApplyProgress(BuildDefaultProgress());
                    return;
                }

                Dictionary<string, string> progress;

                if (task.Result.TryGetValue("level_progress", out Dictionary<string, object> raw))
                {
                    progress = new Dictionary<string, string>();
                    foreach (var kvp in raw)
                        progress[kvp.Key] = kvp.Value.ToString();
                }
                else
                {
                    // First time — initialize progress
                    progress = BuildDefaultProgress();
                    SaveStudentProgress(progress);
                }

                ApplyProgress(progress);
            });
    }

    Dictionary<string, string> BuildDefaultProgress()
    {
        var progress = new Dictionary<string, string>();
        progress["1"] = "unlocked";
        for (int i = 2; i <= TOTAL_LEVELS; i++)
            progress[i.ToString()] = "locked";
        return progress;
    }

    void SaveStudentProgress(Dictionary<string, string> progress)
    {
        Dictionary<string, object> toSave = new Dictionary<string, object>();
        foreach (var kvp in progress)
            toSave[kvp.Key] = kvp.Value;

        db.Collection("users").Document(GlobalUserData.UserId)
            .SetAsync(new Dictionary<string, object> { { "level_progress", toSave } }, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Failed to save level progress: " + task.Exception);
            });
    }

    // ─────────────────────────────────────────────
    // APPLY PROGRESS TO UI
    // ─────────────────────────────────────────────

    void ApplyProgress(Dictionary<string, string> progress)
    {
        for (int i = 0; i < levels.Length; i++)
        {
            int levelNumber = i + 1;
            string key = levelNumber.ToString();
            string status = progress.ContainsKey(key) ? progress[key] : "locked";

            // Level 1 has no locked state — always at least unlocked
            if (levelNumber == 1 && status == "locked")
                status = "unlocked";

            LevelEntry entry = levels[i];

            if (entry.finished != null) entry.finished.SetActive(status == "finished");
            if (entry.unlocked != null) entry.unlocked.SetActive(status == "unlocked");
            if (entry.locked != null)   entry.locked.SetActive(status == "locked");

            bool isPlayable = status == "unlocked" || status == "finished";
            if (entry.button != null)
            {
                entry.button.interactable = isPlayable;
                int captured = levelNumber;
                entry.button.onClick.RemoveAllListeners();
                entry.button.onClick.AddListener(() => OnLevelClicked(captured));
            }
        }
    }

    // ─────────────────────────────────────────────
    // ON LEVEL CLICKED
    // ─────────────────────────────────────────────

    void OnLevelClicked(int levelNumber)
    {
        GlobalUserData.CurrentLevel = levelNumber;
        SceneManager.LoadScene("Level" + levelNumber + "Scene");
    }

    // ─────────────────────────────────────────────
    // PUBLIC: Call this from Level1Scene when a level is completed
    // ─────────────────────────────────────────────

    public static void MarkLevelFinished(int levelNumber)
    {
        int nextLevel = levelNumber + 1;

        if (GlobalUserData.IsGuest)
        {
            PlayerPrefs.SetString("level_" + levelNumber, "finished");
            if (nextLevel <= 10)
                PlayerPrefs.SetString("level_" + nextLevel, "unlocked");
            PlayerPrefs.Save();
        }
        else
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            db.Collection("users").Document(GlobalUserData.UserId)
                .GetSnapshotAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || !task.Result.Exists) return;

                    Dictionary<string, object> updates = new Dictionary<string, object>();
                    updates["level_progress." + levelNumber] = "finished";
                    if (nextLevel <= 10)
                        updates["level_progress." + nextLevel] = "unlocked";

                    db.Collection("users").Document(GlobalUserData.UserId)
                        .UpdateAsync(updates);
                });
        }
    }
}

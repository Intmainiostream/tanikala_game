using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Row Prefab")]
    public GameObject rowPrefab;
    public Transform tableContent;

    [Header("S.Y. Text (optional)")]
    public TMP_Text syText;

    [Header("Loading indicator (optional)")]
    public GameObject loadingPanel;

    [Header("Refresh Button (optional)")]
    public Button refreshBtn;

    private FirebaseFirestore db;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        if (refreshBtn != null)
            refreshBtn.onClick.AddListener(LoadLeaderboard);

        LoadLeaderboard();
    }

    public void LoadLeaderboard()
    {
        ClearTable();
        if (loadingPanel != null) loadingPanel.SetActive(true);

        // Fetch S.Y. first then load leaderboard
        db.Collection("settings").Document("app")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(syTask =>
            {
                if (!syTask.IsFaulted && syTask.Result.Exists &&
                    syTask.Result.TryGetValue("school_year", out string sy))
                {
                    if (syText != null) syText.text = $"S.Y. {sy}";
                }

                FetchLeaderboardEntries();
            });
    }

    void FetchLeaderboardEntries()
    {
        db.Collection("users")
            .WhereEqualTo("role", "student")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (loadingPanel != null) loadingPanel.SetActive(false);

                if (task.IsFaulted)
                {
                    Debug.LogError("Leaderboard fetch failed: " + task.Exception);
                    return;
                }

                var entries = new List<(string name, string lastName, int score, double quizTime, System.DateTime submittedAt)>();

                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    if (!doc.TryGetValue("is_verified", out bool verified) || !verified) continue;

                    var d = doc.ToDictionary();
                    if (!d.ContainsKey("quiz_score")) continue;
                    int quizScore = System.Convert.ToInt32(d["quiz_score"]);
                    double quizTime = d.ContainsKey("quiz_time") ? System.Convert.ToDouble(d["quiz_time"]) : double.MaxValue;
                    System.DateTime submittedAt = System.DateTime.MinValue;
                    if (d.ContainsKey("quiz_submitted_at") && d["quiz_submitted_at"] is Timestamp ts)
                        submittedAt = ts.ToDateTime();

                    string first  = d.ContainsKey("first_name")  ? d["first_name"].ToString()  : "";
                    string middle = d.ContainsKey("middle_name") ? d["middle_name"].ToString() : "";
                    string last   = d.ContainsKey("last_name")   ? d["last_name"].ToString()   : "";

                    string mi = !string.IsNullOrEmpty(middle) ? $" {middle[0]}." : "";
                    string displayName = $"{last}, {first}{mi}".Trim();

                    entries.Add((displayName, last, quizScore, quizTime, submittedAt));
                }

                var top10 = entries
                    .OrderByDescending(e => e.score)
                    .ThenBy(e => e.quizTime)
                    .ThenByDescending(e => e.submittedAt)
                    .ThenBy(e => e.lastName, System.StringComparer.OrdinalIgnoreCase)
                    .Take(10)
                    .ToList();

                for (int i = 0; i < top10.Count; i++)
                    CreateRow(i + 1, top10[i].name, top10[i].score);
            });
    }


    void CreateRow(int rank, string name, int score)
    {
        GameObject row = Instantiate(rowPrefab, tableContent);
        Transform t = row.transform;

        SetText(t, "Rank",       rank.ToString());
        SetText(t, "PlayerName", name);
        SetText(t, "Score",      score + " / 80");
    }

    void ClearTable()
    {
        foreach (Transform child in tableContent)
            Destroy(child.gameObject);
    }

    void SetText(Transform parent, string childName, string value)
    {
        Transform child = parent.Find(childName);
        if (child == null) return;
        TMP_Text t = child.GetComponent<TMP_Text>();
        if (t != null) t.text = value;
    }
}

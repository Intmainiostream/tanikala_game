using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;

public class SchoolYearManager : MonoBehaviour
{
    [Header("S.Y. Text fields across all panels (assign all)")]
    public TMP_Text[] syTexts;

    [Header("Confirm Button")]
    public Button confirmBtn;

    [Header("Leaderboard (refreshed after archiving)")]
    public LeaderboardManager leaderboardManager;

    [Header("Processing Panel (shown while archiving)")]
    public GameObject processingPanel;

    [Header("Success Panel")]
    public GameObject successPanel;

    [Header("Failed Panel (shown when archiving fails)")]
    public GameObject failedPanel;

    [Header("Lock Message (shown when button is disabled)")]
    public TMP_Text lockMessageText;

    private FirebaseFirestore db;
    private string currentSY = "";

    private const string SETTINGS_COLLECTION = "settings";
    private const string SETTINGS_DOC        = "app";

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        if (confirmBtn != null) confirmBtn.onClick.AddListener(OnConfirm);
        if (processingPanel != null) processingPanel.SetActive(false);
        if (successPanel != null)    successPanel.SetActive(false);
        if (failedPanel != null)     failedPanel.SetActive(false);

        FetchCurrentSY();
    }

    void FetchCurrentSY()
    {
        db.Collection(SETTINGS_COLLECTION).Document(SETTINGS_DOC)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) return;

                string sy = "";
                int updatedYear = -1;

                if (task.Result.Exists)
                {
                    task.Result.TryGetValue("school_year", out sy);
                    if (task.Result.ToDictionary().ContainsKey("sy_updated_year"))
                        updatedYear = System.Convert.ToInt32(task.Result.ToDictionary()["sy_updated_year"]);
                }

                currentSY = sy ?? "";
                UpdateSYTexts(currentSY);
                UpdateConfirmBtnState(updatedYear);
            });
    }

    void UpdateConfirmBtnState(int updatedYear)
    {
        if (confirmBtn == null) return;

        bool canUpdate = true;
        string lockMsg = "";

        if (updatedYear > 0)
        {
            System.DateTime unlockDate = new System.DateTime(updatedYear + 1, 6, 1);
            canUpdate = System.DateTime.Now >= unlockDate;
            if (!canUpdate)
                lockMsg = $"Next update available on June 1, {updatedYear + 1}.";
        }

        confirmBtn.interactable = canUpdate;

        if (lockMessageText != null)
        {
            lockMessageText.text = lockMsg;
            lockMessageText.gameObject.SetActive(!canUpdate);
        }
    }

    void UpdateSYTexts(string sy)
    {
        string display = string.IsNullOrEmpty(sy) ? "Not Set" : sy;
        foreach (TMP_Text t in syTexts)
            if (t != null) t.text = display;
    }

    string IncrementSY(string sy)
    {
        // Expects format "2026-2027"
        string[] parts = sy.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
            return $"{start + 1}-{end + 1}";

        // Fallback: can't parse, just return as-is
        return sy;
    }

    void OnConfirm()
    {
        string newSY = string.IsNullOrEmpty(currentSY) ? "2025-2026" : IncrementSY(currentSY);

        if (processingPanel != null) processingPanel.SetActive(true);
        if (confirmBtn != null) confirmBtn.interactable = false;

        SaveSYAndArchiveStudents(newSY);
    }

    void SaveSYAndArchiveStudents(string newSY)
    {
        var settings = new Dictionary<string, object>
        {
            { "school_year", newSY },
            { "sy_updated_year", System.DateTime.Now.Year }
        };

        db.Collection(SETTINGS_COLLECTION).Document(SETTINGS_DOC)
            .SetAsync(settings, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to save S.Y.: " + task.Exception);
                    if (processingPanel != null) processingPanel.SetActive(false);
                    if (confirmBtn != null) confirmBtn.interactable = true;
                    return;
                }

                currentSY = newSY;
                UpdateSYTexts(newSY);
                ArchiveAllStudents(newSY);
            });
    }

    void ArchiveAllStudents(string sy)
    {
        db.Collection("users")
            .WhereEqualTo("role", "student")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to fetch students: " + task.Exception);
                    if (processingPanel != null) processingPanel.SetActive(false);
                    if (confirmBtn != null) confirmBtn.interactable = true;
                    return;
                }

                var docs = new List<DocumentSnapshot>(task.Result.Documents);
                if (docs.Count == 0)
                {
                    OnArchivingDone();
                    return;
                }

                int remaining = docs.Count;
                int failed = 0;

                foreach (var doc in docs)
                {
                    string docID = doc.Id;
                    Dictionary<string, object> data = doc.ToDictionary();
                    data["archivedAt"]  = Timestamp.GetCurrentTimestamp();
                    data["school_year"] = sy;

                    db.Collection("archived_users").Document(docID).SetAsync(data)
                        .ContinueWithOnMainThread(setTask =>
                        {
                            if (!setTask.IsCompletedSuccessfully)
                            {
                                failed++;
                                remaining--;
                                if (remaining <= 0)
                                {
                                    if (processingPanel != null) processingPanel.SetActive(false);
                                    if (confirmBtn != null) confirmBtn.interactable = true;
                                    if (failedPanel != null) StartCoroutine(ShowFailedPanel());
                                }
                                return;
                            }

                            db.Collection("users").Document(docID).DeleteAsync()
                                .ContinueWithOnMainThread(_ =>
                                {
                                    remaining--;
                                    if (remaining <= 0 && failed == 0) OnArchivingDone();
                                });
                        });
                }
            });
    }

    void OnArchivingDone()
    {
        if (processingPanel != null) processingPanel.SetActive(false);
        if (confirmBtn != null) confirmBtn.interactable = false;
        if (leaderboardManager != null) leaderboardManager.LoadLeaderboard();
        StartCoroutine(ShowSuccessPanel());
    }

    IEnumerator ShowSuccessPanel()
    {
        if (successPanel != null) successPanel.SetActive(true);
        yield return new WaitForSeconds(3f);
        if (successPanel != null) successPanel.SetActive(false);
    }

    IEnumerator ShowFailedPanel()
    {
        if (failedPanel != null) failedPanel.SetActive(true);
        yield return new WaitForSeconds(3f);
        if (failedPanel != null) failedPanel.SetActive(false);
    }
}

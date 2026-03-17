using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;

public class ArchiveManager : MonoBehaviour
{
    [Header("Archive Student Table")]
    public GameObject archiveStudentRowPrefab;
    public Transform archiveStudentTableContent;
    public GameObject studentContainer;

    [Header("Archive Teacher Table")]
    public GameObject archiveTeacherRowPrefab;
    public Transform archiveTeacherTableContent;
    public GameObject teacherContainer;

    [Header("Filter")]
    public TMP_Dropdown userFilterDropdown;

    [Header("Search Panel")]
    public GameObject searchPanel;
    public Button openSearchBtn;
    public TMP_InputField searchField;
    public Button searchBtn;
    public Button closeSearchBtn;

    [Header("Restore Panel")]
    public GameObject restoreValidation;
    public Button restoreContinueBtn;
    public Button restoreCancelBtn;

    private string restoringDocID;

    [Header("Overlay")]
    public GameObject darkOverlay;

    [Header("Success Panel")]
    public GameObject successPanel;

    private FirebaseFirestore db;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        if (teacherContainer != null) teacherContainer.SetActive(false);

        if (userFilterDropdown != null)
            userFilterDropdown.onValueChanged.AddListener(OnFilterChanged);

        if (openSearchBtn != null)
            openSearchBtn.onClick.AddListener(() => searchPanel.SetActive(true));

        if (searchBtn != null)
            searchBtn.onClick.AddListener(SearchArchive);

        if (closeSearchBtn != null)
            closeSearchBtn.onClick.AddListener(() =>
            {
                ClearSearch();
                searchPanel.SetActive(false);
            });

        if (searchField != null)
            searchField.onValueChanged.AddListener(value =>
            {
                if (string.IsNullOrEmpty(value))
                    ClearSearch();
            });

        ListenToArchivedStudents();
        ListenToArchivedTeachers();

        if (restoreContinueBtn != null)
            restoreContinueBtn.onClick.AddListener(RestoreUser_Confirmed);

        if (restoreCancelBtn != null)
            restoreCancelBtn.onClick.AddListener(() =>
            {
                restoreValidation.SetActive(false);
                if (darkOverlay != null) darkOverlay.SetActive(false);
            });
    }

    // ─────────────────────────────────────────────
    // FILTER
    // ─────────────────────────────────────────────

    void OnFilterChanged(int index)
    {
        bool showStudents = index == 0;
        if (studentContainer != null) studentContainer.SetActive(showStudents);
        if (teacherContainer != null) teacherContainer.SetActive(!showStudents);
    }

    // ─────────────────────────────────────────────
    // REAL-TIME LISTENERS
    // ─────────────────────────────────────────────

    void ListenToArchivedStudents()
    {
        db.Collection("archived_users")
            .WhereEqualTo("role", "student")
            .Listen(snapshot =>
            {
                ClearTable(archiveStudentTableContent);

                var sorted = snapshot.Documents
                    .OrderBy(doc =>
                    {
                        var d = doc.ToDictionary();
                        string ln = d.ContainsKey("last_name") ? d["last_name"].ToString() : "";
                        string fn = d.ContainsKey("first_name") ? d["first_name"].ToString() : "";
                        return $"{ln},{fn}";
                    }).ToList();

                for (int i = 0; i < sorted.Count; i++)
                    CreateArchiveRow(sorted[i].ToDictionary(), sorted[i].Id, i + 1, archiveStudentRowPrefab, archiveStudentTableContent);
            });
    }

    void ListenToArchivedTeachers()
    {
        db.Collection("archived_users")
            .WhereEqualTo("role", "teacher")
            .Listen(snapshot =>
            {
                ClearTable(archiveTeacherTableContent);

                var sorted = snapshot.Documents
                    .OrderBy(doc =>
                    {
                        var d = doc.ToDictionary();
                        string ln = d.ContainsKey("last_name") ? d["last_name"].ToString() : "";
                        string fn = d.ContainsKey("first_name") ? d["first_name"].ToString() : "";
                        return $"{ln},{fn}";
                    }).ToList();

                for (int i = 0; i < sorted.Count; i++)
                    CreateArchiveRow(sorted[i].ToDictionary(), sorted[i].Id, i + 1, archiveTeacherRowPrefab, archiveTeacherTableContent);
            });
    }

    void CreateArchiveRow(Dictionary<string, object> data, string docID, int index, GameObject prefab, Transform content)
    {
        GameObject row = Instantiate(prefab, content);
        row.name = docID;
        Transform panel = row.transform.Find("Panel");
        if (panel == null) return;

        string firstName  = data.ContainsKey("first_name")  ? data["first_name"].ToString()  : "";
        string middleName = data.ContainsKey("middle_name") ? data["middle_name"].ToString() : "";
        string lastName   = data.ContainsKey("last_name")   ? data["last_name"].ToString()   : "";
        string email      = data.ContainsKey("email")       ? data["email"].ToString()       : "";

        string middleInitial = !string.IsNullOrEmpty(middleName) ? $" {middleName[0]}." : "";
        string displayName = $"{lastName}, {firstName}{middleInitial}".Trim();

        SetText(panel, "NumberText", index.ToString());
        SetText(panel, "NameText", displayName);
        SetText(panel, "EmailText", email);

        Button restoreBtn = panel.Find("RestoreBtn")?.GetComponent<Button>();
        if (restoreBtn != null)
        {
            string capturedID = docID;
            restoreBtn.onClick.AddListener(() => OpenRestorePanel(capturedID));
        }
    }

    // ─────────────────────────────────────────────
    // RESTORE
    // ─────────────────────────────────────────────

    void OpenRestorePanel(string docID)
    {
        restoringDocID = docID;
        if (darkOverlay != null) darkOverlay.SetActive(true);
        restoreValidation.SetActive(true);
    }

    void RestoreUser_Confirmed()
    {
        DocumentReference archiveRef = db.Collection("archived_users").Document(restoringDocID);

        archiveRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                Debug.LogError("❌ Archived user not found: " + task.Exception);
                return;
            }

            Dictionary<string, object> data = task.Result.ToDictionary();
            data.Remove("archivedAt");

            db.Collection("users").Document(restoringDocID).SetAsync(data)
                .ContinueWithOnMainThread(setTask =>
                {
                    if (!setTask.IsCompletedSuccessfully)
                    {
                        Debug.LogError("❌ Failed to restore: " + setTask.Exception);
                        return;
                    }

                    archiveRef.DeleteAsync().ContinueWithOnMainThread(deleteTask =>
                    {
                        if (deleteTask.IsCompletedSuccessfully)
                        {
                            Debug.Log("✅ User restored.");
                            StartCoroutine(ShowSuccessPanel());
                        }
                        else
                        {
                            Debug.LogError("❌ Failed to delete from archived_users: " + deleteTask.Exception);
                        }
                    });
                });
        });
    }

    // ─────────────────────────────────────────────
    // SEARCH
    // ─────────────────────────────────────────────

    void SearchArchive()
    {
        string query = searchField.text.Trim().ToLower();
        if (string.IsNullOrEmpty(query)) return;

        bool isStudentView = userFilterDropdown == null || userFilterDropdown.value == 0;
        Transform activeContent = isStudentView ? archiveStudentTableContent : archiveTeacherTableContent;

        foreach (Transform row in activeContent)
        {
            Transform panel = row.Find("Panel");
            if (panel == null) continue;

            Transform nameTransform = panel.Find("NameText");
            if (nameTransform == null) continue;

            TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
            if (nameText == null) continue;

            bool matches = nameText.text.ToLower().Contains(query);
            row.gameObject.SetActive(matches);
        }
    }

    void ClearSearch()
    {
        searchField.text = "";
        bool isStudentView = userFilterDropdown == null || userFilterDropdown.value == 0;
        Transform activeContent = isStudentView ? archiveStudentTableContent : archiveTeacherTableContent;
        foreach (Transform row in activeContent)
            row.gameObject.SetActive(true);
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    void SetText(Transform parent, string childName, string value)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            TMP_Text t = child.GetComponent<TMP_Text>();
            if (t != null) t.text = value;
        }
        else
        {
            Debug.LogWarning($"⚠️ '{childName}' not found under {parent.name}");
        }
    }

    void ClearTable(Transform content)
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
    }

    IEnumerator ShowSuccessPanel()
    {
        if (darkOverlay != null) darkOverlay.SetActive(false);
        successPanel.SetActive(true);
        yield return new WaitForSeconds(3f);
        successPanel.SetActive(false);
    }
}
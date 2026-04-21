using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;

public class RecordManager : MonoBehaviour
{
    [Header("Table")]
    public GameObject rowPrefab;
    public Transform tableContent;

    [Header("Search Panel")]
    public GameObject searchPanel;
    public Button openSearchBtn;
    public TMP_InputField searchField;
    public Button searchBtn;
    public Button closeSearchBtn;

    [Header("Refresh")]
    public Button refreshBtn;

    private FirebaseFirestore db;
    private ListenerRegistration listener;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        if (openSearchBtn != null)  openSearchBtn.onClick.AddListener(() => searchPanel.SetActive(true));
        if (closeSearchBtn != null) closeSearchBtn.onClick.AddListener(() => { ClearSearch(); searchPanel.SetActive(false); });
        if (searchBtn != null)      searchBtn.onClick.AddListener(SearchRecords);
        if (refreshBtn != null)     refreshBtn.onClick.AddListener(ListenToRecords);

        if (searchField != null)
            searchField.onValueChanged.AddListener(val => { if (string.IsNullOrEmpty(val)) ClearSearch(); });

        ListenToRecords();
    }

    void ListenToRecords()
    {
        ClearTable();
        if (listener != null) listener.Stop();

        listener = db.Collection("users")
            .WhereEqualTo("role", "student")
            .Listen(snapshot =>
            {
                ClearTable();

                var sorted = snapshot.Documents
                    .Where(doc => doc.TryGetValue("is_verified", out bool v) && v)
                    .OrderBy(doc =>
                    {
                        var d = doc.ToDictionary();
                        string ln = d.ContainsKey("last_name")  ? d["last_name"].ToString()  : "";
                        string fn = d.ContainsKey("first_name") ? d["first_name"].ToString() : "";
                        return $"{ln},{fn}";
                    }).ToList();

                foreach (var doc in sorted)
                    CreateRow(doc.ToDictionary());
            });
    }

    void CreateRow(Dictionary<string, object> data)
    {
        GameObject row = Instantiate(rowPrefab, tableContent);
        Transform panel = row.transform.Find("Panel") ?? row.transform;

        string first  = data.ContainsKey("first_name")  ? data["first_name"].ToString()  : "";
        string middle = data.ContainsKey("middle_name") ? data["middle_name"].ToString() : "";
        string last   = data.ContainsKey("last_name")   ? data["last_name"].ToString()   : "";
        string mi = !string.IsNullOrEmpty(middle) ? $" {middle[0]}." : "";
        string displayName = $"{last}, {first}{mi}".Trim();

        int highestFinished = 0;
        if (data.ContainsKey("level_progress") && data["level_progress"] is Dictionary<string, object> progress)
        {
            for (int i = 1; i <= 10; i++)
            {
                string key = i.ToString();
                if (progress.ContainsKey(key) && progress[key].ToString() == "finished")
                    highestFinished = i;
            }
        }
        string progressText = highestFinished > 0 ? $"Level {highestFinished}/10" : "Not Started";
        string scoreText = data.ContainsKey("quiz_score") ? data["quiz_score"].ToString() : "—";

        SetText(panel, "NameText",     displayName);
        SetText(panel, "ProgressText", progressText);
        SetText(panel, "ScoreText",    scoreText);
    }

    void SearchRecords()
    {
        if (searchField == null) return;
        string query = searchField.text.Trim().ToLower();
        if (string.IsNullOrEmpty(query)) return;

        foreach (Transform row in tableContent)
        {
            Transform panel = row.Find("Panel") ?? row;
            Transform nameT = panel.Find("NameText");
            if (nameT == null) continue;
            TMP_Text t = nameT.GetComponent<TMP_Text>();
            if (t == null) continue;
            row.gameObject.SetActive(t.text.ToLower().Contains(query));
        }
    }

    void ClearSearch()
    {
        if (searchField != null) searchField.text = "";
        foreach (Transform row in tableContent)
            row.gameObject.SetActive(true);
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

    void OnDestroy()
    {
        if (listener != null) listener.Stop();
    }
}

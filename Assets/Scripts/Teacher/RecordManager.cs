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

        SetText(panel, "NameText", displayName);

        // Find the dropdown
        Transform ddTransform = panel.Find("LvlDropdown");
        TMP_Dropdown dropdown = ddTransform != null ? ddTransform.GetComponent<TMP_Dropdown>() : null;

        if (dropdown == null) return;

        // Populate Level 1–10
        dropdown.ClearOptions();
        var options = new List<string>();
        for (int i = 1; i <= 10; i++) options.Add($"Level {i}");
        dropdown.AddOptions(options);

        // Default to first completed level, or Level 1
        int defaultIdx = 0;
        if (data.ContainsKey("level_progress") && data["level_progress"] is Dictionary<string, object> prog)
        {
            for (int i = 1; i <= 10; i++)
            {
                if (prog.ContainsKey(i.ToString()) && prog[i.ToString()].ToString() == "finished")
                {
                    defaultIdx = i - 1;
                    break;
                }
            }
        }

        dropdown.value = defaultIdx;
        dropdown.RefreshShownValue();

        // Show initial score
        var capturedData = data;
        UpdateScore(panel, dropdown.value + 1, capturedData);

        // Update score when dropdown changes
        dropdown.onValueChanged.AddListener(idx => UpdateScore(panel, idx + 1, capturedData));
    }

    void UpdateScore(Transform panel, int lvl, Dictionary<string, object> data)
    {
        // Check if level is finished first
        bool finished = false;
        if (data.ContainsKey("level_progress") && data["level_progress"] is Dictionary<string, object> prog)
            finished = prog.ContainsKey(lvl.ToString()) && prog[lvl.ToString()].ToString() == "finished";

        string score = finished ? GetScoreText(lvl, data) : "Not Finished";
        SetText(panel, "ScoreText", score);
    }

    string GetScoreText(int lvl, Dictionary<string, object> data)
    {
        string key = $"level{lvl}_data";

        switch (lvl)
        {
            case 1:
            case 7:
                if (data.ContainsKey(key) && data[key] is Dictionary<string, object> td)
                {
                    int trust = td.ContainsKey("trust") ? System.Convert.ToInt32(td["trust"]) : 0;
                    int doubt = td.ContainsKey("doubt") ? System.Convert.ToInt32(td["doubt"]) : 0;
                    return $"{trust} Trust - {doubt} Doubt";
                }
                return "—";

            case 2:
            case 5:
                if (data.ContainsKey(key) && data[key] is Dictionary<string, object> ad)
                {
                    int approve    = ad.ContainsKey("approve")    ? System.Convert.ToInt32(ad["approve"])    : 0;
                    int notApprove = ad.ContainsKey("notApprove") ? System.Convert.ToInt32(ad["notApprove"]) : 0;
                    return $"{approve} Approve - {notApprove} NotApprove";
                }
                return "—";

            case 3:
            case 4:
            case 6:
            case 9:
                if (data.ContainsKey(key) && data[key] is Dictionary<string, object> timd)
                {
                    float time = timd.ContainsKey("time") ? System.Convert.ToSingle(timd["time"]) : 0f;
                    return $"{Mathf.RoundToInt(time)}s";
                }
                return "—";

            case 8:
                if (data.ContainsKey(key) && data[key] is Dictionary<string, object> hd)
                {
                    int approve = hd.ContainsKey("approve") ? System.Convert.ToInt32(hd["approve"]) : 0;
                    int hold    = hd.ContainsKey("hold")    ? System.Convert.ToInt32(hd["hold"])    : 0;
                    return $"{approve} Approve - {hold} Hold";
                }
                return "—";

            case 10:
                return data.ContainsKey("quiz_score") ? $"{data["quiz_score"]}pts" : "—";

            default:
                return "—";
        }
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

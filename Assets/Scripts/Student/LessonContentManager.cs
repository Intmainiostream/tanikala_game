using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;

public class LessonContentManager : MonoBehaviour
{
    [Header("Table")]
    public GameObject rowPrefab;
    public Transform tableContent;

    [Header("Panel")]
    public GameObject lessonPanel;
    public Button closeBtn;

    private FirebaseFirestore db;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        if (closeBtn != null) closeBtn.onClick.AddListener(() => lessonPanel.SetActive(false));

        lessonPanel.SetActive(false);
        LoadModules();
    }

    public void OpenPanel()
    {
        lessonPanel.SetActive(true);
        LoadModules();
    }

    void LoadModules()
    {
        db.Collection("modules").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) { Debug.LogError("Failed to load modules: " + task.Exception); return; }

            foreach (Transform child in tableContent) Destroy(child.gameObject);

            foreach (var doc in task.Result.Documents)
            {
                string name  = doc.ContainsField("name")   ? doc.GetValue<string>("name")   : "";
                string url   = doc.ContainsField("pdfUrl") ? doc.GetValue<string>("pdfUrl") : "";
                int    level = doc.ContainsField("level")  ? System.Convert.ToInt32(doc.GetValue<object>("level")) : 0;

                GameObject row = Instantiate(rowPrefab, tableContent);
                Transform panel = row.transform.Find("Panel") ?? row.transform;

                SetText(panel, "NameText", name);
                SetText(panel, "LvlText",  level > 0 ? "Level " + level : "—");

                Button viewBtn = panel.Find("ViewBtn")?.GetComponent<Button>();
                if (viewBtn != null)
                {
                    string capturedUrl = url;
                    viewBtn.interactable = !string.IsNullOrEmpty(capturedUrl);
                    viewBtn.onClick.AddListener(() =>
                    {
                        if (!string.IsNullOrEmpty(capturedUrl))
                            Application.OpenURL(capturedUrl);
                    });
                }
            }
        });
    }

    void SetText(Transform parent, string childName, string value)
    {
        Transform child = parent.Find(childName);
        if (child == null) return;
        TMP_Text t = child.GetComponent<TMP_Text>();
        if (t != null) t.text = value;
    }
}

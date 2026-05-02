using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Storage;

public class ContentManager : MonoBehaviour
{
    [Header("Table")]
    public GameObject rowPrefab;
    public Transform tableContent;

    [Header("Search")]
    public TMP_InputField searchField;

    [Header("Add Panel")]
    public GameObject addContentPanel;
    public Button addContentBtn;
    public TMP_InputField moduleNameInput;
    public TMP_Dropdown lvlDropdown;
    public Button uploadPdfBtn;
    public Button saveBtn;
    public Button closePanelBtn;
    public TMP_Text nameValidationText;
    public TMP_Text pdfValidationText;

    [Header("Feedback")]
    public GameObject darkBg;
    public GameObject successPanel;

    [Header("Loading Panel")]
    public GameObject loadingPanel;

    [Header("Remove Confirmation")]
    public GameObject removeValidationPanel;
    public Button confirmRemoveBtn;

    private FirebaseFirestore db;
    private FirebaseStorage storage;
    private string pendingFilePath = "";
    private string pendingRemoveDocId = "";
    private string pendingRemoveStoragePath = "";
    private GameObject pendingRemoveRow = null;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        storage = FirebaseStorage.DefaultInstance;

        addContentBtn.onClick.AddListener(OpenAddPanel);
        uploadPdfBtn.onClick.AddListener(PickPDF);
        saveBtn.onClick.AddListener(SaveModule);
        closePanelBtn.onClick.AddListener(CloseAddPanel);

        if (searchField != null)
            searchField.onValueChanged.AddListener(OnSearch);

        addContentPanel.SetActive(false);
        if (darkBg != null)             darkBg.SetActive(false);
        if (successPanel != null)       successPanel.SetActive(false);
        if (removeValidationPanel != null) removeValidationPanel.SetActive(false);
        if (loadingPanel != null)       loadingPanel.SetActive(false);

        if (confirmRemoveBtn != null) confirmRemoveBtn.onClick.AddListener(ConfirmRemove);

        LoadModules();
    }

    // ─── Load ────────────────────────────────────────────────────────────────

    void LoadModules()
    {
        db.Collection("modules").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) { Debug.LogError("Failed to load: " + task.Exception); return; }
            ClearTable();
            foreach (var doc in task.Result.Documents)
                SpawnRow(doc.Id, doc);
        });
    }

    void SpawnRow(string docId, DocumentSnapshot doc)
    {
        string name        = doc.ContainsField("name")        ? doc.GetValue<string>("name")        : "";
        string pdfUrl      = doc.ContainsField("pdfUrl")      ? doc.GetValue<string>("pdfUrl")      : "";
        string storagePath = doc.ContainsField("storagePath") ? doc.GetValue<string>("storagePath") : "";
        int    level       = doc.ContainsField("level")       ? System.Convert.ToInt32(doc.GetValue<object>("level")) : 0;

        string[] storagePathRef = { storagePath };
        string[] pdfUrlRef      = { pdfUrl };

        GameObject row = Instantiate(rowPrefab, tableContent);
        Transform panel = row.transform.Find("Panel") ?? row.transform;

        SetText(panel, "NameText",   name);
        SetText(panel, "StatusText", !string.IsNullOrEmpty(pdfUrl) ? "Uploaded" : "No PDF");

        Button uploadBtn = panel.Find("UploadBtn")?.GetComponent<Button>();
        Button removeBtn = panel.Find("RemoveBtn")?.GetComponent<Button>();
        Button viewBtn   = panel.Find("ViewBtn")?.GetComponent<Button>();

        if (uploadBtn != null) uploadBtn.onClick.AddListener(() => OnUploadClicked(docId, panel, storagePathRef, pdfUrlRef, viewBtn, uploadBtn));
        if (removeBtn != null) removeBtn.onClick.AddListener(() => OnRemoveClicked(docId, storagePathRef[0], row));
        if (viewBtn != null)
        {
            viewBtn.interactable = !string.IsNullOrEmpty(pdfUrl);
            viewBtn.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(pdfUrlRef[0]))
                    Application.OpenURL(pdfUrlRef[0]);
            });
        }
    }

    // ─── Add Panel ───────────────────────────────────────────────────────────

    void OpenAddPanel()
    {
        moduleNameInput.text = "";
        pendingFilePath = "";
        nameValidationText.text = "";
        pdfValidationText.text = "";
        SetUploadBtnLabel("UPLOAD");
        if (darkBg != null) darkBg.SetActive(true);
        addContentPanel.SetActive(true);
    }

    void CloseAddPanel()
    {
        addContentPanel.SetActive(false);
        if (darkBg != null) darkBg.SetActive(false);
        pendingFilePath = "";
    }

    // ─── Pick PDF ────────────────────────────────────────────────────────────

    void PickPDF()
    {
        NativeFilePicker.PickFile(path =>
        {
            if (string.IsNullOrEmpty(path)) return;
            pendingFilePath = path;
            pdfValidationText.text = "";
            string fileName = System.IO.Path.GetFileName(path);
            SetUploadBtnLabel(fileName.Length > 20 ? fileName.Substring(0, 20) + "..." : fileName);
        }, new string[] { "application/pdf" });
    }

    void SetUploadBtnLabel(string label)
    {
        TMP_Text t = uploadPdfBtn.GetComponentInChildren<TMP_Text>();
        if (t != null) t.text = label;
    }

    // ─── Save ────────────────────────────────────────────────────────────────

    void SaveModule()
    {
        bool valid = true;

        if (string.IsNullOrWhiteSpace(moduleNameInput.text))
        {
            nameValidationText.text = "* Required";
            valid = false;
        }
        else nameValidationText.text = "";

        if (string.IsNullOrEmpty(pendingFilePath))
        {
            pdfValidationText.text = "* Required";
            valid = false;
        }
        else pdfValidationText.text = "";

        if (!valid) return;

        if (saveBtn != null) saveBtn.interactable = false;
        if (loadingPanel != null) loadingPanel.SetActive(true);

        string moduleName  = moduleNameInput.text.Trim();
        string localPath   = pendingFilePath;
        string fileName    = System.IO.Path.GetFileName(localPath);

        CloseAddPanel();
        string storagePath = "modules/" + System.Guid.NewGuid() + "_" + fileName;

        byte[] fileBytes;
        try { fileBytes = System.IO.File.ReadAllBytes(localPath); }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to read file: " + e.Message);
            if (saveBtn != null) saveBtn.interactable = true;
            if (loadingPanel != null) loadingPanel.SetActive(false);
            return;
        }

        var metadata = new Firebase.Storage.MetadataChange { ContentType = "application/pdf" };
        storage.GetReference(storagePath).PutBytesAsync(fileBytes, metadata).ContinueWithOnMainThread(uploadTask =>
        {
            if (uploadTask.IsFaulted)
            {
                Debug.LogError("Upload failed: " + uploadTask.Exception);
                if (saveBtn != null) saveBtn.interactable = true;
                if (loadingPanel != null) loadingPanel.SetActive(false);
                return;
            }

            storage.GetReference(storagePath).GetDownloadUrlAsync().ContinueWithOnMainThread(urlTask =>
            {
                if (urlTask.IsFaulted)
                {
                    Debug.LogError("URL failed: " + urlTask.Exception);
                    if (saveBtn != null) saveBtn.interactable = true;
                    if (loadingPanel != null) loadingPanel.SetActive(false);
                    return;
                }

                string pdfUrl = urlTask.Result.ToString();
                var data = new Dictionary<string, object>
                {
                    { "name",        moduleName },
                    { "pdfUrl",      pdfUrl },
                    { "storagePath", storagePath },
                    { "createdAt",   Timestamp.GetCurrentTimestamp() }
                };

                db.Collection("modules").AddAsync(data).ContinueWithOnMainThread(addTask =>
                {
                    if (saveBtn != null) saveBtn.interactable = true;
                    if (loadingPanel != null) loadingPanel.SetActive(false);
                    if (addTask.IsFaulted) { Debug.LogError("Firestore failed: " + addTask.Exception); return; }
                    pendingFilePath = "";
                    LoadModules();
                    ShowSuccess();
                });
            });
        });
    }

    // ─── Upload for existing row (replaces old file) ──────────────────────────

    public void OnUploadClicked(string docId, Transform panel, string[] storagePathRef, string[] pdfUrlRef = null, Button viewBtn = null, Button uploadBtn = null)
    {
        if (uploadBtn != null) uploadBtn.interactable = false;

        NativeFilePicker.PickFile(path =>
        {
            if (string.IsNullOrEmpty(path))
            {
                if (uploadBtn != null) uploadBtn.interactable = true;
                return;
            }

            if (loadingPanel != null) loadingPanel.SetActive(true);

            string oldPath  = storagePathRef[0];
            string fileName = System.IO.Path.GetFileName(path);
            string newPath  = "modules/" + System.Guid.NewGuid() + "_" + fileName;

            byte[] fileBytes;
            try { fileBytes = System.IO.File.ReadAllBytes(path); }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to read file: " + e.Message);
                if (uploadBtn != null) uploadBtn.interactable = true;
                if (loadingPanel != null) loadingPanel.SetActive(false);
                return;
            }

            var metadata = new Firebase.Storage.MetadataChange { ContentType = "application/pdf" };
            storage.GetReference(newPath).PutBytesAsync(fileBytes, metadata).ContinueWithOnMainThread(uploadTask =>
            {
                if (uploadTask.IsFaulted)
                {
                    Debug.LogError("Upload failed: " + uploadTask.Exception);
                    if (uploadBtn != null) uploadBtn.interactable = true;
                    if (loadingPanel != null) loadingPanel.SetActive(false);
                    return;
                }

                storage.GetReference(newPath).GetDownloadUrlAsync().ContinueWithOnMainThread(urlTask =>
                {
                    if (urlTask.IsFaulted)
                    {
                        if (uploadBtn != null) uploadBtn.interactable = true;
                        if (loadingPanel != null) loadingPanel.SetActive(false);
                        return;
                    }

                    string pdfUrl = urlTask.Result.ToString();
                    db.Collection("modules").Document(docId).UpdateAsync(new Dictionary<string, object>
                    {
                        { "pdfUrl",      pdfUrl },
                        { "storagePath", newPath }
                    }).ContinueWithOnMainThread(_ =>
                    {
                        if (!string.IsNullOrEmpty(oldPath))
                            storage.GetReference(oldPath).DeleteAsync();

                        storagePathRef[0] = newPath;
                        if (pdfUrlRef  != null) pdfUrlRef[0] = pdfUrl;
                        if (viewBtn    != null) viewBtn.interactable = true;
                        if (uploadBtn  != null) uploadBtn.interactable = true;
                        if (loadingPanel != null) loadingPanel.SetActive(false);
                        ShowSuccess();
                    });
                });
            });
        }, new string[] { "application/pdf" });
    }

    // ─── Remove ──────────────────────────────────────────────────────────────

    public void OnRemoveClicked(string docId, string storagePath, GameObject row)
    {
        pendingRemoveDocId       = docId;
        pendingRemoveStoragePath = storagePath;
        pendingRemoveRow         = row;

        if (darkBg != null)             darkBg.SetActive(true);
        if (removeValidationPanel != null) removeValidationPanel.SetActive(true);
    }

    void ConfirmRemove()
    {
        if (confirmRemoveBtn != null) confirmRemoveBtn.interactable = false;
        if (darkBg != null)             darkBg.SetActive(false);
        if (removeValidationPanel != null) removeValidationPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(true);

        db.Collection("modules").Document(pendingRemoveDocId).DeleteAsync().ContinueWithOnMainThread(_ =>
        {
            if (confirmRemoveBtn != null) confirmRemoveBtn.interactable = true;
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (!string.IsNullOrEmpty(pendingRemoveStoragePath))
                storage.GetReference(pendingRemoveStoragePath).DeleteAsync();
            Destroy(pendingRemoveRow);
            pendingRemoveRow = null;
            ShowSuccess();
        });
    }

    // ─── Success Panel ───────────────────────────────────────────────────────

    void ShowSuccess()
    {
        if (successPanel != null) StartCoroutine(SuccessCoroutine());
    }

    IEnumerator SuccessCoroutine()
    {
        successPanel.SetActive(true);
        yield return new WaitForSeconds(2f);
        successPanel.SetActive(false);
    }

    // ─── Search ──────────────────────────────────────────────────────────────

    void OnSearch(string query)
    {
        foreach (Transform row in tableContent)
        {
            Transform panel = row.Find("Panel") ?? row;
            Transform nameT = panel.Find("NameText");
            if (nameT == null) continue;
            TMP_Text t = nameT.GetComponent<TMP_Text>();
            bool match = string.IsNullOrEmpty(query) || (t != null && t.text.ToLower().Contains(query.ToLower()));
            row.gameObject.SetActive(match);
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    void ClearTable()
    {
        foreach (Transform child in tableContent) Destroy(child.gameObject);
    }

    void SetText(Transform parent, string childName, string value)
    {
        Transform child = parent.Find(childName);
        if (child == null) return;
        TMP_Text t = child.GetComponent<TMP_Text>();
        if (t != null) t.text = value;
    }
}

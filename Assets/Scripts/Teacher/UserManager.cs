using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;

public class UserManager : MonoBehaviour
{

    public Button refreshBtn;

    [Header("Student Table")]
    public GameObject studentRowPrefab;
    public Transform studentTableContent;
    public GameObject studentContainer;

    [Header("Teacher Table")]
    public GameObject teacherRowPrefab;
    public Transform teacherTableContent;
    public GameObject teacherContainer;

    [Header("Filter")]
    public TMP_Dropdown userFilterDropdown;
    [Header("Success Panel")]
    public GameObject SuccessEditPanel;
    public GameObject SuccessPanel;
    [Header("Add Student Panel")]
    public GameObject addStudentContainer;
    public TMP_InputField studentEmailField;
    public TMP_InputField studentPasswordField;
    public TMP_InputField studentLastNameField;
    public TMP_InputField studentFirstNameField;
    public TMP_InputField studentMiddleNameField;
    public Button studentGenerateBtn;
    public Button studentSaveBtn;
    public TextMeshProUGUI studentEmailValidation;
    public TextMeshProUGUI studentPasswordValidation;
    public TextMeshProUGUI studentLastNameValidation;
    public TextMeshProUGUI studentFirstNameValidation;

    [Header("Add Teacher Panel")]
    public GameObject addTeacherContainer;
    public TMP_InputField teacherEmailField;
    public TMP_InputField teacherPasswordField;
    public TMP_InputField teacherLastNameField;
    public TMP_InputField teacherFirstNameField;
    public TMP_InputField teacherMiddleNameField;
    public Button teacherGenerateBtn;
    public Button teacherSaveBtn;
    public TextMeshProUGUI teacherEmailValidation;
    public TextMeshProUGUI teacherPasswordValidation;
    public TextMeshProUGUI teacherLastNameValidation;
    public TextMeshProUGUI teacherFirstNameValidation;

    [Header("Edit User Panel")]
    public GameObject editUserContainer;
    public TMP_InputField editFirstNameField;
    public TMP_InputField editMiddleNameField;
    public TMP_InputField editLastNameField;
    public Button editSaveBtn;
    public Button editCancelBtn;
    public TextMeshProUGUI editFirstNameValidation;
    public TextMeshProUGUI editLastNameValidation;

    private string editingDocID;

    [Header("Open Panel Buttons")]
    public Button addStudentBtn;
    public Button addTeacherBtn;

    [Header("Search Panel")]
    public GameObject SearchPanel;
    public Button openSearchBtn;
    public TMP_InputField searchField;
    public Button searchBtn;
    public Button closeSearchBtn; 

    [Header("Archive Panel")]
    public GameObject archiveValidation;
    public Button archiveContinueBtn;
    public Button archiveCancelBtn;

    private string archivingDocID;

    [Header("Overlay")]
    public GameObject darkOverlay;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private ListenerRegistration studentListener;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        if (addStudentBtn != null)
            addStudentBtn.onClick.AddListener(() =>
            {
                ClearStudentValidations();
                addStudentContainer.SetActive(true);
            });

        if (addTeacherBtn != null)
            addTeacherBtn.onClick.AddListener(() =>
            {
                ClearTeacherValidations();
                addTeacherContainer.SetActive(true);
            });

        if (studentGenerateBtn != null)
            studentGenerateBtn.onClick.AddListener(() => studentPasswordField.text = GenerateRandomPassword());

        if (teacherGenerateBtn != null)
            teacherGenerateBtn.onClick.AddListener(() => teacherPasswordField.text = GenerateRandomPassword());

        if (studentSaveBtn != null)
            studentSaveBtn.onClick.AddListener(AddStudent);

        if (teacherSaveBtn != null)
            teacherSaveBtn.onClick.AddListener(AddTeacher);
        

        if (openSearchBtn != null)
    openSearchBtn.onClick.AddListener(() => SearchPanel.SetActive(true));

        if (searchBtn != null)
    searchBtn.onClick.AddListener(SearchStudent);
        ListenToStudents();
        

        ListenToTeachers();

        if (editSaveBtn != null)
            editSaveBtn.onClick.AddListener(SaveEditUser);

        if (editCancelBtn != null)
            editCancelBtn.onClick.AddListener(() =>
            {
                editUserContainer.SetActive(false);
                if (darkOverlay != null) darkOverlay.SetActive(false);
            });

        if (userFilterDropdown != null)
            userFilterDropdown.onValueChanged.AddListener(OnFilterChanged);

        if (teacherContainer != null) teacherContainer.SetActive(false);
        
        if (refreshBtn != null)
            refreshBtn.onClick.AddListener(RefreshUsers);

        if (closeSearchBtn != null)
    closeSearchBtn.onClick.AddListener(() =>
    {
        ClearSearch();
        SearchPanel.SetActive(false);
    });

searchField.onValueChanged.AddListener(value =>
{
    if (string.IsNullOrEmpty(value))
        ClearSearch();
});

if (archiveContinueBtn != null)
            archiveContinueBtn.onClick.AddListener(ArchiveUser);

        if (archiveCancelBtn != null)
            archiveCancelBtn.onClick.AddListener(() =>
            {
                archiveValidation.SetActive(false);
                if (darkOverlay != null) darkOverlay.SetActive(false);
            });

    }
    
    void OnFilterChanged(int index)
    {
        // 0 = Students, 1 = Teachers
        bool showStudents = index == 0;
        if (studentContainer != null) studentContainer.SetActive(showStudents);
        if (teacherContainer != null) teacherContainer.SetActive(!showStudents);
    }
     
     void RefreshUsers()
    {
        ClearTable(studentTableContent);
        ClearTable(teacherTableContent);
        ListenToStudents();
        ListenToTeachers();
    }


    
    // ─────────────────────────────────────────────
    // REAL-TIME STUDENT LISTENER
    // ─────────────────────────────────────────────

    void ListenToStudents()
    {
        studentListener = db.Collection("users")
            .WhereEqualTo("role", "student")
            .Listen(snapshot =>
            {
                ClearTable(studentTableContent);

                var sorted = snapshot.Documents
                    .Where(doc => doc.TryGetValue("is_verified", out bool v) && v)
                    .OrderBy(doc =>
                    {
                        var d = doc.ToDictionary();
                        string ln = d.ContainsKey("last_name") ? d["last_name"].ToString() : "";
                        string fn = d.ContainsKey("first_name") ? d["first_name"].ToString() : "";
                        return $"{ln},{fn}";
                    }).ToList();

                for (int i = 0; i < sorted.Count; i++)
                    CreateStudentRow(sorted[i].ToDictionary(), sorted[i].Id, i + 1);
            });
    }



    void ListenToTeachers()
    {
        db.Collection("users")
            .WhereEqualTo("role", "teacher")
            .Listen(snapshot =>
            {
                ClearTable(teacherTableContent);

                var sorted = snapshot.Documents
                    .Where(doc => doc.TryGetValue("is_verified", out bool v) && v)
                    .OrderBy(doc =>
                    {
                        var d = doc.ToDictionary();
                        string ln = d.ContainsKey("last_name") ? d["last_name"].ToString() : "";
                        string fn = d.ContainsKey("first_name") ? d["first_name"].ToString() : "";
                        return $"{ln},{fn}";
                    }).ToList();

                for (int i = 0; i < sorted.Count; i++)
                    CreateTeacherRow(sorted[i].ToDictionary(), sorted[i].Id, i + 1);
            });
    }

    void CreateTeacherRow(Dictionary<string, object> data, string docID, int index)
    {
        GameObject row = Instantiate(teacherRowPrefab, teacherTableContent);
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

        Button editBtn = panel.Find("EditBtn")?.GetComponent<Button>();
        if (editBtn != null)
        {
            string capturedID = docID;
            string capturedFirst = firstName;
            string capturedMiddle = middleName;
            string capturedLast = lastName;
            editBtn.onClick.AddListener(() => OpenEditPanel(capturedID, capturedFirst, capturedMiddle, capturedLast));
        }

        Button archiveBtn = panel.Find("ArchiveBtn")?.GetComponent<Button>();
        if (archiveBtn != null)
        {
            string capturedID = docID;
            archiveBtn.onClick.AddListener(() => OpenArchivePanel(capturedID));
        }
    }

    void CreateStudentRow(Dictionary<string, object> data, string docID, int index)
    {
        GameObject row = Instantiate(studentRowPrefab, studentTableContent);
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

        Button editBtn = panel.Find("EditBtn")?.GetComponent<Button>();
        if (editBtn != null)
        {
            string capturedID = docID;
            string capturedFirst = firstName;
            string capturedMiddle = middleName;
            string capturedLast = lastName;
            editBtn.onClick.AddListener(() => OpenEditPanel(capturedID, capturedFirst, capturedMiddle, capturedLast));
        }

        Button archiveBtn = panel.Find("ArchiveBtn")?.GetComponent<Button>();
        if (archiveBtn != null)
        {
            string capturedID = docID;
            archiveBtn.onClick.AddListener(() => OpenArchivePanel(capturedID));
        }
    }

    void OpenEditPanel(string docID, string firstName, string middleName, string lastName)
    {
        editingDocID = docID;
        editFirstNameField.text = firstName;
        editMiddleNameField.text = middleName;
        editLastNameField.text = lastName;

        if (editFirstNameValidation != null) editFirstNameValidation.text = "";
        if (editLastNameValidation != null) editLastNameValidation.text = "";

        if (darkOverlay != null) darkOverlay.SetActive(true);
        editUserContainer.SetActive(true);
    }

    void SaveEditUser()
    {
        string firstName  = editFirstNameField.text.Trim();
        string middleName = editMiddleNameField.text.Trim();
        string lastName   = editLastNameField.text.Trim();

        bool valid = true;
        if (editFirstNameValidation != null) editFirstNameValidation.text = "";
        if (editLastNameValidation != null)  editLastNameValidation.text  = "";

        if (string.IsNullOrEmpty(firstName))
        {
            if (editFirstNameValidation != null) editFirstNameValidation.text = "First name is required.";
            valid = false;
        }
        if (string.IsNullOrEmpty(lastName))
        {
            if (editLastNameValidation != null) editLastNameValidation.text = "Last name is required.";
            valid = false;
        }
        if (!valid) return;

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "first_name", firstName },
            { "middle_name", middleName },
            { "last_name", lastName }
        };

        db.Collection("users").Document(editingDocID).UpdateAsync(updates)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("✅ User updated.");
                    editUserContainer.SetActive(false);
                    if (darkOverlay != null) darkOverlay.SetActive(false);
                    StartCoroutine(ShowSuccessEditPanel());
                }
                else
                {
                    Debug.LogError("❌ Update failed: " + task.Exception);
                }
            });
    }
    

    void OpenArchivePanel(string docID)
    {
        archivingDocID = docID;
        if (darkOverlay != null) darkOverlay.SetActive(true);
        archiveValidation.SetActive(true);
    }

    void ArchiveUser()
    {
        if (string.IsNullOrEmpty(archivingDocID)) return;

        DocumentReference sourceRef = db.Collection("users").Document(archivingDocID);

        sourceRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                Debug.LogError("❌ User not found: " + task.Exception);
                return;
            }

            Dictionary<string, object> data = task.Result.ToDictionary();
            data["archivedAt"] = Timestamp.GetCurrentTimestamp();

            db.Collection("archived_users").Document(archivingDocID).SetAsync(data)
                .ContinueWithOnMainThread(setTask =>
                {
                    if (!setTask.IsCompletedSuccessfully)
                    {
                        Debug.LogError("❌ Failed to archive: " + setTask.Exception);
                        return;
                    }

                    sourceRef.DeleteAsync().ContinueWithOnMainThread(deleteTask =>
                    {
                        if (deleteTask.IsCompletedSuccessfully)
                        {
                            Debug.Log("✅ User archived.");
                            archiveValidation.SetActive(false);
                            if (darkOverlay != null) darkOverlay.SetActive(false);
                            StartCoroutine(ShowSuccessEditPanel());
                        }
                        else
                        {
                            Debug.LogError("❌ Failed to delete from users: " + deleteTask.Exception);
                        }
                    });
                });
        });
    }
    // ─────────────────────────────────────────────
    // ADD STUDENT
    // ─────────────────────────────────────────────

    void AddStudent()
    {
        string email      = studentEmailField.text.Trim();
        string password   = studentPasswordField.text;
        string firstName  = studentFirstNameField.text.Trim();
        string middleName = studentMiddleNameField.text.Trim();
        string lastName   = studentLastNameField.text.Trim();

        if (!ValidateStudentFields(email, password, firstName, lastName)) return;

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("❌ Failed to create student auth: " +
                        task.Exception?.Flatten().InnerException?.Message);
                    return;
                }

                FirebaseUser newUser = task.Result.User;
                string uid = newUser.UserId;

                newUser.SendEmailVerificationAsync().ContinueWithOnMainThread(verifyTask =>
                {
                    if (verifyTask.IsCompletedSuccessfully)
                        Debug.Log("📧 Firebase verification email sent to " + email);
                    else
                        Debug.LogError("❌ Failed to send verification email: " + verifyTask.Exception);
                });

                SendAccountEmail(email, password, "student");

                Dictionary<string, object> userData = new Dictionary<string, object>
                {
                    { "first_name", firstName },
                    { "middle_name", middleName },
                    { "last_name", lastName },
                    { "email", email },
                    { "role", "student" },
                    { "createdAt", Timestamp.GetCurrentTimestamp() }
                };

                db.Collection("users").Document(uid).SetAsync(userData)
                    .ContinueWithOnMainThread(fsTask =>
                    {
                        if (fsTask.IsCompletedSuccessfully)
                        {
                            Debug.Log("✅ Student added to users.");
                            ClearStudentForm();
                            ClearStudentValidations();
                            addStudentContainer.SetActive(false);
                            StartCoroutine(ShowSuccessPanel());
                        }
                        else
                        {
                            Debug.LogError("❌ Firestore error: " + fsTask.Exception);
                        }
                    });
            });
    }

    // ─────────────────────────────────────────────
    // ADD TEACHER
    // ─────────────────────────────────────────────

    void AddTeacher()
    {
        string email      = teacherEmailField.text.Trim();
        string password   = teacherPasswordField.text;
        string firstName  = teacherFirstNameField.text.Trim();
        string middleName = teacherMiddleNameField.text.Trim();
        string lastName   = teacherLastNameField.text.Trim();

        if (!ValidateTeacherFields(email, password, firstName, lastName)) return;

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("❌ Failed to create teacher auth: " +
                        task.Exception?.Flatten().InnerException?.Message);
                    return;
                }

                FirebaseUser newUser = task.Result.User;
                string uid = newUser.UserId;

                newUser.SendEmailVerificationAsync().ContinueWithOnMainThread(verifyTask =>
                {
                    if (verifyTask.IsCompletedSuccessfully)
                        Debug.Log("📧 Firebase verification email sent to " + email);
                    else
                        Debug.LogError("❌ Failed to send verification email: " + verifyTask.Exception);
                });

                SendAccountEmail(email, password, "teacher");

                Dictionary<string, object> teacherData = new Dictionary<string, object>
                {
                    { "first_name", firstName },
                    { "middle_name", middleName },
                    { "last_name", lastName },
                    { "email", email },
                    { "role", "teacher" },
                    { "createdAt", Timestamp.GetCurrentTimestamp() }
                };

                db.Collection("users").Document(uid).SetAsync(teacherData)
                    .ContinueWithOnMainThread(fsTask =>
                    {
                        if (fsTask.IsCompletedSuccessfully)
                        {
                            Debug.Log("✅ Teacher added to users.");
                            ClearTeacherForm();
                            ClearTeacherValidations();
                            addTeacherContainer.SetActive(false);
                            StartCoroutine(ShowSuccessPanel());
                        }
                        else
                        {
                            Debug.LogError("❌ Firestore error: " + fsTask.Exception);
                        }
                    });
            });
    }

    // ─────────────────────────────────────────────
    // VALIDATION
    // ─────────────────────────────────────────────

    bool ValidateStudentFields(string email, string password, string firstName, string lastName)
    {
        bool valid = true;

        ClearStudentValidations();

        if (string.IsNullOrEmpty(firstName))
        {
            studentFirstNameValidation.text = "First name is required.";
            valid = false;
        }

        if (string.IsNullOrEmpty(lastName))
        {
            studentLastNameValidation.text = "Last name is required.";
            valid = false;
        }

        if (string.IsNullOrEmpty(email))
        {
            studentEmailValidation.text = "Email is required.";
            valid = false;
        }
        else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            studentEmailValidation.text = "Invalid email address.";
            valid = false;
        }

        if (string.IsNullOrEmpty(password))
        {
            studentPasswordValidation.text = "Password is required.";
            valid = false;
        }
        else if (password.Length < 8)
        {
            studentPasswordValidation.text = "Password must be at least 8 characters.";
            valid = false;
        }
        else if (!password.Any(char.IsDigit))
        {
            studentPasswordValidation.text = "Password must contain at least one number.";
            valid = false;
        }

        return valid;
    }

    bool ValidateTeacherFields(string email, string password, string firstName, string lastName)
    {
        bool valid = true;

        ClearTeacherValidations();

        if (string.IsNullOrEmpty(firstName))
        {
            teacherFirstNameValidation.text = "First name is required.";
            valid = false;
        }

        if (string.IsNullOrEmpty(lastName))
        {
            teacherLastNameValidation.text = "Last name is required.";
            valid = false;
        }

        if (string.IsNullOrEmpty(email))
        {
            teacherEmailValidation.text = "Email is required.";
            valid = false;
        }
        else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            teacherEmailValidation.text = "Invalid email address.";
            valid = false;
        }

        if (string.IsNullOrEmpty(password))
        {
            teacherPasswordValidation.text = "Password is required.";
            valid = false;
        }
        else if (password.Length < 8)
        {
            teacherPasswordValidation.text = "Password must be at least 8 characters.";
            valid = false;
        }
        else if (!password.Any(char.IsDigit))
        {
            teacherPasswordValidation.text = "Password must contain at least one number.";
            valid = false;
        }

        return valid;
    }

    void ClearStudentValidations()
    {
        if (studentFirstNameValidation != null)  studentFirstNameValidation.text  = "";
        if (studentLastNameValidation != null)   studentLastNameValidation.text   = "";
        if (studentEmailValidation != null)      studentEmailValidation.text      = "";
        if (studentPasswordValidation != null)   studentPasswordValidation.text   = "";
    }

    void ClearTeacherValidations()
    {
        if (teacherFirstNameValidation != null)  teacherFirstNameValidation.text  = "";
        if (teacherLastNameValidation != null)   teacherLastNameValidation.text   = "";
        if (teacherEmailValidation != null)      teacherEmailValidation.text      = "";
        if (teacherPasswordValidation != null)   teacherPasswordValidation.text   = "";
    }

    // ─────────────────────────────────────────────
    // SMTP EMAIL
    // ─────────────────────────────────────────────

    void SendAccountEmail(string recipientEmail, string password, string role)
    {
        try
        {
            MailMessage mail = new MailMessage();
            SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");

            mail.From = new MailAddress("dazzledev23@gmail.com", "Tanikala at Laya Team");
            mail.To.Add(recipientEmail);
            mail.Subject = $"Welcome to Tanikala at Laya!";
            mail.IsBodyHtml = true;

            mail.Body = $@"
<html>
<body style='font-family:Segoe UI, sans-serif; background-color:#1a1a2e; color:#e0e0e0; padding:20px;'>
    <h2 style='color:#4ecca3;'>You're Invited to Tanikala at Laya! 🎉</h2>
    <p>Hello <b>{role}</b>,</p>
    <p>You've been invited to join <b>Tanikala at Laya</b>! We're excited to have you on board.
    Here are your login credentials to get started:</p>
    <div style='background-color:#16213e; padding:15px; border-radius:8px; margin-top:10px;'>
        <p><b>Email:</b> <span style='color:#4ecca3;'>{recipientEmail}</span></p>
        <p><b>Password:</b> <span style='color:#f5a623;'>{password}</span></p>
    </div>
    <p style='margin-top:20px;'>
        ✅ Please click the <b>verification link</b> sent in a separate email to verify your account.
    </p>
    <p>Once verified, head over to the app and log in — your journey starts now!</p>
    <p style='margin-top:30px;'>See you inside,<br><b>— The Tanikala at Laya Team</b></p>
</body>
</html>";

            smtpServer.Port = 587;
            smtpServer.Credentials = new NetworkCredential("dazzledev23@gmail.com", "fpnw uent irxd wote");
            smtpServer.EnableSsl = true;

            ServicePointManager.ServerCertificateValidationCallback =
                delegate (object s, X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                { return true; };

            smtpServer.Send(mail);
            Debug.Log($"📧 Welcome email sent to {recipientEmail}");
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Failed to send email: " + ex.Message);
        }
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    string GenerateRandomPassword(int length = 8)
    {
        const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits  = "0123456789";
        const string symbols = "!@#$%^&*";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        System.Random rand = new System.Random();

        sb.Append(digits[rand.Next(digits.Length)]);
        string pool = letters + digits + symbols;
        for (int i = 1; i < length; i++)
            sb.Append(pool[rand.Next(pool.Length)]);

        return new string(sb.ToString().OrderBy(c => rand.Next()).ToArray());
    }

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

    void ClearStudentForm()
    {
        studentEmailField.text      = "";
        studentPasswordField.text   = "";
        studentFirstNameField.text  = "";
        studentMiddleNameField.text = "";
        studentLastNameField.text   = "";
    }

    void ClearTeacherForm()
    {
        teacherEmailField.text      = "";
        teacherPasswordField.text   = "";
        teacherFirstNameField.text  = "";
        teacherMiddleNameField.text = "";
        teacherLastNameField.text   = "";
    }

void SearchStudent()
{
    string query = searchField.text.Trim().ToLower();
    if (string.IsNullOrEmpty(query)) return;

    bool isStudentView = userFilterDropdown == null || userFilterDropdown.value == 0;
    Transform activeContent = isStudentView ? studentTableContent : teacherTableContent;

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
    Transform activeContent = isStudentView ? studentTableContent : teacherTableContent;
    foreach (Transform row in activeContent)
        row.gameObject.SetActive(true);
}

IEnumerator ShowSuccessPanel()
{
    if (darkOverlay != null) darkOverlay.SetActive(false);
    SuccessPanel.SetActive(true);
    yield return new WaitForSeconds(3f);
    SuccessPanel.SetActive(false);
}
    void OnDestroy()
    {
        if (studentListener != null)
            studentListener.Stop();
    }

    IEnumerator ShowSuccessEditPanel()
{
    if (darkOverlay != null) darkOverlay.SetActive(false);
    SuccessEditPanel.SetActive(true);
    yield return new WaitForSeconds(3f);
    SuccessEditPanel.SetActive(false);
}
}
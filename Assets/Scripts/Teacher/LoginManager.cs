using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField EmailField;
    public TMP_InputField PasswordField;
    public Button LoginBtn;
    public TextMeshProUGUI StatusText;
    public Button SeenBtn;
    public Button UnseenBtn;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;

    public TextMeshProUGUI LockText;

    private int failedAttempts = 0;
    private bool isLocked = false;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                firestore = FirebaseFirestore.DefaultInstance;
                Debug.Log("✅ Firebase initialized.");
            }
            else
            {
                SetStatus("Firebase setup failed.", Color.red);
                Debug.LogError("❌ Firebase setup failed.");
            }
        });

        LoginBtn.onClick.AddListener(OnLoginPressed);
        SeenBtn.onClick.AddListener(ShowPassword);
        UnseenBtn.onClick.AddListener(HidePassword);

        if (LockText != null)
            LockText.gameObject.SetActive(false);
    }

    void OnLoginPressed()
    {
        if (isLocked) return;

        string email    = EmailField.text.Trim();
        string password = PasswordField.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            SetStatus("Please fill in both fields.", Color.red);
            return;
        }

        SetStatus("Logging in...", Color.blue);

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    failedAttempts++;

                    if (failedAttempts >= 3)
                    {
                        StartCoroutine(LockLogin());
                    }
                    else
                    {
                        SetStatus("Wrong email or password.", Color.red);
                    }

                    Debug.LogError("❌ Login failed: " + task.Exception?.Flatten().InnerException?.Message);
                    return;
                }

                FirebaseUser user = task.Result.User;

                if (!user.IsEmailVerified)
                {
                    auth.SignOut();
                    SetStatus("Please verify your email first.", Color.red);
                    return;
                }

                firestore.Collection("users").Document(user.UserId).UpdateAsync("is_verified", true);
                firestore.Collection("users").Document(user.UserId)
                    .GetSnapshotAsync().ContinueWithOnMainThread(userTask =>
                    {
                        if (!userTask.IsCompleted || !userTask.Result.Exists)
                        {
                            SetStatus("User data not found.", Color.red);
                            return;
                        }

                        string role = userTask.Result.ContainsField("role")
                            ? userTask.Result.GetValue<string>("role") : "";

                        if (role == "teacher")
                        {
                            SetStatus("Login successful!", Color.green);
                            SceneManager.LoadScene("LoadingToTeacher");
                        }
                        else if (role == "student")
                        {
                            GlobalUserData.UserId = user.UserId;
                            GlobalUserData.IsGuest = false;
                            SetStatus("Login successful!", Color.green);
                            SceneManager.LoadScene("LoadingToMainMenu");
                        }
                        else
                        {
                            auth.SignOut();
                            SetStatus("Unauthorized role.", Color.red);
                        }
                    });
            });
    }

    void SetStatus(string message, Color color)
    {
        if (StatusText == null) return;
        StatusText.text  = message;
        StatusText.color = color;
    }

    void ShowPassword()
    {
        PasswordField.contentType = TMP_InputField.ContentType.Standard;
        PasswordField.ForceLabelUpdate();
    }

    void HidePassword()
    {
        PasswordField.contentType = TMP_InputField.ContentType.Password;
        PasswordField.ForceLabelUpdate();
    }

    System.Collections.IEnumerator LockLogin()
    {
        isLocked = true;
        LoginBtn.interactable = false;

        if (StatusText != null)
            StatusText.gameObject.SetActive(false);

        if (LockText != null)
        {
            LockText.gameObject.SetActive(true);
            LockText.color = Color.red;
        }

        int remainingTime = 30;

        if (LockText != null)
            LockText.text = "Too many failed attempts.\nTry again in " + remainingTime + " seconds.";

        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(1f);
            remainingTime--;

            if (LockText != null)
                LockText.text = "Too many failed attempts.\nTry again in " + remainingTime + " seconds.";
        }

        failedAttempts = 0;
        isLocked = false;
        LoginBtn.interactable = true;

        if (LockText != null)
            LockText.gameObject.SetActive(false);

        if (StatusText != null)
            StatusText.gameObject.SetActive(true);

        SetStatus("You can try logging in again.", Color.white);
    }
}
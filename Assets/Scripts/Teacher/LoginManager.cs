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
    public GameObject LoggingInPanel;
    public GameObject LoginBox;

    [Header("Unverified Panel")]
    public GameObject unverifiedPanel;
    public Button resendEmailBtn;
    public Button closeUnverifiedBtn;

    private FirebaseUser pendingUnverifiedUser;

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
                CheckAutoLogin();
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

        if (unverifiedPanel != null) unverifiedPanel.SetActive(false);

        if (resendEmailBtn != null)
            resendEmailBtn.onClick.AddListener(() =>
            {
                if (pendingUnverifiedUser != null)
                    pendingUnverifiedUser.SendEmailVerificationAsync();
                CloseUnverifiedPanel();
            });

        if (closeUnverifiedBtn != null)
            closeUnverifiedBtn.onClick.AddListener(CloseUnverifiedPanel);

        long lockEndUnix = long.Parse(PlayerPrefs.GetString("LoginLockEndUnix", "0"));
        int secondsLeft = (int)(lockEndUnix - System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        if (secondsLeft > 0)
            StartCoroutine(LockLogin(secondsLeft));
    }

    void CheckAutoLogin()
    {
        FirebaseUser user = auth.CurrentUser;
        if (user == null || !user.IsEmailVerified) return;

        ShowLoggingIn(true);

        firestore.Collection("users").Document(user.UserId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted || !task.Result.Exists)
                {
                    auth.SignOut();
                    ShowLoggingIn(false);
                    return;
                }

                string role = task.Result.ContainsField("role")
                    ? task.Result.GetValue<string>("role") : "";

                firestore.Collection("users").Document(user.UserId).UpdateAsync("is_verified", true);

                if (role == "teacher")
                {
                    SceneManager.LoadScene("LoadingToTeacher");
                }
                else if (role == "admin")
                {
                    SceneManager.LoadScene("LoadingToAdmin");
                }
                else if (role == "student")
                {
                    GlobalUserData.UserId = user.UserId;
                    GlobalUserData.IsGuest = false;
                    SceneManager.LoadScene("LoadingToMainMenu");
                }
                else
                {
                    auth.SignOut();
                    ShowLoggingIn(false);
                }
            });
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

        ShowLoggingIn(true);

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    failedAttempts++;
                    ShowLoggingIn(false);

                    if (failedAttempts >= 3)
                    {
                        StartCoroutine(LockLogin(30));
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
                    ShowLoggingIn(false);
                    pendingUnverifiedUser = user;

                    firestore.Collection("users").Document(user.UserId).GetSnapshotAsync()
                        .ContinueWithOnMainThread(checkTask =>
                        {
                            bool existsInDB = checkTask.IsCompletedSuccessfully && checkTask.Result.Exists;
                            ShowUnverifiedPanel(existsInDB);
                        });
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
                        else if (role == "admin")
                        {
                            SetStatus("Login successful!", Color.green);
                            SceneManager.LoadScene("LoadingToAdmin");
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
                            ShowLoggingIn(false);
                            SetStatus("Unauthorized role.", Color.red);
                        }
                    });
            });
    }

    void ShowUnverifiedPanel(bool showResend)
    {
        if (LoginBox != null)        LoginBox.SetActive(false);
        if (unverifiedPanel != null) unverifiedPanel.SetActive(true);
        if (resendEmailBtn != null)  resendEmailBtn.gameObject.SetActive(showResend);
    }

    void CloseUnverifiedPanel()
    {
        auth.SignOut();
        pendingUnverifiedUser = null;
        if (unverifiedPanel != null) unverifiedPanel.SetActive(false);
        if (LoginBox != null)        LoginBox.SetActive(true);
    }

    void ShowLoggingIn(bool show)
    {
        if (LoggingInPanel != null) LoggingInPanel.SetActive(show);
        LoginBtn.interactable = !show;
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

    System.Collections.IEnumerator LockLogin(int seconds)
    {
        long lockEndUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + seconds;
        PlayerPrefs.SetString("LoginLockEndUnix", lockEndUnix.ToString());
        PlayerPrefs.Save();

        isLocked = true;
        LoginBtn.interactable = false;

        if (StatusText != null)
            StatusText.gameObject.SetActive(false);

        if (LockText != null)
        {
            LockText.gameObject.SetActive(true);
            LockText.color = Color.red;
        }

        int remainingTime = seconds;

        if (LockText != null)
            LockText.text = "Too many failed attempts.\nTry again in " + remainingTime + " seconds.";

        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(1f);
            remainingTime--;

            if (LockText != null)
                LockText.text = "Too many failed attempts.\nTry again in " + remainingTime + " seconds.";
        }

        PlayerPrefs.SetString("LoginLockEndUnix", "0");
        PlayerPrefs.Save();

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
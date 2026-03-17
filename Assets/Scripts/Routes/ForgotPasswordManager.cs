using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Extensions;
using System.Text.RegularExpressions;

public class ForgotPasswordManager : MonoBehaviour
{
    public TMP_InputField emailInputField;
    public Button sendButton;
    public Button backButton;
    public GameObject loginPanel;
    public GameObject forgotPasswordPanel;
    public TextMeshProUGUI forgotStatusText;

    private FirebaseAuth auth;

    private int sendAttempts = 0;
    private int maxAttempts = 3;
    private float cooldownDuration = 300f;
    private float cooldownEndTime;
    private bool isOnCooldown = false;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        cooldownEndTime = PlayerPrefs.GetFloat("ForgotPwdCooldownEndTime", 0f);

        if (Time.time < cooldownEndTime)
        {
            float remaining = cooldownEndTime - Time.time;
            isOnCooldown = true;
            sendAttempts = maxAttempts;
            sendButton.interactable = false;
            forgotStatusText.text = $"Too many attempts. Try again in {(int)remaining} seconds.";
            InvokeRepeating(nameof(UpdateCooldownStatus), 1f, 1f);
        }

        sendButton.onClick.AddListener(SendPasswordResetEmail);
        backButton.onClick.AddListener(GoBackToLogin);
        emailInputField.onValueChanged.AddListener(OnEmailFieldChanged);

        forgotStatusText.text = "Type your email.";
        ValidateInput();
    }

    void SendPasswordResetEmail()
    {
        string email = emailInputField.text.Trim();

        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        {
            forgotStatusText.text = "Please enter a valid email.";
            forgotStatusText.color = Color.red;
            return;
        }

        if (isOnCooldown)
        {
            forgotStatusText.text = $"Try again in {(int)(cooldownEndTime - Time.time)} seconds.";
            return;
        }

        sendAttempts++;

        if (sendAttempts >= maxAttempts)
        {
            StartCooldown();
            return;
        }

        forgotStatusText.text = "Sending...";

        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogWarning("Failed to send reset email: " + task.Exception);
                forgotStatusText.text = "Failed to send reset email. Check the email.";
                forgotStatusText.color = Color.red;
            }
            else
            {
                Debug.Log("Password reset email sent successfully.");
                forgotStatusText.text = "Reset email sent! Check your inbox.";
            }
        });
    }

    void StartCooldown()
    {
        isOnCooldown = true;
        cooldownEndTime = Time.time + cooldownDuration;

        PlayerPrefs.SetFloat("ForgotPwdCooldownEndTime", cooldownEndTime);
        PlayerPrefs.Save();

        sendButton.interactable = false;
        forgotStatusText.text = "Too many attempts. Try again in 5 minutes.";
        InvokeRepeating(nameof(UpdateCooldownStatus), 1f, 1f);
    }

    void UpdateCooldownStatus()
    {
        float remaining = cooldownEndTime - Time.time;

        if (remaining <= 0)
        {
            isOnCooldown = false;
            sendAttempts = 0;
            sendButton.interactable = IsValidEmail(emailInputField.text.Trim());
            forgotStatusText.text = "You may now try again.";

            PlayerPrefs.SetFloat("ForgotPwdCooldownEndTime", 0f);
            PlayerPrefs.Save();

            CancelInvoke(nameof(UpdateCooldownStatus));
        }
        else
        {
            forgotStatusText.text = $"Try again in {(int)remaining} seconds.";
        }
    }

    void GoBackToLogin()
    {
        forgotPasswordPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    void OnEmailFieldChanged(string input)
    {
        ValidateInput();
    }

    void ValidateInput()
    {
        string email = emailInputField.text.Trim();
        bool isValid = IsValidEmail(email);

        sendButton.interactable = isValid && !isOnCooldown;

        if (isValid)
            forgotStatusText.text = "Ready to send reset email.";
        else
            forgotStatusText.text = "Type your email.";
    }

    bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
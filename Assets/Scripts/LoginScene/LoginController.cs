using System;
using System.Threading.Tasks;
using Network;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI loginButtonText;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button loginOrRegisterButton;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private GameObject feedBackWindow;
    [Header("Settings")]
    [SerializeField] private string allowedSpecialCharacters;
    [SerializeField] private string nextSceneName = "ShipEditor";


    private State currentState = State.LOGIN;

    private void Start()
    {
        ChangeState(State.LOGIN);
        loginButton.onClick.AddListener(() => ChangeState(State.LOGIN));
        registerButton.onClick.AddListener(() => ChangeState(State.REGISTER));

        loginOrRegisterButton.onClick.AddListener(LoginOrRegister);
    }

    public void ChangeState(State newState)
    {
        currentState = newState;
        loginButtonText.text = newState == State.LOGIN ? "Login":"Register";//Пока так.

        loginButton.interactable = newState != State.LOGIN;
        registerButton.interactable = newState != State.REGISTER;
    }

    public void LoginOrRegister()
    {
        _ = LoginOrRegisterAsync();
    }

    public async Task LoginOrRegisterAsync()
    {
        ValidationResult result = ValidateCredentials(usernameInput.text, passwordInput.text);
        if (result != ValidationResult.SUCCESS)
        {
            ShowValidationFeedback(result);
            return;
        }
        SwitchButtons(false);
        bool success = false;

        try
        {
            AuthRequest requestData = new AuthRequest
            {
                username = usernameInput.text,
                password = passwordInput.text
            };

            AuthResponse response = null;

            if (currentState == State.LOGIN)
            {
                Debug.Log("Sending Login Request...");
                response = await APINetworkManager.Instance.PostRequestAsync<AuthResponse>("/auth/login", requestData);
            }
            else
            {
                Debug.Log("Sending Register Request...");
                response = await APINetworkManager.Instance.PostRequestAsync<AuthResponse>("/auth/register", requestData);
            }

            if (response != null)
            {
                Debug.Log($"Success! Welcome {response.username}, ID: {response.player_id}");

                PlayerPrefs.SetString("jwt_token", response.token);
                PlayerPrefs.SetInt("player_id", response.player_id);
                PlayerPrefs.SetString("username", response.username);
                PlayerPrefs.Save();

                success = true;
                LoadNextScene();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Auth Error: {ex.Message}");

            ValidationResult serverResult = ParseServerError(ex.Message);

            if (serverResult != ValidationResult.SUCCESS)
            {
                ShowValidationFeedback(serverResult);
            }
            else
            {
                ShowFeedbackMessage($"Server Error: {ex.Message}");
            }
        }
        finally
        {
            SwitchButtons(true);
        }

    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    private void SwitchButtons(bool state)
    {
        loginButton.interactable = state;
        registerButton.interactable = state;
        loginOrRegisterButton.interactable = state;
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && loginButton.interactable)
        {
            _ = LoginOrRegisterAsync();
        }
    }

    public ValidationResult ValidateCredentials(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 1)
        {
            return ValidationResult.USERNAME_INVALID;
        }
        if (string.IsNullOrWhiteSpace(password) || password.Length < 5 || password.Length > 40 || !ValidatePasswordSpecialCharacters(password))
        {
            return ValidationResult.PASSWORD_INVALID;
        }
        return ValidationResult.SUCCESS;
    }

    private bool ValidatePasswordSpecialCharacters(string password)
    {
        foreach (char c in password)
        {
            if (!char.IsLetterOrDigit(c) && !allowedSpecialCharacters.Contains(c.ToString()))
            {
                return false;
            }
        }
        return true;
    }

    private void ShowValidationFeedback(ValidationResult result)
    {
        switch (result)
        {
            case ValidationResult.USERNAME_INVALID:
                ShowFeedbackMessage("Invalid username. Please enter a valid username.");
                break;
            case ValidationResult.PASSWORD_INVALID:
                ShowFeedbackMessage("Invalid password. Password must be 5-40 characters long and can include special characters: " + allowedSpecialCharacters);
                break;
            case ValidationResult.USER_ALREADY_EXISTS:
                ShowFeedbackMessage("User already exists. Please choose a different username.");
                break;
            case ValidationResult.USER_NOT_FOUND:
                ShowFeedbackMessage("User not found. Please check your username.");
                break;
            case ValidationResult.WRONG_PASSWORD:
                ShowFeedbackMessage("Wrong password. Please try again.");
                break;
            default:
                break;
        }
    }

    private ValidationResult ParseServerError(string serverError)
    {
        string error = serverError.ToLower();

        if (error.Contains("incorrect login/password"))
        {
            return ValidationResult.WRONG_PASSWORD;
        }

        if (error.Contains("cannot use this login") || error.Contains("already exists"))
        {
            return ValidationResult.USER_ALREADY_EXISTS;
        }

        if (error.Contains("user not found"))
        {
            return ValidationResult.USER_NOT_FOUND;
        }

        return ValidationResult.SUCCESS;
    }
    private void ShowFeedbackMessage(string message)
    {
        feedbackText.text = message;
        feedBackWindow.SetActive(true);
    }
    public void CloseFeedbackMessage()
    {
        feedBackWindow.SetActive(false);
    }







    [Serializable]
    public enum State
    {
        LOGIN,
        REGISTER
    }
    [Serializable]
    public enum ValidationResult
    {
        SUCCESS,
        USERNAME_INVALID,
        PASSWORD_INVALID,
        USER_ALREADY_EXISTS,
        USER_NOT_FOUND,
        WRONG_PASSWORD,
        UNKNOWN_ERROR
    }
}

using System;
using System.Threading.Tasks;
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
    [Header("Feedback Elements")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private GameObject feedBackWindow;
    [SerializeField] private GameObject loadingIndicator;
    [Header("UI Visuals")]
    [SerializeField] private Image usernameInputBackground;
    [SerializeField] private Image passwordInputBackground;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color errorColor = new Color(1f, 0.5f, 0.5f);
    [Header("UI Elements - Menu")]
    [SerializeField] private Button exitGameButton;
    [Header("Settings")]
    [SerializeField] private string allowedSpecialCharacters;
    [SerializeField] private string nextSceneName = "ShipEditor";


    private State currentState = State.LOGIN;
    private LoginConfigData currentConfig;

    private void Start()
    {
        currentConfig = ConfigManager.LoadConfig();
        loginButton.onClick.AddListener(() => ChangeState(State.LOGIN));
        registerButton.onClick.AddListener(() => ChangeState(State.REGISTER));
        loginOrRegisterButton.onClick.AddListener(LoginOrRegister);
        exitGameButton.onClick.AddListener(ExitGame);
        loadingIndicator.SetActive(false);
        ChangeState(State.LOGIN);
        _ = CheckAutoLoginAsync();
    }
    private async Task CheckAutoLoginAsync()
    {
        if (currentConfig == null || string.IsNullOrEmpty(currentConfig.jwt_token))
        {
            return;
        }

        Debug.Log("Auto-login: Token found in config. Setting it and verifying...");
        SetLoadingState(true);

        APINetworkManager.SetToken(currentConfig.jwt_token);

        try
        {
            VerifyResponse response = await APINetworkManager.Instance.GetRequestAsync<VerifyResponse>("/auth/verify");

            if (response != null && response.valid)
            {
                Debug.Log($"Token verified! Welcome back, {response.username}");

                if (currentConfig.username != response.username)
                {
                    currentConfig.username = response.username;
                    currentConfig.player_id = response.player_id;
                    ConfigManager.SaveConfig(currentConfig);
                }

                LoadNextScene();
            }
            else
            {
                throw new Exception("Server returned invalid token status.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Auto-login failed: {ex.Message}");

            APINetworkManager.SetToken(null);
            ConfigManager.ClearCredentials();

            if (currentConfig != null)
            {
                currentConfig.jwt_token = "";
                currentConfig.username = "";
                currentConfig.player_id = -1;
            }
        }
        finally
        {
            SetLoadingState(false);
        }
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
        ResetInputColors();
        ValidationResult result = ValidateCredentials(usernameInput.text, passwordInput.text);
        if (result != ValidationResult.SUCCESS)
        {
            ShowValidationFeedback(result);
            return;
        }
        SetLoadingState(true);

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
                Debug.Log($"Success! Welcome {response.username}, ID: {response.playerId}");

                APINetworkManager.SetToken(response.token);

                if (currentConfig == null) currentConfig = new LoginConfigData();

                currentConfig.username = response.username;
                currentConfig.jwt_token = response.token;
                currentConfig.player_id = response.playerId;

                ConfigManager.SaveConfig(currentConfig);

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
            SetLoadingState(false);
        }

    }

    private void SetLoadingState(bool isLoading)
    {
        loadingIndicator.SetActive(isLoading);
        loginOrRegisterButton.interactable = !isLoading;
        loginButton.interactable = !isLoading && currentState != State.LOGIN;
        registerButton.interactable = !isLoading && currentState != State.REGISTER;
        usernameInput.interactable = !isLoading;
        passwordInput.interactable = !isLoading;
    }
    private void ExitGame()
    {
        Debug.Log("Exit Game");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    private void ResetInputColors()
    {
        SetInputColor(usernameInputBackground, normalColor);
        SetInputColor(passwordInputBackground, normalColor);
    }

    private void SetInputColor(Image targetImage, Color color)
    {
        if (targetImage != null) targetImage.color = color;
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
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
        if (string.IsNullOrWhiteSpace(username) || username.Length < 1 || username.Length > 40)
        {
            SetInputColor(usernameInputBackground, errorColor);
            return ValidationResult.USERNAME_INVALID;
        }
        if (string.IsNullOrWhiteSpace(password) || password.Length < 5 || password.Length > 40 || !ValidatePasswordSpecialCharacters(password))
        {
            SetInputColor(passwordInputBackground, errorColor);
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
                ShowFeedbackMessage("incorrect login/password. Please try again.");
                break;
            case ValidationResult.WRONG_PASSWORD:
                ShowFeedbackMessage("incorrect login/password. Please try again.");
                break;
            case ValidationResult.UNKNOWN_ERROR:
                ShowFeedbackMessage("An unknown error occurred. Please try again later.");
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

[Serializable]
public class VerifyResponse
{
    public bool valid;
    public int player_id;
    public string username;
}

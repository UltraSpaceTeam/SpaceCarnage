using NUnit.Framework;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.TestTools;
using System.Reflection;
using System.Threading.Tasks;

public class LoginControllerTests
{
    private GameObject testGameObject;
    private LoginController loginController;
    
    // UI Elements
    private TextMeshProUGUI loginButtonText;
    private Button loginButton;
    private Button registerButton;
    private Button loginOrRegisterButton;
    private TMP_InputField usernameInput;
    private TMP_InputField passwordInput;
    private TextMeshProUGUI feedbackText;
    private GameObject feedBackWindow;
    private GameObject loadingIndicator;
    private Image usernameInputBackground;
    private Image passwordInputBackground;
    private Button exitGameButton;

    [SetUp]
    public void Setup()
    {
        // Создаем основной GameObject
        testGameObject = new GameObject("LoginController");
        loginController = testGameObject.AddComponent<LoginController>();
        
        // Создаем и настраиваем все UI элементы
        SetupUIElements();
        
        // Устанавливаем приватные поля через рефлексию
        SetPrivateFields();
        
        // Настраиваем ConfigManager mock
        SetupConfigManagerMock();
        
        // Настраиваем APINetworkManager mock
        SetupAPIMock();
    }

    private void SetupUIElements()
    {
        // LoginButtonText
        var loginButtonTextGO = new GameObject("LoginButtonText");
        loginButtonTextGO.transform.SetParent(testGameObject.transform);
        loginButtonText = loginButtonTextGO.AddComponent<TextMeshProUGUI>();
        
        // Buttons
        loginButton = CreateButton("LoginButton");
        registerButton = CreateButton("RegisterButton");
        loginOrRegisterButton = CreateButton("LoginOrRegisterButton");
        exitGameButton = CreateButton("ExitGameButton");
        
        // Input Fields
        usernameInput = CreateInputField("UsernameInput");
        passwordInput = CreateInputField("PasswordInput");
        
        // Input Backgrounds
        usernameInputBackground = CreateImage("UsernameBackground");
        passwordInputBackground = CreateImage("PasswordBackground");
        
        // Feedback Elements
        feedbackText = CreateTextMeshPro("FeedbackText");
        feedBackWindow = new GameObject("FeedbackWindow");
        feedBackWindow.transform.SetParent(testGameObject.transform);
        loadingIndicator = new GameObject("LoadingIndicator");
        loadingIndicator.transform.SetParent(testGameObject.transform);
    }

    private Button CreateButton(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(testGameObject.transform);
        return go.AddComponent<Button>();
    }

    private TMP_InputField CreateInputField(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(testGameObject.transform);
        var inputField = go.AddComponent<TMP_InputField>();
        //var textMesh = go.AddComponent<TextMeshProUGUI>();
        //inputField.textComponent = textMesh;
        var image = go.AddComponent<Image>();
        inputField.image = image;
        return inputField;
    }

    private TextMeshProUGUI CreateTextMeshPro(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(testGameObject.transform);
        return go.AddComponent<TextMeshProUGUI>();
    }

    private Image CreateImage(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(testGameObject.transform);
        return go.AddComponent<Image>();
    }

    private void SetPrivateFields()
    {
        SetPrivateField("loginButtonText", loginButtonText);
        SetPrivateField("loginButton", loginButton);
        SetPrivateField("registerButton", registerButton);
        SetPrivateField("loginOrRegisterButton", loginOrRegisterButton);
        SetPrivateField("usernameInput", usernameInput);
        SetPrivateField("passwordInput", passwordInput);
        SetPrivateField("feedbackText", feedbackText);
        SetPrivateField("feedBackWindow", feedBackWindow);
        SetPrivateField("loadingIndicator", loadingIndicator);
        SetPrivateField("usernameInputBackground", usernameInputBackground);
        SetPrivateField("passwordInputBackground", passwordInputBackground);
        SetPrivateField("exitGameButton", exitGameButton);
        SetPrivateField("normalColor", Color.white);
        SetPrivateField("errorColor", new Color(1f, 0.5f, 0.5f));
    }

    private void SetupConfigManagerMock()
    {
        // Создаем mock для ConfigManager
        // В реальных тестах нужно использовать mocking framework
    }

    private void SetupAPIMock()
    {
        // Создаем mock для APINetworkManager
        // В реальных тестах нужно использовать mocking framework
    }

    private void SetPrivateField(string fieldName, object value)
    {
        var field = typeof(LoginController).GetField(fieldName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(loginController, value);
    }

    private object GetPrivateField(string fieldName)
    {
        var field = typeof(LoginController).GetField(fieldName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(loginController);
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(testGameObject);
    }

    // ==================== ТЕСТЫ ====================

    #region State Tests

    [Test]
    public void ChangeState_ToLogin_UpdatesUI()
    {
        // Arrange
        loginController.ChangeState(LoginController.State.REGISTER); // Сначала переключаем на регистрацию
        
        // Act
        loginController.ChangeState(LoginController.State.LOGIN);
        
        // Assert
        Assert.AreEqual(LoginController.State.LOGIN, GetPrivateField("currentState"));
        Assert.AreEqual("Login", loginButtonText.text);
        Assert.IsFalse(loginButton.interactable);
        Assert.IsTrue(registerButton.interactable);
    }

    [Test]
    public void ChangeState_ToRegister_UpdatesUI()
    {
        // Arrange
        loginController.ChangeState(LoginController.State.LOGIN); // Сначала переключаем на логин
        
        // Act
        loginController.ChangeState(LoginController.State.REGISTER);
        
        // Assert
        Assert.AreEqual(LoginController.State.REGISTER, GetPrivateField("currentState"));
        Assert.AreEqual("Register", loginButtonText.text);
        Assert.IsTrue(loginButton.interactable);
        Assert.IsFalse(registerButton.interactable);
    }

    #endregion

    #region Feedback Tests

    [Test]
    public void ShowFeedbackMessage_SetsTextAndShowsWindow()
    {
        // Arrange
        string testMessage = "Test Error Message";
        feedBackWindow.SetActive(false);
        
        // Используем рефлексию для вызова приватного метода
        var method = typeof(LoginController).GetMethod("ShowFeedbackMessage", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        method?.Invoke(loginController, new object[] { testMessage });
        
        // Assert
        Assert.AreEqual(testMessage, feedbackText.text);
        Assert.IsTrue(feedBackWindow.activeSelf);
    }

    [Test]
    public void CloseFeedbackMessage_HidesWindow()
    {
        // Arrange
        feedBackWindow.SetActive(true);
        
        // Act
        loginController.CloseFeedbackMessage();
        
        // Assert
        Assert.IsFalse(feedBackWindow.activeSelf);
    }

    [Test]
    public void ShowValidationFeedback_UsernameInvalid_ShowsCorrectMessage()
    {
        // Arrange
        var method = typeof(LoginController).GetMethod("ShowValidationFeedback", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        method?.Invoke(loginController, new object[] { LoginController.ValidationResult.USERNAME_INVALID });
        
        // Assert
        StringAssert.Contains("Invalid username", feedbackText.text);
        Assert.IsTrue(feedBackWindow.activeSelf);
    }

    [Test]
    public void ShowValidationFeedback_PasswordInvalid_ShowsCorrectMessage()
    {
        // Arrange
        var method = typeof(LoginController).GetMethod("ShowValidationFeedback", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        method?.Invoke(loginController, new object[] { LoginController.ValidationResult.PASSWORD_INVALID });
        
        // Assert
        StringAssert.Contains("Invalid password", feedbackText.text);
        Assert.IsTrue(feedBackWindow.activeSelf);
    }
	
    [Test]
    public void ShowValidationFeedback_UserAllreadyExists_ShowsCorrectMessage()
    {
        // Arrange
        var method = typeof(LoginController).GetMethod("ShowValidationFeedback", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        method?.Invoke(loginController, new object[] { LoginController.ValidationResult.USER_ALREADY_EXISTS });
        
        // Assert
        StringAssert.Contains("User already exists.", feedbackText.text);
        Assert.IsTrue(feedBackWindow.activeSelf);
    }
	
    [Test]
    public void ShowValidationFeedback_UserNotFound_ShowsCorrectMessage()
    {
        // Arrange
        var method = typeof(LoginController).GetMethod("ShowValidationFeedback", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        method?.Invoke(loginController, new object[] { LoginController.ValidationResult.USER_NOT_FOUND });
        
        // Assert
        StringAssert.Contains("incorrect login/password.", feedbackText.text);
        Assert.IsTrue(feedBackWindow.activeSelf);
    }
	
    [Test]
    public void ShowValidationFeedback_WrongPassword_ShowsCorrectMessage()
    {
        // Arrange
        var method = typeof(LoginController).GetMethod("ShowValidationFeedback", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        method?.Invoke(loginController, new object[] { LoginController.ValidationResult.WRONG_PASSWORD });
        
        // Assert
        StringAssert.Contains("incorrect login/password.", feedbackText.text);
        Assert.IsTrue(feedBackWindow.activeSelf);
    }
	
    [Test]
    public void ShowValidationFeedback_UnknownError_ShowsCorrectMessage()
    {
        // Arrange
        var method = typeof(LoginController).GetMethod("ShowValidationFeedback", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        method?.Invoke(loginController, new object[] { LoginController.ValidationResult.UNKNOWN_ERROR });
        
        // Assert
        StringAssert.Contains("An unknown error", feedbackText.text);
        Assert.IsTrue(feedBackWindow.activeSelf);
    }

    #endregion

    #region ParseServerError Tests

    [Test]
    public void ParseServerError_WrongPassword_ReturnsWrongPassword()
    {
        // Arrange
        string errorMessage = "Error: incorrect login/password";
        var method = typeof(LoginController).GetMethod("ParseServerError", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(loginController, new object[] { errorMessage });
        
        // Assert
        Assert.AreEqual(LoginController.ValidationResult.WRONG_PASSWORD, result);
    }

    [Test]
    public void ParseServerError_UserAlreadyExists_ReturnsUserAlreadyExists()
    {
        // Arrange
        string errorMessage = "User already exists";
        var method = typeof(LoginController).GetMethod("ParseServerError", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(loginController, new object[] { errorMessage });
        
        // Assert
        Assert.AreEqual(LoginController.ValidationResult.USER_ALREADY_EXISTS, result);
    }

    [Test]
    public void ParseServerError_UserNotFound_ReturnsUserNotFound()
    {
        // Arrange
        string errorMessage = "User not found";
        var method = typeof(LoginController).GetMethod("ParseServerError", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(loginController, new object[] { errorMessage });
        
        // Assert
        Assert.AreEqual(LoginController.ValidationResult.USER_NOT_FOUND, result);
    }

    [Test]
    public void ParseServerError_UnknownError_ReturnsSuccess()
    {
        // Arrange
        string errorMessage = "Unknown server error";
        var method = typeof(LoginController).GetMethod("ParseServerError", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(loginController, new object[] { errorMessage });
        
        // Assert
        Assert.AreEqual(LoginController.ValidationResult.SUCCESS, result);
    }

    #endregion

    #region Loading State Tests

    [Test]
    public void SetLoadingState_True_DisablesInteraction()
    {
        // Arrange
        var method = typeof(LoginController).GetMethod("SetLoadingState", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        method?.Invoke(loginController, new object[] { true });
        
        // Assert
        Assert.IsTrue(loadingIndicator.activeSelf);
        Assert.IsFalse(loginOrRegisterButton.interactable);
        Assert.IsFalse(usernameInput.interactable);
        Assert.IsFalse(passwordInput.interactable);
    }

    [Test]
    public void SetLoadingState_False_EnablesInteraction()
    {
        // Arrange
        var method = typeof(LoginController).GetMethod("SetLoadingState", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        method?.Invoke(loginController, new object[] { false });
        
        // Assert
        Assert.IsFalse(loadingIndicator.activeSelf);
        Assert.IsTrue(loginOrRegisterButton.interactable);
        Assert.IsTrue(usernameInput.interactable);
        Assert.IsTrue(passwordInput.interactable);
    }

    #endregion

    #region AutoLogin Tests

    [UnityTest]
    public IEnumerator CheckAutoLoginAsync_NoToken_DoesNothing()
    {
        // Arrange
        // Устанавливаем пустой конфиг
        var configField = typeof(LoginController).GetField("currentConfig", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        configField?.SetValue(loginController, new LoginConfigData { jwt_token = "" });
        
        // Act
        var method = typeof(LoginController).GetMethod("CheckAutoLoginAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task)method?.Invoke(loginController, null);
        
        // Ждем завершения задачи
        while (!task.IsCompleted)
            yield return null;
        
        // Assert
        Assert.IsFalse(loadingIndicator.activeSelf);
    }

    #endregion

    #region Input Tests

    [Test]
    public void ResetInputColors_WhenCalled_ResetsAllColors()
    {
        // Arrange
        usernameInputBackground.color = Color.red;
        passwordInputBackground.color = Color.red;
        var method = typeof(LoginController).GetMethod("ResetInputColors", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        method?.Invoke(loginController, null);
        
        // Assert
        Assert.AreEqual(Color.white, usernameInputBackground.color);
        Assert.AreEqual(Color.white, passwordInputBackground.color);
    }
	
	[Test]
    public void ResetUsernameFieldColor_WhenCalled_ResetsUsernameColor()
    {
        // Arrange
        usernameInputBackground.color = Color.red;
        usernameInput.image.color = Color.red;
        
        // Act
		loginController.ResetUsernameFieldColor();
        
        // Assert
        Assert.AreEqual(Color.white, usernameInputBackground.color);
        Assert.AreEqual(Color.white, usernameInput.image.color);
    }
	
	[Test]
    public void ResetPasswordFieldColor_WhenCalled_ResetsPasswordColor()
    {
        // Arrange
        passwordInputBackground.color = Color.red;
        passwordInput.image.color = Color.red;
        
        // Act
		loginController.ResetPasswordFieldColor();
        
        // Assert
        Assert.AreEqual(Color.white, passwordInputBackground.color);
        Assert.AreEqual(Color.white, passwordInput.image.color);
    }
	
	[Test]
    public void OnUsernameFieldSelected_WhenCalled_ResetsUsernameColor()
    {
        // Arrange
        usernameInput.image.color = Color.red;
        
        // Act
		loginController.OnUsernameFieldSelected();
        
        // Assert
        Assert.AreEqual(Color.white, usernameInput.image.color);
    }
	
	[Test]
    public void OnPasswordFieldSelected_WhenCalled_ResetsPasswordColor()
    {
        // Arrange
        passwordInput.image.color = Color.red;
        
        // Act
		loginController.OnPasswordFieldSelected();
        
        // Assert
        Assert.AreEqual(Color.white, passwordInput.image.color);
    }
    #endregion
}
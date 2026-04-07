using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class LoginInvalidCredentialsSystemTest
{
    private TMP_InputField _usernameInput;
    private TMP_InputField _passwordInput;
    private Button _loginOrRegisterButton;
    private TextMeshProUGUI _feedbackText;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 09] === SETUP ===");

        AggressiveCleanup();
        DisableAutoLogin();

        yield return SceneManager.LoadSceneAsync("LoginScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(1.8f);

        var loginController = Object.FindAnyObjectByType<LoginController>();
        Assert.NotNull(loginController);

        _usernameInput = GetPrivateField<TMP_InputField>(loginController, "usernameInput");
        _passwordInput = GetPrivateField<TMP_InputField>(loginController, "passwordInput");
        _loginOrRegisterButton = GetPrivateField<Button>(loginController, "loginOrRegisterButton");
        _feedbackText = GetPrivateField<TextMeshProUGUI>(loginController, "feedbackText");

        Assert.NotNull(_usernameInput);
        Assert.NotNull(_passwordInput);
        Assert.NotNull(_loginOrRegisterButton);
        Assert.NotNull(_feedbackText);

        Debug.Log("[System Test 09] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_Login_With_Invalid_Credentials_Shows_Errors()
    {
        LogAssert.ignoreFailingMessages = true;

        Debug.Log("[System Test 09] === TEST START ===");

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[API ERROR\].*Incorrect login/password", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"Auth Error.*Incorrect login/password", System.Text.RegularExpressions.RegexOptions.IgnoreCase));

        _usernameInput.text = "antoxic";
        _passwordInput.text = "wrongpassword";
        _loginOrRegisterButton.onClick.Invoke();
        yield return new WaitForSeconds(2.5f);
        Assert.IsTrue(FeedbackContainsError(), "No error shown for wrong password");

        _usernameInput.text = "nonexistentuser12345";
        _passwordInput.text = "any";
        _loginOrRegisterButton.onClick.Invoke();
        yield return new WaitForSeconds(2.5f);
        Assert.IsTrue(FeedbackContainsError(), "No error shown for non-existent user");

        _usernameInput.text = "";
        _passwordInput.text = "";
        _loginOrRegisterButton.onClick.Invoke();
        yield return new WaitForSeconds(2.5f);
        Assert.IsTrue(FeedbackContainsError(), "No error shown for empty fields");

        _usernameInput.text = "antoxic";
        _passwordInput.text = "bebra123";
        _loginOrRegisterButton.onClick.Invoke();
        yield return new WaitForSeconds(3.5f);

        Assert.AreEqual("ShipEditor", SceneManager.GetActiveScene().name,
            "Successful login did not load ShipEditor scene");

        Debug.Log("[System Test 09] === TEST PASSED ===");
    }

    private bool FeedbackContainsError()
    {
        if (_feedbackText == null) return false;
        string text = _feedbackText.text.ToLower().Trim();
        return text.Contains("incorrect") || text.Contains("error") || text.Contains("invalid") ||
               text.Contains("not found") || text.Contains("wrong") || text.Contains("failed");
    }

    private void DisableAutoLogin()
    {
        var config = ConfigManager.LoadConfig();
        if (config != null)
        {
            config.jwt_token = "";
            config.username = "";
            config.player_id = -1;
            ConfigManager.SaveConfig(config);
        }
        APINetworkManager.SetToken(null);
        Debug.Log("[System Test 09] Auto-login disabled");
    }

    private T GetPrivateField<T>(object obj, string fieldName) where T : class
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(obj) as T;
    }

    private void AggressiveCleanup()
    {
        var managers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers)
            if (m != null) Object.DestroyImmediate(m.gameObject);

        ResetSingleton<UIManager>();
        ResetSingleton<GameResources>();
        ResetSingleton<SessionManager>();
    }

    private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}
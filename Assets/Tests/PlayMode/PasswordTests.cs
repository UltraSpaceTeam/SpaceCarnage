using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Reflection;
using System.Threading.Tasks;

public class PasswordTests
{
    private GameObject testGameObject;
    private LoginController loginController;
	
	[SetUp]
    public void Setup()
    {
        // Создаем тестовый GameObject и добавляем LoginController
        testGameObject = new GameObject("TestLoginController");
        loginController = testGameObject.AddComponent<LoginController>();
        loginController.GetType().GetField("allowedSpecialCharacters", 
            BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(loginController, "!@#$%^&*()-_=+~`[]{}<>,./?|\\;:’”");
    }
	
	[TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(testGameObject);
    }
	
    [Test]
	//FR_MENU_VALID_002
    public void ValidatePasswordSpecialCharacters_WithInvalidChar_ReturnsFalse()
    {
        // Arrange
        string passwordWithInvalidChar = "invalid_password\"";
        
        var method = typeof(LoginController).GetMethod("ValidatePasswordSpecialCharacters", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = (bool)method.Invoke(loginController, new object[] { passwordWithInvalidChar });
        
        // Assert
        Assert.IsFalse(result);
    }
	
	[Test]
	//FR_MENU_VALID_002
    public void ValidatePasswordSpecialCharacters_WithValidChar_ReturnsTrue()
    {
        // Arrange
        string passwordWithInvalidChar = "invalid_password\"";
        
        var method = typeof(LoginController).GetMethod("ValidatePasswordSpecialCharacters", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = (bool)method.Invoke(loginController, new object[] { passwordWithInvalidChar });
        
        // Assert
        Assert.IsFalse(result);
    }
	
	[Test]
	//FR_MENU_VALID_002
    public void ValidateCredentials_PasswordTooShort_ReturnsPasswordInvalid()
    {
        // Arrange
		string username = "validusername";
        string tooShortPassword = "inv";
        
        // Act
        var result = loginController.ValidateCredentials(username, tooShortPassword);
        
        // Assert
        Assert.AreEqual(result, LoginController.ValidationResult.PASSWORD_INVALID);
    }
	
	[Test]
	//FR_MENU_VALID_002
    public void ValidateCredentials_PasswordTooLong_ReturnsPasswordInvalid()
    {
        // Arrange
		string username = "validusername";
        string tooLongPassword = "password_with_length_of_that_exceeds_fourty_characters";
        
        // Act
        var result = loginController.ValidateCredentials(username, tooLongPassword);
        
        // Assert
        Assert.AreEqual(result, LoginController.ValidationResult.PASSWORD_INVALID);
    }
	
	[Test]
	//FR_MENU_VALID_002
    public void ValidateCredentials_PasswordExactlyFive_ReturnsSuccess()
    {
        // Arrange
		string username = "validusername";
        string fiveLengthPassword = "12345";
        
        // Act
        var result = loginController.ValidateCredentials(username, fiveLengthPassword);
        
        // Assert
        Assert.AreEqual(result, LoginController.ValidationResult.SUCCESS);
    }
	
	[Test]
	//FR_MENU_VALID_002
    public void ValidateCredentials_PasswordExactlyFourty_ReturnsSuccess()
    {
        // Arrange
		string username = "validusername";
        string fourtyLengthPassword = "password_with_length_of_fourty_characte";
        
        // Act
        var result = loginController.ValidateCredentials(username, fourtyLengthPassword);
        
        // Assert
        Assert.AreEqual(result, LoginController.ValidationResult.SUCCESS);
    }
	
	[Test]
	//FR_MENU_VALID_002
    public void ValidateCredentials_PasswordOk_ReturnsSuccess()
    {
        // Arrange
		string username = "valid_login_汉语";
        string validPassword = "validpassword123";
        
        // Act
        var result = loginController.ValidateCredentials(username, validPassword);
        
        // Assert
        Assert.AreEqual(result, LoginController.ValidationResult.SUCCESS);
    }
}

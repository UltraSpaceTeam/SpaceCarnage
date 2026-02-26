using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Reflection;
using System.Threading.Tasks;

public class UsernameTests
{
    private GameObject testGameObject;
    private LoginController loginController;
	
	[SetUp]
    public void Setup()
    {
        // Создаем тестовый GameObject и добавляем LoginController
        testGameObject = new GameObject("TestLoginController");
        loginController = testGameObject.AddComponent<LoginController>();
    }
	
	[TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(testGameObject);
    }
	
	[Test]
	//FR_MENU_VALID_001
    public void ValidateCredentials_UsernameTooShort_ReturnsUsernameInvalid()
    {
        // Arrange
		string tooShortUsername = "";
        string password = "validpassword123";
        
        // Act
        var result = loginController.ValidateCredentials(tooShortUsername, password);
        
        // Assert
        Assert.AreEqual(result, LoginController.ValidationResult.USERNAME_INVALID);
    }
	
	[Test]
	//FR_MENU_VALID_001
    public void ValidateCredentials_UsernameTooLong_ReturnsUsernameInvalid()
    {
        // Arrange
		string tooLongUsername = "this_username_is_longer_than_40_characters_so_it_cant_be_used_as_username";
        string password = "validpassword123";
        
        // Act
        var result = loginController.ValidateCredentials(tooLongUsername, password);
        
        // Assert
        Assert.AreEqual(result, LoginController.ValidationResult.USERNAME_INVALID);
    }
	
	[Test]
	//FR_MENU_VALID_001
    public void ValidateCredentials_UsernameExactlyOne_ReturnsSuccess()
    {
        // Arrange
		string oneLenghtUsername = "z";
        string password = "validpassword123";
        
        // Act
        var result = loginController.ValidateCredentials(oneLenghtUsername, password);
        
        // Assert
        Assert.AreEqual(result, LoginController.ValidationResult.SUCCESS);
    }
	
	[Test]
	//FR_MENU_VALID_001
    public void ValidateCredentials_UsernameExactlyFourty_ReturnsSuccess()
    {
        // Arrange
		string fourtyLengthUsername = "_this_username_length_is_exactly_fourty_";
        string password = "validpassword123";
        
        // Act
        var result = loginController.ValidateCredentials(fourtyLengthUsername, password);
        
        // Assert
        Assert.AreEqual(result, LoginController.ValidationResult.SUCCESS);
    }
}

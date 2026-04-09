using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public class ConfigManagerTests
{
    private string _tmpFile;
    private string _originalFilePath;

    [SetUp]
    public void SetUp()
    {
        _tmpFile = Path.Combine(Path.GetTempPath(), $"user_config_{Guid.NewGuid():N}.cfg");

        var field = typeof(ConfigManager).GetField("filePath", BindingFlags.NonPublic | BindingFlags.Static);
        _originalFilePath = (string)field.GetValue(null);
        field.SetValue(null, _tmpFile);
    }

    [TearDown]
    public void TearDown()
    {
        var field = typeof(ConfigManager).GetField("filePath", BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, _originalFilePath);

        if (File.Exists(_tmpFile))
            File.Delete(_tmpFile);
    }

    [Test]
    public void LoadConfig_WhenFileDoesNotExist_ReturnsDefaultData()
    {
        var cfg = ConfigManager.LoadConfig();
        Assert.AreEqual("", cfg.username);
        Assert.AreEqual("", cfg.jwt_token);
        Assert.AreEqual(-1, cfg.player_id);
    }

    [Test]
    public void SaveAndLoad_RoundTrip_PreservesFields()
    {
        var data = new LoginConfigData
        {
            username = "user",
            jwt_token = "token",
            player_id = 123
        };

        ConfigManager.SaveConfig(data);
        var loaded = ConfigManager.LoadConfig();

        Assert.AreEqual("user", loaded.username);
        Assert.AreEqual("token", loaded.jwt_token);
        Assert.AreEqual(123, loaded.player_id);
    }

    [Test]
    public void ClearCredentials_ResetsSensitiveFields()
    {
        ConfigManager.SaveConfig(new LoginConfigData
        {
            username = "user",
            jwt_token = "token",
            player_id = 123
        });

        ConfigManager.ClearCredentials();
        var loaded = ConfigManager.LoadConfig();

        Assert.AreEqual("", loaded.username);
        Assert.AreEqual("", loaded.jwt_token);
        Assert.AreEqual(-1, loaded.player_id);
    }

    [Test]
    public void LoadConfig_WhenJsonCorrupted_ReturnsDefaultData()
    {
        File.WriteAllText(_tmpFile, "{ this is not json }");

        var cfg = ConfigManager.LoadConfig();

        Assert.AreEqual("", cfg.username);
        Assert.AreEqual("", cfg.jwt_token);
        Assert.AreEqual(-1, cfg.player_id);
    }

    [Test]
    public void SaveConfig_WhenDirectoryMissing_DoesNotThrow()
    {
        // Граничный случай: путь в несуществующую директорию
        var missingDirFile = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}", "user_config.cfg");
        var field = typeof(ConfigManager).GetField("filePath", BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, missingDirFile);

        Assert.DoesNotThrow(() => ConfigManager.SaveConfig(new LoginConfigData { username = "u" }));

        // Восстанавливаем путь
        field.SetValue(null, _tmpFile);
    }
}

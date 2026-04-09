using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public class ShipConfigManagerTests
{
    private string _tmpFile;
    private string _originalFilePath;

    [SetUp]
    public void SetUp()
    {
        _tmpFile = Path.Combine(Path.GetTempPath(), $"user_ship_config_{Guid.NewGuid():N}.cfg");

        var field = typeof(ShipConfigManager).GetField("filePath", BindingFlags.NonPublic | BindingFlags.Static);
        _originalFilePath = (string)field.GetValue(null);
        field.SetValue(null, _tmpFile);
    }

    [TearDown]
    public void TearDown()
    {
        var field = typeof(ShipConfigManager).GetField("filePath", BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, _originalFilePath);

        if (File.Exists(_tmpFile))
            File.Delete(_tmpFile);
    }

    [Test]
    public void HasSaved_WhenFileDoesNotExist_ReturnsFalse()
    {
        Assert.IsFalse(ShipConfigManager.HasSaved());
    }

    [Test]
    public void HasSaved_WhenFileExists_ReturnsTrue()
    {
        File.WriteAllText(_tmpFile, "{}");
        Assert.IsTrue(ShipConfigManager.HasSaved());
    }

    [Test]
    public void LoadConfig_WhenFileDoesNotExist_ReturnsDefaults()
    {
        var cfg = ShipConfigManager.LoadConfig();
        Assert.AreEqual(0, cfg.hull_id);
        Assert.AreEqual(0, cfg.weapon_id);
        Assert.AreEqual(0, cfg.engine_id);
    }

    [Test]
    public void SaveAndLoad_RoundTrip_PreservesFields()
    {
        // Попарное/комбинаторное покрытие (pairwise): варьируем 3 независимых параметра (hull/weapon/engine)
        var data = new ShipConfigData { hull_id = 1, weapon_id = 2, engine_id = 3 };

        ShipConfigManager.SaveConfig(data);
        var loaded = ShipConfigManager.LoadConfig();

        Assert.AreEqual(1, loaded.hull_id);
        Assert.AreEqual(2, loaded.weapon_id);
        Assert.AreEqual(3, loaded.engine_id);
    }

    [Test]
    public void LoadConfig_WhenJsonCorrupted_ReturnsDefaults()
    {
        File.WriteAllText(_tmpFile, "{ this is not json }");

        var cfg = ShipConfigManager.LoadConfig();

        Assert.AreEqual(0, cfg.hull_id);
        Assert.AreEqual(0, cfg.weapon_id);
        Assert.AreEqual(0, cfg.engine_id);
    }

    [Test]
    public void SaveConfig_WhenDirectoryMissing_DoesNotThrow()
    {
        var missingDirFile = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}", "user_ship_config.cfg");
        var field = typeof(ShipConfigManager).GetField("filePath", BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, missingDirFile);

        Assert.DoesNotThrow(() => ShipConfigManager.SaveConfig(new ShipConfigData { hull_id = -1, weapon_id = 0, engine_id = int.MaxValue }));

        field.SetValue(null, _tmpFile);
    }
}

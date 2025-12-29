using System;
using System.IO;
using UnityEngine;

[Serializable]
public class LoginConfigData
{
    public string username = "";
    public string jwt_token = "";
    public int player_id = -1;
}

public static class ConfigManager
{
    private static string filePath = Path.Combine(Application.persistentDataPath, "user_config.cfg");

    public static void SaveConfig(LoginConfigData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"Config saved to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save config: {e.Message}");
        }
    }

    public static LoginConfigData LoadConfig()
    {
        if (!File.Exists(filePath))
        {
            return new LoginConfigData();
        }

        try
        {
            Debug.Log(filePath);
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<LoginConfigData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load config: {e.Message}");
            return new LoginConfigData();
        }
    }

    public static void ClearCredentials()
    {
        LoginConfigData current = LoadConfig();
        current.username = "";
        current.jwt_token = "";
        current.player_id = -1;
        SaveConfig(current);
    }
}
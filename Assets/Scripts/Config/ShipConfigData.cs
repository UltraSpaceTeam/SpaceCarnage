using System;
using System.IO;
using UnityEngine;

[Serializable]
public class ShipConfigData
{
    public int hull_id = 0;
    public int weapon_id = 0;
    public int engine_id = 0;
}

public static class ShipConfigManager
{
    private static string filePath = Path.Combine(Application.persistentDataPath, "user_ship_config.cfg");
	public static bool hasSaved = false;

    public static void SaveConfig(ShipConfigData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"Config saved to: {filePath}");
			hasSaved = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save config: {e.Message}");
        }
    }

    public static ShipConfigData LoadConfig()
    {
        if (!File.Exists(filePath))
        {
            return new ShipConfigData();
        }

        try
        {
            Debug.Log(filePath);
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<ShipConfigData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load config: {e.Message}");
            return new ShipConfigData();
        }
    }
}
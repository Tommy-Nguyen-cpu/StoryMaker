using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ConfigLoader
{
    private const string ConfigPathConst = "../../../Configs/config.json";

    public static Config LoadConfig()
    {
        var assetPath = Application.dataPath + "/";

        var configPath = assetPath + ConfigPathConst;
        if (File.Exists(configPath))
        {
            using (StreamReader reader = new StreamReader(configPath))
            {
                string fileContent = reader.ReadToEnd();
                Debug.Log("File content: " + fileContent);

                return JsonUtility.FromJson<Config>(fileContent);
            }
        }
        else
        {
            Debug.LogError("File not found at path: " + configPath);
        }

        return new Config();
    }
}

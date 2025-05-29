using System.IO;
using UnityEngine;

namespace NeonGlowstick.BasisVr.GDriveHosting
{
    internal class GoogleDriveConfig
    {
        public string OAuthToken;

        #region CRUD
        private const string ConfigFile = "BasisVrGDriveConfig.json";
        private static string ConfigDirectory => Application.persistentDataPath;

        public static GoogleDriveConfig Load()
        {
            var path = Path.Combine(ConfigDirectory, ConfigFile);
            if (!File.Exists(path))
                return new GoogleDriveConfig();

            var json = File.ReadAllText(path);
            var config = JsonUtility.FromJson<GoogleDriveConfig>(json);
            return config;
        }

        public static void Save(GoogleDriveConfig config)
        {
            var directory = ConfigDirectory;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var path = Path.Combine(directory, ConfigFile);
            var json = JsonUtility.ToJson(config);
            File.WriteAllText(path, json);
        }

        public static void Delete()
        {
            var path = Path.Combine(ConfigDirectory, ConfigFile);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        #endregion
    }
}

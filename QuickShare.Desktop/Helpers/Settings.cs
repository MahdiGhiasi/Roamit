using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.Helpers
{
    internal static class Settings
    {
        private static readonly string storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"RoamitPCExtension\config");
        private static readonly string storageFile = Path.Combine(storagePath, "settings.json");

        static Settings()
        {
            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);

            if (File.Exists(storageFile))
            {
                try
                {
                    var json = File.ReadAllText(storageFile);
                    Data = JsonConvert.DeserializeObject<SettingsData>(json);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to read config: {ex.Message}");
                }
            }
        }

        public static SettingsData Data { get; } = new SettingsData();

        public static void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(Data);

                if (File.Exists(storageFile))
                    File.Delete(storageFile);

                File.WriteAllText(storageFile, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save config: {ex.Message}");
            }
        }
    }

    internal class SettingsData
    {
        public string AccountId { get; set; } = "";
    }
}

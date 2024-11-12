using Amazon.S3.Model;
using ObjectStorageExplorer.Model;
using System.IO;
using System.Text.Json;

namespace ObjectStorageExplorer.Service
{
    public static class DataService
    {

        private static readonly string SettingsFilePath = "settings.json";

        public static async Task<bool> SaveSettingsAsync(SettingsModel settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);
            return true;
        }

        public static async Task<SettingsModel> LoadSettingsAsync()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                return JsonSerializer.Deserialize<SettingsModel>(json);
            }
            return null;
        }

    }

    
}

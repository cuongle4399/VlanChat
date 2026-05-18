using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using LANChatPro.Utils;

namespace LANChatPro.Storage
{
    public static class JsonStorage
    {
        public static T? Load<T>(string filePath, JsonTypeInfo<T> typeInfo) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize(json, typeInfo);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading JSON from {filePath}", ex);
                return null;
            }
        }

        public static void Save<T>(string filePath, T data, JsonTypeInfo<T> typeInfo) where T : class
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(data, typeInfo);
                string tempPath = Path.Combine(directory, $"{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, filePath, true);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving JSON to {filePath}", ex);
            }
        }
    }
}

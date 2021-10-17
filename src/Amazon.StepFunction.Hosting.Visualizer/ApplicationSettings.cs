using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  /// <summary>Application settings, which maybe persisted to/from disk</summary>
  internal sealed class ApplicationSettings
  {
    private static JsonSerializerOptions SerializerOptions { get; } = new() { WriteIndented = true };

    public bool AutomaticallyOpenExecutions { get; set; } = false;
    public bool AutomaticallyOpenFailures   { get; set; } = false;
    public bool AutomaticallyOpenSuccesses  { get; set; } = false;
    public bool NotifyOnFailures            { get; set; } = true;
    public bool NotifyOnSuccesses           { get; set; } = true;

    public static async Task<ApplicationSettings> LoadAsync()
    {
      var filePath = GetFilePath();

      try
      {
        if (File.Exists(filePath))
        {
          await using var file = File.OpenRead(filePath);

          var settings = await JsonSerializer.DeserializeAsync<ApplicationSettings>(file);
          if (settings != null)
          {
            return settings;
          }
        }
      }
      catch (Exception exception)
      {
        Debug.Write($"An error occurred whilst loading application settings: {exception.Message}");
      }

      return new();
    }

    public async Task SaveAsync()
    {
      try
      {
        await using var file = File.OpenWrite(GetFilePath());

        await JsonSerializer.SerializeAsync(file, this, SerializerOptions);
      }
      catch (Exception exception)
      {
        Debug.Write($"An error occurred whilst saving application settings: {exception.Message}");
      }
    }

    private static string GetFilePath() => Path.Combine(AppContext.BaseDirectory, "settings.json");
  }
}
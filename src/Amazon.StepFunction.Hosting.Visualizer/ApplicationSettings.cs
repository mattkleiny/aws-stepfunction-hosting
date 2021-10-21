using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  /// <summary>Application settings, which maybe persisted to/from disk</summary>
  internal sealed class ApplicationSettings
  {
    private static JsonSerializerOptions SerializerOptions { get; } = new() { WriteIndented = true };

    public bool AutomaticallyOpenExecutions { get; set; } = false;
    public bool AutomaticallyOpenFailures   { get; set; } = false;
    public bool AutomaticallyOpenSuccesses  { get; set; } = false;
    public bool NotifyOnExecutions          { get; set; } = true;
    public bool NotifyOnFailures            { get; set; } = true;
    public bool NotifyOnSuccesses           { get; set; } = true;
    public Size LastWindowSize              { get; set; } = new(1280, 720);

    /// <summary>Loads settings from the default path</summary>
    public static async Task<ApplicationSettings> LoadAsync()
    {
      try
      {
        var filePath = GetFilePath();

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

      return new ApplicationSettings();
    }

    /// <summary>Saves the settings to the default path.</summary>
    public async Task SaveAsync()
    {
      try
      {
        await using var file = File.Open(GetFilePath(), FileMode.Create);

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
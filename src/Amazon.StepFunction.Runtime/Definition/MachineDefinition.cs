using Newtonsoft.Json;

namespace Amazon.StepFunction.Definition
{
  /// <summary>Defines the metadata used to drive a state machine as defined by the StepFunction machine language</summary>
  public sealed class MachineDefinition
  {
    /// <summary>Parses the <see cref="MachineDefinition"/> from the given json.</summary>
    public static MachineDefinition Parse(string json)
    {
      Check.NotNullOrEmpty(json, nameof(json));

      return JsonConvert.DeserializeObject<MachineDefinition>(json);
    }

    public string Comment        { get; set; }
    public string StartAt        { get; set; }
    public string Version        { get; set; }
    public int?   TimeoutSeconds { get; set; }

    [JsonProperty("States")]
    [JsonConverter(typeof(StepDefinitionsConverter))]
    public StepDefinition[] Steps { get; set; }
  }
}
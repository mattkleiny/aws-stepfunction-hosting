using Newtonsoft.Json;

namespace Amazon.StepFunction.Definition
{
  /// <summary>Defines the metadata used to drive a step as defined by the StepFunction machine language</summary>
  public sealed class StepDefinition
  {
    /// <summary>Parses the <see cref="MachineDefinition"/> from the given json.</summary>
    public static StepDefinition Parse(string json)
    {
      Check.NotNullOrEmpty(json, nameof(json));

      return JsonConvert.DeserializeObject<StepDefinition>(json);
    }

    public string Name           { get; set; }
    public string Type           { get; set; }
    public string Resource       { get; set; }
    public string Next           { get; set; }
    public string Default        { get; set; }
    public int?   Seconds        { get; set; }
    public int?   TimeoutSeconds { get; set; }
    public bool?  End            { get; set; }
  }
}
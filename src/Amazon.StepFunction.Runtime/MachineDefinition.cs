using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction
{
  /// <summary>Defines the metadata used to drive a state machine as defined by the StepFunction machine language</summary>
  [JsonConverter(typeof(MachineDefinitionConverter))]
  public sealed class MachineDefinition
  {
    /// <summary>Parses the <see cref="MachineDefinition"/> from the given json.</summary>
    public static MachineDefinition Parse(string json)
    {
      Check.NotNullOrEmpty(json, nameof(json));

      return JsonConvert.DeserializeObject<MachineDefinition>(json);
    }

    public string   Comment { get; set; }
    public string   StartAt { get; set; }
    public string   Version { get; set; }
    public TimeSpan Timeout { get; set; }

    public StepDefinition[] Steps { get; set; }

    internal sealed class MachineDefinitionConverter : JsonConverter<MachineDefinition>
    {
      public override MachineDefinition ReadJson(JsonReader reader, Type objectType, MachineDefinition existingValue, bool hasExistingValue, JsonSerializer serializer)
      {
        var token = JToken.ReadFrom(reader);

        return new MachineDefinition
        {
          Comment = token.Value<string>("Comment"),
          StartAt = token.Value<string>("StartAt"),
          Version = token.Value<string>("Version"),
          Timeout = TimeSpan.FromSeconds(token.Value<int>("TimeoutSeconds")),
          Steps   = token.Value<JObject>("States").Properties().Select(StepDefinition.Parse).ToArray()
        };
      }

      public override void WriteJson(JsonWriter writer, MachineDefinition value, JsonSerializer serializer)
      {
        throw new NotSupportedException();
      }
    }
  }
}
using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Parsing
{
  /// <summary>Defines the metadata used to drive a state machine as defined by the StepFunction machine language</summary>
  [JsonConverter(typeof(Converter))]
  public sealed class StepFunctionDefinition
  {
    /// <summary>Parses the <see cref="StepFunctionDefinition"/> from the given json.</summary>
    public static StepFunctionDefinition Parse(string json)
    {
      Check.NotNullOrEmpty(json, nameof(json));

      return JsonConvert.DeserializeObject<StepFunctionDefinition>(json);
    }

    public string   Comment { get; set; }
    public string   StartAt { get; set; }
    public string   Version { get; set; }
    public TimeSpan Timeout { get; set; }

    public StepDefinition[] Steps { get; set; }

    /// <summary>a <see cref="JsonConverter"/> that deserializes <see cref="StepFunctionDefinition"/>s directly.</summary>
    internal sealed class Converter : JsonConverter<StepFunctionDefinition>
    {
      public override void WriteJson(JsonWriter writer, StepFunctionDefinition value, JsonSerializer serializer) => throw new NotSupportedException();

      public override StepFunctionDefinition ReadJson(JsonReader reader, Type objectType, StepFunctionDefinition existingValue, bool hasExistingValue, JsonSerializer serializer)
      {
        // parses a single step definition from the given property
        StepDefinition ParseStep(JProperty property)
        {
          var body = property.Value;
          var type = body.Value<string>("Type");

          StepDefinition ExtractDefinition()
          {
            switch (type.ToLower())
            {
              case "pass":     return body.ToObject<StepDefinition.Pass>();
              case "task":     return body.ToObject<StepDefinition.Invoke>();
              case "wait":     return body.ToObject<StepDefinition.Wait>();
              case "choice":   return body.ToObject<StepDefinition.Choice>();
              case "succeed":  return body.ToObject<StepDefinition.Succeed>();
              case "fail":     return body.ToObject<StepDefinition.Fail>();
              case "parallel": return body.ToObject<StepDefinition.Parallel>();

              default:
                throw new InvalidOperationException("An unrecognized state type was specified: " + type);
            }
          }

          var definition = ExtractDefinition();

          definition.Name = property.Name;

          return definition;
        }

        var token = JToken.ReadFrom(reader);

        return new StepFunctionDefinition
        {
          Comment = token.Value<string>("Comment"),
          StartAt = token.Value<string>("StartAt"),
          Version = token.Value<string>("Version"),
          Timeout = TimeSpan.FromSeconds(token.Value<int>("TimeoutSeconds")),
          Steps   = token.Value<JObject>("States").Properties().Select(ParseStep).ToArray()
        };
      }
    }
  }
}
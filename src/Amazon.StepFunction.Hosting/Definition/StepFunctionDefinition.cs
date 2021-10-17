using System;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting.Definition
{
  /// <summary>Defines a Step Function by the JSON form of the 'Amazon States Language'.</summary>
  [JsonConverter(typeof(StepFunctionConverter))]
  public sealed class StepFunctionDefinition
  {
    public static StepFunctionDefinition Parse(string json)
    {
      Debug.Assert(!string.IsNullOrEmpty(json), "!string.IsNullOrEmpty(json)");

      return JsonConvert.DeserializeObject<StepFunctionDefinition>(json);
    }

    public string           Comment        { get; set; } = string.Empty;
    public string           StartAt        { get; set; } = string.Empty;
    public int              TimeoutSeconds { get; set; } = 300;
    public string           Version        { get; set; } = string.Empty;
    public StepDefinition[] Steps          { get; set; } = Array.Empty<StepDefinition>();

    /// <summary>The <see cref="JsonConverter{T}"/> for <see cref="StepFunctionDefinition"/>s.</summary>
    private sealed class StepFunctionConverter : JsonConverter<StepFunctionDefinition>
    {
      public override StepFunctionDefinition ReadJson(JsonReader reader, Type objectType, StepFunctionDefinition existingValue, bool hasExistingValue,
        JsonSerializer                                           serializer)
      {
        StepDefinition ParseStep(JProperty property)
        {
          var body = property.Value;
          var type = body.Value<string>("Type");

          StepDefinition definition = type.ToLower() switch
          {
            "pass"     => body.ToObject<StepDefinition.PassDefinition>(),
            "task"     => body.ToObject<StepDefinition.TaskDefinition>(),
            "choice"   => body.ToObject<StepDefinition.ChoiceDefinition>(),
            "wait"     => body.ToObject<StepDefinition.WaitDefinition>(),
            "succeed"  => body.ToObject<StepDefinition.SucceedDefinition>(),
            "fail"     => body.ToObject<StepDefinition.FailDefinition>(),
            "parallel" => body.ToObject<StepDefinition.ParallelDefinition>(),
            "map"      => body.ToObject<StepDefinition.MapDefinition>(),

            _ => throw new InvalidOperationException("An unrecognized state type was specified: " + type)
          };

          definition.Name = property.Name;

          return definition;
        }

        var token = JToken.ReadFrom(reader);

        return new StepFunctionDefinition
        {
          Comment        = token.Value<string>("Comment"),
          StartAt        = token.Value<string>("StartAt"),
          TimeoutSeconds = token.Value<int>("TimeoutSeconds"),
          Version        = token.Value<string>("Version"),
          Steps          = token.Value<JObject>("States").Properties().Select(ParseStep).ToArray()
        };
      }

      public override void WriteJson(JsonWriter writer, StepFunctionDefinition value, JsonSerializer serializer)
      {
        throw new NotSupportedException();
      }
    }
  }
}
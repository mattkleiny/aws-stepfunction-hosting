using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Definition
{
  // TODO: give this a once-over

  /// <summary>Parses <see cref="StepDefinition"/>s from a parent json object array.</summary>
  internal sealed class StepDefinitionsConverter : JsonConverter<StepDefinition[]>
  {
    public override StepDefinition[] ReadJson(JsonReader reader, Type objectType, StepDefinition[] existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
      var states      = JObject.Load(reader).Children().ToArray();
      var definitions = new StepDefinition[states.Length];

      for (var i = 0; i < states.Length; i++)
      {
        var state  = (JProperty) states[i];
        var values = (JObject) state.Value;

        var definition = StepDefinition.Parse(values.ToString());
        definition.Name = state.Name;

        definitions[i] = definition;
      }

      return definitions;
    }

    public override void WriteJson(JsonWriter writer, StepDefinition[] value, JsonSerializer serializer)
    {
      throw new NotSupportedException();
    }
  }
}
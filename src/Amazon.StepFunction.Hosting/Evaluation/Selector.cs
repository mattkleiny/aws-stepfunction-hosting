using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>
  /// Key-value parameter selector that may be expanded into a step input/output.
  /// <para/>
  /// See https://docs.aws.amazon.com/step-functions/latest/dg/concepts-input-output-filtering.html
  /// for more details.
  /// </summary>
  [DebuggerDisplay("Parameters {shape}")]
  [JsonConverter(typeof(SelectorConverter))]
  internal readonly struct Selector
  {
    private readonly StepFunctionData shape;

    public Selector(StepFunctionData shape)
    {
      this.shape = shape;
    }

    public StepFunctionData Expand(StepFunctionData input, StepFunctionData context = default, string jpath = "$")
    {
      return input.Transform(jpath, shape, context);
    }

    private sealed class SelectorConverter : JsonConverter<Selector>
    {
      public override Selector ReadJson(JsonReader reader, Type objectType, Selector existingValue, bool hasExistingValue, JsonSerializer serializer)
      {
        return new Selector(new StepFunctionData(JToken.ReadFrom(reader)));
      }

      public override void WriteJson(JsonWriter writer, Selector value, JsonSerializer serializer)
      {
        throw new NotSupportedException();
      }
    }
  }
}
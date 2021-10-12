using System;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>
  /// Encapsulates the data that a step function passes around during it's execution.
  /// <para/>
  /// This type supports coercion into a JToken-like value, which subsequently permits
  /// JPath queries, and so forth, for StepFunction accesses.
  /// </summary>
  public readonly struct StepFunctionData : IEquatable<StepFunctionData>
  {
    public static StepFunctionData Empty => default;

    private readonly JToken? value;

    public StepFunctionData(object? value)
    {
      this.value = value switch
      {
        StepFunctionData data => data.value, // don't nest StepFunctionData types
        bool raw              => new JValue(raw),
        string raw            => new JValue(raw),
        int raw               => new JValue(raw),
        float raw             => new JValue(raw),
        TimeSpan raw          => new JValue(raw),
        DateTime raw          => new JValue(raw),
        JToken token          => token,
        _ when value != null  => JObject.FromObject(value),
        _                     => null
      };
    }

    public StepFunctionData Query(string jpath)
    {
      if (value != null && !string.IsNullOrEmpty(jpath))
      {
        return new StepFunctionData(value.SelectToken(jpath));
      }

      return this;
    }

    public T? Cast<T>()
    {
      if (value != null)
      {
        return value.ToObject<T>();
      }

      return default;
    }

    public object? Cast(Type type)
    {
      return value?.ToObject(type);
    }

    public override string ToString() => value?.ToString() ?? "null";

    public          bool Equals(StepFunctionData other) => JToken.DeepEquals(value, other.value);
    public override bool Equals(object? obj)            => obj is StepFunctionData other && Equals(other);

    public override int GetHashCode() => throw new NotSupportedException();

    public static bool operator ==(StepFunctionData left, StepFunctionData right) => left.Equals(right);
    public static bool operator !=(StepFunctionData left, StepFunctionData right) => !left.Equals(right);
  }
}
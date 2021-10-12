using System;
using System.Diagnostics.CodeAnalysis;
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

    public StepFunctionData GetPath(string jpath)
    {
      if (value != null && !string.IsNullOrEmpty(jpath))
      {
        return new StepFunctionData(value.SelectToken(jpath));
      }

      return this;
    }

    public T? Cast<T>()
    {
      // N.B: this form is to allow lifting T to a potentially non-nullable type
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

    public T Query<T>(string jpath)
    {
      return (T) Query(jpath, typeof(T));
    }

    public object Query(string jpath, Type type)
    {
      if (!TryQuery(jpath, type, out var result))
      {
        throw new Exception($"Failed to query jpath expression {jpath} from value {value}");
      }

      return result;
    }

    public bool TryQuery<T>(string jpath, out T result)
    {
      if (TryQuery(jpath, typeof(T), out var value))
      {
        result = (T) value;
        return true;
      }

      result = default!;
      return false;
    }

    public bool TryQuery(string jpath, Type type, [NotNullWhen(true)] out object? result)
    {
      if (string.IsNullOrEmpty(jpath))
      {
        result = Cast(type)!;
        return true;
      }

      if (value == null)
      {
        result = default;
        return false;
      }

      result = value
        .SelectToken(jpath, errorWhenNoMatch: true)
        .ToObject(type);

      return true;
    }

    public override string ToString() => value?.ToString() ?? "null";

    public          bool Equals(StepFunctionData other) => JToken.DeepEquals(value, other.value);
    public override bool Equals(object? obj)            => obj is StepFunctionData other && Equals(other);

    public override int GetHashCode() => throw new NotSupportedException();

    public static bool operator ==(StepFunctionData left, StepFunctionData right) => left.Equals(right);
    public static bool operator !=(StepFunctionData left, StepFunctionData right) => !left.Equals(right);
  }
}
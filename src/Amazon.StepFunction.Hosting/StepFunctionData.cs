using System;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Encapsulates the data that a step function passes around during it's execution.</summary>
  public sealed record StepFunctionData
  {
    public static StepFunctionData None { get; } = new(Value: null);

    public object? Value { get; init; }

    public static StepFunctionData Wrap(object? value)
    {
      if (value is StepFunctionData data)
      {
        return data;
      }

      return new StepFunctionData(value);
    }

    private StepFunctionData(object? Value)
    {
      this.Value = Value;
    }

    /// <summary>Queries value of the given type from the given path, throwing an exception if it fails.</summary>
    public T Query<T>(string jpath)
    {
      return (T)Query(jpath, typeof(T));
    }

    /// <summary>Queries value of the given type from the given path, throwing an exception if it fails.</summary>
    public object Query(string jpath, Type type)
    {
      if (!TryQuery(jpath, type, out object result))
      {
        throw new Exception($"Failed to query jpath expression {jpath} from value {Value}");
      }

      return result;
    }

    /// <summary>Attempts to query a value of the given type from the given path.</summary>
    public bool TryQuery<T>(string jpath, out T result)
    {
      if (TryQuery(jpath, typeof(T), out var value))
      {
        result = (T)value;
        return true;
      }

      result = default!;
      return false;
    }

    /// <summary>Attempts to query a value of the given type from the given path.</summary>
    public bool TryQuery(string jpath, Type type, out object result)
    {
      if (string.IsNullOrEmpty(jpath))
      {
        result = Cast(type)!;
        return true;
      }

      var token = JToken.FromObject(Value);
      result = token.SelectToken(jpath, errorWhenNoMatch: true).ToObject(type);

      return true;
    }

    public T? Cast<T>()
    {
      return (T?)Value;
    }

    public object? Cast(Type type)
    {
      return Convert.ChangeType(Value, type);
    }
  }
}
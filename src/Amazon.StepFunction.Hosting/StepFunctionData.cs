using System;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Encapsulates the data that a step function passes around during it's execution.</summary>
  public sealed class StepFunctionData
  {
    /// <summary>Wraps the given value as <see cref="StepFunctionData"/>.</summary>
    public static StepFunctionData Wrap(object value)
    {
      if (value is StepFunctionData data)
      {
        return data;
      }

      return new StepFunctionData(value);
    }

    private StepFunctionData(object value)
    {
      if (value != null)
      {
        Value = value;
        Token = JToken.FromObject(value);
      }
    }

    /// <summary>The CLR type of this data, passed through from our step function input or handler outputs.</summary>
    public object Value { get; }

    /// <summary>The <see cref="JToken"/> representation of this data, serialized from the original input.</summary>
    public JToken Token { get; }

    /// <summary>Queries the given jpath expression on the given <see cref="Type"/>.</summary>
    public T Query<T>(string jpath) => (T) Query(jpath, typeof(T));

    /// <summary>Queries the given jpath expression on the given <see cref="Type"/>.</summary>
    public object Query(string jpath, Type type)
    {
      Check.NotNull(type, nameof(type));

      if (string.IsNullOrEmpty(jpath))
      {
        return Reinterpret(type);
      }

      return Token?.SelectToken(jpath, errorWhenNoMatch: true).ToObject(type);
    }

    /// <summary>Attempts to cast the <see cref="StepFunctionData"/> to the given <see cref="Type"/> via JSON serialization.</summary>
    public T Reinterpret<T>() => (T) Reinterpret(typeof(T));

    /// <summary>Attempts to cast the <see cref="StepFunctionData"/> to the given <see cref="Type"/> via JSON serialization.</summary>
    public object Reinterpret(Type type)
    {
      Check.NotNull(type, nameof(type));

      if (Value == null) return null;

      if (type.IsAssignableFrom(Value?.GetType()))
      {
        return Value;
      }

      return Token?.ToObject(type);
    }
  }
}
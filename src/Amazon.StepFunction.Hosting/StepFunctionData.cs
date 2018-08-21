using System;
using System.Dynamic;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Encapsulates the data that a step function passes around during it's execution.</summary>
  public sealed class StepFunctionData
  {
    private readonly Lazy<JToken> token;

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
      Value = value;
      token = new Lazy<JToken>(() => JToken.FromObject(value));
    }

    public object Value { get; }
    public JToken Token => token.Value;

    public T Query<T>(string jpath) => (T) Query(jpath, typeof(T));
    public T Reinterpret<T>()       => (T) Reinterpret(typeof(T));

    public object Query(string jpath, Type type)
    {
      Check.NotNull(type, nameof(type));

      if (string.IsNullOrEmpty(jpath))
      {
        return Reinterpret(type);
      }

      return Token.SelectToken(jpath).ToObject(type);
    }

    public object Reinterpret(Type type)
    {
      Check.NotNull(type, nameof(type));

      if (type.IsAssignableFrom(Value?.GetType()))
      {
        return Value;
      }

      return JToken.FromObject(Value).ToObject(type);
    }

    /// <summary>Converts the data to a dynamic object, permitting dynamic conversion at the callsite.</summary>
    public dynamic AsDynamic() => new DynamicConversion(this);

    /// <summary>Dynamically converts the <see cref="StepFunctionData"/> to a type dynamically at the callsite.</summary>
    private sealed class DynamicConversion : DynamicObject
    {
      private readonly StepFunctionData data;

      public DynamicConversion(StepFunctionData data) => this.data = data;

      public override bool TryConvert(ConvertBinder binder, out object result)
      {
        result = data.Reinterpret(binder.ReturnType);

        return base.TryConvert(binder, out result);
      }
    }
  }
}
using System;
using System.Dynamic;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>
  /// An opaque wrapper for the data that passes through a Step Function during it's execution.
  /// <para/>
  /// This type supports coercion into a different forms, JPath queries, and input/output transformation.
  /// It  also supports dynamic type coercion through the `dynamic` keyword, which allows runtime dispatch
  /// for varying implementation types.
  /// </summary>
  public readonly struct StepFunctionData : IEquatable<StepFunctionData>, IDynamicMetaObjectProvider
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

    DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
    {
      if (value is IDynamicMetaObjectProvider provider)
      {
        return provider.GetMetaObject(parameter);
      }

      return null!;
    }
  }
}
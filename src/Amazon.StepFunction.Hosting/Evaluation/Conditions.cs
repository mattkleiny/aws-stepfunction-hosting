using System;
using System.Diagnostics;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Evaluates some input to determine which state to transition to next.</summary>
  internal delegate bool Condition(StepFunctionData data);

  internal static class Conditions
  {
    public static Condition True  { get; } = Always(true);
    public static Condition False { get; } = Always(false);

    public static Condition Always(bool result)                               => _ => result;
    public static Condition Not(Condition condition, string next)             => data => !condition(data);
    public static Condition Or(Condition left, Condition right, string next)  => data => left(data) || right(data);
    public static Condition And(Condition left, Condition right, string next) => data => left(data) && right(data);

    public static Condition Equals<T>(T value)
    {
      switch (value)
      {
        case bool raw:     return data => data.Cast<bool>() == raw;
        case string raw:   return data => data.Cast<string>() == raw;
        case int raw:      return data => data.Cast<int>() == raw;
        case TimeSpan raw: return data => data.Cast<TimeSpan>() == raw;

        default:
          throw new ArgumentException($"An unsupported type was requested: {typeof(T)}");
      }
    }

    public static Condition LessThan<T>(T value)
    {
      switch (value)
      {
        case int raw:      return data => data.Cast<int>() < raw;
        case TimeSpan raw: return data => data.Cast<TimeSpan>() < raw;

        default:
          throw new ArgumentException($"An unsupported type was requested: {typeof(T)}");
      }
    }

    public static Condition LessThanEquals<T>(T value)
    {
      switch (value)
      {
        case int raw:      return data => data.Cast<int>() <= raw;
        case TimeSpan raw: return data => data.Cast<TimeSpan>() <= raw;

        default:
          throw new ArgumentException($"An unsupported type was requested: {typeof(T)}");
      }
    }

    public static Condition GreaterThan<T>(T value)
    {
      switch (value)
      {
        case int raw:      return data => data.Cast<int>() > raw;
        case TimeSpan raw: return data => data.Cast<TimeSpan>() > raw;

        default:
          throw new ArgumentException($"An unsupported type was requested: {typeof(T)}");
      }
    }

    public static Condition GreaterThanEquals<T>(T value)
    {
      switch (value)
      {
        case int raw:      return data => data.Cast<int>() >= raw;
        case TimeSpan raw: return data => data.Cast<TimeSpan>() >= raw;

        default:
          throw new ArgumentException($"An unsupported type was requested: {typeof(T)}");
      }
    }

    /// <summary>Parses a <see cref="Condition"/> from the given expression.</summary>
    public static Condition Parse(string type, string value)
    {
      Debug.Assert(!string.IsNullOrEmpty(type), "!string.IsNullOrEmpty(type)");
      Debug.Assert(!string.IsNullOrEmpty(value), "!string.IsNullOrEmpty(expression)");

      return type.ToLower() switch
      {
        "booleanequals" => Equals(bool.Parse(value)),
        "stringequals"  => Equals(value),

        "numericequals"            => Equals(int.Parse(value)),
        "numericlessthan"          => LessThan(int.Parse(value)),
        "numericlessthanequals"    => LessThanEquals(int.Parse(value)),
        "numericgreaterthan"       => GreaterThan(int.Parse(value)),
        "numericgreaterthanequals" => GreaterThanEquals(int.Parse(value)),

        "timestampequals"            => Equals(TimeSpan.Parse(value)),
        "timestamplessthan"          => LessThan(TimeSpan.Parse(value)),
        "timestamplessthanequals"    => LessThanEquals(TimeSpan.Parse(value)),
        "timestampgreaterthan"       => GreaterThan(TimeSpan.Parse(value)),
        "timestampgreaterthanequals" => GreaterThanEquals(TimeSpan.Parse(value)),

        "and" => throw new NotImplementedException(),
        "or"  => throw new NotImplementedException(),
        "not" => throw new NotImplementedException(),

        _ => throw new NotSupportedException($"An unrecognized condition was requested: {type}")
      };
    }
  }
}
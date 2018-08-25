using System;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Evaluates some input to determine which state to transition to next.</summary>
  internal delegate bool Condition(StepFunctionData data);

  /// <summary>Common <see cref="Condition"/> combinators.</summary>
  internal static class Conditions
  {
    public static Condition Not(Condition condition, string next)             => data => !condition(data);
    public static Condition Or(Condition left, Condition right, string next)  => data => left(data) || right(data);
    public static Condition And(Condition left, Condition right, string next) => data => left(data) && right(data);

    public static Condition Equals<T>(T value)
    {
      switch (value)
      {
        case bool raw:     return data => data.Reinterpret<bool>()     == raw;
        case string raw:   return data => data.Reinterpret<string>()   == raw;
        case int raw:      return data => data.Reinterpret<int>()      == raw;
        case TimeSpan raw: return data => data.Reinterpret<TimeSpan>() == raw;

        default:
          throw new ArgumentException($"An unsupported type was requested: {typeof(T)}");
      }
    }

    public static Condition LessThan<T>(T value)
    {
      switch (value)
      {
        case int raw:      return data => data.Reinterpret<int>()      < raw;
        case TimeSpan raw: return data => data.Reinterpret<TimeSpan>() < raw;

        default:
          throw new ArgumentException($"An unsupported type was requested: {typeof(T)}");
      }
    }

    public static Condition LessThanEquals<T>(T value)
    {
      switch (value)
      {
        case int raw:      return data => data.Reinterpret<int>()      <= raw;
        case TimeSpan raw: return data => data.Reinterpret<TimeSpan>() <= raw;

        default:
          throw new ArgumentException($"An unsupported type was requested: {typeof(T)}");
      }
    }

    public static Condition GreaterThan<T>(T value)
    {
      switch (value)
      {
        case int raw:      return data => data.Reinterpret<int>()      > raw;
        case TimeSpan raw: return data => data.Reinterpret<TimeSpan>() > raw;

        default:
          throw new ArgumentException($"An unsupported type was requested: {typeof(T)}");
      }
    }

    public static Condition GreaterThanEquals<T>(T value)
    {
      switch (value)
      {
        case int raw:      return data => data.Reinterpret<int>()      >= raw;
        case TimeSpan raw: return data => data.Reinterpret<TimeSpan>() >= raw;

        default:
          throw new ArgumentException($"An unsupported type was requested: {typeof(T)}");
      }
    }

    /// <summary>Builds an <see cref="Condition"/> for the given expression of the given type.</summary>
    public static Condition Build(string type, string expression)
    {
      Check.NotNullOrEmpty(type, nameof(type));
      Check.NotNullOrEmpty(expression, nameof(expression));

      switch (type.ToLower())
      {
        case "booleanequals": return Equals(bool.Parse(expression));
        case "stringequals":  return Equals(expression);

        case "numericequals":            return Equals(int.Parse(expression));
        case "numericlessthan":          return LessThan(int.Parse(expression));
        case "numericlessthanequals":    return LessThanEquals(int.Parse(expression));
        case "numericgreaterthan":       return GreaterThan(int.Parse(expression));
        case "numericgreaterthanequals": return GreaterThanEquals(int.Parse(expression));

        case "timestampequals":            return Equals(TimeSpan.Parse(expression));
        case "timestamplessthan":          return LessThan(TimeSpan.Parse(expression));
        case "timestamplessthanequals":    return LessThanEquals(TimeSpan.Parse(expression));
        case "timestampgreaterthan":       return GreaterThan(TimeSpan.Parse(expression));
        case "timestampgreaterthanequals": return GreaterThanEquals(TimeSpan.Parse(expression));

        case "and": throw new NotImplementedException();
        case "or":  throw new NotImplementedException();
        case "not": throw new NotImplementedException();

        default:
          throw new NotSupportedException($"An unrecognized condition was requested: {type}");
      }
    }
  }
}
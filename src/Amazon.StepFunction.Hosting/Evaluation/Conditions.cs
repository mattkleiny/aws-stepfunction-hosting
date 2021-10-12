using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Conditionally evaluates <see cref="StepFunctionData"/> and determines the next state to transition to.</summary>
  [JsonConverter(typeof(Converter))]
  internal abstract record Condition(string Next)
  {
    public static Condition Null { get; } = new NullCondition();

    public static Condition Not(string next, Condition other)               => new UnaryCondition(next, UnaryOperator.Not, other);
    public static Condition And(string next, IEnumerable<Condition> others) => new VariadicCondition(next, VariadicOperator.And, others);
    public static Condition Or(string next, IEnumerable<Condition> others)  => new VariadicCondition(next, VariadicOperator.Or, others);

    public static Condition Variable(string next, string path, Predicate<StepFunctionData> predicate)
    {
      return new PredicateCondition(next, path, predicate);
    }

    public abstract bool Evaluate(StepFunctionData input);

    private enum UnaryOperator
    {
      Not
    }

    private enum VariadicOperator
    {
      And,
      Or
    }

    /// <summary>A no-op <see cref="Condition"/>.</summary>
    private sealed record NullCondition() : Condition(string.Empty)
    {
      public override bool Evaluate(StepFunctionData input) => false;
    }

    /// <summary>A <see cref="Condition"/> which evaluates some variable.</summary>
    private sealed record PredicateCondition(string Next, string VariablePath, Predicate<StepFunctionData> Predicate) : Condition(Next)
    {
      public override bool Evaluate(StepFunctionData input)
      {
        return Predicate(input.Query(VariablePath));
      }
    }

    /// <summary>A <see cref="Condition"/> which evaluates some unary operator.</summary>
    private sealed record UnaryCondition(string Next, UnaryOperator Operator, Condition Condition) : Condition(Next)
    {
      public override bool Evaluate(StepFunctionData input)
      {
        return Operator switch
        {
          UnaryOperator.Not => !Condition.Evaluate(input),

          _ => throw new ArgumentOutOfRangeException(nameof(Operator), Operator.ToString(), null)
        };
      }
    }

    /// <summary>A <see cref="Condition"/> which evaluates some combinator predicate.</summary>
    private sealed record VariadicCondition(string Next, VariadicOperator Operator, IEnumerable<Condition> Conditions) : Condition(Next)
    {
      public override bool Evaluate(StepFunctionData input)
      {
        return Operator switch
        {
          VariadicOperator.And => Conditions.All(condition => condition.Evaluate(input)),
          VariadicOperator.Or  => Conditions.Any(condition => condition.Evaluate(input)),

          _ => throw new ArgumentOutOfRangeException(nameof(Operator), Operator.ToString(), null)
        };
      }
    }

    /// <summary>Converts <see cref="Condition"/>s from raw JSON.</summary>
    private sealed class Converter : JsonConverter<Condition>
    {
      private static Condition Parse(string next, string path, string type, JToken value)
      {
        return type.ToLower() switch
        {
          "booleanequals" => Variable(next, path, data => data.Cast<bool>() == value.Value<bool>()),
          "stringequals"  => Variable(next, path, data => data.Cast<string>() == value.Value<string>()),

          "numericequals"            => Variable(next, path, data => data.Cast<int>() == value.Value<int>()),
          "numericlessthan"          => Variable(next, path, data => data.Cast<int>() < value.Value<int>()),
          "numericlessthanequals"    => Variable(next, path, data => data.Cast<int>() <= value.Value<int>()),
          "numericgreaterthan"       => Variable(next, path, data => data.Cast<int>() > value.Value<int>()),
          "numericgreaterthanequals" => Variable(next, path, data => data.Cast<int>() >= value.Value<int>()),

          "timestampequals"            => Variable(next, path, data => data.Cast<DateTime>() == value.Value<DateTime>()),
          "timestamplessthan"          => Variable(next, path, data => data.Cast<DateTime>() < value.Value<DateTime>()),
          "timestamplessthanequals"    => Variable(next, path, data => data.Cast<DateTime>() <= value.Value<DateTime>()),
          "timestampgreaterthan"       => Variable(next, path, data => data.Cast<DateTime>() > value.Value<DateTime>()),
          "timestampgreaterthanequals" => Variable(next, path, data => data.Cast<DateTime>() >= value.Value<DateTime>()),

          _ => throw new NotSupportedException($"An unrecognized condition was requested: {type} for {value}")
        };
      }

      public override Condition ReadJson(JsonReader reader, Type objectType, Condition existingValue, bool hasExistingValue, JsonSerializer serializer)
      {
        var raw = JToken.ReadFrom(reader);
        if (raw is not JObject container)
          throw new InvalidOperationException($"An unrecognized condition was requested: {raw}");

        var variable  = string.Empty;
        var next      = string.Empty;
        var condition = Null;

        // scan through all properties and determine basic details, like variable path and next names
        foreach (var property in container.Properties())
        {
          var name  = property.Name;
          var value = property.Value;

          if (string.Equals("Variable", name, StringComparison.OrdinalIgnoreCase))
            variable = value.Value<string>();

          if (string.Equals("Next", name, StringComparison.OrdinalIgnoreCase))
            next = value.Value<string>();
        }

        // scan through remaining properties and evaluate into condition expressions
        foreach (var property in container.Properties())
        {
          var name  = property.Name;
          var value = property.Value;

          if (string.Equals("Variable", name, StringComparison.OrdinalIgnoreCase))
            continue;

          if (string.Equals("Next", name, StringComparison.OrdinalIgnoreCase))
            continue;

          condition = name.ToLower() switch
          {
            "and"     => And(next, value.ToObject<Condition[]>()),
            "or"      => Or(next, value.ToObject<Condition[]>()),
            "not"     => Not(next, value.ToObject<Condition>()),
            var other => Parse(next, variable, other, value)
          };
        }

        return condition;
      }

      public override void WriteJson(JsonWriter writer, Condition value, JsonSerializer serializer)
      {
        throw new NotSupportedException();
      }
    }
  }
}
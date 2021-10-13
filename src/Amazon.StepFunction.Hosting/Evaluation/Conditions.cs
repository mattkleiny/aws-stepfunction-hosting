using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.StepFunction.Hosting.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Conditionally evaluates <see cref="StepFunctionData"/> and determines the next state to transition to.</summary>
  [JsonConverter(typeof(ConditionConverter))]
  internal abstract record Condition(string Next)
  {
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

    /// <summary>A <see cref="Condition"/> which evaluates some predicate against some variable.</summary>
    private sealed record PredicateCondition(string Next, string VariablePath, Predicate<StepFunctionData> Predicate) : Condition(Next)
    {
      public override bool Evaluate(StepFunctionData input)
      {
        return Predicate(input.Query(VariablePath));
      }
    }

    /// <summary>A <see cref="Condition"/> which evaluates some unary operator against a condition.</summary>
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

    /// <summary>A <see cref="Condition"/> which evaluates some variadic operator against all sub conditions.</summary>
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

    /// <summary>The <see cref="JsonConverter{T}"/> for <see cref="Condition"/>s.</summary>
    private sealed class ConditionConverter : JsonConverter<Condition>
    {
      public override Condition ReadJson(JsonReader reader, Type objectType, Condition existingValue, bool hasExistingValue, JsonSerializer serializer)
      {
        var raw = JToken.ReadFrom(reader);
        if (raw is not JObject container)
        {
          throw new JsonException($"An unrecognized condition was requested {raw}");
        }

        // bucket properties by name, try and extract they common parameters, first
        var propertiesByName = container.Properties().ToDictionary(_ => _.Name, _ => _.Value, StringComparer.OrdinalIgnoreCase);

        var variablePath = propertiesByName.TryPopValueOrDefault("Variable")?.Value<string>() ?? string.Empty;
        var nextState    = propertiesByName.TryPopValueOrDefault("Next")?.Value<string>() ?? string.Empty;

        if (propertiesByName.Count == 0)
        {
          throw new JsonException($"No valid conditions exist in {raw}");
        }

        // the remaining property is the condition variant
        var (key, value) = propertiesByName.First();

        return key.ToLower() switch
        {
          // N.B: we're recursive on the sub-condition paths
          "and" => new VariadicCondition(nextState, VariadicOperator.And, value.ToObject<Condition[]>()),
          "or"  => new VariadicCondition(nextState, VariadicOperator.Or, value.ToObject<Condition[]>()),
          "not" => new UnaryCondition(nextState, UnaryOperator.Not, value.ToObject<Condition>()),

          var other => Parse(nextState, variablePath, other, value)
        };
      }

      public override void WriteJson(JsonWriter writer, Condition value, JsonSerializer serializer)
      {
        throw new NotSupportedException();
      }

      private static PredicateCondition Parse(string next, string variable, string type, JToken comparand) => type.ToLower() switch
      {
        "booleanequals" => new(next, variable, input => input.Cast<bool>() == comparand.Value<bool>()),
        "stringequals"  => new(next, variable, input => input.Cast<string>() == comparand.Value<string>()),

        "numericequals"            => new(next, variable, input => input.Cast<int>() == comparand.Value<int>()),
        "numericlessthan"          => new(next, variable, input => input.Cast<int>() < comparand.Value<int>()),
        "numericlessthanequals"    => new(next, variable, input => input.Cast<int>() <= comparand.Value<int>()),
        "numericgreaterthan"       => new(next, variable, input => input.Cast<int>() > comparand.Value<int>()),
        "numericgreaterthanequals" => new(next, variable, input => input.Cast<int>() >= comparand.Value<int>()),

        "timestampequals"            => new(next, variable, input => input.Cast<DateTime>() == comparand.Value<DateTime>()),
        "timestamplessthan"          => new(next, variable, input => input.Cast<DateTime>() < comparand.Value<DateTime>()),
        "timestamplessthanequals"    => new(next, variable, input => input.Cast<DateTime>() <= comparand.Value<DateTime>()),
        "timestampgreaterthan"       => new(next, variable, input => input.Cast<DateTime>() > comparand.Value<DateTime>()),
        "timestampgreaterthanequals" => new(next, variable, input => input.Cast<DateTime>() >= comparand.Value<DateTime>()),

        _ => throw new NotSupportedException($"An unrecognized condition was requested: {type} for {comparand}")
      };
    }
  }
}
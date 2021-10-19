using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.StepFunction.Hosting.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Conditionally evaluates <see cref="StepFunctionData"/> and determines the next state to transition to.</summary>
  [JsonConverter(typeof(ChoiceConverter))]
  internal abstract record Choice(string Next)
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

    /// <summary>A <see cref="Choice"/> which evaluates some predicate against some variable.</summary>
    private sealed record VariableChoice(string Next, string VariablePath, Predicate<StepFunctionData> Predicate) : Choice(Next)
    {
      public override bool Evaluate(StepFunctionData input)
      {
        return Predicate(input.Query(VariablePath));
      }
    }

    /// <summary>A <see cref="Choice"/> which evaluates some unary operator against another <see cref="Choice"/>.</summary>
    private sealed record UnaryChoice(string Next, UnaryOperator Operator, Choice Choice) : Choice(Next)
    {
      public override bool Evaluate(StepFunctionData input)
      {
        return Operator switch
        {
          UnaryOperator.Not => !Choice.Evaluate(input),

          _ => throw new ArgumentOutOfRangeException(nameof(Operator), Operator.ToString(), null)
        };
      }
    }

    /// <summary>A <see cref="Choice"/> which evaluates some variadic operator against all sub <see cref="Choice"/>s.</summary>
    private sealed record VariadicChoice(string Next, VariadicOperator Operator, IEnumerable<Choice> Choices) : Choice(Next)
    {
      public override bool Evaluate(StepFunctionData input)
      {
        return Operator switch
        {
          VariadicOperator.And => Choices.All(choice => choice.Evaluate(input)),
          VariadicOperator.Or  => Choices.Any(choice => choice.Evaluate(input)),

          _ => throw new ArgumentOutOfRangeException(nameof(Operator), Operator.ToString(), null)
        };
      }
    }

    /// <summary>Parses <see cref="Choice"/>s from JSON.</summary>
    private sealed class ChoiceConverter : JsonConverter<Choice>
    {
      public override Choice ReadJson(JsonReader reader, Type objectType, Choice existingValue, bool hasExistingValue, JsonSerializer serializer)
      {
        var raw = JToken.ReadFrom(reader);
        if (raw is not JObject container)
        {
          throw new JsonException($"An unrecognized choice was requested {raw}");
        }

        // bucket properties by name, try and extract they common parameters, first
        var propertiesByName = container.Properties().ToDictionary(_ => _.Name, _ => _.Value, StringComparer.OrdinalIgnoreCase);

        var variablePath = propertiesByName.TryPopValueOrDefault("Variable")?.Value<string>() ?? string.Empty;
        var nextState    = propertiesByName.TryPopValueOrDefault("Next")?.Value<string>() ?? string.Empty;

        if (propertiesByName.Count == 0)
        {
          throw new JsonException($"No valid choices exist in {raw}");
        }

        // the remaining property is the choice variant
        var (key, value) = propertiesByName.First();

        return key.ToLower() switch
        {
          // N.B: we're recursive on the sub-choice paths
          "and" => new VariadicChoice(nextState, VariadicOperator.And, value.ToObject<Choice[]>()),
          "or"  => new VariadicChoice(nextState, VariadicOperator.Or, value.ToObject<Choice[]>()),
          "not" => new UnaryChoice(nextState, UnaryOperator.Not, value.ToObject<Choice>()),

          var other => Parse(nextState, variablePath, other, value)
        };
      }

      public override void WriteJson(JsonWriter writer, Choice value, JsonSerializer serializer)
      {
        throw new NotSupportedException();
      }

      private static VariableChoice Parse(string next, string variable, string type, JToken comparand) => type.ToLower() switch
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

        _ => throw new NotSupportedException($"An unrecognized choice was requested: {type} for {comparand}")
      };
    }
  }
}
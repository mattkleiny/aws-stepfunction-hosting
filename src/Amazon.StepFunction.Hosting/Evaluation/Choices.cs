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
    private sealed record PredicateChoice(string Next, Predicate<StepFunctionData> Predicate) : Choice(Next)
    {
      public override bool Evaluate(StepFunctionData input)
      {
        return Predicate(input);
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

    private sealed class ChoiceConverter : JsonConverter<Choice>
    {
      public override Choice ReadJson(JsonReader reader, Type objectType, Choice existingValue, bool hasExistingValue, JsonSerializer serializer)
      {
        var raw = JToken.ReadFrom(reader);
        if (raw is not JObject container)
        {
          throw new JsonException($"An unrecognized choice was requested {raw}");
        }

        // bucket properties by name, try and extract the common parameters, first
        var propertiesByName = container.Properties().ToDictionary(_ => _.Name, _ => _.Value, StringComparer.OrdinalIgnoreCase);

        var variablePath = propertiesByName.TryPopValueOrDefault("Variable")?.Value<string>() ?? string.Empty;
        var nextState    = propertiesByName.TryPopValueOrDefault("Next")?.Value<string>() ?? string.Empty;

        if (propertiesByName.Count == 0)
        {
          throw new JsonException($"No valid choices exist in {raw}");
        }

        // the remaining property is the choice variant
        var (type, value) = propertiesByName.First();

        return type.ToLower() switch
        {
          // N.B: we're recursive on the sub-choice paths
          "and" => new VariadicChoice(nextState, VariadicOperator.And, value.ToObject<Choice[]>()),
          "or"  => new VariadicChoice(nextState, VariadicOperator.Or, value.ToObject<Choice[]>()),
          "not" => new UnaryChoice(nextState, UnaryOperator.Not, value.ToObject<Choice>()),

          var otherType => Parse(otherType, nextState, variablePath, value)
        };
      }

      public override void WriteJson(JsonWriter writer, Choice value, JsonSerializer serializer)
      {
        throw new NotSupportedException();
      }

      private static PredicateChoice Parse(string type, string next, string variable, JToken comparand) => type.ToLower() switch
      {
        "isnull"      => new(next, input => input.IsNull),
        "ispresent"   => new(next, input => input.IsPresent),
        "isboolean"   => new(next, input => bool.TryParse(input.Query(variable).Cast<string>(), out _)),
        "isnumeric"   => new(next, input => float.TryParse(input.Query(variable).Cast<string>(), out _)),
        "istimestamp" => new(next, input => DateTime.TryParse(input.Query(variable).Cast<string>(), out _)),

        "booleanequals"     => new(next, input => input.Query(variable).Cast<bool>() == comparand.Value<bool>()),
        "booleanequalspath" => new(next, input => input.Query(variable).Cast<bool>() == input.CastPath<bool>(comparand.Value<string>())),
        "stringequals"      => new(next, input => input.Query(variable).Cast<string>() == comparand.Value<string>()),
        "stringequalspath"  => new(next, input => input.Query(variable).Cast<string>() == input.CastPath<string>(comparand.Value<string>())),

        "numericequals"                => new(next, input => Math.Abs(input.Query(variable).Cast<float>() - comparand.Value<float>()) < float.Epsilon),
        "numericequalspath"            => new(next, input => Math.Abs(input.Query(variable).Cast<float>() - input.CastPath<float>(comparand.Value<string>())) < float.Epsilon),
        "numericlessthan"              => new(next, input => input.Query(variable).Cast<float>() < comparand.Value<float>()),
        "numericlessthanpath"          => new(next, input => input.Query(variable).Cast<float>() < input.CastPath<float>(comparand.Value<string>())),
        "numericlessthanequals"        => new(next, input => input.Query(variable).Cast<float>() <= comparand.Value<float>()),
        "numericlessthanequalspath"    => new(next, input => input.Query(variable).Cast<float>() <= input.CastPath<float>(comparand.Value<string>())),
        "numericgreaterthan"           => new(next, input => input.Query(variable).Cast<float>() > comparand.Value<float>()),
        "numericgreaterthanpath"       => new(next, input => input.Query(variable).Cast<float>() > input.CastPath<float>(comparand.Value<string>())),
        "numericgreaterthanequals"     => new(next, input => input.Query(variable).Cast<float>() >= comparand.Value<float>()),
        "numericgreaterthanequalspath" => new(next, input => input.Query(variable).Cast<float>() >= input.CastPath<float>(comparand.Value<string>())),

        "timestampequals"                => new(next, input => input.Query(variable).Cast<DateTime>() == comparand.Value<DateTime>()),
        "timestampequalspath"            => new(next, input => input.Query(variable).Cast<DateTime>() == input.CastPath<DateTime>(comparand.Value<string>())),
        "timestamplessthan"              => new(next, input => input.Query(variable).Cast<DateTime>() < comparand.Value<DateTime>()),
        "timestamplessthanpath"          => new(next, input => input.Query(variable).Cast<DateTime>() < input.CastPath<DateTime>(comparand.Value<string>())),
        "timestamplessthanequals"        => new(next, input => input.Query(variable).Cast<DateTime>() <= comparand.Value<DateTime>()),
        "timestamplessthanequalspath"    => new(next, input => input.Query(variable).Cast<DateTime>() <= input.CastPath<DateTime>(comparand.Value<string>())),
        "timestampgreaterthan"           => new(next, input => input.Query(variable).Cast<DateTime>() > comparand.Value<DateTime>()),
        "timestampgreaterthanpath"       => new(next, input => input.Query(variable).Cast<DateTime>() > input.CastPath<DateTime>(comparand.Value<string>())),
        "timestampgreaterthanequals"     => new(next, input => input.Query(variable).Cast<DateTime>() >= comparand.Value<DateTime>()),
        "timestampgreaterthanequalspath" => new(next, input => input.Query(variable).Cast<DateTime>() >= input.CastPath<DateTime>(comparand.Value<string>())),

        _ => throw new NotSupportedException($"An unrecognized choice was requested: {type} for {comparand}")
      };
    }
  }
}
using System;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Evaluates some input to determine which state to transition to next.</summary>
  internal delegate string Evaluator(StepFunctionData data);

  /// <summary>Common <see cref="Evaluator"/> combinators.</summary>
  internal static class Evaluators
  {
    public static Evaluator Or(Evaluator left, Evaluator right, string next)  => throw new NotImplementedException();
    public static Evaluator And(Evaluator left, Evaluator right, string next) => throw new NotImplementedException();
    public static Evaluator Not(Evaluator condition, string next)             => throw new NotImplementedException();

    public static Evaluator Equals<T>(T value)            => throw new NotImplementedException();
    public static Evaluator LessThan<T>(T value)          => throw new NotImplementedException();
    public static Evaluator LessThanEquals<T>(T value)    => throw new NotImplementedException();
    public static Evaluator GreaterThan<T>(T value)       => throw new NotImplementedException();
    public static Evaluator GreaterThanEquals<T>(T value) => throw new NotImplementedException();

    /// <summary>Parses the <see cref="Evaluator"/> given by the given expression of the given type.</summary>
    public static Evaluator Parse(string type, string expression) => throw new NotImplementedException();
  }
}
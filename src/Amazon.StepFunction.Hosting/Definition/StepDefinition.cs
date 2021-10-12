using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Amazon.StepFunction.Hosting.Evaluation;

namespace Amazon.StepFunction.Hosting.Definition
{
  // TODO: implement conditional evaluation
  // TODO: support various timeout formats

  /// <summary>Defines the metadata used to drive a step as defined by the StepFunction machine language</summary>
  public abstract record StepDefinition
  {
    public string Name       { get; set; } = string.Empty;
    public string Next       { get; set; } = string.Empty;
    public bool   End        { get; set; } = false;
    public string Comment    { get; set; } = string.Empty;
    public string InputPath  { get; set; } = string.Empty;
    public string ResultPath { get; set; } = string.Empty;

    internal abstract Step Create(StepHandlerFactory factory);

    public sealed record PassDefinition : StepDefinition
    {
      public string Result     { get; set; } = string.Empty;
      public string Parameters { get; set; } = string.Empty;

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.PassStep
        {
          Name  = Name,
          Next  = Next,
          IsEnd = End
        };
      }
    }

    public sealed record TaskDefinition : StepDefinition
    {
      public string                      Resource       { get; set; }  = string.Empty;
      public int                         TimeoutSeconds { get; set; }  = 300;
      public List<RetryPolicyDefinition> Retry          { get; init; } = new();
      public List<CatchPolicyDefinition> Catch          { get; init; } = new();

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.TaskStep(Resource, factory)
        {
          Name        = Name,
          Next        = Next,
          IsEnd       = End,
          InputPath   = InputPath,
          ResultPath  = ResultPath,
          Timeout     = TimeSpan.FromSeconds(TimeoutSeconds),
          RetryPolicy = RetryPolicy.Composite(Retry.Select(_ => _.ToRetryPolicy())),
          CatchPolicy = CatchPolicy.Composite(Catch.Select(_ => _.ToCatchPolicy()))
        };
      }
    }

    public sealed record ChoiceDefinition : StepDefinition
    {
      public ChoiceRule[] Choices { get; set; } = Array.Empty<ChoiceRule>();
      public string       Default { get; set; } = string.Empty;

      internal override Step Create(StepHandlerFactory factory)
      {
        // TODO: parse the choice rules and expressions

        return new Step.ChoiceStep
        {
          Name    = Name,
          Default = Default
        };
      }

      public sealed record ChoiceRule
      {
        public string            Variable   { get; set; } = string.Empty;
        public ChoiceExpression? Expression { get; set; } = null;
        public string            Next       { get; set; } = string.Empty;
      }

      public sealed record ChoiceExpression(string Type, string Value)
      {
        internal Condition ToCondition() => Conditions.Parse(Type, Value);
      }
    }

    public sealed record WaitDefinition : StepDefinition
    {
      public int      Seconds       { get; set; } = 0;
      public TimeSpan Timestamp     { get; set; } = TimeSpan.MinValue;
      public string   SecondsPath   { get; set; } = string.Empty;
      public string   TimestampPath { get; set; } = string.Empty;

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.WaitStep
        {
          Name     = Name,
          Duration = TimeSpan.FromSeconds(Seconds),
          Next     = Next,
          IsEnd    = End
        };
      }
    }

    public sealed record SucceedDefinition : StepDefinition
    {
      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.SucceedStep
        {
          Name = Name
        };
      }
    }

    public sealed record FailDefinition : StepDefinition
    {
      public string Cause { get; set; } = string.Empty;

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.FailStep
        {
          Name  = Name,
          Cause = Cause
        };
      }
    }

    public sealed record ParallelDefinition : StepDefinition
    {
      public List<StepFunctionDefinition> Branches { get; init; } = new();

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.ParallelStep(factory)
        {
          Name     = Name,
          Next     = Next,
          IsEnd    = End,
          Branches = Branches.ToImmutableList()
        };
      }
    }

    public sealed record MapDefinition : StepDefinition
    {
      public List<StepFunctionDefinition> Branches { get; init; } = new();

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.MapStep(factory)
        {
          Name = Name
        };
      }
    }

    public sealed class RetryPolicyDefinition
    {
      public string[] ErrorEquals     { get; set; } = Array.Empty<string>();
      public int      IntervalSeconds { get; set; }
      public int      MaxAttempts     { get; set; }
      public float    BackoffRate     { get; set; }

      internal RetryPolicy ToRetryPolicy()
      {
        if (ErrorEquals.Length > 0 && IntervalSeconds > 0 && MaxAttempts > 0)
        {
          var errorSet = new ErrorSet(ErrorEquals);
          var delay    = TimeSpan.FromSeconds(IntervalSeconds);

          return RetryPolicy.Exponential(errorSet, MaxAttempts, delay, BackoffRate);
        }

        return RetryPolicy.Null;
      }
    }

    public sealed class CatchPolicyDefinition
    {
      public string[] ErrorEquals { get; set; } = Array.Empty<string>();
      public string   ResultPath  { get; set; } = string.Empty;
      public string   Next        { get; set; } = string.Empty;

      internal CatchPolicy ToCatchPolicy()
      {
        if (ErrorEquals.Length > 0)
        {
          var errorSet = new ErrorSet(ErrorEquals);

          return CatchPolicy.Standard(errorSet, ResultPath, Next);
        }

        return CatchPolicy.Null;
      }
    }

    /// <summary>This is a sealed hierarchy.</summary>
    private StepDefinition()
    {
    }
  }
}
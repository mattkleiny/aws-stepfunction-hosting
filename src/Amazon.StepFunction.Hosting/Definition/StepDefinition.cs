using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Amazon.StepFunction.Hosting.Evaluation;

namespace Amazon.StepFunction.Hosting.Definition
{
  /// <summary>Defines a single Step in a <see cref="StepFunctionDefinition"/>, as defined by the JSON form of the 'Amazon States Language'.</summary>
  public abstract record StepDefinition
  {
    public string Name       { get; set; } = string.Empty;
    public string Next       { get; set; } = string.Empty;
    public bool   End        { get; set; } = false;
    public string Comment    { get; set; } = string.Empty;
    public string InputPath  { get; set; } = string.Empty;
    public string ResultPath { get; set; } = string.Empty;

    internal abstract Step Create(StepHandlerFactory factory);

    /// <summary>Describes a <see cref="Step.PassStep"/>.</summary>
    public sealed record PassDefinition : StepDefinition
    {
      public string Result     { get; set; } = string.Empty;
      public string Parameters { get; set; } = string.Empty;

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.PassStep
        {
          InputPath  = InputPath,
          ResultPath = InputPath,
          Name       = Name,
          Next       = Next
        };
      }
    }

    /// <summary>Describes a <see cref="Step.TaskStep"/>.</summary>
    public sealed record TaskDefinition : StepDefinition
    {
      public string  Resource           { get; set; } = string.Empty;
      public int     TimeoutSeconds     { get; set; } = 300;
      public string? TimeoutSecondsPath { get; set; } = null;

      public List<RetryPolicyDefinition> Retry { get; init; } = new();
      public List<CatchPolicyDefinition> Catch { get; init; } = new();

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.TaskStep(Resource, factory)
        {
          Name            = Name,
          Next            = Next,
          IsEnd           = End,
          InputPath       = InputPath,
          ResultPath      = ResultPath,
          TimeoutProvider = TimeSpanProviders.FromDurationParts(TimeoutSecondsPath, TimeoutSeconds),
          RetryPolicy     = RetryPolicy.Composite(Retry.Select(_ => _.ToRetryPolicy())),
          CatchPolicy     = CatchPolicy.Composite(Catch.Select(_ => _.ToCatchPolicy()))
        };
      }
    }

    /// <summary>Describes a <see cref="Step.ChoiceStep"/>.</summary>
    public sealed record ChoiceDefinition : StepDefinition
    {
      internal Condition[] Choices { get; set; } = Array.Empty<Condition>();
      public   string      Default { get; set; } = string.Empty;

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.ChoiceStep
        {
          Name       = Name,
          Default    = Default,
          Conditions = Choices.ToImmutableList()
        };
      }
    }

    /// <summary>Describes a <see cref="Step.WaitStep"/>.</summary>
    public sealed record WaitDefinition : StepDefinition
    {
      public int      Seconds       { get; set; } = 0;
      public string?  SecondsPath   { get; set; } = default;
      public DateTime Timestamp     { get; set; } = default;
      public string?  TimestampPath { get; set; } = default;

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.WaitStep
        {
          Name = Name,
          WaitTimeProvider = Timestamp > DateTime.MinValue || TimestampPath != null
            ? TimeSpanProviders.FromTimestampParts(TimestampPath, Timestamp)
            : TimeSpanProviders.FromDurationParts(SecondsPath, Seconds),
          Next  = Next,
          IsEnd = End
        };
      }
    }

    /// <summary>Describes a <see cref="Step.SucceedStep"/>.</summary>
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

    /// <summary>Describes a <see cref="Step.FailStep"/>.</summary>
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

    /// <summary>Describes a <see cref="Step.ParallelStep"/>.</summary>
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

    /// <summary>Describes a <see cref="Step.MapStep"/>.</summary>
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

    /// <summary>Describes a <see cref="RetryPolicy"/>, used throughout other step types that support execution.</summary>
    public sealed record RetryPolicyDefinition
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

    /// <summary>Describes a <see cref="CatchPolicy"/>, used throughout other step types that support execution.</summary>
    public sealed record CatchPolicyDefinition
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
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Amazon.StepFunction.Hosting.Evaluation;
using Newtonsoft.Json;

namespace Amazon.StepFunction.Hosting.Definition
{
  /// <summary>Defines a single Step in a <see cref="StepFunctionDefinition"/>, as defined by the JSON form of the 'Amazon States Language'.</summary>
  public abstract record StepDefinition
  {
    [JsonProperty] public string Name       { get; set; } = string.Empty;
    [JsonProperty] public string Next       { get; set; } = string.Empty;
    [JsonProperty] public bool   End        { get; set; } = false;
    [JsonProperty] public string Comment    { get; set; } = string.Empty;
    [JsonProperty] public string InputPath  { get; set; } = string.Empty;
    [JsonProperty] public string ResultPath { get; set; } = string.Empty;

    /// <summary>Potential connections that this step might exhibit; mainly used for visualization</summary>
    public virtual IEnumerable<string> Connections => Enumerable.Empty<string>();

    internal abstract Step Create(StepHandlerFactory factory);

    /// <summary>Describes a <see cref="Step.PassStep"/>.</summary>
    public sealed record PassDefinition : StepDefinition
    {
      [JsonProperty] public string Result     { get; set; } = string.Empty;
      [JsonProperty] public string Parameters { get; set; } = string.Empty;

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.PassStep
        {
          Name       = Name,
          Next       = Next,
          InputPath  = InputPath,
          ResultPath = ResultPath,
          Result     = Result,
          Parameters = Parameters
        };
      }
    }

    /// <summary>Describes a <see cref="Step.TaskStep"/>.</summary>
    public sealed record TaskDefinition : StepDefinition
    {
      [JsonProperty] public string  Resource           { get; set; } = string.Empty;
      [JsonProperty] public int     TimeoutSeconds     { get; set; } = 300;
      [JsonProperty] public string? TimeoutSecondsPath { get; set; } = null;

      [JsonProperty] public List<RetryPolicyDefinition> Retry { get; init; } = new();
      [JsonProperty] public List<CatchPolicyDefinition> Catch { get; init; } = new();

      public override IEnumerable<string> Connections
      {
        get
        {
          yield return Next;

          foreach (var catchPolicy in Catch)
          {
            yield return catchPolicy.Next;
          }
        }
      }

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
      [JsonProperty] internal Condition[] Choices { get; set; } = Array.Empty<Condition>();
      [JsonProperty] public   string      Default { get; set; } = string.Empty;

      public override IEnumerable<string> Connections
      {
        get
        {
          yield return Default;

          foreach (var choice in Choices)
          {
            yield return choice.Next;
          }
        }
      }

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
      [JsonProperty] public int      Seconds       { get; set; } = 0;
      [JsonProperty] public string?  SecondsPath   { get; set; } = default;
      [JsonProperty] public DateTime Timestamp     { get; set; } = default;
      [JsonProperty] public string?  TimestampPath { get; set; } = default;

      public override IEnumerable<string> Connections
      {
        get { yield return Next; }
      }

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
      [JsonProperty] public string Cause { get; set; } = string.Empty;

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
      [JsonProperty] public List<StepFunctionDefinition> Branches { get; init; } = new();

      public override IEnumerable<string> Connections
      {
        get { yield return Next; }
      }

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
      [JsonProperty] public List<StepFunctionDefinition> Branches { get; init; } = new();

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
      [JsonProperty] public string[] ErrorEquals     { get; set; } = Array.Empty<string>();
      [JsonProperty] public int      IntervalSeconds { get; set; }
      [JsonProperty] public int      MaxAttempts     { get; set; }
      [JsonProperty] public float    BackoffRate     { get; set; }

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
      [JsonProperty] public string[] ErrorEquals { get; set; } = Array.Empty<string>();
      [JsonProperty] public string   ResultPath  { get; set; } = string.Empty;
      [JsonProperty] public string   Next        { get; set; } = string.Empty;

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
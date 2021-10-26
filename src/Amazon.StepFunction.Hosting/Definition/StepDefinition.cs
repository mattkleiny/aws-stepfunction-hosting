using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Amazon.StepFunction.Hosting.Evaluation;
using Newtonsoft.Json;

namespace Amazon.StepFunction.Hosting.Definition
{
  /// <summary>Defines a single Step in a <see cref="StepFunctionDefinition"/> by the JSON form of the 'Amazon States Language'.</summary>
  public abstract record StepDefinition
  {
    public abstract string Type { get; }

    [JsonProperty] public string Name       { get; set; } = string.Empty;
    [JsonProperty] public string Next       { get; set; } = string.Empty;
    [JsonProperty] public bool   End        { get; set; } = false;
    [JsonProperty] public string Comment    { get; set; } = string.Empty;
    [JsonProperty] public string InputPath  { get; set; } = string.Empty;
    [JsonProperty] public string OutputPath { get; set; } = string.Empty;
    [JsonProperty] public string ResultPath { get; set; } = string.Empty;

    public bool IsTerminal => End || Type is "Success" or "Fail";

    public virtual IEnumerable<string>                 PossibleConnections => Enumerable.Empty<string>();
    public virtual IEnumerable<StepFunctionDefinition> NestedBranches      => Enumerable.Empty<StepFunctionDefinition>();

    internal abstract Step Create(StepHandlerFactory factory, Impositions impositions);

    /// <summary>Describes a <see cref="Step.PassStep"/>.</summary>
    public sealed record PassDefinition : StepDefinition
    {
      public override string Type => "Pass";

      [JsonProperty] public string Result     { get; set; } = string.Empty;
      [JsonProperty] public string Parameters { get; set; } = string.Empty;

      public override IEnumerable<string> PossibleConnections
      {
        get { yield return Next; }
      }

      internal override Step Create(StepHandlerFactory factory, Impositions impositions)
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
      public override string Type => "Task";

      [JsonProperty] public string  Resource           { get; set; } = string.Empty;
      [JsonProperty] public int     TimeoutSeconds     { get; set; } = 300;
      [JsonProperty] public string? TimeoutSecondsPath { get; set; } = null;

      [JsonProperty] public List<RetryPolicyDefinition> Retry { get; init; } = new();
      [JsonProperty] public List<CatchPolicyDefinition> Catch { get; init; } = new();

      public override IEnumerable<string> PossibleConnections
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

      internal override Step Create(StepHandlerFactory factory, Impositions impositions)
      {
        return new Step.TaskStep(Resource, factory)
        {
          Name            = Name,
          Next            = Next,
          IsEnd           = End,
          InputPath       = InputPath,
          ResultPath      = ResultPath,
          TimeoutProvider = TimeSpanProviders.FromSecondsParts(TimeoutSecondsPath, TimeoutSeconds),
          RetryPolicy     = RetryPolicy.Composite(Retry.Select(_ => _.ToRetryPolicy())),
          CatchPolicy     = CatchPolicy.Composite(Catch.Select(_ => _.ToCatchPolicy()))
        };
      }
    }

    /// <summary>Describes a <see cref="Step.ChoiceStep"/>.</summary>
    public sealed record ChoiceDefinition : StepDefinition
    {
      public override string Type => "Choice";

      [JsonProperty] internal Choice[] Choices { get; set; } = Array.Empty<Choice>();
      [JsonProperty] public   string   Default { get; set; } = string.Empty;

      public override IEnumerable<string> PossibleConnections
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

      internal override Step Create(StepHandlerFactory factory, Impositions impositions)
      {
        return new Step.ChoiceStep
        {
          Name    = Name,
          Default = Default,
          Choices = Choices.ToImmutableList()
        };
      }
    }

    /// <summary>Describes a <see cref="Step.WaitStep"/>.</summary>
    public sealed record WaitDefinition : StepDefinition
    {
      public override string Type => "Wait";

      [JsonProperty] public int      Seconds       { get; set; } = 0;
      [JsonProperty] public string?  SecondsPath   { get; set; } = default;
      [JsonProperty] public DateTime Timestamp     { get; set; } = default;
      [JsonProperty] public string?  TimestampPath { get; set; } = default;

      public override IEnumerable<string> PossibleConnections
      {
        get { yield return Next; }
      }

      internal override Step Create(StepHandlerFactory factory, Impositions impositions)
      {
        return new Step.WaitStep
        {
          Name = Name,
          WaitTimeProvider = Timestamp > DateTime.MinValue || TimestampPath != null
            ? TimeSpanProviders.FromTimestampParts(TimestampPath, Timestamp)
            : TimeSpanProviders.FromSecondsParts(SecondsPath, Seconds),
          Next  = Next,
          IsEnd = End
        };
      }
    }

    /// <summary>Describes a <see cref="Step.SucceedStep"/>.</summary>
    public sealed record SucceedDefinition : StepDefinition
    {
      public override string Type => "Succeed";

      internal override Step Create(StepHandlerFactory factory, Impositions impositions)
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
      public override string Type => "Fail";

      [JsonProperty] public string Cause { get; set; } = string.Empty;

      internal override Step Create(StepHandlerFactory factory, Impositions impositions)
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
      public override string Type => "Parallel";

      [JsonProperty] public List<StepFunctionDefinition> Branches { get; init; } = new();

      [JsonProperty] public List<RetryPolicyDefinition> Retry { get; init; } = new();
      [JsonProperty] public List<CatchPolicyDefinition> Catch { get; init; } = new();

      public override IEnumerable<string> PossibleConnections
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

      public override IEnumerable<StepFunctionDefinition> NestedBranches => Branches;

      internal override Step Create(StepHandlerFactory factory, Impositions impositions)
      {
        var branches = Branches.Select(definition => new StepFunctionHost(definition, factory, impositions));

        return new Step.ParallelStep
        {
          Name        = Name,
          Next        = Next,
          IsEnd       = End,
          InputPath   = InputPath,
          OutputPath  = OutputPath,
          ResultPath  = ResultPath,
          RetryPolicy = RetryPolicy.Composite(Retry.Select(_ => _.ToRetryPolicy())),
          CatchPolicy = CatchPolicy.Composite(Catch.Select(_ => _.ToCatchPolicy())),
          Branches    = branches.ToImmutableList()
        };
      }
    }

    /// <summary>Describes a <see cref="Step.MapStep"/>.</summary>
    public sealed record MapDefinition : StepDefinition
    {
      public override string Type => "Map";

      [JsonProperty] public StepFunctionDefinition Iterator       { get; set; } = new();
      [JsonProperty] public string                 ItemsPath      { get; set; } = string.Empty;
      [JsonProperty] public int                    MaxConcurrency { get; set; } = 0;
      [JsonProperty] public string                 ResultSelector { get; set; } = string.Empty;

      [JsonProperty] public List<RetryPolicyDefinition> Retry { get; init; } = new();
      [JsonProperty] public List<CatchPolicyDefinition> Catch { get; init; } = new();

      public override IEnumerable<string> PossibleConnections
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

      public override IEnumerable<StepFunctionDefinition> NestedBranches
      {
        get { yield return Iterator; }
      }

      internal override Step Create(StepHandlerFactory factory, Impositions impositions)
      {
        return new Step.MapStep(new StepFunctionHost(Iterator, factory, impositions))
        {
          Name           = Name,
          Next           = Next,
          IsEnd          = End,
          ItemsPath      = ItemsPath,
          MaxConcurrency = MaxConcurrency,
          InputPath      = InputPath,
          OutputPath     = OutputPath,
          ResultPath     = ResultPath,
          ResultSelector = ResultSelector,
          RetryPolicy    = RetryPolicy.Composite(Retry.Select(_ => _.ToRetryPolicy())),
          CatchPolicy    = CatchPolicy.Composite(Catch.Select(_ => _.ToCatchPolicy()))
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
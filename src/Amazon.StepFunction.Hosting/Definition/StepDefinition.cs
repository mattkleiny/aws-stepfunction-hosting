using System;
using Amazon.StepFunction.Hosting.Evaluation;
using Newtonsoft.Json;

namespace Amazon.StepFunction.Hosting.Definition
{
  // TODO: implement retry descriptions
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
    public string OutputPath { get; set; } = string.Empty;

    internal abstract Step Create(StepHandlerFactory factory);

    public sealed record PassDefinition : StepDefinition
    {
      public string Result     { get; set; } = string.Empty;
      public string ResultPath { get; set; } = string.Empty;
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
      public string Resource       { get; set; } = string.Empty;
      public int    TimeoutSeconds { get; set; } = 300;

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.TaskStep(() => factory(this))
        {
          Name       = Name,
          Next       = Next,
          InputPath  = InputPath,
          OutputPath = InputPath,
          Timeout    = TimeSpan.FromSeconds(TimeoutSeconds),
          IsEnd      = End
        };
      }
    }

    public sealed record ChoiceDefinition : StepDefinition
    {
      public ChoiceRule[] Choices { get; set; } = Array.Empty<ChoiceRule>();
      public string       Default { get; set; } = string.Empty;

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.ChoiceStep
        {
          Name    = Name,
          Default = Default
        };
      }

      [JsonConverter(typeof(Converter))]
      public sealed record ChoiceRule
      {
        public string            Variable   { get; set; } = string.Empty;
        public ChoiceExpression? Expression { get; set; } = null;
        public string            Next       { get; set; } = string.Empty;

        private sealed class Converter : JsonConverter<ChoiceRule>
        {
          public override ChoiceRule ReadJson(JsonReader reader, Type objectType, ChoiceRule existingValue, bool hasExistingValue, JsonSerializer serializer)
          {
            throw new NotImplementedException();
          }

          public override void WriteJson(JsonWriter writer, ChoiceRule value, JsonSerializer serializer)
          {
            throw new NotSupportedException();
          }
        }
      }

      public sealed record ChoiceExpression(string Type, string Value);
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
      public string Error { get; set; } = string.Empty;

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.FailStep
        {
          Name  = Name,
          Error = Error,
          Cause = Cause
        };
      }
    }

    public sealed record ParallelDefinition : StepDefinition
    {
      public StepFunctionDefinition[] Branches       { get; set; } = Array.Empty<StepFunctionDefinition>();
      public string                   ResultPath     { get; set; } = string.Empty;
      public string                   ResultSelector { get; set; } = string.Empty;

      internal override Step Create(StepHandlerFactory factory)
      {
        return new Step.ParallelStep(factory)
        {
          Name     = Name,
          Next     = Next,
          IsEnd    = End,
          Branches = Branches
        };
      }
    }

    /// <summary>This is a sealed hierarchy.</summary>
    private StepDefinition()
    {
    }
  }
}
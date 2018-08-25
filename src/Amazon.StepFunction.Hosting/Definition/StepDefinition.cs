using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting.Definition
{
  // TODO: implement retry descriptions
  // TODO: implement conditional evaluation
  // TODO: support various timeout formats

  /// <summary>Defines the metadata used to drive a step as defined by the StepFunction machine language</summary>
  public abstract class StepDefinition
  {
    public string Name { get; set; }

    internal abstract Step Create(StepHandlerFactory factory);

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Pass"/>.</summary>
    public sealed class Pass : StepDefinition
    {
      public string Next { get; set; }
      public bool   End  { get; set; }

      internal override Step Create(StepHandlerFactory factory) => new Step.Pass
      {
        Name  = Name,
        Next  = Next,
        IsEnd = End
      };
    }

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Invoke"/>.</summary>
    public sealed class Invoke : StepDefinition
    {
      public string Resource       { get; set; }
      public string Next           { get; set; }
      public string InputPath      { get; set; } = null;
      public string OutputPath     { get; set; } = null;
      public int    TimeoutSeconds { get; set; } = 300;
      public bool   End            { get; set; }

      internal override Step Create(StepHandlerFactory factory) => new Step.Invoke(() => factory(this))
      {
        Name       = Name,
        Next       = Next,
        InputPath  = InputPath,
        OutputPath = InputPath,
        Timeout    = TimeSpan.FromSeconds(TimeoutSeconds),
        IsEnd      = End
      };
    }

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Wait"/>.</summary>
    public sealed class Wait : StepDefinition
    {
      public int    Seconds { get; set; }
      public string Next    { get; set; }
      public bool   End     { get; set; }

      internal override Step Create(StepHandlerFactory factory) => new Step.Wait
      {
        Name     = Name,
        Duration = TimeSpan.FromSeconds(Seconds),
        Next     = Next,
        IsEnd    = End
      };
    }

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Choice"/>.</summary>
    public sealed class Choice : StepDefinition
    {
      public Branch[] Choices { get; set; }
      public string   Default { get; set; }

      internal override Step Create(StepHandlerFactory factory) => new Step.Choice
      {
        Name    = Name,
        Default = Default
      };

      public sealed class Expression
      {
        public string Variable  { get; set; }
        public string Condition { get; set; }
      }

      [JsonConverter(typeof(Converter))]
      public sealed class Branch
      {
        public string     Type       { get; set; }
        public Expression Expression { get; set; }
        public string     Next       { get; set; }

        internal sealed class Converter : JsonConverter<Branch>
        {
          public override Branch ReadJson(JsonReader reader, Type objectType, Branch existingValue, bool hasExistingValue, JsonSerializer serializer)
          {
            var token = JToken.ReadFrom(reader);

            throw new NotImplementedException();
          }

          public override void WriteJson(JsonWriter writer, Branch value, JsonSerializer serializer)
          {
            throw new NotSupportedException();
          }
        }
      }
    }

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Succeed"/>.</summary>
    public sealed class Succeed : StepDefinition
    {
      internal override Step Create(StepHandlerFactory factory) => new Step.Succeed
      {
        Name = Name
      };
    }

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Fail"/>.</summary>
    public sealed class Fail : StepDefinition
    {
      public string Error { get; set; }
      public string Cause { get; set; }

      internal override Step Create(StepHandlerFactory factory) => new Step.Fail
      {
        Name  = Name,
        Error = Error,
        Cause = Cause
      };
    }

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Parallel"/>.</summary>
    public sealed class Parallel : StepDefinition
    {
      public string Next { get; set; }
      public bool   End  { get; set; }

      public StepFunctionDefinition[] Branches { get; set; }

      internal override Step Create(StepHandlerFactory factory) => new Step.Parallel
      {
        Name     = Name,
        Next     = Next,
        IsEnd    = End,
        Branches = Branches,
        Factory  = factory
      };
    }

    /// <summary>This is a sealed hierarchy.</summary>
    private StepDefinition()
    {
    }
  }
}
using System;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction
{
  /// <summary>Defines the metadata used to drive a step as defined by the StepFunction machine language</summary>
  public abstract class StepDefinition
  {
    internal static StepDefinition Parse(JProperty property)
    {
      var body = property.Value;
      var type = body.Value<string>("Type");

      StepDefinition Extract()
      {
        switch (type.ToLower())
        {
          case "pass":     return property.Value<PassDefinition>();
          case "task":     return property.Value<InvokeDefinition>();
          case "wait":     return property.Value<WaitDefinition>();
          case "choice":   return property.Value<ChoiceDefinition>();
          case "succeed":  return property.Value<SucceedDefinition>();
          case "fail":     return property.Value<FailDefinition>();
          case "parallel": return property.Value<ParallelDefinition>();

          default:
            throw new InvalidOperationException("An unrecognized state type was specified: " + type);
        }
      }

      var definition = Extract();

      definition.Name = property.Name;

      return definition;
    }

    public string Name { get; set; }
    public string Type { get; set; }

    internal abstract Step Create(StepHandlerFactory factory);

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Pass"/>.</summary>
    public sealed class PassDefinition : StepDefinition
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
    public sealed class InvokeDefinition : StepDefinition
    {
      public string   Resource { get; set; }
      public string   Next     { get; set; }
      public bool     End      { get; set; }
      public TimeSpan Timeout  { get; set; }

      internal override Step Create(StepHandlerFactory factory) => new Step.Invoke(() => factory(this))
      {
        Name    = Name,
        Next    = Next,
        Timeout = Timeout,
        IsEnd   = End
      };
    }

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Wait"/>.</summary>
    public sealed class WaitDefinition : StepDefinition
    {
      public TimeSpan Duration { get; set; }
      public string   Next     { get; set; }
      public bool     End      { get; set; }

      internal override Step Create(StepHandlerFactory factory) => new Step.Wait
      {
        Name     = Name,
        Duration = Duration,
        Next     = Next,
        IsEnd    = End
      };
    }

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Choice"/>.</summary>
    public sealed class ChoiceDefinition : StepDefinition
    {
      public delegate bool Condition(object input);
      public delegate string Selector(object input);

      public Choice[] Choices { get; set; }
      public string   Default { get; set; }

      internal override Step Create(StepHandlerFactory factory) => new Step.Choice
      {
        Name     = Name,
        Default  = Default,
        Selector = BuildSelector(Choices, Default)
      };

      private static Selector BuildSelector(Choice[] choices, string defaultChoice) => input =>
      {
        foreach (var choice in choices)
        {
          if (choice.Condition(input))
          {
            return choice.Next;
          }
        }

        return defaultChoice;
      };

      public sealed class Choice
      {
        public string    Next      { get; set; }
        public Condition Condition { get; set; }
      }
    }

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Succeed"/>.</summary>
    public sealed class SucceedDefinition : StepDefinition
    {
      internal override Step Create(StepHandlerFactory factory) => new Step.Succeed
      {
        Name = Name
      };
    }

    /// <summary>A <see cref="StepDefinition"/> for <see cref="Step.Fail"/>.</summary>
    public sealed class FailDefinition : StepDefinition
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
    public sealed class ParallelDefinition : StepDefinition
    {
      public MachineDefinition[] Branches { get; set; }

      internal override Step Create(StepHandlerFactory factory)
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>This is sealed ADT.</summary>
    private StepDefinition()
    {
    }
  }
}
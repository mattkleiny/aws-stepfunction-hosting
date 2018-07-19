﻿using System;

namespace Amazon.StepFunction
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
    public sealed class Wait : StepDefinition
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
    public sealed class Choice : StepDefinition
    {
      public delegate string Evaluator(object input);

      public Expression[] Expressions { get; set; }
      public string       Default     { get; set; }

      internal override Step Create(StepHandlerFactory factory) => new Step.Choice
      {
        Name      = Name,
        Default   = Default,
        Evaluator = BuildEvaluator(Expressions, Default)
      };

      private static Evaluator BuildEvaluator(Expression[] expressions, string defaultChoice) => input =>
      {
        foreach (var expression in expressions)
        {
          if (expression.Evaluate(input))
          {
            return expression.Next;
          }
        }

        return defaultChoice;
      };

      public sealed class Expression
      {
        public string Variable { get; set; }
        public string Next     { get; set; }

        public bool Evaluate(object input) => false; // TODO: implement me
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

    /// <summary>This is sealed ADT.</summary>
    private StepDefinition()
    {
    }
  }
}
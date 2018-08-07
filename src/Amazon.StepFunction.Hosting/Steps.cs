using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Defines a possible step in a <see cref="StepFunction"/>.</summary>
  internal abstract class Step
  {
    /// <summary>The name of this step.</summary>
    public string Name { get; set; }

    /// <summary>Executes the step asynchronously, observing any required step transition behaviour.</summary>
    public async Task<Transition> ExecuteAsync(Impositions impositions, object input = null, CancellationToken cancellationToken = default)
    {
      try
      {
        return await ExecuteInnerAsync(impositions, input, cancellationToken);
      }
      catch (Exception exception)
      {
        return Transitions.Fail(exception);
      }
    }

    /// <summary>Implements the actual execution operation for this step type.</summary>
    protected abstract Task<Transition> ExecuteInnerAsync(Impositions impositions, object input, CancellationToken cancellationToken);

    /// <summary>A <see cref="Step"/> that passes it's input to output.</summary>
    public sealed class Pass : Step
    {
      public bool   IsEnd { get; set; }
      public string Next  { get; set; }

      protected override Task<Transition> ExecuteInnerAsync(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        var transition = IsEnd
          ? Transitions.Succeed(input)
          : Transitions.Next(Next, input);

        return Task.FromResult(transition);
      }
    }

    /// <summary>A <see cref="Step"/> that invoke a handler for some given resource.</summary>
    public sealed class Invoke : Step
    {
      private readonly Func<StepHandler> factory;

      public Invoke(Func<StepHandler> factory)
      {
        this.factory = factory;
      }

      public bool        IsEnd       { get; set; }
      public TimeSpan    Timeout     { get; set; }
      public string      Next        { get; set; }
      public RetryPolicy RetryPolicy { get; set; } = RetryPolicies.Null;

      protected override async Task<Transition> ExecuteInnerAsync(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        try
        {
          var output = await RetryPolicy(async () =>
          {
            using (var timeoutToken = new CancellationTokenSource(Timeout))
            using (var linkedTokens = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, cancellationToken))
            {
              var handler = factory();

              return await handler(input, linkedTokens.Token);
            }
          });

          return IsEnd
            ? Transitions.Succeed(output)
            : Transitions.Next(Next, output);
        }
        catch (Exception exception)
        {
          return Transitions.Fail(exception);
        }
      }
    }

    /// <summary>A <see cref="Step"/> that merely waits some given interval of time.</summary>
    public sealed class Wait : Step
    {
      public TimeSpan Duration { get; set; }
      public string   Next     { get; set; }
      public bool     IsEnd    { get; set; }

      protected override async Task<Transition> ExecuteInnerAsync(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        await Task.Delay(impositions.WaitTimeOverride.GetValueOrDefault(Duration), cancellationToken);

        return IsEnd
          ? Transitions.Succeed(input)
          : Transitions.Next(Next, input);
      }
    }

    /// <summary>A <see cref="Step"/> that makes a decision based on it's input.</summary>
    public sealed class Choice : Step
    {
      public string Default { get; set; }

      public StepDefinition.Choice.Evaluator Evaluator { get; set; }

      protected override Task<Transition> ExecuteInnerAsync(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        return Task.FromResult(Transitions.Next(Evaluator(input) ?? Default, input));
      }
    }

    /// <summary>A <see cref="Step"/> that completes the execution with a success.</summary>
    public sealed class Succeed : Step
    {
      protected override Task<Transition> ExecuteInnerAsync(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        return Task.FromResult(Transitions.Succeed(input));
      }
    }

    /// <summary>A <see cref="Step"/> that completes the execution with a failure.</summary>
    public sealed class Fail : Step
    {
      public string Error { get; set; }
      public string Cause { get; set; }

      protected override Task<Transition> ExecuteInnerAsync(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        return Task.FromResult(Transitions.Fail());
      }
    }

    /// <summary>A <see cref="Step"/> that executes multiple other <see cref="Step"/>s in parallel.</summary>
    public sealed class Parallel : Step
    {
      public StepFunctionDefinition[] Branches { get; set; }
      public StepHandlerFactory       Factory  { get; set; }
      public string                   Next     { get; set; }
      public bool                     IsEnd    { get; set; }

      protected override async Task<Transition> ExecuteInnerAsync(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        // TODO: history isn't recorded properly when there are multiple parallel steps

        var hosts   = Branches.Select(branch => new StepFunctionHost(branch, Factory)).ToArray();
        var results = await Task.WhenAll(hosts.Select(result => result.ExecuteAsync(impositions, input, cancellationToken)));

        if (results.Any(result => result.IsFailure))
        {
          var exception = new AggregateException(
            from result in results
            where result.IsFailure
            select result.Exception
          );

          return Transitions.Fail(exception.Flatten());
        }

        return IsEnd
          ? Transitions.Succeed(input)
          : Transitions.Next(Next, input);
      }
    }

    /// <summary>This is a sealed hierarchy.</summary>
    private Step()
    {
    }
  }
}
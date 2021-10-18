using System;

namespace Amazon.StepFunction.Hosting.Example.Tests
{
  /// <summary>Base class for any test that exercises the step function.</summary>
  public abstract class StepFunctionTestCase
  {
    protected StepFunctionTestCase()
    {
      Host = StepFunctionHost.CreateFromJson(
        specification: Resources.ExampleMachine,
        factory: StepHandlers.Factory,
        impositions: new Impositions
        {
          WaitTimeOverride = TimeSpan.Zero
        }
      );
    }

    /// <summary>The configured <see cref="StepFunctionHost"/> for testing.</summary>
    protected StepFunctionHost Host { get; }
  }
}